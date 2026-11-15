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
The v1 metric (PoseMath.Distance) is a weighted mean of per-joint angles over
every joint in the hand. U and V differ in ONE joint -- how far the index is
abducted. Divided by the weight of all 25 other joints, an 18 degree spread
contributes well under a degree to the score. The near-miss for U/V therefore
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
import statistics
import sys

import hand_model as hm

SHAPE_IDS = list(hm.SHAPES.keys())

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
    ap.add_argument("--sigma", type=float, default=2.0, help="per-joint tracking noise, degrees")
    args = ap.parse_args()

    poses = build_poses()

    print("=" * 78)
    print("SIGNSPACE matcher tuning")
    print("=" * 78)
    print(f"shapes: {', '.join(SHAPE_IDS)}")
    print(f"canonical joints: {len(hm.JOINT_NAMES)}")
    check_chain_identity(poses)

    v1 = pairwise_v1(poses)
    print_matrix(v1, "v1 metric -- weighted mean over every joint (what ships today)")
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

    print("\n" + "=" * 78)
    print("recommendation -- the metric that separates the pairs from the noise")
    print("=" * 78)
    print("  a lock threshold must sit ABOVE the noise and BELOW the nearest pair.")
    print("  score = (nearest pair) / (noise p95).  higher is better.\n")
    rows = []
    for label, mode, include_tip in variants:
        m = results[label]
        intended = [d for (a, b), d in m.items()
                    if a != b and tuple(sorted((a, b))) in hm.NEAR_MISS_PAIRS]
        accidental = [d for (a, b), d in m.items()
                      if a != b and tuple(sorted((a, b))) not in hm.NEAR_MISS_PAIRS]
        min_intended = min(intended)
        min_accidental = min(accidental)
        p50, p95, p99, _ = floors[label]
        rows.append((min_intended / p95, label, min_intended, min_accidental, p50, p95))
    rows.sort(reverse=True)
    for score, label, mi, ma, p50, p95 in rows:
        print(f"  {label:<18} pair {mi:6.1f}  accidental {ma:6.1f}  "
              f"noise p50 {p50:5.1f} p95 {p95:5.1f}   score {score:5.2f}")

    best = rows[0]
    _score, label, mi, _ma, p50, p95 = best
    _mode_of = {lbl: (md, tp) for lbl, md, tp in variants}
    _tip_of = {lbl: (md, tp) for lbl, md, tp in variants}
    suggested_lock = max(p95 * 1.35, p50 + 3.0)
    suggested_lock = min(suggested_lock, mi * 0.72)
    suggested_close = suggested_lock * 2.4
    suggested_near = min(max(mi * 1.3, suggested_close * 0.85), 26.0)
    print(f"\n  WINNER: {label}")
    print(f"    nearest intended pair : {mi:6.1f} deg")
    print(f"    noise p50 / p95        : {p50:6.1f} / {p95:6.1f} deg")
    print(f"    -> lockedDeg   ~ {suggested_lock:5.1f}")
    print(f"    -> closeDeg    ~ {suggested_close:5.1f}")
    print(f"    -> nearMissDeg ~ {suggested_near:5.1f}")
    print(f"    -> lock clears the noise floor : {'YES' if suggested_lock > p95 else 'NO'}")
    print(f"    -> lock clears the nearest pair: {'YES' if suggested_lock < mi else 'NO'}")

    print("\n" + "=" * 78)
    print("sensitivity -- what happens if real tracking is noisier than assumed")
    print("=" * 78)
    print("  The winning metric, re-measured at several per-joint noise levels.")
    print("  Use this to decide whether lockedDeg must move after the P1.5 probe.\n")
    mode, tip = _mode_of[label]
    print(f"  {'sigma':>6} {'noise p50':>10} {'noise p95':>10} {'lock needed':>12} {'vs pair 26.0':>14}")
    for sigma in (0.5, 1.0, 1.5, 2.0, 2.5, 3.0, 4.0):
        n50, n95, _n99, _mx = noise_floor(poses, mode, sigma, include_tip=tip)
        needed = max(n95 * 1.35, n50 + 3.0)
        verdict = "OK" if needed < mi else "PAIRS AT RISK"
        print(f"  {sigma:>6.1f} {n50:>10.1f} {n95:>10.1f} {needed:>12.1f} {verdict:>14}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
