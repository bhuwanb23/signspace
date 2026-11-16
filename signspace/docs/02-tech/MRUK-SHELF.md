# MRUK Shelf
**Status:** ✅ v1 · **Owner:** W2 · **Phase:** P4 · **Gate:** G4

> **The shelf turns a session into a habit.** It is the only reason to come back, so it has to live
> on the user's actual furniture — a table, a lap, a desk — and survive the app being killed. And
> when MRUK cannot find a surface, the experience must not show an error: it shows a plinth that
> reads as anchored.

---

## 1. What the shelf is

| | |
|---|---|
| What | A surface in the room that holds one object per mastered handshape |
| Where | On a horizontal surface MRUK found, anchored to a **real** surface via an MRUK anchor |
| Why | *"Reason to come back" is a named award criterion, and a shelf that persists is the proof* |
| How many objects | **one per shipped shape** — six in v1, twelve in the full set |
| Persistence | keyed to the **MRUK anchor**, never a world position |

**Where it sits is a rule, not a guess** — the selection contract is §3.1, and the code that implements
it is `Scripts/Spatial/SurfaceFinder.cs` (`MrUkSurfaceFinder`). Two numbers in that rule were wrong until
P4.1's simulation found them; both are recorded in §3.1 rather than quietly fixed.

**The rule that matters:** a world position survives the app but not the room. If the shelf is stored
at `(0.4, 0.9, -0.2)` it reappears in the wrong place in a different room. If it is stored against
the anchor it reappears on the same table.

## 2. MRUK APIs

`com.meta.xr.mrutilitykit` 207.0.0, pulled in by `com.meta.xr.sdk.all`.

| API | What it does for us |
|---|---|
| `MRUK` (the component) | the room understanding system: finds surfaces, classifies them, owns the anchors |
| `MRUKRoom` | the room: a list of `MRUKSurface`s and their anchors |
| `MRUKSurface` | a found surface: its classification (`HorizontalSurface`, …), its extent, its anchor |
| `MRUKAnchor` | a pose in the room. `MRUKAnchor.AnchorState` says whether it is **valid** — this is the "is the room the same room?" check |
| `MRUKAnchor.RequestTracking` / `MRUKAnchor.SceneAnchor` | keeps a pose associated with a scene object across sessions |

**The flow.**

1. On start, the `MRUK` component scans the room. It takes a moment — 1 to 3 seconds typically.
2. Pick the best **horizontal** surface: prefer a table-like surface with enough extent for the
   shelf; fall back to the largest horizontal surface; fall back to the plinth.
3. `MRUKAnchor` it. **The anchor is the identity of the shelf.**
4. Read the save file. If the save's `anchorId` matches this anchor, restore the objects. If it does
   not match, the shelf is empty — the objects belonged to a different surface.
5. On every lock, add the new object to the shelf and write the save.

## 3. Layout and placement

### 3.1 Which surface, and why *(P4.1)*

The candidates are MRUK's anchors; the shelf lands on the best one. Four filters, then a score.

| Filter | Value | Why |
|---|---|---|
| horizontal | `dot(up, worldUp) ≥ 0.9` | a wall is not a shelf |
| label | TABLE, STORAGE, COUCH, BED, OTHER — **never FLOOR** | a lap is a legitimate shelf. A floor is not: accepting it turns every failure into a bad pass |
| free area | ≥ **0.45 × 0.20 m** | the shelf is 0.45 m wide; anything smaller is an object, not a surface |
| **reach** | **0.30 – 1.00 m, measured horizontally, to the nearest edge** | you must be able to put your hand on it. *Two corrections:* it was the **3D** distance (a waist-high table 0.6 m in front of a standing user is 1.0 m away in 3D, so the natural shelf failed), and it was measured to the **centre** (a 1.4 m table whose edge is 0.4 m away also failed). The surface point now comes from `MRUKAnchor.GetClosestSurfacePosition` queried at the user's position, so the SDK does the rect math in the anchor's own frame |
| **in front** | within **60° of forward, measured on yaw** | *also a P4.1 correction*: a waist-high surface 0.4 m away is ~40° below the eye line, so a 3D cone would reject it for being low. "In front of you" is a horizontal question |
| height | **0.75 m ± 0.35 m** above the room's floor | waist level; the same band covers a coffee table, a desk and a lap |

Score, highest wins:

