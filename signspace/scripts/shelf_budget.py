#!/usr/bin/env python3
"""P4.2 step 5 - twelve objects and twelve slots, against the P7 budgets.

The task asks for one thing to be *confirmed* rather than assumed: that a shelf holding twelve
mastered shapes still fits inside the draw-call and triangle budget the project set for Quest 3S.
Confirming it from memory is how a budget document becomes fiction, so this script reads the
recipes out of the shipped C# and the ceilings out of `docs/02-tech/PERFORMANCE.md`, and does the
arithmetic in front of you.

    python3 scripts/shelf_budget.py                     # check, write phases/p4-shelf/budget-check.md
    python3 scripts/shelf_budget.py --check             # check only, no write
    python3 scripts/shelf_budget.py --out /tmp/b.md     # write somewhere else
    python3 scripts/shelf_budget.py --slots 200 --check # negative control: prove the ceiling bites

Exit 1 on any breach, so it can run in CI next to `shelf_sim.py`.

What it counts
--------------
* one draw call per object and per slot marker - the shelf does NOT batch, because each object
  can be a different mesh. That is the honest number, and the cheapest thing to be wrong about.
* triangles from the shipped box recipes (`BoxesFist` ... `TrianglesPerBox`), taking the worst
  shape in the worst slot, not the average.
* the shelf's own furniture: the plate, the plinth, and the markers.
* materials by unique shared material, not per renderer.

What it cannot count
--------------------
The rest of the scene. The ceilings in PERFORMANCE.md are for the whole frame, so the script
prints the headroom it leaves and the number a device capture (P4.2 / P7.4) has to beat.
"""

from __future__ import annotations

import argparse
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
SILHOUETTES = ROOT / "unity/Assets/_Project/Scripts/Spatial/ShelfSilhouettes.cs"
ANCHOR = ROOT / "unity/Assets/_Project/Scripts/Spatial/ShelfAnchor.cs"
PERFORMANCE = ROOT / "docs/02-tech/PERFORMANCE.md"
DEFAULT_OUT = ROOT / "phases/p4-shelf/budget-check.md"

SHAPES = ["B", "U", "V", "W", "C", "O"]


# --- reading the shipped numbers --------------------------------------------------------------

def read_constants(path: pathlib.Path, names: list[str]) -> dict[str, int]:
    text = path.read_text()
    out: dict[str, int] = {}
    for name in names:
        m = re.search(rf"const\s+int\s+{name}\s*=\s*(-?\d+)", text)
        if not m:
            sys.exit(f"FAIL  {path.name}: could not find `const int {name}`. "
                     f"The budget would be arithmetic on a number that is no longer shipped.")
        out[name] = int(m.group(1))
    return out


def read_fields(path: pathlib.Path, names: list[str]) -> dict[str, float]:
    text = path.read_text()
    out: dict[str, float] = {}
    for name in names:
        m = re.search(rf"public\s+int\s+{name}\s*=\s*(-?\d+)\s*;", text)
        if m:
            out[name] = int(m.group(1))
            continue
        m = re.search(rf"public\s+float\s+{name}\s*=\s*(-?[\d.]+)f\s*;", text)
        if not m:
            sys.exit(f"FAIL  {path.name}: could not find field `{name}`.")
        out[name] = float(m.group(1))
    return out


def read_families(path: pathlib.Path) -> dict[str, str]:
    """shape id -> family, out of the switch in `FamilyOf`."""
    text = path.read_text()
    m = re.search(r"static string FamilyOf.*?\n        \}", text, re.S)
    if not m:
        sys.exit("FAIL  FamilyOf() not found: the shape-to-family map lives there and nowhere else.")
    body = m.group(0)
    families: dict[str, str] = {}
    pending: list[str] = []
    # a switch with stacked cases: collect ids until the return, then assign them all
    for part in re.split(r"(case\s+\"[^\"]+\"\s*:|return\s+\"[^\"]+\")", body):
        part = part.strip()
        if not part:
            continue
        cm = re.match(r'case\s+"([^"]+)"\s*:', part)
        if cm:
            pending.append(cm.group(1))
            continue
        rm = re.match(r'return\s+"([^"]+)"', part)
        if rm:
            for shape in pending:
                families[shape] = rm.group(1)
            pending = []
    missing = [s for s in SHAPES if s not in families]
    if missing:
        sys.exit(f"FAIL  FamilyOf() does not map {missing}: a shape with no family has no silhouette.")
    return families


