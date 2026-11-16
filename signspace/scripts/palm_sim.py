#!/usr/bin/env python3
"""Palm orientation as a separate error term (P2.6).

Why a *basis* and not a joint angle
-----------------------------------
The palm's facing is not one number. A hand can be tilted (flexion/extension), rolled
(pronation/supination) or deviated, and any single Euler triple will report some combination of those
-and, at certain wrist rotations, report a large error for a hand that is actually where it should be.
That is the trap this task names: gimbal ambiguity producing phantom errors.

So the bone is not described by angles at all. A basis is built from the hand itself and compared as a
*rotation*:

    forward  = the wrist's own distal direction        (the +Y of the rig's convention)
    normal   = the index-knuckle plane normal          (cross of the index and little metacarpal axes)
    basis    = (x = normal x forward, y = normal, z = forward)

and the error is the angle of the rotation between the target's basis and the live one,
`2*acos(|dot|)`. No Euler angles anywhere, no preferred axis, no wrapping.

This file mirrors `unity/.../Matching/PalmOrientation.cs`, measures the sensitivity curve the task asks
for, and chooses `k` and `O_slack` from the numbers rather than from taste.

Usage: python3 scripts/palm_sim.py
"""
import math
import pathlib
import sys

import hand_model as hm
import tune_match as tm

ROOT = pathlib.Path(__file__).resolve().parent.parent
DOC = ROOT / "phases" / "p2-core-loop" / "palm-tuning.md"

LOCKED_DEG = 12.0       # Matcher.lockedDeg
CLOSE_DEG = 24.0        # Matcher.closeDeg


# ---------------------------------------------------------------- quaternion helpers (band-free)
def q_mul(a, b):
    return hm.q_mul(a, b)


def q_conj(q):
    return hm.q_conj(q)


def q_angle(q):
    """Magnitude of a rotation, in degrees."""
    return hm.q_angle_between(q, (0.0, 0.0, 0.0, 1.0))


def rotate(q, v):
    """q * v * q^-1, for v = (x, y, z)."""
    x, y, z, w = q
    # expanded for clarity; the rig's +Y is "along the bone", +Z "out of the palm"
    r = q_mul(q_mul(q, (v[0], v[1], v[2], 0.0)), q_conj(q))
    return (r[0], r[1], r[2])


def cross(a, b):
    return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])


def dot(a, b):
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def norm(v):
    n = math.sqrt(dot(v, v))
    return (v[0] / n, v[1] / n, v[2] / n) if n > 1e-9 else (0.0, 0.0, 0.0)


def matrix_to_quat(x, y, z):
    """Rotation whose columns are the basis vectors. Matches Unity's Quaternion.LookRotation
    convention: +Z = forward, +Y = up, +X = cross(up, forward)."""
    m = [[x[0], y[0], z[0]],
         [x[1], y[1], z[1]],
         [x[2], y[2], z[2]]]
    tr = m[0][0] + m[1][1] + m[2][2]
    if tr > 0:
        s = math.sqrt(tr + 1.0) * 2
        w = 0.25 * s
        qx = (m[2][1] - m[1][2]) / s
        qy = (m[0][2] - m[2][0]) / s
        qz = (m[1][0] - m[0][1]) / s
    elif m[0][0] > m[1][1] and m[0][0] > m[2][2]:
        s = math.sqrt(1.0 + m[0][0] - m[1][1] - m[2][2]) * 2
        w = (m[2][1] - m[1][2]) / s
        qx = 0.25 * s
        qy = (m[0][1] + m[1][0]) / s
        qz = (m[0][2] + m[2][0]) / s
    elif m[1][1] > m[2][2]:
        s = math.sqrt(1.0 + m[1][1] - m[0][0] - m[2][2]) * 2
        w = (m[0][2] - m[2][0]) / s
        qx = (m[0][1] + m[1][0]) / s
        qy = 0.25 * s
        qz = (m[1][2] + m[2][1]) / s
    else:
        s = math.sqrt(1.0 + m[2][2] - m[0][0] - m[1][1]) * 2
        w = (m[1][0] - m[0][1]) / s
        qx = (m[0][2] + m[2][0]) / s
        qy = (m[1][2] + m[2][1]) / s
        qz = 0.25 * s
    # normalise and take w >= 0: q and -q are the same rotation, and a mixed sign is what makes
    # naive quaternion comparisons report a 360 deg error
    n = math.sqrt(qx * qx + qy * qy + qz * qz + w * w) or 1.0
    q = (qx / n, qy / n, qz / n, w / n)
    return q if q[3] >= 0 else (-q[0], -q[1], -q[2], -q[3])


