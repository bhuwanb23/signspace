#!/usr/bin/env python3
"""
Canonical hand model for SIGNSPACE.

WHY THIS FILE EXISTS
--------------------
The matcher compares *local joint rotations*. That only works if the stored pose
and the live hand are expressed in the same frame, and it only tells you
something if the metric can actually SEE the difference between two shapes.

Two hard facts drove this design:

1. A global weighted mean over all joints cannot see finger spread.
   U and V differ only in how far the index is abducted -- one joint. With 25
   joints in the denominator, an 18 deg abduction contributes ~0.6 deg to the
   score. The near-miss for U/V would then never be able to fire, and the
   near-miss is the product's entire identity. So the metric is computed
   PER FINGER and combined across fingers.

2. Within a finger, the spread lives in ONE joint (the metacarpal) but the
   curl lives in THREE. A plain per-joint mean inside the finger dilutes the
   spread again. So inside a finger we compare the *accumulated rotation of the
   whole finger chain* -- one number per finger that captures spread and curl
   at full magnitude.

The metric is therefore:

    chain_angle(finger) = angle( inverse(target_chain) * current_chain )
    distance             = combine over the 5 fingers  +  wrist term

See scripts/tune_match.py for the offline proof and docs/02-tech/MATCH-ALGORITHM.md.

COORDINATE CONVENTION
---------------------
Authoring only. The runtime never needs this; it just compares quaternions.

    +Y  along the bone, toward the child (distal)
    +Z  out of the palm (dorsal)
    +X  completes the right-handed set

    flexion    = rotation about the bone's own X axis  (curling toward the palm)
    abduction  = rotation about the bone's own Z axis  (spreading in the palm plane)
    opposition = rotation about the bone's own Y axis  (thumb swinging across the palm)

The authoring tool assumes the hand rig's rest pose has IDENTITY local
rotations (a straight-finger rig built with aligned bones, which is how the
ISDK hand rig is authored). If that turns out not to be true on device, pass
the real rest pose to make_pose.py --neutral and regenerate; nothing else changes.

JOINT NAMING
------------
Canonical names follow the OpenXR XR_EXT_hand_tracking layout, because ISDK
v207 defaults to the OpenXR hand skeleton. The legacy Oculus layout differs in
only two ways: the wrist and palm are swapped, and "Pinky" is called "Little".
scripts/make_pose.py carries the alias table, so one pose file works on both.
"""
from __future__ import annotations

import math

# --------------------------------------------------------------------- math

def q_mul(a, b):
    """Hamilton product of quaternions (x, y, z, w)."""
    ax, ay, az, aw = a
    bx, by, bz, bw = b
    return (
        aw * bx + ax * bw + ay * bz - az * by,
        aw * by - ax * bz + ay * bw + az * bx,
        aw * bz + ax * by - ay * bx + az * bw,
        aw * bw - ax * bx - ay * by - az * bz,
    )


def q_conj(q):
    x, y, z, w = q
    return (-x, -y, -z, w)


def q_from_axis_angle(axis, degrees):
    """axis is (x, y, z), need not be normalised; angle in degrees."""
    length = math.sqrt(axis[0] ** 2 + axis[1] ** 2 + axis[2] ** 2)
    if length < 1e-9 or abs(degrees) < 1e-9:
        return (0.0, 0.0, 0.0, 1.0)
    half = math.radians(degrees) * 0.5
    s = math.sin(half) / length
    return (axis[0] * s, axis[1] * s, axis[2] * s, math.cos(half))


def q_angle(q):
    """Rotation angle of a quaternion, in degrees. Uses |w| so that q == -q
    is not read as a 360 deg flip."""
    w = min(1.0, abs(q[3]))
    return 2.0 * math.degrees(math.acos(w))


def q_angle_between(a, b):
    """Angle in degrees between two rotations."""
    return q_angle(q_mul(q_conj(a), b))


IDENTITY = (0.0, 0.0, 0.0, 1.0)

# ------------------------------------------------------------------ anatomy

