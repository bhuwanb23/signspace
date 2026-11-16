#!/usr/bin/env python3
"""
Prove that the two alias tables agree.

THE PROBLEM THIS EXISTS TO SOLVE
-------------------------------
There are two copies of the device-joint-name to canonical-joint-name mapping:

  scripts/hand_model.py                            NAME_ALIASES        (Python, authoring side)
  unity/Assets/_Project/Scripts/Matching/JointNames.cs   Add(...)       (C#, runtime side)

They MUST agree. If they drift, the device reports `XRHand_IndexProximal`, the
Python side says "IndexProximal", the C# side says something else, and the
symptom is not an error message - it is a joint that silently fails to map, so
a handshape that never matches.

Two real bugs of exactly this kind have already happened:
  * the C# table did not strip the `xrhand_` prefix, so v207's `XRHand_*` names
    mapped ZERO of 26 joints and looked like total tracking failure;
  * the table mapped `Hand_Index1` to `IndexMetacarpal`, but Meta's own skeleton
    table says the legacy `Index1` is the PROXIMAL phalange, so every finger
    would have been rotated one joint off its real axis.

validate_poses.py checks the pose FILES. It does not check that these two
tables agree, so it cannot catch either of those. This script does.

Usage:
    python3 scripts/check_alias_tables.py
Exit code 0 if the tables agree, 1 if they do not.
"""
from __future__ import annotations

import pathlib
import re
import sys

REPO_ROOT = pathlib.Path(__file__).resolve().parent.parent

PY_PATH = REPO_ROOT / "scripts" / "hand_model.py"
CS_PATH = REPO_ROOT / "unity" / "Assets" / "_Project" / "Scripts" / "Matching" / "JointNames.cs"


def python_aliases() -> dict[str, str]:
    """The NAME_ALIASES dict from hand_model.py."""
    text = PY_PATH.read_text(encoding="utf-8")
    if "NAME_ALIASES = {" not in text:
        raise SystemExit("hand_model.py: NAME_ALIASES not found")
    block = text.split("NAME_ALIASES = {", 1)[1].split("\n}", 1)[0]
    out: dict[str, str] = {}
    for match in re.finditer(r'"([a-z0-9]+)"\s*:\s*"(\w+)"', block):
        out[match.group(1)] = match.group(2)
    return out


def csharp_aliases() -> dict[str, str]:
    """The alias table from JointNames.cs.

    The C# builds the table with calls like Add("IndexProximal", "Index1").
    Each call contributes the canonical name itself plus every extra spelling,
    and the canonical name maps to itself in both tables.
    """
    text = CS_PATH.read_text(encoding="utf-8")
    out: dict[str, str] = {}
    pattern = r'Add\(\s*"(\w+)"((?:\s*,\s*"[\w\d]+")*)\s*\)'
    for match in re.finditer(pattern, text):
        canonical = match.group(1)
        out[canonical.lower()] = canonical
        for extra in re.findall(r'"([\w\d]+)"', match.group(2)):
            out[extra.lower()] = canonical
    return out


def main() -> int:
    py = python_aliases()
    cs = csharp_aliases()

    # the C# table may hold spellings the Python one does not and vice versa; the
    # comparison is on the SPELLINGS, since the canonical side is identical by
    # construction.
    py_spellings = {k: v for k, v in py.items()}
    cs_spellings = {k: v for k, v in cs.items()}

    only_py = {k: v for k, v in py_spellings.items() if k not in cs_spellings}
    only_cs = {k: v for k, v in cs_spellings.items() if k not in py_spellings}
    conflicts = {k: (v, cs_spellings[k]) for k, v in py_spellings.items()
                 if k in cs_spellings and cs_spellings[k] != v}

    print(f"python aliases : {len(py_spellings)}")
    print(f"C# aliases     : {len(cs_spellings)}")

    ok = True
    if conflicts:
        ok = False
        print("\nCONFLICTS — the same spelling means different canonical joints:")
        for spelling, (a, b) in sorted(conflicts.items()):
            print(f"  {spelling:<22} python={a:<18} C#={b}")
    if only_py:
        ok = False
        print("\nONLY IN PYTHON:")
        for spelling, canonical in sorted(only_py.items()):
            print(f"  {spelling:<22} -> {canonical}")
    if only_cs:
        ok = False
        print("\nONLY IN C#:")
        for spelling, canonical in sorted(only_cs.items()):
            print(f"  {spelling:<22} -> {canonical}")

    print()
    if ok:
        print(f"RESULT: the tables AGREE. {len(py_spellings)} spellings, same canonical joint on both sides.")
        return 0

    print("RESULT: the tables DISAGREE. The device mapping would silently differ "
          "between the authoring side and the runtime side.")
    print("        Fix both, then re-run. See hand_model.py NAME_ALIASES and "
          "JointNames.cs BuildAliases().")
    return 1


if __name__ == "__main__":
    sys.exit(main())
