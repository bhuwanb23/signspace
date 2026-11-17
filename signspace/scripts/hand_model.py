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

#: Device-side name aliases, so one pose file works on every SDK hand layout.
#:
#: Corrected 15 Nov 2026 against Meta's "Choose a Hand Skeleton Version" table.
#: The default skeleton since SDK v71 is OpenXR (26 joints, palm, no forearm);
#: the legacy OVR skeleton is 24 joints, has NO palm and NO index/middle/ring
#: metacarpal, and numbers each finger one joint lower than OpenXR does:
#: OVR Hand_Index1 is the PROXIMAL phalange, not the metacarpal. The table used
#: to assert the opposite, which would have rotated every finger one joint off
#: its real axis. Keep this table in step with JointNames.cs.
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
    # Thumb: OVR Thumb0 is the trapezium (no canonical home); Thumb1-3 are
    # metacarpal / proximal / distal and the tip is separate.
    "thumbmetacarpal": "ThumbMetacarpal",
    "thumbbase": "ThumbMetacarpal",
    "thumb1": "ThumbMetacarpal",
    "thumbproximal": "ThumbProximal",
    "thumb2": "ThumbProximal",
    "thumbdistal": "ThumbDistal",
    "thumb3": "ThumbDistal",
    "thumbtip": "ThumbTip",
    "thumb4": "ThumbTip",
    # Index: OVR has no metacarpal, so 1..3 are proximal / intermediate / distal.
    "indexmetacarpal": "IndexMetacarpal",
    "indexbase": "IndexMetacarpal",
    "indexproximal": "IndexProximal",
    "index1": "IndexProximal",
    "indexintermediate": "IndexIntermediate",
    "index2": "IndexIntermediate",
    "indexdistal": "IndexDistal",
    "index3": "IndexDistal",
    "indextip": "IndexTip",
    "index4": "IndexTip",
    "index5": "IndexTip",
    "middlemetacarpal": "MiddleMetacarpal",
    "middlebase": "MiddleMetacarpal",
    "middleproximal": "MiddleProximal",
    "middle1": "MiddleProximal",
    "middleintermediate": "MiddleIntermediate",
    "middle2": "MiddleIntermediate",
    "middledistal": "MiddleDistal",
    "middle3": "MiddleDistal",
    "middletip": "MiddleTip",
    "middle4": "MiddleTip",
    "middle5": "MiddleTip",
    "ringmetacarpal": "RingMetacarpal",
    "ringbase": "RingMetacarpal",
    "ringproximal": "RingProximal",
    "ring1": "RingProximal",
    "ringintermediate": "RingIntermediate",
    "ring2": "RingIntermediate",
    "ringdistal": "RingDistal",
    "ring3": "RingDistal",
    "ringtip": "RingTip",
    "ring4": "RingTip",
    "ring5": "RingTip",
    # Pinky is the one finger OVR gives a metacarpal (Pinky0), so it numbers
    # from 0 rather than from 1.
    "littlemetacarpal": "LittleMetacarpal",
    "pinkybase": "LittleMetacarpal",
    "pinky0": "LittleMetacarpal",
    "littleproximal": "LittleProximal",
    "pinkyproximal": "LittleProximal",
    "pinky1": "LittleProximal",
    "littleintermediate": "LittleIntermediate",
    "pinkyintermediate": "LittleIntermediate",
    "pinky2": "LittleIntermediate",
    "littledistal": "LittleDistal",
    "pinkydistal": "LittleDistal",
    "pinky3": "LittleDistal",
    "littletip": "LittleTip",
    "pinkytip": "LittleTip",
    "pinky4": "LittleTip",
    "pinky5": "LittleTip",
}


def canonical_joint_name(raw: str) -> str | None:
    """Map a device joint name onto the canonical set, or None if unknown.

    Strips the XRHand_/Hand_ prefix and any _01/_02 suffixes, lowercases, and
    removes separators, so 'XRHand_IndexProximal', 'Hand_IndexProximal', 'index
    proximal' and 'Index_Proximal' all land on the same canonical joint.
    """
    if raw is None:
        return None
    cleaned = raw.strip().lower()
    for prefix in ("xrhand_", "xrhand ", "hand_", "hand ", "finger_", "joint_"):
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
            "index":  _finger(abd=+2.0),
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
            "index":  _finger(abd=+28.0),
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