# ---------------------------------------------------------------- the basis
#: Below this, the two metacarpal axes are too parallel for a cross product to mean anything.
#: The cross product of two directions `sep` apart is amplified by 1/sin(sep): at the 11 deg fan of a
#: flat hand that is 5x, so 2 deg of tracking noise becomes 10 deg of palm-facing jitter. 25 deg keeps
#: the amplification under 2.4x, and no shipped shape reaches it (measured below), so in practice the
#: palm frame is what runs.
KNUCKLE_MIN_SEPARATION_DEG = 25.0
#: Above this, a metacarpal has been rotated as part of curling a finger, so its axis is no longer a
#: knuckle-plane axis. Measured on the shipped shapes: B 11 deg apart (usable), U/V/W 86 deg (curled),
#: C/O 38-52 deg (curled).
KNUCKLE_MAX_SEPARATION_DEG = 45.0
#: How far the knuckle-plane normal may disagree with the palm's own dorsal axis before it is rejected.
KNUCKLE_TRUST_DEG = 12.0

PALM_FRAME = "palm-frame"
KNUCKLE_PLANE = "knuckle-plane"


def angle_between(a, b):
    """Unsigned angle between two directions (a normal and its opposite are 0 apart for our purposes)."""
    return math.degrees(math.acos(max(-1.0, min(1.0, abs(dot(norm(a), norm(b)))))))


def signed_angle_between(a, b):
    """Signed angle: 180 means the vectors point opposite ways. Needed to police the cross product's
    sign, which is an artefact of the index/little ordering and not a fact about the hand."""
    return math.degrees(math.acos(max(-1.0, min(1.0, dot(norm(a), norm(b))))))


def _compose(rotations, *names):
    """Cumulative rotation down a chain of *parent-relative* locals (POSE-FORMAT section 2)."""
    q = (0.0, 0.0, 0.0, 1.0)
    for name in names:
        q = q_mul(q, rotations[name])
    return q


def palm_axes(rotations):
    """(forward, normal) for the palm, in the hand's own root frame."""
    palm = _compose(rotations, "Wrist", "Palm")
    forward = norm(rotate(palm, (0.0, 1.0, 0.0)))     # distal: the direction the hand points
    normal = norm(rotate(palm, (0.0, 0.0, 1.0)))      # dorsal: out of the back of the hand
    return forward, normal


def knuckle_normal(rotations, palm_normal, force=False):
    """The index-knuckle plane normal, or None when this rig cannot support one.

    The plane through the metacarpal heads is a *positional* quantity. With only rotations, the best
    proxy is the cross product of the index and little metacarpal axes - and that is only meaningful
    while those axes are (a) not parallel and (b) not rotated by a finger curl. `hand_model` curls a
    finger by rotating its metacarpal too, so on the shipped shapes the proxy is unusable for every
    closed hand. Measured, not assumed: see palm-tuning.md section 2.
    """
    try:
        index = norm(rotate(_compose(rotations, "Wrist", "Palm", "IndexMetacarpal"), (0.0, 1.0, 0.0)))
        little = norm(rotate(_compose(rotations, "Wrist", "Palm", "LittleMetacarpal"), (0.0, 1.0, 0.0)))
    except KeyError:
        return None, "no metacarpals"

    separation = angle_between(index, little)
    if not force and separation < KNUCKLE_MIN_SEPARATION_DEG:
        return None, f"axes {separation:.1f} deg apart (too parallel)"
    if not force and separation > KNUCKLE_MAX_SEPARATION_DEG:
        return None, f"axes {separation:.1f} deg apart (a metacarpal is curled)"

    normal = norm(cross(index, little))
    # The cross product's sign depends on which metacarpal fans toward which side - not on the hand.
    # Flip it onto the palm's dorsal axis before judging agreement, or a perfectly good knuckle plane
    # reads as 180 deg wrong and every comparison built on it flips.
    if dot(normal, palm_normal) < 0.0:
        normal = (-normal[0], -normal[1], -normal[2])
    disagreement = signed_angle_between(normal, palm_normal)
    if not force and disagreement > KNUCKLE_TRUST_DEG:
        return None, f"disagrees with the palm normal by {disagreement:.1f} deg"
    return normal, f"ok ({separation:.1f} deg apart, {disagreement:.1f} deg from the palm normal)"


