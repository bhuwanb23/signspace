# P1.7 — Persistence log

**Task:** P1.7 · the cube is still there after a relaunch · **Gate fed:** G1 (criterion 8)
**Written:** 15 Nov 2026 · **Status:** 🟡 code complete, **no readings yet** — §6 is the part that
closes the task and it needs the headset

> The acceptance test is "cube restores after a kill/relaunch" and "after a headset reboot". Neither
> can be produced here: the code compiles (16 Nov, 0 errors) but there is no headset, and a
> relaunch test cannot be simulated by reading code. What this page fixes is the *design* — what is
> saved, why in that form, what happens when the room is gone — so that the three readings in §6
> either pass or point at a specific mechanism.

---

## 1. The decision the whole task turns on: what identity is saved

The trap in the task is *"saving a world position instead of an anchored one — it survives the app
but not the room"*. Avoiding it needs an identity that survives a relaunch, and there was a real
choice of three:

| Candidate key | Survives the app? | Survives the room being reconstructed? | Verdict |
|---|---|---|---|
| `transform.position` (world) | yes | **no** — the tracking origin is not preserved between sessions | ✗ the trap |
| `MRUKAnchor.name` (`TABLE_1`, `TABLE_2`, …) | probably | probably not — it is generated at load time and nothing promises the numbering | ✗ fallback only |
| **`OVRAnchor.Uuid`** | yes | yes | ✓ **the key** |

Meta's own reference, on `OVRAnchor`: it *"can represent either a 'spatial anchor', which is created
and managed by the app, or a 'scene anchor', which is created and managed by the system and used to
represent the Scene Model"* — and on `OVRAnchor.Uuid`: *"The unique identifier of this anchor. UUIDs
persist across sessions. If you load a persisted anchor, you can use the UUID to identify it."*
`MRUKAnchor.Anchor` is exactly that `OVRAnchor`, so a scanned table has a UUID that outlives both the
app and the headset process.

`SurfaceFinder` therefore sets `SurfaceCandidate.AnchorId` to the UUID when the platform provides one
and only falls back to the object name when it cannot (prefab rooms). `MrUkSurfaceFinder.TryResolve`
searches **every room**, not just the current one — somebody who saved a shelf in the kitchen and
launches in the living room still gets their kitchen shelf back the moment the headset localises the
kitchen. A UUID match scores 100 and a name match 10, so a name match is visibly second-best in the
log.

## 2. The second decision: the offset is stored in the anchor's frame

Saving the identity gets the anchor back. It does not say *where on the table* the shelf sat, and the
answer cannot be a world position either — the origin moved. So what is written to disk is the shelf
transform expressed **relative to the anchor transform** (`localPos`, `localRot`), and the shelf is
**parented to the anchor** at runtime.

Parenting is not tidiness. It is the mechanism that makes a reboot survivable:

- the room is reconstructed in a new tracking space, so `TABLE_1`'s world position in this session is
  not its world position in the last one;
- the shelf is a child of that anchor, so it inherits the offset in the anchor's own frame and lands
  on the same square of real wood regardless;
- if MRUK's world lock adjusts the tracking space, the child follows for free.

**And the claim is measurable rather than asserted.** `SaveData.lastWorldPos` records the world
position too, and on restore the difference is logged as `worldDriftM`:

```
worldDriftM = |restored world position  −  world position recorded last session|
```

That number *is* the tracking-origin shift, i.e. **the error a world-position save would have
produced**. `lastWorldPos` is never read to place anything; it exists so the log can say "the origin
moved 0.31 m between these two launches, and the shelf was still on the table" instead of claiming it.

## 3. Why not `OVRSpatialAnchor.SaveAsync`

The other honest option is to create our own persistent spatial anchor at the shelf's position, save
it with `SaveAnchorAsync`, and reload it by UUID next session. That is the documented pattern for
free-floating MR content, and it was rejected for four reasons:

| | MRUK scene anchor (chosen) | `OVRSpatialAnchor` (rejected) |
|---|---|---|
| Save step | **none** — the room already persists, so the anchor comes back on its own | an async save that can fail and must be handled |
| Localisation | done by MRUK while loading the room | must be awaited separately; content is unplaced until it localises |
| Extra configuration | none beyond P1.6 | *Anchor And Space Sharing Support* on `OVRManager` |
| Failure mode | a rescanned room changes anchor UUIDs → shelf lost, plinth shown | the anchor survives a rescan but now floats where the table used to be |

