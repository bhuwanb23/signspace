# MASTER PLAN
## SIGNSPACE — Meta VR Start Developer Competition 2026

**Status:** v1.0 · **Written:** Sat 7 Nov 2026 · **Hard deadline:** Thu 19 Nov 2026, 01:30 IST
**Owner:** you · **Working days:** 11 (Sat 7 Nov → Tue 17 Nov), submit Day 11, buffer Wed 18 Nov

---

## 0. HOW TO USE THIS DOCUMENT

> **The executable version of this plan lives in [`phases/`](../../phases/README.md).** Each phase there has a folder with a charter (`README.md`), a gate (`gate.md`) and **one file per sub-phase** under `tasks/`, each containing steps, acceptance criteria, artefacts and traps. This document stays the map; that folder is the work.

This is the **phase map**, not the detail. Every phase below has sub-phases, an exit gate, and a named output document. We will open each one and go deep, in order. Nothing here is allowed to be re-litigated in chat without coming back to this file and changing it deliberately.

Three rules that govern the whole plan:

1. **No phase starts before its gate is green.** The gates exist because this build has one failure mode that kills it: a broken APK submitted on Day 11.
2. **Every phase writes a file.** If it isn't in the repo, it didn't happen.
3. **Scope only ever moves down.** The cut ladder in §6 is pre-agreed. We do not invent features after Day 6.

---

## 1. SINGLE SOURCE OF TRUTH

Any document that disagrees with this table is wrong. Change it here first.

| Constant | Value |
|---|---|
| Competition | Meta VR Start Developer Competition 2026 (Devpost 31024) |
| **Submission deadline** | **19 Nov 2026, 01:30 IST** (18 Nov, 20:00 UTC / 12:00 PT) |
| Internal freeze | Tue 17 Nov 2026, 23:59 IST |
| Winners announced | 11 Dec 2026 |
| Prize pool | $1,000,000 · 20 awards |
| Criteria | Innovation & Creativity 25 · Experience Design 25 · Technical Implementation 25 · Polish & Presentation 25 |
| **Hard tests** | Hands-first (never pair a controller) · Airplane seat (2 ft radius, ≤10 min) · Take-it-away (no thin wrapper from another platform) |
| Judging devices | Quest 3 and Quest 3S · **≥60 fps** |
| Track / division | **Productivity** / **New Experience** · Rating target 10+ |
| Engine | Unity + Meta XR SDK **v207** (All-in-One) |
| Named tech to demonstrate | **gaze, hand tracking, passthrough, spatial anchoring, FoV-aware, MRUK** |
| Product scope v1 | **12 ASL handshapes across 3 families** · **ASL** (not ISL) · supplementary practice, not a tutor |
| Match tolerance model | relative joint angles · 3 bands · 0.7 s hold · **pause, never fail** |
| Perf floor | **72 fps** on Quest 3S (floor is 60) |
| Deliverables | APK on release channel `Competition` + Invite URL · video <3:00 (hands-first by 0:30) · tagline ≤140 · description ≤500 words |
| Kill condition | **No Deaf signer captured by Day 5 → stop and re-pick the concept** |

---

## 2. THE PHASE MAP

Eleven days, eleven phases. Hours are estimates for one full-time person; halve the calendar if two people work in parallel on different phases.

| # | Phase | Calendar | Est. h | Produces | Gate |
|---|---|---|---|---|---|
| **P0** | Foundation & compliance | Day 1 (Sat 7) | 5 | Repo, Start badge, `Competition` channel, Unity project, device build | **G0** account + build plumbing |
| **P1** | De-risk spike | Day 2 (Sun 8) | 8 | Hands in scene, grab-and-rotate proof, MRUK surface found | **G1 go/no-go on mechanics** |
| **P2** | Core loop | Day 3 (Mon 9) | 10 | Reference hand → rotate → match → lock | **G2 one shape lockable** |
| **P3** | Feedback layer | Day 4 (Tue 10) | 8 | Near-miss, bands, confidence, honest mirror | **G3 the hook works** |
| **P4** | Spatial shelf | Day 5 (Wed 11) | 8 | MRUK-anchored shelf, persistence, passthrough | **G4 survives relaunch** |
| **P5** | Content & capture | Days 5–7 (Wed 11–Fri 13) | 12 | 12 signs, 3 families, capturer pipeline, credits | **G5 signs are legitimately sourced** |
| **P6** | Gaze & accessibility | Day 7 (Fri 13) | 7 | Gaze highlight, eyes-only dwell, 5 toggles, hand calibration | **G6 every path playable** |
| **P7** | Polish | Day 8 (Sat 14) | 8 | Sound, FoV reflow, zero-text onboarding, 72 fps verified | **G7 external-stranger test** |
| **P8** | Playtest & iterate | Day 9 (Sun 15) | 6 | 3 uncoached testers, fix list, top 3 fixed | **G8 stranger reaches first lock <75 s** |
| **P9** | Submission | Days 10–11 (Mon 16–Tue 17) | 10 | Video, copy, APK frozen, form submitted, invite URL verified | **G9 submitted + verified** |
| **P10** | Post-submission | Wed 18 → 11 Dec | — | Judging prep, clean-account re-verification, launch plan | — |

