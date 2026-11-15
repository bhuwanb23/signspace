# RISK REGISTER

*Reviewed at every gate. A risk without an owner and a trigger is a wish.*

| # | Risk | Sev | Likelihood | Trigger to watch | Mitigation | Owner |
|---|---|---|---|---|---|---|
| **R1** | **Cultural legitimacy** — a hearing team shipping a sign-language product without Deaf involvement | **Fatal** | Medium | No signer booked by end of **Day 1** | Handshapes captured from named, credited Deaf signers; positioned as *practice companion*; explicit "not a replacement for a Deaf teacher"; anti-claim in the description | W3 |
| **R2** | **11 days is not enough** | High | Medium | Any gate red by >1 day | Pre-agreed cut ladder (`MASTER-PLAN.md` §6); scope cap of 12 shapes; one mechanic | W5 |
| **R3** | **Hand tracking unreliable** (lighting, occlusion, unusual anatomy) | High | Medium | <8 of 20 shapes survive P1.5 | Relative-angle comparison; 3 bands; 0.7 s hold; **pause never fail**; confidence-gated near-miss; honest mirror turns it into content | W2 |
| **R4** | **Track misassignment** — judged as Entertainment | Med | Medium | Description reads like a course | Argue "daily 10-minute practice habit" explicitly; note Meta may reassign tracks at its discretion | W4 |
| **R5** | **Reads as an education app** — low ambition | Med | Medium | Video leads with learning outcome | Lead with the *medium* insight ("you cannot rotate a video"), never the syllabus | W4 |
| **R6** | **Language IRL / Spatial Lingo adjacency** — "another MR language app" | Med | **High** | A judge makes the comparison in the first 20 s | The three separations in `NOVELTY-AND-PRIOR-ART.md` §3; the near-miss as the differentiator | W4 |
| **R7** | **No social dimension** — cannot win Best Social | Low | Certain | — | Accepted. We play for Accessibility + First Five Minutes + Reason to Come Back. **Do not fake it** | W5 |
| **R8** | **ASL correctness** — a wrong handshape is highly visible to anyone who signs | Med | Low | Any shape without signer sign-off | Only 12 shapes; every one verified; prefer unambiguous families | W3 |
| **R9** | **MRUK finds no surface** (lap, bed, plane tray) | Med | Low-medium | P1.6 fails | Permanent virtual plinth fallback that reads as anchored; never an error state | W2 |
| **R10** | **Perf collapse on Quest 3S** (thermal throttling over 15 min) | High | Medium | <72 fps at 10 min | Budget in `TECH-STACK.md` §4; FFR, no shadows, 0 realtime lights; measure from P1.8, not P7 | W2 |
| **R11** | **Video overruns 3:00 or misses hands-first-by-0:30** | Med | Low | First rough cut >3:10 | Storyboard has a hard timecode budget; hands-first is the first shot, not an intention | W4 |
| **R12** | **Build breaks on Day 11** (last-minute change) | High | Low | Any commit after Day 10 | Freeze at Day 11 23:59; tag `v1.0-competition`; no edits after submission | W5 |

## The kill condition

**R1 is the only risk that is not mitigated by effort.** If no Deaf signer is captured by **Day 5**, the plan changes that night — we do not ship a sign-language product without them. The fallback concepts are ordered in `IDEA-EVALUATION.md` §3.3: COURTSIDE (73.5), THE ROOM REMEMBERS (73.3).

## Risk review log

| Date | Gate | Changed |
|---|---|---|
| 7 Nov 2026 | — | Register created at v1.0, P0.8 |
