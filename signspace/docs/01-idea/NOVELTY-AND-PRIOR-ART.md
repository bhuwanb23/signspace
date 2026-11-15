# NOVELTY & PRIOR ART
### What has already been built, what we must stop claiming, and the four things no one has done

*Prepared 7 November 2026 · companion to `THE_IDEA.md` · all sources listed in §7*

---

## 0. THE 60-SECOND SUMMARY OF WHERE WE ARE

| | |
|---|---|
| **Decision** | Build **SIGNSPACE** — a seated, hands-only 3D handshape practice space for sign language |
| **Why it won** | 84.2/100 on 30 weighted metrics; first or joint-first in 4 of 6 categories; 1st in all four sensitivity scenarios; margin 7–13 pts ≈ 2× the 2nd–6th spread |
| **The premise** | **You cannot rotate a video.** Sign language is 3D; every resource that teaches it is flat; so learners cannot see the depth of a handshape and get no feedback when they're wrong |
| **Track / division** | Productivity · New Experience · Rating 10+ |
| **Deadline** | 19 Nov 2026, 01:30 IST (internal freeze 17 Nov) |
| **Kill condition** | Cannot source 1–3 Deaf signers to record 12 handshapes in 72 h → do not build |

**This document does one job:** pressure-test the novelty claim against what has actually been built, throw out everything that isn't ours, and find the angle a judge will not have seen before.

---

## 1. WHAT THE JUDGES ACTUALLY REWARDED IN 2025

The 2025 edition drew **~3,000 developers and 650+ submissions for 32 awards** ([1]). Anyone entering again is being read against these:

| Award | Winner | What it is | The pattern underneath |
|---|---|---|---|
| Best Entertainment ($100k) | **DreamSpace** — Pranay Garg & Shashank Vaibhav | *"Turn any room into stunning immersive mixed-reality spaces"* | **Your real room becomes X** |
| Best Lifestyle ($100k) | **Inkphony** — Martinic, Shroff, Pirchheim | *"Everything around you becomes an instrument"* | **Your real room becomes X** |
| Best Casual Game ($100k) | **Tiny Golf** — BRAINZ GAMIFY | MR mini golf, *"all you need is a pinch"* | **One gesture, taken seriously** |
| Best Social Game ($100k) | **Saber Punks** — EFish Games | VR PvP with 150+ motion combos | Real-time multiplayer |
| PCA + AI ×3 ($50k ea) | **Language IRL**, Chores.gg, Flows.xr | *"Turn your world into a classroom"* | **MR language learning already won a $50k** |
| Hand Interactions ×3 ($50k ea) | Hand Survivor, Pocket Lands, Awesome Hand | Hand-tracking showcase games | Hands as spectacle |
| Judge's Choice ×6 ($30k ea) | Into The Mind, OpenSoundLab, Lasertag prototypes, Final Throwdown, HandCraft XR, ModStorm | Odd, personal, unfinished-but-brilliant | **Weird beats polished** |

**Three readings that matter:**

1. **The grand prizes reward the same thesis: your physical room becomes the content.** SIGNSPACE's MRUK vocabulary shelf sits directly on that line — but as a *consequence of learning*, not a backdrop. That is a fresh instance of a proven shape.
2. **Zero accessibility awards existed in 2025.** 2026 adds **Best Accessibility Forward** — a genuinely new, uncontested category. Nobody has a template for it yet.
3. **Language IRL already won $50k for MR language learning in passthrough.** This is the single most important competitive fact in this document, because a judge *will* make the connection. It cuts both ways: it proves the category pays, and it means "MR language app" is no longer novel. **We must be a different category, not a better version of theirs.** See §3.

---

## 2. THE PRIOR-ART SWEEP: WHO HAS ALREADY TRIED THIS

This is the uncomfortable part, and it is the most valuable part. **Six years of people trying to teach sign language in VR, and not one shipped consumer product.**

