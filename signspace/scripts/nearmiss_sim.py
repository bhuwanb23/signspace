#!/usr/bin/env python3
"""P3.1 - the near-miss rule, measured before it is trusted.

The phase brief gives a rule and eight pairs to test it on. Two things are checked here before any
C# is written:

  1. ARE THE EIGHT PAIRS ACTUALLY CLOSE?  "Near-miss" is a claim about the metric, not about ASL:
     a beginner confusing C with O does not tell us our distance scores them close. Measured first,
     because the answer decides whether the rule can fire at all.
  2. WHICH RULE NEVER LIES?  The brief's rule ("exactly one other inside the lock band") against the
     looser rule that was already in Matcher (another shape nearer by a margin, inside a 26 deg
     window). "Zero false positives across 20 sloppy attempts" is a measurable claim, so it is
     measured, on both rules.

Output: phases/p3-feedback/near-miss-test.csv (the task's named artefact) + a report here.

Run:  python3 scripts/nearmiss_sim.py
"""

import csv
import itertools
import json
import math
import pathlib
import random
import sys

import hand_model as hm
import palm_sim as psim
import tune_match as tm

ROOT = pathlib.Path(__file__).resolve().parent.parent
SIGNS = ROOT / "unity" / "Assets" / "StreamingAssets" / "Signs"
PROBE = ROOT / "unity" / "Assets" / "StreamingAssets" / "Probe"
OUT = ROOT / "phases" / "p3-feedback" / "near-miss-test.csv"

LOCKED_DEG = 12.0        # Matcher.lockedDeg
NEAR_MISS_MAX_DEG = 26.0  # Matcher.nearMissMaxDeg - what the OLD rule used as its window
NEAR_MISS_MARGIN_DEG = 2.0
K, SLACK = 1.0, 15.0

#: SIGNSET.md section 5, in the order the document ranks them.
DOCUMENTED_PAIRS = [
    ("C", "O"), ("U", "V"), ("V", "W"), ("A", "O"),
    ("I", "Y"), ("L", "Y"), ("B", "U"), ("D", "F"),
]


def load(folder, shape_id):
    payload = json.loads((folder / f"{shape_id}.json").read_text())
    rot = payload["rot"]
    out = {}
    for i, name in enumerate(payload["joints"]):
        q = tuple(float(v) for v in rot[i * 4:i * 4 + 4])
        out[name] = q if q[3] >= 0.0 else tuple(-c for c in q)
    return out


def load_set(folder):
    index = json.loads((folder / "index.json").read_text())
    ids = index["shapes"] if isinstance(index, dict) else index
    return ids, {sid: load(folder, sid) for sid in ids}


def distance(a, b, weights):
    d, fingers, o = psim.total_distance(a, b, weights, K, SLACK)
    return d


def all_distances(target_set, pose, weights):
    """D to every shape in the set - 'compute all of them, it is free' (the task's own trap note)."""
    return {sid: distance(p, pose, weights) for sid, p in target_set.items()}


def noisy(pose, rng, sigma=2.0):
    out = {}
    for name, q in pose.items():
        if name in ("Wrist", "Palm"):
            out[name] = q
            continue
        axis = (rng.gauss(0, 1), rng.gauss(0, 1), rng.gauss(0, 1))
        if axis == (0.0, 0.0, 0.0):
            axis = (1.0, 0.0, 0.0)
        out[name] = hm.q_mul(q, hm.q_from_axis_angle(axis, rng.gauss(0.0, sigma)))
    return out


# ------------------------------------------------------------------ the two rules

def rule_strict(target_id, distances):
    """The brief's rule (P3.1 step 3): silent unless exactly one OTHER shape is inside the lock band.

    Returns (fires, other_id, magnitude, reason). The magnitude is D(other) - how close the hand came
    to the shape it is being named as - which is the only number in the claim that is checkable.
    """
    d_target = distances[target_id]
    others = [(sid, d) for sid, d in distances.items() if sid != target_id]
    inside = [(sid, d) for sid, d in others if d < LOCKED_DEG]
    if d_target < LOCKED_DEG:
        return False, None, 0.0, "target already locked"
    if not inside:
        return False, None, 0.0, "no other shape inside the lock band"
    if len(inside) > 1:
        return False, None, 0.0, f"ambiguous: {len(inside)} shapes inside the band"
    sid, d = inside[0]
    if not (d < d_target - NEAR_MISS_MARGIN_DEG):
        # The C# has this branch too: a hand sitting almost exactly between the target edge and the
        # other shape is closer to the other, but not by enough to be worth a claim.
        return False, sid, d, "closer to another shape, but not by the margin"
    return True, sid, d, "fires"


