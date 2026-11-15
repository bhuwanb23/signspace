# GATE G1 — Does grab-and-rotate feel good on device, and do at least 8 of 20 probed handshapes track reliably?

**Phase:** P1 · **Verdict:** ⬜ not yet evaluated

## Criteria — all must be true
- [ ] A custom hand visual tracks both hands at frame rate
- [ ] Grab succeeds 20/20 with each hand, with no ray or controller fallback in the scene
- [ ] A rotation variant is chosen and rated acceptable at both 0.4 m and 0.6 m
- [ ] Two poses load from JSON and reproduce faithfully
- [ ] 20 handshapes probed in two lighting conditions, results recorded
- [ ] **≥8 of 20 shapes rated High or Medium in normal light**
- [ ] MRUK anchors a cube to a real surface in ≥2 of 4 room conditions
- [ ] The anchor survives a relaunch and a headset reboot
- [ ] Baseline fps measured on Quest 3S with a thermal run of ≥10 min

## If red
**P1.3 red (rotation feels wrong):** do not proceed. Test the hold-still-to-rotate variant for one more hour; if it still fails, the core gesture is wrong and the concept is re-opened the same day.

**P1.5 red (<8 shapes):** cut the content to 6 shapes / 1 family immediately (`SIGNSET.md` §7 decision rule) and continue — this is a scope cut, not a concept change.

**P1.6/P1.7 red:** make the virtual plinth the permanent design and remove all surface-dependence from the pitch.

## Decision record

| Field | Value |
|---|---|
| Verdict | ⬜ PASS / ⬜ FAIL |
| Date evaluated | |
| Evaluated by | |
| Evidence (screenshot / log / recording) | |
| Notes | |
