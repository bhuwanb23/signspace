# P1.6 — MRUK surface log

**Task:** P1.6 · find a horizontal surface and anchor a cube to it · **Gate fed:** G1 (criterion 7)
**Written:** 15 Nov 2026 · **Status:** 🟡 code complete, **no device readings yet** — the four-condition
table in §5 is empty on purpose and is the part that closes the task

> The task's own acceptance test is "cube sits on real furniture in **≥2 of 4** conditions, and
> time-to-detection measured". Neither number can be produced in this sandbox: there is no headset
> here and no Unity editor, so nothing in this phase has ever been compiled. What this page contains
> is (a) the SDK facts the code is now built on, verified against Meta's reference rather than
> remembered, (b) three bugs found *before* the first reading, each of which would have produced a
> false "MRUK does not work" verdict, and (c) the exact table to fill.

---

## 1. What exists now that did not before

| | Before P1.6 | Now |
|---|---|---|
| MRUK lifecycle | assumed: read `GetCurrentRoom()` and hope | `MrukBoot` — explicit three-state machine (Waiting → Ready / NoRoom / Failed), registers via `RegisterSceneLoadedCallback`, **timestamps the room** |
| Surface query | planes only, valid-handle required, world-axis geometry | planes **and volumes**, handle informational, surface point from the SDK's own `GetClosestSurfacePosition` |
| Diagnosis when nothing is found | `return false` | `LastRejectionSummary` — a sentence naming which filter rejected what |
| Probe | none | `SurfaceProbe` + `Editor/MrukProbeBuilder.cs` (menu **SIGNSPACE/P1.6 Build MRUK probe rig**) |
| Time-to-detection | not measured | two numbers per run: `secondsToRoom`, `secondsToCube` |
| Permission state | assumed granted | `USE_SCENE` read at boot, written into every row, and checked **first** in the failure taxonomy |

## 2. The SDK facts this is built on (verified 15 Nov 2026, MRUK v207)

Every member below was read from Meta's reference before it was used. The first draft of
`MrUkSurfaceFinder.cs` carried four `[VERIFY]` markers because those names were guesses; all four resolved
correctly, but checking them is what exposed the three problems in §3.

| Member | What it is | Why it is load-bearing here |
|---|---|---|
| `MRUK.Instance` | singleton, one per scene | everything starts here; `null` means the prefab is missing, which is a *build* fault, not a user fault |
| `MRUK.GetCurrentRoom()` | current room | **returns `null` if no rooms have loaded yet**, else the room the headset was last in, else the first room in the list. So calling it in `Start()` is guaranteed to be wrong at least once |
| `MRUK.RegisterSceneLoadedCallback(cb)` | scene-loaded hook | *"will be triggered right away if the component is already initialized"* — safe to call at any time, which is why the probe never subscribes directly to `SceneLoadedEvent` |
| `MRUK.LoadSceneFromDevice(bool, bool)` | loads device scene data | `requestSceneCaptureIfNoDataFound: true` starts Space Setup if the device has no scan — the single most useful recovery path for a judge's headset |
| `MRUKRoom.Anchors` | the anchor list | the thing being searched |
| `MRUKRoom.GetFloorAnchor()` | floor anchor | surface height has to be **relative to the floor**, not to the tracking origin: a headset relaunched on a table is 0.75 m above nothing |
| `MRUKAnchor.Label` → `SceneLabels` | flags enum | `TABLE, COUCH, BED, STORAGE, SCREEN, LAMP, PLANT, OTHER, FLOOR, CEILING, …`; tested with `HasAnyLabel(flags)`, not `==` |
| `MRUKAnchor.PlaneRect` (`Rect?`) | planar extent | walls, floors |
| `MRUKAnchor.VolumeBounds` (`Bounds?`) | volumetric extent | **tables and couches** — see §3.1 |
| `MRUKAnchor.GetAnchorCenter()` | volume centre, else anchor position | start point for the probe ray |
| `MRUKAnchor.GetClosestSurfacePosition(p, out pos, out normal)` | nearest point on the anchor's surface | used instead of our own arithmetic — see §3.3 |
| `MRUKAnchor.HasValidHandle` | device-backed? | *"Anchors loaded from JSON or Prefabs do not have a valid handle"* — see §3.2 |

**One deliberate exception: `LoadSceneFromDevice` is called reflectively.** Its return type has moved
between MRUK versions (`Task<LoadDeviceResult>` in the v207 reference, `void` in older docs and
community samples), and nothing here has ever been compiled. A compile error on that one line would
cost the phase its first build; not calling it costs nothing, because the prefab loads the scene on
startup by default and the boot waits for the room either way. So `MrukBoot.TryRequestDeviceLoad`
finds the overload at runtime, polls it if it is a `Task`, and logs which happened. Room identity
(`room.Anchor.Uuid`) is read the same way, so this file depends on no `OVRAnchor` type.

