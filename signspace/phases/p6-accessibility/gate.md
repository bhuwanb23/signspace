# GATE G6 — Do the one-handed, eyes-only and calibrated-non-standard-hand playthroughs all succeed?

**Phase:** P6 · **Verdict:** ⬜ not yet evaluated

## Criteria — all must be true
- [ ] Gaze highlight accurate at 0.4 m and 0.6 m
- [ ] Eye tracking proven active (eye-move yes, head-move no), with the test recorded
- [ ] Eyes-only full loop recorded end to end
- [ ] Calibration works and a restricted-range hand can still lock a shape
- [ ] Six toggles implemented, persistent, reachable in-space
- [ ] Reduced-motion path works
- [ ] Accessibility doc matches the build with an explicit non-claims section

## If red
If eye tracking is unavailable on the test hardware and the simulator is the only path, ship the dwell-based head-gaze fallback **and say so in the description** — an honest partial is worth more than a claimed feature. If calibration destabilises the matcher, cut it (cut ladder rung 2) rather than ship an inconsistent loop.

## Decision record

| Field | Value |
|---|---|
| Verdict | ⬜ PASS / ⬜ FAIL |
| Date evaluated | |
| Evaluated by | |
| Evidence (screenshot / log / recording) | |
| Notes | |