**Total ≈ 82 hours in 11 days.** That is 7.5 h/day, every day, with no slack. **This is the real constraint of the project** — not the idea, not the tech. Anyone joining the build doubles the available hours and is worth more than any feature.

---

## 3. CROSS-CUTTING WORKSTREAMS

Phases are the *sequence*. These five run *through* it. Each has an owner-artefact in the repo.

| ID | Workstream | Lives in | Judged by |
|---|---|---|---|
| **W1** | Product & design — mechanics, states, first five minutes | `docs/04-design/` | Experience Design 25% |
| **W2** | Engineering — tracking, gaze, MRUK, performance | `docs/02-tech/`, `unity/` | Technical Implementation 25% |
| **W3** | Content & legitimacy — signs, signers, credits, scope honesty | `docs/03-content/` | Innovation 25% + the R1 kill risk |
| **W4** | Submission & narrative — video, copy, award targeting | `docs/06-submission/` | Polish & Presentation 25% |
| **W5** | Risk & compliance — gates, rules, device compat, fallbacks | `docs/05-build/` | Whether we finish at all |

---

## 4. PHASES IN DETAIL

### P0 — FOUNDATION & COMPLIANCE · Day 1 · 5 h

Nothing here is interesting. All of it is fatal if skipped.

| Sub | Task | Exit criterion |
|---|---|---|
| P0.1 | Confirm **Meta Horizon Start** membership is active and the account is the one that will submit | Badge visible on the Devpost entry form |
| P0.2 | Register the Devpost entry, save the draft | Draft exists with the submission ID |
| P0.3 | Create app on the Meta developer dashboard; create release channel **`Competition`** | Channel visible; an empty build uploads successfully |
| P0.4 | Install Unity (6 LTS — verify patch against v207 notes), Meta XR All-in-One v207, Android toolchain | Empty scene builds and installs on the **Quest 3S** |
| P0.5 | Verify **Invite URL** flow from a *signed-out, non-developer* account | A stranger's phone opens the invite page |
| P0.6 | Repo hygiene: `.gitignore`, `assets/README.md`, branch strategy, this file committed | `git log` has a baseline commit |
| P0.7 | Lock device list: which Quest 3 / 3S builds get tested, and by whom | Named physical devices |
| P0.8 | Write `docs/05-build/RISKS.md` first draft from `IDEA-EVALUATION.md` §VII | 8 risks with owners |

> **Gate G0:** an empty APK installs from the `Competition` channel on a real headset, and the invite URL works from an account that has never seen the app. If this isn't true on Day 1, nothing downstream matters.

---

### P1 — DE-RISK SPIKE · Day 2 · 8 h

The single highest-value day. Everything unproven gets proven here or the concept changes.

| Sub | Task | Exit criterion |
|---|---|---|
| P1.1 | Hand tracking running with a custom low-poly hand visual (not the stock one) | Both hands visible, 72 fps |
| P1.2 | **Grab interaction** on a free-floating object | You can grab and move an object |
| P1.3 | **Rotate the grabbed object with your wrist** — no ray, no joystick | Object follows hand rotation naturally; feels like holding it |
| P1.4 | Load a 26-joint reference hand, pose it from a JSON definition | A stored pose reproduces faithfully |
| P1.5 | **Tracking reliability probe:** try 20 candidate handshapes on your own hands, in your own room, in daylight and lamplight | A written table: which shapes survive, which flicker, which fail |
| P1.6 | MRUK: detect a horizontal surface, spawn a test cube on it | Cube sits on your real desk |
| P1.7 | Persistence: relaunch and the cube is still there | Survives app kill |
| P1.8 | Perf baseline on Quest 3S with all of the above running | ≥72 fps, GPU level noted |

