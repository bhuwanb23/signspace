#!/usr/bin/env python3
"""
Offline matcher tuner for SIGNSPACE.

This is the twin of unity/Assets/_Project/Scripts/Matching/{PoseMath,Matcher}.cs,
in Python, so the metric can be measured and tuned on a laptop with no headset
attached. ARCHITECTURE.md says the matcher is unit-testable with recorded data
on a laptop; this is that test harness.

WHAT IT ANSWERS
---------------
1. Does the metric actually SEE the difference between a shape and its
   near-miss partner? If it does not, the near-miss can never fire and the
   product does not work.
2. How far apart are the shapes, in degrees?
3. What lock threshold is safe, given a realistic amount of tracking noise?

THE PROBLEM THIS EXISTS TO SOLVE
-------------------------------
The v1 metric -- retired 15 Nov 2026 -- was a weighted mean of per-joint
angles over every joint in the hand. U and V differ in ONE joint: how far the
index is abducted. Divided by the weight of all 25 other joints, an 18 degree
spread contributed well under a degree to the score, so the near-miss for U/V
could never fire.

The fix is to measure the difference PER FINGER: the accumulated rotation of
each finger chain compared against the target's. Spread and curl then both
register at full magnitude, and the fingers are combined with a rule that does
not dilute a single wrong finger.

Usage:
    python3 scripts/tune_match.py            # full report
    python3 scripts/tune_match.py --matrix   # just the distance matrices
"""
from __future__ import annotations

import argparse
import math
import random
import pathlib
import statistics
import sys

import hand_model as hm

SHAPE_IDS = list(hm.SHAPES.keys())

#: The values the app ships, mirrored from Matcher.Tuned* in C#. Kept in one place here so the report
#: cannot print a threshold the build does not use (it printed closeDeg 20 for a day after shipping 24).
SHIPPED_LOCKED_DEG = 12.0
SHIPPED_CLOSE_DEG = 24.0
SHIPPED_NEAR_MISS_DEG = 26.0
SHIPPED_ORIENTATION_SLACK_DEG = 15.0

WEIGHTS_ASSET = pathlib.Path(__file__).resolve().parent.parent / \
    "unity" / "Assets" / "_Project" / "Matching" / "Weights.asset"

# ------------------------------------------------------------- the v1 metric

# Joint roles, mirroring MATCH-ALGORITHM.md.
PROXIMAL_ROLES = {"base", "metacarpal"}
MID_ROLES = {"proximal"}
DISTAL_ROLES = {"distal", "tip"}


def joint_role(joint_name):
    if joint_name in ("Wrist", "Palm"):
        return "palm"
    for finger, chain in hm.FINGERS.items():
        if joint_name in chain:
            idx = chain.index(joint_name)
            if idx == 0:
                return "proximal"
            if idx == len(chain) - 2:
                return "distal"
            if idx == len(chain) - 1:
                return "tip"
            return "mid"
    return "mid"


def metric_v1_joints(target, current, weights=None):
    """The shipped v1 metric: weighted mean of per-joint angles plus a palm term.

    Kept here so the report can show side by side what changed and why.
    """
    weights = weights or {"proximal": 0.40, "mid": 0.70, "distal": 1.00, "tip": 1.00}
    total = 0.0
    wsum = 0.0
    for name in hm.JOINT_NAMES:
        if name in ("Wrist", "Palm"):
            continue
        w = weights.get(joint_role(name), 0.7)
        total += w * hm.q_angle_between(target[name], current[name])
        wsum += w
    return total / wsum if wsum else float("inf")


# ---------------------------------------------------------- the v2 metric

def finger_chain_angles(target, current, include_tip=True):
    """Per-finger accumulated-rotation angle between two hand poses."""
    out = {}
    for finger in hm.FINGER_ORDER:
        rt = hm.chain_rotation(target, finger, include_tip)
        rc = hm.chain_rotation(current, finger, include_tip)
        out[finger] = hm.q_angle_between(rt, rc)
    return out


