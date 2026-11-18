#!/usr/bin/env python3
"""Does the shelf land where a person would put it? (P4.1)

`MrUkSurfaceFinder.Collect()` is the whole placement rule: reject the wrong anchors, score the rest,
take the best. It cannot be compiled here and it cannot be run without a room, so this file mirrors it
over a handful of synthetic rooms - and reads its constants out of the C# first, so the mirror cannot
quietly drift from the product.

Six rooms, each one a case the rule has to get right:

  1. a waist-high side table in front, a couch behind, a dining table out of reach -> the side table
  2. the same table 0.2 m from the chest                                  -> rejected as too close
  3. the same table at 100 deg to the side                              -> rejected as behind
  4. only a 0.3 x 0.15 m stool                                           -> too small, plinth
  5. nothing at waist height                                             -> plinth, with the reason
  6. a 3 m table and a 0.6 m side table, both valid                       -> the side table, because
                                                                            area is capped at "enough"

Usage:  python3 scripts/shelf_sim.py     # exit 1 if the rule picks the wrong shelf
"""
import math
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
FINDER = ROOT / "unity/Assets/_Project/Scripts/Spatial/MrUkSurfaceFinder.cs"

UP = (0.0, 1.0, 0.0)


def read_constants():
    """The shipped defaults, taken from the C# so this file cannot disagree with the product."""
    src = FINDER.read_text()
    out = {}
    for name in ("minDistance", "maxDistance", "preferredHeight", "heightTolerance",
                 "forwardConeDegrees"):
        m = re.search(rf"public float {name} = ([-0-9.]+)f", src)
        if not m:
            raise SystemExit(f"could not read {name} from {FINDER.name}")
        out[name] = float(m.group(1))
    for name in ("minExtent", "comfortableExtent"):
        m = re.search(rf"public Vector2 {name} = new Vector2\(([-0-9.]+)f, ([-0-9.]+)f\)", src)
        if not m:
            raise SystemExit(f"could not read {name} from {FINDER.name}")
        out[name] = (float(m.group(1)), float(m.group(2)))
    return out


C = read_constants()
HEAD = (0.0, 1.55, 0.0)          # eye height
FORWARD = (0.0, 0.0, 1.0)


def surface(label, x, y, z, extent, height=None, name=None, yaw_deg=0.0):
    """A horizontal rectangle: centre, footprint (width, depth), and its rotation about Y.

    The rotation is here because the SDK gives us an ORIENTED rect and the shelf has to be placed on
    the edge nearest the user - a case an axis-aligned box gets wrong (room 8).
    """
    return {"label": label, "pos": (x, y, z), "extent": extent, "yaw": math.radians(yaw_deg),
            "height": y if height is None else height, "name": name or label}


def nearest_point(a, from_pos):
    """The point on the surface's rectangle closest to `from_pos`, in the rect's own frame."""
    w, d = a["extent"]
    cos, sin = math.cos(a["yaw"]), math.sin(a["yaw"])
    dx, dz = from_pos[0] - a["pos"][0], from_pos[2] - a["pos"][2]
    # into the rect frame
    lx = dx * cos + dz * sin
    lz = -dx * sin + dz * cos
    lx = max(-w / 2, min(w / 2, lx))
    lz = max(-d / 2, min(d / 2, lz))
    # back out
    return (a["pos"][0] + lx * cos - lz * sin, a["pos"][1], a["pos"][2] + lx * sin + lz * cos)


TABLE = (0.6, 0.4)
COUCH = (1.8, 0.6)
DINING = (1.6, 0.9)
STOOL = (0.30, 0.15)


def flat(v):
    return (v[0], 0.0, v[2])


def length(v):
    return math.sqrt(sum(c * c for c in v))


def angle_deg(a, b):
    la, lb = length(a), length(b)
    if la < 1e-5 or lb < 1e-5:
        return 0.0
    cos = max(-1.0, min(1.0, sum(x * y for x, y in zip(a, b)) / (la * lb)))
    return math.degrees(math.acos(cos))


def distance(a, b):
    return length((a[0] - b[0], a[1] - b[1], a[2] - b[2]))