> **Gate G1 (go/no-go):** grab-and-rotate feels good on device, and **at least 8 of the 20 probed handshapes track reliably**. If grab-rotate feels bad, the concept's core gesture is wrong and we stop and re-plan *before* spending days 3–10 on it. If fewer than 8 shapes survive, the content scope changes (§6).

---

### P2 — CORE LOOP · Day 3 · 10 h

| Sub | Task | Exit criterion |
|---|---|---|
| P2.1 | Pose definition format: 26 joints → relative quaternions vs a neutral reference | Schema documented in `docs/02-tech/POSE-FORMAT.md` |
| P2.2 | **Match algorithm v1:** weighted relative-angle distance, normalised 0–1 | A score computed live, printed in the Immersive Debugger |
| P2.3 | Three bands (warming / close / locked), no binary fail | Band transitions logged, no jitter |
| P2.4 | **0.7 s hold** before lock, with dropout tolerance | Brief tracking flicker doesn't reset progress |
| P2.5 | Lock event + visual payoff (the hand dissolving into an object) | Locking feels earned |
| P2.6 | Palm-orientation term, weighted separately from finger angles | Rotating the wrist the wrong way still breaks the match |
| P2.7 | Second shape, mirrored/left-handed | Left hand works or is deliberately disabled |

> **Gate G2:** one handshape can be matched and locked, twice in a row, by someone who has never seen the app, without instruction.

---

### P3 — FEEDBACK LAYER *(`N1` near-miss + `N2` honest mirror)* · Day 4 · 8 h

The phase that makes the submission novel instead of familiar. This is not polish — it is the identity.

| Sub | Task | Exit criterion |
|---|---|---|
| P3.1 | **Near-miss diagnosis:** pairwise delta table across the 12-sign set; identify the nearest *other* sign | On a deliberate wrong shape, the app names the sign you actually made |
| P3.2 | Threshold discipline: fire only above a confidence floor; **silence is the default** | No false "you made X" in 20 trials |
| P3.3 | Copy pass on the near-miss line (short, warm, no scolding) | ≤12 words on screen |
| P3.4 | **Honest mirror panel:** the shapes today's tracking cannot resolve, greyed, with reasons | Panel ships; visible in the video |
| P3.5 | Live **tracking-confidence readout**; the exercise pauses rather than fails | Occluding a hand pauses cleanly, no fail state |
| P3.6 | Numbers: *"x of y handshape-family variants are robustly trackable today"* — verify the count on device | A real, defensible number |
| P3.7 | `docs/03-content/TRACKABILITY.md` published with the measured table | Doc matches device behaviour |

> **Gate G3:** a stranger can be made to *feel* the near-miss in under 30 seconds of play, and it never once lies to them.

---

### P4 — SPATIAL SHELF · Day 5 · 8 h

| Sub | Task | Exit criterion |
|---|---|---|
| P4.1 | Shelf anchor on a detected horizontal surface, positioned relative to it | Shelf sits on real furniture, stable |
| P4.2 | Mastered signs become shelf objects; one per sign, spawned with a settle animation | 12 slots, no overlap |
| P4.3 | Persistence via local save keyed to the room anchor | Relaunch → shelf intact, on the same surface |
| P4.4 | **Fallback**: no surface found → virtual plinth that reads as anchored | Never shows an error, never crashes |
| P4.5 | Passthrough on/off comparison mode (a debug toggle for the video) | The 20-second judge test can be filmed |
| P4.6 | Shelf reflow for narrow FoV: shelf → tray | Legible at 70°×66° |

> **Gate G4:** kill the app, relaunch, and the shelf is on the same real surface with the same objects. And a judge can flip passthrough off and see the experience lose its ground.

---

### P5 — CONTENT & CAPTURE · Days 5–7 · 12 h *(runs parallel to P4/P6)*

The legitimacy phase. **If this slips, we submit a smaller set — we never fake a handshape.**

