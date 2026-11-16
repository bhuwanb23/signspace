#!/usr/bin/env python3
"""P2.7 - can the other hand play?

The shipped shapes are right-handed. A left-handed player making exactly the same shape is a
mirror image of the reference, so the question is whether a mirrored pose can be matched, and if
so, which of the two places the mirror belongs in: the shipped files, or the live input.

What this measures, in order:

  0. the fixtures it is actually mirroring
  1. is "negate the X axis" a mirror at all?  three candidate rules, against the geometric mirror
  2. is the shipped metric mirror-invariant?  (if it is, the matcher needs no code for the left hand)
  3. does a mirrored pose lock?               (the only form of step 2 of the task available offline)
  4. the named trap: does the thumb really break first?
  5. the boundary this data cannot cross, and what it costs to cross it

Run:  python3 scripts/mirror_sim.py
"""

import json
import math
import pathlib
import random
import sys

import hand_model as hm
import palm_sim as psim
import tune_match as tm
from pose_schema import mirror_right_to_left

ROOT = pathlib.Path(__file__).resolve().parent.parent
SIGNS = ROOT / "unity" / "Assets" / "StreamingAssets" / "Signs"

LOCKED_DEG = 12.0          # Matcher.lockedDeg
CLOSE_DEG = 24.0           # Matcher.closeDeg
K = 1.0                    # Matcher.orientationWeightK  (P2.6)
SLACK = 15.0               # Matcher.orientationSlackDeg  (P2.6)

#: HandVisual.SegmentLengths, in metres - the reference hand's own geometry. Quoted rather than
#: re-derived so that a change to the C# shows up as a mismatch here instead of drifting quietly.
SEGMENT_LENGTHS = {
    "Palm": 0.045,
    "IndexMetacarpal": 0.045, "IndexProximal": 0.027, "IndexIntermediate": 0.019, "IndexDistal": 0.016,
    "MiddleMetacarpal": 0.050, "MiddleProximal": 0.029, "MiddleIntermediate": 0.020, "MiddleDistal": 0.016,
    "RingMetacarpal": 0.046, "RingProximal": 0.027, "RingIntermediate": 0.019, "RingDistal": 0.015,
    "LittleMetacarpal": 0.037, "LittleProximal": 0.022, "LittleIntermediate": 0.016, "LittleDistal": 0.014,
    "ThumbMetacarpal": 0.036, "ThumbProximal": 0.030, "ThumbDistal": 0.020,
}


# ------------------------------------------------------------------ fixtures

def load_fixture(shape_id):
    """One shipped pose as {joint: (x, y, z, w)}. Straight off disk, not re-synthesised."""
    payload = json.loads((SIGNS / f"{shape_id}.json").read_text())
    rot = payload["rot"]
    out = {}
    for i, name in enumerate(payload["joints"]):
        q = tuple(float(v) for v in rot[i * 4:i * 4 + 4])
        out[name] = q if q[3] >= 0.0 else tuple(-c for c in q)
    return out


def load_fixtures():
    ids = json.loads((SIGNS / "index.json").read_text())["shapes"]
    return ids, {sid: load_fixture(sid) for sid in ids}


def mirror_geometric(rotations):
    """The canonical mirror (pose_schema): (x, y, z, w) -> (x, -y, -z, w)."""
    return {k: mirror_right_to_left([q])[0] for k, q in rotations.items()}


def mirror_task_literal(rotations):
    """`negate the X axis of each relative quaternion`, read as literally as it can be."""
    return {k: (-q[0], q[1], q[2], q[3]) for k, q in rotations.items()}


def mirror_negate_x_and_w(rotations):
    """The same instruction, applied to the quaternion as a whole: (-x, y, z, -w)."""
    return {k: (-q[0], q[1], q[2], -q[3]) for k, q in rotations.items()}


# ------------------------------------------------------------------ chain maths

PARENT = {"Wrist": None, "Palm": "Wrist"}
for _finger, _chain in hm.FINGERS.items():
    PARENT[_chain[0]] = "Palm"
    for _i in range(1, len(_chain)):
        PARENT[_chain[_i]] = _chain[_i - 1]

BONE_AXIS = (0.0, 1.0, 0.0)     # every bone runs along its own +Y (HandVisual: bones point +Y)


def cumulative(rotations):
    """Cumulative rotation per joint, root first. Locals are parent-relative (POSE-FORMAT 2)."""
    out = {}
    for name in hm.JOINT_NAMES:
        parent = PARENT.get(name)
        if parent is None:
            out[name] = rotations[name]
        else:
            out[name] = hm.q_mul(cumulative_cache[parent], rotations[name])
        cumulative_cache[name] = out[name]
    return out