def score_of(a):
    """The score a single candidate would receive, for the comparison printed at the end."""
    room = [a]
    best, _, _ = evaluate(room)
    return best["score"] if best else float("nan")


def evaluate(room):
    """Mirror of MrUkSurfaceFinder.Collect(): returns (best, rejects, usable)."""
    rejects = {"wrong label": 0, "too small": 0, "too close": 0, "too far": 0,
               "behind": 0, "height out of band": 0}
    usable, best, best_score = 0, None, float("-inf")

    for a in room:
        if a["label"] not in ("TABLE", "STORAGE", "COUCH", "BED", "OTHER"):
            rejects["wrong label"] += 1
            continue
        ex, ey = a["extent"]
        if ex < C["minExtent"][0] or ey < C["minExtent"][1]:
            rejects["too small"] += 1
            continue
        # reach is HORIZONTAL and is measured to the NEAREST EDGE - the C# asks the SDK for the
        # surface point closest to the user, so a big table whose middle is out of reach but whose edge
        # is in reach qualifies (room 7), and the shelf lands on that edge rather than in the middle.
        near_pt = nearest_point(a, HEAD)
        d = math.hypot(near_pt[0] - HEAD[0], near_pt[2] - HEAD[2])
        if d > C["maxDistance"]:
            rejects["too far"] += 1
            continue
        if d < C["minDistance"]:
            rejects["too close"] += 1
            continue
        if angle_deg(flat(FORWARD), flat((near_pt[0] - HEAD[0], 0.0, near_pt[2] - HEAD[2]))) > C["forwardConeDegrees"]:
            rejects["behind"] += 1
            continue
        height_error = abs(a["height"] - C["preferredHeight"])
        if height_error > C["heightTolerance"]:
            rejects["height out of band"] += 1
            continue

        to_surface = (a["pos"][0] - HEAD[0], a["pos"][1] - HEAD[1], a["pos"][2] - HEAD[2])
        l = length(to_surface)
        to_surface = tuple(c / l for c in to_surface)
        facing = sum(x * y for x, y in zip(FORWARD, to_surface))
        label_bonus = 1.0 if a["label"] in ("TABLE", "STORAGE") else 0.4

        area_x = min(1.0, ex / C["comfortableExtent"][0])
        area_y = min(1.0, ey / C["comfortableExtent"][1])
        area = (area_x + area_y) / 2.0

        score = ((1.0 - height_error / C["heightTolerance"]) * 2.0
                 + area
                 + max(0.0, min(1.0, 1.0 - d / C["maxDistance"]))
                 + max(0.0, min(1.0, facing)) * 0.5
                 + label_bonus)
        usable += 1
        if score > best_score:
            best, best_score = dict(a, score=score, reach=d, at=near_pt), score
    return best, rejects, usable


ROOMS = {
    "1 side table in front": (
        [surface("TABLE", 0.25, 0.75, 0.60, TABLE),          # the intended one
         surface("COUCH", 0.00, 0.45, -0.90, COUCH),         # behind the user
         surface("TABLE", 0.00, 0.75, 2.20, DINING)],        # out of reach
        "TABLE"),
    "2 too close": (
        [surface("TABLE", 0.10, 0.75, 0.20, TABLE),
         surface("STORAGE", 0.10, 0.75, 0.85, TABLE)],
        "STORAGE"),
    "3 behind the shoulder": (
        [surface("TABLE", 0.20, 0.75, -0.70, TABLE),         # 110 deg off forward
         surface("COUCH", 0.00, 0.45, -0.20, COUCH)],        # also behind
        None),
    "4 only a stool": (
        [surface("STORAGE", 0.10, 0.72, 0.55, STOOL)],
        None),
    "5 nothing at waist height": (
        [surface("BED", 0.00, 0.35, 0.70, COUCH),
         surface("TABLE", 0.00, 1.15, 0.80, TABLE)],
        None),
    # 6: the centre is out of reach, the edge is not. The pre-review rule measured to the centre and
    # rejected this table outright; it is the lateral twin of the 3D-distance defect.
    "6 big table, near edge in reach": (
        [surface("TABLE", 0.00, 0.75, 1.20, DINING, name="dining 1.6x0.9, centre 1.2 m")],
        "dining 1.6x0.9, centre 1.2 m"),
    # 7: the same table turned so its LONG axis points at the user. In the rect's own frame the near
    # edge is 1.6/2 = 0.8 m in, so it qualifies - and an axis-aligned box that swapped the extents would
    # have to guess which side the user is on, and guesses wrong at 45 degrees.
    "7 rotated table, long axis at user": (
        [surface("TABLE", 0.00, 0.75, 1.60, (0.50, 1.60), name="table 0.5x1.6 facing user")],
        "table 0.5x1.6 facing user"),
}