| Sub | Task | Exit criterion |
|---|---|---|
| P5.1 | Recruit **1–3 Deaf signers**; consent form; usage rights for the shipped handshapes | Signed consent on file |
| P5.2 | Capture protocol written *before* the session (camera, lighting, angles, per-shape takes, naming) | `docs/03-content/CAPTURE-PROTOCOL.md` |
| P5.3 | Capture session: 12 target shapes + the blind-spot set, from 2–3 vantage points | Usable takes for all 12 |
| P5.4 | Retarget to the shipped 26-joint rig; hand-clean; store as pose data | `assets/hand/` populated, manifest filled |
| P5.5 | **Credit block** in-app: signer name(s), how to reach the work | Visible within the first minute |
| P5.6 | Choose the final 12 from the trackability table, not from a wishlist | `docs/03-content/SIGNSET.md` signed off |
| P5.7 | Write the blind-spot copy: what we excluded and why | Shipped in the honest mirror |

> **Gate G5:** every shipped handshape traces to a named, credited signer, and the product describes itself as *practice*, never as a course.

---

### P6 — GAZE & ACCESSIBILITY · Day 7 · 7 h

| Sub | Task | Exit criterion |
|---|---|---|
| P6.1 | Eye-gaze interaction on the finger-highlight layer (ISDK building block) | Looking at a finger highlights it, on device |
| P6.2 | **Verify gaze is really active** — never silently falling back to raycast | Debug log proves eye tracking, not head ray |
| P6.3 | Eyes-only path: dwell selection for every action, no pinch required | Full loop completable with eyes + one hand still |
| P6.4 | **`N5` hand calibration:** 30 s, normalises to the user's own range; supports non-standard hands | A hand with reduced range still locks a shape |
| P6.5 | Five toggles: captions (default on), high contrast, speed, one-handed, eyes-only | All five work, all persist |
| P6.6 | Reduced-motion path | No motion-sickness triggers on the shelf animation |
| P6.7 | `docs/03-content/ACCESSIBILITY.md` written to the actual build | Doc matches build, no aspirational claims |

> **Gate G6:** a one-handed playthrough succeeds; an eyes-only playthrough succeeds; a calibrated non-standard hand succeeds.

---

### P7 — POLISH · Day 8 · 8 h

| Sub | Task | Exit criterion |
|---|---|---|
| P7.1 | Sound design: every state change has a sound; no music bed | Sound map documented |
| P7.2 | Zero-text onboarding pass — every remaining sentence replaced by a spatial cue | ≤3 words of text in the first five minutes |
| P7.3 | FoV reflow verified in the VR Glasses profile | Nothing critical outside the safe cone |
| P7.4 | **Perf hardening on Quest 3S:** fixed foveated rendering, MSAA, no realtime shadows, LOD, texture audit | ≥72 fps over a **15-minute thermal run**, recorded |
| P7.5 | Cold-boot time to first interaction | <15 s |
| P7.6 | Crash sweep: 20 cold launches, 5 mid-session backgroundings | Zero crashes |
| P7.7 | Accessibility regression pass after every change | No toggles broken |

> **Gate G7:** a friend who has never touched a headset gets from launch to their first mastered sign without you saying a word.

---

### P8 — PLAYTEST & ITERATE · Day 9 · 6 h

| Sub | Task | Exit criterion |
|---|---|---|
| P8.1 | Three uncoached testers, three different hand sizes, one with limited dexterity if findable | Recorded sessions |
| P8.2 | Written friction log: every moment a tester stopped moving | ≥10 rows |
| P8.3 | Fix the **top three** only | Three fixes shipped |
| P8.4 | Re-run the stranger test | First lock <75 s |
| P8.5 | `docs/05-build/PLAYTEST-LOG.md` | Honest, including the failures |

> **Gate G8:** stranger reaches first lock in under 75 seconds, and the friction log's top three issues are closed.

---

### P9 — SUBMISSION · Days 10–11 · 10 h

| Sub | Task | Exit criterion |
|---|---|---|
| P9.1 | Freeze the build: version bump, keystore, IL2CPP, ARM64 | APK hash recorded |
| P9.2 | Upload to the **`Competition`** release channel | Upload succeeds, installs from invite |
| P9.3 | Record the video against the storyboard (near-miss hook at 0:00) | <3:00, hands-first by 0:30 |
| P9.4 | Edit: real-time captures, FoV split, passthrough comparison, fps overlay | No fake UI, no motion-smoothing lies |
| P9.5 | Copy: tagline ≤140 (verified), description ≤500 words (verified) | Character counts checked |
| P9.6 | **Submit the Devpost entry** | Confirmation email |
| P9.7 | Re-verify invite URL from a clean, signed-out, non-developer account | Works from a stranger's phone |
| P9.8 | Screenshots, thumbnail, tagline final check | All assets uploaded |

