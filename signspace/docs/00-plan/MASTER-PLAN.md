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
| **P5** | Content & capture | Days 5–7 (Wed 11–Fri 13) | 12 | 6 shipped + 6 deferred validated poses, protocol, credits object, blind-spot copy | **G5 signs are legitimately sourced** — ✅ 7/7 built as work; signer rows open (`HUMAN-TASKS` H1–H7) |
| **P6** | Gaze & accessibility | Day 7 (Fri 13) | 7 | Gaze highlight, dwell, 6 toggles + reduced motion, hand calibration | **G6 every path playable** — ✅ built, 106/106 desk checks; ⬜ device playthroughs NOT RUN (`HUMAN-TASKS` H14–H22) |
| **P7** | Polish | Day 8 (Sat 14) | 7 | Audio, zero-text cue, FoV geometry, perf posture, boot probe, hostile conditions, a11y regression | **G7 first five minutes** — 🟡 built, 131/131 desk checks; ⬜ device rows NOT RUN (`HUMAN-TASKS` H34–H40) |
| **P8** | Playtest & iterate | Day 9 (Sun 15) | 6 | 3 uncoached testers, fix list, top 3 fixed | **G8 stranger reaches first lock <75 s** |
| **P9** | Submission | Days 10–11 (Mon 16–Tue 17) | 10 | Video, copy, APK frozen, form submitted, invite URL verified | **G9 submitted + verified** |
| **P10** | Post-submission | Wed 18 → 11 Dec | 5 | Judging note, launch sketch, freeze guard, post-mortem | **G10 not evaluable** — artifact intact (`81b60582…`), entry absent |

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
| P6.1 | Eye-gaze interaction on the finger-highlight layer (hand-rolled picker; see `GAZE-INPUT.md` §3.1) | Looking at a finger highlights it, on device |
| P6.2 | **Verify gaze is really active** — never silently falling back to raycast | Debug log proves eye tracking, not head ray |
| P6.3 | Eyes-only path: dwell for every control, no pinch required (the lock stays physical) | Every control reachable with the hands still |
| P6.4 | **`N5` hand calibration:** 30 s, normalises to the user's own range; supports non-standard hands | A hand with reduced range still locks a shape |
| P6.5 | Six toggles: captions (default on), high contrast, speed, one-handed, eyes-only, near-miss | All six work, all persist, all reachable in space |
| P6.6 | Reduced-motion path | No motion-sickness triggers on the shelf animation |
| P6.7 | `docs/03-content/ACCESSIBILITY.md` written to the actual build | Doc matches build, no aspirational claims |

> **Gate G6:** a one-handed playthrough succeeds; an eyes-only playthrough succeeds; a calibrated non-standard hand succeeds.

---

### P7 — POLISH · Day 8 · 8 h

