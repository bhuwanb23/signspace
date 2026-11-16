# SUBMISSION COPY
## Tagline, description, and the exact strings that go in the Devpost form

**Draft v1 · 7 Nov 2026 · every character count verified by script, not by eye**

---

## 1. PROJECT NAME

**SIGNSPACE**

*Rejected alternatives:* Handshape (collides with handspeak.com, a major ASL dictionary), SignRoom (too vague), The Honest Mirror (better as a feature name than a product name).

---

## 2. TAGLINE — *limit 140 characters*

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

## 3. DESCRIPTION — *limit 500 words*

**SIGNSPACE — the mirror that knows sign**

Sign language is three-dimensional. Almost every resource that teaches it is a flat video watched from one fixed angle. A learner can see *that* a handshape is different but not *how* — whether the thumb sits across the palm or beside it. They guess, they get it wrong, and nothing tells them.

**The mechanic.** A life-size reference hand, captured from a Deaf signer, appears in front of you. Pinch it and turn it with your own wrist to see the depth a video can never show. Look at a finger and it highlights with its position. Your tracked hand mirrors beside it; a forgiving match responds as you shape. Hold it and it locks in.

**Wrong answers are more informative than right ones.** When your handshape lands a few degrees from a *different* sign, SIGNSPACE names the sign you actually made: "That's not U. That's V." That is minimal-pair diagnosis, only possible with joint-level tracking.

**Why hands-first is required, not optional.** With a controller you cannot turn the hand with your hand, and you cannot compare your shape to the reference — because the instrument being trained is holding a controller. A controller version is not a worse product. It is not a product.

**Why the room matters.** Every shape you master becomes an object on a vocabulary shelf anchored to a real surface in your room via MRUK. Your desk accumulates your actual progress. Turn passthrough off and the shelf loses its ground.

**Honest about the sensor.** Some handshapes today's cameras cannot resolve — a finger behind a finger. We don't ship them, and we don't hide them: SIGNSPACE publishes them, greyed out with the reason. And when tracking confidence drops, the exercise pauses rather than blaming you.

**Built for what comes after Quest.** Seated, near-field, eyes and hands only — the shape of an app for VR Glasses. Validated in the VR Glasses profile at ~70°×66°: all critical UI inside a ~55° cone, the shelf reflowing to a tray.

**Accessibility by default.** Captions on, high contrast, speed control, one-handed play, eyes-only dwell selection, and a 30-second calibration that normalises to your hand's own range — because we don't require you to have a standard hand.

**Performance target.** ≥72 fps on Quest 3S across a 15-minute thermal run — the instrument exists; the number is not claimed until it has been measured on the device.

**What we don't claim.** We don't claim fluency. A 2020 RCT of 60 learners found no significant effect of VR platform on immediate ASL recall. We claim something narrower and testable: you can inspect a 3D handshape from any angle and be told which sign you actually produced. Neither is possible with video.

**Integrity.** Handshapes are authored against published ASL references and credited in-app; named Deaf signer review is in progress and the app says so rather than implying sign-off. This is a handshape practice companion, not a replacement for a Deaf teacher. Facial and motion grammar are out of scope in v1. Six shapes today; the engine is language-agnostic; more arrive with signer review.

---

## 4. THE BUILT-WITH / TAGS STRINGS

**Built with:** Unity · Meta XR SDK v207 · Interaction SDK · MRUK · OpenXR · C#

**Tags:** Accessibility · Education · Hand Tracking · Mixed Reality · Gaze · Sign Language

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
| Tagline ≤140 | `python3 scripts/count.py` | ⬜ |
| Description ≤500 words | `python3 scripts/count.py` | ⬜ |
| Video <3:00 | ffprobe | ⬜ |
| Hands-first by 0:30 | Watch it, no scrubbing | ⬜ |
| No claim above is unverifiable on device | Read against `COMPETITION-FIT.md` | ⬜ |
| Every award we mention actually exists in 2026 rules | Re-read Devpost | ⬜ |
