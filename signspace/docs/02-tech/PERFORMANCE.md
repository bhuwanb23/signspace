# Performance
**Status:** ✅ v1 (method) · 🟡 **first measured row pending (P1.8)** — numbers come from the device · **Owner:** W2 · **Phase:** P7 · **Gate:** G7

> **60 fps is the rubric floor. 72 fps is our floor.** The gap between them is the difference between
> a pass and an award. The number that goes in the video is the number the device produced over a
> 15-minute thermal run — not a target, not an estimate.

---

## 1. The budget

72 fps = **13.9 ms per frame**. Quest 3S has less thermal headroom than Quest 3, so the budget is
set against the weaker device.

| Slice | Budget | Notes |
|---|---|---|
| Total frame | **13.9 ms** | 72 Hz |
| CPU | ≤ 8.0 ms | matching is 6 shapes × 5 finger comparisons = 30 quaternion comparisons per frame. Microseconds, not milliseconds. |
| GPU | ≤ 6.5 ms | **the real constraint** |
| Draw calls | ≤ 120 | the shelf is 6 objects; everything else is a handful of quads |
| Triangles (visible) | ≤ 150k | reference hand ~8k, low-poly, untextured geometry |
| Materials | ≤ 12 unique | shared, unlit or very cheap lit |
| Realtime lights | **0** | baked or unlit only |
| Shadows | **Off** | non-negotiable on a 3S |
| MSAA | 4× `[TUNE]` | measure; 2× may buy pixels back |
| Fixed Foveated Rendering | **On**, medium | invisible on a hand at arm's length |
| Application SpaceWarp | **Off** | conflicts with aggressive hand motion; revisit only if desperate |
| Textures | ≤ 512² except one hero 1024² | audit at P7.4 |

**`BuildScript.ApplySettings()` pins the graphics API to Vulkan** so it cannot drift between the
Day 1 build and the Day 4 submission build.

## 2. How we measure

**Method.** OVR Metrics Tool (already installed: `metavr tools install ovrmetric`), on the device,
for **15 minutes continuously**, with the full loop running: hand tracking, the matcher, the
reference hand, the shelf, the gaze highlight.

**What we record:**

| | |
|---|---|
| Frame rate | min · average · **1% low** — the 1% low is the honest number, the average is marketing |
| GPU utilisation | the time the GPU is busy |
| CPU utilisation | frame time spent on the main thread |
| Thermal | the temperature over the run, and **where throttling starts** |
| The moment it degrades | what visibly changes first |

**The 15 minutes is not arbitrary.** A Quest 3S in a room at 7 p.m. will thermal-throttle inside a
short session. A three-minute demo proves nothing. `RISKS.md` R10's trigger is *"<72 fps at 10
minutes"* — which is why the run is 15 and not 5.

**Cold boot** is measured separately: **launch to first interaction**, median of five cold starts,
and it must be **under 15 seconds**. `FIRST-FIVE-MINUTES.md` has no splash screen and no calibration
screen, so there is nothing to hide the wait behind.

### 2.1 The first measured row (P1.8)

The first row this project produces, kept here rather than in §3 because §3 is the P7.4 table of
*final* runs. This one is the de-risking baseline: it decides whether an architectural decision can
still be changed, which is why it is taken in P1 and not in P7.

| Device | run | avg fps | 1% low | thermal onset | margin vs 72 fps | source |
|---|---|---|---|---|---|---|
| **Quest 3S** | ⬜ 15 min | ⬜ | ⬜ | ⬜ | ⬜ | [`phases/p1-derisk/perf-baseline.md`](../../phases/p1-derisk/perf-baseline.md) §5 |

**Method for this row:** `PerfProbe` — an in-app sampler reading the per-frame interval and Android's
`PowerManager` thermal status — running alongside the **OVR Metrics Tool** overlay, with both hands
tracked, **one anchored object**, and **passthrough on**. No editor, no simulator, 15 minutes with a
20 s warmup excluded, and a run under 10 minutes is recorded as `INVALID` rather than as a result.

**Why these three things are in the loop:** passthrough is compositor work that never appears in app
GPU time, so a baseline without it is optimistic; the anchored object is the product's own path (MRUK
query + anchor parenting); both hands are the heaviest per-frame app work we have. Measuring a simpler
scene would produce a nicer number and a useless one.

**If the row comes in under 72 fps**, the top three profiler costs are recorded in
`perf-baseline.md` §5 in the same session, in the order the budget table implies: MSAA/render scale,
the hand pipeline, then the passthrough layer.

## 3. The measured table

**Empty. It gets filled on the device, and the video overlay carries the same number.**

