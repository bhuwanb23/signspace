#!/usr/bin/env python3
"""The pose file schema — ONE definition, used by everything.

P2.1's job is to freeze this format, and the reason it matters is in the task's own words: every
downstream system reads this one file format, so getting it wrong on Day 3 means rewriting on Day 8.

Before P2.1 the rules lived in three places that could disagree:

    scripts/make_pose.py       validate()   - run at write time
    scripts/make_probe_poses.py  (inline)   - a second, weaker copy
    scripts/validate_poses.py  check()      - run before shipping
    unity/.../HandPose.cs      FromJson()   - the only one that runs on the device

Four copies of a contract is four chances for the authoring tool's good habits to be mistaken for
the loader enforcing something (the exact mistake P1.4 found: rotations were applied positionally
while every file happened to be in canonical order). This module is now the single source of truth
for the *rules*; `HandPose.cs` remains the independent implementation on the device, on purpose —
one shared implementation would mean a bug that agrees with itself.

WHAT FROZE IN P2.1 (schema version 2)
  renamed : jointNames -> joints,  q -> rot,  neutralCaptured -> captured
  added   : signer, vantage, confidence{authoring, measured, note}
  added   : id must match the file name
  added   : rot is documented as XYZW (it always was; now the name says so)

Version 1 files remain READABLE — `migrate_v1()` is the migration path the task's trap asks for.
"""
from __future__ import annotations

import datetime
import math

#: Frozen in P2.1. Bump only on a breaking change; both the loader and the validator refuse unknowns.
SCHEMA_VERSION = 2

#: The oldest schema the loader still accepts, through migrate_v1().
MIN_READABLE_VERSION = 1

JOINT_COUNT = 26

HANDEDNESS = ("right", "left")

#: Where the rotations were observed from. This is not decoration: a first-person capture of a
#: signer's own hand and a third-person capture look different for shapes where the thumb hides
#: behind the fingers, and a "mirrored" vantage silently flips chirality.
VANTAGES = ("authored-neutral", "first-person-palm-down", "first-person-other", "third-person", "mirrored")

SOURCES = ("reference-authored", "probe-reference", "signer-verified")

AUTHORING_CONFIDENCE = ("high", "medium", "low")

#: Filled in after a trackability session (P1.5), never guessed.
MEASURED_CONFIDENCE = (None, "High", "Medium", "Low", "Never")

REQUIRED = (
    "version", "id", "handedness", "signer", "captured", "vantage",
    "jointCount", "joints", "rot", "source", "confidence",
)

OPTIONAL = ("family", "description", "notes", "probeOnly")

#: Keys that only existed in schema 1. Finding one in a v2 file means a half-migrated writer.
LEGACY_KEYS = ("jointNames", "q", "neutralCaptured")

#: A quaternion counts as normalised inside this tolerance, on |q|^2.
NORM_TOLERANCE = 0.01


# --------------------------------------------------------------------------------------------------
# writing
# --------------------------------------------------------------------------------------------------

def canonicalize_quat(q):
    """The same rotation with w >= 0.

    Deliberately part of the WRITER, not the validator: q and -q are the same rotation, so a file
    written with either is correct, but two files describing one pose should compare equal in every
    diff, hash and fixture. Ties at w == 0 are broken on the first non-zero component so the choice
    is deterministic rather than dependent on floating-point noise.
    """
    x, y, z, w = (float(v) for v in q)
    negate = (w < 0) or (w == 0 and (x < 0 or (x == 0 and (y < 0 or (y == 0 and z < 0)))))
    if negate:
        return (-x, -y, -z, -w)
    return (x, y, z, w)


def build_payload(*, shape_id, joints, rot, signer, captured, vantage, source,
                  confidence, handedness="right", family=None, description=None,
                  notes=None, probe_only=False):
    """Assemble a v2 payload. Every writer goes through this, so no writer can invent a field set."""
    payload = {
        "version": SCHEMA_VERSION,
        "id": shape_id,
        "handedness": handedness,
        "signer": signer,
        "captured": captured,
        "vantage": vantage,
        "jointCount": len(joints),
        "joints": list(joints),
        "rot": [round(v, 4) for q in _chunks(rot, 4) for v in canonicalize_quat(q)],
        "source": source,
        "confidence": {
            "authoring": confidence.get("authoring", "medium"),
            "measured": confidence.get("measured"),
            "note": confidence.get("note", ""),
        },
    }
    if family:
        payload["family"] = family
    if description:
        payload["description"] = description
    if notes:
        payload["notes"] = notes
    if probe_only:
        payload["probeOnly"] = True
    return payload


