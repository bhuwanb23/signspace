# SUBMISSION COPY
## Tagline, description, and the exact strings that go in the Devpost form

**Draft v1 Â· 7 Nov 2026 Â· every character count verified by script, not by eye**

---

## 1. PROJECT NAME

**SIGNSPACE**

*Rejected alternatives:* Handshape (collides with handspeak.com, a major ASL dictionary), SignRoom (too vague), The Honest Mirror (better as a feature name than a product name).

---

## 2. TAGLINE â€” *limit 140 characters*

### Recommended (118 chars)

```
Every app that teaches sign language is a video with extra steps. This one tells you which sign you accidentally made.
```

**Why it wins:** it opens with a verdict on the entire existing market, names a behaviour that is impossible in 2D, and lands a small joke. Judges remember jokes.

### Alternates

| # | Tagline | Chars | When to use |
|---|---|---|---|
| B | Hand tracking's real trick isn't grabbing things. It's telling you that you just signed the wrong letter. | 111 | If the panel skews technical |
| C | Sign language is 3D. Every video that teaches it is flat. Grab the reference hand, turn it, match it with yours. | 117 | If the panel skews design |
| D | A mirror that knows sign. It turns to show you what video can't, and tells you which sign you actually made. | 109 | Softer, accessibility-forward |

---

## 3. DESCRIPTION â€” *limit 500 words*

**SIGNSPACE â€” the mirror that knows sign**

Sign language is three-dimensional. Almost every resource that teaches it is a flat video watched from one fixed angle. A learner can see *that* a handshape is different but not *how* â€” whether the thumb sits across the palm or beside it. They guess, they get it wrong, and nothing tells them.

**The mechanic.** A life-size reference hand, captured from a Deaf signer, appears in front of you. Pinch it and turn it with your own wrist to see the depth a video can never show. Look at a finger and it highlights with its position. Your tracked hand mirrors beside it; a forgiving match responds as you shape. Hold it and it locks in.

**Wrong answers are more informative than right ones.** When your handshape lands a few degrees from a *different* sign, SIGNSPACE names the sign you actually made: "That's not C. That's O â€” your thumb is 8Â° too far around." That is minimal-pair diagnosis, only possible with joint-level tracking.

**Why hands-first is required, not optional.** With a controller you cannot turn the hand with your hand, and you cannot compare your shape to the reference â€” because the instrument being trained is holding a controller. A controller version is not a worse product. It is not a product.

**Why the room matters.** Every shape you master becomes an object on a vocabulary shelf anchored to a real surface in your room via MRUK. Your desk accumulates your actual progress. Turn passthrough off and the shelf loses its ground.

**Honest about the sensor.** Some handshapes today's cameras cannot resolve â€” a finger behind a finger. We don't ship them, and we don't hide them: SIGNSPACE publishes them, greyed out with the reason. And when tracking confidence drops, the exercise pauses rather than blaming you.

**Built for what comes after Quest.** Seated, near-field, eyes and hands only â€” the shape of an app for VR Glasses. Validated in the VR Glasses profile at ~70Â°Ã—66Â°: all critical UI inside a ~55Â° cone, the shelf reflowing to a tray.

**Accessibility by default.** Captions on, high contrast, speed control, one-handed play, eyes-only dwell selection, and a 30-second calibration that normalises to your hand's own range â€” because we don't require you to have a standard hand.

**Performance.** â‰¥72 fps on Quest 3S across a 15-minute thermal run.

**What we don't claim.** We don't claim fluency. A 2020 RCT of 60 learners found no significant effect of VR platform on immediate ASL recall. We claim something narrower and testable: you can inspect a 3D handshape from any angle and be told which sign you actually produced. Neither is possible with video.

**Integrity.** Handshapes are authored against published ASL references and reviewed by a Deaf signer before shipping. This is a handshape practice companion, not a replacement for a Deaf teacher. Facial and motion grammar are out of scope in v1. 6 shapes today (B, U, V, W, C, O); the engine is language-agnostic; 60 arrive at launch - **Q1 2027.**

---

## 4. THE BUILT-WITH / TAGS STRINGS

**Built with:** Unity Â· Meta XR SDK v207 Â· Interaction SDK Â· MRUK Â· OpenXR Â· C#

**Tags:** Accessibility Â· Education Â· Hand Tracking Â· Mixed Reality Â· Gaze Â· Sign Language

**Platform:** Meta Quest 3 / 3S. **Rating:** 10+.

---

## 5. THE THREE SENTENCES TO REPEAT EVERYWHERE

1. **You cannot rotate a video.**
2. **This is the first app whose content is your own hand.**
3. **It tells you which sign you accidentally made.**

Put #1 in the video, #2 in the description's second paragraph, #3 in the tagline and on the thumbnail.