The last row decided it. The shelf's whole premise is *"the shelf belongs to a surface, not to a
person"* — a saved spatial anchor would happily restore a shelf into thin air after the furniture
moved, which is a worse outcome than losing it. The cost we accept: **rescanning the room loses the
shelf**. That is documented behaviour, not a bug, and it is what §7's `AnchorNotFound` row says.

## 4. Save file, schema 2

`Application.persistentDataPath/signspace-save.json` (`SaveSystem.Path` is public so the log can name
it). Version 1 held an anchor id and no transform.

| Field | Meaning |
|---|---|
| `schema` | **2** since P1.7 |
| `anchorId` | the surface's `OVRAnchor.Uuid` |
| `anchorLabel` | `TABLE` / `COUCH` / `BED` / `STORAGE`, for the log and the UI |
| `roomId` | the room's UUID (§3 of `mruk-log.md`) |
| `localPos`, `localRot` | the shelf transform **in the anchor's frame** |
| `lastWorldPos` | measurement only, never an input |
| `savedUtc` | so a reading can state how long the app was gone |
| `restoreCount` | how many times this anchor has been re-resolved; written immediately, because the process may be killed next |
| `unlocked`, `calibrated`, `lastShapeId` | progress, unchanged from v1 |

**Migration rule, fixed now:** a `schema` < 2 file is *read* (progress is kept) but its placement is
**not trusted**, because it contains an anchor id and no offset. The shelf re-places itself from the
live surface and writes a v2 record. This is the same strictness P1.4 chose for pose versions: a file
that cannot mean what the code needs it to mean is not silently reinterpreted.

## 5. What the code does on launch, in order

1. `Awake` — `ShelfAnchor` starts, `SaveSystem` loads the file.
2. No usable placement record → wait for the room (via `MrukBoot.OnRoomReady`), place fresh, **save**.
   This is launch 1 of the protocol and it is what creates the record.
3. A usable record exists → wait for the room, then `TryResolve(anchorId)`:
   - **resolved** → `SetParent(anchorTransform)`, apply `localPos`/`localRot`, record `worldDriftM`,
     `NoteRestored()`, restore the unlocked objects, `OnSurface = true`. No plinth is ever shown in
     between: the shelf appears once, in the right place.
   - **not resolved** → the save is **kept**, the plinth is shown, the reason is logged (§7). A later
     launch in the right room restores normally.

Timing is recorded from `Awake`, so `restoreMs` includes the P1.6 time-to-detection. That is
deliberate: the number a launch flow has to cover is "app opened → shelf visible in place", not
"anchor list scanned → transform set".

## 6. The three readings that close the task

One row per launch in `persistence-probe.jsonl`. Kit: `SIGNSPACE/P1.7 Build persistence rig`.

| Launch | What is done | Expected `outcome` | A pass looks like |
|---|---|---|---|
| 1 | fresh install / cleared save | `PlacedFresh` | cube lands on the table; save file now holds a UUID |
| 2 | **kill from the recents menu**, relaunch | `Restored` | cube on **the same physical square**; rating 3 |
| 3 | **reboot the headset**, relaunch | `Restored` | same square again; rating 3 |

Fill this in on the device:

| Run | `outcome` | `resolveMs` | `restoreMs` | `worldDriftM` | `secondsSinceSave` | `restoreCount` | operator rating 1–3 | verdict |
|---|---|---|---|---|---|---|---|---|
| 1 · first run | | | | | | | | |
| 2 · kill/relaunch | | | | | | | | |
| 3 · after reboot | | | | | | | | |
| 4 · wrong room (optional) | | | | | | | | |

**Predictions, written before the data:**

| Claim | Prediction | Reasoning |
|---|---|---|
| `resolveMs` | **single-digit to low tens of ms** | it is a linear scan of the anchor list (tens of anchors, a string compare each); no I/O, no localisation — MRUK already did that by the time the room is ready |
| `restoreMs` | **dominated by `secondsToRoom` from P1.6**, so seconds not milliseconds | the wait for the room is the launch flow's real cost, and P1.6 measures it separately |
| `worldDriftM` | **non-zero, and usually > 1 cm** | this is the point of the whole design: if it is 0.000 the tracking origin happened to be preserved and the test cannot distinguish anchor-relative from world placement — in that case say so rather than claiming the trap was escaped |
| rating after reboot | **3** if `worldDriftM` > 0; anything less means the anchor-relative maths is wrong, not that the origin moved | the two numbers must agree; when they disagree, the disagreement is the finding |
| launch 4 (wrong room) | `AnchorNotFound`, plinth, **save intact** | §7 |