#: Canonical joint list, in the order the pose JSON stores them.
#: Wrist first, then Palm, then the thumb (4 bones), then four fingers (5 each).
JOINT_NAMES = [
    "Wrist",
    "Palm",
    "ThumbMetacarpal", "ThumbProximal", "ThumbDistal", "ThumbTip",
    "IndexMetacarpal", "IndexProximal", "IndexIntermediate", "IndexDistal", "IndexTip",
    "MiddleMetacarpal", "MiddleProximal", "MiddleIntermediate", "MiddleDistal", "MiddleTip",
    "RingMetacarpal", "RingProximal", "RingIntermediate", "RingDistal", "RingTip",
    "LittleMetacarpal", "LittleProximal", "LittleIntermediate", "LittleDistal", "LittleTip",
]

JOINT_INDEX = {name: i for i, name in enumerate(JOINT_NAMES)}

#: The five fingers, each as an ordered chain from root to tip.
FINGERS = {
    "thumb":  ["ThumbMetacarpal", "ThumbProximal", "ThumbDistal", "ThumbTip"],
    "index":  ["IndexMetacarpal", "IndexProximal", "IndexIntermediate", "IndexDistal", "IndexTip"],
    "middle": ["MiddleMetacarpal", "MiddleProximal", "MiddleIntermediate", "MiddleDistal", "MiddleTip"],
    "ring":   ["RingMetacarpal", "RingProximal", "RingIntermediate", "RingDistal", "RingTip"],
    "little": ["LittleMetacarpal", "LittleProximal", "LittleIntermediate", "LittleDistal", "LittleTip"],
}

FINGER_ORDER = ["thumb", "index", "middle", "ring", "little"]

#: Which joint in a finger carries the spread (abduction).
ABDUCTION_JOINT = {f: chain[0] for f, chain in FINGERS.items()}

#: Device-side name aliases, so one pose file works on both SDK hand layouts.
NAME_ALIASES = {
    "wrist": "Wrist",
    "wristroot": "Wrist",
    "palm": "Palm",
    "thumbmetacarpal": "ThumbMetacarpal",
    "thumbbase": "ThumbMetacarpal",
    "thumb1": "ThumbMetacarpal",
    "thumbproximal": "ThumbProximal",
    "thumb2": "ThumbProximal",
    "thumbdistal": "ThumbDistal",
    "thumb3": "ThumbDistal",
    "thumbtip": "ThumbTip",
    "thumb4": "ThumbTip",
    "indexmetacarpal": "IndexMetacarpal",
    "indexbase": "IndexMetacarpal",
    "index1": "IndexMetacarpal",
    "indexproximal": "IndexProximal",
    "index2": "IndexProximal",
    "indexintermediate": "IndexIntermediate",
    "index3": "IndexIntermediate",
    "indexdistal": "IndexDistal",
    "index4": "IndexDistal",
    "indextip": "IndexTip",
    "index5": "IndexTip",
    "middlemetacarpal": "MiddleMetacarpal",
    "middlebase": "MiddleMetacarpal",
    "middle1": "MiddleMetacarpal",
    "middleproximal": "MiddleProximal",
    "middle2": "MiddleProximal",
    "middleintermediate": "MiddleIntermediate",
    "middle3": "MiddleIntermediate",
    "middledistal": "MiddleDistal",
    "middle4": "MiddleDistal",
    "middletip": "MiddleTip",
    "middle5": "MiddleTip",
    "ringmetacarpal": "RingMetacarpal",
    "ringbase": "RingMetacarpal",
    "ring1": "RingMetacarpal",
    "ringproximal": "RingProximal",
    "ring2": "RingProximal",
    "ringintermediate": "RingIntermediate",
    "ring3": "RingIntermediate",
    "ringdistal": "RingDistal",
    "ring4": "RingDistal",
    "ringtip": "RingTip",
    "ring5": "RingTip",
    "littlemetacarpal": "LittleMetacarpal",
    "pinkybase": "LittleMetacarpal",
    "pinky1": "LittleMetacarpal",
    "littleproximal": "LittleProximal",
    "pinkyproximal": "LittleProximal",
    "pinky2": "LittleProximal",
    "littleintermediate": "LittleIntermediate",
    "pinkyintermediate": "LittleIntermediate",
    "pinky3": "LittleIntermediate",
    "littledistal": "LittleDistal",
    "pinkydistal": "LittleDistal",
    "pinky4": "LittleDistal",
    "littletip": "LittleTip",
    "pinkytip": "LittleTip",
    "pinky5": "LittleTip",
}