| # | Project | Year | What it actually did | What it establishes |
|---|---|---|---|---|
| 1 | **Girardin, "VR-Trainingsapplication for finger alphabet"** (cpvrLab, Switzerland) | **2020** | Quest hand tracking; 23 German fingerspelling handshapes; compares a saved hand model to the live pose and validates correctness; free on SideQuest | **The core recognition loop is six years old and solved.** Our novelty cannot be "we validate your hand" |
| 2 | **SAIL** (ASSETS '20, ACM) | **2020** | Signing avatar motion-captured from a **native Deaf signer**; Leap Motion; user sees **their own hands**; 30 signs; produced with Deaf team members | **"Real signer mocap + see your own hands" is published prior art.** Also the source of our integrity protocol |
| 3 | **dillondrum70, ASL-Passthrough** (open source) | ~2022 | Quest 2; **per-joint-angle** comparison with tolerance; hold-duration logic to disambiguate J from I; **built in 1.5 weeks** | Our matching algorithm is public, free, and weekend-sized. **Its author's stated future work is literally our product:** *"a training mode that allows users to analyse a pose from all angles then copy it"* |
| 4 | **HNS-feature motion teaching** (Univ. of Washington line of work) | ~2020–21 | Matches learner motion to features derived from the **Hamburg Notation System**, the linguistic notation for sign | Someone already connected sign *notation* to tracking. Not productised |
| 5 | **ASL Champ!** (ScienceDirect) | **2024** | VR game; Vicon mocap, 18 cameras / 73 markers, real signers; 9 coffee-shop signs; deep-learning recognition; 90% train accuracy | **"VR ASL game with recognition and a teacher avatar" exists in peer review.** Note their finding: recognition is hypersensitive to rotation |
| 6 | **Sign Talker** (SideQuest) | **2024** | Quest 3; user assigns any gesture to a letter; TTS output; aimed at speech impairments | Gesture-to-text consumer utility already ships |
| 7 | **VRSignify, Sign_AI_VR_Game, vr-asl-tutor, VR_ASL_Unity** (GitHub) | 2019–24 | Hackathon and coursework projects: ASL→text, fingerspelling tutors, Leap+HTC Vive | **This is a crowded hobby space.** A judge who has seen Devpost before has seen "VR ASL" |
| 8 | **Meta Spatial Lingo: Language Practice** (Meta, open source) | **Jan 2026** | Meta's own showcase app: passthrough + AI object recognition, labels your real furniture in Spanish/English, 3D companion gives feedback | **Meta already ships an MR language-learning showcase — for spoken languages.** Sign language is the untouched sibling |

### What this sweep changes

| Claim in earlier drafts | Verdict | Correction |
|---|---|---|
| "No one has built VR sign-language learning" | ❌ **False** | True claim: **no consumer product has shipped on the Store; nothing has traction; every attempt is a sideload, a paper, or a hackathon repo** |
| "Per-joint tolerance matching is our technical edge" | ❌ **Not novel** | It is 30 lines of code from 2022. **Stop leading with it** |
| "Rotating the reference hand is the innovation" | ⚠️ **Half-true** | It has been *named as desired future work* — but never built and never shipped. Verifiable as unbuilt, not unimagined |
| "Real signer mocap + mirroring your own hand" | ⚠️ **Published** (SAIL, 2020) | Keep as **credibility and integrity**, not as novelty |
| "VR improves sign language learning" | ❌ **Contested** | SAIL's 60-participant RCT found **no significant effect of platform or embodiment on immediate learning**. **Never claim learning outcomes.** Claim the thing not tested: *inspection of 3D configuration from arbitrary angles* |
| "Hand tracking can't do this" | ❌ **Wrong and self-harming** | It demonstrably works for a robust subset. Be precise about *which* subset (§4, N2) |

---

## 3. THE CATEGORY PROBLEM: LANGUAGE IRL AND SPATIAL LINGO

Two facts create one hazard:

- **Language IRL** won $50k in this exact competition last year for turning your room into a language classroom.
- **Spatial Lingo** is Meta's *own* open-source MR language-practice showcase.

A lazy read of SIGNSPACE is *"the ASL version of those."* We must make that read impossible in the first ten seconds. Three separations, in order of strength:

| Axis | Language IRL / Spatial Lingo | SIGNSPACE |
|---|---|---|
| **The medium of the content** | Words are **flat** — text, audio, a floating label. 2D content in a 3D room | The content is **inherently 3D and cannot be flattened**. This is not a spatial presentation of a 2D subject; it is the only adequate presentation of a 3D one |
| **What is being evaluated** | Audio (pronunciation), object recognition | **The geometry of your hand**, joint by joint, to within degrees |
| **What the reward is** | A label appears, a voice says "bien" | **You are told which sign you accidentally made.** A different kind of feedback entirely |
| **The technology** | Passthrough Camera Access + cloud AI | Deterministic hand tracking + gaze + MRUK. **No AI, no network, no camera access** |

**The line to use:** *"Language IRL put vocabulary in your room. SIGNSPACE puts a language that a room-shaped medium can finally hold."*

And the one-sentence kill shot: **"Every ASL app so far is a video with extra steps. This one can tell you that you just signed the wrong letter."**

---

## 4. THE NOVELTY WEDGE — FIVE THINGS NO ONE IS DOING

Prior art says the *mechanic* is not ours. So the novelty has to live somewhere else. It lives in five places, none of which appear in any project in §2.

**The reframe first, because everything below depends on it:**

> **Old framing:** "a VR app for practising ASL handshapes."
> **New framing:** **the first app whose content is your own hand, seen as data.**

The tracked skeleton is not a control scheme here. It is the *subject*. Every other hand-tracking app uses your hand to move something. This one uses your hand to *tell you something about your hand*. That inversion is the whole novelty story, and it explains in one line why the product cannot exist on any other platform.

---

### N1 — THE NEAR-MISS *(build this first; it is the hook)*

**What it is.** You never get pass/fail. You get **which other sign you accidentally made, and by how much**:

> *"That's not C. That's O — your thumb is 8° too far around the palm."*

**Why it is genuinely new.** Every prior project in §2 returns correct/incorrect. None names the confusion. Sign linguistics has a name for this — **minimal pairs**: handshapes that differ by one articulatory feature (handshape, palm orientation, location, movement). Nobody has put minimal-pair diagnosis into a consumer interaction, because until spatial tracking you could not measure the difference.

**Why judges will like it.** It is a *reward*, not a correction. It gives the player information instead of a red X. And it is a three-second, silent, photographable demo: a hand makes a shape, the app says what you *actually* said, your face changes. That is a better submission-video moment than any rotation shot.

**Why the market needs it.** This is the exact failure mode of self-taught sign learners: they practise a shape that is wrong in a way video cannot reveal, for months. This is the product's reason to exist, stated as a feature.

**Cost:** low. A pairwise delta table over the 12-sign set; fire only when confidence is high, otherwise stay silent.
**Risk:** firing wrongly is worse than not firing. Rule: **when unsure, say nothing.**

---

### N2 — THE HONEST MIRROR *(cheapest novelty in the document, ~half a day)*

**What it is.** A visible, in-product set of **the shapes today's hand tracking cannot resolve** — thumb-tucked-under-fingers (M, N, T, S) and crossed fingers (R) — shown deliberately greyed out, with one line on why: *the cameras cannot see a finger that is behind another finger.* Beside it, a live **tracking-confidence readout** that *pauses* the exercise instead of failing it.

**Why it is genuinely new.** In a field of 650 submissions, **nobody submits their own platform's blind spots.** It has never appeared in a competition entry, because it feels like confessing a weakness.

**Why judges will like it.** Three reasons at once: it proves the developer actually built and tested (you only know a sensor's limits by hitting them); it is *strategically generous* to Meta — it says the roadmap has a reason to exist; and it is the single most trust-building thing you can show a Deaf judge. Meta's own accessibility forum has a standing user request from a Quest 3S owner **born without a thumb**, because the system's gestures assume thumb-index interaction ([7]) — the community is actively asking for honesty about anatomy.

**The reframe that makes it a feature:** *"We do not invent handshapes the sensor can't see. We publish the edge."*

**Cost:** half a day: one static set, four lines of copy, a confidence bar.
**Risk:** could read as "so it doesn't work." **Mitigation:** put a number on it — *"44 of 70 ASL handshape-family variants are robustly trackable today; here are the 26 that aren't, and here is the one that will change first."*

---

### N3 — THE ANTI-CLAIM *(free; the most unexpected thing in the submission)*

**What it is.** One paragraph in the description that says, explicitly, what this product **does not** claim:

> *"We do not claim SIGNSPACE makes you fluent. A 2020 RCT of 60 learners found no significant effect of VR platform or embodiment on immediate ASL recall (SAIL, ASSETS '20). We claim something narrower and testable: that a learner can inspect a 3D handshape from any angle and be told which sign they actually produced — neither of which any video can do."*

**Why it is genuinely new.** Every competition entry overclaims. A submission that cites the study *against* its own category reads as unarguable confidence, and it disarms the one judge in the room who knows the literature.

**Cost:** zero. One paragraph.
**Risk:** almost none. The only failure mode is sounding defensive — so keep it to three lines and put it late in the description, after the demo claims.

---

### N4 — ONE ANGLE FIRST *(game mode; build only if Day 5 is green)*

**What it is.** You are shown a handshape from **one vantage point only** — the same impoverished view a video gives you. You predict the hidden configuration: which fingers, which palm direction. Then you **grab it and rotate it to check.** You are scored on the accuracy of your mental rotation.

**Why it is genuinely new.** It turns the pedagogy into a mechanic. The skill being trained — inferring 3D configuration from a single projection — is *precisely* the deficit that 2D instruction leaves behind, so the game and the learning are the same object. No prior project has gamified the ambiguity itself.

**Why judges will like it.** It gives a Productivity entry a real play loop, and it makes the First Five Minutes award winnable on gameplay rather than on charm.

**Cost:** ~1 day (reuses the rotation and the delta table).
**Risk:** scope. **This is the first thing cut if the build slips.** It is a mode, not the product.

---

### N5 — YOUR HAND IS THE REFERENCE *(accessibility layer; the Best Accessibility Forward argument)*

**What it is.** A 30-second calibration that normalises joint angles and range of motion **to your hand**, not to an idealised one. The reference adapts to your anatomy: reach, finger length, reduced range.

**Why it is genuinely new.** Every recognition system in §2 compares you to a canonical model and fails if you are not canonical. This inverts it: **we do not require you to have a standard hand.** That is a one-sentence accessibility philosophy, and it is directly responsive to a real, unanswered request on Meta's developer accessibility forum ([7]).

**Why judges will like it.** The Best Accessibility Forward award is new in 2026 and has no incumbent template. Most entries will answer it with contrast modes and captions. Yours answers it with **anatomical tolerance** — a much harder claim to make and a much more memorable one.

**Cost:** low — the matching is already relative-angle; this is normalisation arithmetic plus a calibration beat.
**Risk:** do not over-claim clinical validity. Say "works with non-standard hands," not "clinically validated."

---

### N0 — THE POSITIONING NOVELTY: BUILD FOR THE HEADSET THAT HASN'T SHIPPED

**What it is.** SIGNSPACE is designed for **VR Glasses** — ~100 g, $1,299, spring 2027, **eyes and hands only, no controllers** ([6]) — while running today on Quest 3/3S.

It is the canonical shape of an app for that device: seated, near-field, no locomotion, gaze-plus-hands, short sessions, one-handed capable, and legible inside a ~70°×66° field of view. Not a Quest app ported forward — a Glasses app proven early.

**Why judges will like it.** Meta needs a *reason* for a $1,299 eyes-and-hands wearable beyond watching video. A daily-practice training app is that reason. Naming it demonstrates you understand the platform's own roadmap better than the other 600 entries, and it costs nothing but nerve.

**The line:** *"This is not a Quest app. It's the first app for what comes after Quest — running on Quest so you can check it today."*

**Risk:** sounds speculative if unsupported. **Mitigation:** show it — the VR Glasses profile in XR Simulator, the eyes-only dwell playthrough, the FoV reflow. Evidence, then the claim.

---

## 5. SCORING AFTER THE AUDIT

Prior art forces two corrections to `THE_IDEA.md`. I ran them through the same weights rather than quietly leaving the number alone.

| Stage | B1 vacancy | B2 originality | B5 distinctiveness | Total | Rank |
|---|---|---|---|---|---|
| Original (before this sweep) | 10 | 10 | 8 | **84.2** | 1st |
| **After prior-art audit** | 8 | 6 | 8 | **81.2** | 1st |
| **After novelty package N1–N3** | 8 | 9 | 9 | **82.9** | **1st** |

**Why I downgraded and then partially restored it, honestly:**

- **B1 → 8 (−1.0):** a Googler-era sideload app, a shipped SideQuest utility, and a peer-reviewed game all exist. The *store* shelf is empty; the *idea space* is not.
- **B2 → 6 (−2.0):** the matching algorithm and the "rotate the pose to inspect it" wish are public. Saying otherwise would be false.
- **B2 → 9, B5 → 9 (+1.9):** the near-miss diagnosis, the published-blind-spot mode, the anti-claim, and the anatomy-inverted reference appear in none of the eight prior projects. These are ours.

**Revised standing — unchanged at the top, on better evidence:**

| | Score | Gap |
|---|---|---|
| **SIGNSPACE (revised)** | **82.9** | — |
| COURTSIDE | 73.5 | −9.4 |
| THE ROOM REMEMBERS | 73.3 | −9.6 |
| SLEIGHT | 69.5 | −13.4 |
| CONDUCT | 66.9 | −16.0 |
| UNDERTOW | 66.0 | −16.9 |

The margin **widened** on the metric that matters most to you. Novelty and need are no longer *asserted* — they are now **positioned against a documented field**, which is the only version of that claim a judge can't puncture.

---

## 6. WHAT CHANGES IN THE SUBMISSION

### The tagline

| | |
|---|---|
| ~~Old~~ | ~~"Sign language is 3D — but every video that teaches it is flat..."~~ — good, but it's a *problem statement*, and every entry has one |
| **New (recommended)** | **"Every app that teaches sign language is a video with extra steps. This one tells you which sign you accidentally made."** *(119 chars)* |
| Alternate | "Hand tracking's real trick isn't grabbing things. It's telling you that you just signed the wrong letter." *(111)* |

**Why the new one wins:** it starts with a verdict on the entire existing market (which no video-based app can answer), it names a specific impossible-on-2D behaviour, and it contains a small joke. Judges remember jokes.

### The video — new first 10 seconds

| Time | Shot | Why |
|---|---|---|
| 0:00–0:04 | A hand makes a shape. On-screen text: **"You just signed R."** Then: **"You meant U."** | **The near-miss, stated as a fact about the viewer.** Nobody expects to be told they were wrong in the first four seconds |
| 0:04–0:10 | The shape is grabbed and **rotated**; the difference between R and U becomes visible in depth | Pays off the hook with the mechanic |
| 0:10–0:25 | Split-screen: the same sign as flat video (ambiguous) vs. turnable reference | Makes the category argument in one image |

Everything after 0:25 in the existing storyboard stands, with two insertions: **the honest-mirror panel** (~1:30) and **the VR Glasses segment** moved up to 1:10.

### The description — three sentences to insert

1. **Positioning:** *"This is not a Quest app ported forward. It is designed for VR Glasses — eyes and hands, no controllers, seated, short sessions — and runs today on Quest 3 and 3S."*
2. **The near-miss:** *"Wrong answers are more informative than right ones: when your handshape is a few degrees off a different sign, SIGNSPACE names the sign you actually made."*
3. **The anti-claim:** the SAIL paragraph from N3, placed near the end.

### The priority ladder (11 days, protected)

| Tier | Contents | Rule |
|---|---|---|
| **MUST** | Rotation · matching · gaze highlight · MRUK shelf · **N1 near-miss** · **N2 honest mirror** · accessibility toggles | Non-negotiable. N1 and N2 are the submission's identity |
| **SHOULD** | **N5 hand calibration** · sound design · FoV reflow | Build if Day 6 is green |
| **IF GREEN** | **N4 one-angle game mode** | Only after the external playtest passes |
| **CUT FIRST** | N4, then shelf persistence animation, then the second handshape family | Never cut N1, N2, or the signer credits |

> **A compressed-calendar note.** If the eleven days are not actually available — if the calendar is tighter than the plan assumes — cut in this order: 12 signs → **6 signs, 1 handshape family**; drop N4 and N5; keep N1, N2, gaze, and the shelf. A six-sign build with the near-miss and the honest mirror still wins the argument. A twelve-sign build without them is just another ASL demo.

---

## 7. SOURCE REGISTER

| # | Source | Used for |
|---|---|---|
| 1 | Meta, *Meet the Meta Horizon Start Developer Competition Winners* (23 Dec 2025) — developers.meta.com/vr/blog/meta-horizon-start-developer-competition-meet-the-winners/ | 2025 winners, 650+ projects, 3,000 devs, award list |
| 2 | *Utilizing hand tracking technologies in serious games to teach ASL* — journals.gmu.edu/jssr/article/view/4287 | Puzzle-based ASL hand-tracking game |
| 3 | dillondrum70, **ASL-Passthrough** (GitHub) | Per-joint-angle matching, hold-duration disambiguation, M/N/R/T impossibility, the "analyse a pose from all angles" future-work quote |
| 4 | **SAIL** — *Teaching ASL Signs using Signing Avatars and Immersive Learning in VR*, ASSETS '20 | Deaf-signer mocap, own-hands mirroring, 60-participant RCT with **no significant platform effect** |
| 5 | **ASL Champ!** — ScienceDirect, 2024 | Vicon mocap pipeline, 9 signs, DL recognition, rotation sensitivity |
| 6 | Meta VR Glasses essentials + Connect 2026 recap — developers.meta.com/vr/essentials/get-started-with-glasses/ | ~100 g, $1,299, spring 2027, eyes+hands only |
| 7 | Meta community forums, *Request for Custom Hand Tracking Support — Quest 3S accessibility for user without thumb* (Jul 2025) | The anatomy-tolerance request behind N5 and the honest-mirror framing |
| 8 | **Language IRL** — Devpost gallery + winner blog + LinkedIn (P. Howell) | The 2025 MR-language-learning precedent |
| 9 | **Spatial Lingo: Language Practice** — Meta open-source showcase app, Jan 2026 (roadtovr.com) | Meta's own MR language-learning app; the adjacency hazard |
| 10 | *Oculus Quest's hand-tracking is making it possible to learn sign language in VR* (RoadtoVR, 2020) + *Sign Language In VR 'Worth Exploring'* (UploadVR) | Girardin's 23-handshape German fingerspelling app; Deaf users mapping camera limits in <1 hour |
| 11 | **Sign Talker** — SideQuest, 2024 | Shipped consumer gesture-to-text utility |
| 12 | r/OculusQuest threads on ASL fingerspelling via hand tracking | Deaf-community need quotes; documented occlusion complaints |

**Confidence note.** Sources 1–5 and 7–12 are read directly. The 2020 Girardin app's exact recognition behaviour comes from RoadtoVR's summary rather than the app itself — treat the "23 handshapes" figure as reported, not verified. Everything in §5 is my judgement, not measurement; the weights are in `THE_IDEA.md` Part II and the sensitivity analysis in §3.4 so you can re-run it against different assumptions.

---

## 8. THE ONE THING TO REMEMBER

> **We stopped trying to be novel in the mechanic and started being novel in the feedback.**
>
> The mechanic — track a hand, compare it to a stored pose — is six years old and free on GitHub. What no one has built is a system that tells you **which sign you accidentally made**, that **publishes its sensor's blind spots**, that **refuses to overclaim its learning outcomes**, and that **adapts its reference to your anatomy**.
>
> That is a defensible position in front of a panel that has seen 650 entries, and it survives contact with anyone who has actually tried to build this before.