def combine(values, mode):
    """Combine the five per-finger distances into one number."""
    vals = list(values)
    if mode == "mean":
        return sum(vals) / len(vals)
    if mode == "max":
        return max(vals)
    if mode.startswith("pnorm"):
        p = float(mode[5:])
        return (sum(v ** p for v in vals) / len(vals)) ** (1.0 / p)
    if mode == "softmax":
        # log-sum-exp with a soft maximum; tau in degrees
        tau = 8.0
        m = max(vals)
        return m + tau * math.log(sum(math.exp((v - m) / tau) for v in vals) / len(vals))
    raise ValueError(mode)


def metric_v2_chain(target, current, combine_mode="max", include_tip=True):
    """Per-finger chain distance combined across fingers, plus a wrist term."""
    angles = finger_chain_angles(target, current, include_tip)
    finger_score = combine(angles.values(), combine_mode)
    # the wrist term carries palm orientation; it is kept separate and weighted
    wrist = hm.q_angle_between(target["Wrist"], current["Wrist"])
    return finger_score, angles, wrist


# ------------------------------------------------------------- the v3 metric (P2.2)

#: Mirrors JointWeights.DefaultWeightFor() in C#. Used when the asset cannot be read, and printed
#: so a divergence between the two is visible rather than silent.
def default_weight(joint_name):
    if joint_name in ("Wrist", "Palm"):
        return 0.40
    if joint_name.endswith("Metacarpal"):
        return 0.40
    if joint_name.endswith("Tip"):
        return 0.00
    if joint_name.endswith("Distal"):
        return 1.00
    if joint_name.endswith("Intermediate"):
        return 1.00
    if joint_name.endswith("Proximal"):
        return 0.70
    return 0.70


def load_weights(path=WEIGHTS_ASSET):
    """Read the SAME asset the matcher uses.

    The weights are a product judgement that lives in a Unity asset, so the tuner must read that
    asset rather than keep a second copy of the table - otherwise "the metric scores 7.73" and "the
    metric on the headset" are two different claims that happen to look alike. A tiny line parser is
    enough: the asset is `- name: X` / `weight: Y` pairs.
    """
    weights, source = {}, "defaults (asset not found)"
    if path.exists():
        current = None
        for line in path.read_text().splitlines():
            stripped = line.strip()
            if stripped.startswith("- name:"):
                current = stripped.split(":", 1)[1].strip()
            elif stripped.startswith("weight:") and current:
                try:
                    weights[current] = float(stripped.split(":", 1)[1])
                except ValueError:
                    pass
                current = None
        if weights:
            source = str(path.relative_to(path.parents[3]))
    missing = [n for n in hm.JOINT_NAMES if n not in weights]
    if missing:
        print(f"WARNING: {len(missing)} joints absent from the weights source; filling from defaults: "
              f"{', '.join(missing[:6])}{'...' if len(missing) > 6 else ''}")
        for n in missing:
            weights[n] = default_weight(n)
    return weights, source


def per_finger_weighted_means(target, current, weights, finger_order=None):
    """P2.2: within each finger, the WEIGHTED MEAN joint angle.

    Denominator is the sum of the weights of the joints included - never the count of them. That is
    the task's second trap: with a count denominator, every distance scales by (total weight / count),
    so editing a weight in the asset would move every distance uniformly and the thresholds would look
    broken for no visible reason.
    """
    out = {}
    for finger, chain in hm.FINGERS.items():
        num = den = 0.0
        for name in chain:
            w = weights.get(name, 0.0)
            if w <= 0.0:
                continue
            num += w * hm.q_angle_between(target[name], current[name])
            den += w
        out[finger] = num / den if den else 0.0
    return out


def metric_v3_weighted(target, current, weights):
    """The shipped v3 distance: worst finger's weighted mean, plus the flat mean for reporting."""
    fingers = per_finger_weighted_means(target, current, weights)
    d = max(fingers.values())

    num = den = 0.0
    for name in hm.JOINT_NAMES:
        w = weights.get(name, 0.0)
        if w <= 0.0:
            continue
        num += w * hm.q_angle_between(target[name], current[name])
        den += w
    flat = num / den if den else 0.0

    wrist = hm.q_angle_between(target["Wrist"], current["Wrist"])
    return d, fingers, flat, wrist