def read_budgets(path: pathlib.Path) -> dict[str, int]:
    """The ceilings from PERFORMANCE.md section 1.

    Read as UTF-8: the table says `≤`, and on Windows the default locale codec would decode that
    into mojibake, making every ceiling look absent. The doc is not wrong; the reader was.
    """
    text = path.read_text(encoding="utf-8")
    budgets: dict[str, int] = {}
    rows = {
        "draw_calls": r"\|\s*Draw calls\s*\|\s*(?:&le;|<=|≤)\s*([\d,.]+)\s*(k?)",
        "triangles": r"\|\s*Triangles[^|]*\|\s*(?:&le;|<=|≤)\s*([\d,.]+)\s*(k?)",
        "materials": r"\|\s*Materials\s*\|\s*(?:&le;|<=|≤)\s*([\d,.]+)\s*(k?)",
    }
    for key, pattern in rows.items():
        m = re.search(pattern, text)
        if not m:
            sys.exit(f"FAIL  PERFORMANCE.md has no `{key}` ceiling. The budget cannot be checked "
                     f"against a number nobody wrote down.")
        value = float(m.group(1).replace(",", ""))
        budgets[key] = int(value * 1000 if m.group(2) else value)   # "150k" is a number too
    return budgets


# --- the arithmetic --------------------------------------------------------------------------

