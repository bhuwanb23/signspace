# P2 — Core loop

**Day 3 (Mon 9 Nov) · Budget 10 h · Gate:** G2 — Can a stranger lock one handshape, uncoached, without anyone telling them what to do?
**Status:** ⬜ Not started

## Objective
Turn the Day-2 spikes into a coherent loop: a reference hand you can rotate, a matcher that measures you against it, three bands that never say 'wrong', and a payoff that makes the first success feel like an event.

## Why it sits here
This is where the product stops being a demonstration and becomes a system. Everything after this phase is feedback, polish or packaging — so the loop has to be right, not merely present.

## Sub-phases

| # | Task | Est | Workstream | Status |
|---|---|---|---|---|
| [P2.1](tasks/p2.1-pose-format-final.md) | Finalise the pose format and the capture-neutral contract | 1.5 h | W2 | ⬜ |
| [P2.2](tasks/p2.2-matcher-v1.md) | Build the matcher: relative-angle distance with per-joint weights | 2.5 h | W2 | ⬜ |
| [P2.3](tasks/p2.3-bands.md) | Three bands, no failure state | 1 h | W2 | ⬜ |
| [P2.4](tasks/p2.4-hold-and-dropout.md) | The 0.7 s hold, with dropout tolerance | 1 h | W2 | ⬜ |
| [P2.5](tasks/p2.5-lock-payoff.md) | The lock payoff: the shape becomes an object | 1.5 h | W1 | ⬜ |
| [P2.6](tasks/p2.6-palm-orientation.md) | Weight palm orientation separately from finger angles | 1 h | W2 | ⬜ |
| [P2.7](tasks/p2.7-left-hand.md) | Left-hand support, or an explicit decision not to | 1.5 h | W2 | ⬜ |

**Budget:** 10 h — the longest single phase. If it slips, take the time from P7, never from P3.

## Inputs required before starting
- A working grab-and-rotate gesture (P1.3 verdict)
- A pose loader (P1.4)
- The trackability results (P1.5)
- Two or three real handshapes to test with — placeholders are fine

## Outputs — the phase is done when all of these are true
- [ ] A playable loop, one shape deep
- [ ] A tuned distance metric with recorded evidence
- [ ] A filmed lock payoff
- [ ] A documented decision on left-hand support

## Gate
[`gate.md`](gate.md) — Can a stranger lock one handshape, uncoached, without anyone telling them what to do?

## Traps specific to this phase
- Tuning thresholds by taste instead of against fixtures
- Letting the UI show a percentage — percentages invite optimisation and destroy the felt sense of a shape
- Fixing feel problems by adding features

## Related documents
- [`docs/02-tech/MATCH-ALGORITHM.md`](../../docs/02-tech/MATCH-ALGORITHM.md)
- [`docs/02-tech/POSE-FORMAT.md`](../../docs/02-tech/POSE-FORMAT.md)
- [`docs/04-design/UX-STATES.md`](../../docs/04-design/UX-STATES.md)
- [`docs/08-decisions/ADR-003-forgiveness.md`](../../docs/08-decisions/ADR-003-forgiveness.md)

---
*Part of the [master plan](../../docs/00-plan/MASTER-PLAN.md).*