def _chunks(values, size):
    for i in range(0, len(values), size):
        yield values[i:i + size]


# --------------------------------------------------------------------------------------------------
# migration
# --------------------------------------------------------------------------------------------------

def migrate_v1(d, *, signer="legacy-v1-file", vantage="authored-neutral"):
    """Turn a schema-1 payload into a schema-2 one.

    The three renames are mechanical, but two fields cannot be invented from a v1 file: `signer`
    (whose neutral the rotations are relative to) and `vantage`. They are set to an explicit
    "unknown/legacy" label rather than to a plausible-looking value, because a wrong signer would
    make the file look certifiable when nobody certified it.
    """
    out = dict(d)
    if "jointNames" in out:
        out["joints"] = out.pop("jointNames")
    if "q" in out:
        out["rot"] = out.pop("q")
    if "neutralCaptured" in out:
        out["captured"] = out.pop("neutralCaptured")

    out["version"] = SCHEMA_VERSION
    out["signer"] = out.get("signer") or signer
    out["vantage"] = out.get("vantage") or vantage
    out["source"] = out.get("source") or "reference-authored"

    conf = out.get("confidence")
    if not isinstance(conf, dict):
        # v1 carried `authoringConfidence` only on the probe set; the shipped six carried nothing.
        authoring = out.pop("authoringConfidence", None) or out.pop("authoring", None) or "medium"
        out.pop("authoringConfidence", None)
        conf = {"authoring": authoring, "measured": None,
                "note": "migrated from schema 1; no signer review recorded"}
    out["confidence"] = conf
    return out


# --------------------------------------------------------------------------------------------------
# validation
# --------------------------------------------------------------------------------------------------

def _is_iso8601(value):
    if not isinstance(value, str) or not value:
        return False
    text = value[:-1] + "+00:00" if value.endswith("Z") else value
    try:
        datetime.datetime.fromisoformat(text)
        return True
    except ValueError:
        return False