| Term | Weight | What it buys |
|---|---|---|
| height closeness | **2.0** | waist height first; the band is generous but the centre is the target |
| free area (capped at 0.75 × 0.40 m) | **1.0** | enough room for the shelf — and **no reward for being big**. Uncapped, a 1.6 × 0.9 m table beats a 0.6 × 0.4 m one by **+1.29**; capped, by **+0.10** |
| proximity | **1.0** | close enough to touch |
| label | **1.0** (TABLE/STORAGE) · **0.4** (others) | a surface built to be a surface |
| facing | **0.5** | a tie-break, not a filter (the cone has already done the filtering) |

**No surface qualifies → the plinth (§6), immediately.** The rejection summary is a real diagnosis, not
"MRUK found nothing": *"anchors 12 · horizontal 5 · wrong label 1 · too small 2 · too close 0 · too far
1 · behind 1 · height out of band 2 · usable 0"* tells you which limit to change and which is the room's
fault.

**Evidence.** `scripts/shelf_sim.py` mirrors this rule, reads the constants **out of the C#** so the two
cannot drift, and runs seven rooms plus one property:

| Room | What it proves |
|---|---|
| side table in front | wins over a couch behind and a dining table out of reach |
| table 0.2 m away | rejected as too close; the next best is chosen |
| table 110° to the side | rejected as behind |
| only a 0.30 × 0.15 m stool | too small → plinth |
| nothing at waist height | → plinth, with the counter that explains it |
| 1.6 × 0.9 m table 1.2 m away | **accepted at 0.75 m** — its near edge is in reach although its centre is not |
| 0.5 × 1.6 m table rotated to face the user | **accepted at 0.80 m** — the oriented rect is used, not an axis-aligned box |
| the area cap *(a property, not a winner)* | uncapped, size is worth **+1.29**; capped, **+0.10**; the two tables end up 0.07 apart, decided by reach and height rather than by size |

The placement point is that same nearest-edge point, which is what makes §3.2's "at the edge of the
surface" literally true — before this it was the middle of the table.

### 3.2 Placement

| Rule | Value | Reason |
|---|---|---|
| Orientation | rotates to **face the user** on the yaw plane | a shelf you have to walk around is not a shelf |
| Clearance | **2 cm** above the surface point | it must read as *resting on* something, and z-fighting is not a style |
| Parenting | the anchor transform, **not** the world | the runtime corrects the anchor; a world-space shelf inherits the drift instead (§5) |
| Where on the surface | the **nearest edge** to the user, not the centre | a shelf in the middle of a dining table is out of reach; on its edge it is at your hand |
| Distance from user | 0.30–1.00 m by construction; the plinth fallback sits 0.45 m ahead at −0.25 m | inside the 2 ft airplane-seat shell |
| Spacing | one slot per shape, 7.5 cm apart, no overlap | six must be legible without crowding |
| Slot order | the order the shapes were mastered | the shelf is a record, not a menu |
| **Empty slot** | **one visible empty slot until everything is mastered** | the hook at the end of the five minutes: *"Tomorrow: one more."* |
| FoV reflow | at ~70°×66° the shelf becomes a **tray** — one row, closer, angled up | see §7 |

**The empty slot is not decoration.** `FIRST-FIVE-MINUTES.md` ends on it. It is the difference between a
shelf and a trophy.

### 3.3 The launch log *(P4.1 step 5)*

Every launch appends one JSON line to `Application.persistentDataPath/shelf-launches.jsonl`:

```json
{"utc":"…","app":"0.9.3","outcome":"Restored","onSurface":true,"label":"TABLE","score":4.54,
 "anchor":"…","savedAnchor":"…","room":"…","resolveMs":38.2,"restoreMs":1240.0,"driftM":0.041,
 "detail":"restored onto TABLE (… ) in …","finder":"anchors 12 · horizontal 5 · …"}
```

It exists for three reasons: **G4** is judged on relaunches, so the file is the evidence that a
relaunch chose the same surface; **P8** needs to know how often the plinth actually appears in a
playtest; and the video's shelf shot needs to know which launch had drift rather than grabbing a
random one. It is written from one place — after `Begin()` returns — so no outcome path can skip it,
and a write failure is a warning, never a lost shelf.

## 4. The objects

