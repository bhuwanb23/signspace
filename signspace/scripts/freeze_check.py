#!/usr/bin/env python3
"""G10, as a command: is the submitted entry still intact, installable and unchanged?

P10 is the only phase whose work is *not doing things*. Its gate asks a question about the passage of
time, so the mechanical part of the answer has to be a check that can be re-run every week and diffs
against the freeze record written on submission night:

    phases/p9-submission/apk-hash.txt   — sha256, version, commit, frozen-at

It compares that record against the repository as it is *now*: the APK bytes, the version constants, the
tag, whether anything landed on `main` after the freeze, whether the archive is actually complete, and
whether the submission record has the three things a judge needs (submission id, invite URL, video).

It cannot check the channel listing, whether the video is public, or whether the invite URL works from a
clean account. Those need an account and a browser, so they are reported as **NOT RUN** — never inferred
from a file that happens to exist.

Usage
  python scripts/freeze_check.py                 # the table, and a non-zero exit on any FAIL
  python scripts/freeze_check.py --json          # the same rows as JSON
  python scripts/freeze_check.py --tag v1.0-competition

Exit codes: 0 all PASS (NOT RUN allowed), 1 any FAIL, 2 no freeze record.
"""

import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
from datetime import datetime, timezone

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.normpath(os.path.join(HERE, ".."))
RECORD = os.path.join(PROJECT, "phases", "p9-submission", "apk-hash.txt")
SUBMITTED = os.path.join(PROJECT, "phases", "p9-submission", "submitted.md")
ARCHIVE = os.path.join(PROJECT, "phases", "p9-submission", "archive")
BUILD_SCRIPT = os.path.join(PROJECT, "unity", "Assets", "_Project", "Editor", "BuildScript.cs")
VERSION_FILE = os.path.join(PROJECT, "unity", "Assets", "_Project", "version-code.txt")
RAW_VIDEO = os.path.join(PROJECT, "assets", "raw", "video")

PASS, FAIL, NOTRUN = "PASS", "FAIL", "NOT RUN"


def git(*args):
    try:
        return subprocess.run(["git", "-C", PROJECT] + list(args),
                              capture_output=True, text=True, timeout=30).stdout.strip()
    except (OSError, subprocess.SubprocessError):
        return ""


def git_ok(*args):
    """True when git exits 0 (merge-base --is-ancestor reports by status, not stdout)."""
    try:
        return subprocess.run(["git", "-C", PROJECT] + list(args),
                              capture_output=True, text=True, timeout=30).returncode == 0
    except (OSError, subprocess.SubprocessError):
        return False


def sha256(path):
    digest = hashlib.sha256()
    with open(path, "rb") as handle:
        for block in iter(lambda: handle.read(1 << 20), b""):
            digest.update(block)
    return digest.hexdigest()


def apk_version_code(apk):
    """Read versionCode from the packaged manifest with aapt2, if the SDK ships one."""
    import glob
    patterns = [
        os.environ.get("AAPT2", ""),
        "/e/unity/Editors/*/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/build-tools/*/aapt2.exe",
        os.path.expanduser("~/AppData/Local/Android/Sdk/build-tools/*/aapt2.exe"),
    ]
    for pattern in patterns:
        if not pattern:
            continue
        for candidate in glob.glob(pattern):
            if not os.path.isfile(candidate):
                continue
            try:
                out = subprocess.run([candidate, "dump", "badging", apk],
                                     capture_output=True, text=True, timeout=60).stdout
            except (OSError, subprocess.SubprocessError):
                continue
            found = re.search(r"versionCode='(\d+)'", out)
            if found:
                return found.group(1)
    return ""


def parse_record(path):
    fields = {}
    with open(path, encoding="utf-8") as handle:
        for line in handle:
            if ":" not in line:
                continue
            key, _, value = line.partition(":")
            fields[key.strip().lower()] = value.strip()
    version_line = fields.get("version", "")
    match = re.search(r"bundle version code (\d+)", version_line)
    return {
        "apk": fields.get("apk", ""),
        "package": fields.get("package", ""),
        "version_name": version_line.split("(")[0].strip(),
        "version_code": match.group(1) if match else "",
        "sha256": fields.get("sha256", ""),
        "frozen_at": fields.get("frozen at", ""),
        "commit": fields.get("commit", ""),
    }


def row(rows, name, verdict, expected, actual):
    rows.append({"check": name, "verdict": verdict, "expected": expected, "actual": actual})


