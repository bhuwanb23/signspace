# P3 — the feedback layer, reviewed end to end

**Phase:** P3 · Day 4 · 8 h budget · **Gate:** G3 · **Written:** 7 Nov 2026
**Commits:** `3846ed7` P3.1 · `93e82f7` P3.2 · `bed8b75` P3.3 · `e471970` P3.4 · `e0b7957` + `68f9c55`
P3.5 · `200f0ad` P3.6 · `a8b9987` P3.7

> **Verdict: green on everything that can be checked from a desk; amber on the two moves that need a
> second person.** Seven of seven tasks are complete as *work*. Two G3 criteria cannot be closed alone —
> a stranger playing for thirty seconds, and a non-signer reading the near-miss line — and one
> correctness claim (the excluded handshapes are drawn *right*, not merely *well-formed*) needs a signer.
> Nothing in the build lies about any of that, which is the only reason the amber is acceptable.

> **Validation note — 16 Nov 2026.** This review was written before a compiler was available. The
> project has since been compiled (**0 errors**), both prefabs imported, the scene built with the
> coverage meter and the panel, and every simulator below re-run in this workspace: near-miss 8/8,
> false-positive 9 → 1 with the window, pause PASS, poses **28, 0 problems**, trackability 0
> violations, copy lint 0. **No number in this review changed.** What changed is §7's first claim:
> it now describes how the phase closed, not how it stands today.

---

## 1. What P3 is for

G3 asks one question: **does the near-miss land in under thirty seconds of play — and does it never once
lie?** Two novelty moves live in this phase, and both are honesty moves rather than feature moves:

| Move | In one sentence | Failure it exists to prevent |
|---|---|---|
| **N1 the near-miss** | when your hand is inside the lock band on a *different* shape, the app names the shape you actually made and the one thing to change | the learner is corrected toward nothing, or told "wrong" with no way forward |
| **N2 the honest mirror** | a panel in the room publishes the shapes the cameras cannot see, with a reason each, and a count that carries its own date | an app quietly teaching a shape it cannot read |

The phase's real product is not the panel or the sentence. It is the **discipline**: three separate
mechanisms stop the app speaking when it does not know (`NearMiss.Evaluate`, the confidence floor, the
session cap), one mechanism stops it printing an unmeasured number (`measured` in the data file), and one
command checks the copy and the numbers rather than trusting the writer.

---

## 2. The seven tasks

