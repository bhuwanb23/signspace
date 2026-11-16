#!/usr/bin/env python3
"""
Emit the twenty probe poses for P1.5 — and nothing that could ship by accident.

WHAT THESE ARE, AND WHAT THEY ARE NOT
-------------------------------------
The six shipped shapes (B, U, V, W, C, O) are content: they are meant to be taught, and
they carry the signer-review question in ADR-007.

The fourteen kept here are **references for a measurement**. P1.5 holds each of twenty
shapes and asks what the sensor does with it. To answer "does it track, or does it track
as a DIFFERENT shape", the matcher needs a pose for that other shape to compare against.
So these exist to make discrimination measurable — they are not taught, not shipped, and
they live in StreamingAssets/Probe/, which `SignSetLoader` only reads when a build is
explicitly pointed at it.

That distinction matters for honesty. A badly drawn reference pose would show up in the
probe as "the sensor confused R with V", when the truth would be "our R is wrong". Each
candidate therefore carries an `authoringConfidence` field: high for a shape whose
articulation is unambiguous on paper, low where the shape depends on contact or depth
that a rotation-only format cannot express (M, N, T, R, S — the suspected blind spots
in TRACKABILITY.md). A `low` result on a `low`-confidence reference is a finding about
the POSE, not necessarily about the sensor, and probe-raw.md is written to keep those
apart.

Usage:
    python3 scripts/make_probe_poses.py                 # write StreamingAssets/Probe
    python3 scripts/make_probe_poses.py --dry-run
    python3 scripts/make_probe_poses.py --neutral rest.json
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys

import hand_model as hm
import pose_schema as ps
from hand_model import _finger, _thumb

REPO_ROOT = pathlib.Path(__file__).resolve().parent.parent
DEFAULT_OUT = REPO_ROOT / "unity" / "Assets" / "StreamingAssets" / "Probe"

AUTHORED_ON = "2026-11-15T00:00:00Z"

#: The twenty candidates. The six shipped shapes are copied in so the probe folder is
#: self-contained: one folder, one index, one matcher set, no cross-folder reads.
PROBE_SHAPES = {
    # ---- the six that ship --------------------------------------------------------
    # Regenerated from hand_model.SHAPES below rather than duplicated here, so the probe
    # measures the same numbers the product ships.

    # ---- candidates, authored for P1.5 --------------------------------------------
    "A": {
        "family": "closure",
        "authoringConfidence": "high",
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
        "authoringConfidence": "medium",
        "description": "Index extended; middle, ring and little curved to meet the thumb "
                       "tip. Two contacts the sensor cannot resolve.",
        "fingers": {
            "index":  _finger(flex_base=4.0, abd=+2.0),
            "middle": _finger(flex_base=44.0, flex_prox=58.0, flex_inter=40.0, flex_distal=14.0),
            "ring":   _finger(flex_base=46.0, flex_prox=60.0, flex_inter=42.0, flex_distal=14.0, abd=-3.0),
            "little": _finger(flex_base=48.0, flex_prox=62.0, flex_inter=44.0, flex_distal=16.0, abd=-6.0),
        },
        "thumb": _thumb(flex_base=42.0, flex_prox=48.0, flex_distal=20.0, abd=-6.0, opp=64.0),
    },
    "E": {
        "family": "closure",
        "authoringConfidence": "low",
        "description": "Fingers curled so the tips rest on the thumb. Every tip is at the "
                       "same place, which is the hardest thing a joint-only sensor has to see.",
        "fingers": {
            "index":  _finger(flex_base=78.0, flex_prox=62.0, flex_inter=44.0, flex_distal=18.0, abd=+2.0),
            "middle": _finger(flex_base=80.0, flex_prox=64.0, flex_inter=46.0, flex_distal=18.0),
            "ring":   _finger(flex_base=80.0, flex_prox=64.0, flex_inter=46.0, flex_distal=18.0, abd=-2.0),
            "little": _finger(flex_base=78.0, flex_prox=62.0, flex_inter=44.0, flex_distal=18.0, abd=-4.0),
        },
        "thumb": _thumb(flex_base=34.0, flex_prox=44.0, flex_distal=22.0, abd=-4.0, opp=58.0),
    },
    "F": {
        "family": "closure",
        "authoringConfidence": "low",
        "description": "Index tip meets the thumb tip in a ring; middle, ring and little stay "
                       "extended. The index–thumb contact makes the index look shorter.",
        "fingers": {
            "index":  _finger(flex_base=34.0, flex_prox=46.0, flex_inter=36.0, flex_distal=20.0, abd=+2.0),
            "middle": _finger(flex_base=2.0, abd=0.0),
            "ring":   _finger(flex_base=2.0, abd=-3.0),
            "little": _finger(flex_base=2.0, abd=-6.0),
        },
        "thumb": _thumb(flex_base=36.0, flex_prox=46.0, flex_distal=26.0, abd=-8.0, opp=74.0),
    },
    "G": {
        "family": "spread",
        "authoringConfidence": "low",
        "description": "Index points sideways with the thumb parallel to it. Orientation-dependent: "
                       "the same joint angles read as different letters depending on wrist rotation.",
        "fingers": {
            "index":  _finger(flex_base=4.0, abd=-6.0),
            "middle": _finger(flex_base=86.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=-4.0),
            "ring":   _finger(flex_base=86.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=-6.0),
            "little": _finger(flex_base=86.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=-8.0),
        },
        "thumb": _thumb(flex_base=8.0, flex_prox=10.0, flex_distal=6.0, abd=+26.0, opp=6.0),
    },
    "I": {
        "family": "spread",
        "authoringConfidence": "high",
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
        "authoringConfidence": "high",
        "description": "Index up, thumb out at right angles to it; the other three folded.",
        "fingers": {
            "index":  _finger(flex_base=2.0, abd=+4.0),
            "middle": _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0),
            "ring":   _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=-2.0),
            "little": _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=-4.0),
        },
        "thumb": _thumb(flex_base=4.0, flex_prox=6.0, flex_distal=4.0, abd=+44.0, opp=4.0),
    },
    "M": {
        "family": "closure",
        "authoringConfidence": "low",
        "description": "Three fingers folded over the thumb. Depth-ambiguous: the tips sit ON "
                       "the thumb, which a joint-only sensor reports as 'folded', same as A or S.",
        "fingers": {
            "index":  _finger(flex_base=82.0, flex_prox=92.0, flex_inter=64.0, flex_distal=12.0, abd=+4.0),
            "middle": _finger(flex_base=84.0, flex_prox=94.0, flex_inter=66.0, flex_distal=12.0),
            "ring":   _finger(flex_base=84.0, flex_prox=94.0, flex_inter=66.0, flex_distal=12.0, abd=-2.0),
            "little": _finger(flex_base=86.0, flex_prox=98.0, flex_inter=68.0, flex_distal=8.0, abd=-4.0),
        },
        "thumb": _thumb(flex_base=44.0, flex_prox=40.0, flex_distal=18.0, abd=-4.0, opp=46.0),
    },
    "N": {
        "family": "closure",
        "authoringConfidence": "low",
        "description": "Two fingers over the thumb with the ring and little loosely curled. "
                       "Differs from M by one stacked finger - a CONTACT difference, which a "
                       "joint-only sensor may be unable to see at all. The probe measures whether "
                       "it can; if the two land inside the lock threshold, that is the finding.",
        "fingers": {
            "index":  _finger(flex_base=82.0, flex_prox=92.0, flex_inter=64.0, flex_distal=12.0, abd=+3.0),
            "middle": _finger(flex_base=84.0, flex_prox=94.0, flex_inter=66.0, flex_distal=12.0),
            "ring":   _finger(flex_base=46.0, flex_prox=54.0, flex_inter=30.0, flex_distal=10.0, abd=-2.0),
            "little": _finger(flex_base=42.0, flex_prox=50.0, flex_inter=28.0, flex_distal=10.0, abd=-4.0),
        },
        "thumb": _thumb(flex_base=44.0, flex_prox=40.0, flex_distal=18.0, abd=-4.0, opp=46.0),
    },
    "R": {
        "family": "spread",
        "authoringConfidence": "low",
        "description": "Index and middle extended and CROSSED. Crossing is a rotation about the "
                       "finger's own axis; a joint-only format cannot express it, so this reference "
                       "is an approximation and the probe treats a low score here with suspicion.",
        "fingers": {
            "index":  _finger(flex_base=2.0, abd=-12.0),
            "middle": _finger(flex_base=2.0, abd=+14.0),
            "ring":   _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=-2.0),
            "little": _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=-4.0),
        },
        "thumb": _thumb(flex_base=38.0, flex_prox=44.0, flex_distal=22.0, abd=-12.0, opp=72.0),
    },
    "S": {
        "family": "closure",
        "authoringConfidence": "low",
        "description": "Fist with the thumb across the front of the fingers. The classic "
                       "thumbs-alongside / thumbs-across ambiguity against A.",
        "fingers": {
            "index":  _finger(flex_base=87.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=+1.0),
            "middle": _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0),
            "ring":   _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=-2.0),
            "little": _finger(flex_base=87.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=-4.0),
        },
        "thumb": _thumb(flex_base=40.0, flex_prox=46.0, flex_distal=24.0, abd=-14.0, opp=84.0),
    },
    "T": {
        "family": "closure",
        "authoringConfidence": "low",
        "description": "Thumb tucked between index and middle. Inter-finger depth: the thumb "
                       "is hidden, and hiding is exactly what a joint-only sensor cannot report.",
        "fingers": {
            "index":  _finger(flex_base=84.0, flex_prox=96.0, flex_inter=68.0, flex_distal=10.0, abd=+5.0),
            "middle": _finger(flex_base=84.0, flex_prox=96.0, flex_inter=68.0, flex_distal=10.0, abd=-3.0),
            "ring":   _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=-2.0),
            "little": _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=-4.0),
        },
        "thumb": _thumb(flex_base=34.0, flex_prox=30.0, flex_distal=14.0, abd=+2.0, opp=38.0),
    },
    "X": {
        "family": "closure",
        "authoringConfidence": "medium",
        "description": "Index hooked at the proximal joint; the others folded. A bent finger is "
                       "a clean rotation, so this one should survive if the tracker is honest.",
        "fingers": {
            "index":  _finger(flex_base=62.0, flex_prox=42.0, flex_inter=10.0, abd=+2.0),
            "middle": _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0),
            "ring":   _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=-2.0),
            "little": _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=-4.0),
        },
        "thumb": _thumb(flex_base=30.0, flex_prox=36.0, flex_distal=18.0, abd=-10.0, opp=62.0),
    },
    "Y": {
        "family": "spread",
        "authoringConfidence": "high",
        "description": "Thumb and little extended, the middle three folded. Two ends of the "
                       "hand at once, which is a good test of whether the tracker keeps the "
                       "little finger's abduction.",
        "fingers": {
            "index":  _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=+1.0),
            "middle": _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0),
            "ring":   _finger(flex_base=88.0, flex_prox=100.0, flex_inter=70.0, flex_distal=8.0, abd=-2.0),
            "little": _finger(flex_base=2.0, abd=-16.0),
        },
        "thumb": _thumb(flex_base=4.0, flex_prox=6.0, flex_distal=4.0, abd=+42.0, opp=6.0),
    },
    # ---- the facing pair (P2.6 content gap / G3) -----------------------------------
    # K and P are the SAME finger posture with the palm turned 90 degrees: this is the only
    # authored pair where the difference is orientation and nothing else, so it is the pair
    # that proves the palm term works on real content rather than on a synthetic rotation.
    # P carries "wrist": pronation, which no previous shape used - every one of them left
    # Wrist and Palm at identity, which is why the gap existed.
    "K": {
        "family": "spread",
        "authoringConfidence": "medium",
        "description": "Index and middle extended and together, thumb resting between them, "
                       "ring and little folded. Palm forward.",
        "fingers": {
            "index":  _finger(flex_base=4.0, abd=+2.0),
            "middle": _finger(flex_base=4.0, abd=-2.0),
            "ring":   _finger(flex_base=82.0, flex_prox=96.0, flex_inter=66.0, flex_distal=8.0, abd=-4.0),
            "little": _finger(flex_base=84.0, flex_prox=98.0, flex_inter=68.0, flex_distal=8.0, abd=-8.0),
        },
        "thumb": _thumb(flex_base=16.0, flex_prox=22.0, flex_distal=10.0, abd=+6.0, opp=34.0),
    },
    "P": {
        "family": "spread",
        "authoringConfidence": "medium",
        "description": "K with the palm turned down. Identical fingers; the only difference is "
                       "90 degrees of pronation, which is exactly what the palm term measures.",
        "fingers": {
            "index":  _finger(flex_base=4.0, abd=+2.0),
            "middle": _finger(flex_base=4.0, abd=-2.0),
            "ring":   _finger(flex_base=82.0, flex_prox=96.0, flex_inter=66.0, flex_distal=8.0, abd=-4.0),
            "little": _finger(flex_base=84.0, flex_prox=98.0, flex_inter=68.0, flex_distal=8.0, abd=-8.0),
        },
        "thumb": _thumb(flex_base=16.0, flex_prox=22.0, flex_distal=10.0, abd=+6.0, opp=34.0),
        "wrist": {"twist": 90.0},
    },
}

#: Probe order. Shipped six first (they are the ones that must survive), then the suspected
#: blind spots, then the rest. Order matters: if the session is cut short, the most
#: consequential shapes have already been measured.
PROBE_ORDER = ["B", "U", "V", "W", "C", "O",
               "A", "M", "N", "T", "R", "S",
               "D", "E", "F", "G", "I", "L", "X", "Y"]

#: The facing pair (P2.6's content gap). Deliberately NOT in PROBE_ORDER: they are not
#: trackability candidates, and adding them to the probe would lengthen P1.5's session from
#: 20 shapes to 22 for no measurement reason. They get their own folder.
FACING_IDS = ["K", "P"]


def shape_for(shape_id):
    """A probe shape dict for any of the twenty, shipped or candidate."""
    if shape_id in PROBE_SHAPES:
        return PROBE_SHAPES[shape_id]
    return hm.SHAPES[shape_id]


def payload_for(shape_id):
    """A schema-2 probe pose, built through the one schema implementation (P2.1).

    `probeOnly: true` is the flag that keeps these out of shipped content; validate_poses.py rejects
    any file carrying it inside the shipped Signs folder.
    """
    shape = shape_for(shape_id)
    rotations = hm.shape_local_rotations(shape)
    flat = []
    for name in hm.JOINT_NAMES:
        x, y, z, w = rotations[name]
        flat.extend([round(x, 4), round(y, 4), round(z, 4), round(w, 4)])
    return ps.build_payload(
        shape_id=shape_id,
        joints=list(hm.JOINT_NAMES),
        rot=flat,
        signer="synthetic-neutral-v1",
        captured=AUTHORED_ON,
        vantage="authored-neutral",
        source="probe-reference",
        confidence={
            "authoring": shape.get("authoringConfidence", "high"),
            # P1.5 measures this on hands; until a session runs, it stays empty on purpose
            "measured": None,
            "note": "probe reference, not content and not signer-reviewed",
        },
        family=shape.get("family", "spread"),
        description=shape.get("description", ""),
        notes="P1.5 probe reference. NOT SHIPPED CONTENT.",
        probe_only=True,
    )


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--out", default=str(DEFAULT_OUT))
    args = ap.parse_args()

    out = pathlib.Path(args.out)
    problems = []
    written = []

    if not args.dry_run:
        out.mkdir(parents=True, exist_ok=True)

    for shape_id in PROBE_ORDER:
        payload = payload_for(shape_id)
        errs, _warns = ps.validate(payload, list(hm.JOINT_NAMES), filename=f"{shape_id}.json")
        problems += errs

        path = out / f"{shape_id}.json"
        if args.dry_run:
            print(f"[dry-run] {path.name}: {payload['family']:8s} "
                  f"{payload['authoringConfidence']:6s} {payload['description'][:60]}")
        else:
            path.write_text(json.dumps(payload, indent=2) + "\n")
            written.append(shape_id)

    if not args.dry_run:
        index = {"version": 1, "shapes": PROBE_ORDER}
        (out / "index.json").write_text(json.dumps(index, indent=2) + "\n")

        # ---- the facing pair, in its own folder ---------------------------------------
        facing_dir = out.parent / "Facing"
        facing_dir.mkdir(parents=True, exist_ok=True)
        for shape_id in FACING_IDS:
            payload = payload_for(shape_id)
            errs, _ = ps.validate(payload, list(hm.JOINT_NAMES),
                                  filename=f"Facing/{shape_id}.json")
            problems += errs
            (facing_dir / f"{shape_id}.json").write_text(json.dumps(payload, indent=2) + "\n")
        (facing_dir / "index.json").write_text(
            json.dumps({"version": 1, "shapes": FACING_IDS}, indent=2) + "\n")

        meta = {
            "version": 1,
            "purpose": "P1.5 probe references. NOT SHIPPED CONTENT. Not signer-reviewed.",
            "authoredOn": AUTHORED_ON,
            "order": PROBE_ORDER,
            "candidates": {
                sid: {
                    "family": shape_for(sid).get("family", "spread"),
                    "authoringConfidence": shape_for(sid).get("authoringConfidence", "high"),
                    "description": shape_for(sid).get("description", ""),
                }
                for sid in PROBE_ORDER
            },
        }
        (out / "probe-meta.json").write_text(json.dumps(meta, indent=2) + "\n")

    print()
    print(f"{len(written) if not args.dry_run else len(PROBE_ORDER)} probe poses, "
          f"{len(problems)} problems")
    for p in problems:
        print("  PROBLEM:", p)
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