def canonical_joint_name(raw: str) -> str | None:
    """Map a device joint name onto the canonical set, or None if unknown.

    Strips the Hand_ prefix and any _01/_02 suffixes, lowercases, and removes
    separators, so 'Hand_IndexProximal', 'index proximal' and 'Index_Proximal'
    all land on the same canonical joint.
    """
    if raw is None:
        return None
    cleaned = raw.strip().lower()
    for prefix in ("hand_", "hand ", "finger_", "joint_"):
        if cleaned.startswith(prefix):
            cleaned = cleaned[len(prefix):]
    # drop trailing numeric bone suffixes like _01, _02
    while len(cleaned) > 2 and cleaned[-1].isdigit() and cleaned[-2] in "_ -":
        cleaned = cleaned[:-2]
    cleaned = cleaned.replace("_", "").replace("-", "").replace(" ", "")
    return NAME_ALIASES.get(cleaned)


# ------------------------------------------------------------------- shapes

def _finger(flex_base=0.0, flex_prox=0.0, flex_inter=0.0, flex_distal=0.0, flex_tip=0.0,
            abd=0.0):
    """Build the angle spec for one of the four long fingers.

    flex_*  : flexion in degrees at base / proximal / intermediate / distal / tip
    abd      : abduction at the metacarpal, degrees. Positive spreads away from
               the middle finger; negative brings the finger toward it.
    """
    return {
        "base":       {"flex": flex_base,   "abd": abd},
        "proximal":   {"flex": flex_prox},
        "intermediate": {"flex": flex_inter},
        "distal":     {"flex": flex_distal},
        "tip":        {"flex": flex_tip},
    }


def _thumb(flex_base=0.0, flex_prox=0.0, flex_distal=0.0, flex_tip=0.0,
           abd=0.0, opp=0.0):
    """Angle spec for the thumb.

    flex_*  : flexion at metacarpal / proximal / distal / tip
    abd      : abduction at the metacarpal (away from the index)
    opp      : opposition, the rotation that swings the thumb across the palm
    """
    return {
        "base":    {"flex": flex_base, "abd": abd, "opp": opp},
        "proximal": {"flex": flex_prox},
        "distal":  {"flex": flex_distal},
        "tip":     {"flex": flex_tip},
    }


