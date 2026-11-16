#!/usr/bin/env python3
"""The gate that stops an invented number reaching the panel (P3.6, G3).

`trackability.json` is the only source of the honest mirror's numbers, so this script is the thing that
decides whether what is in it is publishable. It is a *floor*, in the same spirit as `lint_copy.py`:

  - the file must have exactly twenty variants, unique ids, ids that match the probe folder;
  - every `expected` and every `status` must be one of the agreed words;
  - **if `measured` is false, every status must be `unmeasured` and every lock rate `-1`** - a half-filled
    file that claims nothing is honest, one that claims a bit is not;
  - **if `measured` is true, every status must be a measurement**, every lock rate must be in [0, 1],
    and `measuredUtc` must be a real ISO date with a device and a conditions string beside it;
  - a `borderline` shape must actually have repeated holds behind it (`rates` with more than one entry)
    and must sit inside 15 points of the floor - otherwise the range is decoration;
  - the count line the panel will print must appear **verbatim** in `TRACKABILITY.md`. If the doc and
    the panel disagree, this fails; the fix is the file, never the doc.

Exit 1 on any violation. Prints the sentence the panel will show either way, so a human can read it.

Usage:  python3 scripts/check_trackability.py [--data PATH] [--doc PATH]
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
DATA = ROOT / "unity" / "Assets" / "StreamingAssets" / "trackability.json"
PROBE = ROOT / "unity" / "Assets" / "StreamingAssets" / "Probe"
DOC = ROOT / "docs" / "03-content" / "TRACKABILITY.md"

EXPECTED_VALUES = {"fine", "may-resolve", "not-resolvable", "out-of-scope"}
STATUS_VALUES = {"unmeasured", "tracks-and-locks", "not-resolvable",
                 "flickers", "tracks-wrong-shape", "tracks-never-locks"}
FLOOR = 0.70
BORDERLINE_BAND = 0.15


def count_line(data):
    """Mirror of Trackability.CountLine. Kept identical by hand - it is five lines, and a shared
    implementation would be a build step for a sentence."""
    total = len(data["variants"])
    if not data.get("measured"):
        excluded = sum(1 for v in data["variants"] if v["expected"] == "not-resolvable")
        words = ["No", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten"]
        figure = words[excluded] if 0 <= excluded < len(words) else excluded
        return (f"{figure} handshape{'s' if excluded != 1 else ''} stay out of this build because the "
                f"cameras cannot see {'it' if excluded == 1 else 'them'}. Here is why.")
    robust = sum(1 for v in data["variants"] if v["status"] == "tracks-and-locks" and not v.get("borderline"))
    in_doubt = sum(1 for v in data["variants"] if v.get("borderline"))
    figure = f"{robust} to {robust + in_doubt}" if in_doubt else str(robust)
    return (f"{figure} of {total} handshape variants are robustly trackable today — measured "
            f"{data['measuredUtc']} on {data['device']}, {data['conditions']}.")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--data", default=str(DATA))
    ap.add_argument("--doc", default=str(DOC))
    args = ap.parse_args()

    data = json.loads(pathlib.Path(args.data).read_text())
    doc = pathlib.Path(args.doc).read_text()
    variants = data.get("variants", [])
    problems = []

    def fail(where, why):
        problems.append((where, why))

    # ---- shape of the file
    if len(variants) != 20:
        fail("variants", f"{len(variants)} rows; the denominator the panel prints is 20")
    ids = [v.get("id") for v in variants]
    if len(set(ids)) != len(ids):
        fail("variants", "duplicate ids")
    for shape in ids:
        if not (PROBE / f"{shape}.json").exists():
            fail(shape, "no pose file in StreamingAssets/Probe - the panel would have nothing to draw")

    for v in variants:
        shape = v.get("id", "?")
        if v.get("expected") not in EXPECTED_VALUES:
            fail(shape, f"expected={v.get('expected')!r} is not one of {sorted(EXPECTED_VALUES)}")
        if v.get("status") not in STATUS_VALUES:
            fail(shape, f"status={v.get('status')!r} is not one of {sorted(STATUS_VALUES)}")

    # ---- the two states, and never a third
    if data.get("measured"):
        for v in variants:
            shape = v.get("id", "?")
            if v.get("status") == "unmeasured":
                fail(shape, "measured is true but this row has no status")
            rate = v.get("lockRate", -1)
            if not (0.0 <= rate <= 1.0):
                fail(shape, f"lockRate={rate} is not a fraction")
            if v.get("borderline"):
                if len(v.get("rates", {})) < 2:
                    fail(shape, "borderline with no repeated holds behind it")
                if abs(rate - FLOOR) > BORDERLINE_BAND:
                    fail(shape, f"borderline at {rate:.2f}, more than {BORDERLINE_BAND} from the floor")
        try:
            dt.date.fromisoformat(data.get("measuredUtc", ""))
        except ValueError:
            fail("measuredUtc", f"{data.get('measuredUtc')!r} is not an ISO date - a number without a date is a guess")
        if not data.get("device"):
            fail("device", "measured with no device named")
        if not data.get("conditions"):
            fail("conditions", "measured with no conditions named")
    else:
        for v in variants:
            shape = v.get("id", "?")
            if v.get("status") != "unmeasured":
                fail(shape, "measured is false but this row carries a status - half-filled files lie")
            if v.get("lockRate", -1) != -1:
                fail(shape, "measured is false but this row carries a lock rate")

    # ---- panel and document must say the same thing
    line = count_line(data)
    if line not in doc:
        fail("TRACKABILITY.md", "the panel's count line does not appear verbatim in the document")

    print("=" * 78)
    print(f"trackability: {len(variants)} variants · measured={data.get('measured')} · "
          f"{sum(1 for v in data.get('variants', []) if v.get('borderline'))} borderline")
    print(f"the panel will print:\n  {line}")
    if problems:
        for where, why in problems:
            print(f"  VIOLATION {where}: {why}")
        print(f"  {len(problems)} violation(s) - the number is not publishable.")
        print("=" * 78)
        return 1
    print("  0 violations - every number in the panel traces to this file, and this file says what it knows.")
    print("=" * 78)
    return 0


if __name__ == "__main__":
    sys.exit(main())