| | |
|---|---|
| What is an object | the mastered handshape, as a small solid form. The handshape itself, solidified and simplified — **not a photo, not a video** |
| Why solidified | a photo of a hand is a flat video again. The object has to be 3D or the whole thesis collapses |
| How it appears | one arc from the hand to its slot, **500 ms**, then a settle (§4.2) |
| Idle | breathing: `localScale` scaled by ±0.6%, 0.18 Hz (§4.3) |
| Count | one per mastered shape, up to **12** slots |
| The credits object | a distinct object, placed last, that opens the credits |

**The settle animation is the only flourish in the whole product.** It is cut ladder rung 4
(*"visual charm only"*), so if the day is bad, it goes first and nothing else changes.

### 4.1 Four silhouettes, six shapes *(P4.2 steps 1, 2)*

Six hand-authored models on day five is three hours of modelling for something read at 16 cm through
passthrough. Four **family recipes**, with one cached mesh per shape and the difference inside a family carried by one parameter,
which is also what makes the difference *legible* rather than decorative:

| family | shapes | recipe | boxes | triangles | colour |
|---|---|---|---|---|---|
| fist | B | palm + thumb across it | 2 | 24 | warming amber (0.78, 0.46, 0.16) |
| pair | U, V | palm + two fingers; **the spread is the difference** — 0.000 vs 0.020 m | 3 | 36 | teal (0.16, 0.65, 0.63) |
| triple | W | palm + three fingers | 4 | 48 | (0.45, 0.58, 0.78) |
| curve | C, O | an arc of segments; **the gap is the difference** — 230° vs 300° | 6 | 72 | locked tone (0.95, 0.89, 0.76) |

Built from unit boxes at runtime (`ShelfSilhouettes.cs`) and cached per shape. A box is 12 triangles,
which is the only triangle count that does not move between Unity versions. **One mesh, one renderer,
one estimated draw call per object in a single pass** — batching and extra passes remain device checks. `scripts/shelf_budget.py` reads these recipes out of the C# and re-does the arithmetic; the
result is `phases/p4-shelf/budget-check.md`.

If two shapes in a family differed only in colour, that would be a legend, not a silhouette. Colour
is per family; the difference is geometry.

### 4.2 Slots *(P4.2 steps 2, 4)*

Twelve slots in a **6 × 2 grid** on the 0.50 × 0.16 m plate (grid 0.375 m × 0.065 m). Each slot is
a child transform under `Slots`, created once by `SlotTransform` and reused, so the flight target,
the resting place and the marker share a position, with the object lifted by its mesh bounds. The
grid dimensions are centre-to-centre; overlap still needs visual verification.

- On lock, `LockPayoff` asks `ShelfAnchor.TryGetSlotFor(shapeId)` and flies the object to **that**
  slot's position on a **500 ms** arc. An already-unlocked shape returns to its **own** slot; a new
  shape takes the next empty one. When the shelf is full, a new distinct shape has no slot; its fallback and capacity need product validation.
- The flight is resolved *after* `Unlock()` has run, one line above it, because the slot index
  depends on what was unlocked before this lock.
- With a shelf, nothing is trimmed: `maxObjectsOnAnchor` only applies to the pre-shelf pile. The
  shelf is the record.
- **The next empty slot is visible and dimmed from the first launch** — a marker quad, generated if
  nobody wired a prefab, RGB-tinted through a `MaterialPropertyBlock` even on an opaque material. One
  dim slot is the invitation; a row of twelve lit ones would be a to-do list.

### 4.3 The idle is one Update, not twelve *(P4.2 step 3)*

`ShelfIdle` holds every object's transform and breathes them from a **single** `Update`: one `Sin`,
one multiply, one `localScale` write per object per frame. Twelve objects each running their own
`Update` is the trap the task names. Twelve `Mathf.Sin` calls still have a cost; its actual CPU
cost remains unmeasured.

`breathing` is a field, not a constant, because the acceptance number — **<1% CPU, or the idle is
dropped** — is measured on a device and has not been measured yet. If the capture says no, the switch
goes off and the shelf is still, which is a worse-looking shelf and an unchanged product.

## 5. Persistence (P4.3 — desk-verified, not device verified)

`SaveSystem` schema 3 stores `roomId` (room UUID), `anchorId` (surface GUID), the shelf's
**anchor-local** `localPos` and `localRot`, `layoutVariant` (`grid-6x2-v1`), ordered `unlocked`
shape IDs and `unlockRecords` with per-shape UTC timestamps. The ID list is the authoritative
unlock/slot order. `lastWorldPos` is diagnostic only and is never used for restoration.

