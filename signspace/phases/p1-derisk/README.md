# P1 — De-risk spike

**Day 2 (Sun 8 Nov) · Budget 8 h · Gate:** G1 — Does grab-and-rotate feel good on device, and do at least 8 of 20 probed handshapes track reliably?
**Status:** ⬜ Not started

## Objective
Prove, on real hardware, in one day, the four things the concept depends on: hands render well, grab feels good, wrist rotation is a viable core gesture, and the sensor can actually resolve enough handshapes to make a product. Plus the two platform bets — MRUK anchoring and persistence — and a measured frame budget.

## Why it sits here
Everything the concept assumes is true gets tested here, in one day, on real hardware. If the core gesture doesn't feel good, we must know on Day 2 — not Day 8.

## Sub-phases

| # | Task | Est | Workstream | Status |
|---|---|---|---|---|
| [P1.1](tasks/p1.1-hand-visual.md) | Custom hand visual running at frame rate | 45 min | W2 | ⬜ |
| [P1.2](tasks/p1.2-grab-interaction.md) | Grab a free-floating object with hands only | 45 min | W2 | ⬜ |
| [P1.3](tasks/p1.3-wrist-rotate.md) | Rotate the grabbed object with your own wrist | 1.5 h | W2 | ⬜ |
| [P1.4](tasks/p1.4-reference-hand-and-pose.md) | Load a posed reference hand from JSON | 1 h | W2 | ⬜ |
| [P1.5](tasks/p1.5-trackability-probe.md) | Probe 20 candidate handshapes on real hands, in real rooms | 2 h | W3 | ⬜ |
| [P1.6](tasks/p1.6-mruk-surface.md) | Find a horizontal surface and anchor a cube to it | 1 h | W2 | ⬜ |
| [P1.7](tasks/p1.7-persistence.md) | Survive a relaunch: the cube is still there | 45 min | W2 | ⬜ |
| [P1.8](tasks/p1.8-perf-baseline.md) | Measure the baseline frame budget on Quest 3S | 45 min | W2 | ⬜ |

**Budget:** 8 h. This is the highest value-per-hour day in the project. If it needs 10 h, take them from P7.

## Inputs required before starting
- A working empty-scene build on the primary device (P0.4)
- A 26-joint hand mesh
- Meta XR SDK v207 imported
- OVR Metrics Tool installed
- Two hours of daylight and one of lamplight

## Outputs — the phase is done when all of these are true
- [ ] A written go/no-go on the core gesture
- [ ] A measured trackability table for 20 handshapes
- [ ] A working anchored-and-persistent object
- [ ] A baseline frame time on the weaker device
- [ ] The first two real poses in JSON

## Gate
[`gate.md`](gate.md) — Does grab-and-rotate feel good on device, and do at least 8 of 20 probed handshapes track reliably?

## Traps specific to this phase
- Spending the day making it pretty — nothing in P1 survives into the build except the knowledge
- Not writing verdicts down: an unwritten verdict is a rumour by Day 4
- Testing rotation with a cube instead of a hand
- Probing handshapes with one hand in ideal light

## Related documents
- [`docs/02-tech/TECH-STACK.md`](../../docs/02-tech/TECH-STACK.md) §6 (the five unknowns)
- [`docs/02-tech/POSE-FORMAT.md`](../../docs/02-tech/POSE-FORMAT.md)
- [`docs/03-content/TRACKABILITY.md`](../../docs/03-content/TRACKABILITY.md)
- [`docs/03-content/SIGNSET.md`](../../docs/03-content/SIGNSET.md)

---
*Part of the [master plan](../../docs/00-plan/MASTER-PLAN.md).*
