# GATE G1 — Does grab-and-rotate feel good on device, and do at least 8 of 20 probed handshapes track reliably?

**Phase:** P1 · **Verdict:** ⬜ not yet evaluated · **P1.1–P1.8 all code complete — every remaining criterion is waiting on the same thing: one headset session.**

> Recounted 15 Nov 2026: **9 criteria — 0 met, 9 with a rig built and readings pending, 0 blocked by anything other than hardware.** The only criterion with nothing built behind it is #6 (≥8 of 20 High/ Medium), which needs #5's sessions to exist first.

## Criteria — all must be true
- [ ] A custom hand visual tracks both hands at frame rate — 🟡 **instrumented, needs the device** (P1.1 code + readout done)
- [ ] Grab succeeds 20/20 with each hand, with no ray or controller fallback in the scene — 🟡 rig built, fallback guard active, **trials pending**
- [ ] A rotation variant is chosen and rated acceptable at both 0.4 m and 0.6 m — 🟡 three variants + rating rig built, **verdict pending**
- [ ] Two poses load from JSON and reproduce faithfully — 🟡 six load and apply by name; **visual fidelity pending**
- [ ] 20 handshapes probed in two lighting conditions, results recorded — 🟡 20 references + automatic harness ready, **sessions pending**
- [ ] **≥8 of 20 shapes rated High or Medium in normal light** — ⬜ needs the sessions
- [ ] MRUK anchors a cube to a real surface in **≥2 of 4** room conditions — 🟡 rig ready, readings pending ([mruk-log.md](mruk-log.md) §5; pass rule fixed in §4, decision rule if it fails in §8)
- [ ] The anchor survives a relaunch **and** a headset reboot — 🟡 rig ready, readings pending ([persistence-log.md](persistence-log.md) §6; not-found behaviour fixed in §7)
- [ ] Baseline fps measured on Quest 3S with a thermal run of ≥10 min — 🟡 sampler + protocol ready, run pending ([perf-baseline.md](perf-baseline.md) §5; 15 min run, <10 min recorded INVALID)

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
