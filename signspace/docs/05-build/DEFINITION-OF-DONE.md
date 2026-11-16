# Definition of Done

**Status:** ✅ v1 — signed off as part of P0.8 · **Owner:** W5 · **Phase:** P0 · **Gate:** G0

A build is not done when it compiles. This is the checklist that decides whether a phase closes.

---

## 1. Per-phase criteria

Each phase closes only when its gate in `phases/pN-*/gate.md` is **PASS**, with the evidence field filled. Nothing here is negotiable at the gate.

| Phase | Closes when |
|---|---|
| P0 | Empty APK installs from `Competition`; invite URL works for a stranger; toolchain versions pinned |
| P1 | Grab-rotate verdict written; ≥8 of 20 shapes track; MRUK anchors; fps baseline measured |
| P2 | A stranger locks one shape uncoached |
| P3 | Near-miss lands in 30 s and never lies; honest mirror ships |
| P4 | Shelf survives relaunch on real furniture |
| P5 | Every shipped pose is credited per `ADR-007` |
| P6 | One-handed, eyes-only and calibrated paths all pass |
| P7 | A novice finishes the first five minutes wordlessly |
| P8 | First lock <75 s with three strangers |
| P9 | Submitted, confirmed, invite verified from a clean device |

## 2. Global done — true of every build after P2

- [ ] Zero crashes over 20 cold launches and 4 hostile conditions
- [ ] **Zero error states anywhere in the UX** (`UX-STATES.md` §5)
- [ ] ≥72 fps on Quest 3S over a 10-minute thermal run
- [ ] Cold boot to first interaction <15 s
- [ ] Every accessibility toggle works and persists
- [ ] `python3 scripts/validate_poses.py` passes
- [ ] `python3 scripts/count.py` passes
- [ ] `python3 scripts/check_trackability.py` passes — the honest mirror's every number traces to
      `trackability.json`, the file carries a date if it claims a measurement, and the panel's sentence
      appears verbatim in `TRACKABILITY.md` (`P3.6`)
- [ ] `python3 scripts/lint_copy.py` passes — no user-facing string reads as a verdict (`P2.3`, `P3.3`)
- [ ] `main` builds from a clean clone

## 3. Never true of a submitted build

| Never | Why |
|---|---|
| A red X, a "try again", or a percentage score | `UX-STATES.md` §5 |
| A handshape the sensor cannot resolve, taught as if it works | `N2` — we publish the edge instead |
| A pose with no `source` field | `ADR-007` |
| A claim in the description the build cannot demonstrate | `COMPETITION-FIT.md` |
| A controller requirement anywhere in the loop | Hands-first is absolute |
| An SDK type referenced inside `Matcher` | `ARCHITECTURE.md` §1 |
| A commit after submission | `P9.8` |

## 4. Sign-off

| Phase | Signed by | Date | Notes |
|---|---|---|---|
| P0 (this definition) | owner | 14 Nov 2026 | Cut ladder in `MASTER-PLAN.md` §6 confirmed as the order we would actually cut in |
| | | | |