> **Gate G9:** submitted, confirmed by email, and an invite URL that works for someone who has never installed anything from Meta before. **Submit Day 11, not Day 12.**

---

### P10 — POST-SUBMISSION · Wed 18 Nov → 11 Dec

| Sub | Task |
|---|---|
| P10.1 | Quiet re-verification of the build and the invite URL on the morning of 18 Nov |
| P10.2 | Judging-prep note: the three sentences a judge must understand, the two questions we expect |
| P10.3 | Store-launch plan sketch for Q1 2027 — the number the video promises |
| P10.4 | Archive: tag the submitted build `v1.0-competition`, keep the video master |
| P10.5 | Post-mortem: what the 11 days cost, what we'd do with 30 |

---

## 5. DEPENDENCIES & CRITICAL PATH

```
P0 ──► P1 ──► P2 ──► P3 ──► P7 ──► P8 ──► P9 ──► SUBMIT
        │       └──► P4 ──► (P4.6 feeds P7.3)
        └──► P5 (signers must be booked on Day 1 — longest lead time)
                 └──► P5.4 blocks P2.4's real content
P6 depends on P2.6 and P3.5 ──► P6.7 blocks P7.7
```

**Longest-lead item: booking Deaf signers.** It must start on Day 1, because it is the only thing in the project that depends on another person's calendar. Everything else is solo work.

**Critical path:** P0 → P1 → P2 → P3 → P7 → P8 → P9. Note that P3 sits on it: the near-miss is not a nice-to-have, it is on the road to submission.

---

## 6. THE CUT LADDER (pre-agreed, in order)

Cut from the bottom. Never skip a rung.

| Rung | Cut | Cost of cutting | Trigger |
|---|---|---|---|
| 1 | `N4` one-angle game mode | Loses the play loop; keeps the product | Day 7 not green |
| 2 | `N5` hand calibration | Weakens the accessibility argument | Day 7 not green |
| 3 | Left-hand support | Right-hand-only play | Day 6 not green |
| 4 | Shelf settle animation, sound pass | Visual charm only | Day 8 not green |
| 5 | **12 signs → 6 signs, 1 family** | Less content, same demonstration | Day 6 not green |
| 6 | Shelf reflow for narrow FoV | Weaker FoV story | Day 8 not green |
| — | **NEVER CUT** | N1 near-miss · N2 honest mirror · gaze highlight · the shelf itself · signer credits · accessibility toggles | — |

---

## 7. DECISION GATES

| Gate | Day | Question | If red |
|---|---|---|---|
| **G0** | 1 | Does an empty build install from the `Competition` channel and open from an invite URL? | Stop everything, fix plumbing |
| **G1** | 2 | Does grab-and-rotate feel good, and do ≥8 shapes track? | Re-plan the core gesture or re-pick the concept |
| **G2** | 3 | Can a stranger lock one shape uncoached? | Fix the match feel before adding anything |
| **G3** | 4 | Does the near-miss land, and does it never lie? | Ship silence rather than a wrong diagnosis |
| **G4** | 5 | Does the shelf survive a relaunch on a real surface? | Fall back to the virtual plinth permanently |
| **G5** | 5–7 | Is every handshape credited to a real signer? | **Kill condition.** Re-pick the concept |
| **G6** | 7 | Do one-handed, eyes-only, and calibrated paths all work? | Cut the weakest path, document it |
| **G7** | 8 | Does a novice finish the first five minutes wordlessly? | Day 9 becomes a design day, not a test day |
| **G8** | 9 | First lock <75 s with three strangers? | Ship with the honest friction log |
| **G9** | 11 | Submitted, confirmed, invite verified from a clean account? | Submit anyway — partial credit beats none |

---

## 8. STATUS BOARD

Update this table at the end of every working day. It is the only place progress is tracked.

