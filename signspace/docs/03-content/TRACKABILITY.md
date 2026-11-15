# Trackability
**Status:** ✅ v1 (method) — **the table is filled by the P1.5 probe** · **Owner:** W3 · **Phase:** P1 / P3 · **Gate:** G1, G3

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

**F, G, M, N, R, S, T and X are expected to fail** — F has a thumb-index contact that occludes
itself, M/N/T/R are ambiguous in depth, X is a variant of S, G is orientation-dependent, and E has a
thumb position that tracking cannot resolve. **Probing them anyway is the point**: we want the
*measured* reason for the greyed-out panel, not our opinion of it.

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
2. **Attribution is decided in advance.** Seven candidates carry `authoringConfidence: low` because they
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

## 3. The table

**Empty until the probe runs. This is the table, and it is filled on the device, not estimated.**

### 3.1 Quest 3S — normal light

| Shape | Lock rate | 10 s hold behaviour | Category | Shipped? |
|---|---|---|---|---|
| B | ⬜ | ⬜ | ⬜ | ⬜ |
| U | ⬜ | ⬜ | ⬜ | ⬜ |
| V | ⬜ | ⬜ | ⬜ | ⬜ |
| W | ⬜ | ⬜ | ⬜ | ⬜ |
| C | ⬜ | ⬜ | ⬜ | ⬜ |
| O | ⬜ | ⬜ | ⬜ | ⬜ |
| A | ⬜ | ⬜ | ⬜ | — |
| D | ⬜ | ⬜ | ⬜ | — |
| F | ⬜ | ⬜ | ⬜ | — |
| I | ⬜ | ⬜ | ⬜ | — |
| L | ⬜ | ⬜ | ⬜ | — |
| M | ⬜ | ⬜ | ⬜ | — |
| N | ⬜ | ⬜ | ⬜ | — |
| R | ⬜ | ⬜ | ⬜ | — |
| S | ⬜ | ⬜ | ⬜ | — |
| T | ⬜ | ⬜ | ⬜ | — |
| X | ⬜ | ⬜ | ⬜ | — |
| Y | ⬜ | ⬜ | ⬜ | — |
| E | ⬜ | ⬜ | ⬜ | — |
| G | ⬜ | ⬜ | ⬜ | — |

### 3.2 The other three conditions

| Shape | Quest 3, normal | Quest 3S, daylight | Quest 3S, lamplight | second hand, smaller |
|---|---|---|---|---|
| B | ⬜ | ⬜ | ⬜ | ⬜ |
| U | ⬜ | ⬜ | ⬜ | ⬜ |
| V | ⬜ | ⬜ | ⬜ | ⬜ |
| W | ⬜ | ⬜ | ⬜ | ⬜ |
| C | ⬜ | ⬜ | ⬜ | ⬜ |
| O | ⬜ | ⬜ | ⬜ | ⬜ |

## 4. From the table to the product

**The shipped set** is chosen from §3.1 by the decision rule, not by a wishlist.

**The honest mirror panel** shows the greyed-out shapes and their measured category. The copy is
written per shape from the category, not from a general statement:

| Category | What the panel says |
|---|---|
| Never tracks | *"We can't see this one yet — the shape hides itself from the camera."* |
| Flickers | *"This one loses tracking in some light. We left it out rather than teach it badly."* |
| Tracks the wrong shape | *"We could read this one, but not reliably enough to teach it."* |
| Tracks but never locks | *"We can see this one, but the match is too tight for it today."* |

**The number that goes in the product.** `NOVELTY-AND-PRIOR-ART.md` proposed *"x of y handshape-family
variants are robustly trackable today"*. The real number comes from the table:

> *"**4 of 20** handshapes we tested are reliably trackable on a Quest 3S in normal light. Here are
> the four."*

With a **measurement date** next to it. A number without a date is a guess.

**The count is not fixed in advance.** If the probe finds six shapes that work, the number is six and
the shipped set is those six. If it finds three, the number is three — and the cut ladder rung 5
(*"12 signs → 6 signs, 1 family"*) has already been cut, so a third rung would need an explicit
decision.

## 5. What this document must never contain

| | |
|---|---|
| A number that is not from a row in §3 | every number in the product traces to a row here |
| A category assigned from opinion | the category comes from a hold, not from a prior |
| "Probably" | if it was not probed, it was not measured, and it does not appear in the table |
| A shape marked shipped that is not in the shipped set | the table and the build are checked against each other |

## 6. Inputs
`SIGNSET.md` (the decision rule) · `MATCH-ALGORITHM.md` (the lock threshold the lock rate depends on) ·
`P1.5` (the probe) · `P3.7` (publishing it) · `NOVELTY-AND-PRIOR-ART.md` N2

## 7. Output / done when
- [x] the probe method, the twenty candidates, and the categories
- [x] the table structure
- [x] the decision rule that connects the table to the shipped set
- [x] the copy for each category of failure
- [x] the rule that every number in the product traces to a row here
- [ ] **the table is filled on the device**
- [ ] **the measurement date is recorded**
- [ ] **the shipped set is chosen from the table and `SIGNSET.md` is updated to match**
- [ ] **the honest-mirror number matches the table**