def validate(payload, canonical_joints, *, filename=None, folder_is_shipped=False):
    """Return (errors, warnings). Errors are things that would break the device; warnings are things
    a human should look at.

    Loud by design: the caller prints every error and exits non-zero. A pose that silently never
    locks is the worst demo failure available, and it looks exactly like a bad sensor.
    """
    errs, warns = [], []

    if not isinstance(payload, dict):
        return ["payload is not a JSON object"], []

    where = filename or payload.get("id") or "?"

    # MIGRATE FIRST, THEN CHECK. A version-1 file is readable by design, so it must be judged in its
    # migrated form - checking the v2 required fields against a v1 file would reject every legacy file
    # for missing fields that migration is precisely there to supply. (The self-test caught this
    # ordering the first time it ran, which is why --self-test exists.)
    if payload.get("version") == MIN_READABLE_VERSION:
        warns.append(f"{where}: schema 1 - readable, migrated on load, but it carries no "
                     f"signer/vantage/confidence. Run --migrate to upgrade the file on disk")
        payload = migrate_v1(payload)

    for key in REQUIRED:
        if key not in payload:
            errs.append(f"{where}: missing required field '{key}'")
    if errs:
        return errs, warns          # everything below assumes the field set exists

    for key in LEGACY_KEYS:
        if key in payload:
            errs.append(f"{where}: legacy schema-1 field '{key}' is present in a version "
                        f"{payload.get('version')} file - run --migrate instead of writing both")

    if payload["version"] not in (SCHEMA_VERSION, MIN_READABLE_VERSION):
        errs.append(f"{where}: schema version {payload['version']}, this build reads "
                    f"{MIN_READABLE_VERSION}..{SCHEMA_VERSION}")

    if payload["handedness"] not in HANDEDNESS:
        errs.append(f"{where}: handedness '{payload['handedness']}' not in {HANDEDNESS}")

    if not isinstance(payload["signer"], str) or not payload["signer"].strip():
        errs.append(f"{where}: signer is empty - a rotation is relative to SOMEBODY's neutral, "
                    f"and an empty signer means nobody can tell whose")

    if not _is_iso8601(payload["captured"]):
        errs.append(f"{where}: captured '{payload['captured']}' is not ISO-8601")

    if payload["vantage"] not in VANTAGES:
        errs.append(f"{where}: vantage '{payload['vantage']}' not in {VANTAGES}")

    if payload["source"] not in SOURCES:
        errs.append(f"{where}: source '{payload['source']}' not in {SOURCES}")

    conf = payload["confidence"]
    if not isinstance(conf, dict):
        errs.append(f"{where}: confidence must be an object {{authoring, measured, note}}")
    else:
        authoring = conf.get("authoring")
        if authoring not in AUTHORING_CONFIDENCE:
            errs.append(f"{where}: confidence.authoring '{authoring}' not in {AUTHORING_CONFIDENCE}")
        measured = conf.get("measured")
        if measured not in MEASURED_CONFIDENCE:
            errs.append(f"{where}: confidence.measured '{measured}' not in {MEASURED_CONFIDENCE} - "
                        f"it is filled from a P1.5 session, never estimated")
        if not isinstance(conf.get("note", ""), str):
            errs.append(f"{where}: confidence.note must be a string")

    n = payload["jointCount"]
    joints, rot = payload["joints"], payload["rot"]

    if not isinstance(joints, list) or not isinstance(rot, list):
        return errs + [f"{where}: joints and rot must be arrays"], warns

    if n != JOINT_COUNT:
        errs.append(f"{where}: jointCount {n}, the matcher expects {JOINT_COUNT}")
    if len(joints) != n:
        errs.append(f"{where}: {len(joints)} joint names for jointCount {n}")
    if len(rot) != n * 4:
        errs.append(f"{where}: rot has {len(rot)} floats, expected {n * 4} (x,y,z,w per joint)")

    if len(set(joints)) != len(joints):
        dupes = sorted({j for j in joints if joints.count(j) > 1})
        errs.append(f"{where}: duplicate joint names {dupes}")

    unknown = [j for j in joints if j not in canonical_joints]
    if unknown:
        errs.append(f"{where}: unknown joint names {unknown} - see POSE-FORMAT.md section 3")

    missing = [j for j in canonical_joints if j not in joints]
    if missing:
        errs.append(f"{where}: missing canonical joints {missing}")

    if not unknown and not missing and list(joints) != list(canonical_joints):
        errs.append(f"{where}: joint names are in the wrong ORDER. The loader maps by name so this "
                    f"would still load, but a reordered file means some tool sorts joints, and that "
                    f"tool will eventually be trusted to build a matcher input")

    negatives = 0
    for i in range(min(n, len(rot) // 4)):
        x, y, z, w = rot[i * 4:i * 4 + 4]
        if any(isinstance(v, bool) or not isinstance(v, (int, float)) for v in (x, y, z, w)):
            errs.append(f"{where}: joint {i} has a non-numeric component")
            continue
        if any(math.isnan(v) or math.isinf(v) for v in (x, y, z, w)):
            errs.append(f"{where}: joint {i} contains NaN/inf")
            continue
        magnitude = x * x + y * y + z * z + w * w
        if abs(magnitude - 1.0) > NORM_TOLERANCE:
            errs.append(f"{where}: joint {i} ({joints[i]}) |q|^2 = {magnitude:.4f}, not normalised")
        if w < 0:
            negatives += 1

    if negatives:
        warns.append(f"{where}: {negatives} joint(s) have w < 0. Same rotation, different sign of the "
                     f"double cover - the matcher is sign-agnostic, but the canonical form is w >= 0 "
                     f"so that two files describing one pose compare equal")

    if filename:
        stem = filename.rsplit("/", 1)[-1].rsplit(".", 1)[0]
        if payload["id"] != stem:
            errs.append(f"{where}: id '{payload['id']}' does not match the file name '{stem}'")

    if folder_is_shipped and payload.get("probeOnly"):
        errs.append(f"{where}: probeOnly is true but this file sits in the SHIPPED set. Probe "
                    f"references are not content - see P1.5's decision in phases/p1-derisk/probe-raw.md")

    return errs, warns


# --------------------------------------------------------------------------------------------------
# the neutral capture file — the other half of the "capture-neutral contract"
# --------------------------------------------------------------------------------------------------

NEUTRAL_REQUIRED = ("version", "handedness", "signer", "captured", "frameCount",
                    "heldSeconds", "joints", "rot", "spreadDeg", "droppedFrames")


def validate_neutral(payload, canonical_joints, *, filename=None):
    """A neutral capture is a pose file with a quality record. It is validated by the same joint
    rules, because a neutral with a non-normalised joint poisons every match that uses it."""
    errs = []
    if not isinstance(payload, dict):
        return ["neutral: payload is not a JSON object"]

    for key in NEUTRAL_REQUIRED:
        if key not in payload:
            errs.append(f"neutral: missing required field '{key}'")
    if errs:
        return errs

    if payload["version"] != SCHEMA_VERSION:
        errs.append(f"neutral: schema version {payload['version']}, expected {SCHEMA_VERSION}")

    pose_like = dict(payload)
    pose_like.setdefault("id", "neutral")
    pose_like.setdefault("vantage", "first-person-palm-down")
    pose_like.setdefault("source", "signer-verified")
    pose_like.setdefault("confidence", {"authoring": "medium", "measured": None, "note": ""})
    pose_like.setdefault("jointCount", len(payload.get("joints", [])))
    sub, _ = validate(pose_like, canonical_joints, filename=filename)
    errs += [e for e in sub if "confidence" not in e]

    if not isinstance(payload["frameCount"], int) or payload["frameCount"] < 60:
        errs.append(f"neutral: frameCount {payload['frameCount']} < 60 - the procedure averages the "
                    f"last 60 frames of the hold")
    if not isinstance(payload["spreadDeg"], (int, float)) or payload["spreadDeg"] < 0:
        errs.append(f"neutral: spreadDeg must be a non-negative number")
    return errs


# --------------------------------------------------------------------------------------------------
# the capture-neutral contract, in one sentence, as code
# --------------------------------------------------------------------------------------------------

def relative_to_neutral(live_local, neutral_local):
    """`live` expressed in the signer's neutral frame:  q_rel = inverse(q_neutral) * q_live.

    This is the whole "capture-neutral" idea (P2.1). A pose file's rotations are already in this
    form: they are relative to the neutral capture named by its `signer` field. The runtime's job is
    to put the user's LIVE hand in the same form before comparing, so that a hand with a different
    rest angle is compared as a *shape*, not as a different wrist.
    """
    result = []
    for live, neutral in zip(live_local, neutral_local):
        result.append(quat_mul(quat_conjugate(neutral), live))
    return result


def quat_conjugate(q):
    return (-q[0], -q[1], -q[2], q[3])


def quat_mul(a, b):
    ax, ay, az, aw = a
    bx, by, bz, bw = b
    return (
        aw * bx + ax * bw + ay * bz - az * by,
        aw * by - ax * bz + ay * bw + az * bx,
        aw * bz + ax * by - ay * bx + az * bw,
        aw * bw - ax * bx - ay * by - az * bz,
    )


def mirror_right_to_left(rot):
    """Mirror a right-hand pose to a left-hand one across the sagittal plane.

    Derivation, not folklore: a reflection in the plane x = 0 is M = diag(-1, 1, 1), and a rotation R
    becomes M R M, which is still a rotation. Conjugating a unit quaternion by that reflection gives
    (x, y, z, w) -> (x, -y, -z, w), and positions (x, y, z) -> (-x, y, z).

    THE ASSUMPTION, stated because it is testable and could be wrong: this holds when the left hand's
    joint frames are themselves mirrored versions of the right hand's, which is how a skeleton is
    usually defined. If OpenXR's left-hand joints keep the right-hand axis convention instead, the
    mirrored file will be subtly wrong in a way only the device can show. POSE-FORMAT.md section 4.3
    carries the verification test and the fallback (author left poses directly).
    """
    return [(x, -y, -z, w) for (x, y, z, w) in rot]
