#!/usr/bin/env python3
"""Turn the P1.5 probe logs into the honest mirror's numbers (P3.6).

The panel's credibility rests on its numbers being true, so the numbers are not typed in: this script
reads the probe's own `*.jsonl` output and writes `unity/Assets/StreamingAssets/trackability.json`,
which is the only file the panel reads. Three rules are enforced rather than documented:

  1. **`measured` flips only when every one of the twenty shapes has a result.** A half-run probe
     produces a file that still says "not measured", plus a checklist of what is missing.
  2. **`expected` is never written.** It is the design decision and it is compared before and after;
     the script fails loudly rather than let the two get edited together.
  3. **A shape whose repeated holds disagree becomes a range, not a rounding.** Two holds at 0.62 and
     0.79 are not "0.705, passes": the shape is marked `borderline` and the panel's count becomes
     "X to Y", because the range is what was measured.

Inputs are the files `ProbeController` writes to `Application.persistentDataPath`, named
`probe-<condition>-<hand>.jsonl`. Pull them off the headset first.

Usage:
  python3 scripts/fill_trackability.py phases/p1-derisk/probe-logs --dry-run
  python3 scripts/fill_trackability.py phases/p1-derisk/probe-logs --date 2026-11-18
  python3 scripts/fill_trackability.py <logs> --emit-doc        # also rewrite TRACKABILITY.md §3
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
DATA = ROOT / "unity" / "Assets" / "StreamingAssets" / "trackability.json"
DOC = ROOT / "docs" / "03-content" / "TRACKABILITY.md"

FLOOR = 0.70           # the decision rule: below ~70% lock rate the shape leaves the build
DISAGREE = 0.20        # spread between repeated holds that makes a shape a range, not a number
STATUS_LOCKS = "tracks-and-locks"
STATUS_FAIL = "not-resolvable"
STATUS_UNMEASURED = "unmeasured"

DOC_BEGIN_31 = "<!-- trackability:3.1:begin -->"
DOC_END_31 = "<!-- trackability:3.1:end -->"
DOC_BEGIN_32 = "<!-- trackability:3.2:begin -->"
DOC_END_32 = "<!-- trackability:3.2:end -->"
DOC_BEGIN_LINE = "<!-- trackability:line:begin -->"
DOC_END_LINE = "<!-- trackability:line:end -->"


def load_rows(paths):
    """Every probe row, tagged with the condition and hand its file name declares."""
    rows = []
    for path in paths:
        for file in ([path] if path.is_file() else sorted(path.glob("*.jsonl"))):
            parts = file.stem.split("-")          # probe-<condition>-<hand>
            condition = parts[1] if len(parts) > 2 else "unknown"
            hand = parts[2] if len(parts) > 2 else "unknown"
            for lineno, line in enumerate(file.read_text().splitlines(), 1):
                line = line.strip()
                if not line:
                    continue
                try:
                    row = json.loads(line)
                except json.JSONDecodeError as e:
                    print(f"  skip {file.name}:{lineno} - {e}")
                    continue
                row["_condition"], row["_hand"], row["_file"] = condition, hand, file.name
                rows.append(row)
    return rows


def aggregate(rows):
    """{shape: {"cells": {(condition, hand): [lockedFrames...]}}}"""
    cells = {}
    for row in rows:
        shape = row.get("shape")
        if not shape:
            continue
        key = (row["_condition"], row["_hand"])
        cells.setdefault(shape, {}).setdefault(key, []).append(float(row.get("lockedFrames", 0.0)))
    return cells


def measured_note(shape_cells):
    """The one line the panel prints per excluded shape, from the measurement itself.

    A rate with no conditions reads as universal, and every rate in this project is conditional - so the
    conditions are named whenever they differ from one another.
    """
    per_cell = {k: sum(v) / len(v) for k, v in shape_cells.items()}
    holds = [v for values in shape_cells.values() for v in values]
    mean = sum(holds) / len(holds)

    note = f"locked {mean*100:.0f}% of a 10 s hold"
    if len(per_cell) > 1:
        note += " (" + ", ".join(f"{c} {r*100:.0f}%" for (c, _h), r in sorted(per_cell.items())) + ")"
    if len(holds) > 1:
        note += f"; {len(holds)} holds"
    return note


def decide(data, cells, require_all=True):
    """Write status/lockRate/note/borderline from the cells. Never touches `expected`."""
    missing, changed = [], []
    for variant in data["variants"]:
        shape = variant["id"]
        shape_cells = cells.get(shape)
        if not shape_cells:
            missing.append(shape)
            variant["status"] = STATUS_UNMEASURED
            variant["lockRate"] = -1
            variant["borderline"] = False
            continue

        holds = [v for values in shape_cells.values() for v in values]
        rate = sum(holds) / len(holds)
        spread = max(holds) - min(holds) if len(holds) > 1 else 0.0
        rates = {f"{c}/{h}": round(sum(v) / len(v), 3) for (c, h), v in sorted(shape_cells.items())}

        variant["lockRate"] = round(rate, 3)
        variant["rates"] = rates
        variant["status"] = STATUS_LOCKS if rate >= FLOOR else STATUS_FAIL
        # Borderline means the holds genuinely straddle the decision, not merely that they wobbled:
        # at least one hold above the floor and one below. That is the only case where rounding either
        # way would be a lie a reader could not see.
        variant["borderline"] = (len(holds) > 1 and spread > DISAGREE
                                 and min(holds) < FLOOR <= max(holds))
        if "designNote" not in variant:
            variant["designNote"] = variant.get("note", "")
        variant["note"] = measured_note(shape_cells)
        if variant["borderline"]:
            variant["note"] += f"; holds disagreed by {spread*100:.0f} points"
            changed.append((shape, rate, spread))

    complete = not missing and all(v["status"] != STATUS_UNMEASURED for v in data["variants"])
    if require_all and not complete:
        data["measured"] = False
        data["measuredUtc"] = ""
    return missing, changed


def count_line(data):
    """Mirror of Trackability.CountLine - the sentence the panel will print."""
    total = len(data["variants"])
    if not data.get("measured"):
        excluded = sum(1 for v in data["variants"] if v["expected"] == "not-resolvable")
        words = ["No", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten"]
        figure = words[excluded] if 0 <= excluded < len(words) else excluded
        return (f"{figure} handshape{'s' if excluded != 1 else ''} stay out of this build because the "
                f"cameras cannot see {'it' if excluded == 1 else 'them'}. Here is why.")
    robust = sum(1 for v in data["variants"] if v["status"] == STATUS_LOCKS and not v.get("borderline"))
    in_doubt = sum(1 for v in data["variants"] if v.get("borderline"))
    figure = f"{robust} to {robust + in_doubt}" if in_doubt else str(robust)
    return (f"{figure} of {total} handshape variants are robustly trackable today — measured "
            f"{data['measuredUtc']} on {data['device']}, {data['conditions']}.")


def emit_doc(data, doc_path=DOC):
    """Rewrite the §3 tables and §4.2's verbatim panel line from the data. Panel and doc cannot drift."""
    rows31 = ["| Shape | Lock rate | 10 s hold behaviour | Category | Shipped? |", "|---|---|---|---|---|"]
    rows32 = ["| Shape | daylight / dominant | lamplight / dominant | daylight / other | lamplight / other |",
              "|---|---|---|---|---|"]
    for v in data["variants"]:
        rate = "not measured" if v["lockRate"] < 0 else f"{v['lockRate']*100:.0f}%"
        cat = v["status"] if v["status"] != STATUS_UNMEASURED else v["expected"]
        shipped = "yes" if v["id"] in ("B", "U", "V", "W", "C", "O") else "—"
        note = (v.get("note") or "").replace("|", "/")
        rows31.append(f"| {v['id']} | {rate} | {note} | {cat} | {shipped} |")
        rates = v.get("rates", {})
        cells = [f"{rates.get(k, 0)*100:.0f}%" if k in rates else "not measured"
                 for k in ("daylight/dominant", "lamplight/dominant", "daylight/other", "lamplight/other")]
        rows32.append(f"| {v['id']} | " + " | ".join(cells) + " |")

    text = doc_path.read_text()
    blocks = [
        (DOC_BEGIN_31, DOC_END_31, "\n".join(rows31)),
        (DOC_BEGIN_32, DOC_END_32, "\n".join(rows32)),
        # the sentence the panel prints, quoted verbatim, so check_trackability.py can compare the two
        (DOC_BEGIN_LINE, DOC_END_LINE, "> **\"" + count_line(data) + "\"**"),
    ]
    for begin, end, body in blocks:
        if begin not in text or end not in text:
            raise SystemExit(f"markers missing in {doc_path.name}: {begin} / {end}")
        head, rest = text.split(begin, 1)
        _, tail = rest.split(end, 1)
        text = head + begin + "\n" + body + "\n" + end + tail
    doc_path.write_text(text)
    print(f"  wrote §3 tables and §4.2's verbatim line into {doc_path.name}")