def noise_floor_v3(poses, weights, sigma_deg=2.0, trials=4000, seed=7):
    """The same noise model as noise_floor(), measured with v3."""
    rng = random.Random(seed)
    samples, flat_samples = [], []
    for _ in range(trials):
        sid = rng.choice(SHAPE_IDS)
        deltas = hm.shape_deltas(sid)
        noisy = {}
        for name, q in deltas.items():
            if name in ("Wrist", "Palm"):
                noisy[name] = q
                continue
            axis = (rng.gauss(0, 1), rng.gauss(0, 1), rng.gauss(0, 1))
            if axis == (0.0, 0.0, 0.0):
                axis = (1.0, 0.0, 0.0)
            noisy[name] = hm.q_mul(q, hm.q_from_axis_angle(axis, rng.gauss(0.0, sigma_deg)))
        current = hm.local_rotations_from_deltas(noisy)
        d, _f, flat, _w = metric_v3_weighted(poses[sid], current, weights)
        samples.append(d)
        flat_samples.append(flat)
    for series in (samples, flat_samples):
        series.sort()
    def pct(series, p):
        return series[min(len(series) - 1, int(p * len(series)))]
    return {
        "d_p50": pct(samples, 0.50), "d_p95": pct(samples, 0.95),
        "d_p99": pct(samples, 0.99), "d_max": samples[-1],
        "flat_p50": pct(flat_samples, 0.50), "flat_p95": pct(flat_samples, 0.95),
    }


def report_v3(poses, sigma=2.0):
    """The report P2.2's evidence comes from: v3 vs the metric it replaced, on our own content."""
    weights, source = load_weights()
    print("\n" + "=" * 78)
    print("v3 metric -- per-joint weights, weighted mean WITHIN a finger, max ACROSS fingers")
    print("=" * 78)
    print(f"weights source: {source}")
    zero = [n for n in hm.JOINT_NAMES if weights.get(n, 0.0) <= 0.0]
    print(f"zero-weight joints ({len(zero)}): {', '.join(zero)}")
    print(f"total weight: {sum(weights.values()):.2f} over {len([n for n in hm.JOINT_NAMES if weights.get(n,0)>0])} joints")

    # matrix
    n = len(SHAPE_IDS)
    matrix = {}
    for i in range(n):
        for k in range(n):
            matrix[(SHAPE_IDS[i], SHAPE_IDS[k])] = \
                metric_v3_weighted(poses[SHAPE_IDS[i]], poses[SHAPE_IDS[k]], weights)[0]
    print_matrix(matrix, "v3 pairwise distance (degrees)")

    nearest, nearest_pair = float("inf"), None
    for i in range(n):
        for k in range(i + 1, n):
            if matrix[(SHAPE_IDS[i], SHAPE_IDS[k])] < nearest:
                nearest, nearest_pair = matrix[(SHAPE_IDS[i], SHAPE_IDS[k])], (SHAPE_IDS[i], SHAPE_IDS[k])
    print(f"  nearest pair: {nearest_pair[0]}-{nearest_pair[1]} at {nearest:.1f} deg")

    # the trap, quantified on our own shapes
    flat_u_v = metric_v3_weighted(poses["U"], poses["V"], weights)[2]
    v3_u_v = metric_v3_weighted(poses["U"], poses["V"], weights)[0]
    print(f"  U-V: v3 {v3_u_v:.1f} deg, flat weighted mean {flat_u_v:.1f} deg "
          f"({flat_u_v / v3_u_v * 100:.0f}% of it - this is the trap)")

    noise = noise_floor_v3(poses, weights, sigma)
    v2_noise = noise_floor(poses, "max", sigma, include_tip=False)
    d2 = pairwise_v2(poses, "max", include_tip=False)
    print("\n  noise under tracking noise, and the separability score (nearest pair / noise p95):")
    print(f"    {'metric':34s} {'nearest pair':>12s} {'noise p95':>10s} {'score':>6s}")
    print(f"    {'v2  max of chain angles':34s} {_nearest_v2(d2):12.1f} {v2_noise[1]:10.1f} "
          f"{_nearest_v2(d2) / v2_noise[1]:6.2f}")
    print(f"    {'v3  max of weighted finger means':34s} {nearest:12.1f} {noise['d_p95']:10.1f} "
          f"{nearest / noise['d_p95']:6.2f}   <- ships")
    print(f"    {'v3  flat weighted mean (the trap)':34s} {flat_u_v:12.1f} {noise['flat_p95']:10.1f} "
          f"{flat_u_v / noise['flat_p95']:6.2f}")

    print("\n  v3 noise percentiles (sigma = "
          f"{sigma} deg/joint): p50 {noise['d_p50']:.1f}  p95 {noise['d_p95']:.1f}  "
          f"p99 {noise['d_p99']:.1f}  max {noise['d_max']:.1f}")
    print(f"  shipped thresholds lockedDeg {SHIPPED_LOCKED_DEG:.0f} / closeDeg {SHIPPED_CLOSE_DEG:.0f} "
          f"/ nearMissMaxDeg {SHIPPED_NEAR_MISS_DEG:.0f}:")
    print(f"    lock sits {SHIPPED_LOCKED_DEG / noise['d_p95']:.1f}x above the noise p95 "
          f"and {nearest / SHIPPED_LOCKED_DEG:.1f}x below the nearest pair")
    print(f"    close leaves {nearest - SHIPPED_CLOSE_DEG:.1f} deg to the nearest pair; "
          f"a wrong shape at 26 deg reads as warming, which is the honest display")
    print(f"    palm slack {SHIPPED_ORIENTATION_SLACK_DEG:.0f} deg is separate from the band edges "
          f"(D_total = D_fingers + k*max(0, O - slack), k = 1)")
    return weights, nearest, noise


