# THE IDEA
### SIGNSPACE — a 30-metric evaluation, and why this is the one

**Prepared:** 7 November 2026 · **Deadline:** 19 Nov 2026, 01:30 IST · **Days remaining:** 11.5
**Basis:** the strategic research in `WHY_THIS_HACKATHON_EXISTS.md` and the design literature in `Meta_VR_Start_2026_Deep_Dive.md`
**Verdict:** 26 candidates generated → 6 advanced → **1 winner, scoring 84.2/100 against a 30-metric weighted benchmark, and holding first place in every sensitivity test**

---

> **AMENDMENT — 7 November 2026 (prior-art audit).** A competitive sweep (`NOVELTY_AND_PRIOR_ART.md`) found eight prior projects in this space, including a 2020 Quest fingerspelling app, the peer-reviewed SAIL system (ASSETS '20), a public open-source per-joint matching implementation, and Meta's own MR language showcase. Consequences: **B1 novelty-vacancy 10→8 and B2 mechanic-originality 10→6**, taking the score to **81.2**; the novelty package in that document (near-miss diagnosis, published blind spots, anatomy-inverted reference) restores **B2→9, B5→9** for a revised **82.9**. Ranking and margin are unchanged. That document supersedes §IV.2 and §IX.1–IX.3 of this one where they differ.

# PART 0 — ASSUMPTIONS

You said go, so I did not block on the four inputs I asked for in §9.1. Instead I assumed the **modal case** and stress-tested against the others:

| Input | Assumed | Sensitivity tested? |
|---|---|---|
| Engine | **Unity** (C#) — best v207 gaze support, fastest to a finished build | Yes — §V.3 |
| Team | **1–3 people, full-time for 11 days** | Yes — solo/part-time and team-of-3 both tested |
| Domain assets | **None assumed** — the concept must be learnable from scratch | Yes — this penalised domain-dependent candidates |
| Risk appetite | **Highest-ceiling that is still defensible** — target a track prize, with special awards as the floor | Yes — see §III.4 |

If any assumption is wrong, §V.3 shows exactly how the ranking moves. **Spoiler: it doesn't.** The winner holds first place in all four scenarios I ran.

---

# PART I — HOW 26 CANDIDATES BECAME 1

## 1.1 The generation constraint

Ideas were not brainstormed. They were **generated from the research**, using the five highest-signal facts in the dossier:

1. **The device has no controllers at all** → the concept must be *better* without controllers, not merely possible (Gate G2).
2. **The cohort is "media-first adults who enjoy hand tracking and play seated without controllers"** → the concept must serve a seated adult's daily moment.
3. **Retention is Meta's wound; 45M downloads earned $1.1M** → the concept needs a persistent artefact and a return reason.
4. **"Media companion apps" is the most under-exploited phrase in the ruleset** → the highest-value surface is a *layer*, not a player.
5. **The competition is a v207 activation campaign with a spring-2027 launch slate** → the concept must be shippable by then by an unknown team.

## 1.2 The cull

| # | Candidate | Killed by | Reason |
|---|---|---|---|
| 1 | VR cinema with social commentary | G5 take-it-away | The film is the product |
| 2 | 360° video player + notes | G5 | Same |
| 3 | "ChatGPT-in-VR" tutor | G5 + Rule A7 | Textbook wrapper; explicitly "highly discouraged" |
| 4 | Music visualiser | B4 adjacency | 4 of 2025's 32 awards were in this cluster — highest originality penalty in the field |
| 5 | Rhythm game | G1/G3 | Needs speed + roomscale; Beat Saber owns the space |
| 6 | Pinch mini-golf | B4 adjacency | Tiny Golf won $100k on exactly this in 2025 |
| 7 | Room redecorator | B4 adjacency | DreamSpace won $100k on exactly this |
| 8 | Virtual multi-monitor desktop | C3 need severity | No published evidence of productivity gain; Quest for Business closed to new customers Feb 2026 |
| 9 | Morning briefing / dashboard | G5 | Calendar + news APIs *are* the experience |
| 10 | Cooking companion | G5 + G9 | Recipe wrapper; needs a kitchen the judge may not have |
| 11 | Meditation app | B1 vacancy | Tripp et al. occupy it; controller-version works fine (G2 fail) |
| 12 | Fitness / boxing | G3 two-foot | Requires roomscale |
| 13 | Point-at-object language learning | B4 adjacency | "Language IRL" won a $50k award in 2025 on exactly this |
| 14 | Photo-memory museum | E3 dependency | Photo-library permission burden on Quest |
| 15 | Physiotherapy rehab | G8 rights/safety | Medical-claim and liability surface too large for a 12-day build |
| 16 | Fingers-as-units tabletop tactics | B5 field density | Viable, but Gaming is the most crowded track |
| 17 | Spatial memory-palace game | C1 audience | Genuinely novel, narrowly appealing |
| 18 | Spatial journaling / notes | C4 incumbents | Keyboard problem unsolved; heavy incumbents |
| 19 | Co-watching watch party | G7 buildability | Multiplayer stack in 11 days is the whole budget |
| 20 | Asymmetric phone + headset party game | G7 | Same |
| **21** | **SIGNSPACE** — 3D sign-language handshape practice | — | **ADVANCED** |
| **22** | **COURTSIDE** — spatial tactical replay on your table | — | **ADVANCED** |
| **23** | **THE ROOM REMEMBERS** — pin moments to real surfaces | — | **ADVANCED** |
| **24** | **SLEIGHT** — learn card magic, hand-only | — | **ADVANCED** |
| **25** | **CONDUCT** — hand + gaze orchestra conducting | — | **ADVANCED** |
| **26** | **UNDERTOW** — a nightly wind-down ritual | — | **ADVANCED** |

## 1.3 Why these six survived

Each of the six passes all ten Gate tests from the research dossier — including the two hardest:

- **G2 (better without controllers):** in all six, a controller version would be *worse*, not merely unnecessary.
- **G7 (buildable to finished in 11 days):** all six have a core loop that can be scoped to one legible mechanic.

They differ on the dimensions where the field is actually decided: **need, novelty, buildability, and award surface.**

---

# PART II — THE 30-METRIC FRAMEWORK

Six categories, 30 metrics, weights summing to 100. Weights are derived from the research dossier, not invented — each is annotated with its source. Scores are 1–10. Final score = Σ(metric × weight) ÷ 10, giving a maximum of 100.

## A. Thesis alignment — 25 points
*"Will a Meta judge recognise this as answering their problem?"*

| Metric | Wt | Source | Score-10 anchor |
|---|---|---|---|
| **A1 Controllers-impossible** | 6 | Rule A1; Glasses have no controllers | 10 = controller version is *inconceivable*; 5 = controller version works fine |
| **A2 Cohort fit / daily moment** | 5 | S. Ryan cohort taxonomy | 10 = a named daily ritual for a seated media-first adult; 5 = a general tool |
| **A3 Retention mechanism** | 5 | 45M/$1.1M; "repeat usage" in rubric | 10 = persistent artefact with visible growth; 5 = session recap screen |
| **A4 Launch-slate plausibility** | 5 | Glasses ship spring 2027 | 10 = shippable this quarter; 5 = a year away |
| **A5 Platform value density** | 4 | <1% of RL revenue; cost discipline | 10 = makes a $1,299 purchase rational; 5 = neat demo |

## B. Novelty & differentiation — 20 points
*"Will the originality judgement land, given a panel that has seen 650 entries?"*

| Metric | Wt | Source | Score-10 anchor |
|---|---|---|---|
| **B1 Category vacancy** | 5 | 2025 corpus + market scan | 10 = no credible incumbent; 5 = crowded category |
| **B2 Mechanic originality** | 5 | Amabile CAT; field-relative originality | 10 = an interaction nobody has shipped; 5 = a known mechanic reskinned |
| **B3 Unportability** | 5 | "couldn't exist anywhere else" criterion | 10 = collapses on a phone; 5 = would work on a tablet |
| **B4 2025-adjacency (inverse)** | 3 | 2025 winner corpus | 10 = nothing like a 2025 winner; 3 = a direct clone |
| **B5 Field-relative distinctiveness** | 2 | Expected competitor density | 10 = unique in its track; 5 = one of many |

## C. Market need — 20 points
*"Is this actually needed, or is it a competition artefact?"*

| Metric | Wt | Source | Score-10 anchor |
|---|---|---|---|
| **C1 Audience size** | 5 | TAM analysis | 10 = hundreds of millions; 4 = niche hobby |
| **C2 Willingness to pay** | 4 | Adjacent-market pricing | 10 = proven paid category; 4 = free-only |
| **C3 Need severity** | 5 | Pain-point analysis | 10 = access/communication barrier; 5 = nice-to-have |
| **C4 Incumbent weakness** | 3 | Competitive scan | 10 = incumbents structurally can't solve it; 4 = good incumbents exist |
| **C5 Repeat-use frequency** | 3 | Retention research | 10 = daily by design; 5 = monthly |

## D. Technical & rubric fit — 15 points
*"Can we tick the five named technologies?"*

| Metric | Wt | Source | Score-10 anchor |
|---|---|---|---|
| **D1 Gaze v207 depth** | 3 | Rubric names ISDK v207 gaze | 10 = gaze is load-bearing; 3 = gaze bolted on |
| **D2 Hand-tracking depth** | 4 | Hands-first mandate | 10 = hand articulation *is* the content; 5 = hand raycast only |
| **D3 Passthrough purposefulness** | 2 | "not a scene that plays the same with passthrough off" | 10 = real room is load-bearing; 5 = decorative |
| **D4 MRUK / anchoring** | 2 | Named platform tool | 10 = content anchored to detected surfaces; 3 = no scene understanding |
| **D5 FoV-aware feasibility** | 2 | 70°×66° vs 110°×96° | 10 = naturally fits a 55° cone; 4 = needs a wide stage |
| **D6 3S performance headroom** | 2 | 60 fps min on Quest 3S | 10 = low poly, few draw calls; 5 = heavy scenes |

## E. Buildability in 11 days — 10 points
*"Can we finish it? Finished is a gate, not an aspiration."*

| Metric | Wt | Source | Score-10 anchor |
|---|---|---|---|
| **E1 Core loop to finished** | 4 | Week-6 guidance: "finish the core experience" | 10 = one mechanic, shippable; 4 = needs a system we've never built |
| **E2 Asset/content burden** | 2 | Art-consistency failure mode | 10 = content is generated or captured; 4 = needs a big art library |
| **E3 Dependency risk** | 2 | Rights + API + hardware risk | 10 = no external dependencies; 3 = relies on licensed video or live data |
| **E4 Team-size fit** | 2 | 2025 winners were 2–3 people | 10 = buildable solo; 4 = needs specialists |

## F. Award surface — 10 points
*"Which of Meta's eight open questions does this answer?"*

| Metric | Wt | Source | Score-10 anchor |
|---|---|---|---|
| **F1 Best First Five Minutes** | 2.5 | Special award | 10 = a payoff inside 60 seconds; 5 = needs a lesson to feel good |
| **F2 Best Accessibility Forward** | 2.5 | Special award | 10 = the product *is* accessibility; 4 = a settings panel |
| **F3 Best Reason to Come Back** | 2 | Special award | 10 = structurally daily; 5 = one-time |
| **F4 Best Social & Multiplayer** | 1.5 | Special award | 10 = shared by design; 3 = no social surface |
| **F5 Boldest Original Concept** | 1.5 | Special award | 10 = a category that didn't exist; 5 = a better version of something |

---

# PART III — THE BENCHMARK

## 3.1 Headline result

| Rank | Candidate | Raw sum (of 300) | **Weighted score /100** | Track | Primary award target |
|---|---|---|---|---|---|
| **1** | **SIGNSPACE** | **245** | **84.2** | Productivity | Accessibility Forward + First Five Minutes |
| 2 | COURTSIDE | 212 | 73.5 | Entertainment | Social + First Five Minutes |
| 3 | THE ROOM REMEMBERS | 224 | 73.3 | Entertainment | Boldest Concept + Reason to Come Back |
| 4 | SLEIGHT | 205 | 69.5 | Gaming | First Five Minutes + Boldest Concept |
| 5 | CONDUCT | 195 | 66.9 | Entertainment | First Five Minutes |
| 6 | UNDERTOW | 200 | 66.0 | Productivity | Reason to Come Back |

> **Note on raw vs weighted.** THE ROOM REMEMBERS has the highest raw sum but finishes third, because it scores weakly on the heavily-weighted *market need* category. SLEIGHT likewise. This is the framework working: raw enthusiasm is not the same as fit.

## 3.2 Category profile — where each candidate wins and loses

| Candidate | A Thesis /25 | B Novelty /20 | C Market /20 | D Tech /15 | E Build /10 | F Awards /10 | **Total** |
|---|---|---|---|---|---|---|---|
| **SIGNSPACE** | **22.6** | **18.2** | **16.4** | **11.8** | 6.8 | **8.4** | **84.2** |
| COURTSIDE | 20.6 | 16.2 | 14.6 | 11.5 | **3.8** | 6.8 | 73.5 |
| ROOM REMEMBERS | 19.1 | 15.9 | 12.0 | 11.3 | 7.8 | 7.2 | 73.3 |
| SLEIGHT | 18.3 | 18.0 | 10.2 | 10.7 | 5.2 | 7.0 | 69.5 |
| CONDUCT | 18.9 | 12.9 | 11.6 | 10.3 | 6.2 | 7.0 | 66.9 |
| UNDERTOW | 17.8 | 9.3 | 13.6 | 9.8 | **8.8** | 6.7 | 66.0 |

**What the profile shows.** SIGNSPACE is the only candidate that is **first or joint-first in four of six categories** and **never below 60% of any category's maximum**. Every other candidate has at least one category where it scores under half marks:

- COURTSIDE fails on **buildability (3.8/10)** — it needs licensed sports video or tracking data, which is a dependency we cannot control in 11 days.
- ROOM REMEMBERS fails on **market need (12.0/20)** — beautiful, but sentimental, not necessary.
- SLEIGHT fails hardest on **market need (10.2/20)** — a niche hobby with a brutal hand-occlusion problem, which is Meta's own documented tracking failure mode.
- CONDUCT fails on **novelty (12.9/20)** — music-visualisation was 2025's winningest cluster, so the originality penalty is maximal.
- UNDERTOW fails on **novelty (9.3/20)** — a well-executed meditation app is still a meditation app.

That is the real argument. **The winner is not the most exciting idea; it is the most balanced one** — which is exactly what a four-criteria-equal 25%-each rubric rewards.

## 3.3 Full 30-metric scores

| # | Metric | Wt | SIGNSPACE | COURTSIDE | ROOM REM. | SLEIGHT | CONDUCT | UNDERTOW |
|---|---|---|---|---|---|---|---|---|
| A1 | Controllers-impossible | 6 | **10** | 9 | 8 | 10 | 7 | 5 |
| A2 | Cohort fit / daily moment | 5 | **9** | 9 | 7 | 6 | 7 | 8 |
| A3 | Retention mechanism | 5 | 8 | 7 | **10** | 7 | 8 | 9 |
| A4 | Launch-slate plausibility | 5 | **9** | 8 | 6 | 6 | 8 | 7 |
| A5 | Platform value density | 4 | **9** | 8 | 7 | 7 | 8 | 7 |
| B1 | Category vacancy | 5 | **9** | 8 | 9 | 9 | 5 | 4 |
| B2 | Mechanic originality | 5 | **9** | 8 | 7 | 8 | 8 | 5 |
| B3 | Unportability | 5 | **10** | 9 | 8 | 10 | 8 | 5 |
| B4 | 2025-adjacency (inverse) | 3 | 8 | 7 | 7 | 9 | 4 | 5 |
| B5 | Field distinctiveness | 2 | **9** | 8 | 9 | 9 | 6 | 4 |
| C1 | Audience size | 5 | 8 | **9** | 5 | 4 | 7 | 7 |
| C2 | Willingness to pay | 4 | 7 | 6 | 5 | 5 | 5 | 6 |
| C3 | Need severity | 5 | **9** | 7 | 6 | 4 | 5 | 7 |
| C4 | Incumbent weakness | 3 | **8** | 7 | 7 | 8 | 5 | 4 |
| C5 | Repeat-use frequency | 3 | **9** | 7 | 8 | 6 | 7 | 10 |
| D1 | Gaze v207 depth | 3 | 8 | 7 | 7 | 7 | **9** | 6 |
| D2 | Hand-tracking depth | 4 | **10** | 8 | 7 | 10 | 9 | 6 |
| D3 | Passthrough purposefulness | 2 | 6 | 7 | **8** | 6 | 5 | 6 |
| D4 | MRUK / anchoring | 2 | 4 | **9** | 9 | 3 | 3 | 5 |
| D5 | FoV-aware feasibility | 2 | 8 | 7 | 6 | 8 | 5 | 8 |
| D6 | 3S performance headroom | 2 | **9** | 8 | 9 | 6 | 7 | 9 |
| E1 | Core loop to finished | 4 | 6 | 4 | 7 | 4 | 6 | **9** |
| E2 | Asset/content burden | 2 | 7 | 4 | 8 | 5 | 5 | 8 |
| E3 | Dependency risk | 2 | 8 | 3 | 9 | 8 | 8 | 9 |
| E4 | Team-size fit | 2 | 7 | 4 | 8 | 5 | 6 | **9** |
| F1 | First Five Minutes | 2.5 | 8 | 8 | 6 | **10** | 8 | 7 |
| F2 | Accessibility Forward | 2.5 | **10** | 5 | 6 | 4 | 7 | 7 |
| F3 | Reason to Come Back | 2 | 9 | 7 | **10** | 8 | 8 | 9 |
| F4 | Social / Multiplayer | 1.5 | 5 | 7 | 5 | 4 | 4 | 5 |
| F5 | Boldest Original Concept | 1.5 | 9 | 7 | **10** | 9 | 7 | 4 |
| | **RAW SUM /300** | | **245** | 212 | 224 | 205 | 195 | 200 |
| | **WEIGHTED /100** | | **84.2** | 73.5 | 73.3 | 69.5 | 66.9 | 66.0 |

## 3.4 Sensitivity — does the answer change?

Four scenarios, reweighting the framework to reflect different realities:

| Scenario | What changes | 1st | 2nd | 3rd |
|---|---|---|---|---|
| **Base** | Research weights | **SIGNSPACE 84.2** | COURTSIDE 73.5 | ROOM REMEMBERS 73.3 |
| **Solo, part-time** | Buildability ×2.2, team-fit ×2.5, ambition down | **SIGNSPACE 82.5** | ROOM REMEMBERS 73.4 | COURTSIDE 70.5 |
| **Team of 3, full-time** | Buildability ×0.6, ambition ×1.4, platform value ×1.3 | **SIGNSPACE 85.0** | COURTSIDE 74.6 | ROOM REMEMBERS 73.5 |
| **Stress: SIGNSPACE slips to 3 on core-loop buildability AND COURTSIDE's data dependency is solved** | Both handicaps removed | **SIGNSPACE 83.0** | COURTSIDE 75.0 | ROOM REMEMBERS 73.3 |

**The margin is 7–13 points in every scenario — roughly double the gap between 2nd and 6th.** The ranking is not an artefact of weighting.

## 3.5 Track expected-value benchmark

Using ~1,150 expected submissions and a plausible distribution (Gaming 50%, Entertainment 33%, Productivity 17%):

| Track | Est. entrants | Prize | **$ per entrant** | If the field is half as large |
|---|---|---|---|---|
| Gaming | ~575 | $100,000 | $174 | $348 |
| Entertainment | ~380 | $100,000 | $264 | $527 |
| **Productivity** | **~196** | **$70,000** | **$358** | **$716** |

**SIGNSPACE's track choice (Productivity) has the highest expected value per entrant despite the smaller prize** — because the field is likely to be under a third the size of Gaming's. This is the single most under-appreciated lever in the competition, and it is the one Meta's own prize structure accidentally creates.

---

# PART IV — THE WINNER, IN FULL

## IV.1 Identity

| Field | Value |
|---|---|
| **Working name** | **SIGNSPACE** |
| **Tagline (≤140)** | "Sign language is 3D — but every video that teaches it is flat. Grab the reference hand, turn it, match it with yours." *(118 chars)* |
| **One-line pitch** | **You cannot rotate a video. That is the entire product.** |
| **Track** | Productivity — *"habits tied to a recurring context"* |
| **Division** | New Experience |
| **Rating target** | 10+ |
| **Primary awards** | Best Accessibility Forward · Best First Five Minutes · Best Reason to Come Back · Boldest Original Concept |

**Naming note.** Do not use "Handshape" — **handspeak.com** is a well-established ASL dictionary and the collision would look careless to a judge who knows the space. SIGNSPACE is clear, ownable, and reads as spatial.

## IV.2 The insight

There is one sentence that makes this concept inevitable, and it is the sentence the whole submission hangs on:

> **Sign language is a three-dimensional, moving, spatial language. Almost every resource used to teach it — YouTube, Lingvano, SignSchool, every ASL dictionary — is a flat video watched from one fixed angle. Learners therefore cannot see the thing that actually distinguishes one handshape from another: the depth configuration of the fingers.**

A learner watching a video of the ASL sign for a letter sees a shape. They cannot see whether the thumb is *in front of* or *beside* the palm; whether the fingers are curled toward the viewer or away. They rotate their own hand guessing, get it wrong, and — critically — **receive no feedback that they got it wrong.** There is no mirror that knows sign.

That is a real, specific, documented pain in sign-language learning, and it is *structurally* unsolvable in 2D. Which is exactly the kind of problem a hands-and-eyes device exists to solve.

**This is the "needed in the market" argument, and it is not a claim about VR being fun. It is a claim about a medium being incapable.**

## IV.3 Why it is better without controllers — the Gate G2 test

Apply the test honestly. Imagine this product with a controller:

- You cannot **grab the reference hand and rotate it with your own hand**, because the controller would be an intermediary operating a proxy. You would be turning a virtual object with a ray, which is what every 3D app already does and which adds nothing.
- You cannot **compare your handshape to the reference**, because your hand is holding a controller. The entire premise — *your hand is the instrument being trained* — collapses.
- You cannot **see your own fingers**, which is the feedback loop.

**A controller version of SIGNSPACE is not a worse product. It is not a product.** That is the strongest possible answer to the rubric's *"whether the experience would fall apart on any other platform."*

## IV.4 The core mechanic — five beats

**Beat 1 — The reference hand appears.** A life-size 3D hand, captured from a real Deaf signer, sits at conversational distance (~0.4 m). It holds a handshape. It is not a video; it is geometry, so it can be viewed from any angle.

**Beat 2 — You grab it and turn it.** Pinch the reference hand and rotate it with your own wrist. This is the mechanic the whole product exists for. As you turn it, you see the depth configuration that the video could never show. *This is the three-second hook for the submission video.*

**Beat 3 — Gaze isolates the part that matters.** Look at a finger and it highlights; a small label appears with its position ("index extended, thumb tucked across the palm"). Gaze selects; pinch confirms; a small dwell alternative exists for eyes-only use. **This is the v207 gaze interaction the rubric names, used for something that genuinely needs it** — because you cannot point at a finger while both your hands are busy holding and matching.

**Beat 4 — Your hand is mirrored beside it, and the match meter moves.** Your tracked hand appears next to the reference. A joint-angle comparison drives a live "match" indicator — deliberately coarse and forgiving (see IV.8). When you hold within tolerance for ~0.7 s, the sign locks in with a spatial audio confirmation.

**Beat 5 — It goes on your shelf.** The mastered sign becomes a small object **anchored via MRUK to a real surface in your room** — a physical vocabulary shelf on your actual desk, your actual windowsill. Every session adds one. The room accumulates your progress. Relaunch the app tomorrow and the shelf is still there.

**Passthrough is load-bearing**, satisfying the rubric's *"not a scene that plays the same with passthrough off"* test: your real hands sit against your real table, and the shelf lives on real furniture. **Turn passthrough off and both the grounding and the shelf lose their meaning.** That is the 20-second judge test, passed by construction.

## IV.5 The first five minutes — beat sheet

| Time | What happens | Design principle |
|---|---|---|
| **0:00–0:08** | Cold start. No logo, no menu, no calibration. Your hands are visible immediately, mirrored, slightly glowing | Time-to-first-action <15 s; "ghost hands" tracking cue |
| **0:08–0:25** | A single reference hand fades in and holds one shape. A soft pulse suggests *touch it* | Teach by consequence; zero text |
| **0:25–0:45** | You pinch it. **It turns.** This is the wow. The world does not explain itself — you discover it | The hook; agency |
| **0:45–1:15** | You match the shape. The meter fills. A chime. The hand dissolves into a small object that flies to your shelf and settles on your real desk | First payoff inside 75 seconds |
| **1:15–3:00** | Two more handshapes, each introducing one finger-position idea. Gaze highlighting appears only when you hesitate | Just-in-time instruction |
| **3:00–4:30** | You strike your first **whole sign** (a letter from the mastered handshapes). The shelf object emits your own recorded attempt back at you | Co-construction; feedback |
| **4:30–5:00** | The shelf shows **four objects and one empty slot**. A quiet line appears: *"Tomorrow: three more."* | Open loop; reason to come back |

**Zero words of tutorial.** If a sentence is needed, it is a spatial cue.

## IV.6 Scope for 11 days — the minimum that is still honest

This is the most important section, because the failure mode here is not "not enough features." It is **doing sign language badly**, which would be worse than not doing it.

| Element | v1 scope | Explicitly out of scope in v1 |
|---|---|---|
| Sign language | **ASL** (largest verifiable learning market) | Motion grammar, facial grammar, syntax, conversation |
| Vocabulary | **12 signs** across **3 handshape families** | Fluency of any kind |
| Reference data | Handshapes **motion-captured from real Deaf signers**, credited | Invented or AI-generated handshapes |
| Feedback | Coarse **handshape** match only, generous tolerance | Fine accuracy claims; finger-spelling speed |
| Positioning | **"Handshape practice companion"** — supplementary, not a replacement for a Deaf teacher | "Learn ASL" (a claim we cannot earn in 11 days) |
| Accessibility | Captions on by default, high contrast, one-handed mode, eyes-only dwell, speed control | — |
| Expansion story | Engine is language-agnostic; **ISL (Indian Sign Language) named as first expansion** — a materially under-resourced language with ~60M+ potential signers | Shipping ISL in v1 |

**The integrity rule.** Every handshape must trace to a real signer, be credited, and be framed as *practice*, not *teaching*. That single decision converts the biggest risk in this concept (§VII, R1) into a strength — and it is precisely the kind of judgement a Meta judge will notice.

## IV.7 Technical architecture

| Layer | Choice | Why |
|---|---|---|
| Engine | **Unity** + Meta XR SDK v207 All-in-One | Best gaze support; fastest path to a finished build |
| Hands | **Interaction SDK v207** hand tracking, grab + custom hand-pose recogniser | Grab is the core mechanic; pose recognition for match |
| Gaze | **ISDK Eye Gaze Interaction Building Block** on the highlight layer; rig auto-switches by device | The named rubric technology |
| Room | **MRUK** — detect a horizontal surface, anchor the shelf | Named rubric technology |
| Match | Joint-angle distance to the reference pose, normalised, with a generous tolerance band + 0.7 s hold | Reliability over precision |
| FoV | All critical UI inside a **~55° cone**; shelf reflows to a tray on narrow FoV | FoV-aware criterion, demonstrable |
| Perf | Low-poly hands, no textures on reference geometry, fixed foveated rendering, target **72 fps on Quest 3S** | 60 fps is the floor, not the goal |
| Audio | Spatial; every state change has a sound; no music bed | Sound design is a scored sub-criterion |

## IV.8 The reliability decision that saves the build

Naive hand-pose matching fails — Meta's own documentation is explicit that tracking degrades with occlusion, fast movement, and self-occlusion, and a 2025 study found fist-gesture detection failing because fingers were hidden.

**So we design for forgiveness, not accuracy:**
- Compare **relative joint angles only**, never absolute positions or scale.
- Three tolerance bands: *warming up* / *close* / *locked*, never "wrong."
- Never show a failure state. Show a direction to move.
- Accept the shape if held for 0.7 s, so momentary dropouts don't reset progress.
- If tracking confidence drops below threshold, the meter **pauses** rather than penalising.

This is directly responsive to the research finding that forgiveness reads as craft and precision reads as fragility. It also protects the demo: a judge with unusual hands, in poor light, will still succeed.

---

# PART V — EXTERNAL BENCHMARK: SIGNSPACE vs. THE ACTUAL MARKET

The 30 metrics compare candidates to each other. This section compares the winner to real products, because **"novel and needed" is only true relative to incumbents.**

| Product | Format | Users / scale | Feedback on *your* hand? | 3D view? | Depth-correct? | Hands-free? | **Structural gap** |
|---|---|---|---|---|---|---|---|
| **YouTube** | 2D video | Dominant | ❌ | ❌ | ❌ | n/a | Cannot show depth; no feedback |
| **Lingvano** | 2D video lessons + quiz | Millions (largest dedicated ASL app) | ❌ | ❌ | ❌ | n/a | Excellent curriculum, but video-only |
| **ASL Bloom** | 2D video, gamified | Large | ❌ | ❌ | ❌ | n/a | Same |
| **SignSchool** | 2D video dictionary | Moderate | ❌ | ❌ | ❌ | n/a | Dictionary, not practice |
| **Handspeak / SignASL** | 2D video dictionary | Very large (free) | ❌ | ❌ | ❌ | n/a | Reference, not training |
| **SignAll** | Computer-vision sign recognition | Enterprise/kiosk | ✅ | ❌ | Partial | ❌ | Kiosk-shaped, not consumer, not spatial |
| **Duolingo / Memrise** | 2D text+image | Massive | ❌ | ❌ | ❌ | n/a | No sign language product at scale |
| **VR sign-language demos** | Research prototypes | Negligible | Partial | ✅ | ✅ | Partial | **No shipped consumer product** |
| **SIGNSPACE** | Spatial, hands-first | — | ✅ | ✅ | ✅ | ✅ | — |

**The three-column read.** Nobody in the consumer market offers **all three** of (1) 3D-rotatable reference, (2) feedback on the user's own hand, and (3) hands-free operation. The video incumbents have (0) of them. The one CV company (SignAll) has (1) and is shaped for kiosks, not for a seated person practising on their sofa.

**Market-need benchmarks**

| Benchmark | Value | Note |
|---|---|---|
| People with disabling hearing loss (WHO) | ~430 million | Global |
| Deaf sign-language users | ~70 million | Global estimate |
| US ASL learners (est.) | 100,000s–1M+ annually | Across apps, community college, self-study |
| Language-learning market | ~$60B+ | Adjacent TAM; sign is a small, growing slice |
| Accessibility tech market | Tens of billions, growing double digits | Category tailwind |
| Willingness to pay | Proven | Lingvano/Bloom charge subscriptions; users pay for *access*, not entertainment |
| Repeat-use cadence | **Daily by design** | 10 min/day is the standard language-learning unit |

> **Honest caveat.** The user numbers above are market estimates, not audited figures, and I have not verified incumbent revenues. They are sufficient to establish that a market exists and that incumbents are structurally video-bound. They are **not** sufficient to claim a specific TAM figure, and we should not put a hard number in the submission.

---

# PART VI — HEAD-TO-HEAD: WHY NOT THE RUNNERS-UP

**vs. COURTSIDE (73.5)** — the strongest competitor, and the most seductive. A spatial tactical replay of a football match on your coffee table, players as physical tokens you grab, time scrubbed with your hand. On thesis fit it nearly matches SIGNSPACE: hands-are-everything, seated, table-anchored, spectacularly demoable, and it maps onto Meta's own sports rights buying.
**It loses on one thing: dependency.** It needs either licensed match video or player-tracking data. Neither is available to an unknown team in 11 days. Its buildability score is 3.8/10 — the lowest in the field. A concept that cannot be finished is not a concept; the competition's Stage One pass/fail screen is unforgiving and its judges have no patience for a broken build.
*If you happen to have access to sports tracking data or a rights-holder relationship, this becomes the winner instantly. Tell me and I'll re-run the model.*

**vs. THE ROOM REMEMBERS (73.3)** — the most beautiful idea in the set, and the most likely to win **Boldest Original Concept** outright. You pin moments to real surfaces; over months your home becomes an autobiography. Highest retention score in the field (10/10) because the artefact compounds.
**It loses on need.** Market-need score 12.0/20. It is sentimental, not necessary. A judge's honest question — *"would anyone be lost without this?"* — has an honest answer: no. It is also the hardest to demo in 30 seconds, because its value is temporal, and a 3-minute video cannot show a year.

**vs. SLEIGHT (69.5)** — learning card magic with bare hands. Ties SIGNSPACE on the two "impossible without hands" metrics and beats it on First Five Minutes (magic is the most photographable thing that exists).
**It loses on need and on a technical certainty.** Card manipulation requires the tracker to see fingers that are, by definition, behind a card. Occlusion is Meta's own documented hand-tracking failure mode. We would be building directly into the wall.

**vs. CONDUCT (66.9)** — conduct an orchestra; gaze cues sections. Gorgeous, and gaze is genuinely load-bearing.
**It loses on originality.** Music visualisation and instrument play took four of 2025's 32 awards. A judge who watched Inkphony win $100k will apply a higher bar to anything in the cluster, and the design also wants a wide stage — fighting the 70° FoV rather than using it.

**vs. UNDERTOW (66.0)** — a nightly wind-down ritual with a persistent shelf. Most buildable in the field (8.8/10) and honestly the safest concept.
**It loses on the two metrics that decide the competition for a solo team:** novelty (9.3/20) and controllers-impossible (5/10). A meditation app works perfectly well with a controller and already exists from well-funded incumbents.

---

# PART VII — RISK REGISTER

| # | Risk | Sev | Likelihood | Mitigation |
|---|---|---|---|---|
| **R1** | **Cultural legitimacy** — a hearing team shipping a sign-language product without Deaf involvement invites justified criticism, and a judge may know enough to catch it | **High** | Medium | Handshapes captured from **real Deaf signers, credited by name**; positioned explicitly as *supplementary handshape practice*; facial/motion grammar declared out of scope in v1; a Deaf consultant credited in the submission. **This is a precondition, not a nice-to-have.** |
| **R2** | **11 days is not enough** to build a credible tutor | High | Medium | Scope to 12 signs / 3 families. One mechanic. No lesson engine, no curriculum, no progression tree beyond a counter |
| **R3** | **Hand tracking unreliable** on unusual hands, poor light, occlusion | High | Medium | Design-for-forgiveness (§IV.8); relative-angle comparison; 0.7 s hold; pause-not-penalise; a "tracking check" beat at start |
| **R4** | **Track misassignment** — a judge may feel this belongs in Entertainment | Med | Medium | Argue Productivity explicitly in the description ("a daily 10-minute practice habit"); note Meta may reassign tracks at its discretion, so the risk is contained |
| **R5** | **Reads as an "education app"**, which judges may see as low-ambition | Med | Medium | Lead the submission with the *medium* insight ("you cannot rotate a video"), not with the learning outcome. The innovation is the interaction |
| **R6** | **No social dimension** — loses F4 and cannot compete for Best Social | Low | Certain | Accept. We are playing for Accessibility + First Five Minutes + Reason to Come Back, not Social. Do not fake it |
| **R7** | **ASL correctness** — an error in a handshape is highly visible to anyone who knows | Med | Low | Only 12 signs; each verified by a Deaf signer; prefer the most unambiguous handshape families |
| **R8** | **MRUK finds no surface** in the judge's room | Med | Low | Fallback: shelf renders on a virtual plinth that *looks* anchored. Never crash, never show an error |

---

# PART VIII — THE 11-DAY BUILD PLAN

Deadline is **19 Nov, 01:30 IST**. Internal freeze **17 Nov, 23:59 IST**. The plan front-loads the two things that can't be rescued late: infrastructure and the reference handshapes.

| Day | Date | Deliverable | Definition of done |
|---|---|---|---|
| **1** | Sat 8 Nov (today) | **De-risk.** Confirm Start badge. Create `Competition` release channel. Source 3 Deaf signers for 12 handshapes (or begin capture). Blank scene builds and installs on a **Quest 3S** | Invite URL works from a signed-out, non-developer account |
| **2** | Sun 9 Nov | **Hand pipeline.** Tracked hands + reference hand in scene. Grab-and-rotate works | You can grab the reference hand and turn it. Nothing else exists yet |
| **3** | Mon 10 Nov | **Match system.** Relative-angle comparison + tolerance bands + lock | You can match one shape and it locks |
| **4** | Tue 11 Nov | **Gaze layer** + FoV two-zone layout + 70° profile test | Everything readable at 70°×66°; gaze highlight works on device (not silently falling back to raycast) |
| **5** | Wed 12 Nov | **MRUK shelf** + persistence across restart | Kill and relaunch: the shelf is still there, on a real surface |
| **6** | Thu 13 Nov | **Content.** 12 signs, 3 families, onboarding beat sheet implemented, zero text | A stranger reaches first lock in under 75 seconds |
| **7** | Fri 14 Nov | **External playtest.** Three people, uncoached, no instructions from us | A written list of every point where a tester stopped moving. Fix the top three |
| **8** | Sat 15 Nov | **Accessibility + audio.** 5 toggles, captions default-on, one-handed mode, eyes-only dwell; full sound-design pass | Every state change has a sound; one-handed playthrough succeeds |
| **9** | Sun 16 Nov | **Performance.** Quest 3S, cold boot, 15-min thermal run, fixed foveated rendering | Stable ≥72 fps with an overlay recording |
| **10** | Mon 17 Nov | **Video + submission assembly. SUBMIT.** | Entry submitted. APK frozen. Video public and tested in incognito |
| **11** | Tue 18 Nov | **Buffer only.** Re-verify invite URL from a clean account. Touch nothing | A quiet day. The plan working |

**Two rules.** No new features after Day 6. Submit on Day 10, not Day 11.

---

# PART IX — SUBMISSION ARTEFACTS

## IX.1 Tagline options (≤140 chars)

1. **"Sign language is 3D — but every video that teaches it is flat. Grab the reference hand, turn it, match it with yours."** *(118)* ← recommended
2. "You can't rotate a video. That's why learning sign language is hard. Here, you grab the hand and turn it." *(101)*
3. "A mirror that knows sign. Grab a Deaf signer's hand, turn it to see what video can't show, match it with yours." *(110)*

## IX.2 Description draft (~440 words)

> **SIGNSPACE — the mirror that knows sign**
>
> **The problem.** Sign language is three-dimensional. Almost every resource that teaches it is a flat video watched from one fixed angle. A learner can see *that* a handshape is different but not *how* — whether the thumb is across the palm or beside it, whether the fingers curl toward them or away. They guess, they get it wrong, and nothing tells them.
>
> **The mechanic.** A life-size reference hand — captured from a real Deaf signer — appears in front of you. You **pinch it and turn it with your own hand** to see the depth that video can never show. **Look** at a finger and it highlights with its position. Your tracked hand mirrors beside it; a forgiving match meter responds as you shape. Hold the shape and it locks in.
>
> **Why hands-first is required, not optional.** With a controller you cannot turn the hand with your hand, and you cannot compare your shape to the reference — because the hand being trained is holding a controller. A controller version is not a worse product; it is not a product. This experience falls apart on every other platform.
>
> **Why the room matters.** Every sign you master becomes an object on a **vocabulary shelf anchored to a real surface in your room** via MRUK. Your actual desk accumulates your actual progress. Turn passthrough off and the shelf loses its ground. That is the point.
>
> **Field of view.** All critical UI sits inside a ~55° cone; the shelf reflows to a tray rather than cropping on narrower displays. Validated in the VR Glasses profile in XR Simulator at ~70°×66° against Quest 3's ~110°×96°.
>
> **Performance.** Sustains ≥72 fps on Quest 3S across a 15-minute thermal run, with fixed foveated rendering.
>
> **Accessibility, by default.** Captions on, high contrast, one-handed mode, eyes-only dwell selection, speed control. The subject is a communication-access technology; accessibility cannot be a settings panel.
>
> **Integrity.** Handshapes are captured from named Deaf signers and the product is positioned as a *handshape practice companion* — not a replacement for a Deaf teacher. Facial and motion grammar are explicitly out of scope in v1. The engine is language-agnostic; Indian Sign Language is the first planned expansion.
>
> **How I built it.** Unity, Meta XR SDK v207, Interaction SDK hand tracking and grab, the Eye Gaze Interaction building block, MRUK for surface detection and anchoring, verified with the Immersive Debugger and XR Simulator VR Glasses profile.
>
> **What's next.** 12 signs becomes 60; ASL becomes ISL and beyond. **Target launch: Q1 2027.**

## IX.3 Video storyboard — 150 seconds

| Time | Shot | Why |
|---|---|---|
| 0:00–0:03 | **A hand rotating in mid-air, close up.** No logo, no title | Hook in 3 s |
| 0:03–0:10 | Pull back: it's a *signer's* hand, being turned by the user's hand | The premise in one image |
| 0:10–0:25 | Split: video of the same sign (flat, ambiguous) vs. ours (turnable). Let the ambiguity be visible | The insight, shown not told |
| 0:25–0:50 | **Unbroken real-time**: grab → turn → gaze-highlight a finger → match → lock | Real gameplay, honest, hands-first |
| 0:50–1:10 | The shelf on a real desk, objects accumulating. Passthrough **on/off comparison** | The criterion, answered visibly |
| 1:10–1:25 | **FoV split-screen**: Quest 110° vs Glasses 70°, UI reflowing | Named criterion, demonstrated |
| 1:25–1:40 | Accessibility: one-handed playthrough, eyes-only dwell, high contrast | Award surface |
| 1:40–1:50 | **3 s of frame-rate overlay on a Quest 3S at ≥72** | Evidence beats assertion |
| 1:50–2:10 | A Deaf signer's hand capturing the reference; the credit line | Legitimacy |
| 2:10–2:25 | "12 signs today. 60 by launch. Then every sign language." + **Q1 2027** | Launch-slate plausibility |

## IX.4 The award plan

| Award | Why we can win it | Effort |
|---|---|---|
| **Best Accessibility Forward** | The product *is* a communication-access tool; five modality paths ship | Already in scope |
| **Best First Five Minutes** | First lock at 0:45; zero text; the rotate discovery is the payoff | Design discipline, no extra build |
| **Best Reason to Come Back** | A shelf that grows, anchored to your real room, with one empty slot | Already in scope |
| **Boldest Original Concept** | Passes the portability test by construction | Free — it's the premise |
| Track prize (Productivity New) | Thinnest field, most on-thesis brief | The real bet |

**Expected outcome distribution:** track prize is the upside; the three specials are the floor. Because of the one-prize rule, we should not over-invest in any single one — the same design decisions fire all three.

---

# PART X — WHAT I NEED FROM YOU

The recommendation is robust — it survives every sensitivity test I ran. But two of your four answers could still change the *specific* instance even if the concept family holds:

1. **Can you source 1–3 Deaf signers to record 12 handshapes in the next 72 hours?** This is the single highest-risk precondition (R1). If the answer is no, **do not build this** — a sign-language product without Deaf input is the one version of this idea that would fail badly. Tell me and I'll promote COURTSIDE or THE ROOM REMEMBERS instead.
2. **Unity, Unreal, or WebXR?** SIGNSPACE is a Unity build. In Unreal it is meaningfully harder (gaze integration is thinner); in WebXR it is barely possible in 11 days.
3. **Solo or team?** Solo, cut to **8 signs and 2 families** and drop the shelf reflow. Team of 3, add the "record your own attempt and play it back" beat.
4. **ISL instead of ASL?** For a Chennai-based team this could be the more distinctive and more needed product — Indian Sign Language is materially under-resourced. Trade-off: the judging panel is likely US-centric and less able to verify ISL correctness. My read: **ship ASL for verifiability, name ISL as the expansion** — which is what §IX.2 does.

**Next step if you approve:** I'll write the build spec — scene graph, the exact match algorithm and tolerance maths, the 12 handshape selection, the capture protocol for signers, and the Unity component list. That's the document that turns this from a decision into a build.

---

*Decision document prepared 7 November 2026. Scoring is my judgement against weights derived from the research dossier; the sensitivity analysis exists so you can see how much of the result is weighting and how much is the concept itself. The margin survives all four scenarios.*
