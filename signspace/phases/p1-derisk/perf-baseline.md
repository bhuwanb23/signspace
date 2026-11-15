# P1.8 — Frame budget baseline

**Task:** P1.8 · measure the baseline on Quest 3S · **Gate fed:** G1 (criterion 9) and, indirectly, G7
**Written:** 15 Nov 2026 · **Status:** 🟡 sampler built, **no device run yet**

> Forty percent of the remaining plan can be wrecked by a performance surprise on Day 8, which is why
> this is measured now rather than in P7. There is no headset in this environment and nothing here has
> been compiled, so this page fixes the method, the definitions and the pass rule. §5 is the table
> that closes the task.

---

## 1. The two traps, and how the code refuses them

| Trap | Why it is tempting | How the rig makes it impossible to do by accident |
|---|---|---|
| Measuring in the editor or the simulator | it is where the project opens | The summary stamps `deviceModel`, `graphicsDeviceName`, `graphicsDeviceType` and `build: player / EDITOR — NOT ADMISSABLE`. An editor number is written down as inadmissible rather than quietly compared against 72 |
| Measuring 60 s and calling it thermal-stable | 60 s always looks fine | `requiredSeconds = 600`. A run shorter than 10 minutes is written with verdict **`INVALID`** and the words "this file is evidence of a method, not a baseline" — the probe will not produce a pass for a short run |

Both are enforced in `Scripts/Perf/PerfProbe.cs`, not in this page, because a rule that lives only in
prose gets broken at 2 a.m.

## 2. The run length — a conflict in the plan, resolved explicitly

Three documents disagree slightly, and pretending otherwise would make the number unquotable:

| Source | Says |
|---|---|
| P1.8 task | "run for 10 minutes" |
| Gate G1, criterion 9 | "baseline fps on **3S** with **≥10 min** thermal" |
| `docs/02-tech/PERFORMANCE.md` §2 | "**15 minutes** continuously", because `RISKS.md` R10's trigger is *"<72 fps at 10 minutes"* |

**Resolution: run 15 minutes and refuse to certify anything under 10.** That satisfies all three,
exceeds the gate, and matches the method the submission actually quotes. `PerfProbe.durationSeconds`
defaults to 900 s and `requiredSeconds` to 600 s, so the strict one is the one enforced.

## 3. What is being measured, and why each element is in the loop

P1.8 asks for "both hands tracked, one anchored object, and passthrough on". That is not a minimal
scene, it is the **product's** scene, and each part is included because it is a plausible source of the
surprise this task exists to find:

| In the loop | Why it costs what it costs |
|---|---|
| Both hands, 26 joints each | two skinned skeletons plus the matcher's per-frame distance computation |
| One anchored object (the P1.7 shelf + cube) | the MRUK surface query and the anchor parenting — and the thing a judge will actually look at |
| Passthrough | compositor work; it does not appear in app GPU time, which is *exactly* why a baseline without it lies |
| Nothing else | the trial rigs are disabled by the builder: a logger writing jsonl mid-run measures the harness |

## 4. Definitions, fixed before the data arrives

Frame interval is measured per frame from `unscaledDeltaTime`. With vsync that is the cadence the user
sees: a frame the compositor dropped shows up as **two** intervals, which is the honest way to count it.
The numbers this page will quote:

| Term | Definition |
|---|---|
| **average fps** | frames ÷ elapsed, over the run **after a 20 s warmup** |
| **1% low** | sort frame intervals, take the worst 1% of frames, report `1000 / mean(those)`. The number that decides whether the app feels bad |
| p50 / p95 / p99 | the interval percentiles, so a bimodal run is visible rather than averaged away |
| over budget | frames whose interval exceeded **13.9 ms** (one 72 Hz deadline), as a count and a percentage |
| hitch | a frame over **2 × budget** (27.8 ms), timestamped. The first hitch time is recorded, because "it was fine until 6 minutes in" is the finding |
| thermal onset | the first moment Android's `PowerManager.getCurrentThermalStatus()` reports **≥ 2 (moderate = throttling has started)**, with the full status timeline |
| GPU level | 0–4, read off the **OVR Metrics overlay** by eye and written into §5. It is deliberately not inferred |

**Warmup is excluded from the headline but not hidden.** The first 20 s includes scene load, MRUK's
room resolution (P1.6 measures that separately) and shader variants. Those frames are still sampled
and still appear in the 10-second windows of `perf-baseline.jsonl`, marked `"warmup": true`.

**Why 1% low and not just the average:** a locked 72 with occasional 40 ms hitches and a steady 68 can
have the same average and feel nothing alike. `PERFORMANCE.md` §2 already commits to the 1% low being
"the honest number, the average is marketing"; this is where it gets measured.

## 5. The run — sheet to fill

**Before starting, record the conditions.** A thermal measurement without them is not repeatable:

| Condition | Value |
|---|---|
| Device | must be **Quest 3S** (the rubric names it; a Quest 3 column cannot substitute — `RISKS.md` R10) |
| `SystemInfo.deviceModel` as reported | |
| Room temperature / time of day | |
| Battery % at start, and unplugged? | |
| Overlay FPS line (confirms the refresh rate) | |
| GPU level / CPU level at t = 1 min | |
| Which build (`Application.version`) | |

**The result:**

