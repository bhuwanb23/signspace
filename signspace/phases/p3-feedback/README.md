# P3 — Feedback layer

**Day 4 (Tue 10 Nov) · Budget 8 h · Gate:** G3 — Does the near-miss land in under 30 seconds of play — and does it never once lie?
**Status:** ⬜ Not started

## Objective
Build the layer that makes this submission different from every prior project in the field: the near-miss diagnosis, the honest mirror, and the discipline that keeps both truthful.

## Why it sits here
This phase is the submission's identity, not its polish. Two of the five novelty moves live here, and the video's first four seconds are shot in this phase.

## Sub-phases

| # | Task | Est | Workstream | Status |
|---|---|---|---|---|
| [P3.1](tasks/p3.1-near-miss-table.md) | Build the near-miss: name the sign they actually made | 2.5 h | W2 | ⬜ |
| [P3.2](tasks/p3.2-confidence-gate.md) | Confidence gating: silence is the default | 1.5 h | W2 | ⬜ |
| [P3.3](tasks/p3.3-copy.md) | Write the near-miss copy | 45 min | W4 | ⬜ |
| [P3.4](tasks/p3.4-honest-mirror.md) | Build the honest mirror: publish the shapes we can't see | 1.5 h | W3 | ⬜ |
| [P3.5](tasks/p3.5-confidence-readout.md) | Live confidence readout that pauses instead of failing | 1 h | W2 | ⬜ |
| [P3.6](tasks/p3.6-numbers.md) | Verify the honest-mirror numbers on device | 30 min | W3 | ⬜ |
| [P3.7](tasks/p3.7-trackability-publish.md) | Publish the trackability document | 30 min | W3 | ⬜ |

**Budget:** 8 h. On the critical path: P7 cannot start until G3 is green.

## Inputs required before starting
- A working matcher with tuned bands (P2)
- Measured trackability data (P1.5)
- The 12-shape set with validated pairs

## Outputs — the phase is done when all of these are true
- [ ] A near-miss feature with a zero-false-positive record
- [ ] A shipped honest-mirror panel with real numbers
- [ ] A complete trackability document
- [ ] In-product copy signed off

## Gate
[`gate.md`](gate.md) — Does the near-miss land in under 30 seconds of play — and does it never once lie?

## Traps specific to this phase
- Adding features after the gate goes green — this phase is where scope creep is most tempting
- Making the honest mirror a settings screen instead of a place
- Letting the near-miss become frequent; it must stay rare to stay valuable

## Related documents
- [`docs/01-idea/NOVELTY-AND-PRIOR-ART.md`](../../docs/01-idea/NOVELTY-AND-PRIOR-ART.md) — N1 and N2
- [`docs/03-content/SIGNSET.md`](../../docs/03-content/SIGNSET.md) §5–7
- [`docs/03-content/TRACKABILITY.md`](../../docs/03-content/TRACKABILITY.md)
- [`docs/04-design/SOUND-MAP.md`](../../docs/04-design/SOUND-MAP.md)

---
*Part of the [master plan](../../docs/00-plan/MASTER-PLAN.md).*