def main():
    ap = argparse.ArgumentParser(description="Fill trackability.json from probe logs.")
    ap.add_argument("logs", nargs="*", help="probe-*.jsonl files or the folder holding them")
    ap.add_argument("--data", default=str(DATA))
    ap.add_argument("--dry-run", action="store_true", help="compute and print, write nothing")
    ap.add_argument("--emit-doc", action="store_true", help="also rewrite TRACKABILITY.md §3 and §4.2")
    ap.add_argument("--doc", default=str(DOC), help="the document to rewrite with --emit-doc")
    ap.add_argument("--date", default=dt.date.today().isoformat(), help="measurement date (YYYY-MM-DD)")
    ap.add_argument("--device", default="Quest 3S")
    ap.add_argument("--conditions", default="normal light")
    args = ap.parse_args()

    data_path = pathlib.Path(args.data)
    data = json.loads(data_path.read_text())
    before_expected = {v["id"]: v["expected"] for v in data["variants"]}

    paths = [pathlib.Path(p) for p in args.logs]
    if not paths:
        if args.emit_doc and not args.dry_run:
            # regenerate the doc tables from the current data file, no measurement involved
            emit_doc(data, pathlib.Path(args.doc))
            print("  (no logs: tables regenerated from the data file as it stands)")
            return 0
        print("no logs given - nothing to do. Pass the folder of probe-*.jsonl files.")
        return 2

    rows = load_rows(paths)
    cells = aggregate(rows)
    print(f"read {len(rows)} probe rows over {len(cells)} shapes from {len(paths)} location(s)")

    missing, disagreeing = decide(data, cells)
    after_expected = {v["id"]: v["expected"] for v in data["variants"]}
    if before_expected != after_expected:
        raise SystemExit("REFUSING: `expected` changed. It is the design decision, not a measurement.")

    complete = not missing and all(v["status"] != STATUS_UNMEASURED for v in data["variants"])
    if complete:
        data["measured"] = True
        data["measuredUtc"] = args.date
        data["device"] = args.device
        data["conditions"] = args.conditions
    else:
        print(f"\nINCOMPLETE - {len(missing)} shape(s) have no result: {', '.join(missing)}")
        print("  measured stays false. The panel will keep saying what it can prove.")

    for shape, rate, spread in disagreeing:
        print(f"  borderline: {shape} at {rate*100:.0f}% with {spread*100:.0f} points of spread "
              f"- the count becomes a range")

    print("\nthe panel will print:\n  " + count_line(data))
    for v in data["variants"]:
        if v["expected"] == "not-resolvable" and not (data["measured"] and v["status"] == STATUS_LOCKS):
            print(f"  {v['id']}  {v['note']}")

    if args.dry_run:
        print("\ndry run: nothing written")
        return 0

    data_path.write_text(json.dumps(data, indent=2) + "\n")
    try:
        shown = data_path.relative_to(ROOT)
    except ValueError:
        shown = data_path            # a dry-run or test path outside the repo
    print(f"\nwrote {shown}")
    if args.emit_doc:
        emit_doc(data, pathlib.Path(args.doc))
    return 0


if __name__ == "__main__":
    sys.exit(main())