| Phase | Status | Started | Completed | Notes |
|---|---|---|---|---|
| P0 Foundation | ⬜ Not started | | | |
| P1 De-risk spike | ⬜ Not started | | | |
| P2 Core loop | ⬜ Not started | | | |
| P3 Feedback layer | ⬜ Not started | | | |
| P4 Spatial shelf | ⬜ Not started | | | |
| P5 Content & capture | ⬜ Not started | | | |
| P6 Gaze & accessibility | ⬜ Not started | | | |
| P7 Polish | ⬜ Not started | | | |
| P8 Playtest | ⬜ Not started | | | |
| P9 Submission | ⬜ Not started | | | |
| P10 Post | ⬜ Not started | | | |

---

## 9. DOCUMENT MAP — WHAT EXISTS, WHAT'S NEXT

| Doc | Phase | Status |
|---|---|---|
| `docs/00-plan/MASTER-PLAN.md` | — | ✅ this file |
| `docs/01-idea/IDEA-EVALUATION.md` | P0 | ✅ 30-metric benchmark, 26 candidates |
| `docs/01-idea/NOVELTY-AND-PRIOR-ART.md` | P0 | ✅ eight prior projects, five novelty moves |
| `docs/01-idea/CONCEPT.md` | P0 | ✅ one-page concept, the whole product on a page |
| `docs/01-idea/COMPETITION-FIT.md` | P0 | ✅ rubric → evidence mapping, award targeting |
| `docs/02-tech/TECH-STACK.md` | P0 | ✅ every package, why this and not that |
| `docs/02-tech/ARCHITECTURE.md` | P2 | ✅ systems, boundaries, data flow |
| `docs/02-tech/POSE-FORMAT.md` | P2 | ✅ frozen schema + neutral contract |
| `docs/02-tech/MATCH-ALGORITHM.md` | P2 | ✅ maths, weights, bands, tuning procedure |
| `docs/02-tech/GAZE-INPUT.md` | P6 | ⬜ stub |
| `docs/02-tech/MRUK-SHELF.md` | P4 | ⬜ stub |
| `docs/02-tech/PERFORMANCE.md` | P7 | ⬜ stub — budget, targets, measured numbers |
| `docs/03-content/SIGNSET.md` | P5 | ✅ the 12 shapes, the 3 families, the exclusions |
| `docs/03-content/CAPTURE-PROTOCOL.md` | P5 | ⬜ next — the session plan for the signers |
| `docs/03-content/TRACKABILITY.md` | P1/P3 | ⬜ filled from the P1.5 probe |
| `docs/03-content/ACCESSIBILITY.md` | P6 | ⬜ stub |
| `docs/04-design/FIRST-FIVE-MINUTES.md` | P7 | ✅ beat sheet, second by second |
| `docs/04-design/UX-STATES.md` | P2 | ✅ 16 states, none a failure state |
| `docs/04-design/SOUND-MAP.md` | P7 | ⬜ stub |
| `docs/05-build/RISKS.md` | P0 | ✅ 8 risks + the kill condition |
| `docs/05-build/DEFINITION-OF-DONE.md` | P0 | ✅ signed off |
| `docs/05-build/PLAYTEST-LOG.md` | P8 | ⬜ filled on Day 9 |
| `docs/06-submission/SUBMISSION-COPY.md` | P9 | ✅ tagline set, 500-word description |
| `docs/06-submission/VIDEO-SCRIPT.md` | P9 | ⬜ next — shot list with timecodes |
| `docs/06-submission/CHECKLIST.md` | P9 | ✅ form map + runbook |
| `docs/08-decisions/ADR-00*.md` | — | ✅ seven decisions of record |

### Phase folders — built

| Folder | Phase | Files |
|---|---|---|
| [`phases/p0-foundation/`](../../phases/p0-foundation/) | Foundation & compliance | 9 tasks + gate |
| [`phases/p1-derisk/`](../../phases/p1-derisk/) | De-risk spike | 8 tasks + gate |
| [`phases/p2-core-loop/`](../../phases/p2-core-loop/) | Core loop | 7 tasks + gate |
| [`phases/p3-feedback/`](../../phases/p3-feedback/) | Feedback layer | 7 tasks + gate |
| [`phases/p4-shelf/`](../../phases/p4-shelf/) | Spatial shelf | 6 tasks + gate |
| [`phases/p5-content/`](../../phases/p5-content/) | Content & capture | 7 tasks + gate |
| [`phases/p6-accessibility/`](../../phases/p6-accessibility/) | Gaze & accessibility | 7 tasks + gate |
| [`phases/p7-polish/`](../../phases/p7-polish/) | Polish | 7 tasks + gate |
| [`phases/p8-playtest/`](../../phases/p8-playtest/) | Playtest & iterate | 5 tasks + gate |
| [`phases/p9-submission/`](../../phases/p9-submission/) | Submission | 8 tasks + gate |
| [`phases/p10-post/`](../../phases/p10-post/) | Post-submission | 5 tasks + gate |

