#!/usr/bin/env python3
"""
Per-shape tracking-noise margins, with no headset attached.

WHAT THIS ANSWERS
-----------------
`tune_match.py` proves the *metric* separates the six shipped shapes at a modelled noise level, and
`band_sim.py` proves the *bands* survive it. Both report aggregates. Neither answers the question a
person holding a headset actually asks:

    for shape B, how much room does the 12 degree lock band have before noise moves the hand
    out of B and into something else?

This script answers it per shape. For each shape it perturbs the target pose at a per-joint Gaussian
noise level, measures the shipped v3 distance back to its own target and to every other shape, and
reports:

  self p95        how far noise alone pushes a correct hand from its own shape
  rival p05       how close noise brings it to the nearest *other* shape (the 5th percentile = worst case)
  margin          rival p05 minus self p95; positive means the lock band is narrower than the gap
  wrong-lock %    frames where the hand's nearest shape is NOT the target and it is still inside 12 deg

WHAT THIS DOES NOT PROVE
------------------------
This is a model, not a measurement. It cannot show that a Quest 3S tracks at 2 deg/joint, cannot show
frame rate or thermals, and cannot stand in for a device row in TRACKABILITY.md. What it can do is say
which shapes are structurally fragile *if* the device behaves as modelled - so a headset session spends
its time on the shapes that need it, and a reviewer can see the reasoning rather than a claim.

Usage:
    python3 scripts/noise_margin_sim.py                 # sigma 1.5 / 2.0 / 3.0 / 4.0
    python3 scripts/noise_margin_sim.py --sigma 2 --trials 4000 --csv out.csv
"""
from __future__ import annotations

import argparse
import csv
import random
import pathlib
import sys

import hand_model as hm
import tune_match as tm

LOCKED_DEG = 12.0          # Matcher.TunedLockedDeg
SIGMAS = (1.5, 2.0, 3.0, 4.0)
TRIALS = 2000
SEED = 11


def noisy_current(sid, sigma_deg, rng):
    """The same noise model as tune_match.noise_floor_v3: per-joint angular jitter.

    Wrist and palm are left alone because the device's jitter on them is a different problem and the
    matcher treats the palm additively - mixing the two would hide which one moved.
    """
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
    return hm.local_rotations_from_deltas(noisy)


def pct(series, p):
    series = sorted(series)
    return series[min(len(series) - 1, int(p * len(series)))]


def measure(poses, weights, sigma, trials, seed):
    rng = random.Random(seed)
    rows = []
    for sid in tm.SHAPE_IDS:
        target = poses[sid]
        self_d, rival_d, wrong = [], [], 0
        rivals = [other for other in tm.SHAPE_IDS if other != sid]

        for _ in range(trials):
            current = noisy_current(sid, sigma, rng)
            own, _f, _flat, _w = tm.metric_v3_weighted(target, current, weights)
            distances = {other: tm.metric_v3_weighted(poses[other], current, weights)[0]
                         for other in rivals}
            nearest = min(distances, key=distances.get)
            self_d.append(own)
            rival_d.append(distances[nearest])
            if own < LOCKED_DEG and distances[nearest] < own:
                wrong += 1

        self_p95 = pct(self_d, 0.95)
        rival_p05 = pct(rival_d, 0.05)
        rows.append({
            "sigma": sigma,
            "shape": sid,
            "self_p50": pct(self_d, 0.50),
            "self_p95": self_p95,
            "rival_p05": rival_p05,
            "margin": rival_p05 - self_p95,
            "wrong_lock_pct": 100.0 * wrong / trials,
        })
    return rows


def main():
    parser = argparse.ArgumentParser(description="Per-shape noise margins (no headset)")
    parser.add_argument("--sigma", type=float, default=None,
                        help="one noise level in degrees per joint (default: the four in SIGMAS)")
    parser.add_argument("--trials", type=int, default=TRIALS)
    parser.add_argument("--csv", default="")
    args = parser.parse_args()

    poses = tm.build_poses()
    weights, source = tm.load_weights()
    sigmas = (args.sigma,) if args.sigma is not None else SIGMAS

    print("=" * 86)
    print("per-shape noise margins -- simulated, not measured on a device")
    print("=" * 86)
    print(f"shapes: {', '.join(tm.SHAPE_IDS)}   joints: {len(hm.JOINT_NAMES)}   "
          f"weights: {source}   trials/shape/level: {args.trials}")
    print(f"lock band {LOCKED_DEG:.0f} deg; 'rival p05' is the worst-case closeness of another shape\n")

    all_rows = []
    for sigma in sigmas:
        rows = measure(poses, weights, sigma, args.trials, SEED)
        all_rows.extend(rows)
        print(f"--- sigma {sigma:.1f} deg/joint")
        print(f"  {'shape':6s} {'self p50':>9s} {'self p95':>9s} {'rival p05':>10s} "
              f"{'margin':>8s} {'wrong-lock':>11s}")
        for r in rows:
            verdict = "" if r["margin"] > 0 else "  <- gap narrower than noise"
            print(f"  {r['shape']:6s} {r['self_p50']:9.1f} {r['self_p95']:9.1f} "
                  f"{r['rival_p05']:10.1f} {r['margin']:8.1f} {r['wrong_lock_pct']:10.2f}%{verdict}")
        fragile = [r["shape"] for r in rows if r["margin"] <= 0]
        worst = max(rows, key=lambda r: r["wrong_lock_pct"])
        print(f"  -> {'all six shapes keep a positive margin' if not fragile else 'no margin for: ' + ', '.join(fragile)};"
              f" highest wrong-lock {worst['wrong_lock_pct']:.2f}% on {worst['shape']}\n")

    if args.csv:
        out = pathlib.Path(args.csv)
        out.parent.mkdir(parents=True, exist_ok=True)
        with out.open("w", newline="", encoding="utf-8") as handle:
            writer = csv.DictWriter(handle, fieldnames=list(all_rows[0].keys()))
            writer.writeheader()
            for row in all_rows:
                writer.writerow({k: (round(v, 3) if isinstance(v, float) else v) for k, v in row.items()})
        print(f"wrote {len(all_rows)} rows to {out}")

    print("SIMULATED. This does not replace a device row in TRACKABILITY.md.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
