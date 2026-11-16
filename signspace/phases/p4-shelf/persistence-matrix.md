# P4.3 — persistence test matrix

**Status: desk rows RUN and PASS; every headset row NOT RUN. G4 remains open.**

Unity 6000.3.28f1 was available and ran the editor check and a cross-process relaunch probe on
16 Nov 2026. That proves the save file, schema and room policy across process boundaries; it does
not prove an Android launch, MRUK, a physical anchor or a reboot. The device rows below stay NOT RUN
and must not be inferred from the desk rows.

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

## Desk evidence — 16 Nov 2026 (editor, not a headset)

Commands, all `-batchmode -quit`, each `Verify`/`Leave`/`Return` a **separate Unity process**:

```
Unity -executeMethod Signspace.Editor.PersistenceRelaunch.Init
Unity -executeMethod Signspace.Editor.PersistenceRelaunch.Verify      # ×5, one process each
Unity -executeMethod Signspace.Editor.PersistenceRelaunch.LeaveRoom
Unity -executeMethod Signspace.Editor.PersistenceRelaunch.ReturnRoom
```

| ID | What the probe does | Result | Measured output |
|---|---|---|---|
| desk-A0 | Write placement (room UUID, anchor GUID, anchor-local pos/rot), unlock U, B, C | **PASS** | `schema 3, unlock order U,B,C`; file written to `%TEMP%/signspace-p43-relaunch/save.json` |
| desk-A1–A5 | Load that file in a fresh process, resolve through the real `ResolveForCurrentRoom`, restore | **PASS 5/5** | `restore 1..5, order U,B,C, objects 3, localPos (0.120, -0.030, 0.210)`; `restoreCount` advanced once per process |
| desk-B1 | Launch in a different room UUID | **PASS** | `DifferentRoom`, not on a surface, no anchor claimed, 3 objects kept, home room/anchor unchanged, `restoreCount` not advanced |
| desk-B2 | Unlock `W` away from home, then reload the file | **PASS** | order `U,B,C,W`, 4 timestamps, home room/anchor still `1111…`/`3333…` |
| desk-B4 | Launch back in the home room, fresh process | **PASS** | `Restored`, parented to the home anchor, local pos/rot unchanged, order `U,B,C,W`, 4 objects, no duplicates |
| desk-C4/C5 | Schema 1/2 migration, unknown/newer schema, corrupt JSON | **PASS** | via `SIGNSPACE/P4.3 Check persistence`; order retained, historic timestamps empty, placement untrusted for schema 1, future/corrupt files never overwritten |
| desk-fix | Second restore in one frame | **PASS after fix** | was `6` objects for 3 shapes; now `3`. Cause: deferred `Destroy`; fix: deactivate before destroy, used by restore, slot replacement and adoption |

**These rows are not a device pass.** The finder is fake, the UUIDs are fixed, and no Android process,
MRUK localisation, physical surface or reboot took part. A1–A6 on a headset are still what G4 asks for.

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

## Non-device checks, as actually run

- `python3 scripts/test_persistence_contract.py` — **PASS** (4 static source checks; not C# execution).
- `SIGNSPACE/P4.3 Check persistence` — **PASS** in Unity 6000.3.28f1, 16 Nov 2026. Cannot verify MRUK,
  OS file replacement, tracking or a reboot.
- `PersistenceRelaunch` — **PASS** for Init, Verify ×5, LeaveRoom, ReturnRoom (details above). Each run
  is a separate Unity process, so statics and loaded assemblies cannot carry state between them.
- `python3 scripts/shelf_sim.py`, `scripts/shelf_budget.py`, `scripts/test_shelf_wiring.py` — **PASS**
  (P4.1/P4.2, rerun unchanged by this task).
- APK **v13** built from this tree (`sha256 0445267f…`); no device launch was performed with it.