| Sub | Task | Exit criterion |
|---|---|---|
| P7.1 | Sound design: every state change has a sound | 16 clips, 12 cues, all ≤ −6 dBFS |
| P7.2 | The zero-text onboarding pass | 0 instructional words + one spatial cue |
| P7.3 | Verify the FoV reflow in the VR Glasses profile | 19.0° worst slot at 70°, tray 26.0°; device pass NOT RUN |
| P7.4 | Performance hardening on Quest 3S | 0 lights / 0 shadows / MSAA 2× / FFR medium; fps NOT RUN |
| P7.5 | Cold boot and stability sweep | probe + static splash; 5 launches NOT RUN |
| P7.6 | Crash sweep | 5 hostile conditions answered; 20 launches NOT RUN |
| P7.7 | Accessibility regression pass | P6 suite inline (106) + high-contrast reach; recordings NOT RUN |

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
| P10.1 | Quiet re-verification of the build and the invite URL on the morning of 18 Nov — **desk row written, account rows NOT RUN** |
| P10.2 | Judging-prep note: the three sentences a judge must understand, the two questions we expect — **written** |
| P10.3 | Store-launch plan sketch for Q1 2027 — the number the video promises — **written** |
| P10.4 | Archive: tag the submitted build `v1.0-competition`, keep the video master — **guard written, tag and master owed (H41, H45)** |
| P10.5 | Post-mortem: what the 11 days cost, what we'd do with 30 — **written; hours NOT RECORDED** |

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
| P0 Foundation | 🔄 **6 of 9 gate criteria met** | 14 Nov | — | **The Unity project exists, compiles with 0 errors and 0 warnings, and builds a 62 MB APK.** Licence verified. Blocked on: the headset, and the App ID |
| P1 De-risk spike | 🟡 **Desk-verified, 0 of 9 gate criteria met on device** | 15 Nov | — | Every probe is instrumented. P1.5 needs a headset. **Check `OVRManager → Hand Skeleton Version = Default (OpenXR)` first** |
| P2 Core loop | 🟡 **Desk-verified, 5 of 8 gate criteria ticked** | 15 Nov | — | Matcher, pose format, loader and shapes exist and are **measured offline**: U-V separation 3.1° → 26.0°. The metric had to change |
| P3 Feedback layer | 🟡 **Desk-verified, 4 of 7 gate criteria ticked** | 15 Nov | — | The near-miss now enforces "ambiguity is not a diagnosis" (strictly closer than the runner-up, and by a margin) — the v1 code did not |
| P4 Spatial shelf | 🟡 **Desk-verified, G4 open** | 16 Nov | — | Save schema 3 restores across five processes and survives a leave/return; plinth + pinch-only surface offer; dev-only passthrough switch; measured FoV reflow. **Owed: headset relaunch/reboot, real MRUK rooms, the two clips, the FoV capture** |
| P5 Content & capture | 🔄 **Re-scoped** | 14 Nov | — | **The six poses are authored from published references, so the signer call is ONE call, not two** (ADR-007) |
| P6 Gaze & accessibility | 🟡 **Desk-verified, 106/106 checks pass** | 15 Nov | — | **No eye tracking on Quest 3/3S** — gaze is evidenced through the VR Glasses simulator profile |
| P7 Polish | 🟡 **Desk-verified, 131/131 checks pass** | 15 Nov | — | Sound 16 clips, text 0 instructional words, FoV 19.0° at 70°×66°, perf posture clean. **Owed: sustained 72 fps, thermal, 20 launches, chime fatigue** |
| P8 Playtest | 🟡 **Instrumenting** | 17 Nov | — | `PlaytestProbe`, `PlaytestTimeline`, `playtest_report.py` exist and compile. `P8Checks` = 47/47. **Owed: the actual sessions** |
| P9 Submission | 🟡 **The build is a command** | 15 Nov | — | `BuildScript.BuildAndroid` → 62 MB APK, SHA-256 logged, bundle version code auto-increments. `P9Checks.CheckAll` = freeze record written |
| P10 Post | 🟡 **Desk-side done; G10 NOT EVALUABLE** | 18 Nov | — | `freeze_check.py` = **4 PASS / 9 FAIL / 3 NOT RUN**: APK byte-identical to the freeze record; no tag, empty commit field, 3 post-freeze commits, empty submission record, no video master. **Owed: H41–H48** |

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
| `docs/02-tech/POSE-FORMAT.md` | P2 | ✅ frozen schema + neutral contract — **v1.1: canonical 26-joint set, name-based matching, index.json** |
| `docs/02-tech/MATCH-ALGORITHM.md` | P2 | ✅ **v2: the metric changed.** The maths, the weights, the tuning procedure, and what the matcher deliberately doesn't do |
| `docs/02-tech/GAZE-INPUT.md` | P6 | ✅ **written 15 Nov** — including the fact that **no Quest 3/3S has eye tracking** |
| `docs/02-tech/MRUK-SHELF.md` | P4 | ✅ **written 15 Nov** — APIs, the plinth fallback, the FoV reflow, the 13-row test matrix |
| `docs/02-tech/PERFORMANCE.md` | P7 | ✅ **method written 15 Nov** — the numbers fill on the device; the 3S gap is written down |
| `docs/03-content/SIGNSET.md` | P5 | ✅ the 12 shapes, the 3 families, the exclusions — **now 6 shapes, 2 families** |
| `docs/03-content/CAPTURE-PROTOCOL.md` | P5 | ✅ **written 15 Nov** — one 60-minute call, the consent script, the credit states, the contingency |
| `docs/03-content/TRACKABILITY.md` | P1/P3 | ✅ **method written 15 Nov** — the table fills from the P1.5 probe |
| `docs/03-content/ACCESSIBILITY.md` | P6 | ✅ **written 15 Nov** — the five paths, the calibration, **and the non-claims section** |
| `docs/04-design/FIRST-FIVE-MINUTES.md` | P7 | ✅ beat sheet, second by second |
| `docs/04-design/UX-STATES.md` | P2 | ✅ 16 states, none a failure state |
| `docs/04-design/SOUND-MAP.md` | P7 | ✅ **written 15 Nov** — every state has a sound or a documented silence |
| `docs/05-build/RISKS.md` | P0 | ✅ 8 risks + the kill condition |
| `docs/05-build/DEFINITION-OF-DONE.md` | P0 | ✅ signed off |
| `docs/05-build/PLAYTEST-LOG.md` | P8 | ✅ **template written 15 Nov** — filled on Day 9 |
| `docs/06-submission/SUBMISSION-COPY.md` | P9 | ✅ tagline set, 500-word description |
| `docs/06-submission/VIDEO-SCRIPT.md` | P9 | ✅ **written 15 Nov** — 14 shots, 2:45, hands-first by 0:30, the hook at 0:00 |
| `docs/06-submission/CHECKLIST.md` | P9 | ✅ form map + runbook |
| `docs/08-decisions/ADR-00*.md` | — | ✅ seven decisions of record |

