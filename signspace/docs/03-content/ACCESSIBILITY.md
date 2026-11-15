# Accessibility
**Status:** ✅ v1 · **Owner:** W3 · **Phase:** P6 · **Gate:** G6

> **Accessibility cannot be a settings panel at the end.** This app is about a communication-access
> technology, made by people who are not Deaf, for people who are learning something that Deaf people
> already do. The accessibility work is not a compliance checkbox — it is the difference between a
> product and a demo. `Best Accessibility Forward` is a **$25k award with no incumbent template.**

---

## 1. The five modality paths

Each path must complete **the whole loop** — boot, inspect, rotate, match, lock, see the shelf —
not just one interaction. A path that can select but cannot lock is not a path.

| # | Path | Who it is for | What it removes | Status |
|---|---|---|---|---|
| 1 | **Full** — both hands, gaze | everyone | nothing | ⬜ |
| 2 | **One-handed** | an injury, a disability, a table in the way, a parent holding a baby | one hand | ⬜ |
| 3 | **Eyes-only** | limited mobility, a hand that cannot be used at all | both hands | ⬜ |
| 4 | **Calibrated / non-standard hand** | reduced range of motion, a joint condition, a congenital difference, an older hand | the demand that a hand be standard | ⬜ |
| 5 | **Reduced motion** | motion sensitivity, vestibular disorders | large animation | ⬜ |

**Path 2 is not a special case.** `BATTLE-CARD.md` lists one-handed as a design law from the
literature, not an accessibility afterthought: it reduces occlusion, reduces fatigue, and it scores
under *Experience Design*.

**Path 4 is the one that matters most for this product.** A handshape app that only works on a
standard hand has a ceiling on who can use it, and the fix is small.

## 2. The calibration — and why it was almost cut

`N5`, the 30-second anatomical calibration. It was cut from the ladder (rung 2) in the four-day
plan, and that was **right**, because of a decision made in `POSE-FORMAT.md`:

> All rotations are stored **relative to the signer's neutral pose**:
> `q_stored[i] = inverse(q_neutral[i]) * q_captured[i]`

Because every comparison is **relative**, the shape of the user's hand stops mattering. A user with
shorter fingers produces the same relative angles. **The comparison is already scale-free.**

**So what is left for calibration to do?** Only the part that neutral-relative does *not* normalise:
the **range of motion** — how far a particular joint can actually bend, and how far it can spread.
A hand with reduced range cannot reach the full flexion of a canonical shape, so it can never get
under the lock threshold no matter how correctly it is trying.

**Calibration therefore means: measure the user's achievable range, and normalise the target to it.**
Three steps, thirty seconds:

| Step | What the user does | What we measure | How |
|---|---|---|---|
| 1 | **Open hand, palm down, held 2 s** | the neutral | the average of 60 frames. The doc's original procedure, and it is still right |
| 2 | **Curl the fingers as far as they go** | the maximum flexion per finger | the minimum achievable chain rotation per finger |
| 3 | **Spread the fingers as far as they go** | the maximum abduction per finger | the maximum achievable chain rotation per finger |

From step 1 we get the user's neutral; from steps 2 and 3 we get their envelope. **The lock
threshold is then compared against the user's envelope rather than against a canonical range.** A
hand that can only reach 70% of the canonical curl is not "wrong" — it is a different hand, and the
app has to know that before it tells anyone they are wrong.

> **The product principle, restated:** never tell a learner that they are wrong when they are
> correct within their own body. That is why the calibration exists, and why it is not a cosmetic
> feature.

**The cost of the calibration being cut:** a user with reduced range of motion simply cannot lock
some shapes. The app will not tell them they are wrong — it will simply never close the band. That
is a safe failure, but it is a failure, and if the time appears, the calibration is the first thing
to build.

## 3. Non-standard anatomy

| | |
|---|---|
| **What we mean** | a hand that differs from the canonical one: length ratios, joint hypermobility, a missing or stiff digit, a healed injury, a much smaller or larger hand |
| **What we assume** | the canonical joint set and the canonical *angles* are a reference, not a requirement |
| **What we do** | the comparison is relative, so length and proportion are already handled. Range of motion is handled by calibration |
| **What we explicitly do not claim** | **there is no clinical validation of any kind.** We have not tested this with a clinician, with a rehabilitation department, or with any population defined by a medical condition. **Do not say the app is "accessible to people with arthritis"** — say it is *designed* to work with a wide range of hands, and that a calibration exists |

## 4. The five toggles

