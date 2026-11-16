#!/usr/bin/env python3
"""
The P2.6 experiment that could not be run before: K vs P.

P2.6 measured the palm term on *synthetic* wrist turns, because no two authored shapes differed in
palm facing - all six left Wrist and Palm at identity. This script runs the same measurement on real
authored content: K and P are the same finger posture with 90 degrees of pronation, so the ONLY
thing that separates them is the thing the palm term exists to see.

It asserts four things:

  1. the finger distance between K and P is ~0        -> the postures really are identical
  2. the palm error O is ~90 deg                      -> the facing really does differ
  3. the full metric separates them (> close band)    -> a tracker cannot confuse them
  4. with the palm term removed they are indistinguishable (0 deg, a lock)
                                                      -> the term is doing the work, not the fingers

Usage:
    python3 scripts/facing_pair_sim.py
"""
from __future__ import annotations

import make_probe_poses as mp
import hand_model as hm
import palm_sim as ps
import tune_match as tm

LOCKED_DEG = 12.0
CLOSE_DEG = 24.0
SLACK_DEG = 15.0
K_WEIGHT = 1.0


def rotations(shape_id):
    """Absolute local rotations for a probe shape, through the one authoring path."""
    return hm.shape_local_rotations(mp.PROBE_SHAPES[shape_id])


def main() -> int:
    weights, source = tm.load_weights()
    k_pose, p_pose = rotations("K"), rotations("P")

    total, fingers, o = ps.total_distance(k_pose, p_pose, weights, K_WEIGHT, SLACK_DEG)
    # Reverse direction must agree: the metric is not allowed to depend on which shape is the target.
    total_rev, fingers_rev, o_rev = ps.total_distance(p_pose, k_pose, weights, K_WEIGHT, SLACK_DEG)

    print("=" * 78)
    print("K vs P - the facing-only pair")
    print("=" * 78)
    print(f"  weights: {source}")
    print()
    print(f"  finger distance            {fingers:6.2f} deg   (the handshape is identical)")
    print(f"  palm error O               {o:6.2f} deg   (90 deg of pronation)")
    print(f"  orientation term k*max(0,O-{SLACK_DEG:.0f}) "
          f"{K_WEIGHT * max(0.0, o - SLACK_DEG):6.2f} deg")
    print(f"  TOTAL metric               {total:6.2f} deg")
    print(f"  reverse direction          {total_rev:6.2f} deg (O {o_rev:.2f})")
    print()
    print(f"  without the palm term, K and P sit {fingers:.2f} deg apart - inside the "
          f"{LOCKED_DEG:.0f} deg lock band,")
    print("  which is the exact failure P2.6 existed to prevent.")
    print()

    checks = [
        ("postures identical (finger distance < 0.5 deg)", fingers < 0.5),
        ("facing differs (O >= 60 deg)", o >= 60.0),
        ("metric separates them (total > close band)", total > CLOSE_DEG),
        ("without the palm term they would lock (fingers < locked band)", fingers < LOCKED_DEG),
        ("direction-independent (K->P == P->K within 0.01 deg)", abs(total - total_rev) < 0.01),
    ]

    for label, ok in checks:
        print(f"  [{'PASS' if ok else 'FAIL'}] {label}")

    # Coverage: every shipped pair must still read 0, or K/P is not the first facing-only pair.
    shipped = ["B", "U", "V", "W", "C", "O"]
    shipped_poses = {sid: hm.shape_local_rotations(sid) for sid in shipped}
    worst = 0.0
    for i, a in enumerate(shipped):
        for b in shipped[i + 1:]:
            worst = max(worst, ps.orientation_error(shipped_poses[a], shipped_poses[b]))
    print()
    print(f"  largest O among the shipped six: {worst:.2f} deg "
          f"- {'PASS' if worst < 0.01 else 'FAIL'} (K/P is the first pair that differs in facing)")

    passed = all(ok for _, ok in checks) and worst < 0.01
    print()
    print("RESULT:", "PASS - the palm term has content and it is load-bearing."
          if passed else "FAIL - see the failing row above.")
    return 0 if passed else 1


if __name__ == "__main__":
    raise SystemExit(main())
