#!/usr/bin/env python3
"""Validate every handshape JSON before it ships. Fails loudly: a malformed pose is a
shape that silently never locks, which is the worst demo failure available."""
import json, sys, pathlib

def check(path: pathlib.Path) -> list[str]:
    errs = []
    try:
        d = json.loads(path.read_text())
    except Exception as e:
        return [f"{path.name}: unparseable JSON ({e})"]
    for k in ("version", "id", "jointCount", "jointNames", "q"):
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
    for i in range(min(n, len(q) // 4)):
        x, y, z, w = q[i*4:i*4+4]
        m = x*x + y*y + z*z + w*w
        if abs(m - 1.0) > 0.01:
            errs.append(f"{path.name}: joint {i} ({d['jointNames'][i]}) |q|^2 = {m:.4f}")
    return errs

def main(folder="unity/Assets/StreamingAssets/Signs"):
    files = [p for p in pathlib.Path(folder).glob("*.json") if p.name != "set-meta.json"]
    if not files:
        print("no pose files found"); return 0
    all_errs = []
    for f in sorted(files):
        e = check(f)
        print(f"[{'FAIL' if e else ' OK '}] {f.name}")
        all_errs += e
    for e in all_errs:
        print("   " + e)
    print(f"\n{len(files)} files, {len(all_errs)} problems")
    return 1 if all_errs else 0

if __name__ == "__main__":
    sys.exit(main(*sys.argv[1:]))
