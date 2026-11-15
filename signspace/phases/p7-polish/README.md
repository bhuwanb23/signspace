# P7 — Polish

**Day 8 (Sat 14 Nov) · Budget 8 h · Gate:** G7 — Does someone who has never touched a headset get from launch to their first mastered shape without you saying a word?
**Status:** ⬜ Not started

## Objective
Turn a working build into a finished one: sound, zero-text onboarding, verified FoV, measured performance, and a build that cannot crash in front of a judge.

## Why it sits here
Polish is 25% of the score and it is the cheapest 25% in the competition. This phase exists because the difference between a good idea and a winning entry is almost entirely here.

## Sub-phases

| # | Task | Est | Workstream | Status |
|---|---|---|---|---|
| [P7.1](tasks/p7.1-sound.md) | Sound design: every state change has a sound | 2 h | W1 | ⬜ |
| [P7.2](tasks/p7.2-zero-text.md) | The zero-text onboarding pass | 2 h | W1 | ⬜ |
| [P7.3](tasks/p7.3-fov-verify.md) | Verify the FoV reflow in the VR Glasses profile | 45 min | W2 | ⬜ |
| [P7.4](tasks/p7.4-perf-harden.md) | Performance hardening on Quest 3S | 1.5 h | W2 | ⬜ |
| [P7.5](tasks/p7.5-cold-boot.md) | Cold boot and stability sweep | 45 min | W2 | ⬜ |
| [P7.6](tasks/p7.6-crash-sweep.md) | Crash sweep | 1 h | W2 | ⬜ |
| [P7.7](tasks/p7.7-a11y-regression.md) | Accessibility regression pass | 45 min | W1 | ⬜ |

**Budget:** 8 h. This is the phase that absorbs overruns from everywhere else, which means it will overrun. Protect P7.4 and P7.6 above all.

## Inputs required before starting
- All prior phases green
- Sound assets
- OVR Metrics Tool
- XR Simulator VR Glasses profile

## Outputs — the phase is done when all of these are true
- [ ] A polished, measured, crash-free build
- [ ] The performance overlay, FoV split and onboarding recordings
- [ ] A text budget under three words

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
