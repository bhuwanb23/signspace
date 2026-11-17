# Accessibility
**Status:** ✅ built · **Owner:** W3 · **Phase:** P6 · **Gate:** G6

> **Accessibility cannot be a settings panel at the end.** This app is about a communication-access
> technology, made by people who are not Deaf, for people who are learning something that Deaf people
> already do. The accessibility work is not a compliance checkbox — it is the difference between a
> product and a demo. `Best Accessibility Forward` is a **$25k award with no incumbent template.**
>
> **What this document is.** It describes the build, not the intention. Every claim below is traceable
> to a file in this repository or to the recording named beside it. Where the claim needs a headset and
> a body, the row says **NOT RUN** — those are the rows that are still owed, and they are listed in
> `../00-plan/HUMAN-TASKS.md`.

---

## 1. The five modality paths

Each path must complete **the whole loop** — boot, inspect, rotate, match, lock, see the shelf — not
just one interaction. A path that can select but cannot lock is not a path.

| # | Path | Who it is for | What it removes | Built | Device evidence |
|---|---|---|---|---|---|
| 1 | **Full** — both hands, gaze | everyone | nothing | ✅ | NOT RUN — `assets/raw/video/full-loop.mp4` |
| 2 | **One-handed** | an injury, a disability, a table in the way, a parent holding a baby | one hand | ✅ | NOT RUN — `assets/raw/video/one-handed-loop.mp4` |
| 3 | **Eyes-only** | limited mobility, a hand that cannot be used at all | both hands, for every interface action | ✅ (interface) | NOT RUN — `assets/raw/video/eyes-only-playthrough.mp4` |
| 4 | **Calibrated / non-standard hand** | reduced range of motion, a joint condition, a congenital difference, an older hand | the demand that a hand be standard | ✅ | NOT RUN — `assets/raw/video/calibration-with-nonstandard-hand.mp4` |
| 5 | **Reduced motion** | motion sensitivity, vestibular disorders | large animation | ✅ | NOT RUN — `assets/raw/video/reduced-motion-loop.mp4` |

**Path 2 is not a special case.** `BATTLE-CARD.md` lists one-handed as a design law from the
literature, not an accessibility afterthought: it reduces occlusion, reduces fatigue, and it scores
under *Experience Design*.

**Path 3 is honest about its edge.** Gaze replaces every *control*; it does not replace making the
handshape. A shape is the skill being measured, and an unlock granted by looking at something would
record progress the user's hand never made. §5 states that plainly, and it is the main known gap.

## 2. The calibration — what it actually measures

`N5`, the 30-second anatomical calibration.

All pose comparisons are **neutral-relative** (`POSE-FORMAT.md` §4): stored rotations are
`inverse(q_neutral) * q_captured`, so finger length and hand proportion already cancel. What
neutral-relative does **not** normalise is **range of motion**. A hand that can bend 45° can never
reach a 90° target, so it can never get under the lock threshold however correctly it is trying.

**So calibration measures range and stretches the user's motion onto the canonical scale:**

| Step | Prompt | What is measured |
|---|---|---|
| 1 | "Open your hand wide and hold it." | the bottom of each finger's range |
| 2 | "Make a fist, as tight as is comfortable." | the top of each finger's range |
| 3 | "Touch your thumb to each fingertip, in turn." | the thumb's opposition range |

22 seconds of guidance plus a 30-second budget; **skippable at any moment** and re-runnable from the
tray. The measured range is stored in `SaveData.calibration` (per finger: `min`, `max`, `canonical`,
`capturedUtc`) and restored on the next launch.

**The maths, in one line each:**

- `scale[f] = clamp(canonical[f] / measuredRange[f], 1, 2.5)` — a hand at half range is stretched ×2.
- The stretch is applied **to the user's joint rotation before the comparison**
  (`Matcher.ScaleRotation`), not to the resulting distance. Scaling the distance would amplify the
  error instead of removing it: 45° away × 2 is 90° away, not 0.
- `bandScale = clamp(sqrt(mean(scale)), 1, 1.35)` — the stretch enlarges the tracking noise with the
  signal, so the lock band is widened a little. Capped, because a band that is too wide would lock on
  a nearby shape.
- A record whose measured span is under 4° for every finger is **not** a usable calibration; it is
  discarded rather than half-applied.

**What it does not do.** It does not diagnose, score, or advise, and it says nothing about what a
hand "should" do. It measures a range.

## 3. Non-standard anatomy