All five work, all five **persist**, and all five are reachable **in space** — not in a settings
menu behind a headset-removal gesture.

| # | Toggle | Default | What it does | Persistence |
|---|---|---|---|---|
| 1 | **Captions** | **on** | every spoken or sound-only cue has a text equivalent | user setting |
| 2 | **High contrast** | off | the highlight, the meter and the shelf objects shift to a high-contrast palette. **Never red** — red means failure and failure does not exist here | user setting |
| 3 | **Speed** | 1.0× | the pace of the reference hand's idle rotation, the settle animation, and the dwell confirmation | user setting |
| 4 | **One-handed** | off | the interface moves to one side, the hand tracker ignores the second hand, and the shelf becomes a single column | user setting |
| 5 | **Eyes-only** | off | dwell selection replaces every pinch. See `GAZE-INPUT.md` §5 | user setting |

**A sixth, not a toggle: the reduced-motion path.** The shelf settle animation and the idle rotation
are the only motion in the product, and both have to be switchable off. It is not in the five
because it is not a preference — **it is a correctness requirement** (`P6.6`).

**Reduced motion, concretely:**

| With motion | Without motion |
|---|---|
| the object drops into place over 250 ms | the object is simply there |
| the idle rotation at 2°/s | the object is static |
| the reference hand rotates very slightly on its own | it does not |
| the gaze highlight fades in over 150 ms | it is instant |

## 5. The dwell / eyes-only path

Full detail in [`../02-tech/GAZE-INPUT.md`](../02-tech/GAZE-INPUT.md) §5. The accessibility-relevant
part:

- Every action is reachable by dwell. No action requires a pinch.
- **Dwell must be cancellable without a pinch** — look away.
- **Dwell must be re-triggerable** on the same target, or the path is one-shot and unusable.
- The dwell targets are **larger** in eyes-only mode, because the user's hand may be resting and
  their head may be less still than in the full path.
- Every dwell has a **visible ring**, so the user knows how long is left.

## 6. What we are **not** claiming

This section is the point of the document. It exists so that the marketing copy cannot drift into
something we have not done.

| We do **not** claim | Why |
|---|---|
| "Clinically validated" or "clinically proven" | **no clinical study exists.** Nobody has tested this with a clinician or a patient population |
| "Accessible to people with arthritis / RSI / cerebral palsy" | we have not tested it with any such population. We designed it to work with a wide range of hands and we **do not know** that it does |
| "Works for every body" | we have not met every body |
| "Replaces a Deaf teacher" | the opposite. The app says so, on the credits screen, in the description, and in the credit line |
| "Improves ASL recall" | the 2020 SAIL RCT (60 learners) found **no significant effect of VR platform on immediate ASL recall**. We cite that. We claim something narrower and testable: that a user is told **which** sign they made, which no 2D resource can do |
| "Detects sign language" | it does not. Nothing is transcribed. Nothing is interpreted. It compares joint angles against six stored shapes |
| "AI-powered" / "machine learning" | it is deterministic joint comparison. **No AI, no network, no camera access** |

## 7. The claims we *do* make

| Claim | Evidence |
|---|---|
| "Every interaction is reachable without a controller" | hands-first is a hard test; the build contains no controller code path |
| "Every interaction is reachable without a pinch" | the eyes-only playthrough, recorded |
| "The reference adapts to the shape of your hand" | the relative-angle design in `POSE-FORMAT.md` §4, and the calibration |
| "Captions on by default" | the toggle |
| "It runs at ≥72 fps on a Quest 3S" | `PERFORMANCE.md` — **only if measured**. See the 3S gap |

## 8. Inputs
`NOVELTY-AND-PRIOR-ART.md` N5 and the accessibility source note (a thumbless Quest 3S owner in the
Meta accessibility forum thread) · `POSE-FORMAT.md` §4 · `GAZE-INPUT.md` · `UX-STATES.md` ·
`COMPETITION-FIT.md` (the Best Accessibility Forward award definition, which names eyes-only
navigation for limited mobility)

## 9. Output / done when
- [x] the five modality paths, and what each removes
- [x] what the calibration actually normalises, and why it was nearly cut
- [x] non-standard anatomy, and what we explicitly do not claim
- [x] the five toggles and the reduced-motion path
- [x] **the non-claims section**
- [ ] **a one-handed playthrough is recorded**
- [ ] **an eyes-only playthrough is recorded**
- [ ] **a calibrated playthrough is recorded**
- [ ] **the doc matches the build** — `P6.7` says "to the build, not to the aspiration", and that means re-reading it after every change
