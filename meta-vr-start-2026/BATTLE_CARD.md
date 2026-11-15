# META VR START 2026 — BATTLE CARD
### One page. Print it. Put it on the wall.

---

## THE CLOCK
Deadline **18 Nov 2026, 12:00 PM PT** = **19 Nov 2026, 01:30 IST**
Internal freeze / submit: **17 Nov 23:59 IST** — one full day early, no exceptions.

---

## THE ONE-PARAGRAPH STRATEGY

This year is not a repeat of 2025. Meta unveiled **VR Glasses** the day before the competition opened: ~100 g, **no controllers at all**, eyes and hands only, and a **~70° × 66° field of view against Quest 3's ~110° × 96°**. That single device explains three things in the rules: hands-first is now a **hard requirement**, **FoV-aware design** became a scored criterion, and **gaze interaction (ISDK v207+)** is named in the rubric. Meanwhile the special awards shifted from *what SDK did you use* to *what does the user do tomorrow*. So: build something small, finished, seated, one-handed-capable, gaze-driven, and designed at 70° — and spend your last four days on onboarding, persistence, performance, and the video, not on features.

---

## THE SIX TESTS THAT DECIDE YOUR SCORE

| # | Test | Pass condition |
|---|---|---|
| 1 | **Controller test** | Entire experience completable with controllers never paired, never in the room |
| 2 | **Airplane-seat test** | Every interaction inside a **2 ft / 0.25–0.6 m** shell from the chest |
| 3 | **One-bus-stop test** | A complete, satisfying moment in **under 10 minutes** |
| 4 | **Take-it-away test** | Remove the third-party service — is there still a product? |
| 5 | **Passthrough test** | Toggle passthrough off. Does the experience *change*? If not, you fail this criterion |
| 6 | **Untested-room test** | Works with zero detected planes, a bed instead of a table, a wall at 0.5 m |

---

## THE RUBRIC (25% each — no criterion is optional)

**Innovation & Creativity** → is it *portable elsewhere*? Answer in one sentence.
**Experience Design** → <15 s to first meaningful action; zero-text onboarding; habit-forming purpose.
**Technical Implementation** → gaze v207 + hand tracking + MRUK + spatial anchoring + FoV-aware + **≥60 fps (target 72) on a Quest 3S**.
**Polish & Presentation** → one palette, designed sound, UI states, and a video that could ship as a trailer.

> **Rule of thumb:** if you are below 8/10 in two or more columns, you are not winning a track prize. Aim your remaining days at the column with the smallest gap — not the one you enjoy.

---

## THE FIVE HIGHEST-ROI MOVES (in order)

1. **Rewrite the description + tagline + video script** — 1 day, moves 25% of the score, and it is the only artefact *every* judge sees.
2. **Re-shoot the video**: hook in 3 s, real hands-first gameplay in the first 30 s, 1920×1080, frame-rate overlay for 3 s, <3:00, no AI-generated footage.
3. **Gaze-select / pinch-confirm on the primary UI**, validated on device (not silently falling back to raycast).
4. **FoV pass**: test at 70° × 66°, reflow rather than crop, capture the screenshot pair as proof.
5. **Onboarding rebuild**: ghost hands at launch, zero text, first action under 15 s.

---

## THE SIX TRAPS THAT ACTUALLY KILL ENTRIES

- Not a **Start member** at submission time (check the forum flair — today).
- **Invite URL broken** / channel not named `Competition` / reused last year's channel.
- A **controller required somewhere** in the flow.
- App previously received **Meta funding** → ineligible outright.
- **Build modified after the deadline** → disqualification.
- **"APKs will not be considered"** — the release-channel link *is* the deliverable.

---

## THE DOUBLE-DIP TRAP

**One prize per entry. Highest value only.**
Best New Gaming ($100k) + Best Social ($25k) = **$100k**. Specials are a **floor, not a bonus**.
→ Decide which game you're playing: chase your **track**, or chase **Best First Five Minutes / Best Accessibility Forward** ($25k each, cheap to win, judged across all tracks, shallow field).

---

## DESIGN LAWS FROM THE LITERATURE

- **Gaze selects. Pinch confirms.** Never gaze-only (Midas touch). Dwell is the worst primary confirm — keep it only as an accessibility toggle.
- **One primitive, used everywhere.** Tiny Golf won $100k on *"all you need is a pinch."*
- **One-handed, always.** Reduces occlusion, reduces fatigue, and scores accessibility — three wins, one decision.
- **Never overlap hands.** "The more one hand covers the other, the lower the accuracy." — Meta
- **Pinch beats grasp for precision.** Controllers are faster at grasp; hand tracking is more accurate and preferred at pinch.
- **Style the shape, never the timing.** Rubber-banding hands destroy agency, and agency is what makes the experience unportable.
- **The room must be the level.** Passthrough as wallpaper fails a criterion a judge can test in 20 seconds.
- **Ship a persistence artefact.** Something that survives restart and visibly grows. Cheapest "reason to come back" that exists.
- **AI changes the world; it doesn't talk.** Perception-grounded or procedural beats conversational — all three 2025 AI winners were.
- **No artificial locomotion.** Move small objects, never the horizon.
- **Frame rate beats fidelity.** 60 fps with modest art beats 45 fps with great art, always.

---

## THE PORTABILITY SENTENCE (fill this in and put it in your description)

> *"This experience cannot exist on a phone, a console, or a desktop because ______________________."*

If you cannot finish that sentence with a mechanism — the room, the hands, the eyes, the depth, the real-world scale — then it isn't a Boldest Original Concept, and it probably isn't an Innovation either.

---

## BEFORE YOU SLEEP TONIGHT

- [ ] Forum flair says **Start Member** / **Start Partner**
- [ ] `Competition` release channel exists
- [ ] Tagline written (≤140 chars)
- [ ] Track + division decided
- [ ] Portability sentence written
- [ ] XR Simulator opened in the **VR Glasses profile** at least once
