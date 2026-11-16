# P1 READ-MAP — what the eight sub-phases actually require

**Written:** 15 Nov 2026 · **Purpose:** a read-through of P1 against the actual repository, before any code is written.
**Sources read:** `phases/p1-derisk/` (README, gate, all 8 task files) · `docs/02-tech/TECH-STACK.md` §4–6 · `docs/02-tech/POSE-FORMAT.md` §1–3 · `docs/03-content/TRACKABILITY.md` §2 · `docs/03-content/SIGNSET.md` §7 · every C# file under `unity/Assets/_Project/`.

---

## 1. The one-paragraph truth about P1

P1 reads like a day of *testing*. Against this repository it is mostly a day of **wiring that does not exist yet**, plus a day of measuring that only a headset can do. The pure logic layer — the matcher, the pose math, the pose files, the save schema, the shelf-anchor logic — is already written and reviewed. What is missing is every line of glue between that logic and Meta's SDK, because until now nothing needed a device. Two greps make the size of the gap concrete:

| Grep | Result | What it means |
|---|---|---|
| Implementations of `SkeletonPoseSource.IHandStateProvider` | **zero** — only the interface and three tooltips mention it | Pinch state has no source. `ReferenceHand.IsPinching()` returns `false` when the field is unassigned, so **as wired, grab can never fire** |
| Callers of `SkeletonPoseSource.AutoBind(…)` | **zero** | Nothing binds a tracked hand skeleton. The matcher currently receives an empty joint array |

Neither is a surprise — both are P1.1/P1.2 by construction — but it changes the shape of the day: **P1 is 4 units of glue, 3 units of verification, 1 unit of measurement**, and the glue has to exist before the headset can tell us anything.

## 2. Build order (the task-file order is nearly right, with one split)

```
P1.1 hand visual ──┬──► P1.2 grab ──► P1.3 wrist rotate ──► [G1 half 1: the core gesture]
                   │
                   ├──► P1.4 pose from JSON ──► P1.5 20-shape probe ──► [G1 half 2: ≥8 of 20]
                   │
                   └──► P1.8 perf baseline

P1.6 MRUK surface ──► P1.7 persistence ──► [G1 half 3: anchor survives]
```

**Why the split matters:** the three branches are independent after P1.1. The document lists them sequentially as one 8-hour day, which assumes the device is present throughout. If the headset lands late, the order that survives is **glue first (1.1–1.4), then the device-dependent three (1.5, 1.8, and the verification halves of 1.2/1.3/1.6/1.7)** — because glue is the only part that can be written without the device and is the only part that *blocks everything else*.

## 3. Sub-phase by sub-phase, against the repo

| | Task asks for | Already exists | Missing | Needs device |
|---|---|---|---|---|
| **P1.1** | custom hand visual at frame rate | `Hands/HandVisual.cs` — **procedural** hand, 25 capsules, ~8k tris, self-documented at ~50 of the 120 allowed draw calls | SDK→joint binding (`AutoBind` has no caller); confidence readout; measured triangle/draw-call count | **yes** — frame time |
| **P1.2** | grab a floating object, hands only | `ReferenceHand` grab/release state machine, `IHandStateProvider` interface, `grab-log.md` named | **the provider implementation itself**; miss-rate logging; a scene with no ray fallback (none exists to disable) | yes — 20/20 trials |
| **P1.3** | three rotation variants, rated 1–5 at 0.4 m and 0.6 m | `ReferenceHand.FollowWrist()` = damped 1:1 (variant **b**) with `followSmoothness`, `distance` default 0.40 m | variant **a** as a selectable mode; variant **c** (hold-still) entirely; in-headset 1–5 rating mechanism | **yes** — this is the make-or-break judgement |
| **P1.4** | two poses from JSON, load + apply time | six pose files already authored (B, C, O, U, V, W) via `make_pose.py`; `HandPose.FromJson`; `SignSetLoader` (UnityWebRequest, Android-correct); `ReferenceHand.ShowPose` | fidelity check on device; load/apply timing; schema `version` already present | partly — timing |
| **P1.5** | 20 shapes × 2 lighting, scored | procedure in `TRACKABILITY.md` §2; `Matcher` already emits `OnBandChanged`, `OnLocked`, `OnDistanceUpdated` | **the probe harness** — no scene, no logger, no per-shape timer; and the doc's method is manual scoring | **yes** — the entire point |
| **P1.6** | MRUK finds a horizontal surface, anchors a cube | `Spatial/SurfaceFinder.cs` (MRUK, 4 `[VERIFY]` call sites) + `ShelfAnchor.cs`, written at the time of this reading but **now compiled** (16 Nov 2026, 0 errors — see [`../P0-P2-VALIDATION.md`](../P0-P2-VALIDATION.md)) | MRUK initialisation in the scene; passthrough; the four room conditions | yes — detection in 2 of 4 |
| **P1.7** | survives relaunch and reboot | `SaveSystem` with `roomId`/`anchorId`, `BindToAnchor`, `MatchesAnchor`, save on quit and pause | real anchor GUID plumbing from MRUK → save; re-resolution on launch | **yes** |
| **P1.8** | fps + 1% low + thermal onset on the 3S | the budget table (`TECH-STACK.md` §4: 13.9 ms, ≤120 draw calls, ≤150k tris, 0 realtime lights, shadows off) | OVR Metrics Tool session; the measurement itself | **yes, and on the weaker device specifically** |