**Room policy:** one protected home placement. When the current room UUID matches home, resolve the
saved anchor in that current room and rebuild shapes in unlock order. In a different room, with no
current room UUID, or with an unavailable home anchor, show a temporary plinth and retain the home
placement. Progress is global: unlocks on that plinth persist and appear when home is restored.
No object-name fallback or world transform is treated as a stable identity. Returning to the home room
retries its original anchor; there is no independent per-room shelf. Unknown layout variants fall back
without overwriting the saved layout.

Schema 2 migrates the existing unlock order and retains unknown historic timestamps as empty strings;
schema 1's placement is not trusted. Future/invalid or unreadable saves are protected from writes.
A temporary file and backup are used for writes, but kill/power-loss behaviour on Android is not yet
verified. `phases/p4-shelf/persistence-matrix.md` specifies the device tests. Static source checks are
not proof of actual serialisation, MRUK room changes, physical position, or a headset reboot.

**What has actually been run (16 Nov 2026, Unity 6000.3.28f1).** `SIGNSPACE/P4.3 Check persistence`
passes: schema 1/2/3 migration, idempotent unlocks, rejected replacement, all four room branches, and
the refusal to overwrite a corrupt or future file. `PersistenceRelaunch` then loads the same file in
**five separate Unity processes**: each restores `U,B,C` in order at the same anchor-local position,
three objects and no duplicates. `LeaveRoom` reports `DifferentRoom` with the plinth, keeps the home
UUIDs and saves a fourth shape unlocked away from home; `ReturnRoom` restores the home anchor with
`U,B,C,W` and four objects. That probe found a real defect — a second restore in one frame duplicated
objects because `Destroy` is deferred — now fixed by deactivating before destroying.

It is still not a device pass: the finder is fake, the UUIDs are fixed, and no Android process, MRUK
localisation, physical surface or reboot took part.

## 6. The plinth — the fallback that never shows an error

`P4.4`: *"Fallback: no surface found → virtual plinth that reads as anchored. Never shows an error,
never crashes."*

| Condition | What the user sees |
|---|---|
| A horizontal surface is found | the shelf, on it |
| No horizontal surface, but there is room | a **virtual plinth**: a soft, shadow-catching platform at hip height, in the same position the shelf would have been |
| No surface and no room | the plinth, smaller, closer |
| MRUK is still scanning | the plinth, immediately; if a surface appears later, a small bead **offers** it and the shelf does not move until the bead is pinched |

**The plinth is not a degraded mode. It is a designed object.** `Prefabs/Plinth.prefab` (built by
`PlinthBuilder`) has four parts: a top carrying a generated front-to-back gradient, a darker lip for
thickness, a soft transparent shadow (queue 3000, ZWrite off) and a thin seam that catches light. The
prefab's local Y=0 is its top, it is instantiated 1 cm under the plate so the plate sits on it with
no gap, and it is tilted 4°: a perfectly level slab reads as UI, a slightly tilted one reads as an
object somebody put there. The user never sees a message, a spinner or "no surface detected".

**A surface that appears later is offered, never applied.** A small bead on the plate's right edge
(`SurfaceOffer`) is hidden until there is an offer; only a pinch inside 14 cm accepts it. The offer
is raised only when there is no saved home, so a surface in another room cannot capture the home
placement — the same non-destructive rule as §5.

**The plinth keeps the same anchor identity as the shelf** — if MRUK later finds a surface, the
objects move onto it rather than being re-earned.

## 7. FoV reflow for ~70°×66°

The VR Glasses have a much narrower field of view: **37° temporal, 37° nasal, 34° up, 34° down**,
against Quest's ~110°×96°.

| | Quest | VR Glasses |
|---|---|---|
| Shelf layout | **6 columns × 2 rows** on the surface | **4 columns × 3 rows**, pulled 4 cm closer, tilted up 20° |
| Grid angular width | 51.6° at 0.45 m | 35.1° |
| Widest object edge | 27.5° (at the 55° cone edge) | **22.2°** (5.3° spare) |
| Object size | unchanged | **unchanged** — narrowing the grid, never scaling the objects |
| Everything else | unchanged | unchanged |