def main():
    parser = argparse.ArgumentParser(description="Verify the freeze (G10)")
    parser.add_argument("--json", action="store_true")
    parser.add_argument("--tag", default="v1.0-competition")
    args = parser.parse_args()

    rows = []
    if not os.path.isfile(RECORD):
        print("no freeze record at %s — nothing has been frozen" % RECORD)
        return 2

    record = parse_record(RECORD)

    # 1. the artifact itself
    # The record stores the path as written from unity/ ("Builds/Android/..."); accept either base.
    apk = ""
    if record["apk"]:
        for base in (PROJECT, os.path.join(PROJECT, "unity")):
            candidate = os.path.join(base, record["apk"])
            if os.path.isfile(candidate):
                apk = candidate
                break
        else:
            apk = os.path.join(PROJECT, "unity", record["apk"])
    if apk and os.path.isfile(apk):
        row(rows, "apk present", PASS, record["apk"], "%d bytes, mtime %s" % (
            os.path.getsize(apk),
            datetime.fromtimestamp(os.path.getmtime(apk)).strftime("%Y-%m-%d %H:%M UTC")))
        actual_hash = sha256(apk)
        row(rows, "apk sha256", PASS if actual_hash == record["sha256"] else FAIL,
            record["sha256"][:16] + "…", actual_hash[:16] + "…")

        manifest_code = apk_version_code(apk)
        if manifest_code:
            row(rows, "bundle version code (APK manifest)",
                PASS if manifest_code == record["version_code"] else FAIL,
                record["version_code"], manifest_code)
        else:
            row(rows, "bundle version code (APK manifest)", NOTRUN, record["version_code"],
                "aapt2 not found; cannot read the packaged manifest")
    else:
        row(rows, "apk present", FAIL, record["apk"] or "a path in the record", "missing")

    # 2. version constants, so a rebuild with a new code is visible even if the hash somehow matched
    version_name = ""
    if os.path.isfile(BUILD_SCRIPT):
        text = open(BUILD_SCRIPT, encoding="utf-8").read()
        found = re.search(r'Version\s*=\s*"([^"]+)"', text)
        version_name = found.group(1) if found else ""
    version_code = ""
    if os.path.isfile(VERSION_FILE):
        version_code = open(VERSION_FILE, encoding="utf-8").readline().strip()
    row(rows, "version name", PASS if version_name == record["version_name"] else FAIL,
        record["version_name"], version_name or "not found")
    row(rows, "bundle version code (record)",
        PASS if version_code == record["version_code"] else FAIL,
        record["version_code"], version_code or "not found")

    # The record and version-code.txt can agree with each other and still be wrong: the build path
    # ignores that file and uses the const in BuildScript.cs. Check the const too, so a stale
    # version-code.txt cannot certify itself.
    build_code = ""
    if os.path.isfile(BUILD_SCRIPT):
        found_code = re.search(r"const int BundleVersionCode\s*=\s*(\d+)",
                               open(BUILD_SCRIPT, encoding="utf-8").read())
        build_code = found_code.group(1) if found_code else ""
    row(rows, "bundle version code (build path)",
        PASS if build_code == record["version_code"] else FAIL,
        record["version_code"], build_code or "not found")

    # 3. the freeze, in git
    tag_commit = git("rev-list", "-n", "1", args.tag)
    row(rows, "tag %s exists" % args.tag, PASS if tag_commit else FAIL,
        "a tag at the submitted commit", tag_commit[:12] if tag_commit else "no such tag")

    # The record must name the commit that produced the APK, not HEAD. A freeze is a point in
    # history; later commits are a separate failure (reported below). What makes the record
    # trustworthy is that the named commit still exists, still sits on main, and carries the tag.
    head = git("rev-parse", "HEAD")
    record_commit = record["commit"].split()[0] if record["commit"] else ""
    resolved = git("rev-parse", "--verify", record_commit + "^{commit}") if record_commit else ""

    if record_commit and resolved:
        on_main = bool(head) and git_ok("merge-base", "--is-ancestor", resolved, "HEAD")
        row(rows, "record commit is on main", PASS if on_main else FAIL,
            "an ancestor of HEAD",
            resolved[:12] + ("" if on_main else " (not reachable from HEAD)"))
        if tag_commit:
            row(rows, "tag is the record commit", PASS if tag_commit == resolved else FAIL,
                resolved[:12], tag_commit[:12])
    else:
        row(rows, "record commit is on main", FAIL,
            "a commit hash in apk-hash.txt",
            "the record says 'commit: ' (empty)" if not record_commit
            else "cannot resolve " + record_commit[:12])

    frozen_at = record["frozen_at"]
    if frozen_at:
        after = git("log", "--since=" + frozen_at, "--format=%h %cI %s")
        lines = [line for line in after.splitlines() if line.strip()]
        row(rows, "no commits after freeze", PASS if not lines else FAIL,
            "0 commits on main", "%d: %s" % (len(lines), lines[0][:60]) if lines else "0")
    else:
        row(rows, "no commits after freeze", FAIL, "a frozen-at timestamp", "missing from the record")

    dirty = [line for line in git("status", "--porcelain").splitlines() if line.strip()]
    row(rows, "working tree clean", PASS if not dirty else FAIL,
        "0 modified or untracked files", "%d files%s" % (len(dirty), (": " + dirty[0][:40]) if dirty else ""))

    # 4. the archive: what a judge's answer must be reconstructable from in three weeks
    required = {
        "BUILD.md": "the frozen build record",
        "shot-list.csv": "the 14 shots",
        "edit-notes.md": "the edit decisions",
        "channel-state.png": "the channel listing",
        "count-output.txt": "the copy counts",
    }
    missing = [name for name in required if not os.path.exists(os.path.join(ARCHIVE, name))]
    row(rows, "archive complete", PASS if not missing else FAIL,
        "%d files" % len(required), "missing: " + ", ".join(missing) if missing else "complete")

    # 5. the submission record — the only place a judge's path is written down
    submitted = open(SUBMITTED, encoding="utf-8").read() if os.path.isfile(SUBMITTED) else ""
    filled = {
        "submission id": bool(re.search(r"\|\s*Submission ID\s*\|\s*(?!\(|$)[^|]{4,}\|", submitted)),
        "invite URL": bool(re.search(r"https?://\S*(horizon|meta|store|oculus)\S*", submitted, re.I)),
        "confirmation email": bool(re.search(r"confirmation", submitted, re.I) and
                                 re.search(r"\b(from|received|at)\b[^\n]*\d{2}:\d{2}", submitted, re.I)),
    }
    for name, ok in filled.items():
        row(rows, "submission record: " + name, PASS if ok else FAIL, "recorded", "empty" if not ok else "present")
    row(rows, "invite URL from a clean account", NOTRUN, "opens, installs, launches",
        "needs a clean browser session and a headset")
    row(rows, "channel lists the frozen version", NOTRUN, record["version_name"],
        "needs a developer-account login")
    row(rows, "video plays publicly", NOTRUN, "the submitted video URL",
        "needs the video URL and a browser")

    # 6. the video master: the raw capture is the only irreplaceable asset
    masters = []
    for root, _dirs, files in os.walk(ARCHIVE if os.path.isdir(ARCHIVE) else PROJECT):
        for name in files:
            if name.lower().endswith((".mp4", ".mov")) and "previs" not in name.lower():
                masters.append(os.path.join(root, name))
    if os.path.isdir(RAW_VIDEO):
        for name in os.listdir(RAW_VIDEO):
            if name.lower().endswith((".mp4", ".mov")) and "previs" not in name.lower():
                masters.append(os.path.join(RAW_VIDEO, name))
    row(rows, "video master present", PASS if masters else FAIL,
        "the captured entry video", masters[0] if masters else "only a previs exists")

    if args.json:
        print(json.dumps(rows, indent=2))
    else:
        width = max(len(r["check"]) for r in rows)
        print("FREEZE CHECK - %s" % datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC"))
        print("record: %s" % os.path.relpath(RECORD, PROJECT))
        print("")
        for item in rows:
            print("  %-8s %-*s  expected %-28s  actual %s" % (
                item["verdict"], width, item["check"], item["expected"], item["actual"]))
        counts = {verdict: sum(1 for r in rows if r["verdict"] == verdict)
                  for verdict in (PASS, FAIL, NOTRUN)}
        print("")
        print("%d PASS | %d FAIL | %d NOT RUN" % (counts[PASS], counts[FAIL], counts[NOTRUN]))
        print("G10: %s" % ("NOT EVALUABLE - device and account rows are NOT RUN"
                           if counts[FAIL] == 0 else "RED - the freeze is not intact"))

    return 0 if all(r["verdict"] != FAIL for r in rows) else 1


if __name__ == "__main__":
    sys.exit(main())
