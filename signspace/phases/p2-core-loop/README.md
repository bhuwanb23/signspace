# P2 — Core loop

**Day 3 (Mon 9 Nov) · Budget 10 h · Gate:** G2 — Can a stranger lock one handshape, uncoached, without anyone telling them what to do?
**Closing report:** [`phase-2-complete.md`](phase-2-complete.md) — what shipped, what is pending, what is still unverified

**Status:** ✅ **P2 complete** — P2.1–P2.7 all built and measured; device checks, footage and the left-hand mirror test are open limitations

## Objective
Turn the Day-2 spikes into a coherent loop: a reference hand you can rotate, a matcher that measures you against it, three bands that never say 'wrong', and a payoff that makes the first success feel like an event.

## Why it sits here
This is where the product stops being a demonstration and becomes a system. Everything after this phase is feedback, polish or packaging — so the loop has to be right, not merely present.

## Sub-phases

| # | Task | Est | Workstream | Status |
|---|---|---|---|---|
| [P2.1](tasks/p2.1-pose-format-final.md) | Finalise the pose format and the capture-neutral contract | 1.5 h | W2 | ✅ — **schema 2 frozen**, validator self-tested (29 cases), migration path proven |
| [P2.2](tasks/p2.2-matcher-v1.md) | Build the matcher: relative-angle distance with per-joint weights | 2.5 h | W2 | ✅ — **v3 weighted metric**, weights in `Weights.asset`, overlay names the worst three; separability 3.89 (was 3.06). Device check open |
| [P2.3](tasks/p2.3-bands.md) | Three bands, no failure state | 1 h | W2 | ✅ — **12° / 24° + 2° release**, all measured; colour+motion per band, band sounds wired, 0 error strings by lint. 30 s device test open |
| [P2.4](tasks/p2.4-hold-and-dropout.md) | The 0.7 s hold, with dropout tolerance | 1 h | W2 | ✅ — **20/20 locks, 0/20 false locks in simulation**; 200 ms freeze + 0.5/s decay, latch released after 500 ms; every lock logged to CSV. Device run open |
| [P2.5](tasks/p2.5-lock-payoff.md) | The lock payoff: the shape becomes an object | 1.5 h | W1 | ✅ — **0.99 s** of a 1.2 s budget, transformation by overlap + brighten, chime + hand pulse, ×0.55 after 3 locks. Footage blocked |
| [P2.6](tasks/p2.6-palm-orientation.md) | Weight palm orientation separately from finger angles | 1 h | W2 | ✅ — **k = 1.0, slack = 15°**, basis not angles; 0% lock at 30° mis-face, 100% to 20°. No authored shape differs in facing (content gap flagged for P3) |
| [P2.7](tasks/p2.7-left-hand.md) | Left-hand support, or an explicit decision not to | 1.5 h | W2 | ✅ — **right hand only**, decided and measured: mirror exact, metric mirror-invariant, but the rig question needs hardware. Also fixed both hands feeding one matcher |

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