`ShelfReflow` measures the display rather than guessing: horizontal FoV from the eye camera's
vertical FoV and aspect, distance from the head in the shelf's own frame, width as
`2·atan(halfWidth / distance)`. Below **85°** horizontal FoV it folds into the tray. The threshold is
FoV, not a fixed 50°: the same 51.6° grid is comfortable at 110° and cramped at 70°, so the display
is the honest variable. The 50° cap from P4.6 is still enforced as an assertion on the tray's width.
`ShelfAnchor.SetLayout` carries the objects in world space, rebuilds the slots, tilts and pulls the
tray, and narrows the plate. The layout is not persisted — `layoutVariant` records the grid the save
was written against, and the display decides how it is presented today.

`showSafeCone` draws the 55° cone for the video and is off in the scene. The transition is eased over
0.35 s.

**Reflow, not crop.** The shelf becomes a tray. Nothing is cut off, because cropping the shelf is
cropping the reward.

**The FoV pair is shot 12 of the video** — the same frame at both field sizes, side by side. The
narrow one must have *everything critical inside the ~55° cone*.

## 8. The test matrix

Every row must behave. No row may show an error.

| # | Condition | Expected | What it proves |
|---|---|---|---|
| 1 | A dining table, in daylight | shelf on the table | the happy path |
| 2 | A coffee table, low, in lamplight | shelf on the table | lighting does not break it |
| 3 | **A lap** (sitting, no table) | plinth, or a shelf on the thighs if a surface is found | the airplane-seat case |
| 4 | **A bed** | plinth | a bed is not a table |
| 5 | **An airplane tray table** | shelf on the tray | literally the competition's test |
| 6 | **A bare floor** | plinth | the worst case |
| 7 | **An unmade room, clutter** | plinth or a small surface, never an error | robustness |
| 8 | **A wall at 0.5 m** (no surface in reach) | plinth | "untested-room test" from `BATTLE-CARD.md` |
| 9 | **Kill and relaunch** | the shelf is on the same surface with the same objects | **G4** |
| 10 | **Kill, reboot the headset, relaunch** | the same | persistence survives a reboot |
| 11 | **Five relaunches in a row** | the same, every time | it is not luck |
| 12 | **Move to a different room** | temporary plinth with global progress; home placement preserved | P4.3 policy; device test pending |
| 13 | Passthrough off | the shelf loses its ground and floats | the passthrough test |

**Rows 9, 10 and 12 are the gate.** Row 9 is `G4` exactly.

## 9. What is deliberately not here

| | |
|---|---|
| No room decoration, no furniture the user did not ask for | the room is the user's |
| No placement of objects on surfaces the user did not intend | the shelf is on one surface, and one |
| No multiple shelves | one anchor, one shelf, one honest place |
| No social presence of other people's shelves | no multiplayer, deliberately (`RISKS.md` R7) |
| No object physics | an object that rolls off the desk is not a reward |

## 10. Inputs
`ARCHITECTURE.md` §4 (the save schema) · `UX-STATES.md` (`SurfaceFound`, `Plinth`, `ShelfFull`) ·
`MRUK-SHELF.md` is this file · `FIRST-FIVE-MINUTES.md` (the one empty slot) · `SaveSystem.cs`

## 11. Output / done when
- [x] which MRUK APIs, and what triggers detection
- [x] behaviour on a lap, a bed, a tray table, an unmade room
- [ ] anchor persistence across sessions and room transitions verified on device *(desk: 5/5
  relaunches and leave/return pass; headset rows open)*
- [x] the fallback that never shows an error *(prefab + silent routing + pinch-only offer, editor check
  PASS; dark room open)*
- [x] the FoV reflow at 70°×66° *(measured geometry, editor check PASS; visual legibility open)*
- [x] the test matrix
- [x] **the selection contract, with weights** (§3.1) and the two corrections P4.1 found
- [x] **the launch log** (§3.3) — every launch, one line, one writer
- [x] **four silhouettes for six shapes, twelve slots, one idle Update** (§4.1–§4.3)
- [x] the budget re-derived from the shipped constants — `phases/p4-shelf/budget-check.md`
- [x] `scripts/shelf_sim.py` — seven rooms and the area-cap property, constants read out of the C#
- [ ] **every row of the matrix has been run**
- [ ] **row 9 has been recorded**
- [ ] **a real room has produced a launch-log line** (needs the headset)