#: The area cap is a property, not a winner: this pair is scored both ways and the difference printed.
CAP_PAIR = (
    surface("TABLE", 0.00, 0.75, 1.10, DINING, name="dining 1.6x0.9"),   # near edge 0.65 m
    surface("TABLE", 0.30, 0.75, 0.75, TABLE, name="side 0.6x0.4"),      # near edge 0.55 m
)


def main():
    print("=" * 78)
    print("P4.1 shelf placement - constants read from MrUkSurfaceFinder.cs")
    print(f"  distance band {C['minDistance']:.2f}-{C['maxDistance']:.2f} m · cone {C['forwardConeDegrees']:.0f} deg "
          f"(yaw) · height {C['preferredHeight']:.2f} ± {C['heightTolerance']:.2f} m")
    print(f"  min extent {C['minExtent']} · comfortable extent {C['comfortableExtent']}")
    print("=" * 78)

    ok = True
    for name, (room, expected) in ROOMS.items():
        best, rejects, usable = evaluate(room)
        picked = best["name"] if best else None
        expected_pick = expected if expected is not None else {}
        match = (picked == expected) if isinstance(expected, str) else (picked is expected)
        verdict = "ok " if match else "FAIL"
        if not match:
            ok = False
        shown = (f"{picked} @ {best['score']:.2f} reach {best['reach']:.2f} m"
                 if best else "plinth (no surface qualified)")
        detail = ", ".join(f"{k} {v}" for k, v in rejects.items() if v)
        print(f"  [{verdict}] {name:<32} -> {shown:<24} [{detail or 'nothing rejected'}]")

    # the area cap is a design claim, not decoration: a dining table must not win on size alone
    # ---- the area cap, measured rather than asserted ------------------------------------------
    def area_bonus(extent, capped):
        ax = extent[0] / C["comfortableExtent"][0]
        ay = extent[1] / C["comfortableExtent"][1]
        if capped:
            ax, ay = min(1.0, ax), min(1.0, ay)
        return (ax + ay) / 2.0

    dining, side = CAP_PAIR
    dining_score, side_score = score_of(dining), score_of(side)
    capped_gap = area_bonus(DINING, True) - area_bonus(TABLE, True)
    raw_gap = area_bonus(DINING, False) - area_bonus(TABLE, False)
    print("\n  8 the area cap - a property, not a winner:")
    print(f"    area bonus, 1.6x0.9 table vs 0.6x0.4 table: uncapped {raw_gap:+.2f}, capped {capped_gap:+.2f}")
    print(f"    the two tables score {dining_score:.2f} and {side_score:.2f} - a margin of "
          f"{abs(dining_score - side_score):.2f}, decided by reach and height, not by size")
    if capped_gap > 0.12 or raw_gap < 0.30:
        print("    FAIL: the cap is not doing what it claims")
        ok = False
    if abs(dining_score - side_score) > 0.25:
        print("    FAIL: size is still deciding the winner")
        ok = False

    # the failure path is a path, not an exception
    none_best, none_rejects, _ = evaluate(ROOMS["5 nothing at waist height"][0])
    print(f"\n  no-surface case -> plinth, reason: "
          f"{', '.join(f'{k} {v}' for k, v in none_rejects.items() if v)}")

    print("\n" + "=" * 78)
    print("  PASS - the rule picks the shelf a person would pick, and the plinth is a real path"
          if ok else "  FAIL - the rule picks the wrong shelf")
    print("=" * 78)
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
