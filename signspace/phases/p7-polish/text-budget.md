# P7.2 — Text budget

**Phase:** P7 · **Measured by:** `SIGNSPACE/P7.2 Check text budget` · **Date:** 17 Nov 2026

## What is counted, and what is not

A word budget that counts every glyph is a budget that can only be met by hiding the credit, the
disclaimer and the near-miss line — the three sentences the product exists to say. So the count is
**instructional words**: words that tell the user what to do. Everything else is listed, with its
category, so nothing is hidden and the categories can be argued with.

| Category | Counted against the 3-word budget? | Why |
|---|---|---|
| Instructional copy ("Pinch the bead", "Turn the hand") | **yes** | this is what spatial teaching replaces |
| Settings labels on the accessibility tray | no | they name a setting, not an action; a user looking for captions needs the word "Captions" |
| The credit line and the fixed "not a replacement" sentence | no | `P5.5` and `G5` require them; they are survivors, not instructions |
| The near-miss line ("That's O — 3° away. C opens a little.") | no | it is the product's signature sentence, and it is a comment on what happened, not an instruction |
| Fallback explanations (`GAZE-INPUT.md` §5) | no | honesty about a missing capability is not onboarding copy |
| `DevOnly` overlays | no | removed from a release build by `DevOnly` |

## The measurement

Run `SIGNSPACE/P7.2 Check text budget`. It walks every `Text` in the saved scene, prints its path and
its category, and sums the instructional words that are **visible at start** (active, not dev-only,
not a survivor).

```
instructional words visible at start: 0
```

Zero is not a trick: the instructional sentence count in the build is zero because the loop is taught
by the reference hand and one pulse. What is on screen in the first five minutes is:

| Object | Text | Category |
|---|---|---|
| `TrackingLamp` | none | — |
| `OnboardingPulse` | none | — (a breathing halo on the reference hand) |
| `NearMiss/Line` | empty until a near-miss fires | survivor, silent by default |
| `Credits/Content` (opens once at 20 s) | authorship state + fixed sentence | survivor |
| `HonestMirror/Content` (opens on a pinch) | what the cameras cannot see | survivor |
| `A11yHud` lines | empty unless a finger is looked at or a fallback exists | survivor/state |
| `A11yToggleTray` (8 blocks) | 7 setting names + "Calibrate" | settings labels |

## The three survivors

1. the credit line (and the "not a replacement" sentence that is appended to it),
2. the near-miss template,
3. the fallback explanation.

All three are in the build and all three are asserted by the check.

## Cold start to first interaction

| Row | Value | Evidence |
|---|---|---|
| Editor play-mode to first interactive frame | not a cold boot; deliberately excluded from the CSV | `BootProbe` skips `Application.isEditor` |
| Device cold boot, median of 5 | **NOT RUN** | needs the headset, `HUMAN-TASKS` H24 |
| Splash animation | static (one frame, no dolly, no zoom) | `SplashSettings.IsStatic`, asserted by `P7.5 Check boot readiness` |
| Boot timeline source | `BootProbe` writes `startup_ms, hands_ms, interactive_ms` per launch | `Application.persistentDataPath/boot-times.csv` |

The `<15 s` target is a device claim. Nothing in this document asserts it.

## What replaced the words

| Before | After |
|---|---|
| a label saying "grab the hand" | the reference hand itself, plus `OnboardingCue`: a 1.2 s breathing halo at 5.5 cm that stops on the first grab |
| a label saying which shape to make | the shape is the target; the reference hand shows it |
| a label saying "hold it" | the band colour and the band tones (`BandAudio`), which are non-text by construction |
| a label saying "you were close" | the near-miss line, which is the one sentence allowed to speak |

## Known limits

- The tray's eight labels are always visible. A low-vision user needs them, and a sighted user reading
  "High contrast" is not being instructed. If a later pass decides they count, the answer is a
  pinch-summoned tray, not smaller type.
- The near-miss line is 5–12 words. It is the only multi-word sentence the product says to a learner,
  and it is bounded by `P3.3`'s 12-word rule rather than by this budget.
