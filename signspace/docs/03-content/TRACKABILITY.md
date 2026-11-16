# Trackability
**Status:** ✅ v2 published — method, taxonomy, decision, the shipped panel, and §3 generated from `trackability.json`; the *measurement* is P3.6 · **Owner:** W3 · **Phase:** P1 / P3 · **Gate:** G1, G3

> **This document publishes the sensor's blind spots.** It is the measured evidence behind the
> honest mirror panel, and it is the thing that stops us shipping a shape that does not work.
> Every number in the product's honest mirror has to trace to a row in here.

---

## 1. Why it exists

A sign-language app that teaches a shape the camera cannot see is worse than one that teaches
nothing, because the learner cannot tell that it is being taught badly.

So the rule is the opposite of the usual one: **we publish the edge of what works, instead of
hiding it.** That is novelty move `N2`, the honest mirror, and it is a named award criterion
(*Innovation* — publishing your sensor's blind spots is a defensible, unusual thing to do).

**The decision rule, from `SIGNSET.md`:** any shape below about **70% lock rate in normal light** is
removed from the shipped set and moved to the honest mirror. **The set may shrink. It may not
include a shape that fails.**

## 2. The probe — P1.5

Twenty candidate handshapes, tried on real hands, in real rooms, in real light.

### 2.1 The twenty candidates

The six we ship, plus fourteen more we may or may not need:

| Shipped | Candidates to probe |
|---|---|
| B, U, V, W, C, O | A, D, F, I, L, M, N, R, S, T, X, Y, E, G |

**F, G, M, N, R, S, T and X were expected to fail** — F has a thumb-index contact that occludes
itself, M/N/T/R are ambiguous in depth, X is a variant of S, G is orientation-dependent, and E has a
thumb position that tracking cannot resolve. **Probing them anyway is the point**: we want the
*measured* reason for the greyed-out panel, not our opinion of it.

> **Revision, P3.4 (7 Nov 2026).** Reviewing the probe captures moved three of those rows, and the
> document says so rather than quietly editing the list: **F is not excluded** — its contact survives
> when fewer fingers are used, so it is `may-resolve`; **X and S are `may-resolve`**, not expected
> failures; and **E** is `may-resolve` too. The by-design exclusion is now **M, N, R, T** and nothing
> else, which is what the panel prints. The probe can still move any of them — `expected` is a design
> decision, `status` is what the device says, and only P1.5/P3.6 write the second one.

### 2.2 The procedure

| Step | What | Notes |
|---|---|---|
| 1 | Sit in a normal room. **Not a lab.** A living room at 7 p.m. | the judge's environment, not ours |
| 2 | For each shape, hold it for **10 seconds** | long enough to see flicker, short enough to do 20 of them |
| 3 | Record, per shape, per lighting condition | |
| 4 | Repeat the whole set in **daylight** | two lighting conditions, because lamplight is where tracking degrades |
| 5 | Repeat with **two different hands** if available | hand size changes occlusion |
| 6 | For each hold, note the **lock rate**: how many of the 10 s produced a lock | |

**The metric is the lock rate, not the confidence.** Confidence is the sensor's opinion of itself;
the lock rate is whether the thing worked.

### 2.2b How it is actually measured *(P1.5, 15 Nov 2026)*

The procedure above is manual, and a manual procedure recorded from memory is a rumour with numbers
attached. So the probe now measures and asks at the same time — `ProbeController` per 10-second hold:

| Signal | Source | What it detects |
|---|---|---|
| **lock %** | fraction of frames inside the 12° lock band | "tracks, but never locks" vs "tracks and locks" |
| **wrong %** | fraction of frames where a *different* shape is the nearest match (`Matcher.BestOtherId`) | **"tracks as a different shape" — the one dangerous failure** |
| **dropouts** | tracked → lost transitions | **"flickers"** |
| **manual score** | operator, 1–4 pinches, captured at the moment of judging | the human half: does it feel like it is tracking |

**Two things this changed, both before any device data existed:**

1. **The candidates had to be separable by our own metric or the probe would lie.** All 20 references
   were run through the v2 chain metric pairwise first. M and N came out **10.3° apart** — inside the
   lock threshold — because the references had been authored with effectively identical joint angles.
   The app could not have told them apart however well the sensor performed, and the result would have
   been filed as a sensor failure. N was re-authored; the pair now sits at 29.5° / 70.1°.
2. **Attribution is decided in advance.** Seven candidates carry `confidence.authoring: low` (mirrored as `authoringConfidence` in `Probe/probe-meta.json`, which is what the probe rig reads) because they
   depend on contact or depth that a rotation-only format cannot express. A poor result on one of those
   is recorded as **"not separable by this method"**, not as "the sensor cannot see it". The greyed-out
   panel in the honest mirror can honestly say *we could not measure this reliably*.

Raw notes, the twenty-shape table and the four session sheets: [`phases/p1-derisk/probe-raw.md`](../../phases/p1-derisk/probe-raw.md).

### 2.3 What to record

| | |
|---|---|
| **Never tracks** | the shape is not resolvable at all. The worst outcome — the app must never be told the shape is wrong, because it cannot know |
| **Flickers** | it tracks sometimes. **The dangerous category** — it may lock the *wrong* shape, or lock and drop out |
| **Tracks the wrong shape** | the worst possible outcome. If this happens for any shape, that shape is removed and we investigate before shipping anything |
| **Tracks, but never locks** | it tracks correctly but the distance never gets under the lock threshold. A threshold problem, not a tracking problem |
| **Tracks and locks** | fine, and it is what we ship |

> **"Flickers" and "tracks the wrong shape" are the dangerous ones.** A shape that never tracks
> simply never locks, and the learner waits — mildly frustrating but safe. A shape that flickers can
> latch the wrong letter, and the learner is *taught wrong*. `P3.2` is the defence: the near-miss only
> fires above a confidence floor, and a low-confidence hold pauses the exercise rather than locking.

### 2.4 The failure taxonomy, with a real example per class

Every excluded shape fails for one of four reasons. The classes are not theoretical: each has a shape
in `StreamingAssets/trackability.json` and one line in `note` that the panel prints verbatim.

| Class | What the tracker loses | Real example (the `note` the panel shows) | Defence |
|---|---|---|---|
| **Stacking** — one finger hides behind another | depth between two fingers that are 2 cm apart | **M**: *"three fingertips sit on the thumb: a finger hides behind a finger"* · **N**: *"two fingertips on the thumb; differs from M by one stacked finger"* | not shipped; no threshold can recover an unreported joint |
| **Concealment** — a finger is inside another contact | the whole finger, not its angle | **T**: *"the thumb is between two fingers: hidden, not bent"* | not shipped |
| **Unreported articulation** — motion the joint set does not carry | the crossing, which no finger joint angle expresses | **R**: *"crossing is not a joint angle the tracker reports"* | not shipped; a different metric would be needed, not a different threshold |
| **Self-occluding contact** — the shape hides itself | the contact, sometimes | **F** (*resolved*): the contact survives when fewer fingers are used, so F is `may-resolve`, not excluded | re-authored, not excluded |

Two further classes are *risk categories*, not exclusions, and the probe is what decides them:
**curvature without contact** (C, O — where the shape is a smooth arc and the tracker has no edge to
grab) and **orientation dependence** (G — where the palm angle carries the meaning).

> **What this taxonomy is for.** It is the difference between "the cameras are limited" and a reason a
> Deaf judge can check. Every line in the panel is one of these classes, stated about a specific shape.

## 3. The table

**Empty until the probe runs. This is the table, and it is filled on the device, not estimated.**

### 3.1 Quest 3S — normal light

> **Generated from `trackability.json` by `scripts/fill_trackability.py --emit-doc`.** Do not type into
> this table: the panel reads the same file, so anything typed here would be a second version of the
> truth. "not measured" is a real state, not a blank.

<!-- trackability:3.1:begin -->
| Shape | Lock rate | 10 s hold behaviour | Category | Shipped? |
|---|---|---|---|---|
| B | not measured | baseline | fine | yes |
| U | not measured | watch finger separation | fine | yes |
| V | not measured |  | fine | yes |
| W | not measured | ring-finger tracking is the weak link | fine | yes |
| C | not measured | curvature without contact is the risk case | fine | yes |
| O | not measured | contact may help the tracker | fine | yes |
| A | not measured | thumb alongside vs across is the ambiguity | may-resolve | — |
| D | not measured | ring and little meet the thumb | may-resolve | — |
| F | not measured | index tip meets thumb tip; hardest in the set, first candidate for the cut ladder | may-resolve | — |
| I | not measured | isolated little finger is the weakest joint | may-resolve | — |
| L | not measured |  | may-resolve | — |
| M | not measured | three fingertips sit on the thumb: a finger hides behind a finger | not-resolvable | — |
| N | not measured | two fingertips on the thumb; differs from M by one stacked finger | not-resolvable | — |
| R | not measured | crossing is not a joint angle the tracker reports | not-resolvable | — |
| S | not measured | thumb across the front of the fist; competes with A | may-resolve | — |
| T | not measured | the thumb is between two fingers: hidden, not bent | not-resolvable | — |
| X | not measured | hooked index; deferred to v2 in any case | may-resolve | — |
| Y | not measured |  | may-resolve | — |
| E | not measured | every tip at the same point | may-resolve | — |
| G | not measured | orientation-dependent direction, not a handshape | out-of-scope | — |
<!-- trackability:3.1:end -->

### 3.2 The conditions the probe repeats

> Two lightings and both hands, per the procedure in §2.2. The columns the probe actually writes are
> `probe-<condition>-<hand>.jsonl`; a cell that was never run says so.

<!-- trackability:3.2:begin -->
| Shape | daylight / dominant | lamplight / dominant | daylight / other | lamplight / other |
|---|---|---|---|---|
| B | not measured | not measured | not measured | not measured |
| U | not measured | not measured | not measured | not measured |
| V | not measured | not measured | not measured | not measured |
| W | not measured | not measured | not measured | not measured |
| C | not measured | not measured | not measured | not measured |
| O | not measured | not measured | not measured | not measured |
| A | not measured | not measured | not measured | not measured |
| D | not measured | not measured | not measured | not measured |
| F | not measured | not measured | not measured | not measured |
| I | not measured | not measured | not measured | not measured |
| L | not measured | not measured | not measured | not measured |
| M | not measured | not measured | not measured | not measured |
| N | not measured | not measured | not measured | not measured |
| R | not measured | not measured | not measured | not measured |
| S | not measured | not measured | not measured | not measured |
| T | not measured | not measured | not measured | not measured |
| X | not measured | not measured | not measured | not measured |
| Y | not measured | not measured | not measured | not measured |
| E | not measured | not measured | not measured | not measured |
| G | not measured | not measured | not measured | not measured |
<!-- trackability:3.2:end -->

### 3.3 The decision, and where it comes from

The table is empty, so the decision below is traceable to a **row of data** rather than to a row of
measurement. That distinction is written down on purpose — when the numbers arrive, the same table
records what changed and which decision moved with it.

| Category | Shapes | Where it is recorded | What it means for the build |
|---|---|---|---|
| `fine` | B, U, V, W, C, O **(6)** | `trackability.json` → `expected` | shipped and taught |
| `may-resolve` | A, D, F, I, L, S, X, Y, E **(9)** | same | in the probe, not in the build; **the probe decides** |
| `not-resolvable` | **M, N, R, T (4)** | same | excluded by design, on the honest-mirror panel with a reason |
| `out-of-scope` | G **(1)** | same | orientation-dependent; a different feature, not a tracking failure |

Total **20**, which is the denominator in the count line. Nothing in this table is a measurement, and
the panel says so (`measured:false`, §4.2) — but every number in the panel comes from this table, so
there is one place to change and no place to guess.

**How the table gets filled.** `scripts/fill_trackability.py` reads the probe's `probe-*.jsonl` output
and writes this file — including `measured`, the date, and one measured `note` per shape — and
`scripts/check_trackability.py` refuses to pass if the panel's sentence and this document disagree, if a
half-filled file carries a status, or if a `borderline` shape has no repeated holds behind it. The
tables in §3 are generated; nobody types into them.

**The specific rows P3.6 must move**, in the order that matters: `S` and `E` (the two borderline shapes
the task names, three holds each in lamplight), then `F` (re-authored — confirm the contact survives),
then the six `fine` rows (confirm they still lock, which is the claim the whole build rests on).
`M`, `N`, `R`, `T` are expected to stay where they are; if one of them *does* lock, that is a finding,
and the panel loses a shape rather than gaining one.

## 4. From the table to the product

The table in §3 is empty on purpose; the panel does not wait for it. What ships today is the *design
decision* (`expected`) plus one reason per excluded shape — and one guard that stops an unmeasured
number from ever reaching a learner.

### 4.1 The data, not the prose

`unity/Assets/StreamingAssets/trackability.json` — twenty variants, one row each, read by
`Matching/Trackability.cs`:

| Field | Means |
|---|---|
| `expected` | `fine` · `may-resolve` · `not-resolvable` · `out-of-scope` — the decision from `SIGNSET.md` §6, on record before any measurement |
| `status` | `unmeasured` today; `tracks-and-locks` when P1.5's probe has run. **Only the probe writes this** |
| `lockRate` | `-1` until measured. A negative lock rate is not a number, it is a hole |
| `rates` | optional, per condition and hand (`daylight/dominant`, …) — the table in §3.2 |
| `borderline` | true when repeated holds disagreed by more than 20 points. It widens the count to a range instead of rounding the shape into or out of the build |
| `designNote` | why the design expected the shape to fail; kept after measurement so the document can show what changed |
| `note` | one line, per shape, of *why* — this is what the panel shows. Before measurement it is the design reason; after, it is the result (`locked 34% of a 10 s hold; …`) |

`measured` is `false` and stays false until the probe has run on every hand. Counts as they stand:
**6 fine · 9 may-resolve · 4 not-resolvable · 1 out-of-scope**. The four excluded by design are
**M, N, R, T** — the same four the task expected, with two differences worth recording: **S and E
resolve** (they are `may-resolve`, not excluded) and **F was demoted from `not-resolvable` to
`may-resolve`** after review, because the contact survives when fewer fingers are used. The excluded
set is therefore M/N/R/T and nothing else.

### 4.2 The count line, and the guard that keeps it honest

Two states, chosen by the data, never by the writer:

| `measured` | The line |
|---|---|
| `false` *(today)* | **"Four handshapes stay out of this build because the cameras cannot see them. Here is why."** |
| `true` | **"N of 20 handshape variants are robustly trackable today — measured {date} on Quest 3S, normal light."** |
| `true`, with borderline shapes | **"N to N+k of 20 …"** — the range *is* the measurement. Under it: *"Repeated holds disagreed on two; the range is those shapes."* |

and under it, while unmeasured: **"Not measured on every hand yet."**

The unmeasured form deliberately does **not** say "trackable": that would be a tracking claim with no
tracking behind it. It also does not say "4 of the 20 stay out of this build" — because fourteen of the
twenty are not in the build, and only four are out *because the cameras cannot see them*. The first
version of this line said the fraction, and a judge could fairly have asked what happened to the other
ten; the answer ("they were never in it") is true but it is not a count line's job to explain it. The moment `measured` flips, the same code produces the dated form, because a
number without a date is a guess and a number without a wall is a lie. **This flag is the whole point
of the file**: it is cheaper to forget to measure than to forget to ship.

**The line the panel is showing today, verbatim from the file:**

<!-- trackability:line:begin -->
> **"Four handshapes stay out of this build because the cameras cannot see them. Here is why."**
<!-- trackability:line:end -->

### 4.3 The reasons, verbatim from the file

| Shape | Note shown |
|---|---|
| **M** | three fingertips sit on the thumb: a finger hides behind a finger |
| **N** | two fingertips on the thumb; differs from M by one stacked finger |
| **R** | crossing is not a joint angle the tracker reports |
| **T** | the thumb is between two fingers: hidden, not bent |

One line each, from `note`, never from a template. `ReasonFor()` falls back to a category sentence only
if a note is missing, and the fallback is itself a fact ("Not resolvable from the cameras' angle
today.") rather than an apology. The earlier per-category copy — *"We can't see this one yet…"* etc. —
was drafted and then dropped: a category sentence is a general statement, and a general statement is
what this panel exists to replace.

### 4.4 The panel

- **Prefab** `unity/Assets/_Project/Prefabs/HonestMirror.prefab` — `HonestMirror` (component) → `Content`
  (hidden at rest) → `Canvas` (world-space, 0.77 × 0.53 m: count line, reasons, credit) and `Specimens`
  (four world-space slots). The hands are **not** canvas children: `HandVisual` builds in metres and the
  canvas scale (0.0007) would erase them.
- **Reached from an object, not a menu** — `UI/PanelOpener.cs` on a bead atop a plinth stands at
  (0, 0.62, 0.90). Pinch within 12 cm and the panel opens; pinch again and it closes. P4 re-homes that
  object onto the shelf, which is the version the gate is judged on.
- **Closed at rest.** It is something you find. It does not appear when the app starts and it is never
  pushed at anyone.
- **The shapes are the real ones.** Each hand is the `Probe/<id>.json` capture, built by the same
  `HandVisual` and posed with the same `ApplyPose` as the taught shapes, in grey unlit at 0.85 scale.
  `scripts/validate_poses.py` reports **26 shapes, 0 problems** including M, N, R, T — the excluded
  renders are held to the shipped standard, not a lower one.
- **The credit** is `set-meta.json`'s `creditLine`, verbatim (ADR-007). With no meta loaded it says
  *"Handshapes authored against published ASL references. Signer review in progress."* — true today,
  and it stops being true without anyone editing the panel.
- **Reproducible.** `HonestMirrorBuilder` *(Editor menu: SIGNSPACE → P3.4 Build honest mirror)* regenerates
  the prefab, the materials and the plinth, and is the reason the checked-in YAML exists in a shape
  anyone can rebuild.

### 4.5 What the panel must never do

| | Why |
|---|---|
| Say sorry | it is a demonstration of rigour, not a confession |
| Draw a shape wrongly | a wrong specimen discredits the right ones — and "it's excluded anyway" is exactly the reasoning to refuse |
| Print a number with no date | the date is the difference between a measurement and a marketing claim |
| Use red, a cross, or a verdict word | `scripts/lint_copy.py`, 0 violations over 66 literals in 9 files |
| Be a settings screen | a setting is something you configure; this is something you walk up to |

## 5. What this document must never contain

| | |
|---|---|
| A number that is not from a row in §3 | every number in the product traces to a row here |
| A category assigned from opinion | the category comes from a hold, not from a prior |
| "Probably" | if it was not probed, it was not measured, and it does not appear in the table |
| A shape marked shipped that is not in the shipped set | the table and the build are checked against each other |

## 6. Inputs
`SIGNSET.md` §7 (the decision rule and the expectation table) · `MATCH-ALGORITHM.md` (the lock threshold
the lock rate depends on) · `P1.5` (the probe) · `P3.4` (the panel that reads this file) · `P3.6` (the
numbers) · `P3.7` (publishing it) · `NOVELTY-AND-PRIOR-ART.md` N2

## 7. Output / done when
- [x] the probe method, the twenty candidates, and the categories
- [x] the table structure
- [x] the decision rule that connects the table to the shipped set
- [x] the copy for each category of failure *(then dropped — one line per shape, from `note`, P3.4)*
- [x] the rule that every number in the product traces to a row here
- [x] the data file the product reads (`trackability.json`) with an `measured:false` guard
- [x] **the panel ships**: `Prefabs/HonestMirror.prefab`, four excluded shapes drawn from their own
      captures, one line of why each, count line on top, 0 lint violations
- [x] **it is reachable from inside the space**: `PanelOpener` on a plinth — no settings menu
- [~] the honest-mirror number matches the table — *it does, and both say nothing has been measured
      yet; the dated form is unreachable until `measured` flips*. Enforced from now on by
      `scripts/check_trackability.py`, which fails if the panel's sentence is not in this document
- [x] a range instead of a rounding, when repeated holds disagree (`borderline`)
- [x] the doc tables are generated from the data file (`fill_trackability.py --emit-doc`)
- [ ] **the table is filled on the device** *(P3.6)*
- [ ] **the measurement date is recorded** *(P3.6)*
- [ ] **the shipped set is chosen from the table and `SIGNSET.md` is updated to match**
- [ ] **a Deaf signer confirms the four excluded shapes are drawn correctly** *(P3.4 step 5; the poses
      pass `validate_poses.py`, which checks the format, not the handshape)*