| Task | Claim | Artefact | Evidence | Would be falsified by |
|---|---|---|---|---|
| **P3.1** near-miss | when the hand sits inside the band on exactly one other shape, and that shape is ≥2° nearer, name it | `Matching/NearMiss.cs`, `phases/p3-feedback/near-miss.md` | 8/8 testable pairs fire with the **correct** shape, `D(made)` **2.2–2.7°**; **0** false positives over 2000 sloppy samples; pair distances C/O 65.0 · U/V 26.0 · V/W 118.1 · B/U 117.0 | one pair naming the wrong shape, or a fire on a hand that is not inside the band |
| **P3.2** silence by default | confidence floor, decision-stability window, session cap, off switch | `Matcher` fields + `false_positive_sim.py`, `false-positive-test.md` | floor **0.70** · window **0.40 s** · cap **1 per 20 s**; 18 non-qualifying attempts → **0 fires**; probe 20 → **1 fire**; **with the window removed: 9 fires** | a fire below the floor, or two fires inside 20 s |
| **P3.3** the line | ≤12 words, no verdict, says what to change | `UI/NearMissDisplay.cs`, `copy-variants.md`, `SUBMISSION-COPY.md` §7.1 | shipped **"That's O — 3° away. C opens a little."** — 9 words, worst composed 11, plain form 6; 8 hints derived from the authored deltas | a non-signer reading it twice, or a hint that contradicts the geometry |
| **P3.4** honest mirror | the shapes we cannot see, drawn correctly, greyed, one reason each, reachable in-space | `Prefabs/HonestMirror.prefab`, `UI/HonestMirror.cs`, `UI/PanelOpener.cs`, `Matching/Trackability.cs`, `trackability.json` | **M, N, R, T** excluded; four shapes built from their own `Probe/<id>.json` captures; `validate_poses.py` **26 shapes, 0 problems**; count line refuses to name a tracking figure; opened by pinching a plinth | a wrong specimen, a number without a date, or the panel being a menu |
| **P3.5** the visible pause | the app stops measuring, visibly, and says nothing | `UI/TrackingConfidence.cs`, `BandFeedback.pauseDim`, `pause-behaviour.md` | **0** locks below the floor in occlusion / fast motion / low light; **1 each with the floor removed** (the control); all three resume; threshold strobing **20 → 1** state changes | a lock during a blind window, or a pause that loses the held progress |
| **P3.6** the numbers | the panel cannot print an unmeasured number | `scripts/fill_trackability.py`, `scripts/check_trackability.py`, `trackability.json` | gate passes on the honest file; **four deliberately broken files each fail** (half-filled, no date, fake borderline, drifted doc); synthetic run produced `14 to 15 of 20 …` | a `measured:true` with no ISO date, or a doc that disagrees with the panel |
| **P3.7** publish | the measurement is written where the submission draws from | `TRACKABILITY.md` §2.4, §3, §3.3, §4; `SIGNSET.md` §7 | **zero** `[TBD]`, **zero** blank cells, §3 generated from the data file (20/20 rows), taxonomy with one real example per class, one authority for the decision | a decision edited in two places, or a table typed by hand |

---

## 3. The evidence that actually ran

Everything below was run for this review, in this workspace, on 7 Nov 2026.

| Command | Result |
|---|---|
| `python3 scripts/lint_copy.py` | **0 violations**, 66 literals in 9 files, 2 dev views confirmed gated |
| `python3 scripts/check_trackability.py` | **0 violations** — the panel prints *"Four handshapes stay out of this build because the cameras cannot see them. Here is why."* |
| `python3 scripts/validate_poses.py` | **26 shapes, 0 problems** (28 after the K/P facing pair was added) |
| `python3 scripts/hold_sim.py --cases` | **16/16** state-machine cases |
| `python3 scripts/pause_sim.py` | **PASS** — 0 blind locks ×3, control 1 each, hysteresis 20 → 1 |
| `python3 scripts/nearmiss_sim.py` | closest probe pair **R/U 14.2°**; midpoint hand → *ambiguous, silent* |
| `python3 scripts/false_positive_sim.py` | **9 fires** without the 400 ms window, 1 with it |
| C# brace/paren sweep, 60 files | balanced at the time; **no compiler available then** — compiled later with 0 errors, see §7 |

Two of these are worth reading as arguments rather than numbers. **The false-positive control** (9 fires
without the window) is the reason the window exists; **the pause control** (one blind lock per scenario
with the floor removed) is the reason the floor is not decoration. A test that only ever reports zero is
a test of nothing.

---

## 4. The copy, in full

The words are the feature, so they belong in the review rather than only in a table elsewhere.

**The near-miss** — `{1}` is what the hand is actually doing, `{2}` is how far inside the band it was:

> **"That's O — 3° away. C opens a little."**

Eight pair hints ship (C→O *opens*, O→C *closes the circle*, V/U, U/V, W/V, V/W, U/B, B/U). Four pairs
(A/O, I/Y, L/Y, D/F) have **no** hint because their partners are not in the shipped set — the app says
only its first sentence rather than inventing a correction it cannot derive.

**The honest mirror**, today:

> **"Four handshapes stay out of this build because the cameras cannot see them. Here is why."**
> *Not measured on every hand yet.*
> **M** three fingertips sit on the thumb: a finger hides behind a finger · **N** two fingertips on the
> thumb; differs from M by one stacked finger · **R** crossing is not a joint angle the tracker reports ·
> **T** the thumb is between two fingers: hidden, not bent

