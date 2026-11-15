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

def finger_chain_angles(target, current):
    """Per-finger accumulated-rotation angle between two hand poses."""
    out = {}
    for finger in hm.FINGER_ORDER:
        rt = hm.chain_rotation(target, finger)
        rc = hm.chain_rotation(current, finger)
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


def metric_v2_chain(target, current, combine_mode="max"):
    """Per-finger chain distance combined across fingers, plus a wrist term."""
    angles = finger_chain_angles(target, current)
    finger_score = combine(angles.values(), combine_mode)
    # the wrist term carries palm orientation; it is kept separate and weighted
    wrist = hm.q_angle_between(target["Wrist"], current["Wrist"])
    return finger_score, angles, wrist


# ------------------------------------------------------------------ reports

def build_poses():
    return {sid: hm.shape_local_rotations(sid) for sid in SHAPE_IDS}


def pairwise_v1(poses):
    m = {}
    for a in SHAPE_IDS:
        for b in SHAPE_IDS:
            m[(a, b)] = metric_v1_joints(poses[a], poses[b])
    return m


def pairwise_v2(poses, mode):
    m = {}
    for a in SHAPE_IDS:
        for b in SHAPE_IDS:
            m[(a, b)] = metric_v2_chain(poses[a], poses[b], mode)[0]
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


def noise_floor(poses, mode, sigma_deg=2.0, trials=4000, seed=7):
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
        d = metric_v2_chain(poses[sid], current, mode)[0]
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

    v1 = pairwise_v1(poses)
    print_matrix(v1, "v1 metric -- weighted mean over every joint (what ships today)")
    summarise(v1, "v1 intended near-miss pairs")
    print("\n  ^ if the intended pairs sit near 0-2 degrees, the near-miss can never fire.")

    results = {}
    for mode in ("mean", "pnorm3", "pnorm6", "softmax", "max"):
        m = pairwise_v2(poses, mode)
        results[mode] = m

    print("\n" + "=" * 78)
    print("v2 metric -- per-finger chain rotation")
    print("=" * 78)
    for mode in ("mean", "pnorm3", "pnorm6", "softmax", "max"):
        print_matrix(results[mode], f"combine = {mode}")
        summarise(results[mode], f"combine = {mode}")

    if args.matrix:
        return 0

    print("\n" + "=" * 78)
    print("noise floor (distance from a CORRECT shape, under tracking noise)")
    print("=" * 78)
    print(f"assumed per-joint noise sigma = {args.sigma} deg  [REPLACE with the P1.5 measurement]")
    print(f"{'combine':<10} {'p50':>8} {'p95':>8} {'p99':>8} {'max':>8}")
    for mode in ("mean", "pnorm3", "pnorm6", "softmax", "max"):
        p50, p95, p99, mx = noise_floor(poses, mode, args.sigma)
        print(f"{mode:<10} {p50:>8.1f} {p95:>8.1f} {p99:>8.1f} {mx:>8.1f}")

    print("\n" + "=" * 78)
    print("recommendation")
    print("=" * 78)
    for mode in ("pnorm3", "pnorm6", "softmax", "max"):
        m = results[mode]
        intended = [d for (a, b), d in m.items()
                    if a != b and tuple(sorted((a, b))) in hm.NEAR_MISS_PAIRS]
        accidental = [d for (a, b), d in m.items()
                      if a != b and tuple(sorted((a, b))) not in hm.NEAR_MISS_PAIRS]
        min_intended = min(intended)
        min_accidental = min(accidental)
        p50, p95, p99, _ = noise_floor(poses, mode, args.sigma)
        # a lock threshold must clear the noise and stay under the nearest pair
        suggested_lock = min(max(p95 * 1.6, p50 + 4.0), min_intended * 0.75)
        suggested_close = suggested_lock * 2.2
        suggested_near = min(max(min_intended * 1.25, suggested_close * 0.9), 24.0)
        print(f"\n  combine = {mode}")
        print(f"    nearest intended pair : {min_intended:6.1f} deg")
        print(f"    nearest accidental    : {min_accidental:6.1f} deg")
        print(f"    noise p95             : {p95:6.1f} deg")
        print(f"    -> lockedDeg  ~ {suggested_lock:5.1f}")
        print(f"    -> closeDeg   ~ {suggested_close:5.1f}")
        print(f"    -> nearMissDeg~ {suggested_near:5.1f}")
        ok = min_intended > suggested_lock
        print(f"    -> lock threshold clears the nearest pair: {'YES' if ok else 'NO -- raise separation or lower lock'}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
