# PHASES

One folder per phase of the [master plan](../docs/00-plan/MASTER-PLAN.md). Each folder holds a charter (`README.md`), a gate (`gate.md`), and one file per sub-phase under `tasks/`.

**Rule:** a phase is only started when its predecessor's gate is green.

| Phase | Day | Budget | Sub-phases | Gate | Folder |
|---|---|---|---|---|---|
| **P0** Foundation & compliance | 1 · Sat 7 Nov | 5 h | 9 | G0 — empty build on the `Competition` channel + stranger's invite URL | [`p0-foundation/`](p0-foundation/) |
| **P1** De-risk spike | 2 · Sun 8 Nov | 8 h | 8 | G1 — grab-rotate feels good; ≥8 of 20 shapes track | [`p1-derisk/`](p1-derisk/) |
| **P2** Core loop | 3 · Mon 9 Nov | 10 h | 7 | G2 — a stranger locks one shape, uncoached | [`p2-core-loop/`](p2-core-loop/) |
| **P3** Feedback layer | 4 · Tue 10 Nov | 8 h | 7 | G3 — near-miss lands in 30 s and never lies | [`p3-feedback/`](p3-feedback/) |
| **P4** Spatial shelf | 5 · Wed 11 Nov | 8 h | 6 | G4 — shelf survives relaunch on real furniture | [`p4-shelf/`](p4-shelf/) |
| **P5** Content & capture | 5–7 · Wed–Fri | 12 h | 7 | G5 — every shape credited to a real Deaf signer | [`p5-content/`](p5-content/) |
| **P6** Gaze & accessibility | 7 · Fri 13 Nov | 7 h | 7 | G6 — one-handed, eyes-only, calibrated paths all pass | [`p6-accessibility/`](p6-accessibility/) |
| **P7** Polish | 8 · Sat 14 Nov | 8 h | 7 | G7 — novice finishes the first five minutes wordlessly | [`p7-polish/`](p7-polish/) |
| **P8** Playtest & iterate | 9 · Sun 15 Nov | 6 h | 5 | G8 — first lock <75 s with three strangers | [`p8-playtest/`](p8-playtest/) |
| **P9** Submission | 10–11 · Mon–Tue | 10 h | 8 | G9 — submitted, confirmed, invite verified clean | [`p9-submission/`](p9-submission/) |
| **P10** Post-submission | → 11 Dec | 4 h | 5 | G10 — entry intact and installable three weeks later | [`p10-post/`](p10-post/) |

## Critical path

`P0 → P1 → P2 → P3 → P7 → P8 → P9 → SUBMIT`, with **P5 (signers)** running in parallel from Day 1 because it is the longest-lead item in the project and the only fatal risk.

## How to work a phase

1. Read the phase `README.md`. Check every input on the list is actually available.
2. Work the `tasks/` files in order. Each one has a **Done when** list — do not proceed on a partial.
3. Tick the phase outputs in the README as they complete.
4. Run the gate in `gate.md`. Record the verdict, the date, and the evidence.
5. Update the status board in `docs/00-plan/MASTER-PLAN.md` §8.

## If a gate goes red

Use the pre-agreed cut ladder (`MASTER-PLAN.md` §6) in order. Never invent a workaround; never skip a rung; never cut a never-cut item.