def rule_loose(target_id, distances):
    """What Matcher shipped: another shape nearer than the target by a margin, inside 26 deg."""
    d_target = distances[target_id]
    others = sorted(((sid, d) for sid, d in distances.items() if sid != target_id), key=lambda p: p[1])
    if d_target < LOCKED_DEG:
        return False, None, 0.0, "target already locked"
    best_id, best = others[0]
    second = others[1][1]
    if best > NEAR_MISS_MAX_DEG:
        return False, None, 0.0, "nothing inside the 26 deg window"
    if not best < second:
        return False, None, 0.0, "tie with the runner-up"
    if not best < d_target - NEAR_MISS_MARGIN_DEG:
        return False, None, 0.0, "not closer than the target by the margin"
    return True, best_id, best, "fires"


RULES = [("strict (brief P3.1.3)", rule_strict), ("loose (shipped in Matcher)", rule_loose)]


# ------------------------------------------------------------------ experiments

def section_1(pairs, weights):
    print("\n1. ARE THE EIGHT DOCUMENTED PAIRS CLOSE IN THIS METRIC?")
    print("   (authored poses, no noise - the number the near-miss has to work with)")
    print(f"   {'pair':8s} {'D':>7s} {'both shipped?':>15s}   what it means for the rule")
    rows = []
    for a, b in pairs:
        if not ((SIGNS / f"{a}.json").exists() and (SIGNS / f"{b}.json").exists()):
            missing = [s for s in (a, b) if not (SIGNS / f"{s}.json").exists()]
            print(f"   {a}/{b:6s} {'-':>7s} {'no: ' + ','.join(missing):>15s}   cannot be tested with the shipped set")
            rows.append((a, b, None, False))
            continue
        d = distance(load(SIGNS, a), load(SIGNS, b), weights)
        verdict = "inside lock band" if d < LOCKED_DEG else (
            "inside the 26 deg window" if d < NEAR_MISS_MAX_DEG else "far apart")
        print(f"   {a}/{b:6s} {d:7.1f} {'yes':>15s}   {verdict}")
        rows.append((a, b, d, True))
    return rows


def section_2(poses, weights, trials=200):
    """Ask for shape A, make shape B correctly: does the rule name B, and only then?"""
    print("\n2. THE EIGHT PAIRS, BOTH DIRECTIONS, 200 NOISY SAMPLES PER CELL")
    print("   'asked for A, made B' is the only situation where naming B is true.")
    print(f"   {'asked':6s} {'made':6s} {'D(target)':>10s} {'D(made)':>8s} {'strict':>7s} {'loose':>7s}   names")
    rows = []
    for a, b in DOCUMENTED_PAIRS:
        for asked, made in ((a, b), (b, a)):
            if asked not in poses or made not in poses:
                rows.append(dict(asked_for=asked, made=made, d_target="", d_made="", strict="", loose="",
                                 names="", reason="shape not in the shipped set"))
                continue
            rng = random.Random(hash((asked, made)) % 10000)
            strict_fire = loose_fire = 0
            d_targets, d_mades, names = [], [], set()
            for _ in range(trials):
                hand = noisy(poses[made], rng)
                dist = all_distances(poses, hand, weights)
                d_targets.append(dist[asked])
                d_mades.append(dist[made])
                for _label, rule in RULES:
                    fires, sid, _mag, _reason = rule(asked, dist)
                    if fires:
                        names.add(sid)
                if rule_strict(asked, dist)[0]:
                    strict_fire += 1
                if rule_loose(asked, dist)[0]:
                    loose_fire += 1
            print(f"   {asked:6s} {made:6s} {sum(d_targets) / trials:10.1f} {sum(d_mades) / trials:8.1f} "
                  f"{100.0 * strict_fire / trials:6.1f}% {100.0 * loose_fire / trials:6.1f}%   {','.join(sorted(names)) or '-'}")
            rows.append(dict(asked_for=asked, made=made,
                             d_target=f"{sum(d_targets) / trials:.1f}", d_made=f"{sum(d_mades) / trials:.1f}",
                             strict=f"{100.0 * strict_fire / trials:.1f}%",
                             loose=f"{100.0 * loose_fire / trials:.1f}%",
                             names=",".join(sorted(names)), reason=""))
    return rows


def section_3(ids, poses, weights, trials=2000, seed=5):
    """The G3 claim: zero false positives on sloppy attempts. A sloppy attempt is a hand that is
    near NOTHING - uniform random wrist/finger noise of 8-25 deg on any shipped shape."""
    print("\n3. FALSE POSITIVES - 2000 sloppy attempts, a hand that resembles no shape")
    rng = random.Random(seed)
    fires = {label: 0 for label, _ in RULES}
    for _ in range(trials):
        target = rng.choice(ids)
        hand = noisy(poses[target], rng, sigma=rng.uniform(8.0, 25.0))
        dist = all_distances(poses, hand, weights)
        for label, rule in RULES:
            if rule(target, dist)[0]:
                fires[label] += 1
    for label, _ in RULES:
        print(f"   {label:26s} {fires[label]:5d} / {trials}   {100.0 * fires[label] / trials:.1f}%")
    print("   -> a rule that fires on a hand near nothing is naming a shape the user did not make.")
    return fires


