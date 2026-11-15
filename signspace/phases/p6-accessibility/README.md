# P6 — Gaze & accessibility

**Day 7 (Fri 13 Nov) · Budget 7 h · Gate:** G6 — Do the one-handed, eyes-only and calibrated-non-standard-hand playthroughs all succeed?
**Status:** ⬜ Not started

## Objective
Make every modality path real: gaze that genuinely reads your eyes, a full eyes-only loop, hand-agnostic calibration, and five accessibility toggles that all work with the loop rather than around it.

## Why it sits here
Gaze is a named rubric technology and the mechanism that makes finger-level inspection possible with both hands busy. Accessibility is simultaneously a $25k award, a rubric beat, and the moral centre of the product.

## Sub-phases

| # | Task | Est | Workstream | Status |
|---|---|---|---|---|
| [P6.1](tasks/p6.1-gaze-highlight.md) | Eye-gaze interaction on the finger-highlight layer | 2 h | W2 | ⬜ |
| [P6.2](tasks/p6.2-prove-eye-tracking.md) | Prove eye tracking is actually active — not silently head-raycasting | 1 h | W2 | ⬜ |
| [P6.3](tasks/p6.3-eyes-only.md) | The eyes-only path | 1 h | W1 | ⬜ |
| [P6.4](tasks/p6.4-hand-calibration.md) | 30-second calibration that normalises to the user's hand | 1.5 h | W1 | ⬜ |
| [P6.5](tasks/p6.5-toggles.md) | The five accessibility toggles | 1 h | W1 | ⬜ |
| [P6.6](tasks/p6.6-reduced-motion.md) | Reduced motion path | 30 min | W1 | ⬜ |
| [P6.7](tasks/p6.7-a11y-doc.md) | Write the accessibility doc to the build, not to the aspiration | 1 h | W3 | ⬜ |

**Budget:** 7 h. If it overruns, cut the calibration (rung 2), not the gaze.

## Inputs required before starting
- Working matcher and bands (P2)
- Working shelf (P4)
- XR Simulator VR Glasses profile
- A hand you can restrict deliberately

## Outputs — the phase is done when all of these are true
- [ ] Verified gaze interaction with a proof recording
- [ ] Eyes-only, one-handed and calibrated playthroughs
- [ ] Six persistent toggles
- [ ] An accessibility doc that matches reality

## Gate
[`gate.md`](gate.md) — Do the one-handed, eyes-only and calibrated-non-standard-hand playthroughs all succeed?

## Traps specific to this phase
- Believing the rig when it falls back to head gaze
- Adding accessibility after the loop is 'done' — it changes the loop
- Overclaiming in the doc

## Related documents
- [`docs/02-tech/GAZE-INPUT.md`](../../docs/02-tech/GAZE-INPUT.md)
- [`docs/03-content/ACCESSIBILITY.md`](../../docs/03-content/ACCESSIBILITY.md)
- [`docs/01-idea/NOVELTY-AND-PRIOR-ART.md`](../../docs/01-idea/NOVELTY-AND-PRIOR-ART.md) — N5
- [`docs/04-design/UX-STATES.md`](../../docs/04-design/UX-STATES.md)

---
*Part of the [master plan](../../docs/00-plan/MASTER-PLAN.md).*
