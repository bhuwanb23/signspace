# P6 — Gaze & accessibility

**Day 7 (Fri 13 Nov) · Budget 7 h · Gate:** G6 — Do the one-handed, eyes-only and calibrated-non-standard-hand playthroughs all succeed?
**Status:** ✅ built and desk-checked · ⬜ device playthroughs NOT RUN

## Objective
Make every modality path real: gaze that genuinely reads your eyes, a full eyes-only loop, hand-agnostic calibration, and five accessibility toggles that all work with the loop rather than around it.

## Why it sits here
Gaze is a named rubric technology and the mechanism that makes finger-level inspection possible with both hands busy. Accessibility is simultaneously a $25k award, a rubric beat, and the moral centre of the product.

## Sub-phases

| # | Task | Est | Workstream | Status |
|---|---|---|---|---|
| [P6.1](tasks/p6.1-gaze-highlight.md) | Eye-gaze interaction on the finger-highlight layer | 2 h | W2 | ✅ built · ⬜ 0.4/0.6 m test |
| [P6.2](tasks/p6.2-prove-eye-tracking.md) | Prove eye tracking is actually active — not silently head-raycasting | 1 h | W2 | ✅ built · ⬜ recording |
| [P6.3](tasks/p6.3-eyes-only.md) | The eyes-only path | 1 h | W1 | ✅ controls · ⬜ recording |
| [P6.4](tasks/p6.4-hand-calibration.md) | 30-second calibration that normalises to the user's hand | 1.5 h | W1 | ✅ built · ⬜ restricted-hand video |
| [P6.5](tasks/p6.5-toggles.md) | The five accessibility toggles | 1 h | W1 | ✅ built · ⬜ per-switch loops |
| [P6.6](tasks/p6.6-reduced-motion.md) | Reduced motion path | 30 min | W1 | ✅ built · ⬜ recording |
| [P6.7](tasks/p6.7-a11y-doc.md) | Write the accessibility doc to the build, not to the aspiration | 1 h | W3 | ✅ |

**Budget:** 7 h. If it overruns, cut the calibration (rung 2), not the gaze.

## Inputs required before starting
- Working matcher and bands (P2) ✅
- Working shelf (P4) ✅
- XR Simulator VR Glasses profile — **not available; no eye pose is bound, and the build says so**
- A hand you can restrict deliberately — **needs a person**

## Outputs — the phase is done when all of these are true
- [x] Gaze interaction built, with the fallback stated rather than silent
- [ ] Verified gaze interaction with a proof recording — **NOT RUN** (needs a headset)
- [x] Eyes-only controls, one-handed mode, calibration and reduced motion built
- [ ] Eyes-only, one-handed and calibrated playthroughs — **NOT RUN** (needs a body)
- [x] Seven persistent toggles, reachable in space by pinch and by dwell
- [x] An accessibility doc that matches reality

## What was actually checked (2026-11-17)

| Check | Result |
|---|---|
| `SIGNSPACE/P6 Check all` | 106 passed, 0 failed |
| compile | 0 errors |
| scene rebuild | ✅ — `Accessibility` wired, 8 tray blocks, 5 finger targets, both panel handles gaze-reachable |
| APK | 64,516,403 B · `sha256 c71b9d26df9838b2eee4698bf6e748722b22fe8dd03f8195be1a920f18f83e4f` |
| regression | P5 credits, P5.7 copy, shelf, hand rig, P4.3 persistence all pass |

**Every human/device row from this phase is in [`docs/00-plan/HUMAN-TASKS.md`](../../docs/00-plan/HUMAN-TASKS.md)** (H14–H22).

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