def section_4(weights):
    """Two shapes inside the band at once: the branch the shipped set can never reach, and the
    reason the 12-shape set will need it."""
    print("\n4. AMBIGUITY - two shapes inside the band at once, must be silence")
    probe_ids, probe = load_set(PROBE)
    print(f"   probe set: {len(probe_ids)} shapes (P1.5's), shipped set: 6")

    # the closest pair in each set, which is what decides whether ambiguity is reachable at all
    for label, subset in (("shipped (6)", {k: probe[k] for k in ("B", "U", "V", "W", "C", "O")}),
                          ("probe (20)", probe)):
        best = (None, None, 1e9)
        for a, b in itertools.combinations(sorted(subset), 2):
            d = distance(subset[a], subset[b], weights)
            if d < best[2]:
                best = (a, b, d)
        print(f"   {label:12s} closest pair {best[0]}/{best[1]} = {best[2]:.1f} deg")
        if best[2] < 2 * LOCKED_DEG:
            print(f"   {'':12s} -> under {2 * LOCKED_DEG:.0f} deg apart, so a hand between them can sit inside")
            print(f"   {'':12s}    the band twice. The ambiguity branch is reachable, and must be tested.")
            mid = {j: hm.q_mul(probe[best[0]][j], hm.q_from_axis_angle((1, 0, 0), 0)) for j in hm.JOINT_NAMES}
            # a hand halfway to the neighbour: slerp each joint halfway
            for j in hm.JOINT_NAMES:
                qa, qb = probe[best[0]][j], probe[best[1]][j]
                dot = sum(qa[i] * qb[i] for i in range(4))
                if dot < 0.0:
                    qb = tuple(-c for c in qb)
                    dot = -dot
                dot = min(1.0, max(-1.0, dot))
                theta = math.acos(dot)
                if theta < 1e-6:
                    mid[j] = qa
                else:
                    s = math.sin(theta)
                    mid[j] = tuple((math.sin((1 - 0.5) * theta) * qa[i] + math.sin(0.5 * theta) * qb[i]) / s
                                   for i in range(4))
            target_id = "B" if best[0] != "B" else "U"
            dist = all_distances(probe, mid, weights)
            inside = sorted((d, sid) for sid, d in dist.items() if d < LOCKED_DEG)
            fires, sid, mag, reason = rule_strict(target_id, dist)
            print(f"   midpoint hand, target {target_id}: inside the band = "
                  f"{', '.join(f'{sid} {d:.1f}' for d, sid in inside) or 'none'}")
            print(f"   strict rule -> fires={fires} reason: {reason}")
            return dict(closest_probe=f"{best[0]}/{best[1]}", closest_probe_deg=f"{best[2]:.1f}",
                        inside=";".join(f"{sid} {d:.1f}" for d, sid in inside), fires=str(fires), reason=reason)
    return dict(closest_probe="none under 24 deg", closest_probe_deg="", inside="", fires="False",
                reason="unreachable")


def main():
    weights, _src = tm.load_weights()
    ids, poses = load_set(SIGNS)
    print("=" * 78)
    print("P3.1  THE NEAR-MISS: is the metric close where the brief says people are confused?")
    print("=" * 78)
    print(f"   shipped set: {', '.join(ids)}")
    print(f"   lock band {LOCKED_DEG} deg   old rule window {NEAR_MISS_MAX_DEG} deg   margin {NEAR_MISS_MARGIN_DEG} deg")

    rows1 = section_1(DOCUMENTED_PAIRS, weights)
    rows2 = section_2(poses, weights)
    fires = section_3(ids, poses, weights)
    amb = section_4(weights)

    OUT.parent.mkdir(parents=True, exist_ok=True)
    with OUT.open("w", newline="") as fh:
        w = csv.writer(fh)
        w.writerow(["# P3.1 near-miss test - generated by scripts/nearmiss_sim.py"])
        w.writerow(["# rule_strict: silent unless exactly one OTHER shape is inside the lock band"])
        w.writerow(["# rule_loose: another shape nearer than the target by the margin, inside 26 deg"])
        w.writerow([])
        w.writerow(["section", "asked_for", "made", "d_target_deg", "d_made_deg", "strict", "loose", "names", "note"])
        for a, b, d, shipped in rows1:
            w.writerow(["1_pair_distance", a, b, "" if d is None else f"{d:.1f}", "", "", "", "",
                        "shipped set" if shipped else "shape not in the shipped set"])
        for r in rows2:
            w.writerow(["2_pair_both_directions", r["asked_for"], r["made"], r["d_target"], r["d_made"],
                        r["strict"], r["loose"], r["names"], r["reason"]])
        for label, _rule in RULES:
            w.writerow(["3_sloppy_false_positives", "", "", "", "", f"{fires[label]}/2000" if "strict" in label else "",
                        f"{fires[label]}/2000" if "loose" in label else "", "", label])
        w.writerow(["4_ambiguity", "B", "midpoint of closest probe pair", "", "", str(amb["fires"]), "",
                    amb["inside"], f"{amb['reason']} (closest pair {amb['closest_probe']} = {amb['closest_probe_deg']} deg)"])
    print(f"\nwrote {OUT.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