**76 sub-phase files across 11 phases.**

### Code that exists

| File | State |
|---|---|
| `unity/Assets/_Project/Scripts/Matching/HandPose.cs` | ✅ |
| `.../PoseMath.cs` | ✅ |
| `.../Matcher.cs` | ✅ bands · hysteresis · hold · confidence pause · near-miss |
| `.../SignSetLoader.cs` | ✅ |
| `.../Hands/SkeletonPoseSource.cs` | ✅ one TODO: `ReadConfidence()` |
| `unity/SETUP.md` | ✅ scene assembly + the P1 smoke test |
| `scripts/validate_poses.py` | ✅ |

### The deep-dive order I propose

1. **`CAPTURE-PROTOCOL.md`** — because it has the longest lead time and blocks the most.
2. **`ARCHITECTURE.md` + `POSE-FORMAT.md` + `MATCH-ALGORITHM.md`** — the three that turn the concept into code.
3. **`UX-STATES.md`** — kills ambiguity before the build starts.
4. **`TRACKABILITY.md`** — filled in from P1.5, not written in advance.
5. **`FIRST-FIVE-MINUTES.md` + `SOUND-MAP.md`** — P7 material, written early, cheap.
6. **`VIDEO-SCRIPT.md` + `CHECKLIST.md`** — last week, but the script can be drafted now.

---

## 10. THE ONE THING THAT CAN STILL KILL THIS

**Not novelty. Not technology. Not time.** It is `G5`: a sign-language product built without Deaf participation. Every mitigation in `RISKS.md` and `NOVELTY-AND-PRIOR-ART.md` depends on a real signer being in front of a camera by **Day 5**.

If that booking hasn't happened by the end of Day 1, the plan changes that night — not on Day 5.


---

## 11. THE FOUR-DAY PLAN (CONFIRMED — replaces §2)

**Confirmed 14 Nov 2026:** deadline 19 Nov 01:30 IST · **solo** · Unity + Android toolchain already installed · **no signer booked yet** (one is expected during playtesting).

| Day | Date | Work | Gates |
|---|---|---|---|
| **0** | Sat 14 Nov | P0.1 ✅ · P0.2 form map ✅ · **P0.3 app + `Competition` channel** · **P0.4 Unity build on the 3S** | G0 |
| **1** | Sun 15 Nov | **P1 de-risk**: hands, grab, wrist-rotate verdict, 20-shape probe, MRUK, persistence, fps baseline | G1 |
| **2** | Mon 16 Nov | **P2 core loop** (pose format, matcher, bands, hold, payoff) + **P3 feedback** (near-miss, honest mirror) | G2, G3 |
| **3** | Tue 17 Nov | **P4 shelf** · **P6 gaze + eyes-only** · **P5 signer review call, evening** · **P7 polish** · 2-tester playtest | G4–G8 |
| **4** | Wed 18 Nov | Flip authorship state if approved → **record video → submit by 23:59 IST** · verification only | G9 |

### Scope for four days, solo

| Decision | Value | Reason |
|---|---|---|
| Handshapes | **6: B, U, V, W, C, O** | Four from the spread family + the C/O pair. All traceable, all fingers visible, no thumb-under shapes. **Four near-miss pairs from six shapes** — highest demo value per shape in the entire set |
| Families | 2 (spread, closure) | Enough to prove the idea of families; one would look arbitrary |
| One-angle mode · hand calibration · left hand · sound polish · settle animation | ❌ cut | Cut ladder rungs 1–4 |
| Signer | **remote review, Day 3 evening** | `ADR-007` + `CAPTURE-PROTOCOL.md` §0 |

**What never gets cut:** the near-miss, the honest mirror, gaze, the shelf, the credits line, the eyes-only path.

**If the signer call slips:** ship authorship state **B** (reference-authored, review pending) and say so in the credits and the description. We do not ship silence about authorship.