**35 documents, 35 written, 0 stubs.** The remaining work is measurement, not writing: the numbers
in `PERFORMANCE.md` and `TRACKABILITY.md` come from the device.

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
| `unity/Assets/_Project/Scripts/Matching/HandPose.cs` | ✅ JSON model + `SetMeta`, now with `family` |
| `.../PoseMath.cs` | ✅ **v2 metric** — per-finger chain angle, max across fingers, wrist term |
| `.../Matcher.cs` | ✅ bands · hysteresis · hold · confidence pause · near-miss · **"ambiguity is not a diagnosis"** |
| `.../JointNames.cs` | ✅ **new** — joints matched by name, so both SDK hand layouts work |
| `.../SignSetLoader.cs` | ✅ **rewritten** — `UnityWebRequest`; the old file could not read StreamingAssets on Android |
| `unity/Assets/_Project/Scripts/Hands/SkeletonPoseSource.cs` | ✅ **rewritten** — maps joints by name, reports what it could not map. One TODO left: `ReadConfidence()` |
| `.../Core/AppState.cs` | ✅ the 16-state machine, no failure state |
| `.../Core/SaveSystem.cs` | ✅ anchor-keyed persistence |
| `.../UI/DebugOverlay.cs` | ✅ the live numbers, in the cone |
| `.../Editor/SceneBuilder.cs` | ✅ the scene, built by script |
| `.../Editor/BuildScript.cs` | ✅ every Player setting as code, plus the APK hash |
| `.../Editor/RestPoseDump.cs` | ✅ dumps the rig rest pose for pose regeneration |
| `unity/Assets/StreamingAssets/Signs/*.json` | ✅ **the six shapes** + `index.json` |
| `scripts/hand_model.py` | ✅ the canonical hand model and the shape definitions |
| `scripts/make_pose.py` | ✅ writes the shapes and the index |
| `scripts/tune_match.py` | ✅ **the offline tuner** — measures the metric without a headset |
| `scripts/validate_poses.py` | ✅ now also checks the joint set and the index |
| `scripts/setup_toolchain.ps1` | ✅ installs the whole toolchain onto E:, unattended |
| `scripts/setup_unity_project.ps1` | ✅ creates the project, merges the manifest, compiles |
| `unity/SETUP.md` | ✅ scene assembly + the P1 smoke test — **update: joints are no longer pasted by hand** |

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

**Confirmed 15 Nov 2026:** deadline 19 Nov 01:30 IST (**3 days, 16 hours**) · **solo** · Unity + Android toolchain already installed · **no signer booked yet** (one is expected during playtesting).

| Day | Date | Work | Gates |
|---|---|---|---|
| **1** | Sun 15 Nov (today) | P0.1 ✅ · P0.3 ✅ (app + channels) · P0.2 form map ✅ · **P0.4 Unity build** · **licence + EULA** | G0 |
| **2** | Mon 16 Nov | **P1 de-risk**: hands, grab, wrist-rotate verdict, 20-shape probe, MRUK, persistence, fps baseline | G1 |
| **3** | Tue 17 Nov | **P2 core loop** (pose format, matcher, bands, hold, payoff) + **P3 feedback** (near-miss, honest mirror) | G2, G3 |
| **4** | Wed 18 Nov | **P4 shelf** · **P6 gaze + eyes-only** · **P5 signer review call, evening** · **P7 polish** · 2-tester playtest | G4–G8 |
| — | *Tue 17 → Wed 18* | Flip authorship state if approved → **record video → submit by 23:59 IST Wed 18** | G9 |

### Scope for four days, solo

| Decision | Value | Reason |
|---|---|---|
| Handshapes | **6: B, U, V, W, C, O** | Four from the spread family + the C/O pair. All traceable, all fingers visible, no thumb-under shapes. **Four near-miss pairs from six shapes** — highest demo value per shape in the entire set |
| Families | 2 (spread, closure) | Enough to prove the idea of families; one would look arbitrary |
| One-angle mode · hand calibration · left hand · sound polish · settle animation | ❌ cut | Cut ladder rungs 1–4 |
| Signer | **remote review, Day 3 evening** | `ADR-007` + `CAPTURE-PROTOCOL.md` §0 |

**What never gets cut:** the near-miss, the honest mirror, gaze, the shelf, the credits line, the eyes-only path.

**If the signer call slips:** ship authorship state **B** (reference-authored, review pending) and say so in the credits and the description. We do not ship silence about authorship.