#: The six shipped shapes.
#:
#: Chosen for four deliberate near-miss pairs. Each pair differs in exactly one
#: articulatory feature, which is what makes a near-miss diagnosis honest:
#:
#:     C <-> O    curvature and thumb contact
#:     U <-> V    index spread
#:     V <-> W    a third finger extended
#:     B <-> U    two fingers folded
#:
#: Every shape is a standard fingerspelling letter, which is the narrowest
#: possible authorship question (ADR-007).
SHAPES = {
    # ---- spread family -------------------------------------------------
    "B": {
        "family": "spread",
        "description": "Flat hand, fingers extended and together, thumb folded across the palm.",
        "fingers": {
            "index":  _finger(abd=+3.0),
            "middle": _finger(abd=0.0),
            "ring":   _finger(abd=-4.0),
            "little": _finger(abd=-8.0),
        },
        "thumb": _thumb(flex_base=32.0, flex_prox=34.0, flex_distal=16.0,
                        abd=-14.0, opp=72.0),
    },
    "U": {
        "family": "spread",
        "description": "Index and middle extended and together; ring and little folded down.",
        "fingers": {
            "index":  _finger(abd=+4.0),
            "middle": _finger(abd=0.0),
            "ring":   _finger(flex_base=86.0, flex_prox=100.0, flex_inter=68.0, flex_distal=6.0),
            "little": _finger(flex_base=86.0, flex_prox=100.0, flex_inter=68.0, flex_distal=6.0),
        },
        "thumb": _thumb(flex_base=40.0, flex_prox=44.0, flex_distal=24.0,
                        abd=-16.0, opp=84.0),
    },
    "V": {
        "family": "spread",
        "description": "Index and middle extended and spread apart; ring and little folded down.",
        "fingers": {
            "index":  _finger(abd=+19.0),
            "middle": _finger(abd=0.0),
            "ring":   _finger(flex_base=86.0, flex_prox=100.0, flex_inter=68.0, flex_distal=6.0),
            "little": _finger(flex_base=86.0, flex_prox=100.0, flex_inter=68.0, flex_distal=6.0),
        },
        "thumb": _thumb(flex_base=40.0, flex_prox=44.0, flex_distal=24.0,
                        abd=-16.0, opp=84.0),
    },
    "W": {
        "family": "spread",
        "description": "Index, middle and ring extended and spread; little folded; thumb holds it.",
        "fingers": {
            "index":  _finger(abd=+21.0),
            "middle": _finger(abd=0.0),
            "ring":   _finger(abd=-21.0),
            "little": _finger(flex_base=86.0, flex_prox=100.0, flex_inter=68.0, flex_distal=6.0),
        },
        "thumb": _thumb(flex_base=44.0, flex_prox=48.0, flex_distal=26.0,
                        abd=-18.0, opp=96.0),
    },
    # ---- closure family ------------------------------------------------
    "C": {
        "family": "closure",
        "description": "Fingers curved into a C with a visible gap; thumb opposed, not touching.",
        "fingers": {
            "index":  _finger(flex_base=38.0, flex_prox=48.0, flex_inter=34.0, abd=+1.0),
            "middle": _finger(flex_base=38.0, flex_prox=48.0, flex_inter=34.0, abd=0.0),
            "ring":   _finger(flex_base=38.0, flex_prox=48.0, flex_inter=34.0, abd=-1.0),
            "little": _finger(flex_base=38.0, flex_prox=48.0, flex_inter=34.0, abd=-2.0),
        },
        "thumb": _thumb(flex_base=22.0, flex_prox=32.0, flex_distal=18.0,
                        abd=-8.0, opp=48.0),
    },
    "O": {
        "family": "closure",
        "description": "All fingers and thumb form a closed circle; the thumb touches the fingertips.",
        "fingers": {
            "index":  _finger(flex_base=52.0, flex_prox=72.0, flex_inter=54.0, flex_distal=8.0, abd=+1.0),
            "middle": _finger(flex_base=52.0, flex_prox=72.0, flex_inter=54.0, flex_distal=8.0, abd=0.0),
            "ring":   _finger(flex_base=52.0, flex_prox=72.0, flex_inter=54.0, flex_distal=8.0, abd=-1.0),
            "little": _finger(flex_base=52.0, flex_prox=72.0, flex_inter=54.0, flex_distal=8.0, abd=-2.0),
        },
        "thumb": _thumb(flex_base=44.0, flex_prox=56.0, flex_distal=24.0,
                        abd=-12.0, opp=92.0),
    },
}

#: The four deliberate near-miss pairs, with the feature that separates them.
NEAR_MISS_PAIRS = {
    ("C", "O"): "curvature and thumb contact",
    ("U", "V"): "index spread",
    ("V", "W"): "a third finger extended",
    ("B", "U"): "two fingers folded",
}


# ------------------------------------------------------------- pose builder

