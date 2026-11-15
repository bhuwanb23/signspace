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

| Rule | Value | Reason |
|---|---|---|
| Orientation | the shelf faces the user, rotated to the surface's normal | a shelf you have to walk around is not a shelf |
| Distance | placed **0.35 m to 0.5 m** from the user, at the edge of the surface | inside the 2 ft airplane-seat shell |
| Height | on the surface, not floating | it must read as *resting on* something |
| Spacing | one slot per shape, evenly spaced, no overlap | twelve slots must be legible without crowding |
| Slot order | the order the shapes were mastered | the shelf is a record, not a menu |
| **Empty slot** | **one visible empty slot until everything is mastered** | the hook at the end of the five minutes: *"Tomorrow: one more."* |
| FoV reflow | at ~70°×66° the shelf becomes a **tray** — one row, closer, angled up | see §7 |

**The empty slot is not decoration.** `FIRST-FIVE-MINUTES.md` ends on it. It is the difference
between a shelf and a trophy.

## 4. The objects

| | |
|---|---|
| What is an object | the mastered handshape, as a small solid form. The handshape itself, solidified and simplified — **not a photo, not a video** |
| Why solidified | a photo of a hand is a flat video again. The object has to be 3D or the whole thesis collapses |
| How it appears | a **settle animation**: it drops into place and comes to rest. 250 ms. |
| Idle | a very slow rotation, 2° per second, so it reads as an object and not a sticker |
| Count | one per mastered shape |
| The credits object | a distinct object, placed last, that opens the credits |

**The settle animation is the only flourish in the whole product.** It is cut ladder rung 4
(*"visual charm only"*), so if the day is bad, it goes first and nothing else changes.

## 5. Persistence

One file, written on lock and on quit. `SaveSystem.cs` already implements it.

```json
{
  "schema": 1,
  "roomId": "…",
  "anchorId": "…",
  "unlocked": ["B", "U"],
  "calibrated": false
}
```

| Field | Why |
|---|---|
| `roomId` | which room the save belongs to. A different room is a different shelf. |
| `anchorId` | **the identity of the surface.** The load check is `anchorId == currentAnchorId` |
| `unlocked` | which shapes are mastered. The shelf is rebuilt from this list |
| `calibrated` | whether the hand calibration has been done |

**The load rule, in full:**

```
if save.anchorId == currentAnchor.anchorId:
    rebuild the shelf from save.unlocked
else:
    empty shelf, silently
```

**Silently.** No message, no "your progress was lost". The shelf simply starts empty. If the room
changed, the old shelf is not there any more — that is not an error, it is a new room.

**Never store a world position.** A position is only meaningful against the anchor that produced it.

## 6. The plinth — the fallback that never shows an error

`P4.4`: *"Fallback: no surface found → virtual plinth that reads as anchored. Never shows an error,
never crashes."*

| Condition | What the user sees |
|---|---|
| A horizontal surface is found | the shelf, on it |
| No horizontal surface, but there is room | a **virtual plinth**: a soft, shadow-catching platform at hip height, in the same position the shelf would have been |
| No surface and no room | the plinth, smaller, closer |
| MRUK is still scanning | the plinth, fading in — **the shelf fades in over it when a surface appears** |

**The plinth is not a degraded mode. It is a designed object.** It has a gradient that catches light
so it reads as *placed*, and it sits in the identical position the shelf would. The user never sees
a message, never sees a spinner, never sees "no surface detected".

**The plinth keeps the same anchor identity as the shelf** — if MRUK later finds a surface, the
objects move onto it rather than being re-earned.

## 7. FoV reflow for ~70°×66°

The VR Glasses have a much narrower field of view: **37° temporal, 37° nasal, 34° up, 34° down**,
against Quest's ~110°×96°.

| | Quest | VR Glasses |
|---|---|---|
| Shelf layout | a **row** along the surface, six slots | a **tray**: one row, pulled closer, tilted up ~20° |
| Slot spacing | generous | tight |
| Labels | floating labels under each slot | labels suppressed below a threshold — at this FoV a label is unreadable and in the way |
| Everything else | unchanged | unchanged |

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
| 12 | **Move to a different room** | an empty shelf, silently | the anchor rule |
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
- [x] anchor persistence across sessions **and** across rooms
- [x] the fallback that never shows an error
- [x] the FoV reflow at 70°×66°
- [x] the test matrix
- [ ] **every row of the matrix has been run**
- [ ] **row 9 has been recorded**