def _nearest_v2(matrix):
    """Nearest off-diagonal distance in a {(a, b): value} matrix, whichever layout it uses."""
    keys = [k for k in matrix.keys() if isinstance(k, tuple)]
    if not keys:
        return float("inf")
    return min(matrix[(a, b)] for (a, b) in keys if a != b)


# ------------------------------------------------------------------ reports

def build_poses():
    return {sid: hm.shape_local_rotations(sid) for sid in SHAPE_IDS}


def check_chain_identity(poses):
    """The runtime reads a finger's chain rotation from the chain's LAST joint.

    Prove that this equals the composed-delta chain rotation the tuner uses,
    so the numbers printed here are the numbers the shipped C# will produce.
    """
    chain_end = {"thumb": 4, "index": 9, "middle": 14, "ring": 19, "little": 24}
    worst = 0.0
    for sid, rotations in poses.items():
        for finger, end in chain_end.items():
            composed = hm.chain_rotation(rotations, finger)
            from_last = rotations[hm.JOINT_NAMES[end]]
            worst = max(worst, hm.q_angle_between(composed, from_last))
    print(f"  chain identity check: worst disagreement {worst:.6f} deg "
          f"({'PASS' if worst < 1e-3 else 'FAIL -- the tuner and the C# would disagree'})")
    return worst < 1e-3


def pairwise_v1(poses):
    m = {}
    for a in SHAPE_IDS:
        for b in SHAPE_IDS:
            m[(a, b)] = metric_v1_joints(poses[a], poses[b])
    return m


def pairwise_v2(poses, mode, include_tip=True):
    m = {}
    for a in SHAPE_IDS:
        for b in SHAPE_IDS:
            m[(a, b)] = metric_v2_chain(poses[a], poses[b], mode, include_tip)[0]
    return m


def print_matrix(matrix, title):
    print(f"\n{title}")
    header = "        " + "".join(f"{s:>9}" for s in SHAPE_IDS)
    print(header)
    for a in SHAPE_IDS:
        row = f"  {a:<6}" + "".join(f"{matrix[(a, b)]:>9.1f}" for b in SHAPE_IDS)
        print(row)