| | |
|---|---|
| **What we mean** | a hand that differs from the canonical one: length ratios, joint hypermobility, a missing or stiff digit, a healed injury, a much smaller or larger hand |
| **What we assume** | the canonical joint set and the canonical *angles* are a reference, not a requirement |
| **What we do** | the comparison is relative, so length and proportion are already handled. Range of motion is handled by calibration |
| **What we explicitly do not claim** | **there is no clinical validation of any kind.** We have not tested this with a clinician, with a rehabilitation department, or with any population defined by a medical condition. **Do not say the app is "accessible to people with arthritis"** — say it is *designed* to work with a wide range of hands, and that a calibration exists |

## 4. The toggles — seven blocks on one tray

All of these **persist** in `signspace-save.json` and are restored on launch. All are reachable
**in space**: each is a 36 mm plate in a tray that appears within arm's reach in front of the user
(P6.5's trap is a settings menu behind a controller gesture; there is no settings menu). Each plate
answers to **pinch and to gaze-dwell**, so the switches themselves are usable by the eyes-only path.

| # | Block | Default | What it actually changes | Field |
|---|---|---|---|---|
| 1 | **Captions** | **on** | every sound-only cue (band "close", lock chime, release) gets a text line the same moment, driven by the matcher's events, not the audio clips | `captionsOn` |
| 2 | **High contrast** | off | caption and label palettes go to white on black; the gaze label brightens. Never red — red means failure and failure does not exist here | `highContrast` |
| 3 | **Relaxed pace** | off (1×) | multiplies the **hold requirement** and the **dwell time** by 1.5 together, because they are the same quantity to the user: how long a shape must be held still. Thresholds are unchanged — a slower pace must not make the gesture easier | `speedScale` |
| 4 | **One hand** | off | the reference hand follows the tracked wrist **without a pinch**, so shaping, inspecting and selecting all work with one hand. The second hand is not required by anything in the loop | `oneHanded` |
| 5 | **Eyes only** | off | every `DwellAction` in the scene becomes reachable by looking at it, a dwell ring fills in view, and the hold requirement is lengthened a further ×1.35. See §5 | `eyesOnly` |
| 6 | **Near-miss naming** | on | the P3.2 switch: stops the product naming the shape you actually made | `nearMissEnabled` |
| 7 | **Reduced motion** | off | see §6 | `reducedMotion` |

Switches 1–6 are the six the phase task asks for; **reduced motion is the seventh** because P6.6 asks
for it to be a toggle and the alternative — a build with no way to turn it on — is not a path.

There is one more block on the tray: **Calibrate**. It is not a toggle; it starts the flow in §2, and
a second pinch skips it.

## 5. The dwell / eyes-only path

Full detail in [`../02-tech/GAZE-INPUT.md`](../02-tech/GAZE-INPUT.md) §5. The accessibility-relevant
part:

- **Dwell is 0.7 s** on eye gaze, ×1.5 on head gaze, ×1.5 with relaxed pace, ×1.35 in eyes-only. A
  ring in the HUD fills with the progress, so the user always knows how long is left.
- **Cancel without a pinch**: look away. The timer resets and the action does not fire.
- **Re-trigger works**: a completed dwell fires once and waits; look away and back, and it fires
  again. Holding the gaze does not machine-gun the action.
- **Every control is reachable**: select a shape from the shelf, read a finger's label, open and close
  the honest mirror, open and close the credits, flip all seven switches, start or skip calibration.
- **Not reachable**: the lock. A locked shape is the measured skill. Looking at the reference hand
  cannot make the user's handshape, so eyes-only mode does not grant a lock, and the build does not
  pretend it does. This is the path's real limit.
- **No accidental locks**: because eyes-only never locks, this is structural rather than tuned. The
  dwell controls are one-shot per acquisition and the hold is longer; the 5-minute quiet-session test
  is still owed on device (NOT RUN).

## 6. Reduced motion — the exact changes

| With motion | With **Reduced motion** on |
|---|---|
| the shelf's objects breathe at 0.18 Hz | `ShelfIdle.breathing = false` — the objects are still |
| the reference hand drifts at 2°/s | `idleDegreesPerSecond = 0` |
| the lock payoff arcs the object over 0.50 s and shrinks/spins the hand | `arc = 0`, the object appears at its slot; the hand cross-fades out with no shrink and no spin; the payoff is ~⅓ of its normal length |
| the gaze highlight's colour blends | the colour set is immediate (no fade to wait out) |

**No camera motion anywhere.** Nothing in the product writes the camera's transform; the reference
hand places itself from the camera once at start and then rotates in place. The check asserts that no
shake/bob/mover component exists (`P6Checks.CheckReducedMotion`).

## 7. What we are not claiming — the explicit non-claims section

This section is the point of the document. It exists so that the marketing copy cannot drift into
something we have not done.

| We do **not** claim | Why |
|---|---|
| "Clinically validated" or "clinically proven" | **no clinical study exists.** Nobody has tested this with a clinician or a patient population |
| "Accessible to people with arthritis / RSI / cerebral palsy" | we have not tested it with any such population. We designed it to work with a wide range of hands and we **do not know** that it does |
| "Works for every body" | we have not met every body |
| "Replaces a Deaf teacher" | the opposite. The app says so, on the credits screen, in the description, and in the credit line |
| "Improves ASL recall" | the 2020 SAIL RCT (60 learners) found **no significant effect of VR platform on immediate ASL recall**. We cite that. We claim something narrower and testable: that a user is told **which** sign they made, which no 2D resource can do |
| "Detects sign language" | it does not. Nothing is transcribed. Nothing is interpreted. It compares joint angles against stored shapes |
| "AI-powered" / "machine learning" | it is deterministic joint comparison. **No AI, no network, no camera access** |
| "Eyes-only completes the whole loop" | it completes every control; the handshape itself is still made by a hand (§5) |
| "Eye tracking works on Quest 3 / 3S" | those headsets have no eye-tracking hardware. The build says `gaze: head` and explains why |

## 8. The claims we *do* make

| Claim | Evidence |
|---|---|
| "Every interaction is reachable without a controller" | hands-first is a hard test; the build contains no controller code path, and `NoFallbackGuard` passes |
| "Every control is reachable without a pinch" | the eyes-only router, the dwell ring and the seven switch blocks; end-to-end **recording NOT RUN** |
| "The reference adapts to the range of your hand" | `Calibration.cs`, `Matcher.ScaleRotation`, and a headless proof that a half-range hand reads 0.0° against a canonical target (`P6Checks.CheckCalibration`) |
| "Captions on by default" | `SaveData.captionsOn = true`, the toggle block, and the audio-caption panel |
| "Six switches plus reduced motion, in space, persistent" | the tray builder, the save fields, `P6Checks.CheckToggles` |
| "The fallback is stated, not silent" | `EyeTrackingProbe.Explanation` and the one-line caption; `P6Checks.CheckEyeTracking` |
| "It runs at ≥72 fps on a Quest 3S" | `PERFORMANCE.md` — **only if measured**. See the 3S gap |

## 9. Evidence ledger

| Path | What it needs | Recording | Status |
|---|---|---|---|
| Full loop | headset | `assets/raw/video/full-loop.mp4` | NOT RUN |
| One-handed | headset, one hand behind the back | `assets/raw/video/one-handed-loop.mp4` | NOT RUN |
| Eyes-only | headset, hands in lap | `assets/raw/video/eyes-only-playthrough.mp4` | NOT RUN |
| Calibration with a restricted hand | headset, a hand held rigid | `assets/raw/video/calibration-with-nonstandard-hand.mp4` | NOT RUN |
| Reduced motion | headset | `assets/raw/video/reduced-motion-loop.mp4` | NOT RUN |
| Eyes-still / head-still discriminator | headset with eye tracking (simulator VR Glasses profile) | `assets/raw/video/gaze-proof.mp4` | NOT RUN |

Every recording above needs a headset and a body. The desk checks prove the wiring and the maths; they
are **not** evidence for these rows, and this table does not move until the videos exist.

## 10. Inputs
`NOVELTY-AND-PRIOR-ART.md` N5 and the accessibility source note (a thumbless Quest 3S owner in the
Meta accessibility forum thread) · `POSE-FORMAT.md` §4 · `GAZE-INPUT.md` · `UX-STATES.md` ·
`COMPETITION-FIT.md` (the Best Accessibility Forward award definition, which names eyes-only
navigation for limited mobility) · `phases/p6-accessibility/tasks/p6.1..p6.7`

## 11. Output / done when
- [x] the five modality paths, and what each removes
- [x] what the calibration actually normalises, and why it was nearly cut
- [x] non-standard anatomy, and what we explicitly do not claim
- [x] the six toggles, the seventh (reduced motion) and the calibrate block
- [x] **the non-claims section**
- [x] **the doc matches the build** — every row re-read against `Scripts/` and `Editor/P6Checks.cs`
- [ ] **a one-handed playthrough is recorded** — NOT RUN
- [ ] **an eyes-only playthrough is recorded** — NOT RUN
- [ ] **a calibrated playthrough is recorded** — NOT RUN
- [ ] **a reduced-motion playthrough is recorded** — NOT RUN