#: The six shapes authored for the 12-shape protocol but NOT shipped in the four-day build
#: (SIGNSET section 1.5). They are validated content candidates, kept out of `SHAPES` on purpose:
#: every simulator, the tuner and the shipped index iterate `SHAPES`, and adding unmeasured shapes
#: there would silently change the six-shape commitment and every number measured against it.
#:
#: Angles are the same ones the P1.5 probe authored (`make_probe_poses.py`), so the probe's
#: measurement and the candidate content describe the same hand.
DEFERRED_SHAPES = {
    "A": {
        "family": "closure",
        "description": "Fist with the thumb alongside, not across. The ambiguity is thumb "
                       "position: alongside and across both read as a closed hand.",
        "fingers": {
            "index":  _finger(flex_base=86.0, flex_prox=100.0, flex_inter=68.0, flex_distal=8.0, abd=+1.0),
            "middle": _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0),
            "ring":   _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=-2.0),
            "little": _finger(flex_base=86.0, flex_prox=100.0, flex_inter=68.0, flex_distal=8.0, abd=-4.0),
        },
        "thumb": _thumb(flex_base=12.0, flex_prox=18.0, flex_distal=8.0, abd=+14.0, opp=18.0),
    },
    "D": {
        "family": "spread",
        "description": "Index extended; middle, ring and little curved to meet the thumb tip. "
                       "Two contacts the sensor cannot resolve.",
        "fingers": {
            "index":  _finger(flex_base=4.0, abd=+2.0),
            "middle": _finger(flex_base=44.0, flex_prox=58.0, flex_inter=40.0, flex_distal=14.0),
            "ring":   _finger(flex_base=46.0, flex_prox=60.0, flex_inter=42.0, flex_distal=14.0, abd=-3.0),
            "little": _finger(flex_base=48.0, flex_prox=62.0, flex_inter=44.0, flex_distal=16.0, abd=-6.0),
        },
        "thumb": _thumb(flex_base=42.0, flex_prox=48.0, flex_distal=20.0, abd=-6.0, opp=64.0),
    },
    "I": {
        "family": "spread",
        "description": "Little finger extended; the other three folded; thumb across them.",
        "fingers": {
            "index":  _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=+1.0),
            "middle": _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0),
            "ring":   _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=-2.0),
            "little": _finger(flex_base=2.0, abd=-4.0),
        },
        "thumb": _thumb(flex_base=40.0, flex_prox=44.0, flex_distal=22.0, abd=-12.0, opp=76.0),
    },
    "L": {
        "family": "spread",
        "description": "Index up, thumb out at right angles to it; the other three folded.",
        "fingers": {
            "index":  _finger(flex_base=2.0, abd=+4.0),
            "middle": _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0),
            "ring":   _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=-2.0),
            "little": _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=-4.0),
        },
        "thumb": _thumb(flex_base=4.0, flex_prox=6.0, flex_distal=4.0, abd=+44.0, opp=4.0),
    },
    "Y": {
        "family": "spread",
        "description": "Thumb and little extended, the middle three folded. Two ends of the "
                       "hand at once.",
        "fingers": {
            "index":  _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=+1.0),
            "middle": _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0),
            "ring":   _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=-2.0),
            "little": _finger(flex_base=2.0, abd=-16.0),
        },
        "thumb": _thumb(flex_base=4.0, flex_prox=6.0, flex_distal=4.0, abd=+42.0, opp=6.0),
    },
    "F": {
        "family": "closure",
        "description": "Index tip meets the thumb tip in a ring; middle, ring and little stay "
                       "extended. The index-thumb contact makes the index look shorter.",
        "fingers": {
            "index":  _finger(flex_base=34.0, flex_prox=46.0, flex_inter=36.0, flex_distal=20.0, abd=+2.0),
            "middle": _finger(flex_base=2.0, abd=0.0),
            "ring":   _finger(flex_base=2.0, abd=-3.0),
            "little": _finger(flex_base=2.0, abd=-6.0),
        },
        "thumb": _thumb(flex_base=36.0, flex_prox=46.0, flex_distal=26.0, abd=-8.0, opp=74.0),
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
    # `twist` is the rotation about the bone's own long axis. It only appears on the wrist/palm
    # specs, and it is what lets two shapes differ in FACING while their fingers are identical
    # (the K/P probe pair). Without it the Wrist joint could only be identity, so no authored pair
    # could exercise the palm term in the metric.
    twist = spec.get("twist", 0.0)

    q = IDENTITY
    if abs(abd) > 1e-9:
        q = q_mul(q, q_from_axis_angle((0.0, 0.0, 1.0), abd))
    if abs(opp) > 1e-9:
        q = q_mul(q, q_from_axis_angle((0.0, 1.0, 0.0), opp))
    if abs(twist) > 1e-9:
        q = q_mul(q, q_from_axis_angle((0.0, 1.0, 0.0), twist))
    if abs(flex) > 1e-9:
        q = q_mul(q, q_from_axis_angle((1.0, 0.0, 0.0), flex))
    return q


def _resolve_shape(shape):
    """Accept either a shape id or a shape dict.

    P1.5 needs to author PROBE-ONLY shapes that must never appear in the shipped set:
    they are references for measuring the sensor, not content, and putting them in
    SHAPES would make make_pose.py ship them into index.json. Passing a dict through
    the same maths keeps one authoring path and one set of conventions.

    DEFERRED_SHAPES are content candidates that are authored and validated but not
    shipped in the four-day build; resolving them here keeps one authoring path for
    all twelve shapes without changing what `SHAPES` means to the simulators.
    """
    if not isinstance(shape, str):
        return shape
    if shape in SHAPES:
        return SHAPES[shape]
    return DEFERRED_SHAPES[shape]


def shape_spec(shape_id):
    """The spec for a shipped or deferred shape."""
    return _resolve_shape(shape_id)


def is_deferred(shape_id):
    return shape_id in DEFERRED_SHAPES


def shape_deltas(shape_id):
    """Per-joint delta quaternions for a shape, indexed like JOINT_NAMES."""
    shape = _resolve_shape(shape_id)
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

    # Optional wrist/palm orientation. Shapes that omit them keep both joints at identity, which
    # is every shape authored before the palm term existed - so this changes no measured degree
    # for them, and gives the K/P probe pair its 90 degrees of pronation.
    if "wrist" in shape:
        deltas["Wrist"] = _joint_delta(shape["wrist"])
    if "palm" in shape:
        deltas["Palm"] = _joint_delta(shape["palm"])

    return deltas


def local_rotations_from_deltas(deltas, neutral=None):
    """Compose per-joint deltas into absolute local rotations, JOINT_NAMES order.

    This is the one place the hierarchy is applied, so it is also the place a
    real rig rest pose would be folded in.
    """
    def _own(name):
        """This joint's own rotation, with the rest pose folded in when one is supplied."""
        return q_mul(neutral[name], deltas[name]) if neutral is not None else deltas[name]

    # Wrist and Palm are real joints, not placeholders. They were hard-coded to identity, which
    # made the palm term in the metric unfalsifiable: no authored shape could ever differ in
    # facing, so nothing could show the term doing work. K/P is the content that proves it.
    wrist = _own("Wrist")
    palm = _own("Palm")

    # Finger locals stay local: the wrist is composed in only where a basis needs the whole arm
    # (palm_sim/PalmOrientation do exactly that), so a wrist turn cannot masquerade as finger error.
    accumulated = {}
    for finger, chain in FINGERS.items():
        running = IDENTITY
        for joint in chain:
            accumulated[joint] = running
            running = q_mul(running, deltas[joint])

    rotations = {}
    for name in JOINT_NAMES:
        if name == "Wrist":
            rotations[name] = wrist
            continue
        if name == "Palm":
            rotations[name] = palm
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


def chain_rotation(rotations, finger, include_tip=True):
    """Accumulated rotation of a finger chain, root to tip.

    Composes the per-joint deltas, not the stored rotations, so each joint
    contributes exactly once.

    include_tip=False drops the terminal joint. The tip carries no shape
    information for these six letters but does carry tracking noise, and the
    noise model in tune_match.py shows it costs a measurable amount of margin.
    """
    deltas = joint_deltas_from_rotations(rotations)
    chain = FINGERS[finger] if include_tip else FINGERS[finger][:-1]
    running = IDENTITY
    for joint in chain:
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
