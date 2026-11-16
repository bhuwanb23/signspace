#!/usr/bin/env python3
"""Zero error strings in the build (P2.3, step 5).

The rule is a product rule, not a style rule: there is no failure state, so there is no string that
can tell a learner they got it wrong (ADR-003, UX-STATES.md). A rule that lives only in a document
decays, so this script makes it executable.

What it scans: every string literal reachable by a *learner* - the UI scripts, the payoff, and the
copy tables. Developer tooling (debug overlays, probe controllers, trial loggers, Debug.Log*) is
exempt, because "wrongShapeFrames" in a CSV column is a measurement, not a verdict, and renaming it
would make the data harder to read for no gain.

Usage:
  python3 scripts/lint_copy.py            # exit 1 on a violation
  python3 scripts/lint_copy.py --list     # print every string it considered
"""
import argparse
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
UNITY = ROOT / "unity" / "Assets" / "_Project" / "Scripts"

#: Files a learner can see a string from. Adding a new user-facing script means adding it here.
USER_FACING = [
    "UI/NearMissDisplay.cs",
    "UI/HonestMirror.cs",
    "UI/BandFeedback.cs",
    "UI/BandAudio.cs",
    "Core/AppState.cs",
    "Core/LockPayoff.cs",
    "Hands/ReferenceHand.cs",
]

#: Banned in user-facing copy. Word-boundary matched, case-insensitive.
BANNED = [
    "wrong", "incorrect", "error", "invalid", "failed", "failure",
    "try again", "sorry", "oops", "nope", "bad", "denied", "rejected", "missed",
    "not quite", "too far", "keep trying",
]
#: Symbols that read as a verdict whatever the words are.
BANNED_SYMBOLS = ["\u2717", "\u2718", "\u274c", "\u00d7", "\u2715"]

#: Scripts that DO contain verdict-shaped words ("LOST", "no matcher") but are not user-facing
#: because they are gated to development builds. Checked rather than trusted: a file listed here must
#: actually contain the gate, or it is a leak in a release build.
GATED_DEV = [
    "UI/DebugOverlay.cs",
    "UI/HandConfidenceReadout.cs",
]

#: A string containing any of these is developer text that happens to be in a user-facing file.
DEV_MARKERS = ["Debug.", "[Tooltip", "[Header", "nameof", "Assert", "LogWarning", "LogError"]

STRING_RE = re.compile(r'"((?:[^"\\]|\\.)*)"')


def user_facing_strings(path):
    """Every literal in `path` that is not developer text, with its line number."""
    found = []
    for lineno, line in enumerate(path.read_text().splitlines(), start=1):
        stripped = line.strip()
        if stripped.startswith("///") or stripped.startswith("//") or stripped.startswith("*"):
            continue
        if any(marker in line for marker in DEV_MARKERS):
            continue
        # property names, paths and format fragments are not copy
        for match in STRING_RE.finditer(line):
            literal = match.group(1)
            if len(literal.strip()) < 4:
                continue
            if literal.startswith("Assets/") or literal.startswith("_"):
                continue
            found.append((lineno, literal))
    return found


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--list", action="store_true", help="print every string considered")
    args = ap.parse_args()

    violations = []
    total = 0
    for rel in USER_FACING:
        path = UNITY / rel
        if not path.exists():
            print(f"MISSING  {rel}  (listed as user-facing but not found)")
            violations.append((rel, 0, "<file missing>", "file listed in USER_FACING does not exist"))
            continue
        strings = user_facing_strings(path)
        total += len(strings)
        if args.list:
            print(f"\n{rel}  ({len(strings)} strings)")
        for lineno, literal in strings:
            if args.list:
                print(f"  {lineno:4d}  {literal}")
            low = literal.lower()
            for word in BANNED:
                if re.search(r"\b" + re.escape(word) + r"\b", low):
                    violations.append((rel, lineno, literal, f"banned word: {word}"))
            for symbol in BANNED_SYMBOLS:
                if symbol in literal:
                    violations.append((rel, lineno, literal, f"banned symbol: {symbol!r}"))

    # dev views must actually be gated, not merely believed to be
    for rel in GATED_DEV:
        path = UNITY / rel
        if not path.exists():
            violations.append((rel, 0, "<file missing>", "gated dev file does not exist"))
            continue
        text = path.read_text()
        if "DevOnly.Allowed" not in text:
            violations.append((rel, 0, "<no gate>",
                               "contains verdict-shaped text but has no DevOnly guard"))

    print("=" * 78)
    print(f"copy lint: {total} user-facing strings in {len(USER_FACING)} files, "
          f"{len(GATED_DEV)} dev views checked for a release-build gate")
    if violations:
        for rel, lineno, literal, why in violations:
            print(f"  VIOLATION {rel}:{lineno}  {why}\n            {literal!r}")
        print(f"  {len(violations)} violation(s) - the build has an error string.")
        return 1
    print("  0 violations - no user-facing string reads as an error or a verdict.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