def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="check only; do not write the report")
    ap.add_argument("--out", default=str(DEFAULT_OUT))
    ap.add_argument("--slots", type=int, default=None,
                    help="override the shipped slot count. A probe, not a plan: use it to watch the "
                         "check fail, because a ceiling nothing can breach is not a check.")
    args = ap.parse_args()

    boxes = read_constants(SILHOUETTES, ["BoxesFist", "BoxesPair", "BoxesTriple", "BoxesCurve",
                                         "TrianglesPerBox"])
    slots = read_fields(ANCHOR, ["slotCount", "slotColumns", "slotSpacing", "slotRowSpacing"])
    families = read_families(SILHOUETTES)
    budget = read_budgets(PERFORMANCE)

    per_box = boxes["TrianglesPerBox"]
    family_boxes = {
        "fist": boxes["BoxesFist"],
        "pair": boxes["BoxesPair"],
        "triple": boxes["BoxesTriple"],
        "curve": boxes["BoxesCurve"],
    }
    family_tris = {f: b * per_box for f, b in family_boxes.items()}
    shape_tris = {s: family_tris[families[s]] for s in SHAPES}

    slot_count = int(args.slots if args.slots is not None else slots["slotCount"])
    columns = int(slots["slotColumns"])
    rows = (slot_count + columns - 1) // columns

    # Worst case, not average: every slot holding the heaviest shape. A shelf that only fits when
    # the user happens to learn the cheap shapes is not a shelf that fits.
    worst_shape = max(shape_tris, key=lambda s: (shape_tris[s], s))
    heaviest = shape_tris[worst_shape]

    # Draw calls. One renderer per object, one per marker, plus the shelf's own plate and plinth.
    objects = slot_count
    markers = slot_count           # a marker for every filled slot and one dim for the next empty
    furniture = 2                  # ShelfPlate, Plinth (the plinth is inactive when on a surface)
    draw_calls = objects + markers + furniture

    triangles = objects * heaviest + markers * 2 + furniture * 12   # a quad is 2 tris, a cube 12

    # Materials: the object silhouette material, the dimmed marker material, the plate, the plinth.
    materials = 4

    # Meshes, not draw calls - worth printing because it is the reason there is no batching.
    unique_meshes = len(SHAPES)  # ShelfSilhouettes caches by shape ID, not family

    # Geometry sanity: does a two-row grid actually fit the plate the builder makes?
    plate_width, plate_depth = 0.50, 0.16
    grid_width = (columns - 1) * float(slots["slotSpacing"])
    grid_depth = (rows - 1) * float(slots["slotRowSpacing"])

    checks = [
        ("draw calls", draw_calls, budget["draw_calls"]),
        ("triangles", triangles, budget["triangles"]),
        ("materials", materials, budget["materials"]),
        ("slot grid width on the plate", round(grid_width, 3), plate_width),
        ("slot grid depth on the plate", round(grid_depth, 3), plate_depth),
    ]

    failures = [name for name, value, limit in checks if value > limit]
    ok = not failures

    lines: list[str] = []
    add = lines.append
    add("# P4.2 step 5 — twelve objects, twelve slots, and the budget")
    add("")
    add("_Generated by `scripts/shelf_budget.py` from the shipped C# and `PERFORMANCE.md` §1. "
        "Do not edit by hand: the numbers are read, not remembered._")
    add("")
    add(f"**{'PASS' if ok else 'FAIL'}** — {slot_count} objects, {slot_count} slots, "
        f"{columns} × {rows} grid.")
    add("")
    add("## The shapes")
    add("")
    add("| shape | family | boxes | triangles |")
    add("|---|---|---|---|")
    for s in SHAPES:
        add(f"| {s} | {families[s]} | {family_boxes[families[s]]} | {shape_tris[s]} |")
    add("")
    add(f"One mesh per shape, cached and shared: **{unique_meshes} meshes** for {len(SHAPES)} shapes. "
        "A box is 12 triangles, which is the only triangle count that does not drift between Unity "
        "versions.")
    add("")
    add("## The shelf")
    add("")
    add(f"Worst case: every one of the {slot_count} slots holding the heaviest shape (**{worst_shape}**, "
        f"{heaviest} triangles), and every slot carrying a marker. This is a hypothetical stress "
        "case; the current six unique shapes cannot populate twelve slots without a dedicated test rig.")
    add("")
    add("| item | count | draw calls | triangles |")
    add("|---|---|---|---|")
    add(f"| objects | {objects} | {objects} | {objects * heaviest} |")
    add(f"| slot markers (quads) | {markers} | {markers} | {markers * 2} |")
    add(f"| plate + plinth | {furniture} | {furniture} | {furniture * 12} |")
    add(f"| **total** | | **{draw_calls}** | **{triangles}** |")
    add("")
    add("Estimate one draw call per object renderer in a single pass. Actual rendering, batching and additional passes require a device capture. "
        f"{unique_meshes} distinct meshes are possible; the estimate does not assume batching.")
    add("")
    add("## Against the ceilings")
    add("")
    add("| slice | this shelf | ceiling | headroom |")
    add("|---|---|---|---|")
    for name, value, limit in checks:
        headroom = limit - value
        unit = " m" if "grid" in name else ""
        add(f"| {name} | {value}{unit} | {limit}{unit} | {headroom:+.3f}{unit} |"
            if unit else f"| {name} | {value:,} | {limit:,} | {headroom:+,} |")
    add("")
    add(f"The estimate leaves **{budget['draw_calls'] - draw_calls} draw calls**, "
        f"**{budget['triangles'] - triangles:,} triangles** and "
        f"**{budget['materials'] - materials} materials** for the rest of the scene — the reference "
        "hands, the panel, the halo, the near-miss coach. Those are the numbers P7.4 measures on device; "
        "if the frame comes in over budget, this estimate alone cannot identify the cause of an over-budget frame.")
    add("")
    add("## What this does not prove")
    add("")
    add("- Nothing here is a device measurement. Draw calls assume one pass per renderer; triangle "
        "counts rely on the declared box constants matching the mesh recipes. Materials are an "
        "assumption, not parsed assets. Frame time and idle CPU require a Quest 3S capture "
        "(<1% CPU for the breathing, or the idle is dropped).")
    add("- Grid dimensions measure slot centres only, not object bounds or visual overlap. "
        "Check spacing and rendering in Unity/device.")
    add("- Markers are counted for all twelve slots because a filled slot keeps its marker. If P7 "
        "drops the markers on filled slots the single-pass estimate drops twelve draw calls.")
    add("- Twelve objects at 72 triangles is ~0.9k triangles: the triangles were never the risk. The "
        "draw calls and the single Update are.")
    add("")

    report = "\n".join(lines)
    if not args.check:
        out = pathlib.Path(args.out)
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_text(report)
        print(f"wrote {out.relative_to(ROOT) if out.is_relative_to(ROOT) else out}")

    print(report if args.check else "")
    for name, value, limit in checks:
        print(f"  {'ok  ' if value <= limit else 'FAIL'} {name}: {value} / {limit}")
    print(("PASS  " if ok else "FAIL  ") + f"{slot_count} objects, {slot_count} slots, "
          f"{draw_calls} draw calls, {triangles} triangles")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