def summarise(matrix, label):
    """Report the intended near-miss pairs against the accidental ones."""
    intended = []
    accidental = []
    for (a, b) in matrix:
        if a == b:
            continue
        key = tuple(sorted((a, b)))
        d = matrix[(a, b)]
        if key in hm.NEAR_MISS_PAIRS:
            intended.append((key, d))
        else:
            accidental.append((key, d))
    # keep one entry per unordered pair
    intended = list({k: v for k, v in intended}.items())
    accidental = list({k: v for k, v in accidental}.items())

    intended.sort(key=lambda kv: kv[1])
    accidental.sort(key=lambda kv: kv[1], reverse=True)

    print(f"\n  {label}")
    print(f"  {'pair':<10} {'deg':>8}   feature")
    for (a, b), d in intended:
        print(f"  {a}-{b:<7} {d:>8.1f}   {hm.NEAR_MISS_PAIRS[tuple(sorted((a, b)))]}")
    print(f"  --- nearest accidental pair: {accidental[0][0][0]}-{accidental[0][0][1]} at {accidental[0][1]:.1f} deg")
    print(f"  --- nearest intended pair:  {intended[0][0][0]}-{intended[0][0][1]} at {intended[0][1]:.1f} deg")
    return intended, accidental


def noise_floor(poses, mode, sigma_deg=2.0, trials=4000, seed=7, include_tip=True):
    """How far does a CORRECT shape drift when the tracker is noisy?

    Perturbs every joint by a random rotation of the given magnitude and reports
    the distribution of the resulting distance to the true shape. The 95th
    percentile is the honest lower bound for a lock threshold.
    """
    rng = random.Random(seed)
    samples = []
    for _ in range(trials):
        sid = rng.choice(SHAPE_IDS)
        deltas = hm.shape_deltas(sid)
        noisy = {}
        for name, q in deltas.items():
            if name in ("Wrist", "Palm"):
                noisy[name] = q
                continue
            axis = (rng.gauss(0, 1), rng.gauss(0, 1), rng.gauss(0, 1))
            if axis == (0.0, 0.0, 0.0):
                axis = (1.0, 0.0, 0.0)
            angle = rng.gauss(0.0, sigma_deg)
            noisy[name] = hm.q_mul(q, hm.q_from_axis_angle(axis, angle))
        current = hm.local_rotations_from_deltas(noisy)
        d = metric_v2_chain(poses[sid], current, mode, include_tip)[0]
        samples.append(d)
    samples.sort()
    p50 = samples[int(0.50 * len(samples))]
    p95 = samples[int(0.95 * len(samples))]
    p99 = samples[int(0.99 * len(samples))]
    return p50, p95, p99, max(samples)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--matrix", action="store_true", help="matrices only")
    ap.add_argument("--v3-only", action="store_true", help="only the P2.2 weighted report")
    ap.add_argument("--sigma", type=float, default=2.0, help="per-joint tracking noise, degrees")
    args = ap.parse_args()

    poses = build_poses()

    if args.v3_only:
        report_v3(poses, args.sigma)
        return 0

    print("=" * 78)
    print("SIGNSPACE matcher tuning")
    print("=" * 78)
    print(f"shapes: {', '.join(SHAPE_IDS)}")
    print(f"canonical joints: {len(hm.JOINT_NAMES)}")
    check_chain_identity(poses)

    v1 = pairwise_v1(poses)
    print_matrix(v1, "v1 metric -- weighted mean over every joint (retired 15 Nov 2026)")
    summarise(v1, "v1 intended near-miss pairs")
    print("\n  ^ if the intended pairs sit near 0-2 degrees, the near-miss can never fire.")

    results = {}
    variants = []
    for include_tip in (True, False):
        for mode in ("mean", "pnorm3", "pnorm6", "pnorm8", "softmax", "max"):
            label = f"{mode}{'' if include_tip else ' (no tip)'}"
            m = pairwise_v2(poses, mode, include_tip)
            results[label] = m
            variants.append((label, mode, include_tip))

    print("\n" + "=" * 78)
    print("v2 metric -- per-finger chain rotation")
    print("=" * 78)
    for label, _mode, _tip in variants:
        if label in ("max", "max (no tip)", "pnorm8", "pnorm8 (no tip)"):
            print_matrix(results[label], f"combine = {label}")
            summarise(results[label], f"combine = {label}")

    if args.matrix:
        return 0

    print("\n" + "=" * 78)
    print("noise floor (distance from a CORRECT shape, under tracking noise)")
    print("=" * 78)
    print(f"assumed per-joint noise sigma = {args.sigma} deg  [REPLACE with the P1.5 measurement]")
    print(f"{'variant':<18} {'p50':>7} {'p95':>7} {'p99':>7} {'max':>7}")
    floors = {}
    for label, mode, include_tip in variants:
        p50, p95, p99, mx = noise_floor(poses, mode, args.sigma, include_tip=include_tip)
        floors[label] = (p50, p95, p99, mx)
        print(f"{label:<18} {p50:>7.1f} {p95:>7.1f} {p99:>7.1f} {mx:>7.1f}")

    weights, nearest, v3_noise = report_v3(poses, args.sigma)

    print("\n" + "=" * 78)
    print("decision -- the shipped metric, and what moves it")
    print("=" * 78)
    print("  a lock threshold must sit ABOVE the noise and BELOW the nearest pair.")
    print("  score = (nearest pair) / (noise p95).  higher is better.\n")
    min_accidental = min(
        metric_v3_weighted(poses[a], poses[b], weights)[0]
        for a in SHAPE_IDS for b in SHAPE_IDS
        if a != b and tuple(sorted((a, b))) not in hm.NEAR_MISS_PAIRS)
    print(f"  SHIPPING: max of weighted finger means")
    print(f"    nearest intended pair : {nearest:6.1f} deg (U-V)")
    print(f"    nearest accidental pair: {min_accidental:6.1f} deg")
    print(f"    noise p50 / p95        : {v3_noise['d_p50']:6.1f} / {v3_noise['d_p95']:6.1f} deg "
          f"at sigma {args.sigma:.1f}")
    print(f"    -> lockedDeg   {SHIPPED_LOCKED_DEG:5.1f}  "
          f"({SHIPPED_LOCKED_DEG / v3_noise['d_p95']:.1f}x the noise p95, "
          f"{nearest / SHIPPED_LOCKED_DEG:.1f}x inside the nearest pair)")
    print(f"    -> closeDeg    {SHIPPED_CLOSE_DEG:5.1f}  "
          f"({nearest - SHIPPED_CLOSE_DEG:.1f} deg of separation from the nearest pair)")
    print(f"    -> nearMissDeg {SHIPPED_NEAR_MISS_DEG:5.1f}  (exactly the nearest intended pair)")
    print(f"    -> lock clears the noise floor : "
          f"{'YES' if SHIPPED_LOCKED_DEG > v3_noise['d_p95'] else 'NO'}")
    print(f"    -> lock clears the nearest pair: "
          f"{'YES' if SHIPPED_LOCKED_DEG < nearest else 'NO'}")

    print("\n" + "=" * 78)
    print("sensitivity -- what happens if real tracking is noisier than assumed")
    print("=" * 78)
    print("  v3 re-measured at several per-joint noise levels.")
    print("  Use this to decide whether the thresholds must move after the P1.5 probe.\n")
    print(f"  {'sigma':>6} {'noise p50':>10} {'noise p95':>10} {'lock needed':>12} {'vs pair 26.0':>14}")
    for sigma in (0.5, 1.0, 1.5, 2.0, 2.5, 3.0, 4.0):
        n = noise_floor_v3(poses, weights, sigma)
        needed = max(n["d_p95"] * 1.35, n["d_p50"] + 3.0)
        verdict = "OK" if needed < nearest else "PAIRS AT RISK"
        print(f"  {sigma:>6.1f} {n['d_p50']:>10.1f} {n['d_p95']:>10.1f} {needed:>12.1f} {verdict:>14}")
    print("\n  The tripwire (BUILD-LOG section 4.1): if measured noise exceeds ~1.4x the model,")
    print("  closeDeg comes down to 20. The threshold is a measurement, not a taste.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
