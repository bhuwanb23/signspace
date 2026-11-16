#!/usr/bin/env python3
"""
Offline test of the near-miss DECISION RULE (P3.1/P3.2).

This is not a hardware test and does not pretend to be one: it feeds the shipped shapes through
the same metric and the same seven conditions as `Matcher.cs`, so the rule's logic is exercised
without a headset. On-device false-positive testing (P3.2) is still owed.

For each (target, attempt) pair it records the distance to the target, the nearest other shape,
the runner-up, whether the rule fires, and what it would name. A correct rule must:

  * fire for every attempt that is a clean OTHER shape, naming that shape - the 26 deg window is
    about how close the HAND is to the wrong shape, not how far the wrong shape is from the target;
  * stay silent for the perfect target (band is Locked);
  * name U/V when the user lands on the other one of that pair, which is the case the product exists
    for (the `confusable_pair` column marks it).

Usage:
    python3 scripts/near_miss_sim.py            # writes phases/p3-feedback/near-miss-test.csv
"""
from __future__ import annotations

import csv
import pathlib

import hand_model as hm
import tune_match as tm

LOCKED_DEG = 12.0
NEAR_MISS_MAX_DEG = 26.0
MARGIN_DEG = 2.0

OUT = pathlib.Path(__file__).resolve().parent.parent / "phases" / "p3-feedback" / "near-miss-test.csv"


def decide(target_id, attempt, weights):
    d_target = tm.metric_v3_weighted(POSES[target_id], attempt, weights)[0]

    others = []
    for sid in tm.SHAPE_IDS:
        if sid == target_id:
            continue
        others.append((sid, tm.metric_v3_weighted(POSES[sid], attempt, weights)[0]))
    others.sort(key=lambda kv: kv[1])

    best_id, best = others[0]
    second = others[1][1] if len(others) > 1 else float("inf")
    fired = (
        d_target > LOCKED_DEG
        and best <= NEAR_MISS_MAX_DEG
        and best < d_target
        and best < second
        and best < d_target - MARGIN_DEG
    )
    return d_target, best_id, best, second, fired


weights, _source = tm.load_weights()
POSES = {sid: hm.shape_local_rotations(sid) for sid in tm.SHAPE_IDS}

rows = []
for target in tm.SHAPE_IDS:
    for attempt_id in tm.SHAPE_IDS:
        attempt = POSES[attempt_id]
        d_target, best_id, best, second, fired = decide(target, attempt, weights)
        rows.append({
            "target": target,
            "attempt": attempt_id,
            "distance_to_target_deg": f"{d_target:.1f}",
            "nearest_other": best_id,
            "distance_to_nearest_other_deg": f"{best:.1f}",
            "runner_up_deg": f"{second:.1f}",
            "fired": "yes" if fired else "no",
            "named": best_id if fired else "",
            "confusable_pair": "yes" if (target, attempt_id) in (("U", "V"), ("V", "U")) else "no",
            "expected": "yes" if attempt_id != target else "no",
            "source": "simulated",
        })

OUT.parent.mkdir(parents=True, exist_ok=True)
with OUT.open("w", newline="") as f:
    writer = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
    writer.writeheader()
    writer.writerows(rows)

fired_rows = [r for r in rows if r["fired"] == "yes"]
mismatches = [r for r in rows if r["fired"] != r["expected"]]
print(f"wrote {OUT.relative_to(OUT.parents[2])}: {len(rows)} pairs")
print(f"fired: {len(fired_rows)} of {len(rows)} (every clean non-target shape)")
print("confusable U/V rows: " + ", ".join(
    f"{r['target']}<-{r['attempt']} names {r['named']}" for r in rows if r["confusable_pair"] == "yes"))
print(f"rule disagreements with expectation: {len(mismatches)}")
if mismatches:
    for r in mismatches:
        print("  MISMATCH", r)