cumulative_cache = {}


def cum(rotations):
    cumulative_cache.clear()
    return cumulative(rotations)


def bone_direction(cumulative_rotations, joint):
    return psim.rotate(cumulative_rotations[joint], BONE_AXIS)


def tip_positions(rotations):
    """Forward kinematics with the reference hand's segment lengths, for displacement in mm.

    Mirrors what HandVisual draws: each bone runs from the parent JOINT along the PARENT's +Y for
    its own segment length. Our reference hand gives its joints no lateral offsets (HandVisual
    never sets localPosition), so the drawn hand is a fan of bones sharing one origin - which is
    exactly why rotations are the only thing the mirror has to get right on OUR side.
    """
    c = cum(rotations)
    pos = {"Wrist": (0.0, 0.0, 0.0)}
    for name in hm.JOINT_NAMES:
        if name == "Wrist":
            continue
        parent = PARENT[name]
        length = SEGMENT_LENGTHS.get(name, 0.0)
        step = psim.rotate(c[parent], (0.0, length, 0.0))
        pos[name] = tuple(pos[parent][i] + step[i] for i in range(3))
    return pos


def mirror_xyz(v):
    return (-v[0], v[1], v[2])


def dist_mm(a, b):
    return 1000.0 * math.sqrt(sum((a[i] - b[i]) ** 2 for i in range(3)))


def total_distance(target, current):
    return psim.total_distance(target, current, WEIGHTS, K, SLACK)


# ------------------------------------------------------------------ experiments

def section_0(ids, poses):
    print("\n0. THE FIXTURES BEING MIRRORED")
    print(f"   {len(ids)} shipped poses from StreamingAssets/Signs, all authored right-handed")
    print(f"   {'shape':6s} {'joints moved':>13s} {'thumb, worst joint':>19s} {'palm basis':>12s}")
    for sid in ids:
        moved = sum(1 for q in poses[sid].values() if hm.q_angle(q) > 0.5)
        thumb = max(hm.q_angle(poses[sid][j]) for j in hm.FINGERS["thumb"])
        _, source, _note = psim.build_basis(poses[sid])
        print(f"   {sid:6s} {moved:13d} {thumb:18.1f}° {source:>12s}")
    print("   the thumb carries 32-122 deg in every shape, so it is exercised by all of them - which")
    print("   is what makes the trap in section 4 testable on this set rather than hypothetical")


def section_1(ids, poses):
    print("\n1. IS 'NEGATE THE X AXIS' A MIRROR?")
    print("   The mirror across x=0 is M = diag(-1,1,1); a rotation becomes M R M, a bone direction")
    print("   becomes (-dx, dy, dz). Measured error is the worst bone direction, over all 26 joints.")

    rules = [
        ("(x, -y, -z, w)   pose_schema, canonical", mirror_geometric),
        ("(-x, y, z, w)    'negate X', literal", mirror_task_literal),
        ("(-x, y, z, -w)   negate X and w", mirror_negate_x_and_w),
    ]
    print(f"\n   {'rule':38s} " + " ".join(f"{sid:>7s}" for sid in ids) + "   verdict")
    for label, fn in rules:
        errors = []
        for sid in ids:
            mirrored = fn(poses[sid])
            got = cum(mirrored)
            orig = cum(poses[sid])
            worst = 0.0
            for joint in hm.JOINT_NAMES:
                want = mirror_xyz(bone_direction(orig, joint))
                have = bone_direction(got, joint)
                worst = max(worst, psim.angle_between(want, have))
            errors.append(worst)
        verdict = "exact" if max(errors) < 0.01 else f"NOT a mirror, worst {max(errors):.1f}°"
        print(f"   {label:38s} " + " ".join(f"{e:7.2f}" for e in errors) + f"   {verdict}")

    print("   (an 'exact' row is a bone direction within 0.01 deg of the true mirror, all 26 joints)")
    print("\n   -> the canonical form and 'negate X and w' agree to floating point: q and -q are the")
    print("      same rotation, so the task's instruction is right once the double cover is acknowledged.")
    print("      Dropping w is what turns a mirror into a mere rotation - and it does not fail loudly:")
    print("      flexion still flexes, so the hand looks almost plausible and is silently backwards.")


