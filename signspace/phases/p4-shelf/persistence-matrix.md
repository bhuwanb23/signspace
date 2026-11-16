# P4.3 — persistence test matrix

**Status: NOT RUN on a headset. G4 remains open.** No Unity editor or Quest was available in this
workspace. Rows below are a procedure and evidence sheet, not results. Do not infer a pass from
source checks or from a JSONL line alone.

## Contract under test

- Schema **3** stores the home room UUID, surface anchor GUID, anchor-local position and rotation,
  ordered `unlocked` IDs, `unlockRecords` (ID + UTC timestamp), `layoutVariant` (`grid-6x2-v1`),
  and diagnostic fields. The ordered ID list determines slots; repeating a shape leaves its timestamp
  and slot unchanged. Legacy schema 2 order is retained and unknown historical timestamps stay empty;
  schema 1 cannot claim a valid placement. Unknown/newer/corrupt files are read-only, not overwritten.
- The saved home room and anchor are **not replaced** when entering a different or unidentified room.
  A temporary plinth carries the same progress there; new unlocks remain global. Returning to the
  original room re-resolves that room's original anchor. This is a single-home policy, **not** a
  promise of independent room shelves. A no-room or no-anchor fallback also uses the plinth.
- Only non-empty room and surface UUIDs can establish a persistent placement. Simulator rooms that
  provide only object names will use the temporary plinth. Actual UUID exposure/localisation in MRUK
  v207 has not yet been verified on device.
- Placement is relative to the resolved anchor, never restored from `lastWorldPos`. The latter is
  diagnostic tracking-origin displacement, **not physical drift** or proof of correct placement.

## Setup and evidence

1. Build the main scene/APK on Unity 6000.3.28f1 with MRUK v207. Run the editor menu
   `SIGNSPACE/P4.3 Check persistence` first; record its output and any compile/import failure.
   This check creates isolated temporary files; it does not use the headset save.
2. On Quest 3S, grant scene access, scan two rooms, and mark the home surface with a removable
   physical reference. Launch in room A. Unlock at least two distinct shapes in a known order. Capture
   the home room UUID, anchor GUID, layout, ordered IDs, timestamps, and **physical reference photo**.
   Confirm the room and anchor IDs are non-empty real UUIDs. Note app version, date, device, room IDs,
   baseline save copy, and operator. A third distinct shape can test progress unlocked away from home.
3. For each row save a timestamped screen recording/photo and JSONL log excerpt. On device, the app
   writes `shelf-launches.jsonl` and `signspace-save.json` under `Application.persistentDataPath`
   (typically `/sdcard/Android/data/com.signspace.signspace/files/`); verify the actual path in logcat
   and use `adb pull` if device permissions allow. Keep backups private. JSONL `event` distinguishes
   `launch` from `room-transition`; fields include outcome, saved/current room and anchor, schema,
   layout and unlock order. Save file contains timestamps. Log presence is not a visual pass.
4. Inspect restored position **against the physical reference**, object IDs, order and layout; an
   unchanged local transform alone is insufficient. Record any visible displacement and decide pass
   only if the original physical spot and order match, with no misplaced/duplicate objects. Record
   actual offset in cm when measurable. Never use `driftM` as the physical-offset measurement.
5. A failed or ambiguous row stays FAIL/BLOCKED, with evidence and diagnosis. Do not clear app data
   between the five relaunches. A force-stop or remove-from-recents counts as kill; record which.

## Device matrix

| ID | Action | Expected | Result | Evidence / observed offset, IDs, order, notes |
|---|---|---|---|---|
| A0 | First placement + unlock in room A | Stable room+anchor UUID saved; timestamps, schema 3, layout and order present | NOT RUN | — |
| A1 | Kill and relaunch 1 in A | `Restored`; same physical mark, home IDs, ordered objects | NOT RUN | — |
| A2 | Kill and relaunch 2 in A | Same | NOT RUN | — |
| A3 | Kill and relaunch 3 in A | Same | NOT RUN | — |
| A4 | Kill and relaunch 4 in A | Same | NOT RUN | — |
| A5 | Kill and relaunch 5 in A | Same | NOT RUN | — |
| A6 | Reboot headset, launch in A | Same physical mark, home IDs and object order after reboot | NOT RUN | — |
| B1 | Walk to scanned room B with app running | `room-transition` / `DifferentRoom`; temporary plinth; A placement unchanged | NOT RUN | — |
| B2 | Unlock a new shape in B, then inspect save | New timestamp/order persisted; A room, anchor and local transform unchanged | NOT RUN | — |
| B3 | Walk back to A without relaunching | `room-transition` / `Restored`; original mark; original order plus B unlock | NOT RUN | — |
| B4 | Launch in B, then return to A | `DifferentRoom` at launch; original A anchor restored on return | NOT RUN | — |
| C1 | No current room / missing room UUID | `RoomNotFound`; plinth; saved home and unlocks preserved | NOT RUN | — |
| C2 | Room A available but saved anchor unavailable | `AnchorNotFound`; plinth; save unchanged; later retry restores if anchor reappears | NOT RUN | — |
| C3 | Decline or withhold scene permission | Safe fallback; existing save retained; capture boot/permission reason | NOT RUN | — |
| C4 | Unsupported layout or future/corrupt schema on an isolated test copy | No destructive overwrite; unsupported layout uses plinth, corrupt/future file is read-only | NOT RUN | — |
| C5 | Legacy schema 1 and 2 on isolated test copies | IDs/order retained; old timestamps unknown; schema 1 location not trusted | NOT RUN | — |
| C6 | Interrupted save / restart with backup on an isolated test copy | Last committed data recovered or write failure reported; never claim zero loss without testing | NOT RUN | — |

**Gate:** A1–A5 and A6 must pass, as must non-destructive B behaviour and schema storage. B2/B3
exercise the defined global-progress policy. C rows expose degraded-case risks and should be run
before claiming robustness. Fill actual evidence before changing the task's acceptance boxes.

## Non-device checks available now

- `python3 scripts/test_persistence_contract.py`: 4 static source contract checks passed; not a
  substitute for invoking C# or testing MRUK. The four changed C# files also parsed without syntax
  errors using tree-sitter; this is not a Unity compilation or API compatibility test.
- `SIGNSPACE/P4.3 Check persistence`: editor integration check for the actual save code and fake
  room finder; **not run here**. It cannot verify MRUK, OS file replacement, tracking or a reboot.
- Prior P4.2 static wiring/budget checks and P4.1 selection simulator can be rerun separately.