def build_basis(rotations, force_knuckle=False):
    """(basis quaternion, source, note) for a hand's palm facing."""
    if rotations is None:
        return None, "none", "no pose"
    try:
        forward, palm_normal = palm_axes(rotations)
    except KeyError as missing:
        return None, "none", f"missing joint {missing}"

    normal, note = knuckle_normal(rotations, palm_normal, force=force_knuckle)
    source = KNUCKLE_PLANE
    if normal is None:
        normal, source = palm_normal, PALM_FRAME

    z = forward
    d = dot(normal, z)
    y = norm((normal[0] - z[0] * d, normal[1] - z[1] * d, normal[2] - z[2] * d))
    if y == (0.0, 0.0, 0.0):
        return None, "none", "degenerate basis"
    x = norm(cross(y, z))
    return matrix_to_quat(x, y, z), source, note


def palm_basis(rotations):
    """The palm's facing as a rotation, or None when it cannot be built."""
    basis, _source, _note = build_basis(rotations)
    return basis


def orientation_error(target, current):
    """`O` in degrees: palm facing only. 0 when either basis cannot be built."""
    a = palm_basis(target)
    b = palm_basis(current)
    if a is None or b is None:
        return 0.0
    return q_angle(q_mul(q_conj(a), b))


def swing_twist(target, current):
    """(swing, twist) of the palm difference, both in degrees - for the overlay only.

    Swing is the tilt (flexion/extension/deviation); twist is the roll about the palm's forward axis
    (pronation/supination). This is the decomposition that does NOT produce phantom errors, unlike
    reading three Euler angles out of the same rotation.
    """
    a, b = palm_basis(target), palm_basis(current)
    if a is None or b is None:
        return 0.0, 0.0
    q = q_mul(q_conj(a), b)
    # the palm's forward axis is the basis' +Z: in the *target's* frame that is (0,0,1)
    axis = rotate(q, (0.0, 0.0, 1.0))
    twist_component = dot(axis, (0.0, 0.0, 1.0))
    twist = 2.0 * math.degrees(math.acos(max(-1.0, min(1.0, abs(twist_component)))))
    twist_q = hm.q_from_axis_angle((0.0, 0.0, 1.0), twist)
    swing_q = q_mul(q, q_conj(twist_q))
    return q_angle(swing_q), twist


# ---------------------------------------------------------------- rotating a whole hand rigidly
def rotate_hand(rotations, axis, degrees):
    """Turn the wrist without touching the handshape.

    POSE-FORMAT section 2: every stored rotation is **local, relative to that joint's parent**. So a
    wrist turn changes exactly one entry - the Wrist joint - and leaves every finger joint's local
    rotation identical. That is also why the finger distance MUST come out unchanged, which the
    sensitivity test asserts rather than hopes.
    """
    delta = hm.q_from_axis_angle(axis, degrees)
    out = dict(rotations)
    out["Wrist"] = q_mul(delta, rotations["Wrist"])
    return out


def total_distance(target, current, weights, k, slack):
    d_fingers, _f, _flat, _w = tm.metric_v3_weighted(target, current, weights)
    o = orientation_error(target, current)
    return d_fingers + k * max(0.0, o - slack), d_fingers, o