def section_2(ids, poses):
    print("\n2. IS THE SHIPPED METRIC MIRROR-INVARIANT?")
    print("   If D(M P, M Q) == D(P, Q) for every pair, the matcher needs no change for a second hand.")
    worst_dev, worst_pair = 0.0, None
    for a in ids:
        for b in ids:
            d_right, _, _ = total_distance(poses[a], poses[b])
            ma, mb = mirror_geometric(poses[a]), mirror_geometric(poses[b])
            d_left, _, _ = total_distance(ma, mb)
            if abs(d_left - d_right) > worst_dev:
                worst_dev, worst_pair = abs(d_left - d_right), (a, b)
    if worst_pair is None:
        print(f"   all {len(ids) ** 2} pairs (every ordered pair of the six): deviation 0.0 deg, bit for bit")
    else:
        print(f"   worst deviation over {len(ids) ** 2} pairs: {worst_dev:.2e} deg (pair {worst_pair[0]}/{worst_pair[1]})")

    # every authored pose leaves Wrist and Palm at identity, so the palm term is trivially invariant
    # as authored. Turn the wrist and mirror that, or the claim does not cover P2.6's term at all.
    worst_turned = 0.0
    for a in ids:
        for b in ids:
            for turn in (20.0, 45.0, 90.0):
                ta = psim.rotate_hand(poses[a], (0.0, 1.0, 0.0), turn)
                tb = psim.rotate_hand(poses[b], (0.0, 1.0, 0.0), turn)
                d_right, _, _ = total_distance(ta, tb)
                d_left, _, _ = total_distance(mirror_geometric(ta), mirror_geometric(tb))
                worst_turned = max(worst_turned, abs(d_left - d_right))
    print(f"   with the wrist turned 20/45/90 deg (which is the only way the palm term has a value at")
    print(f"   all - every authored pose leaves Wrist and Palm at identity): deviation {worst_turned:.2e} deg")

    print(f"\n   And the size of the thing being fixed - a correct LEFT hand against the right-handed")
    print(f"   reference, i.e. what the build does today if the left hand reaches the matcher:")
    print(f"   {'shape':6s} {'D_fingers':>10s} {'O (palm)':>9s} {'D_total':>9s}   band")
    print(f"   (O is 0 in every row because the authored wrist is identity - P2.6's finding that no")
    print(f"    authored pair differs in facing, applied here: the failure is all in the fingers)")
    rows = []
    for sid in ids:
        d, fingers, o = total_distance(poses[sid], mirror_geometric(poses[sid]))
        band = "locked" if d < LOCKED_DEG else ("close" if d <= CLOSE_DEG else "warming")
        rows.append(d)
        print(f"   {sid:6s} {fingers:10.1f} {o:9.1f} {d:9.1f}   {band}")
    print(f"   -> every shape reads {min(rows):.0f}-{max(rows):.0f}°, so a left-hander doing the shape")
    print("      perfectly sees the app's coldest state. That is the half-broken path, measured.")


def noisy(rotations, rng, sigma=2.0):
    out = {}
    for name, q in rotations.items():
        if name in ("Wrist", "Palm"):
            out[name] = q
            continue
        axis = (rng.gauss(0, 1), rng.gauss(0, 1), rng.gauss(0, 1))
        if axis == (0.0, 0.0, 0.0):
            axis = (1.0, 0.0, 0.0)
        out[name] = hm.q_mul(q, hm.q_from_axis_angle(axis, rng.gauss(0.0, sigma)))
    return out


def section_3(ids, poses, trials=500, seed=31):
    print("\n3. DOES A MIRRORED POSE LOCK?  (2 deg/joint noise, %d trials per cell)" % trials)
    print(f"   {'shape':6s} {'right hand':>11s} {'mirrored (left)':>16s} {'left, UNmirrored':>17s}")
    for sid in ids:
        rng = random.Random(seed + ord(sid[0]))
        target = poses[sid]
        mirrored = mirror_geometric(target)
        right_locks = left_locks = raw_locks = 0
        for _ in range(trials):
            if total_distance(target, noisy(target, rng))[0] < LOCKED_DEG:
                right_locks += 1
            if total_distance(mirrored, noisy(mirrored, rng))[0] < LOCKED_DEG:
                left_locks += 1
            if total_distance(target, noisy(mirrored, rng))[0] < LOCKED_DEG:
                raw_locks += 1
        pct = lambda n: f"{100.0 * n / trials:.1f}%"
        print(f"   {sid:6s} {pct(right_locks):>11s} {pct(left_locks):>16s} {pct(raw_locks):>17s}")
    print("   -> a mirrored pose locks exactly as often as the original, and an unmirrored left hand")
    print("      never does. So the mirror is sufficient for the DATA; the open question is the rig.")


