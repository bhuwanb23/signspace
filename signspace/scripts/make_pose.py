#!/usr/bin/env python3
"""
Emit the shipped handshape pose files.

The six shapes (B, U, V, W, C, O) are authored here from published ASL
fingerspelling references, in a canonical joint frame defined by hand_model.py.
Nothing in this file needs a signer, a headset, or the Unity editor.

That is the point: the shapes exist BEFORE the signer call, so the signer
reviews a finished artifact in a single call instead of a two-call dance where
we author poses from their recording and then ask them to approve what we made.

Usage:
    python3 scripts/make_pose.py                     # write the six files
    python3 scripts/make_pose.py --dry-run           # print, do not write
    python3 scripts/make_pose.py --neutral rest.json # fold in a real rig rest pose
    python3 scripts/make_pose.py --out DIR           # write somewhere else

--neutral expects a JSON object of {jointName: [x, y, z, w]} holding the hand
rig's rest-pose local rotations. The authoring tool assumes the rest pose has
identity local rotations; if the device disagrees, dump the rig's rest pose and
regenerate with this flag. Nothing else changes.
"""
from __future__ import annotations

import argparse
import datetime
import json
import pathlib
import sys

import hand_model as hm
import pose_schema as ps

REPO_ROOT = pathlib.Path(__file__).resolve().parent.parent
DEFAULT_OUT = REPO_ROOT / "unity" / "Assets" / "StreamingAssets" / "Signs"

AUTHORED_ON = "2026-11-15T00:00:00Z"

#: Whose neutral the rotations are relative to. For authored references that is the canonical
#: synthetic neutral in hand_model.py: every joint at identity. It is a real value, not a placeholder -
#: it says "these rotations are already shape, not wrist", which is what makes them comparable to a
#: user's own neutral-relative capture.
SIGNER = "synthetic-neutral-v1"

VANTAGE = "authored-neutral"

SOURCE = "reference-authored"

NOTES = (
    "Authored from published ASL fingerspelling references by scripts/make_pose.py. "
    "Not yet reviewed by a Deaf signer -- see set-meta.json for the current authorship state."
)


def build_payload(shape_id, neutral=None):
    shape = hm.SHAPES[shape_id]
    flat = hm.serialize(shape_id, neutral)
    return ps.build_payload(
        shape_id=shape_id,
        joints=list(hm.JOINT_NAMES),
        rot=flat,
        signer=SIGNER,
        captured=AUTHORED_ON,
        vantage=VANTAGE,
        source=SOURCE,
        confidence={
            "authoring": "high" if shape_id in ("B", "U", "V", "W", "C") else "medium",
            # empty until a P1.5 session measures it. Never estimated: a guessed number here would
            # let an untrackable shape look certified.
            "measured": None,
            "note": "reference-authored; no signer review recorded in set-meta.json yet",
        },
        family=shape["family"],
        description=shape["description"],
        notes=NOTES,
    )


def validate(payload, path_name):
    """The same contract scripts/validate_poses.py enforces, run at write time.

    It delegates to pose_schema so the writer cannot drift from the validator: P2.1's whole point is
    that there is one definition of this format. (Before P2.1 this function and validate_poses.py
    each carried their own copy of the rules, which is how a format quietly develops two meanings.)
    """
    errors, _warnings = ps.validate(payload, list(hm.JOINT_NAMES), filename=path_name)
    return errors


def load_neutral(path):
    raw = json.loads(pathlib.Path(path).read_text())
    out = {}
    for name in hm.JOINT_NAMES:
        if name in raw:
            v = raw[name]
            out[name] = (float(v[0]), float(v[1]), float(v[2]), float(v[3]))
    missing = [n for n in hm.JOINT_NAMES if n not in out]
    if missing:
        print(f"warning: neutral pose is missing {len(missing)} joints: {', '.join(missing)}")
    return out or None


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=str(DEFAULT_OUT))
    ap.add_argument("--neutral", default=None,
                    help="JSON file of rig rest-pose local rotations")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--shapes", default=None,
                    help="comma separated subset, default all")
    args = ap.parse_args()

    neutral = load_neutral(args.neutral) if args.neutral else None
    out_dir = pathlib.Path(args.out)
    shape_ids = args.shapes.split(",") if args.shapes else list(hm.SHAPES.keys())

    errors = []
    written = []
    for shape_id in shape_ids:
        if shape_id not in hm.SHAPES:
            print(f"unknown shape '{shape_id}'", file=sys.stderr)
            return 2
        payload = build_payload(shape_id, neutral)
        name = f"{shape_id}.json"
        errors += validate(payload, name)
        body = json.dumps(payload, indent=2)
        if args.dry_run:
            print(f"--- {name} ---")
            print(body)
        else:
            out_dir.mkdir(parents=True, exist_ok=True)
            (out_dir / name).write_text(body + "\n")
            written.append(name)
            print(f"wrote {out_dir / name}  ({len(payload['rot'])} floats, "
                  f"{payload['jointCount']} joints, {hm.SHAPES[shape_id]['family']})")

    # index.json -- a zip archive cannot be listed at runtime, so the loader is
    # told what is in the folder. SignSetLoader reads this first.
    index = {"version": 1, "shapes": shape_ids}
    index_body = json.dumps(index, indent=2)
    if args.dry_run:
        print("--- index.json ---")
        print(index_body)
    else:
        (out_dir / "index.json").write_text(index_body + "\n")
        print(f"wrote {out_dir / 'index.json'}  ({len(shape_ids)} shapes listed)")

    if errors:
        print("\nVALIDATION FAILED:", file=sys.stderr)
        for e in errors:
            print("  " + e, file=sys.stderr)
        return 1

    print(f"\n{len(shape_ids)} shape files written, 0 validation problems.")
    print(f"joint set: {len(hm.JOINT_NAMES)} joints, canonical (OpenXR XR_EXT_hand_tracking order)")
    print("near-miss pairs:")
    for (a, b), feature in hm.NEAR_MISS_PAIRS.items():
        print(f"  {a}-{b}  {feature}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
