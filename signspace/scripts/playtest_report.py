#!/usr/bin/env python3
"""Turn the device's playtest CSVs into the numbers G8 asks for.

One stranger's session is written by `PlaytestProbe` as `playtest-<stamp>.csv`:

    timestamp_utc,unix_ms,session,source,event,seconds,detail

This script reads one or more of those files and prints, per session:

    first grab · first lock · first band · first near-miss · stalls · locks · the 75 s verdict

Why a script rather than reading the CSV by eye: G8's criterion is a comparison against 75 s across
several sessions, and the phase's credibility depends on that comparison being mechanical. It also
refuses to treat a fixture as evidence — rows with `source=fixture` are excluded unless asked for, and a
run with no device rows exits non-zero.

Usage
  python scripts/playtest_report.py                      # everything in phases/p8-playtest
  python scripts/playtest_report.py path/to/*.csv        # specific files
  python scripts/playtest_report.py --gate               # exit 1 unless the latest device run is <75 s
  python scripts/playtest_report.py --fixture out.csv    # write a synthetic session for testing
"""

import argparse
import csv
import glob
import os
import sys
from datetime import datetime, timedelta

TARGET = 75.0
HERE = os.path.dirname(os.path.abspath(__file__))
DEFAULT_DIR = os.path.join(HERE, "..", "phases", "p8-playtest")


def read_session(path):
    """Returns {session: {source, events: [(seconds, event, detail)]}} for one CSV file."""
    sessions = {}
    with open(path, newline="", encoding="utf-8") as handle:
        for row in csv.DictReader(handle):
            if not row or not row.get("session"):
                continue
            key = row["session"]
            entry = sessions.setdefault(key, {
                "source": row.get("source", "unset"),
                "file": os.path.basename(path),
                "events": [],
            })
            try:
                seconds = float(row.get("seconds") or 0.0)
            except ValueError:
                seconds = 0.0
            entry["events"].append((seconds, row.get("event", ""), row.get("detail", "")))
    return sessions


def first(events, name):
    for seconds, event, _ in sorted(events):
        if event == name:
            return seconds
    return None


def count(events, name):
    return sum(1 for _, event, _ in events if event == name)


def summarise(session, data, include_fixtures):
    events = data["events"]
    first_lock = first(events, "first_lock")
    source = data["source"]

    verdict = "NO LOCK"
    if first_lock is not None:
        verdict = "MET" if first_lock <= TARGET else "MISSED"

    return {
        "session": session,
        "source": source,
        "file": data["file"],
        "grab": first(events, "first_grab"),
        "band": first(events, "band_close"),
        "lock": first_lock,
        "near": first(events, "first_near_miss"),
        "stalls": count(events, "stall"),
        "locks": count(events, "first_lock"),
        "verdict": verdict,
        "fixture": source == "fixture",
    }


def fmt(value):
    return "-" if value is None else "%.1fs" % value


def print_table(rows):
    if not rows:
        print("no sessions found")
        return
    header = ("session", "source", "grab", "band", "lock", "near", "stalls", "locks", "vs 75s")
    widths = [16, 7, 7, 7, 7, 7, 7, 6, 8]
    print("  ".join(h.ljust(w) for h, w in zip(header, widths)))
    print("  ".join("-" * w for w in widths))
    for row in rows:
        cells = [row["session"][:16], row["source"], fmt(row["grab"]), fmt(row["band"]),
                 fmt(row["lock"]), fmt(row["near"]), str(row["stalls"]), str(row["locks"]),
                 row["verdict"]]
        print("  ".join(c.ljust(w) for c, w in zip(cells, widths)))


def write_fixture(path):
    """A synthetic session, clearly marked. It exists so the report and the checks can be tested
    without a stranger; it is never mixed into a real run unless --include-fixtures is passed."""
    start = datetime.utcnow().replace(microsecond=0)
    rows = [
        (0.0, "session_start", "fixture"),
        (3.2, "hand_seen", ""),
        (11.4, "first_grab", ""),
        (14.1, "band_close", ""),
        (31.8, "first_lock", "C"),
        (47.5, "stall", "still 10 s"),
        (63.0, "first_near_miss", "O 4.2°"),
        (70.0, "first_switch", "Captions"),
        (88.0, "session_end", "fixture summary"),
    ]
    with open(path, "w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(["timestamp_utc", "unix_ms", "session", "source", "event", "seconds", "detail"])
        for seconds, event, detail in rows:
            at = start + timedelta(seconds=seconds)
            writer.writerow([at.strftime("%Y-%m-%dT%H:%M:%S.000Z"),
                             int(at.timestamp() * 1000),
                             "fixture-20260101-000000", "fixture", event,
                             "%.3f" % seconds, detail])
    print("wrote fixture %s (9 events, source=fixture)" % path)


def main():
    parser = argparse.ArgumentParser(description="Summarise SIGNSPACE playtest sessions")
    parser.add_argument("files", nargs="*", help="CSV files (default: phases/p8-playtest/playtest-*.csv)")
    parser.add_argument("--dir", default=DEFAULT_DIR)
    parser.add_argument("--include-fixtures", action="store_true")
    parser.add_argument("--gate", action="store_true",
                        help="exit 1 unless the latest device session locked inside %g s" % TARGET)
    parser.add_argument("--fixture", metavar="PATH", help="write a synthetic session and exit")
    args = parser.parse_args()

    if args.fixture:
        write_fixture(args.fixture)
        return 0

    files = args.files or sorted(glob.glob(os.path.join(args.dir, "playtest-*.csv")))
    if not files:
        print("no playtest CSVs in %s" % os.path.abspath(args.dir))
        print("A session exists only after someone runs the app: see phases/p8-playtest/session-kit.md")
        return 2

    rows = []
    for path in files:
        for session, data in read_session(path).items():
            row = summarise(session, data, args.include_fixtures)
            if row["fixture"] and not args.include_fixtures:
                continue
            rows.append(row)

    rows.sort(key=lambda r: r["session"])
    print_table(rows)

    device = [r for r in rows if r["source"] == "device"]
    print("")
    print("%d session(s), %d device, %d fixture (excluded: %s)"
          % (len(rows), len(device), sum(1 for r in rows if r["fixture"]),
             "no" if args.include_fixtures else "yes"))

    if not device:
        print("No DEVICE session yet. G8 cannot be evaluated from fixtures or the editor.")
        return 2 if args.gate else 0

    latest = device[-1]
    print("latest device run %s: first lock %s (%s)"
          % (latest["session"], fmt(latest["lock"]), latest["verdict"]))

    if args.gate:
        if latest["verdict"] != "MET":
            print("G8 NOT MET: first lock %s vs %gs target" % (fmt(latest["lock"]), TARGET))
            return 1
        print("G8 first-lock criterion MET for %s" % latest["session"])
    return 0


if __name__ == "__main__":
    sys.exit(main())