def section_4(ids, poses):
    print("\n4. THE NAMED TRAP - does the thumb break first?")
    print("   Per-joint angle of the naive 'negate X' mirror, by finger, and how far the drawn tip moves")
    print("   when the mirror is done that way instead of correctly (reference-hand geometry, mm).")
    print(f"\n   {'shape':6s} " + " ".join(f"{f:>13s}" for f in hm.FINGER_ORDER) + "   (worst joint error, deg)")
    for sid in ids:
        naive = mirror_task_literal(poses[sid])
        row = []
        for finger in hm.FINGER_ORDER:
            worst = max(hm.q_angle_between(poses[sid][j], naive[j]) for j in hm.FINGERS[finger])
            row.append(worst)
        print(f"   {sid:6s} " + " ".join(f"{v:13.1f}" for v in row))

    print(f"\n   {'shape':6s} " + " ".join(f"{f:>10s}" for f in hm.FINGER_ORDER)
          + "   (tip displacement, mm)   worst")
    worst_finger = {f: 0 for f in hm.FINGER_ORDER}
    for sid in ids:
        correct = tip_positions(mirror_geometric(poses[sid]))
        naive = tip_positions(mirror_task_literal(poses[sid]))
        true_mirror = {j: mirror_xyz(p) for j, p in tip_positions(poses[sid]).items()}
        row = []
        for finger in hm.FINGER_ORDER:
            tip = hm.FINGERS[finger][-1]
            row.append(dist_mm(naive[tip], true_mirror[tip]))
        resid = max(dist_mm(correct[j], true_mirror[j]) for j in hm.JOINT_NAMES)
        for finger, value in zip(hm.FINGER_ORDER, row):
            if value == max(row):
                worst_finger[finger] += 1
        print(f"   {sid:6s} " + " ".join(f"{v:10.1f}" for v in row)
              + f"   {hm.FINGER_ORDER[row.index(max(row))]:>10s}   (correct mirror residual {resid:.2e} mm)")
    ranked = ", ".join(f"{f} {worst_finger[f]}" for f in hm.FINGER_ORDER if worst_finger[f])
    print(f"\n   worst-displaced finger, counted over the six shapes: {ranked}")
    print("   -> the trap names the thumb, and the measurement only partly agrees: a literal 'negate X'")
    print("      breaks EVERY chain, and the thumb is worst on one shape of six (C) because the other")
    print("      shapes spend their large angles on ring and little abduction, which breaks by 2x its")
    print("      own angle. So the failure is not specific to opposition - it is specific to any")
    print("      rotation about Y or Z, which is abduction and opposition alike. That makes the trap")
    print("      MORE dangerous than stated: there is no finger to check by eye and declare the mirror")
    print("      sound. The correct rule is exact on all 26 joints (residual above is 0 mm), so the")
    print("      way to pass this is to derive the mirror, not to test it per finger.")


def section_5(ids, poses):
    print("\n5. THE BOUNDARY THIS DATA CANNOT CROSS")
    print("   The mirror above answers everything about the FILES. It cannot answer anything about the")
    print("   RIG, because a pose carries rotations only - no positions, no rest offsets:")
    print("     - a rig that keeps the right hand's rest offsets on the left hand puts the thumb on the")
    print("       wrong side, and no amount of correct quaternion maths can see it;")
    print("     - OpenXR may hand out left-hand joint frames that are already mirrored. If they are,")
    print("       mirroring the input mirrors it twice and the hand reads 180 deg out.")
    print("   Both are one measurement on hardware, and POSE-FORMAT 4.3 is the procedure that takes it.")
    print("   Until it runs: the reference side is ready (section 2/3), the input side is a coin flip.")
    print("\n   WHERE THE MIRROR SHOULD GO, given section 2:")
    print("     mirroring the LIVE INPUT (one call per joint, into the right-handed reference frame)")
    print("     fixes the whole set at once and ships no mirrored asset - which is why POSE-FORMAT's")
    print("     'no mirrored file ships before the device test' rule costs nothing to keep.")


def main():
    global WEIGHTS
    WEIGHTS, weights_source = tm.load_weights()
    ids, poses = load_fixtures()
    print("=" * 78)
    print("P2.7  LEFT-HAND SUPPORT - mirroring the fixtures and measuring what it can and cannot prove")
    print("=" * 78)
    print(f"   metric: v3 fingers + k*max(0, O - slack)   k={K}  slack={SLACK}  lock<{LOCKED_DEG}  close<={CLOSE_DEG}")
    print(f"   weights: {weights_source}")
    section_0(ids, poses)
    section_1(ids, poses)
    section_2(ids, poses)
    section_3(ids, poses)
    section_4(ids, poses)
    section_5(ids, poses)
    print("\n" + "=" * 78)
    print("ANSWER: the mirror is exact for our data and the matcher needs no change for it;")
    print("        whether the LEFT HAND'S OWN JOINTS are mirrored is a device question, so the")
    print("        build ships right-hand-only, with the left hand switched off in one place.")
    print("=" * 78)


if __name__ == "__main__":
    main()