---

## 6. CHECKS BEFORE SUBMIT *(P9.5)*

| Check | Method | Pass |
|---|---|---|
| Tagline â‰¤140 | `python3 scripts/count.py` | â¬œ |
| Description â‰¤500 words | `python3 scripts/count.py` | â¬œ |
| Video <3:00 | ffprobe | â¬œ |
| Hands-first by 0:30 | Watch it, no scrubbing | â¬œ |
| No claim above is unverifiable on device | Read against `COMPETITION-FIT.md` | â¬œ |
| Every award we mention actually exists in 2026 rules | Re-read Devpost | â¬œ |

---

## 7. IN-PRODUCT COPY

The strings the app itself says. Every one is â‰¤ 12 words, carries no verdict word, no exclamation mark and
no percentage â€” enforced by `scripts/lint_copy.py` (0 violations across 66 literals in 9 files â€” an upper bound: the scan takes in status keys and file names too), which is a
floor rather than a standard: **the near-miss line passed the lint while failing the rule** (Â§7.1).

### 7.1 Near-miss *(P3.1, P3.2, P3.3)*

| | |
|---|---|
| Shipped | **"That's O â€” 3Â° away. C opens a little."** |
| Plain form, high-contrast / text-only *(P6.5)* | **"That's O. C opens a little."** |
| Fallback, pair with no authored hint | **"That's O â€” 3Â° away."** |
| Replaced | ~~"That's not C. That's O â€” 3Â° away."~~ â€” negative framing, nothing to do |

`{1}` is the shape the hand actually made (the app only says this when the hand is inside the lock band on
it), `{2}` is how many degrees away that shape was, and the closing clause is the pair's own difference.
Eight hints ship (C/O, U/V, V/W, B/U both directions); four pairs (A/O, I/Y, L/Y, D/F) have none because
their shapes are not in the set yet, and the app says only what it knows rather than inventing a
correction.

**Status: pending non-signer confirmation.** One reader has picked the line (P3.3 Â§3); the two-reader test
in `phases/p3-feedback/copy-variants.md` Â§5 has not run. Recorded so this is never mistaken for tested copy.

### 7.2 The honest mirror *(P3.4)*

Three strings ship, all data-driven. It is a panel, not a sentence: the count line is computed from
`StreamingAssets/trackability.json` and the reasons are that file's own `note` per shape.

| Slot | Today (`measured:false`) | When the probe has run |
|---|---|---|
| Count line | **"Four handshapes stay out of this build because the cameras cannot see them. Here is why."** | **"N of 20 handshape variants are robustly trackable today â€” measured {date} on Quest 3S, normal light."** â€” or **"N to N+k"** when holds disagreed |
| Provenance | **"Not measured on every hand yet."** | *(dropped â€” the dated line carries its own provenance)*, unless a shape is borderline: *"Repeated holds disagreed on two; the range is those shapes."* |
| Reasons Ã—4 | M Â· N Â· R Â· T, one line each from the file, e.g. **"R   crossing is not a joint angle the tracker reports"** | the same four slots, now carrying the measured result: *"locked 14% of a 10 s hold"* |

The unmeasured line used to read *"4 of the 20 handshapes we tested stay out of this build"*. That was
true of four shapes and false of fourteen: the others were never in the build. A count line cannot afford
a sentence a judge has to reconcile, so it names the four and the reason and stops.

The credit slot renders `set-meta.json`'s `creditLine` verbatim (Â§7.3). With no meta loaded it says
*"Handshapes authored against published ASL references. Signer review in progress."*

**Why the count line is longer than 12 words.** The 12-word rule (Â§7.4) exists for the line that lands at
the emotional low point, mid-play. This is a panel you walk up to and read; its whole job is one
sentence of number plus one line of reason per shape. The rule it does obey is the harder one: **no
number without a date.** The unmeasured form names no tracking figure at all, because there is none.

Plus, while the left hand is off (`HandSupport`, P2.7):

> **"This build reads your right hand. Left-hand play is not in this version."**

### 7.3 Credits *(ADR-007: authorship is data, not copy)*

The panel renders `set-meta.json` verbatim. The sentence that never changes:

> **"This is practice software, not a replacement for a Deaf teacher."**

### 7.4 Copy rules, for whoever writes the next line

| Rule | Why |
|---|---|
| â‰¤ 12 words | it lands at the emotional low point; a paragraph is a lecture |
| No verdict words | no *wrong / error / missed / try again* â€” the app describes, it does not grade |
| No exclamation marks | enthusiasm at someone's mistake reads as mockery |
| Never a percentage | a score invites optimising a number instead of making a shape |
| One instruction, derived | if the line says what to change, that change must come from the authored deltas, not from the writer's memory of the alphabet |

---
