# GATE G6 — Do the one-handed, eyes-only and calibrated-non-standard-hand playthroughs all succeed?

**Phase:** P6 · **Verdict:** ⬜ not yet evaluated on device · desk checks green

## Criteria — all must be true

| # | Criterion | Desk | Device | Note |
|---|---|---|---|---|
| 1 | Gaze highlight accurate at 0.4 m and 0.6 m | — | ⬜ | 5 fingertip targets at 11 mm; the distance test needs a headset (`HUMAN-TASKS` H14) |
| 2 | Eye tracking proven active (eye-move yes, head-move no), recorded | ✅ honesty enforced | ⬜ | the probe refuses to claim eyes without an eye pose; recording is H15 |
| 3 | Eyes-only full loop recorded end to end | ✅ every **control** reachable | ⬜ | the lock stays physical by design — see `ACCESSIBILITY.md` §5; recording H17 |
| 4 | Calibration works and a restricted-range hand can still lock a shape | ✅ half-range hand reads 0.0° | ⬜ | `P6Checks.CheckCalibration`; a real hand is H19 |
| 5 | Six toggles implemented, persistent, reachable in-space | ✅ 7 + calibrate, 8 blocks | ⬜ | both inputs on every block; per-switch loops H20 |
| 6 | Reduced-motion path works | ✅ | ⬜ | shelf, hand drift and payoff verified headless; recording H21 |
| 7 | Accessibility doc matches the build with an explicit non-claims section | ✅ | — | `P6Checks.CheckA11yDoc` |

**Green means "the build can do it and says so honestly".** It does not mean a person has done it.

## If red
If eye tracking is unavailable on the test hardware and the simulator is the only path, ship the
dwell-based head-gaze fallback **and say so in the description** — an honest partial is worth more
than a claimed feature. If calibration destabilises the matcher, cut it (cut ladder rung 2) rather
than ship an inconsistent loop.

Calibration did **not** destabilise the matcher: with no profile installed the matcher takes the
previous code path exactly (`_fingerScale == null` short-circuits), and the P2/P3/P4 checks still pass.

## Decision record

| Field | Value |
|---|---|
| Verdict | ⬜ PASS / ⬜ FAIL |
| Date evaluated | |
| Evaluated by | |
| Evidence (screenshot / log / recording) | `P6Checks` 106/106 + the six recordings named in `ACCESSIBILITY.md` §9 |
| Notes | every remaining row needs a headset or a body: `docs/00-plan/HUMAN-TASKS.md` H14–H22 |