| Device | GPU level | avg | 1% low | min | thermal throttles at | notes |
|---|---|---|---|---|---|---|
| Quest 3S | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | **the target device** |
| Quest 3 | ⬜ | ⬜ | ⬜ | ⬜ | ⬜ | |

### The 3S problem, stated plainly

**If only a Quest 3 is available, the 3S column cannot be filled.** That is not a detail; it is a
judging criterion (the rubric names Quest 3S explicitly) and it is `RISKS.md` R10.

**What we do instead, in order:**

1. **Hold to 3S-conservative settings anyway** — no realtime shadows, no realtime lights, FFR on,
   MSAA 4×, ≤120 draw calls, ≤150k triangles, ≤12 materials. Build to the *spec* of the 3S even
   when we can only test on the 3.
2. **Lower the claim to what was measured.** If the 3 held 72 fps, the description says *"72 fps on a
   Quest 3"* — not "on a Quest 3S". Never claim the device we did not test.
3. **Disclose it in `PERFORMANCE.md`** — this table, with the gap named.

That is a weaker claim, honestly stated. It is defensible. Quietly asserting 3S numbers we never
measured is not.

## 4. What we turned off, and what it bought

The point of a budget is knowing what each choice cost. **This table gets filled in at P7.4** by
measuring with and without.

| Change | fps bought | visual cost | kept? |
|---|---|---|---|
| Realtime shadows → off | ⬜ | none on an unlit hand | ✅ already off |
| Realtime lights → 0 | ⬜ | none, the rig is unlit | ✅ already off |
| MSAA 4× → 2× | ⬜ | visible edge shimmer on the highlight shell | ⬜ |
| Foveated rendering → higher level | ⬜ | slight blur at the edge of the gaze cone | ⬜ |
| Reference hand: 8k tris → 4k | ⬜ | the silhouette gets rounder | ⬜ |
| Shelf objects: 6 → fewer | ⬜ | less reward | ⬜ last |

## 5. Where the frame time actually goes

**To be measured.** The expected answer, and the thing to check first if we are short:

| Suspect | Why it might be slow | How to check |
|---|---|---|
| **Hand tracking** | 26 joints × 2 hands, every frame, on the CPU | Immersive Debugger: is the CPU frame time flat or does it spike? |
| **The matcher** | 6 shapes × 5 fingers = 30 quaternion comparisons per frame | it should be invisible. If it is not, something is allocating. **The budget is zero allocations per frame** — `ARCHITECTURE.md` §3 |
| The gaze highlight | a raycast and a label update per finger | is the highlight updating every frame or only on change? |
| The reference hand | a skinned mesh with 26 bones | bone count and skinning cost |
| The shelf | 6 objects, each an object | draw calls, and are they instanced? |

**The allocation trap.** `Matcher.Tick` must not allocate. Every buffer is fixed size and reused —
the finger angles array is allocated once and passed in, and `PoseMath.Distance` writes into it
rather than returning a new one. If any of that regresses to allocating per frame, it is a GC spike
every frame, and it will look like "the app gets janky after a minute". **Check this first.** The
allocating version of `Distance` was written and caught in review before the first device build;
`git log` on `PoseMath.cs` has the change.

## 6. Stability

| Check | Requirement |
|---|---|
| Cold launches | **20 launches, zero crashes** |
| Hostile conditions | 4 — backgrounding during a match, the headset losing tracking, a storage-full warning, a low-battery warning |
| Background cycles | 5 mid-session backgroundings, clean |
| Cold boot | median under 15 s |
| Thermal | 15 minutes with no visible degradation |

## 7. The regression checklist

Run this before every build that goes on a channel. Anything unchecked is a build that does not ship.

- [ ] `python3 scripts/validate_poses.py` passes
- [ ] `python3 scripts/count.py` passes
- [ ] the build compiles with zero errors
- [ ] 20 cold launches, zero crashes
- [ ] the 15-minute thermal run, with the numbers recorded above
- [ ] cold boot under 15 s
- [ ] all five accessibility toggles still work
- [ ] the near-miss still fires for U→V (the smoke test for the whole matching stack)

## 8. Inputs
`TECH-STACK.md` §4 · `RISKS.md` R10 · `DEFINITION-OF-DONE.md` · the OVR Metrics Tool APK

## 9. Output / done when
- [x] the method, the budget, and what each lever costs
- [x] the measurement procedure and the 15-minute requirement
- [x] the 3S gap written down before it becomes a problem
- [x] the regression checklist
- [ ] **the table in §3 is filled**
- [ ] **the 15-minute run is recorded**
- [ ] **the fps overlay in the video shows the same number**
