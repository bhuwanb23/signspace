#!/usr/bin/env python3
"""Validate every handshape JSON before it ships. Fails loudly: a malformed pose is a
shape that silently never locks, which is the worst demo failure available.

Also checks the things that only bite on device:
  * the joint set is the canonical one the matcher expects
  * index.json lists exactly the shapes that are actually present
  * every shape's source is declared
"""
import json, sys, pathlib

REPO_ROOT = pathlib.Path(__file__).resolve().parent.parent
sys.path.insert(0, str(REPO_ROOT / "scripts"))

import hand_model as hm  # noqa: E402

SKIP = {"set-meta.json", "index.json"}
REQUIRED_SHAPE_KEYS = ("version", "id", "handedness", "jointCount", "jointNames", "q", "source")


def check(path: pathlib.Path, canonical: list[str]) -> list[str]:
    errs = []
    try:
        d = json.loads(path.read_text())
    except Exception as e:
        return [f"{path.name}: unparseable JSON ({e})"]

    for k in REQUIRED_SHAPE_KEYS:
        if k not in d:
            errs.append(f"{path.name}: missing '{k}'")
    if errs:
        return errs

    n, q = d["jointCount"], d["q"]
    if len(d["jointNames"]) != n:
        errs.append(f"{path.name}: jointNames {len(d['jointNames'])} != jointCount {n}")
    if len(q) != n * 4:
        errs.append(f"{path.name}: q has {len(q)} floats, expected {n*4} (4 per joint)")
    if len(set(d["jointNames"])) != len(d["jointNames"]):
        errs.append(f"{path.name}: duplicate joint names")

    if list(d["jointNames"]) != canonical:
        missing = [j for j in canonical if j not in d["jointNames"]]
        extra = [j for j in d["jointNames"] if j not in canonical]
        if missing:
            errs.append(f"{path.name}: missing canonical joints {missing}")
        if extra:
            errs.append(f"{path.name}: unexpected joints {extra}")
        if not missing and not extra:
            errs.append(f"{path.name}: joint names are in the wrong ORDER")

    for i in range(min(n, len(q) // 4)):
        x, y, z, w = q[i*4:i*4+4]
        m = x*x + y*y + z*z + w*w
        if abs(m - 1.0) > 0.01:
            errs.append(f"{path.name}: joint {i} ({d['jointNames'][i]}) |q|^2 = {m:.4f}")
    return errs


def check_index(folder: pathlib.Path, present: list[str]) -> list[str]:
    errs = []
    index_path = folder / "index.json"
    if not index_path.exists():
        return [f"index.json is missing - SignSetLoader cannot list the folder on Android"]

    try:
        index = json.loads(index_path.read_text())
    except Exception as e:
        return [f"index.json: unparseable JSON ({e})"]

    listed = index.get("shapes") or []
    if not listed:
        return ["index.json: lists no shapes"]

    for s in listed:
        if s not in present:
            errs.append(f"index.json lists '{s}' but {s}.json is not present")
    for s in present:
        if s not in listed:
            errs.append(f"{s}.json is present but index.json does not list it")
    return errs


def main(folder="unity/Assets/StreamingAssets/Signs"):
    folder = pathlib.Path(folder)
    if not folder.exists():
        print(f"no pose folder at {folder}")
        return 1

    canonical = list(hm.JOINT_NAMES)
    files = [p for p in sorted(folder.glob("*.json")) if p.name not in SKIP]

    if not files:
        print("no pose files found")
        return 0

    all_errs = []
    present = []
    for f in files:
        if f.name.startswith("EXAMPLE"):
            print(f"[SKIP] {f.name} (example, not shipped)")
            continue
        e = check(f, canonical)
        present.append(f.stem)
        print(f"[{'FAIL' if e else ' OK '}] {f.name}")
        all_errs += e

    index_errs = check_index(folder, present)
    print(f"[{'FAIL' if index_errs else ' OK '}] index.json")
    all_errs += index_errs

    for e in all_errs:
        print("   " + e)
    print(f"\n{len(present)} shapes, {len(all_errs)} problems")
    return 1 if all_errs else 0


if __name__ == "__main__":
    sys.exit(main(*sys.argv[1:]))