## 2.5 Project prerequisites — true or false before any reading counts

Step 1 of the task is "enable MRUK in the project", and three of the four requirements are Unity
settings that this repository does not track (there is no `ProjectSettings/` in the repo at all, and
nothing here has been opened in an editor). Listing them is the only way they survive:

| # | Requirement | Where | State |
|---|---|---|---|
| 1 | MR Utility Kit package present | — | ✅ **already satisfied by the All-in-One**: the `com.meta.xr.sdk.all` 207 package tree lists `com.meta.xr.mrutilitykit` 207.0.1 as a dependency, two independent package listings confirm it. `Packages/manifest.json` is deliberately **not** edited: adding `com.meta.xr.mrutilitykit` a second time is at best redundant and at worst a version conflict with the bundled one |
| 2 | `OVRManager` → Quest Features → **Scene Support = Required** | the OVRCameraRig / Camera Rig building block | ⬜ owner step; without it MRUK gets no scene data |
| 3 | `OVRManager` → Permission Requests On Startup → tick **Scene** | same | ⬜ owner step |
| 4 | **Meta → Tools → Update AndroidManifest.xml** | menu | ⬜ owner step; this is what writes `com.oculus.permission.USE_SCENE` |
| 5 | Anchor Support enabled on `OVRManager` | same | ⬜ needed for a *persistent* anchor later (P1.7), not for P1.6 detection |
| 6 | an **MRUK** object in the scene, from **Building Blocks > Scene** | scene | ⬜ `MrukProbeBuilder` warns loudly if it is missing rather than faking it |
| 7 | `MRUK.SceneSettings` data source | inspector | ⬜ **headset = Device**, **simulator = Prefab** (a prefab room has no `HasValidHandle`, §3.2). `DeviceWithPrefabFallback` exists and is fine for development but must be recorded as such — a prefab reading is not a device reading |

**A failure mode that would fake the whole table.** A vendor report on managed-device deployments
states that on a **sideloaded** APK, Meta can withhold `ACCESS_MR_SENSORS` / `USE_SCENE` on relaunch
without store-level authorisation, and that spatial features then "silently fail" — appearing to work
on first install and not afterwards. That is a plausible explanation for a condition that passed
yesterday and fails today, so `MrukBoot` now reads the permission at startup and the probe writes
`scenePermission` into every row. Attribution, in order: permission missing → permission fix; room
null with permission granted → space setup or data source; anchors present but rejected → our
thresholds (§6).

## 3. Three bugs found before the first reading

None of these was visible in the code review; all three were found by reading the SDK reference
against the code. Two would have made a working SDK look broken.

### 3.1 Volumes were ignored — every table and couch skipped

The draft accepted only anchors with `PlaneRect`. Meta's anchor reference, on `VolumeBounds`, says
plainly: *"Example of anchors with a volume are a table or a couch."* A TABLE is the most likely shelf
host in the entire product, so the finder was skipping its primary target and would have reported that
MRUK finds no surfaces on exactly the furniture the rubric will be tested on.

Fixed: both geometry types are accepted, and extent comes from whichever exists (still sorted
widest-first, because a 0.6 × 1.2 table and a 1.2 × 0.6 table are the same table).

### 3.2 Prefab rooms were rejected — the simulator would report zero surfaces

The draft required `anchor.HasValidHandle`. The same reference: *"An anchor will have a valid handle
if it was loaded from device and there is a system-level anchor backing it. Anchors loaded from JSON
or Prefabs do not have a valid handle."*

That makes the check a device-detector, not a validity check — and since the Meta XR Simulator has no
room to scan, **every** simulator run (i.e. every run before the headset arrives) would have shown
"no surfaces found". Fixed: the handle is recorded as `FromDevice` and reported in the log, never used
to filter.

### 3.3 Geometry assumed the anchor's local axes are world-aligned

`bounds.center + up * bounds.size.y / 2` is only the tabletop when local **+Y** is up. Nothing in the
reference promises that for a volume anchor. Fixed: probe from a point above `GetAnchorCenter()` and
ask the anchor for its closest surface plus the normal there, then require `dot(normal, up) > 0.9`.
Orientation-agnostic, and it returns the top face rather than whichever face happened to be nearest.

### 3.4 And one that is not a bug but decides the phase

`GetCurrentRoom()` returning `null` before load is documented, which is why `MrukBoot` never reads it
in `Start()`. The probe's first query happens only after the boot state leaves `Waiting` **or** the
timeout expires — and on timeout it records a failure *with a reason*, because a missing surface is
data (trap b), not an exception.

## 4. What "success" means here, stated before any run

Fixed now so it cannot be reinterpreted later:

- **A condition passes** when a cube of 10 cm appears sitting on (not in) the furniture with its blue
  orientation nose visible, placed along the surface **normal** at +2 cm, **and** the operator rates
  the placement **3** (exactly on the surface) — a rating of 2 is recorded as a *partial* and does not
  count toward the ≥2.
- **Flush placement is deliberately wrong.** A cube embedded in a tabletop z-fights, and z-fighting
  looks exactly like a failed placement on video. +2 cm costs nothing and removes the ambiguity.
- **The gate needs 2 of 4.** Cluttered table and bed are the two that are expected to be hard, so if
  only two pass they must include the clean desk — a clean desk failing means the pipeline is broken,
  not that the room was awkward.
- **A failure is only a failure if it is attributable.** Each failure gets one of the six causes in §6
  plus the rejection sentence from `LastRejectionSummary`; "did not work" is not a row.

## 5. The four conditions — sheet to fill

One **launch per condition**. The condition is chosen by pinching 1–4 times within the first 6 s
(1 = clean desk, 2 = lap, 3 = bed, 4 = cluttered table), and cannot be changed afterwards: a reading
taken after a mid-session change is not a cold start, and cold start is the number the phase requires.

| # | Condition | Environment | Data source | `secondsToRoom` | `secondsToCube` | Surface found | Label | Height (m) | Extent (m) | Best score | Operator rating 1–3 | Verdict | Failure cause (§6) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Clean desk | | device / simulator | | | | | | | | | | |
| 2 | Lap | | device / simulator | | | | | | | | | | |
| 3 | Bed | | device / simulator | | | | | | | | | | |
| 4 | Cluttered table | | device / simulator | | | | | | | | | | |

**Predictions, written before the data** (so a surprise is visible as a surprise):

| Condition | Prediction | Reason |
|---|---|---|
| Clean desk | **Pass** — this is MRUK's home case | a `TABLE` volume, flat, well inside 1 m, at 0.75 m ± 0.35 |
| Lap | **Fail, by design** — a lap is not a scanned anchor | MRUK reports room geometry, not a person's thighs; expecting it to would be a category† error. The *product* answers a lap with the plinth, and that is the point of trap b |
| Bed | **Uncertain** — `BED` is a real label, `VolumeBounds` in metres, but a mattress top is ~0.55 m and often beyond 1 m from where someone sits | may pass on height alone; `heightTolerance 0.35` puts the acceptable band at 0.40–1.10 m, so a mattress is inside it |
| Cluttered table | **Likely pass, with a caveat** — clutter does not shrink `VolumeBounds` | the cube may land on a book that MRUK never scanned, i.e. a *correct* placement that *looks* wrong. Record it as such rather than as an SDK failure |

† "category error" is the accurate description: expected failure of a condition the SDK never claimed.

## 6. Failure taxonomy — seven causes, all already implemented

`SurfaceProbe.ExplainNoSurface()` emits exactly these sentences, so a log row is never just `false`:

| Cause | Message shape | What it means | What is done |
|---|---|---|---|
| Permission withheld | *"USE_SCENE not granted, so MRUK cannot read the room: …"* | the manifest setting or the runtime grant is missing | owner step in §2.5; **checked before every other cause**, because it imitates all of them |
| Boot failed | *"MRUK failed to load: …"* | build/package problem | fix the build; not a data point |
| No room | *"no room: the device has no space setup for this room"* | the headset has never scanned this room, or the room was deleted | the user runs Space Setup; the product shows the plinth meanwhile |
| Room, zero anchors | *"room has zero anchors"* | scan exists but carries no furniture | recorded; plinth |
| No geometry | *"N anchors (0 with geometry)"* | anchors exist that carry neither `PlaneRect` nor `VolumeBounds` | recorded as an SDK/scan oddity |
| Filtered out | *"… but none passed the filter: anchors 7 · horizontal 3 · wrong label 1 · too small 1 · too far 0 · height out of band 1 · usable 0"* | MRUK found furniture; **our** thresholds rejected it | a product decision, and the tunables (`maxDistance`, `preferredHeight`, `heightTolerance`, `minExtent`) are the fix |
| Placed, but wrong-looking | cube visible on a surface the user did not mean | often correct: clutter is not modelled | recorded as a placement finding, not a failure |

The distinction that matters: rows 1–3 are MRUK's, row 5 is **ours**. Conflating them is how a
threshold gets "fixed" by blaming the platform.

## 7. Time-to-detection — the trap, and what the launch flow must do with the number

Trap (a) is "assuming detection is instant — it is not, and launch flow must account for it". So the
phase records two numbers from the same clock, started in `Awake()`:

```
secondsToRoom  = first room with ≥1 anchor
secondsToCube  = cube actually on furniture and on screen       (always ≥ secondsToRoom)
```

`secondsToCube` is the honest one for the launch flow: it includes the query, the filter and the
spawn. The decision rule is fixed now, before the number exists:

| Measured `secondsToCube` | Launch flow |
|---|---|
| **< 1.0 s** | open straight into the shelf; no loading state at all |
| **1.0 – 3.0 s** | a single "finding your table…" line, non-blocking; the shelf's own frame is already drawn |
| **3.0 – 8.0 s** | open in **plinth mode immediately** and offer the surface when it arrives — `ShelfAnchor` rule 3 already forbids auto-migrating, so this needs no new behaviour |
| **> 8.0 s, or never** | the shelf is a plinth-first product; MRUK becomes an enhancement. Written into the submission as a limitation, not hidden |

**No published figure exists for MRUK device load latency.** Meta documents the *mechanism*
(`LoadSceneFromDevice`, the scene-loaded event, the Script Execution List that orders your components
after it) and not a duration. Anything quoted as a duration here would be invented, so the table above
is a decision rule, not a prediction, and stays empty until measured.

**One launch-flow consequence already committed:** the boot never blocks. `MrukBoot` publishes a state
and returns; `ShelfAnchor` runs with `TryGetPrimary` returning `false` and uses its plinth. A judge
whose headset has no scan sees a working product, not a spinner.

## 8. Verdict against the gate

| Gate G1 criterion | Needs | Status at time of writing |
|---|---|---|
| 7 — MRUK anchors in **≥2 of 4** conditions | 2 passing rows in §5 | 🟡 **unmeasured** — code ready, readings pending |

If fewer than 2 pass, the same-day responses, in order: (1) re-tune `preferredHeight` /
`heightTolerance` / `maxDistance` from the recorded rejection sentences, since those are the only
values between MRUK and the shelf; (2) fall back to the SDK's `MRUKRoom.FindLargestSurface(TABLE)` /
`TryGetClosestSurfacePosition` rather than our own scoring loop; (3) if a room is being scanned but no
furniture anchor exists at all, the ratable claim becomes "MRUK anchors the shelf where the room has
furniture, and uses the plinth otherwise" — true, weaker, and stated.

## 9. How to run it

```
0. Owner steps in §2.5 first: Scene Support = Required, Scene permission ticked,
   Meta > Tools > Update AndroidManifest.xml, an MRUK object from Building Blocks > Scene.
   A reading taken without these measures measures the configuration, not MRUK.
1. Scene: SIGNSPACE/P1.6 Build MRUK probe rig
2. MRUK data source: headset = Device · simulator = Prefab (a room with a table)
3. Build & Run. Pinch 1–3 times in the first 6 s to choose the condition.
4. Confirm the cube by eye, then pinch 1–3 to rate it (3 = exactly on the surface).
5. Pull the numbers:
     adb shell run-as com.signspace.signspace cat \
       /sdcard/Android/data/com.signspace.signspace/files/mruk-probe.jsonl
     adb shell run-as com.signspace.signspace cat \
       /sdcard/Android/data/com.signspace.signspace/files/mruk-clean-desk-summary.txt
6. Relaunch for the next condition. Never reuse a session.
```

`mruk-probe.jsonl` appends one row per run with the condition, boot state, `scenePermission`, both
timings, the filter
sentence, every candidate (label, height, extent, score, `fromDevice`) and the operator rating, so the
table in §5 can be filled from data instead of memory.

## 10. Change log

| Date | Change | Why |
|---|---|---|
| 15 Nov 2026 | `SurfaceFinder` rewritten against verified v207 API | four members were guesses; verifying them exposed §3.1–3.3 |
| 15 Nov 2026 | volume anchors accepted | a table is a volume; the finder was skipping its main target |
| 15 Nov 2026 | `HasValidHandle` demoted from filter to field | it is a device-detector, so it made prefab/simulator rooms invisible |
| 15 Nov 2026 | surface point from `GetClosestSurfacePosition` | local axes are not promised to be world-aligned |
| 15 Nov 2026 | `MrukBoot` added | `GetCurrentRoom()` is `null` before load; detection is not instant (trap a) |
| 15 Nov 2026 | floor-relative heights | a headset relaunched on a different surface has a different origin |
| 15 Nov 2026 | `LastRejectionSummary` added | "found nothing" is not a diagnosis, and §6 needs a cause per row |
| 15 Nov 2026 | `USE_SCENE` read at boot and written into every row | a vendor report says sideloaded builds can lose scene access between launches; without this the failure looks like bad surface detection |
| 15 Nov 2026 | manifest **not** changed | the All-in-One 207 tree already contains `com.meta.xr.mrutilitykit` 207.0.1; a second entry risks a conflict |
| 15 Nov 2026 | `LoadSceneFromDevice` and `room.Anchor.Uuid` bound reflectively | their signatures differ across MRUK versions and none of this has been compiled; a missing call is survivable, a build-breaking one is not |