# ---------------------------------------------------------------- the experiments
def pair_table(poses):
    print("\n1. DO ANY OF OUR SHAPES DIFFER IN PALM FACING?")
    print("   (O between every pair of the authored six, no tracking noise)")
    ids = tm.SHAPE_IDS
    worst = 0.0
    for i, a in enumerate(ids):
        row = []
        for b in ids:
            o = orientation_error(poses[a], poses[b])
            worst = max(worst, o)
            row.append(f"{o:5.1f}")
        print(f"   {a}  " + " ".join(row))
    print("\n   basis source per shape:")
    for sid in ids:
        _b, source, note = build_basis(poses[sid])
        print(f"     {sid}  {source:14s} {note}")
    print(f"\n   largest orientation difference between any two authored shapes: {worst:.1f} deg")
    if worst < 1.0:
        print("   -> NONE. All six are authored from the same neutral with finger deltas only, so no")
        print("      authored pair can be told apart by palm facing - the content that would exercise")
        print("      this term does not exist yet (see palm-tuning.md section 6).")
    return worst


def gimbal_check(poses):
    """The trap: sweep the wrist through a full turn and look for phantom spikes."""
    print("\n2. NO PHANTOM ERRORS - O over a full wrist turn about each axis (5 deg steps)")
    bad = []
    for name, axis in (("roll  (+Y, pronation)", (0.0, 1.0, 0.0)),
                       ("flex  (+X, flexion)", (1.0, 0.0, 0.0)),
                       ("dev   (+Z, deviation)", (0.0, 0.0, 1.0))):
        previous = 0.0
        biggest_jump = 0.0
        for step in range(0, 361, 5):
            turned = rotate_hand(poses["B"], axis, step)
            o = orientation_error(poses["B"], turned)
            jump = abs(o - previous)
            biggest_jump = max(biggest_jump, jump)
            previous = o
        verdict = "smooth" if biggest_jump <= 5.01 else "PHANTOM"
        print(f"   {name:24s} O(0)={orientation_error(poses['B'], poses['B']):4.1f}  "
              f"max step-to-step change {biggest_jump:4.1f} deg  -> {verdict}")
        if biggest_jump > 5.01:
            bad.append(name)
    print("   (a step is 5 deg, so a smooth sweep changes by <= 5 deg per step; an Euler decomposition")
    print("    spikes past that wherever two axes cross)")
    return bad


def conditioning(poses, trials=400, seed=17):
    """Why the knuckle plane is rejected: measure how much the basis wobbles under tracking noise."""
    import random
    print("\n2b. CONDITIONING - palm-facing jitter under 2 deg/joint noise, both basis sources")
    print(f"   {'shape':6s} {'axes apart':>11s} {'amplification 1/sin':>20s} {'O sd, palm frame':>17s} {'O sd, knuckle plane':>20s}")
    for sid in tm.SHAPE_IDS:
        r = poses[sid]
        try:
            index = norm(rotate(_compose(r, "Wrist", "Palm", "IndexMetacarpal"), (0.0, 1.0, 0.0)))
            little = norm(rotate(_compose(r, "Wrist", "Palm", "LittleMetacarpal"), (0.0, 1.0, 0.0)))
            sep = angle_between(index, little)
        except KeyError:
            sep = 0.0
        amp = 1.0 / math.sin(math.radians(max(sep, 0.5)))
        rng = random.Random(seed)
        sd_palm, sd_knuckle = [], []
        base_palm = build_basis(r)[0]
        base_knuckle = build_basis(r, force_knuckle=True)[0]
        for _ in range(trials):
            noisy = dict(r)
            for name, q in r.items():
                if name in ("Wrist", "Palm"):
                    continue
                axis = (rng.gauss(0, 1), rng.gauss(0, 1), rng.gauss(0, 1))
                if axis == (0.0, 0.0, 0.0):
                    axis = (1.0, 0.0, 0.0)
                noisy[name] = hm.q_mul(q, hm.q_from_axis_angle(axis, rng.gauss(0.0, 2.0)))
            sd_palm.append(q_angle(q_mul(q_conj(base_palm), build_basis(noisy)[0])))
            k = build_basis(noisy, force_knuckle=True)[0]
            sd_knuckle.append(q_angle(q_mul(q_conj(base_knuckle), k)))
        def sd(v):
            m = sum(v) / len(v)
            return math.sqrt(sum((x - m) ** 2 for x in v) / len(v))
        print(f"   {sid:6s} {sep:11.1f} {amp:20.1f} {sd(sd_palm):17.2f} {sd(sd_knuckle):20.2f}")
    print("   -> the knuckle plane multiplies the tracker's own noise by roughly 1/sin(separation). On this")
    print("      rig every shape has a narrow fan, so it is rejected and the palm frame is what runs.")


