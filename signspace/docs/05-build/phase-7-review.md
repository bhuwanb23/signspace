# Phase 7 review

**Written:** 17 Nov 2026 · **Phase:** P7 · **Gate:** G7 · **Companion to:** `docs/02-tech/PERFORMANCE.md`, `docs/04-design/SOUND-MAP.md`, `docs/04-design/UX-STATES.md`

> P3 and P8 set the pattern: every task against its evidence, the gate criterion by criterion, what is
> done and what is owed, and an honest "what we knowingly shipped broken" section. This is the same
> shape, because the honesty is the point.

---

## 1. What P7 is

P7 is the phase where a working build becomes a finished one: sound, zero-text onboarding, verified
FoV, measured performance, a build that cannot crash in front of a judge. Its gate asks whether a
novice reaches their first mastered sign without anyone saying a word.

## 2. What is built

### 2.1 Desk-verified (131/131 checks pass)

| Task | What | Verified |
|---|---|---|
| P7.1 Sound | 16 clips, 596 kB, 12 cues wired, every peak ≤ −6 dBFS | ✅ `P7Checks.CheckSound()` |
| P7.2 Zero-text | 0 instructional words, text-budget.md | ✅ `P7Checks.CheckText()` |
| P7.3 FoV reflow | Worst shelf slot 19.0° at 70°×66°, narrow tray 26.0° (1.5° spare), HUD 19.2° | ✅ `P7Checks.CheckFov()` |
| P7.4 Perf posture | 0 realtime lights, 0 shadow casters, MSAA 2×, FFR medium | ✅ `P7Checks.CheckPerformance()` |
| P7.5 Cold boot | `BootProbe` writes startup/hands/interactive_ms; static splash, no animation | ✅ `P7Checks.CheckBoot()` |
| P7.6 Crash sweep | 5 hostile conditions answered in code with desk checks | ✅ `P7Checks.CheckCrashPosture()` |
| P7.7 A11y regression | P6 suite passes inline (106 assertions) | ✅ `P7Checks.CheckA11yRegression()` |

### 2.2 What is NOT verified (device rows)

| Criterion | Status | Evidence owed |
|---|---|---|
| Sustained ≥72 fps for 15 min | ⬜ | OVR Metrics Tool trace |
| Thermal curve | ⬜ | thermal onset timestamp |
| 5 cold boots + 5 background cycles | ⬜ | measured times |
| 20 launches + hostile conditions on hardware | ⬜ | crash-free record |
| Chime fatigue by ear | ⬜ | human judgement |
| Cold start <15 s | ⬜ | measured time |
| Full loop in the VR Glasses profile | ⬜ | recording |
| Split-screen FoV capture | ⬜ | recording |

## 3. The gate — G7

**Verdict: not yet evaluated.**

| Criterion | Status | Evidence |
|---|---|---|
| Every state change has a sound or a documented silence | ✅ | `SOUND-MAP.md` + `P7.1` |
| ≤3 words of text in the first five minutes | ✅ | `text-budget.md` + `P7.2` |
| Full loop playable at 70°×66° | ⬜ | recording needed |
| ≥72 fps sustained for 15 min on Quest 3S | ⬜ | OVR Metrics trace |
| Median cold boot <15 s | ⬜ | measured time |
| 0 crashes across 20 launches | ⬜ | crash log |
| All eight accessibility paths pass | ⬜ | 8 refreshed recordings |

**The gate cannot be evaluated without a headset.** Six of seven criteria are device rows. The desk
work is done; the remaining work is measurement.

## 4. What the desk work buys

If the headset never arrives, the desk work is still worth something:

1. **The sound design is complete.** 16 clips, 596 kB, 12 cues wired, every peak ≤ −6 dBFS. The
   lock chime is the product's signature sound, and it is wired.
2. **The text budget is zero.** 0 instructional words in the first five minutes. The copy lint
   passes. The near-miss line is 9 words, under the 12-word ceiling.
3. **The FoV geometry fits.** The worst shelf slot is 19.0° at 70°×66°, with 1.5° spare. The HUD
   is at 19.2°. Nothing critical is outside the safe cone.
4. **The performance posture is clean.** 0 realtime lights, 0 shadow casters, MSAA 2×, FFR medium.
   The budget is not exceeded by the scene's own choices.
5. **The crash posture is documented.** 5 hostile conditions have written answers that run in code.
   The answers are not tested on hardware, but they exist.

## 5. What we knowingly shipped broken

| Issue | Why it is still broken | What we would do with a day more |
|---|---|---|
| Sustained fps not measured | no Quest 3S | run the 15-minute thermal |
| Thermal not measured | no Quest 3S | run the thermal probe |
| Chime fatigue not tested | no ears on hardware | listen six times in a row |
| Cold start not measured | no headset | time five cold boots |
| 20 launches not run | no headset | launch 20 times |
| Full loop in VR Glasses profile | no headset | run in the simulator (partial) |

## 6. Change log

| Date | Change | By |
|---|---|---|
| 17 Nov 2026 | File created. Desk-verified status recorded. Device rows listed honestly. | agent |