| avg fps | 1% low | p99 frame ms | frames over 13.9 ms | thermal onset | peak thermal status | GPU level range observed | margin vs 72 fps | verdict |
|---|---|---|---|---|---|---|---|---|
| | | | | | | | | |

**Headline, in words** (this is what goes into `PERFORMANCE.md` §2 and the video overlay):

> ⬜ fps average, ⬜ fps 1% low, throttling ⬜, on a Quest 3S over a 15-minute run.

**Predictions, written before the data:**

| Claim | Prediction | Reasoning |
|---|---|---|
| average fps | **72 locked** | the whole scene is the shelf, two hand skeletons and passthrough; there are no realtime lights, no shadows, and the matcher is a few hundred quaternion comparisons per frame |
| 1% low | **the risk, not the average** | hand tracking cost is bursty and the matcher allocates nothing but the hand pipeline can; a single GC or a shader compile is a 40 ms frame |
| thermal onset | **6–14 min if it happens at all** | a 3S has less headroom than a 3 and passive cooling; Meta's general guidance is that sustained heavy GPU load throttles inside 10–15 min. If onset is under 5 min the scene is heavier than it looks and that is the finding |
| margin vs 72 | **positive but not comfortable** — passthrough is the unknown | app-side rendering is trivial; the compositor's passthrough compositing is the part we cannot see from Unity |

**If the average comes in below 72 fps**, the task requires the top three costs from the Unity profiler
*immediately*, and in this order of suspicion, because these are the only things that can plausibly
cost anything here:

1. **MSAA at 4× and the render scale** — the cheapest lever, one setting, `[TUNE]` in the budget table.
2. **Per-frame work in the hand pipeline** (`SkeletonPoseSource` / `HandVisual` / `Matcher`) — including
   any `OnGUI` left enabled: a `GUI.Box` per frame is a full-canvas repaint. `PerfRigBuilder` disables
   the probes' GUIs for this reason.
3. **Passthrough layer configuration** (overlay vs underlay, `OVRPassthroughLayer` edge rendering) — if
   app GPU time looks fine but fps is low, the cost is downstream of the app and the fix is in the layer.

## 6. Where the numbers land

| Destination | What goes there |
|---|---|
| `docs/02-tech/PERFORMANCE.md` §2 | the **first measured row** — this run, with the method sentence and the conditions |
| `docs/02-tech/PERFORMANCE.md` §3 | the same row joined by the later 15-minute full-loop runs; §3 stays otherwise empty until P7.4 |
| This page | the raw run: windows, hitches, thermal timeline — the evidence behind both |

## 7. How to run it

```
1. Scene: SIGNSPACE/P1.8 Build perf rig   (disables the trial loggers, checks passthrough + shelf)
2. SIGNSPACE/P1.8 Enable Frame Timing Stats   (optional: fills cpuMs/gpuMs; fps does not need it)
3. OVR Metrics Tool on, overlay on:
     adb shell am start omms://app
     adb shell am broadcast -n com.oculus.ovrmonitormetricsservice/.SettingsBroadcastReceiver \
       -a com.oculus.ovrmonitormetricsservice.ENABLE_OVERLAY
4. Build to the QUEST 3S (not the editor, not the simulator), hands in view, cube anchored,
   passthrough on, headset on a stand or worn while sitting still.
5. Touch nothing for 15 minutes. Do not pick up a controller: that is a hitch, and hitches are recorded.
6. Pull:
     adb shell run-as com.signspace.signspace cat \
       /sdcard/Android/data/com.signspace.signspace/files/perf-baseline-summary.txt
     adb shell run-as com.signspace.signspace cat \
       /sdcard/Android/data/com.signspace.signspace/files/perf-baseline.jsonl
7. If the app reports thermal "unavailable" (PowerManager not exposed), poll the ground truth:
     adb shell dumpsys thermalservice | head -40      (once a minute, note the timestamps)
8. Write the numbers into §5, then into PERFORMANCE.md §2.
```

## 8. Verdict against the gate

| Gate G1 criterion | Needs | Status |
|---|---|---|
| 9 — baseline fps on Quest 3S with a ≥10 min thermal run | one admissible run: `avg`, `1% low`, thermal onset or "none in 15 min", margin vs 72 | 🟡 **unmeasured** — sampler built |

## 9. Change log

| Date | Change | Why |
|---|---|---|
| 15 Nov 2026 | run length 15 min, refuse < 10 min | task says 10, gate says ≥10, PERFORMANCE.md says 15; the strictest is enforced |
| 15 Nov 2026 | 1% low defined as worst-1%-frames mean, not a percentile | several definitions exist; this one is stated before the data |
| 15 Nov 2026 | thermal read from Android `PowerManager`, not inferred from fps | an fps drop is a *consequence* of throttling; the status change is the event |
| 15 Nov 2026 | `unavailable` returned instead of `0` when the platform call fails | `0 = none` would look like a pass — the one thing a thermal measurement must not do |
| 15 Nov 2026 | warmup excluded via an exact frame index | an estimate from an assumed 72 fps would move the boundary on a run that was not hitting 72 |
| 15 Nov 2026 | trial rigs and probe `OnGUI`s disabled by the builder | a per-frame `GUI.Box` is a full-canvas repaint; measuring the harness is not measuring the product |
