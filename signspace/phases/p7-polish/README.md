# P7 — Polish

**Day 8 (Sat 14 Nov) · Budget 8 h · Gate:** G7 — Does someone who has never touched a headset get from launch to their first mastered shape without you saying a word?
**Status:** 🟡 Built and desk-verified · device rows NOT RUN (`HUMAN-TASKS` H23–H28)

## Objective
Turn a working build into a finished one: sound, zero-text onboarding, verified FoV, measured performance, and a build that cannot crash in front of a judge.

## Why it sits here
Polish is 25% of the score and it is the cheapest 25% in the competition. This phase exists because the difference between a good idea and a winning entry is almost entirely here.

## Sub-phases

| # | Task | Est | Workstream | Status |
|---|---|---|---|---|
| [P7.1](tasks/p7.1-sound.md) | Sound design: every state change has a sound | 2 h | W1 | ✅ 16 clips, 12 cues wired, all ≤ −6 dBFS |
| [P7.2](tasks/p7.2-zero-text.md) | The zero-text onboarding pass | 2 h | W1 | ✅ 0 instructional words; cold-start NOT RUN |
| [P7.3](tasks/p7.3-fov-verify.md) | Verify the FoV reflow in the VR Glasses profile | 45 min | W2 | ✅ geometry measured at 70°; device/split-screen NOT RUN |
| [P7.4](tasks/p7.4-perf-harden.md) | Performance hardening on Quest 3S | 1.5 h | W2 | 🟡 posture measured (0 lights/shadows, MSAA 2×, FFR medium); 15-min fps NOT RUN |
| [P7.5](tasks/p7.5-cold-boot.md) | Cold boot and stability sweep | 45 min | W2 | 🟡 probe + static splash shipped; 5 launches NOT RUN |
| [P7.6](tasks/p7.6-crash-sweep.md) | Crash sweep | 1 h | W2 | 🟡 5 hostile conditions answered in code; 20 launches NOT RUN |
| [P7.7](tasks/p7.7-a11y-regression.md) | Accessibility regression pass | 45 min | W1 | ✅ all P6 checks pass inline; device recordings NOT RUN |

**Budget:** 8 h. This is the phase that absorbs overruns from everywhere else, which means it will overrun. Protect P7.4 and P7.6 above all.

## Inputs required before starting
- All prior phases green
- Sound assets
- OVR Metrics Tool
- XR Simulator VR Glasses profile

## Outputs — the phase is done when all of these are true
- [ ] A polished, measured, crash-free build — **polished and crash-hardened; the measurements are
      device rows and none of them ran**
- [ ] The performance overlay, FoV split and onboarding recordings — **NOT RUN** (H23)
- [x] A text budget under three words — **0 instructional words**, `text-budget.md`

## What is actually true today

| Row | Value | Evidence |
|---|---|---|
| Headless checks | **128 passed, 0 failed** | `SIGNSPACE/P7 Check all` |
| Sound | 16 clips, 596 kB, every peak ≤ −6 dBFS | `P7.1`, `SOUND-MAP.md` §6.1 |
| Instructional words | 0 | `text-budget.md` |
| Worst shelf slot at 70°×66° | 19.0° off-axis | `P7.3`, `MRUK-SHELF.md` §7.1 |
| Narrow-profile tray | 26.0° (1.5° spare) | `P7.3` |
| Realtime lights / shadow casters / MSAA | 0 / 0 / 2× | `P7.4`, `PERFORMANCE.md` §2.2 |
| APK | 64,584,319 B, `sha256 299ad042…398e` | this build |
| Sustained fps, thermal, cold boot, 20 launches, spoken onboarding | **NOT RUN** | `HUMAN-TASKS` H23–H28 |

## Gate
[`gate.md`](gate.md) — Does someone who has never touched a headset get from launch to their first mastered shape without you saying a word?

## Traps specific to this phase
- Polishing visuals before performance — on a 3S that order is backwards
- Adding 'just one' new feature; the freeze is at Day 11 and scope only moves down

## Related documents
- [`docs/04-design/FIRST-FIVE-MINUTES.md`](../../docs/04-design/FIRST-FIVE-MINUTES.md)
- [`docs/04-design/SOUND-MAP.md`](../../docs/04-design/SOUND-MAP.md)
- [`docs/02-tech/PERFORMANCE.md`](../../docs/02-tech/PERFORMANCE.md)
- [`docs/03-content/ACCESSIBILITY.md`](../../docs/03-content/ACCESSIBILITY.md)

---
*Part of the [master plan](../../docs/00-plan/MASTER-PLAN.md).*
