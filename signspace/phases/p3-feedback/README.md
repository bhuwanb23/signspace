# P3 — Feedback layer

**Day 4 (Tue 10 Nov) · Budget 8 h · Gate:** G3 — Does the near-miss land in under 30 seconds of play — and does it never once lie?
**Status:** ✅ **P3.1–P3.7 complete as work; P3.6's measurement is owed** — near-miss, gates, copy, honest mirror, visible pause, published document and the fill/check pipeline. What is left needs a headset: see [phase-3-review.md](phase-3-review.md) §7

**Phase review:** [`phase-3-review.md`](phase-3-review.md) — every task against its evidence, the gate
criterion by criterion, the eight things that were wrong and what caught each, and what still needs a
headset or a second person.

## Objective
Build the layer that makes this submission different from every prior project in the field: the near-miss diagnosis, the honest mirror, and the discipline that keeps both truthful.

## Why it sits here
This phase is the submission's identity, not its polish. Two of the five novelty moves live here, and the video's first four seconds are shot in this phase.

## Sub-phases

| # | Task | Est | Workstream | Status |
|---|---|---|---|---|
| [P3.1](tasks/p3.1-near-miss-table.md) | Build the near-miss: name the sign they actually made | 2.5 h | W2 | ✅ 4/8 pairs testable (6 shapes missing), 0/2000 false positives, ambiguity proven silent |
| [P3.2](tasks/p3.2-confidence-gate.md) | Confidence gating: silence is the default | 1.5 h | W2 | ✅ 0/18 false positives, 400 ms window over the decision too; 9 fires without it |
| [P3.3](tasks/p3.3-copy.md) | Write the near-miss copy | 45 min | W4 | ✅ 9 words, 8 pair hints derived from the deltas; non-signer test still owed |
| [P3.4](tasks/p3.4-honest-mirror.md) | Build the honest mirror: publish the shapes we can't see | 1.5 h | W3 | ✅ panel + `HonestMirror.prefab` + plinth opener; 4 excluded shapes drawn from their captures, `measured:false` keeps the count line honest; signer check owed |
| [P3.5](tasks/p3.5-confidence-readout.md) | Live confidence readout that pauses instead of failing | 1 h | W2 | ✅ lamp dims + breathes, no words; meter goes still and dim (`BandFeedback.pauseDim`); 0 locks below the floor in 3 scenarios (1 each as control); hardware run owed |
| [P3.6](tasks/p3.6-numbers.md) | Verify the honest-mirror numbers on device | 30 min | W3 | 🟡 **tooling done, measurement blocked on a headset** — `fill_trackability.py` + `check_trackability.py`; range phrasing for borderline shapes; doc tables generated; 30 min on device left |
| [P3.7](tasks/p3.7-trackability-publish.md) | Publish the trackability document | 30 min | W3 | ✅ taxonomy §2.4 · decision §3.3 · §3 tables generated (20/20 rows, "not measured") · SIGNSET §7 · no `[TBD]` |

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

## Results

| | |
|---|---|
| Tasks | **7 of 7 complete as work**; P3.6's measurement owed |
| Near-miss | 8/8 testable pairs, correct shape, D(made) 2.2–2.7°; 0 false positives over 2000 sloppy samples; 9 fires without the 400 ms window |
| Copy | "That's O — 3° away. C opens a little." — 9 words, worst composed 11; 0 lint violations over 66 literals in 9 files |
| Honest mirror | M, N, R, T drawn from their own captures (28 shapes: 26 + the K/P facing probe, 0 problems); count line refuses to name a tracking figure until `measured` flips |
| Visible pause | 0 locks below the floor in three degradations; 1 each with the floor removed; hysteresis 20 → 1 |
| Numbers | `fill_trackability.py` + `check_trackability.py`; four broken files each rejected |
| Gate G3 | 3 of 8 criteria can be closed from a desk; 3 need a headset or a person; 1 is red until a stranger plays |
| Verified 16 Nov | **compiled, 0 errors**; both prefabs import and the scene builds with the payoff and panel; 12 simulator/validator scripts re-run green; reference hand **45 / 20 / 19 / 215 mm** (`HandRigCheck`); APK v13 built |
| Still not verified | every device number, the non-signer read, the signer's eye on four renders, a real hand firing a near-miss |