and the moment the probe runs, the same code says: *"N of 20 handshape variants are robustly trackable
today — measured 18 Nov 2026 on Quest 3S, normal light."* — or `N to N+k` when holds disagree.

---

## 5. Eight things that were wrong, and what caught each

This is the most useful part of the review. None of these were caught by the thing that was supposed to
catch them.

| # | What was wrong | What caught it |
|---|---|---|
| 1 | The near-miss used a proximity rule; documented pairs are **26–118° apart**, so "close" was meaningless | P3.1's pair-distance measurement, before shipping |
| 2 | The near-miss showed the **target's gap** as the magnitude — a number about the wrong hand | re-reading the spec's wording against `D(made)` |
| 3 | *(P3.2)* the stability window covered the distance but not the **decision** — a sweeping hand named every shape it passed | the 20-attempt false-positive test, 9 fires |
| 4 | **"That's not C. That's O — 3° away."** passed `lint_copy.py` and still failed the standard: negative framing, no action | a human reading it as a learner would. The lint is a floor, and this is the example the phase quotes |
| 5 | The C/O hint was **backwards** (C is the *more open* shape; O closes) | comparing the authored joint angles before writing the hint |
| 6 | *(P3.4)* the earlier draft copy table was per-category — a general statement, in the panel built to replace general statements | writing the panel's first real line |
| 7 | *(P3.4)* "4 of the 20 handshapes we tested stay out of this build" — true of four, false of fourteen | reading it back as a judge would, in P3.6 |
| 8 | *(P3.5)* the match display **could not be dimmed at all** (`MaterialPropertyBlock` overwrote the material), and the dim itself scaled **alpha** on an opaque material — implemented, compiled, invisible | reading the code back after the task was "done" (`68f9c55`) |

Also recorded, both in P3.4: **F was demoted** out of `not-resolvable` (its contact survives with fewer
fingers) and **S/E/X resolve**, so the excluded set is narrower than the brief predicted. The document
carries a revision note saying which rows moved rather than quietly editing the list.

---

## 6. The gate, criterion by criterion

| G3 criterion | Status | Why |
|---|---|---|
| All 8 documented pairs fire | **[~]** | 8/8 testable cells fire correctly in simulation; **never fired on a real hand** |
| Zero false positives over 20 sloppy attempts | **[x]** | 0 fires on 18 non-qualifying, 1 on the qualifying, 0 across 2000 samples |
| Confidence gate, session cap, off switch | **[x]** | 0.70 / 1-per-20 s / `SetNearMissEnabled` persisted in `Start()` |
| Copy ≤12 words, understood by a non-signer | **[~]** | 9 words shipped, worst 11; **the non-signer read is owed** |
| Honest mirror ships, in-space, correct renders | **[x]** *(with a caveat)* | prefab + plinth opener + four real captures, validator-clean; **handshape correctness wants a signer** |
| Published count matches measured data, with a date | **[~]** | enforced by `check_trackability.py`; **the measurement is owed** |
| No lock below the confidence floor | **[x]** | asserted in shipped code (`HoldTimer.SelfTest` case 8) and measured against a control |
| A stranger, 30 s, explains what it told them | **[ ]** | needs a stranger |

**Verdict: G3 cannot go green from this desk.** Three criteria need a person or a headset. What can be
said is that none of them is blocked by missing work — each has a run sheet and, in two cases, a script.

---

## 7. What cannot be closed here, and the highest risk

**At the time of writing, nothing had ever been compiled.** That was true for the whole phase: the
prefab `HonestMirror.prefab` was hand-authored YAML, internally consistent but unimported, and
`HonestMirrorBuilder` existed so one menu command could rebuild it if the YAML was wrong.