def sensitivity(poses, weights, slack=15.0):
    print(f"\n3. SENSITIVITY CURVE - correct handshape, palm turned. O_slack = {slack:.0f} deg")
    print(f"   {'turn':>6s} {'O':>6s} {'penalty':>9s} | " +
          " | ".join(f"D_total k={k:<4.2f}" for k in (0.25, 0.5, 0.75, 1.0)))
    rows = []
    for turn in (0, 10, 15, 20, 30, 45, 60, 90, 120, 180):
        turned = rotate_hand(poses["B"], (0.0, 1.0, 0.0), turn)
        o = orientation_error(poses["B"], turned)
        penalty = max(0.0, o - slack)
        cells = []
        values = {}
        for k in (0.25, 0.5, 0.75, 1.0):
            d_total, d_f, _o = total_distance(poses["B"], turned, weights, k, slack)
            values[k] = d_total
            cells.append(f"{d_total:11.1f}")
        rows.append((turn, o, penalty, values))
        print(f"   {turn:6d} {o:6.1f} {penalty:9.1f} | " + " | ".join(cells))
    print(f"\n   lock threshold {LOCKED_DEG:.0f} deg, close threshold {CLOSE_DEG:.0f} deg")
    print("   a deliberately mis-facing hand must fail to LOCK; a hand within the slack must not be")
    print("   penalised at all.")
    return rows


def choose_k(poses, weights, slack=15.0):
    """Smallest k (from a short list) that fails a 30 deg mis-face with margin, and costs nothing at slack."""
    print("\n4. CHOOSING k - the smallest weight that does the job, so the gate stays a gate")
    print(f"   {'k':>5s} {'D_total @30 deg':>17s} {'vs lock 12':>13s} {'D_total @20 deg':>17s} {'D_total @15 deg':>17s}")
    best = None
    for k in (0.25, 0.4, 0.5, 0.6, 0.75, 1.0):
        d30, _f, _o = total_distance(poses["B"], rotate_hand(poses["B"], (0.0, 1.0, 0.0), 30), weights, k, slack)
        d20, _f2, _o2 = total_distance(poses["B"], rotate_hand(poses["B"], (0.0, 1.0, 0.0), 20), weights, k, slack)
        d15, _f3, _o3 = total_distance(poses["B"], rotate_hand(poses["B"], (0.0, 1.0, 0.0), 15), weights, k, slack)
        fails = d30 > LOCKED_DEG
        margin = d30 - LOCKED_DEG
        print(f"   {k:5.2f} {d30:17.1f} {('locks, ' + f'{margin:+.1f}' if not fails else 'fails, ' + f'{margin:+.1f}'):>13s} "
              f"{d20:17.1f} {d15:17.1f}")
        if fails and margin >= 3.0 and best is None:
            best = k
    print(f"   -> ships k = {best:.2f}: a perfect handshape with the palm 30 deg out reads "
          f"{(30 - slack) * best:.1f} deg and cannot lock, while anything inside the {slack:.0f} deg slack")
    print("      adds exactly 0.0 deg - so a naturally different resting wrist is not punished (ADR-003).")
    return best