def _joint_delta(spec):
    """Turn one joint's angle spec into a local delta quaternion.

    Composition order is abduction, then opposition, then flexion. Flexion is
    applied last because it is the dominant motion of the joint and it reads
    most naturally as the outermost rotation.
    """
    flex = spec.get("flex", 0.0)
    abd = spec.get("abd", 0.0)
    opp = spec.get("opp", 0.0)

    q = IDENTITY
    if abs(abd) > 1e-9:
        q = q_mul(q, q_from_axis_angle((0.0, 0.0, 1.0), abd))
    if abs(opp) > 1e-9:
        q = q_mul(q, q_from_axis_angle((0.0, 1.0, 0.0), opp))
    if abs(flex) > 1e-9:
        q = q_mul(q, q_from_axis_angle((1.0, 0.0, 0.0), flex))
    return q


def shape_deltas(shape_id):
    """Per-joint delta quaternions for a shape, indexed like JOINT_NAMES."""
    shape = SHAPES[shape_id]
    deltas = {name: IDENTITY for name in JOINT_NAMES}

    for finger in ("index", "middle", "ring", "little"):
        spec = shape["fingers"][finger]
        chain = FINGERS[finger]
        roles = ["base", "proximal", "intermediate", "distal", "tip"]
        for joint, role in zip(chain, roles):
            deltas[joint] = _joint_delta(spec[role])

    thumb_spec = shape["thumb"]
    thumb_roles = ["base", "proximal", "distal", "tip"]
    for joint, role in zip(FINGERS["thumb"], thumb_roles):
        deltas[joint] = _joint_delta(thumb_spec[role])

    return deltas


def local_rotations_from_deltas(deltas, neutral=None):
    """Compose per-joint deltas into absolute local rotations, JOINT_NAMES order.

    This is the one place the hierarchy is applied, so it is also the place a
    real rig rest pose would be folded in.
    """
    # accumulated ancestor delta for each joint
    accumulated = {}
    for finger, chain in FINGERS.items():
        running = IDENTITY
        for joint in chain:
            accumulated[joint] = running
            running = q_mul(running, deltas[joint])

    rotations = {}
    for name in JOINT_NAMES:
        if name in ("Wrist", "Palm"):
            rotations[name] = IDENTITY
            continue
        own = deltas[name]
        if neutral is not None:
            own = q_mul(neutral[name], own)
            base = q_mul(accumulated.get(name, IDENTITY), own)
        else:
            base = q_mul(accumulated[name], own)
        rotations[name] = base
    return rotations


def shape_local_rotations(shape_id, neutral=None):
    """Absolute local rotations for a shape, indexed like JOINT_NAMES.

    The rig's rest pose is assumed to have identity local rotations. If a real
    rest pose is supplied, each joint's delta is applied on top of it.
    """
    return local_rotations_from_deltas(shape_deltas(shape_id), neutral)


def joint_deltas_from_rotations(rotations):
    """Recover the per-joint DELTA rotations from absolute local rotations.

    A stored local rotation already contains its ancestors' deltas, so naively
    composing the stored rotations along a chain counts the metacarpal once per
    downstream joint. Undo that first:

        delta[j] = inverse(stored[parent]) * stored[j]

    The runtime does exactly this, which is why the matcher can work from
    Quaternion[] alone with no knowledge of the rig.
    """
    deltas = {}
    for finger, chain in FINGERS.items():
        previous = rotations["Palm"]
        for joint in chain:
            deltas[joint] = q_mul(q_conj(previous), rotations[joint])
            previous = rotations[joint]
    return deltas


def chain_rotation(rotations, finger):
    """Accumulated rotation of a finger chain, root to tip.

    Composes the per-joint deltas, not the stored rotations, so each joint
    contributes exactly once.
    """
    deltas = joint_deltas_from_rotations(rotations)
    running = IDENTITY
    for joint in FINGERS[finger]:
        running = q_mul(running, deltas[joint])
    return running


def serialize(shape_id, neutral=None):
    """Flat quaternion array for the pose JSON: 4 floats per joint, SDK order."""
    rotations = shape_local_rotations(shape_id, neutral)
    flat = []
    for name in JOINT_NAMES:
        x, y, z, w = rotations[name]
        flat.extend([round(x, 4), round(y, 4), round(z, 4), round(w, 4)])
    return flat