## 7. What happens when the room cannot be re-found — the documented behaviour

This is written as behaviour, not as an error path, because it is the normal case for a product
demoed in a stranger's house.

| Cause | `outcome` | What the user sees | What happens to the save |
|---|---|---|---|
| No room loaded after `restoreWaitSeconds` (20 s) | `AnchorNotFound` | the plinth, in the same relative position — no message | **kept**, retried next launch |
| The room is a *different* room than the one saved | `AnchorNotFound` | plinth | **kept** — walking between rooms must not delete a week of progress |
| The room was rescanned, so anchor UUIDs changed | `AnchorNotFound` | plinth | **kept**, but it will never resolve: a rescan is a new room as far as anchors are concerned (§3, accepted cost) |
| `USE_SCENE` withheld | `AnchorNotFound` with the permission in the detail | plinth | kept; the failure is named, not guessed |
| Save file unreadable/corrupt | `PlacedFresh` | fresh shelf | replaced; a corrupt save must never stop the app |
| `schema` < 2 | `SaveTooOld` → `PlacedFresh` | fresh shelf, progress kept | rewritten as v2 |

**The single rule:** a shelf that cannot find its anchor *keeps the anchor it was looking for*. Never
clear, never migrate to "whatever table is nearest". Rule 3 of `ShelfAnchor` already forbids applying
a surface the user has not accepted; this is the same principle one level down.

## 8. How to run it

```
1. Scene: SIGNSPACE/P1.7 Build persistence rig   (reuses the P1.6 boot + finder if present)
2. Prerequisites from mruk-log.md §2.5 — Scene Support = Required, Scene permission, manifest updated
3. Clean start: SIGNSPACE/P1.7 Clear save, or set clearSaveOnStart = true for one launch
4. Launch: cube lands on real furniture. This is row 1.
5. Kill from the recents menu (do NOT force-stop from settings — that is a different lifecycle).
6. Relaunch within a minute. Pinch 1-3 in the rating window: 3 = same spot, 2 = close, 1 = wrong.
7. Reboot the headset, relaunch, repeat.
8. Pull the rows:
     adb shell run-as com.signspace.signspace cat \
       /sdcard/Android/data/com.signspace.signspace/files/persistence-probe.jsonl
     adb shell run-as com.signspace.signspace cat \
       /sdcard/Android/data/com.signspace.signspace/files/persistence-summary.txt
```

Row 2 must be a **kill from recents**, not a recompile: a rebuild can change the app's install time
and Unity's own state, which makes it a different experiment from the one the task asks for.

## 9. Verdict against the gate

| Gate G1 criterion | Needs | Status |
|---|---|---|
| 8 — anchor survives relaunch **and** reboot | rows 2 and 3 both `Restored`, rated 3 | 🟡 **unmeasured** — rig ready |

If row 2 passes and row 3 fails, the finding is specific and worth writing down: relaunch survival is
a save/load question, reboot survival is a *localisation* question, and only the second one depends on
MRUK's world lock and the runtime re-localising the room. The response in that case is to turn on
`MRUK.Instance.EnableWorldLock` and re-measure before touching any of the persistence code.

## 10. Change log

| Date | Change | Why |
|---|---|---|
| 15 Nov 2026 | anchor identity = `OVRAnchor.Uuid`, name demoted to fallback | a generated object name is not promised to survive a load |
| 15 Nov 2026 | save stores `localPos`/`localRot` in the anchor's frame; shelf parented to the anchor | a world position returns offset by however far the origin moved |
| 15 Nov 2026 | `lastWorldPos` added as measurement only | turns "anchors beat world positions" into a number (`worldDriftM`) |
| 15 Nov 2026 | save schema 2, v1 read but not trusted | v1 has an id and no offset; reinterpreting it would place content wrongly |
| 15 Nov 2026 | `AnchorNotFound` keeps the save and shows the plinth | walking into another room must not delete progress |
| 15 Nov 2026 | `TryResolve` searches all rooms | the saved room need not be the current one |
| 15 Nov 2026 | restore waits on `MrukBoot.OnRoomReady`, no polling | P1.6 already owns that state machine; two consumers would race |
| 15 Nov 2026 | `NoteRestored()` saves immediately | the process may be killed before `OnApplicationQuit` |