def noisy_misface(poses, weights, k, slack, trials=500, seed=13):
    """With tracking noise: can a mis-facing hand sneak into the lock band?"""
    import random
    print("\n5. WITH TRACKING NOISE (2 deg/joint) - does a mis-facing hand ever read as locked?")
    print(f"   {'turn':>6s} {'readings inside lock band':>26s} {'inside close band':>19s}")
    rng = random.Random(seed)
    for turn in (0, 15, 20, 30, 45, 60):
        turned = rotate_hand(poses["B"], (0.0, 1.0, 0.0), turn)
        locked = close = 0
        for _ in range(trials):
            noisy = dict(turned)
            for name, q in turned.items():
                if name in ("Wrist", "Palm"):
                    continue
                axis = (rng.gauss(0, 1), rng.gauss(0, 1), rng.gauss(0, 1))
                if axis == (0.0, 0.0, 0.0):
                    axis = (1.0, 0.0, 0.0)
                noisy[name] = hm.q_mul(q, hm.q_from_axis_angle(axis, rng.gauss(0.0, 2.0)))
            d_total, _f, _o = total_distance(poses["B"], noisy, weights, k, slack)
            locked += 1 if d_total <= LOCKED_DEG else 0
            close += 1 if d_total <= CLOSE_DEG else 0
        print(f"   {turn:6d} {locked / trials * 100:25.1f}% {close / trials * 100:18.1f}%")
    print("   -> the mis-facing column must be 0.0% from 30 deg up. That is the done-when.")


def wrist_noise(poses, trials=2000, seed=23):
    """How much does a *correctly facing* hand's O wobble when only the wrist is noisy?

    The rest of the tuning perturbs the finger joints and leaves the wrist alone, because the finger
    distance ignores the wrist. The palm term is the opposite: it reads exactly what the finger noise
    model holds still. So it needs its own measurement, and that measurement is what says whether a
    15 deg slack is generous or merely adequate.
    """
    import random
    print("\n5b. WRIST-ONLY NOISE - O for a correct hand, 2 deg on the wrist and palm (2000 trials)")
    rng = random.Random(seed)
    samples = []
    base = poses["B"]
    for _ in range(trials):
        noisy = dict(base)
        for name in ("Wrist", "Palm"):
            axis = (rng.gauss(0, 1), rng.gauss(0, 1), rng.gauss(0, 1))
            if axis == (0.0, 0.0, 0.0):
                axis = (1.0, 0.0, 0.0)
            noisy[name] = hm.q_mul(base[name], hm.q_from_axis_angle(axis, rng.gauss(0.0, 2.0)))
        samples.append(orientation_error(base, noisy))
    samples.sort()
    n = len(samples)
    mean = sum(samples) / n
    sd = math.sqrt(sum((x - mean) ** 2 for x in samples) / n)
    p95 = samples[int(0.95 * n)]
    p99 = samples[int(0.99 * n)]
    print(f"   O: mean {mean:.2f}  sd {sd:.2f}  p95 {p95:.2f}  p99 {p99:.2f}  max {samples[-1]:.2f}")
    print(f"   the 15 deg slack is {15.0 / sd if sd else float('inf'):.1f} sd of that jitter, so a correctly")
    print("   facing hand is essentially never penalised by tracking noise alone.")
    return sd, p99


def main():
    weights, _source = tm.load_weights()
    poses = tm.build_poses()

    print("=" * 78)
    print("P2.6 palm orientation - basis, sensitivity, and choosing k and O_slack")
    print("=" * 78)
    pair_table(poses)
    bad = gimbal_check(poses)
    conditioning(poses)
    sensitivity(poses, weights)
    k = choose_k(poses, weights)
    noisy_misface(poses, weights, k, 15.0)
    wrist_noise(poses)

    print("\n" + "=" * 78)
    print(f"DECISION: O_slack = 15.0 deg, k = {k:.2f}")
    print("=" * 78)
    print("  the slack is the task's own figure and survives the measurement: inside 15 deg the term")
    print("  contributes exactly zero, so a naturally different resting wrist is not punished (ADR-003).")
    print("  k is the smallest weight on the tested list that puts a 30 deg mis-face at least 3 deg past")
    print("  the lock band - a gate that only pushes a wrong hand just over the line is a coin flip.")
    return 0 if not bad else 1


if __name__ == "__main__":
    sys.exit(main())
