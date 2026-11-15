# CONCEPT
## SIGNSPACE — the whole product on one page

> **Tagline (119 chars):** *"Every app that teaches sign language is a video with extra steps. This one tells you which sign you accidentally made."*

---

## 1. THE PROBLEM, IN ONE SENTENCE

**Sign language is three-dimensional, but almost every resource that teaches it is a flat video watched from one fixed angle** — so a learner cannot see the depth configuration of a handshape (whether the thumb sits *across* the palm or *beside* it), and receives **no feedback at all** when they get it wrong.

Two consequences follow, and they are the whole product:

1. They cannot **see** the thing that distinguishes one handshape from another.
2. They cannot be **told** they got it wrong — and self-taught sign learners can practise the same wrong shape for months.

## 2. THE PRODUCT, IN FIVE BEATS

| # | Beat | What the user does | Why it needs this medium |
|---|---|---|---|
| 1 | **The reference appears** | A life-size hand, captured from a Deaf signer, holds a shape at conversational distance | It is geometry, not video — it can be viewed from any angle |
| 2 | **They turn it** | Pinch the reference hand and rotate it **with their own wrist** | This is the 3-second demo. You cannot rotate a video |
| 3 | **Gaze isolates the detail** | Look at a finger → it highlights with its position | Both hands are busy; you cannot point |
| 4 | **Their hand is compared** | Tracked hand mirrors beside the reference; a forgiving match responds | The hand being trained is the input device |
| 5 | **The near-miss** | If they're close to a *different* sign, they are told **which one they actually made** | Only possible with joint-level data. Nobody does this |

**And then it persists:** every mastered shape becomes an object on a **vocabulary shelf anchored to real furniture** via MRUK. Kill the app, relaunch, the room remembers.

## 3. WHY IT CANNOT EXIST ANYWHERE ELSE

| Test | Answer |
|---|---|
| **Hands-first** (never pair a controller) | With a controller you cannot turn the hand with your hand, and you cannot compare your shape to the reference **because the instrument being trained is holding a controller**. A controller version is not a worse product — it is not a product |
| **Airplane seat** (2 ft, ≤10 min) | Seated, near-field, no locomotion, one-handed capable, 10-minute session by design |
| **Take-it-away** (no thin wrapper) | The content is 3D geometry of a moving hand. There is no 2D original to wrap |

## 4. WHAT IT IS NOT

| Not this | Why |
|---|---|
| Not an ASL course | 12 handshapes, no grammar, no syntax, no motion, no facial grammar. It is a **handshape practice companion** |
| Not an interpreter / translator | Nothing is transcribed. No deaf-to-hearing utility is claimed |
| Not an AI product | Deterministic hand tracking. **No AI, no network, no camera access.** The whole loop works offline |
| Not a fluency claim | We cite the 2020 RCT that found no VR platform effect on recall, and claim something narrower and testable (see `NOVELTY-AND-PRIOR-ART.md` N3) |
| Not a replacement for a Deaf teacher | Stated in the app, in the description, and on the credits screen |

## 5. THE NOVELTY, IN ONE PARAGRAPH

The **mechanic** — track a hand, compare it to a stored pose — is six years old and free on GitHub. So the novelty is not there. It is in the feedback: SIGNSPACE tells you **which sign you accidentally made** (near-miss, built on real minimal-pair linguistics), **publishes its sensor's blind spots** instead of hiding them, **refuses to overclaim learning outcomes**, and **adapts its reference to your anatomy** rather than demanding a standard hand. Every other hand-tracking app uses your hand to move something. **This one uses your hand to tell you something about your hand.**

## 6. WHO IT IS FOR

| Segment | Why they care | Evidence |
|---|---|---|
| **Self-taught ASL learners** | The depth ambiguity is their daily obstacle and no tool addresses it | Every video-based incumbent is 2D |
| **Deaf-community allies, parents, partners** | They need to *be understood*, and a wrong handshape is the failure they can't see | Community threads describe the anxiety of being unintelligible in social VR in blunt terms |
| **Mainstream Adults (Meta's 2026 cohort)** | Seated, controller-free, media-first, 10-minute sessions, learning something | Meta's own GDC 2026 cohort framing |

## 7. THE FIGURES THAT MATTER

| | |
|---|---|
| Handshapes shipped in v1 | **12**, across **3 families** |
| Session length | 10 minutes, by design |
| First payoff | **< 75 seconds** from launch |
| Perf floor | **72 fps** on Quest 3S (floor is 60) |
| Words of tutorial | **≤ 3** in the first five minutes |
| Engine | Unity + Meta XR SDK v207 |
| Track / division | Productivity / New Experience · Rating 10+ |
| Launch claim | 60 signs by **Q1 2027** |

## 8. WHAT WINNING LOOKS LIKE

Track prize is the upside. The floor is **Best Accessibility Forward** + **Best First Five Minutes** + **Best Reason to Come Back** — all three fire off the same design decisions, which matters because the competition awards **one prize per entry**.

## 9. THE KILL CONDITION

**No Deaf signer captured by Day 5 → stop and re-pick the concept.** A sign-language product without Deaf participation is the one version of this idea that fails badly, and it is the only risk in the project that cannot be mitigated by working harder.
