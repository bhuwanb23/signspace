#!/usr/bin/env python3
"""Validate every handshape JSON before it ships. Fails loudly.

A malformed pose is a shape that silently never locks, which is the worst demo failure available:
on the headset it looks exactly like a bad sensor.

P2.1 changed three things about this script:

  1. The rules now live in `scripts/pose_schema.py`, not here. This file is the runner.
  2. `--self-test` proves the validator *catches* broken files instead of asking a human to trust it.
     It mutates a known-good payload through every rule P2.1 froze and asserts each mutation is
     rejected, then runs the deliberately broken files in `scripts/fixtures/broken/`.
  3. `--migrate` upgrades schema-1 files to schema 2 in place, which is the migration path the
     format's `version` field exists to make possible.

Usage:
    python3 scripts/validate_poses.py                     # both pose folders
    python3 scripts/validate_poses.py --self-test         # prove the rules bite
    python3 scripts/validate_poses.py --migrate           # upgrade v1 files in place
    python3 scripts/validate_poses.py path/to/folder      # one folder
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys

REPO_ROOT = pathlib.Path(__file__).resolve().parent.parent
sys.path.insert(0, str(REPO_ROOT / "scripts"))

import hand_model as hm          # noqa: E402
import pose_schema as ps         # noqa: E402

SKIP = {"set-meta.json", "index.json", "probe-meta.json"}

DEFAULT_FOLDERS = (
    REPO_ROOT / "unity" / "Assets" / "StreamingAssets" / "Signs",
    # Authored-but-not-shipped candidates (P5.4): validated by the same rules as the shipped six,
    # so promoting one in P5.6 cannot promote an unvalidated pose.
    REPO_ROOT / "unity" / "Assets" / "StreamingAssets" / "Signs" / "deferred",
    REPO_ROOT / "unity" / "Assets" / "StreamingAssets" / "Probe",
    REPO_ROOT / "unity" / "Assets" / "StreamingAssets" / "Facing",
)

BROKEN_DIR = REPO_ROOT / "scripts" / "fixtures" / "broken"


# --------------------------------------------------------------------------------------------------
# folder validation
# --------------------------------------------------------------------------------------------------

def check_index(folder: pathlib.Path, present: list[str]) -> list[str]:
    errs = []
    index_path = folder / "index.json"
    if not index_path.exists():
        return [f"{folder.name}/index.json is missing - SignSetLoader cannot list the folder on Android"]

    try:
        index = json.loads(index_path.read_text())
    except Exception as e:
        return [f"{folder.name}/index.json: unparseable JSON ({e})"]

    listed = index.get("shapes") or []
    if not listed:
        return [f"{folder.name}/index.json: lists no shapes"]

    for s in listed:
        if s not in present:
            errs.append(f"{folder.name}/index.json lists '{s}' but {s}.json is not present")
    for s in present:
        if s not in listed:
            errs.append(f"{folder.name}/{s}.json is present but index.json does not list it")
    if len(set(listed)) != len(listed):
        errs.append(f"{folder.name}/index.json: duplicate entries")
    return errs


def validate_folder(folder: pathlib.Path, canonical, *, verbose=True) -> tuple[int, list[str]]:
    if not folder.exists():
        return 0, [f"no pose folder at {folder}"]

    # `Facing/` is the K/P probe pair: reference-only, exactly like Probe, and never shipped.
    shipped = folder.name.lower() not in ("probe", "facing")
    files = [p for p in sorted(folder.glob("*.json")) if p.name not in SKIP]
    if not files:
        return 0, []

    errors, present = [], []
    for f in files:
        if f.name.startswith("EXAMPLE"):
            if verbose:
                print(f"[SKIP] {folder.name}/{f.name} (example, not shipped)")
            continue
        try:
            payload = json.loads(f.read_text())
        except Exception as e:
            errors.append(f"{folder.name}/{f.name}: unparseable JSON ({e})")
            present.append(f.stem)
            continue

        errs, warns = ps.validate(payload, canonical,
                                  filename=f"{folder.name}/{f.name}",
                                  folder_is_shipped=shipped)
        present.append(f.stem)
        if verbose:
            print(f"[{' FAIL' if errs else '  OK '}] {folder.name}/{f.name}")
        errors += errs
        if verbose:
            for w in warns:
                print(f"   warning: {w}")
    index_errs = check_index(folder, present)
    errors += index_errs
    if verbose:
        print(f"[{' FAIL' if index_errs else '  OK '}] {folder.name}/index.json")
    return len(present), errors


# --------------------------------------------------------------------------------------------------
# migration
# --------------------------------------------------------------------------------------------------

def migrate_folder(folder: pathlib.Path, canonical) -> int:
    changed = 0
    for f in sorted(folder.glob("*.json")):
        if f.name in SKIP:
            continue
        try:
            payload = json.loads(f.read_text())
        except Exception as e:
            print(f"[MIGRATE FAIL] {f.name}: unparseable ({e})")
            continue
        if payload.get("version") == ps.SCHEMA_VERSION:
            continue
        upgraded = ps.migrate_v1(payload)
        errs, _ = ps.validate(upgraded, canonical, filename=f.name,
                              folder_is_shipped=folder.name.lower() != "probe")
        if errs:
            print(f"[MIGRATE FAIL] {f.name}: still invalid after migration")
            for e in errs:
                print("   " + e)
            continue
        f.write_text(json.dumps(upgraded, indent=2) + "\n")
        changed += 1
        print(f"[MIGRATED] {f.name}: schema {payload.get('version')} -> {ps.SCHEMA_VERSION}")
    print(f"\n{changed} file(s) migrated in {folder.name}")
    return changed


# --------------------------------------------------------------------------------------------------
# self-test — the part that answers "how do you know the validator works?"
# --------------------------------------------------------------------------------------------------

def _valid_payload():
    shape = hm.SHAPES["B"]
    rotations = hm.shape_local_rotations(shape)
    flat = []
    for name in hm.JOINT_NAMES:
        flat.extend(rotations[name])
    return ps.build_payload(
        shape_id="B",
        joints=list(hm.JOINT_NAMES),
        rot=flat,
        signer="synthetic-neutral-v1",
        captured="2026-11-15T00:00:00Z",
        vantage="authored-neutral",
        source="reference-authored",
        confidence={"authoring": "high", "measured": None, "note": "self-test copy"},
        family=shape.get("family"),
        description=shape.get("description"),
    )


def _mutations():
    """(name, mutate, substring the error must contain). One per rule P2.1 froze."""
    def drop(field):
        return lambda d: d.pop(field, None)

    def setter(field, value):
        def apply(d):
            d[field] = value
        return apply

    def swap_joints(d):
        d["joints"][0], d["joints"][1] = d["joints"][1], d["joints"][0]

    def bad_quat(d):
        d["rot"][4] = 0.5          # joint 1, x component: |q|^2 will be off

    def nan_quat(d):
        d["rot"][0] = float("nan")

    return [
        ("missing signer",            drop("signer"),                  "missing required field 'signer'"),
        ("missing vantage",           drop("vantage"),                 "missing required field 'vantage'"),
        ("missing confidence",        drop("confidence"),              "missing required field 'confidence'"),
        ("missing captured",          drop("captured"),                "missing required field 'captured'"),
        ("bad handedness",            setter("handedness", "ambidextrous"), "not in"),
        ("empty signer",              setter("signer", "   "),         "signer is empty"),
        ("bad vantage",               setter("vantage", "selfie"),     "vantage 'selfie'"),
        ("bad source",                setter("source", "vibes"),       "source 'vibes'"),
        ("bad captured",              setter("captured", "last tuesday"), "not ISO-8601"),
        ("bad authoring confidence",  setter("confidence", {"authoring": "certain", "measured": None}), "confidence.authoring"),
        ("estimated measured value",  setter("confidence", {"authoring": "high", "measured": "Pretty good"}), "confidence.measured"),
        ("jointCount 25",             setter("jointCount", 25),        "the matcher expects 26"),
        ("rot too short",             lambda d: d.update(rot=d["rot"][:-4]), "expected 104"),
        ("duplicate joint",           lambda d: d["joints"].__setitem__(4, d["joints"][3]), "duplicate joint names"),
        ("unknown joint",             lambda d: d["joints"].__setitem__(3, "ThumbWonder"), "unknown joint names"),
        ("wrong joint order",         swap_joints,                     "wrong ORDER"),
        ("unnormalised quaternion",   bad_quat,                        "not normalised"),
        ("NaN component",             nan_quat,                        "NaN"),
        ("legacy key in v2",          lambda d: d.update(q=d["rot"]),  "legacy schema-1 field 'q'"),
        ("unknown version",           setter("version", 9),            "this build reads"),
        ("id != filename",            setter("id", "Z"),               "does not match the file name"),
        ("probeOnly in shipped set",  setter("probeOnly", True),       "SHIPPED set"),
    ]


def self_test(verbose=True) -> int:
    canonical = list(hm.JOINT_NAMES)
    failures = []

    # 0. the baseline must pass, or every "caught it" below is meaningless
    good = _valid_payload()
    errs, warns = ps.validate(good, canonical, filename="B.json", folder_is_shipped=True)
    ok = not errs
    if verbose:
        print(f"[{'  OK ' if ok else ' FAIL'}] baseline: an unmutated payload validates")
    if not ok:
        failures.append("baseline")
        for e in errs:
            print("   " + e)

    # 1. every frozen rule bites
    for name, mutate, expected in _mutations():
        payload = json.loads(json.dumps(good))     # deep copy
        mutate(payload)
        errs, _ = ps.validate(payload, canonical, filename="B.json", folder_is_shipped=True)
        caught = any(expected in e for e in errs)
        if verbose:
            print(f"[{'  OK ' if caught else ' FAIL'}] rule: {name}"
                  + ("" if caught else f"  (expected an error containing {expected!r}, got {errs})"))
        if not caught:
            failures.append(name)

    # 2. the sign-of-double-cover warning fires without failing the file
    neg = json.loads(json.dumps(good))
    neg["rot"][0], neg["rot"][1], neg["rot"][2], neg["rot"][3] = (
        0.0, 0.0, 0.0, -1.0)   # identity with w < 0
    errs, warns = ps.validate(neg, canonical, filename="B.json", folder_is_shipped=True)
    ok = not errs and any("w < 0" in w for w in warns)
    if verbose:
        print(f"[{'  OK ' if ok else ' FAIL'}] w < 0 warns but does not reject")
    if not ok:
        failures.append("negative-w")

    # 3. schema 1 is readable, migrated, and reported
    v1 = {k: v for k, v in good.items()
          if k not in ("joints", "rot", "captured", "signer", "vantage", "confidence")}
    v1["version"] = 1
    v1["jointNames"] = list(good["joints"])
    v1["q"] = list(good["rot"])
    v1["neutralCaptured"] = good["captured"]
    errs, warns = ps.validate(v1, canonical, filename="B.json", folder_is_shipped=True)
    ok = not errs and any("schema 1" in w for w in warns)
    if verbose:
        print(f"[{'  OK ' if ok else ' FAIL'}] schema 1 is readable and reported as needing migration")
    if not ok:
        failures.append("v1-readable")

    # 4. a v1 file migrates into something that validates cleanly
    errs, _ = ps.validate(ps.migrate_v1(v1), canonical, filename="B.json", folder_is_shipped=True)
    ok = not errs
    if verbose:
        print(f"[{'  OK ' if ok else ' FAIL'}] migrate_v1 output validates")
    if not ok:
        failures.append("migrate")
        for e in errs:
            print("   " + e)

    # 5. the committed broken fixtures are rejected (the task's "deliberately broken file")
    fixtures = sorted(BROKEN_DIR.glob("*.json")) if BROKEN_DIR.exists() else []
    if not fixtures:
        if verbose:
            print("[ FAIL] no committed fixtures in scripts/fixtures/broken/")
        failures.append("fixtures-missing")
    for f in fixtures:
        payload = json.loads(f.read_text())
        errs, _ = ps.validate(payload, canonical, filename=f"Signs/{f.name}", folder_is_shipped=True)
        ok = bool(errs)
        if verbose:
            print(f"[{'  OK ' if ok else ' FAIL'}] fixture rejected: {f.name}"
                  + ("" if ok else "  (was accepted — the fixture no longer tests anything)"))
        if not ok:
            failures.append(f"fixture:{f.name}")

    print()
    if failures:
        print(f"SELF-TEST FAILED: {len(failures)} case(s): {', '.join(failures)}")
        return 1
    print(f"SELF-TEST PASSED: {len(_mutations()) + 4 + len(fixtures)} cases")
    return 0


# --------------------------------------------------------------------------------------------------

def main(argv=None):
    ap = argparse.ArgumentParser(description="Validate, self-test or migrate handshape pose files.")
    ap.add_argument("folders", nargs="*", help="pose folders (default: Signs and Probe)")
    ap.add_argument("--self-test", action="store_true", help="prove every frozen rule is enforced")
    ap.add_argument("--migrate", action="store_true", help="upgrade schema-1 files in place")
    ap.add_argument("--quiet", action="store_true")
    args = ap.parse_args(argv)

    canonical = list(hm.JOINT_NAMES)

    if args.self_test:
        return self_test(verbose=not args.quiet)

    folders = [pathlib.Path(f) for f in args.folders] if args.folders else list(DEFAULT_FOLDERS)

    if args.migrate:
        total = 0
        for folder in folders:
            if folder.exists():
                total += migrate_folder(folder, canonical)
        print(f"\n{total} file(s) migrated overall. Re-run without --migrate to validate.")
        return 0

    total_shapes, all_errs = 0, []
    for folder in folders:
        shapes, errs = validate_folder(folder, canonical, verbose=not args.quiet)
        total_shapes += shapes
        all_errs += errs

    for e in all_errs:
        print("   " + e)
    print(f"\n{total_shapes} shapes, {len(all_errs)} problems")
    if all_errs:
        print("Schema: docs/02-tech/POSE-FORMAT.md — frozen at version "
              f"{ps.SCHEMA_VERSION} in P2.1")
    return 1 if all_errs else 0


if __name__ == "__main__":
    sys.exit(main())