**Resolved 16 Nov 2026.** The tree now compiles with **0 errors**, the prefab imports, `SceneBuilder`
builds a scene containing the panel and its plinth opener, and `BuildAndroid` produces APK v13. The
builder was needed exactly once: a cold batch import reported the committed prefab missing, and the
scene was written without it until `AssetDatabase.Refresh()` was added. That is the failure this
section predicted, caught by running the build rather than trusting the file.

**Highest risk in the phase:** the **30-second stranger criterion** collides with P3.2's session cap.
The near-miss fires at most **once per 20 s**, and only when the hand lands inside the band on one of the
eight qualifying pairs — so a stranger handed a headset may simply not meet one in thirty seconds through
no failure of the feature. Two options before playtest (P8): order the first target so a documented pair
is reachable in the first few attempts, or accept a 60-second observation window and say so. **This is a
design decision, not a bug**, and it is the single thing most likely to turn G3 red for a reason nobody
expects.

Other open items, in the order they would bite:

1. The plinth is not yet on the shelf — P4 re-homes it. Until then the panel is reachable from a
   standalone object, which satisfies P3.4's wording but not the shelf's.
2. Four hand slots in the prefab means the panel can *draw* four excluded shapes. If measurement ever
   excludes a fifth, the fifth gets a reason line and no hand — which is honest but thin.
3. The lamp's position (0.20, 1.42, 0.62) and colour are build-time guesses; neither has been seen.
4. Only the six *shipped* shapes are intended to be signer-verified; the twenty probe poses are
   reference-authored, including the four the panel publishes as specimens.
5. The credit line falls back to *"Handshapes authored against published ASL references. Signer review
   in progress."* when `set-meta.json` is absent — accurate today, and it will keep being printed after a
   signer has reviewed unless someone updates the meta. That fallback is a claim with an expiry.

---

## 8. What a judge would see, in thirty seconds

1. Reference hand shows **C**; they copy it slightly wrong and make an **O**. The line says *"That's O —
   3° away. C opens a little."* *(the four seconds the video opens with)*
2. They hold C correctly: the meter goes still, colour warms to locked, and 0.7 s later the shape becomes
   an object. Nothing is scored, nothing is red.
3. They pass their other hand in front: the meter stops breathing and dims, the lamp dims, the hold
   freezes — and resumes without a word when the hand comes back.
4. They pinch the bead on the plinth: a panel appears naming four shapes the cameras cannot see, with a
   reason each, and a count that says it has not been measured on every hand yet.

Every one of those four beats is a claim the app makes about itself, and each has a mechanism behind it.

---

## 9. What P3 hands to the rest of the project

| Hand-off | To | State |
|---|---|---|
| The plinth object → the shelf | **P4** | built, positioned, one `PanelOpener`; needs re-parenting |
| Accessibility toggle for the near-miss already persisted (`save.Data.nearMissEnabled`) | **P6.5** | setting exists; the in-space toggle does not |
| The near-miss as the video's cold open | **P9** | copy frozen in `SUBMISSION-COPY.md` §7.1; both directions in shot 7 |
| The honest mirror as shot 11 | **P9** | doc + panel done; **the number in the shot depends on P3.6** |
| `fill_trackability.py` / `check_trackability.py` | **P1.5, P9** | the probe's numbers now have exactly one destination, and one gate |
| Trackability document | **P9 narrative** | §2.4 taxonomy is the "engineering depth" paragraph |

---

## 10. Verdict

**P3 is the strongest phase in the project so far, and the least verified.** Its ideas are cheap to demo
and hard to fake, its copy has been through the machine and through a human, its numbers are gated by
scripts rather than promises, and eight separate things were caught and fixed because someone read the
work back instead of trusting the task's checkboxes. What it does not have is a single frame of evidence
from a headset, and the phase's own honesty rule means that has to be stated in the same breath as the
green ticks.

**Next, in order:** P3.2's rerun on a real hand → P3.6's thirty minutes → the signer's eye on four
renders → the non-signer read → the stranger test at P8. Everything else in P3 is done.