## 4. Defects found by reading, not by testing

Five things that will bite whoever starts P1.1 literally, in order of severity:

1. **Grab is dead as wired.** `ReferenceHand.IsPinching()` falls through to `return false` when `handStateProvider` is unassigned, and `SceneBuilder.cs` never assigns it. A day spent tuning pinch thresholds would be a day spent tuning a signal with no source.
2. **The matcher is being fed nothing.** `AutoBind` has no caller. `SkeletonPoseSource` will sit with an empty `joints` array and `JointsFullyMapped = false` on device. The debug overlay would show this immediately; nothing else would.
3. **P1.3 needs three variants and the code has one.** `followSmoothness` can *approximate* variant (a) at a large value, but there is no mode switch and no variant (c). Rating three variants requires a rating UI that no task file specifies — the honest reading is that P1.3 also needs a way to record 1–5 while wearing a headset, or the scores become a memory test.
4. **P1.4's own text contradicts the project's deliberate design.** It says "import a low-poly hand mesh skinned to 26 joints"; `HandVisual.cs` opens with a *"WHY PROCEDURAL"* rationale for generating the hand from the same canonical joints the pose data uses. Following P1.4 literally would add a binary asset, a 20-vs-26-bone remap, and a rig the authored poses were never built against. The same file names `PoseProvider.cs`, which does not exist — the real loader is `SignSetLoader` + `HandPose.FromJson` + `ReferenceHand.ShowPose`. **P1.4's remaining work is fidelity and timing, not creation.**
5. **P1.5's method is manual where it could be automatic.** `Matcher` already emits the exact signal the probe needs. A hand-scored "High/Medium/Low" with no log behind it is a rumour with a number attached — the phase's own trap list says *"an unwritten verdict is a rumour by Day 4"*. The probe should log lock rate per shape per condition and use hand-scoring only as the cross-check.

## 5. The four things only the device can decide

Everything else in P1 is a means to these:

| # | Question | Decided by | Consequence if bad |
|---|---|---|---|
| 1 | Does wrist-rotation of a floating hand feel natural at 0.4 m and 0.6 m? | P1.3 | **Re-opens the concept** the same day (gate's red branch) |
| 2 | Do ≥8 of 20 handshapes track in normal light? | P1.5 | Scope cut to 6 shapes / 1 family — allowed, planned, not fatal |
| 3 | Does MRUK find a surface in ≥2 of 4 room conditions? | P1.6 | Plinth becomes the permanent design; pitch loses "your room" |
| 4 | Is 72 fps sustainable on the 3S with both hands + passthrough? | P1.8 | 60 fps floor and a disclosure line |

Note what is **not** on that list: nothing about the matcher, the pose files, the save format, or the shelf logic. Those are already decided; P1 merely confirms them.

## 6. Simulator versus headset — where the line falls

The Meta XR Simulator 207.1 runs the Quest 3 and VR Glasses profiles and simulates hand tracking, so glue can be written and *seen* without hardware. It cannot answer any of §5: no thermal behaviour, no real room mesh, no real sensor noise, no real occlusion. **Every G1 criterion that counts is a hardware measurement.** That is why the headset's arrival day is the single scheduling fact that matters, and why the glue should be finished before it lands rather than during.

## 7. Questions to settle before the first line of P1 code

Grouped so they can be answered in one pass:

**Scope and correctness**
1. P1.4 says "import a mesh"; the repo says procedural. Confirm procedural wins and P1.4 is re-scoped to fidelity + timing?
2. Should P1.3's three variants be runtime-selectable in one build (so one headset session can rate all three), rather than three builds?
3. Is a 1–5 rating captured by voice note acceptable, or does it need an in-headset button?

**Measurement**
4. Does the P1.5 harness log lock rate automatically from `Matcher` events, with hand-scoring as cross-check?
5. Does the same harness also count P1.2's 20 grab trials, so one build answers both?
6. What exactly counts as "tracked" for the 10-second hold — a lock at any point, or continuous band ≥ Close?

**Sequencing**
7. If the headset lands after Day 2, does P1.5 move to Day 3 and P2 start on simulator-only work, or does P1 hold?
8. Is P1.8 run once with an empty-ish scene (budget floor) or after the shelf exists (realistic)?

**Documentation hygiene found while reading**
9. `phases/p1-derisk/README.md` still says **"Day 2 (Sun 8 Nov)"** — the same stale calendar that was rolled forward in `MASTER-PLAN.md` §11. It should read Mon 16 Nov.
10. P1's input list still requires "a 26-joint hand mesh", which contradicts decision (1).
11. `docs/06-submission/CHECKLIST.md` and the P1 artefacts name four log files (`grab-log.md`, `rotation-verdict.md`, `probe-raw.md`, `mruk-log.md`, `persistence-log.md`, `perf-baseline.md`) — none exist yet. Six files to be created by this phase.

---

*Read-only. No code or task file was modified in producing this page.*
