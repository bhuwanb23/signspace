# Meta VR Start Developer Competition 2026
## An End-to-End Briefing and Deep Literature Review

**Prepared:** 7 November 2026 · **Deadline:** 18 Nov 2026, 12:00 PM PT = **19 Nov 2026, 01:30 IST**
**Status:** Submissions open · 5,371 registered participants · $1,000,000 total prize pool
**Primary sources:** Official Devpost competition site (Overview, Rules, Judging, FAQs, Special Awards, Resources, Updates, Webinars), Meta VR Developer Center, Meta Connect 2026 announcements, 2025 winner corpus.

---

### How to read this document

| Part | What it covers | Read if you are… |
|---|---|---|
| **I** | The competition end-to-end: eligibility, dates, tracks, divisions, deliverables, prizes, judging, IP, tax, disqualification traps | Anyone entering |
| **II** | Evidence from the 2025 edition — what actually won and the patterns behind it | Choosing a concept |
| **III** | The literature review: 16 research domains mapped onto the four judging criteria, with design rules | Designing / building |
| **IV** | Rubric mathematics — where a marginal week of work buys the most score | Deciding what to cut |
| **V** | A 12-day execution plan in IST | Building right now |
| **VI** | Submission-package craft (video, text, build access) | Final 72 hours |
| **VII** | Research gaps — where the literature is thin and therefore where originality is cheap | Anyone aiming at "Boldest Original Concept" |
| **VIII** | Bibliography with confidence markers | Verification |

A note on citation hygiene: works marked **[verified]** were checked against a primary source during the preparation of this brief. Works marked **[canonical]** are standard, well-established references cited from knowledge — the ideas are reliable, but confirm volume/page numbers before quoting them in a formal document.

---

# PART I — THE COMPETITION, END TO END

## 1.1 Identity and provenance

| Field | Value |
|---|---|
| Official name | Meta VR Start Developer Competition 2026 |
| Devpost challenge ID | 31024 |
| Sponsor | Meta Platforms, Inc., 1 Hacker Way, Menlo Park, CA 94025 |
| Administrator | Devpost (managed hackathon) |
| Hackathon manager | keshya@devpost.com |
| Themes | AR/VR · Machine Learning/AI · Web |
| Format | Online, public, 8-week entry period |
| Total prize pool | $1,000,000 USD |
| Top prize | $100,000 (six of them) |
| Edition | 2nd annual |

The competition is the successor to the **Meta Horizon Start Developer Competition 2025** ($1.5M, 32 awards, 650+ submissions from ~2,900 developers, winners announced 23 Dec 2025).

**Naming note:** "Meta Horizon Start" and "Meta VR Start" are the same program — Meta VR Start is the rebranded name. Membership in the program is a *hard eligibility gate*, not an optional benefit. Verification method per the official FAQ: log into the Meta Developer Forum; you should see forum flair reading **"Start Member"** or **"Start Partner"** next to your username. If you applied more than 24 hours ago and don't see it, file a support ticket. Do not leave this to the final day — it is the single most common silent disqualifier.

## 1.2 The 2026 strategic context — why this year is structurally different

This is the most important section in Part I, because it determines what "good" means this year.

At **Meta Connect, 23 September 2026**, Meta unveiled **Meta VR Glasses**:

- ~100 g glasses tethered by an optical cable to a ~300 g compute puck
- Micro-OLED "5K Infinite Display", ~37 pixels per degree, ~2412×2288 per eye
- Dolby Vision + Dolby Atmos spatial audio; first IMAX Enhanced–certified VR device
- Qualcomm Snapdragon Reality Elite (Meta developer pages list XR2 Gen 3 — the sources disagree; treat the chip name as unresolved)
- ~3 hours continuous high-res media playback, 45 W fast charging
- $1,299.99; ships **spring 2027** — i.e. *nobody owns one yet*
- Runs the **same Meta Horizon OS, same Store, same SDKs** as Quest
- **Eyes and hands are the only inputs. No controllers. Ever.**
- **Field of view ≈ 70° × 66°** versus Quest 3's ≈ **110° × 96°**

The competition opened **the day after Connect** (24 September 2026), and Meta's own developer blog framed it explicitly as the on-ramp: *"We're very excited to announce that the second Start Developer Competition will run September 24 through November 18, 2026… You can build new or significantly updated gaming, entertainment, or productivity experiences for Quest, VR Glasses, or both."*

### Why this is the whole ballgame

Four consequences cascade from that hardware reveal:

1. **Hands-first stopped being a nicety and became the platform's input model.** Meta VR Glasses have no controller option at all. The competition's hard requirement — "can someone complete the entire experience without ever pairing a controller?" — is no longer a design preference. It is device compatibility.
2. **FoV-aware design became a scored judging criterion.** It did not exist as a named criterion in 2025. It appears in 2026 because a UI tuned for Quest 3's 110° horizontal FoV will lose its edges on a 70° display. Judging language: *"Since the field of view differs between Meta VR devices, keep essential UI and interactions within a comfortable, narrower FoV and adapt across devices."*
3. **Gaze became a first-class input API, not a research curiosity.** Meta XR SDK / Interaction SDK **v207** introduced **Gaze Interaction** as the headline addition — addable to a UI Canvas from Unity's Quick Actions menu, or via the Eye Gaze Interaction Building Block; the rig switches between gaze and raycast **based on device**. Judging names *"gaze interactions (Unity/Unreal ISDK v207+)"* explicitly under Technical Implementation.
4. **The Store will algorithmically favour controller-free titles** for VR Glasses users. Hands support directly affects distribution reach. That is a commercial fact judges will hold in mind when scoring "reason to come back."

### The build paths Meta recommends for v207

| Path | Requirement |
|---|---|
| **Unity** | Meta XR SDKs **v81+** (v207) All-in-One UPM package |
| **Unreal** | Unreal Engine **5.8** + Oculus/Meta Integration SDK **v207+** |
| **Android / native** | React Native via Expo, or Jetpack Compose via the Meta VR Plugin for Android Studio; Meta VR Layout SDK (multi-window), Meta VR Gaze SDK (dev preview), Meta VR UI Set SDK |
| **Immersive Web SDK (IWSDK)** | Node.js v20.19.0+; `npm create @iwsdk@latest`; deploy to GitHub Pages / Vercel |
| Also permitted | Godot, or "another engine" |

### The AI tooling that changes your build economics

Meta now ships an agentic toolchain, which matters enormously on a 12-day clock:

- **Meta VR CLI (`metavr`)** — drives a device from terminal *or* from an AI coding agent via an **MCP server**. Surfaces: device management, app installs (`metavr app install`, `-r`/`-g`/`-t`/`--downgrade`/`--verify`), logs (`metavr adb logcat`), performance traces, documentation search, 3D asset search, Store publishing. Note it has its own embedded device-communication library — no separate ADB install needed.
- **Agent skills** — teach an agent a Meta VR workflow.
- **LLM documentation indexes** — Meta publishes machine-readable versions of its docs (e.g. `developers.meta.com/horizon/llmstxt/...`). Point an agent at these instead of letting it hallucinate APIs.
- **Meta AI Developer Assistant**, **Muse Code**, **Muse Wonder** (the latter two referenced in the AI-for-QA webinar).

**Practical upshot:** the marginal cost of *technical correctness* has collapsed. What remains scarce is taste, scoping discipline, and honest polish — which is exactly what four of the judging criteria measure. Build your differentiation accordingly.

## 1.3 Eligibility — the gate list

All entrants (individual, Team, or Organization) must, as of the start of the Entry Period:

- **(a)** be at least 18 and the age of majority in their jurisdiction;
- **(b)** **not** reside in / be domiciled in **Brazil, Quebec**, or any comprehensively sanctioned area (**Crimea, Russia, Cuba, Iran, North Korea, Syria**, or any OFAC-designated country/individual) — with the rationale that banking/wire restrictions make it a regulatory violation to let someone enter who cannot legally claim the prize;
- **(c)** hold an active Meta account with **Developer Access enabled**;
- **(d)** have the submitting Representative be a **Meta VR Start member** by the time of submission.

Additional exclusions:

- Government and political officials; employees/agents/family of Meta, Devpost, and their agencies; **politically affiliated Organizations**.
- **Apps that previously received, or are currently in development with, direct financial support from Meta** (e.g. RFP funding, Horizon Start Lifestyle RFP, accelerator cash) — **ineligible**. This is the second-most-common silent disqualifier.
- Any team/org may submit **only one Entry**, via its Representative.
- Entry includes the full app *and* all related materials; one Entry per individual.

**What is explicitly *allowed* (from the FAQ — these are the reassuring lines):**

- **Managed developers** with a Meta Launch Manager / Partner Manager — eligible, provided the app isn't Meta-funded and the update is genuinely significant.
- **Previously published apps** — eligible in the Adapted division.
- **Past competition winners** — eligible again in the Adapted division for an update to the same project.
- **Start Accelerator projects** (Social VR Accelerator, Hands-First VR Accelerator) — eligible in Adapted.
- **Prior 1:1 mentorship** — fine, *as long as you do not use 1:1 mentorship on the competition project during 24 Sept – 18 Nov 2026*.
- **Ports from other platforms** — allowed (original work / rights held), but must go in the **Adapted** division.
- **No headset required** — you can build and capture footage entirely in simulators.
- **No hardware devkits provided** — the Start program does not supply devices.
- **Third-party Bluetooth accessories not supported** — your experience must not require a non-Meta device, because the globally distributed judging panel all has the same standard kit.
- **No maximum team size.**
- **No regional restriction on tools** beyond the eligible-country list.

**Eligibility for a Chennai-based entrant:** India is not in the excluded list, so a resident Indian entrant with an eligible bank account is fine. Two caveats worth planning for: (i) if you are *also* a tax resident of an excluded jurisdiction, you may still be eligible provided you reside in, hold a bank account in, and complete all documents using your address in the eligible country, with proof of legal status on request; (ii) prize payments outside the US may be subject to **US federal withholding** and you'll be issued an **IRS Form 1042-S** — budget for the possibility that the net receipt is below the headline ARV, and get treaty advice before you rely on the number.

## 1.4 Timeline, with IST conversions

| Event | PT | IST (your local) |
|---|---|---|
| Entry period opens | 24 Sep 2026, 10:00 AM PT | 24 Sep, 10:30 PM |
| **Entry period closes** | **18 Nov 2026, 12:00 PM PT** | **19 Nov 2026, 01:30 AM** |
| Judging (Stage 1 → Stage 2) | on/around 18 Nov → 9 Dec | 19 Nov → 9/10 Dec |
| Judging window (Devpost display) | 23 Nov 12:00 PM EST → 9 Dec 10:00 PM EST | 23 Nov, 10:30 PM → 10 Dec, 8:30 AM |
| **Winners announced** | **11 Dec 2026, 1:00 PM EST** | **11 Dec, 11:30 PM** |

**Calendar arithmetic that matters:** the deadline is a *post-midnight* local event on a Wednesday night / Thursday small hours. Nov 7 is a Saturday. That gives you **12 calendar days**, of which you should treat **Nov 17 as the real internal deadline** — because a release-channel invite URL that hasn't propagated, or a video still processing on YouTube, is functionally a disqualification.

Also note the **hard freeze**: after the entry period closes you may not modify the submission, and the build must remain available and unchanged through the winner announcement. Modifying after the deadline is grounds for disqualification. Schedule your last APK upload deliberately.

## 1.5 Tracks

Pick **one**. Meta reserves the right to re-assign your entry to another track at its sole discretion.

### Entertainment
> Lean-back media and content experiences — spatial cinema, interactive video, music visualization, immersive storytelling, spatial audio, media companion apps (commentary tracks, popup production notes, social comments). Content designed to be consumed in any environment. Hands-first implementation required.

Note the unusual sub-genre: **media companion apps** — commentary tracks, production notes, social comments layered on media. That is a wide-open, under-contested space and it maps neatly onto the "Best Social & Multiplayer" and "Best Agentic Interaction" specials.

### Gaming
> Games designed for hands-first (required) or eyes and hands. Puzzle, strategy, casual, social, narrative — genres that thrive seated, without controllers.

The phrase "**thrive seated, without controllers**" is doing real work. It excludes shooters, fast locomotion, and anything requiring a thumbstick. It includes puzzle, tabletop, diorama, rhythm, tower-defence, narrative, party games.

### Productivity
> Apps that make you more effective anywhere or are designed around specific daily moments — multi-panel workspaces, task management, creative tools, or habits tied to a recurring context (morning routine, commute, wind-down).

"**Habits tied to a recurring context**" is the most interesting clause in the entire ruleset. It invites a *ritual* product: a morning briefing, a commute companion (Travel Mode), a wind-down/meditation app. That framing is also the cheapest route to "Best Reason to Come Back."

## 1.6 Divisions

| | New Experience | Adapted / Significantly Updated |
|---|---|---|
| Definition | Conceived **and** built inside the competition window, from a blank slate — beginning 24 Sep 2026 | Pre-existing project (any build, prototype, or published app existing **before** 24 Sep 2026) with a meaningful new feature, mode, or platform integration shipped during the window |
| Excluded | No pre-existing codebases, no shipped titles, no repurposed early-access builds | Bug fixes, minor UI tweaks, cosmetic changes, perf optimisation without new user-facing features |
| Qualifying examples | — | Implementing hand interactions **as a new feature**; adding multiplayer; introducing an MR interaction mode; releasing a UGC creation tool |
| Evidence required | Standard submission | 1–2 paragraph summary of new features + screenshots/changelogs/demo video **strongly encouraged** |

Meta determines significance at its own discretion, and may reassign divisions.

**Why the division choice is the highest-leverage strategic decision in the whole competition:** Stage 2 applies identical criteria to both pools, but the *composition* of each pool is radically different. A "New" entry competes against other ≤8-week builds — judges will implicitly discount raw scope and reward inventiveness and completeness. An "Adapted" entry competes against studios shipping multi-year, store-published products with millions of hours of iteration — and is expected to look like it. If you have an existing app, the Adapted pool is *easier to satisfy but harder to win*; if you're starting fresh, the New pool is *harder to satisfy but winnable with a small, ruthlessly finished idea*. Part IV does the arithmetic.

## 1.7 Hard requirements and design guidelines — the three tests

These are quoted nearly verbatim because their wording *is* the grading key.

### Hard requirements

> **Hands-first:** Your experience must be fully usable with hands, end-to-end. Controller support is welcome, but not required. The test is simple: **can someone complete the entire experience without ever pairing a controller?**
>
> **Policy compliance:** adherence to Meta VR Start Program Terms, Community Standards, Code of Conduct in VR Policy, Developer App Policies, Developer Content Guidelines, and all other Developer Policies.

### Design guidelines (guidelines, not gates — but judges read them as criteria)

> **Seated-optimized:** Design for seated, stationary use. Limit roomscale, no large physical movement. Apply the **airplane seat test**: does every interaction work in a two-foot radius?
>
> **Easy to get into and out of:** Fast cold start, clean pause/resume, and something satisfying in 10 minutes or less. Apply the **one bus stop test**: can someone have a complete moment in a short ride?
>
> **Original, not a wrapper:** Core experience is Entrant-built. Thin wrappers around existing platforms or services (YouTube, ChatGPT, etc.) are **highly discouraged**. Apply the **take it away test**: if you remove the third-party service, is there still a Project?

The FAQ clarifies the seated guideline is genuinely a guideline — *"You are not required to build an app that fits this guideline for the competition"* — framed as a 2 ft diameter space "about the amount of space you'd have to move around in an airplane or bus seat without disrupting the person sitting next to you."

Additional content rules:

- **Target hardware for judging: Quest 3 / Quest 3S.** Prioritise performance for 3/3S. Backward compatibility to Quest 2/Pro is permitted but **not judged**. Apps for "AI Glasses" still must be playable on Quest 3/3S.
- **Minimum 60 fps** on Meta Quest hardware (linked to Meta's VRC performance criteria).
- Age rating must be 10+, 13+, or 18+ per Developer Content Guidelines. No obscene, offensive, hateful, or adult content.
- **English only, or English subtitles.**
- No identifiable person other than entrants may be mentioned or depicted.
- No commercial/corporate advertising, brand names, slogans, logos, recognisable branded products, or commercial artwork.
- No implication of association between Meta and any individual or entity.
- No malware; no false/deceptive/defamatory content.
- Must be **free of charge** and accessible through the winner announcement.
- Must use only **publicly released** SDKs/features — *no unreleased Meta tools*.
- Must be **original** to you, or public domain, or Meta-provided.
- No third-party rights violations (copyright, moral rights, trademark, trade secret, privacy, publicity).

## 1.8 Deliverables — exactly three artefacts

### (1) The playable build — get access right, or nothing else counts

| Build path | What to submit |
|---|---|
| **Unity, Unreal, Godot, Android/native, other engines** | An **APK** uploaded to a **new release channel named `Competition`** in the Meta VR Developer Dashboard, plus an **Invite URL** so judges can access it |
| **IWSDK (WebXR)** | A **public link** (GitHub Pages, Vercel, DigitalOcean, AWS, Azure…) |

Critical details buried in the FAQ:

- **"APKs will not be considered."** You cannot email a binary or attach it to Devpost. The *release-channel invite URL* is the deliverable. Do this the day you have a build that launches, not at the end.
- Invite URLs from a previous year's competition channel **should not be reused** — create a new channel.
- If you're updating a published app and can't generate a shareable URL, or your accessible build is past the **90-day limit**, Meta's recommended workaround is to draft the updated app as a **new project with a different name** in the Developer Center and upload to a **private Alpha channel**; failing that, you may submit the public **Meta Quest Store link**, provided everything stays unchanged through judging.
- The build must remain available and unchanged through judging (18 Nov 12:00 PM PT → ~11 Dec).

### (2) The demonstration video — under 3 minutes, and judges are not required to watch to the end

- < 3:00, on **YouTube or Vimeo**, **publicly visible**, link on the submission form.
- Footage of the project **as viewed on a Meta Quest device, via XR Simulator, or an equivalent emulator**.
- "**Lead with your best material.**"
- Judging page adds: *"show real gameplay that honestly represents the experience. Lead with the story and what makes your project distinct. **Don't lean on AI-generated video to carry the pitch.**"*
- Meta's own Week-6 guidance: *"open your submission video with real gameplay that makes your hands-first design clear in **the first 30 seconds**."*
- The XRCC Packaging Workshop distils the craft: **hook in the first three seconds**, a thumbnail that sparks curiosity, pacing that holds attention for a full minute, clean **1920×1080** footage.

### (3) The submission form

Required:

- Submission name
- **Tagline — 140 characters** (a hard limit; write it before you write anything else)
- **Track**: Entertainment / Gaming / Productivity
- **Division**: New Experience / Adapted–Significantly Updated
- **Text description**: inspiration, how you built it, future plans. Suggested ≤ **500 words**
- For Adapted: summary of new features/capabilities added in the window (+ screenshots, changelogs, demo videos highly recommended)
- Optional: how hand interactions are implemented, and what excites you about the future of VR
- **Target launch date** (if not already on the Store)
- Names, emails, roles of all team members

## 1.9 Prize architecture — the full table and what its shape tells you

| Award | Qty | Amount |
|---|---|---|
| **New** Gaming — Winner | 1 | **$100,000** |
| **New** Gaming — Runner-up | 1 | $50,000 |
| **New** Entertainment — Winner | 1 | **$100,000** |
| **New** Entertainment — Runner-up | 1 | $50,000 |
| **New** Productivity — Winner | 1 | $70,000 |
| **New** Productivity — Runner-up | 1 | $35,000 |
| **Adapted** Gaming — Winner | 1 | **$100,000** |
| **Adapted** Gaming — Runner-up | 1 | $50,000 |
| **Adapted** Entertainment — Winner | 1 | **$100,000** |
| **Adapted** Entertainment — Runner-up | 1 | $50,000 |
| **Adapted** Productivity — Winner | 1 | $70,000 |
| **Adapted** Productivity — Runner-up | 1 | $35,000 |
| Best Social & Multiplayer Experience | 1 | $25,000 |
| Best Agentic Interaction | 1 | $25,000 |
| Best Reason to Come Back | 1 | $25,000 |
| Best First Five Minutes | 1 | $25,000 |
| Best Accessibility Forward Experience | 1 | $25,000 |
| Boldest Original Concept | 1 | $25,000 |
| Judge's Choice | 2 | $20,000 each |

**Total: $1,000,000 exactly.** Verified: track prizes (New $405k + Adapted $405k) + specials ($150k) + Judge's Choice ($40k) = $1,000,000.

### Five structural observations

1. **Productivity winners are worth 30% less.** $70k vs $100k. Same work, less money — unless the field is proportionally thinner, which is plausible. Part IV models this.
2. **There are no "honorable mentions" this year.** 2025 had a three-tier structure ($100k/$60k/$30k). 2026 collapses to two tiers per category and reallocates the money into **eight special awards**. This *widens the tail*: there are now 20 ways to win instead of 32, but the cheapest winning ticket ($20k Judge's Choice) is more accessible than 2025's $30k honorable mention in a crowded category — because Judge's Choice is discretionary and not limited to a track.
3. **One prize per Entry. Full stop.** *"Each Entry is only eligible for one prize. If in consideration for multiple prizes, you will be awarded the highest prize value."* This is the most consequential rule most entrants will read past. It means: **a special award is a floor, not a stacking bonus.** If you win Best New Gaming ($100k) and Best Social & Multiplayer ($25k), you get $100k. Therefore special awards function primarily as a **safety net for strong entries that lose their track**, and as the *only* prize for entries that aren't top-2 in their track.
4. **Special awards span all tracks and both divisions.** *"Open to both new and adapted / significantly updated experiences across all tracks."* A Productivity app can win "Best First Five Minutes." An Entertainment app can win "Boldest Original Concept." This creates deliberate cross-category competition — and it means design choices that earn a special award are worth pursuing *even if you expect to lose your track*.
5. **Judge's Choice is awarded at discretion "for standout entries that don't fit neatly into a single track or category."** If your concept is genuinely weird, the ruleset has a lane for you — but only two of them.

## 1.10 Judging — the two-stage funnel

### Stage One: pass/fail viability
> *"The first stage will determine via pass/fail whether the Projects meet a baseline level of viability, in that the Project and Entry reasonably fits the theme and reasonably applies the required tools and features featured in the Contest."*

Key word: **reasonably**. Stage One is not where quality is assessed — it is where *non-compliance* is filtered. The Stage One killers are: build doesn't launch, invite URL broken, controller required, no video, wrong release channel, not a Start member, Meta-funded app, no hands-first path end-to-end.

### Stage Two: four equally weighted criteria (25% each)

| Criterion | Weight | What judges are told to reward | What judges are told to punish / avoid |
|---|---|---|---|
| **Innovation & Creativity** | 25% | Originality, creative ambition, **relevance to the chosen track**. Differentiation, developer ingenuity, **unique leverage of Meta device capabilities** — spatial depth, hands-first design, gaze, passthrough, scene understanding, persistent anchors — *to drive repeat usage* | Generic concepts; capability use that could be done on a 2D screen; novelty without a reason to return |
| **Experience Design** | 25% | Design, mechanics, user journey for the intended audience, **emphasising seated, hands-first, or context-specific use**. **Intuitive navigation from the moment of entry**, **clear onboarding**, a **rewarding, habit-forming purpose** | Unclear entry; tutorial walls; passthrough used decoratively — *"not a scene that plays the same with passthrough off"*; FoV-naive UI laid out for a Quest-width display |
| **Technical Implementation** | 25% | Technical expertise, **reliability**, strategic use of platform tools **particularly the features that define hands-first experiences: gaze interactions (ISDK v207+), hand tracking, passthrough, spatial anchoring, FoV-aware design**. **≥60 fps**, solid and bug-free, seamless feature integration. For passthrough: **correct MRUK scene-understanding use**, anchoring to detected planes/surfaces/room geometry, **holding up in rooms the developer never tested** | Crashes; sub-60 fps; hand tracking that fails outside ideal conditions; passthrough that ignores room geometry; hard-coded room assumptions |
| **Polish & Presentation** | 25% | Overall submission quality: **UI/UX, art direction, sound design**; visually cohesive; performant; **high-quality submission materials (text, images, video) that effectively communicate the project's value** | Sloppy video; inconsistent art; AI-generated video carrying the pitch; poor audio |

### Process mechanics

- Panel of experts designated by Meta; **may be Meta employees or third parties**; may or may not be individually listed; **may change before or during judging**; judging may run in **one or more rounds with one or more panels**.
- **AI tools may be used to assist in judging, but final decisions are made by human judges.** (Assume an LLM reads your text description and possibly your video transcript as a first filter. Write for machine legibility as well as human persuasion.)
- **Ties broken by judges' vote.**
- All decisions final and binding in Meta's sole discretion.
- Additional discretionary powers Meta reserves: reassign track, reassign division, disqualify for conflict-of-interest or unfair advantage, request proof of ownership/licence, remove/restrict any entry violating rules.

### Winner obligations
Winner Documents may be required, including **government-issued photo ID**. If notification is undeliverable or unanswered, after reasonable efforts the entrant may be disqualified and an alternate selected — **Meta will pursue at most two alternates** per prize. A valid bank account is required for prizes ≥ $1,000. Payment only after Winner Documents are received.

## 1.11 IP, rights, publicity, privacy, tax

- **You keep your IP.** *"All Entries and Projects remain the intellectual property of the individuals or Organizations that developed them."*
- **But you grant Meta a sweeping licence.** By entering you grant the Released Parties an **irrevocable, transferable, sublicensable, absolute** right to *use, edit, modify, copy, reproduce, and distribute* the Entry, in whole or in part, **in perpetuity**, in any medium, worldwide, for evaluating/administering **and for any marketing or promotional purposes**, without further review, notice, approval, consideration, or compensation beyond the chance to win.
- You also grant a non-exclusive, irrevocable, worldwide, transferable, sublicensable licence to use **your and your app's trade names, trademarks, logos, and your Meta username and profile** in connection with the contest — plus publicity releases.
- **Data:** entries and personal data shared with Meta and third parties for administration, evaluation, display, marketing, promotion, winner verification/announcement, prize fulfilment, and legal compliance, under the Supplemental Meta Platforms Technologies Privacy Policy.
- **Tax:** all taxes are the winner's responsibility. US residents at ≥$2,000 ARV get a **1099-MISC**; **non-US winners get a 1042-S**, and *"actual prize payment may be less than the total ARV."*
- **No transfers, substitutions, or cash redemptions** except at Meta's sole discretion. Teams/Orgs are paid to the **single designated Representative**, who alone allocates among members — Meta takes no role. Get your team agreement in writing *before* you submit.

## 1.12 The support ecosystem (use it — most entrants won't)

**Start program benefits:** global community, exclusive Meta events, advanced technical education, **community mentorship**, **software credits**, go-to-market guidance, **priority technical support**, and competition access. Free to join; requires 18+, good standing, supported country.

**Named Start mentors in 2026 programming:** Degly, Quentin, Sidney, Tevfik (plus guest Jelitza on Blender asset restyling, and XRCC on trailer packaging). Mentor specialisms visible in the 2025 blog include performance/optimization clinics (Angelsin) and Passthrough Camera Access / Attestation API deep-dives (Ombra).

**Session series run during this competition:**
- **Monday Unblock** (weekly, EMEA + NORAM) — live working sessions on blockers
- **AI for QA: Find the Bugs Before Players Do** — Meta Muse Wonder, AI pipelines for test planning, checks, playtest-feedback triage
- **Make It Yours: Restyling Free and Store-Bought Assets** — Blender workflow: repaint textures, unify materials, shared palette. Directly addresses the "yard sale" art-inconsistency failure mode that kills Polish scores
- **Finding Your First Players: Recruiting External Testers**
- **Playtesting Playground** (incl. a Non-Gaming Edition for entertainment/productivity)
- **Pushing Your Buttons: Fixing Engineer UI** — literally a session on the most common Polish failure
- **XRCC: Packaging Workshop** — trailer craft
- **Agentic Workflows for New & Existing Apps**
- **Show, Don't Tell: The Visual Craft of Onboarding**
- **First Build Review**, **Playtest & React**, **Fast Essentials: Playable Prototype — Ship One Complete Thing**

Recordings appear on the Webinars tab roughly a week after each session. **Resources tab** carries the technical spine: MQDH, release channels, Hands & Eyes essentials, device readiness, **FOV simulation**, adapting from controllers, **hands limitations & mitigations**, hands best practices, ISDK/microgestures/custom hand poses/grab best practices, XR Simulator, Immersive Debugger, MRUK, Immersive Web Emulator, and per-engine dev essentials.

## 1.13 Disqualification and silent-failure register

Ordered by how often they actually kill entries, not by severity.

| # | Trap | Why it happens | Mitigation |
|---|---|---|---|
| 1 | **Not a Start member at submission** | Assumed membership = application | Verify forum flair shows "Start Member"/"Start Partner" ≥48h before deadline |
| 2 | **Invite URL broken / expired / points to wrong channel** | Generated once, never re-tested; channel reused from 2025; 90-day limit | Create **new** channel named exactly `Competition`; test the invite URL **from a clean account, on a Quest 3/3S, logged out of developer mode** |
| 3 | **Controller required somewhere in the flow** | A single menu, an OS-level dialog, or a "press trigger to continue" | Full playthrough with controllers **physically removed from the room** |
| 4 | **App previously Meta-funded** | Accelerator/RFP cash received | If any Meta money touched it, switch to a genuinely new build |
| 5 | **Modifying the build after the deadline** | "One quick bug fix" on the 19th | Freeze at the internal deadline; do not touch the channel after |
| 6 | **Video not public / over 3:00 / not device footage** | Private YouTube upload; emulator footage that doesn't read as real | Public, unlisted-but-linkable, <3:00, Quest or XR Simulator footage |
| 7 | **Unreleased SDK/feature used** | Following a preview doc | Only publicly released tools |
| 8 | **Missing English subtitles for non-English content** | Assumed fine | Subtitle everything |
| 9 | **Branded/commercial content in the app** | Real product in the scene, real logo on a poster | Audit every texture, decal, screen |
| 10 | **Depicting an identifiable person** | Using a celebrity photo, or a team member's face without consent | Use synthetic avatars; get written consent for anyone depicted |
| 11 | **Two Entries from one person** | Solo entry + team Representative | One Entry per individual, maximum |
| 12 | **Sub-60 fps / crashes on judge hardware** | Tested on Quest 3 only, or on ideal dev build | Test on Quest **3S** in particular (weaker, and a listed judging device), cold-start, freshly booted |
| 13 | **Passthrough that ignores the room** | Scene plays identically with passthrough off | Wire MRUK; make the real room load-bearing |
| 14 | **Exceeding 140-char tagline / omitting launch date** | Rushed form | Validate every field the night before |
| 15 | **Team prize dispute** | No written agreement | Written allocation agreement signed by all before submission |

## 1.14 What changed from 2025 → 2026 (and why it matters)

| Dimension | 2025 | 2026 | Implication |
|---|---|---|---|
| Pool | $1.5M | $1.0M | Fewer prizes; competition intensity per dollar rises |
| Awards | 32 | 20 | Two-tier per category, no honorable mentions |
| Tracks | Entertainment, Lifestyle, Gaming (Casual + Social) | **Entertainment, Gaming, Productivity** | "Lifestyle" → "Productivity" with a much sharper brief (specific daily moments, habits) |
| Division terminology | New / significant update | **New Experience / Adapted–Significantly Updated**, explicitly named | More transparent; more entrants will correctly self-select |
| Headline hardware | Quest 3 / 3S | Quest 3 / 3S **+ Meta VR Glasses (spring 2027)** | Hands-first becomes mandatory in *both* senses; FoV becomes a criterion |
| Hands-first | Special award category ($50k × 3) | **Hard requirement** across all tracks | The 2025 "hand interactions" special is gone — it's now table stakes |
| New criteria language | — | **FoV-aware design**, **gaze interactions (ISDK v207+)** | Brand-new, thinly served, cheap points |
| AI tooling | Minimal | **Meta VR CLI + MCP, agent skills, LLM docs** | Build velocity up; AI-slop detection up |
| Anti-AI-slop guidance | — | *"Don't lean on AI-generated video to carry the pitch"* | Judges are primed to penalise generated trailers |
| Contest theme tags | AR/VR, ML/AI | AR/VR, **Machine Learning/AI**, **Web** | IWSDK/WebXR and agentic entries actively welcomed |

---

# PART II — EVIDENCE FROM THE 2025 EDITION

The single best predictor of 2026 judging behaviour is the 2025 winner list. It is a public, labelled dataset of 32 outcomes produced by an overlapping judge pool under near-identical criteria. Treat it as ground truth.

## 2.1 The corpus

| Category | Winner ($100k) | Runner-up ($60k) | Honorable mention ($30k) |
|---|---|---|---|
| Best Casual Game | **Tiny Golf** — BRAINZ GAMIFY | Little Critters – *Advanced hand tracking update* — Purple Yonder | Le Dino Labo — Realcast |
| Best Social Game | **Saber Punks** — EFish Games | FALCON — Wild Vision Games | Pack Attack — Sunflovr |
| Best Entertainment | **DreamSpace** — Pranay Garg & Shashank Vaibhav | Photon — Photon XR | (m)ORPH MR Music Experience — Kasson Crooker |
| Best Lifestyle | **Inkphony** — Lucas Martinic, Tejas Shroff, Nadja Pirchheim | The Tarot Experience VR – AI Edition — Adam James Malone | IMMERROCK — IMMERROCK MEDIA INC |

**Special awards**

| Award | Winners |
|---|---|
| Best Implementation of Hand Interactions ($50k ×3) | Hand Survivor · Pocket Lands · Awesome Hand (KAYAC GAMES) |
| Best Implementation of Passthrough Camera Access with AI ($50k ×3) | Language IRL · Chores.gg · Flows.xr |
| Best Immersive Experience with Immersive Web SDK ($30k ×2) | The Nanauts · Dun Jun |
| Best Immersive Experience with Spatial SDK ($50k ×2) | Compile Crops · — Mise — |
| Best Experience with React Native ($25k) | Magic Wonders |
| Best Android App Leveraging Features Unique to Quest ($25k) | GeoPortal |
| Best Android Utility App ($25k) | MarketView VR |
| Best Android App for Travel Mode ($25k) | OnTheGoVR |
| Judges' Choice ($30k ×6) | Into The Mind · OpenSoundLab · Lasertag (Operator & Scene Meshing) · Final Throwdown · HandCraft XR: Winter Edition · ModStorm Block Party |

## 2.2 Nine patterns the corpus induces

**P1 — Pinch is a complete control scheme, not a gesture.** Tiny Golf's own tagline — *"all you need is a pinch"* — won Best Casual Game outright. The lesson is not "add pinch." It is that a *single, learnable, one-handed primitive repeated with escalating consequence* beats a gesture vocabulary. Zero onboarding cost, infinite depth.

**P2 — The room is the level.** DreamSpace ("turn any room into stunning, immersive mixed-reality spaces"), Inkphony ("everything around you becomes an instrument"), — Mise —, and the Lasertag scene-meshing prototypes all make **the user's actual room load-bearing**. This is precisely the criterion judges are told to apply in 2026: *"the player's real room should meaningfully change the experience."* The 2025 winners were already there.

**P3 — Hands as mechanic, not as input method.** Hand Survivor, Pocket Lands, Awesome Hand, HandCraft XR all treat the hand itself as the game object — count fingers, shape a hand, occupy a pocket of space. Contrast entries that merely swapped a controller raycast for a hand raycast: those did not place. **The distinguishing move is making hand morphology or hand-space the mechanic.**

**P4 — A modality upgrade is a legitimate, winning strategy.** Little Critters placed second in Casual purely on an "**Advanced hand tracking update**." Saber Punks won Best Social Game after, by their own account, *"spen[ding] the past month absolutely grinding to get Mixed Reality integrated."* EFish also won a Judges' Choice for room-aware collisions. **Two of the four track wins in 2025 came from updates, not new builds.** This maps directly onto the 2026 Adapted division and its explicit qualifiers: new hand interactions, new multiplayer, new MR mode, new UGC tool.

**P5 — "AI" that wins is perception-grounded, not conversational.** Every PCA-with-AI winner connects a *camera* to a *model* to a *real-world consequence*: Language IRL (see an object, learn its Spanish name), Chores.gg (see a mess, get it gamified), Flows.xr. Meanwhile "The Tarot Experience VR – **AI Edition**" placed runner-up — an AI layer on narrative, not a chatbot. **No 2025 award went to a thin LLM wrapper.** For 2026's "Best Agentic Interaction," the implied rubric is *AI as core mechanic with spatial consequence*.

**P6 — Small teams beat studios.** DreamSpace: two people, $100,000. Inkphony: three people, $100,000. Tiny Golf and Saber Punks are small studios. Multiple $25k–$50k specials went to solo developers (Dun Jun: two people; GeoPortal, MarketView VR, OnTheGoVR, Flows.xr, Language IRL, Pocket Lands: essentially solo). **Scope discipline beats headcount** — exactly what Meta's Week-6 update says: *"A focused, satisfying experience is a stronger showcase than a bigger one with unfinished features."*

**P7 — Music and instrumental play is a reliable winner cluster.** Inkphony ($100k), OpenSoundLab ($30k), (m)ORPH ($30k), IMMERROCK ($30k). Four of 32 awards in one family. Why it works structurally: music visualization is inherently hands-first, inherently seated, needs no locomotion, has infinite replay value ("reason to come back"), demos trivially in 3 minutes, and satisfies "Boldest Original Concept" because the core loop depends on real-world scale and spatial placement. **The highest-yield concept cluster in the corpus.**

**P8 — Utilities with a named daily moment win.** OnTheGoVR won on Travel Mode — a *context*, not a genre. MarketView VR (utilities), GeoPortal (Quest-unique features). The 2026 Productivity brief is written in exactly this language: *"habits tied to a recurring context (morning routine, commute, wind-down)."* Meta is signalling that "an app for a moment" is a first-class product shape.

**P9 — Platform-native craft was rewarded separately — and in 2026 that lane is gone.** 2025 had dedicated awards for Spatial SDK, IWSDK, React Native, and Android (8 awards, $270k). **2026 has none.** All capability-demonstration now competes inside the three tracks under Technical Implementation. Two consequences: (i) a niche build path no longer buys a protected lane — it must earn its keep within the track; (ii) conversely, a genuinely skilful IWSDK or Spatial SDK build now *differentiates* inside a track where most rivals are on Unity.

## 2.3 The 2026 award shift: from *what API* to *what the user does tomorrow*

This is the deepest structural change, and it should drive concept selection.

| 2025 special awards asked… | 2026 special awards ask… |
|---|---|
| Which SDK did you use? (Spatial SDK, IWSDK, React Native, Android) | Will the user **come back tomorrow**? |
| Did you implement hand interactions? | Was the **first five minutes** effortless? |
| Did you combine camera + AI? | Is **AI the core**, agentically? |
| — | Is it **social**? **accessible**? **bold**? |

2026's specials are **behavioural and experiential**, not technical. Hands-first, once a bonus, is now a gate. The scoring centre of gravity has moved from *capability demonstration* to *product viability*. That is a harder bar for a 12-day build — and it is why Part IV advises spending the last third of your time on retention and onboarding rather than on a new feature.

---

# PART III — DEEP LITERATURE REVIEW

## 3.0 Method and orientation

**What this review is.** A structured synthesis of the research and technical-design literature bearing on the four equally weighted judging criteria — Innovation & Creativity, Experience Design, Technical Implementation, Polish & Presentation — plus the eight special awards. Each domain follows a fixed template:

> **Key literature → Consensus → Contested/open → Competition translation → Concrete design rules → Failure modes**

**What it is not.** It is not a PRISMA-style systematic review, and it does not attempt exhaustive coverage of a field spanning HCI, cognitive science, graphics, and media studies. It is a *decision-support* review: the goal is to convert each body of evidence into build decisions you can defend to a judge in 500 words.

**Why a literature review is the right instrument here.** This competition is judged by experts on four dimensions where the academic record is unusually mature — presence, selection technique, comfort, onboarding, retention. Most entrants build from intuition and store precedent. The marginal advantage from knowing *why* gaze+pinch works, or *why* 2 ft matters, is disproportionate, because the knowledge converts directly into (a) better design choices and (b) the vocabulary judges recognise as expertise. Technical Implementation literally names ISDK v207, MRUK, and FoV-aware design — **the vocabulary is the rubric.**

**Orientation table — which literature pays off fastest on a 12-day clock**

| Domain | Score impact | Time to implement | Priority |
|---|---|---|---|
| D4 Gaze / gaze+pinch | ★★★★★ | Low | **Do first** |
| D5 FoV-aware design | ★★★★☆ | Very low | **Do first** |
| D8 Onboarding & first five minutes | ★★★★★ | Low | **Do first** |
| D3 Direct manipulation & the reach problem | ★★★★★ | Medium | Do second |
| D6 Comfort & seated design | ★★★★☆ | Low | Do second |
| D15 Performance engineering | ★★★★☆ | Medium | Do second |
| D9 Retention & progression | ★★★★☆ | Medium | Do third |
| D12 Accessibility | ★★★★☆ | Low–medium | Cheap win |
| D7 Passthrough & scene understanding | ★★★★☆ | High | Only if core |
| D1/D2 Presence & embodiment | ★★★☆☆ | Design-level | Continuous |
| D13 Media & spatial audio | ★★★☆☆ | Medium | Track-dependent |
| D14 Productivity & legibility | ★★★★☆ | Medium | Track-dependent |
| D10 Social/multiplayer | ★★★☆☆ | High | Only if core |
| D11 Agentic AI | ★★★★☆ | High (latency risk) | Only if core |

---

## D1 — Presence, place illusion, and plausibility

**Key literature.** Slater & Sanchez-Vives (2016), *Enhancing Our Lives with Immersive Virtual Reality*, Frontiers in Robotics and AI 3:74 [canonical] — presence as **place illusion** (the qualia of "being here") plus **plausibility illusion** (the feeling that what is happening is really happening). Slater (2009), *Place illusion and plausibility can lead to realistic behaviour in immersive virtual environments*, Phil. Trans. R. Soc. B 364(1535):3549–3557 [canonical] — place illusion comes from *sensorimotor contingencies*, plausibility from *events responding to you*. Witmer & Singer (1998), *Measuring presence in virtual environments*, Presence 7(3):225–240 [canonical]. Cummings & Bailenson (2016), *How immersive is enough? A meta-analysis of the effect of immersive technology on user presence*, Media Psychology 19(2):272–309 [canonical]. Jerald (2015), *The VR Book* [canonical].

**Consensus.** Presence is not a single dial set by fidelity. It is produced by (i) sensory-motor contingency — the world updating correctly when *you* move; (ii) plausibility — the world responding appropriately when *you* act; (iii) embodiment; (iv) a coherent, non-distracting interface. Resolution matters less than assumed; **latency and tracking coherence matter more**.

**Contested / open.** Cummings & Bailenson is frequently misread as "fidelity doesn't matter." The correct reading: the *marginal* presence return from resolution is smaller than from tracking and FoV. Also open: whether presence predicts enjoyment, learning, or retention — the evidence is weak and inconsistent. Presence is a poor proxy for outcome, which matters because 2026 judging rewards *repeat usage*, not presence.

**Competition translation.** The judging language "uniquely leverage Meta device capabilities… **to drive repeat usage**" is a presence-plus-retention formulation. Presence gets you the first ten minutes; plausibility gets you the second session. The cheapest plausibility sources in a hands-first build: the world yielding to your hands (grab, deform, snap), the room acknowledging your furniture, and gaze-responsive agents. Each is a plausibility engine, not a graphics upgrade.

**Concrete design rules.** (1) Make **one** interaction loop extremely sensorimotor-coherent (1:1 hand tracking, no smoothing lag, immediate physical response) and let the rest be staged — judges forgive stylisation, they do not forgive rubber-banding hands. (2) Put a plausibility beat in the first 60 s: something that reacts *because* you looked at it or reached for it. (3) Do not chase resolution; chase **frame coherence**. (4) Treat audio as a plausibility channel, not a soundtrack.

**Failure modes.** Beautiful static environments with no causal response; hands that visibly lag the real hand; a "cinematic" opening that is only a camera on rails (kills plausibility because nothing responds).

---

## D2 — Embodiment, agency, and the virtual hand as identity

**Key literature.** Biocca (1997), *The cyborg's dilemma: Progressive embodiment in virtual environments*, JCMC 3(2) [canonical]. Kilteni, Groten & Slater (2012), *The sense of embodiment in virtual reality*, Presence 21(4):373–387 [canonical] — **self-location, agency, body ownership**. Yee & Bailenson (2007), *The Proteus effect*, Human Communication Research 33(3):271–290 [canonical]. Gonzalez-Franco & Peck (2018), *Avatar embodiment: Towards a standardized questionnaire*, Frontiers in Robotics and AI 5:74 [canonical].

**Consensus.** Embodiment = self-location + agency + ownership. **Agency is the load-bearing factor for hands-first design**: the sense that *I* caused that movement. Body ownership survives heavy stylisation, non-human morphology, and cartoon proportions — the virtual-hand literature shows ownership is driven by *synchronous visuomotor correlation*, not realism. The Proteus effect shows avatar appearance shifts subsequent behaviour, which is a design lever.

**Contested / open.** How far ownership transfers to *partial* representations (hands only, no arms) is unsettled — and matters greatly because most hands-first builds render floating hands. Also open: whether non-anatomical hand representations (tools, claws, paddles) impair fine manipulation; evidence suggests they impair *precision* while *increasing* presence and enjoyment — the exact trade-off "Awesome Hand" and "Pocket Lands" exploited.

**Competition translation.** Judging rewards "hands-first design" under Innovation and "whether the experience would fall apart on any other platform" under Boldest Original Concept. Embodiment theory locates that fragility in **agency**. An experience whose core loop requires you to feel that your hand *is* the instrument cannot be ported to a controller, mouse, or phone — because there you would be operating a proxy. State this explicitly in your description; it is the theoretical justification for "couldn't exist anywhere else."

**Concrete design rules.** (1) Immediate visuomotor coupling; if you stylise, stylise *shape* but never *timing*. (2) Prefer **one-hand-capable** interactions — Meta's hands documentation notes one-handed interactions "not only enhance accessibility but also require less physical effort" and reduce occlusion, making this simultaneously an embodiment, accessibility, and tracking-reliability decision: the highest-value architectural choice in the build. (3) If you replace hands with tools, design tasks that tolerate coarse precision. (4) Never fake agency with animation — if the hand didn't cause it, don't show the hand doing it.

**Failure modes.** Hands rendered but not causally connected; hand presence achieved only in a mirror; grabbing that snaps objects to a socket while the hand floats (breaks agency instantly).

---

## D3 — Direct manipulation, selection, and the reach problem

**Key literature.** Bowman & Hodges (1997), *An evaluation of techniques for grabbing and manipulating remote objects in immersive virtual environments*, I3D '97 [canonical]. Poupyrev, Billinghurst, Weghorst & Ichikawa (1996), *The go-go interaction technique*, UIST '96 [canonical]. Mine (1995), *Virtual environment interaction techniques*, UNC TR95-018 [canonical]. Pierce et al. (1997), *Image plane interaction techniques in 3D immersive environments*, I3D '97 [canonical]. Bowman, Kruijff, LaViola & Poupyrev (2004), *3D User Interfaces: Theory and Practice* [canonical]. MacKenzie (1992), *Fitts' law as a research and design tool in HCI*, HCI 7(1):91–139 [canonical]. Hinckley, Pausch, Goble & Kassell (1994), *Passive real-world interface props for neurosurgical visualization*, CHI '94 [canonical]. Meta, *Hands: Limitations & Mitigations* (Sep 2026) and *Designing with Hands best practices* [verified]. *Reaching further in VR: a comparative study with a novel velocity-based technique* (Virtual Reality, Nov 2025) [verified]. A 2026 study on haptics across hand tracking and controllers [verified].

**Consensus.**
- **Direct manipulation wins on naturalness and learnability; indirect (raycast, go-go, HOMER, image-plane) wins on reach and fatigue.** The classic resolution is a mode switch with a clear affordance.
- **Arm fatigue ("gorilla arm") is a hard constraint** above shoulder height or held more than a few seconds. Go-Go's modeless design forces an extended arm — a documented failure. HOMER mitigates with raycast-assisted positioning plus an explicit deactivate; the 2025 velocity-based study reports **HOMER as most effective** in its comparison and replicates Go-Go's precision/fatigue problems.
- **Hand tracking trades speed for accuracy and preference.** The 2026 haptics study: *"controllers were faster for grasp and pinch, whereas hand tracking was more accurate for pinch and preferred overall."* Extremely practical: **hand tracking is defensible for precision pinch work**, less so for fast gross grasping.
- **Precision degrades with distance**, from tremor and control-display gain.
- **Clutching by fist gesture fails** — fingers are self-occluded, documented both by the 2025 study and by Meta.

**Contested / open.** Whether clutching should be gesture-based at all given occlusion. Whether gain-based amplification is acceptable to users who notice it. Whether "microgestures" (small finger movements near the thumb) can carry a full interface — the research base is thin relative to how much Meta's SDKs support.

**Competition translation.** Judging names "hand tracking" and "spatial anchoring" under Technical Implementation, and "mechanics and user journey… emphasising seated… use" under Experience Design. The reach problem is where most hands-first entries die: a design requiring reach past ~0.5 m fails the airplane-seat test *and* the tracking-volume test simultaneously. Meta's docs state hands resting by the thighs or too close to the face (on the cheek) are outside the tracking volume — so **both too-far and too-close fail**. There is a sweet spot.

**Concrete design rules.** (1) Confine interaction to a **comfort shell roughly 0.25–0.6 m from the chest**, centred slightly below eye level — satisfies the 2 ft radius, stays inside the tracking volume, avoids gorilla arm. (2) **One primary primitive, used everywhere** (P1: Tiny Golf won on pinch alone). (3) **Prefer pinch over grasp** for precision; grasp only for large forgiving objects. (4) **Never require bilateral hand overlap** — Meta: *"The more one hand covers the other, the lower the accuracy."* (5) **Avoid fist-gesture clutching**; use wrist-up or palm-facing. (6) **Add forgiveness, not precision** — snap radii, generous colliders, sticky selection, undo; Meta's own Academy module is *"Designing Forgiving and Reversible Interactions."* (7) **Smooth with a 1€ filter** if raycasting from the hand. (8) For reach beyond the shell use **image-plane or HOMER-style indirect manipulation with an explicit, discoverable toggle** — never implicit gain.

**Failure modes.** Reaching to a distant menu; two-handed pinching and rotating; a grab requiring a closed fist at arm's length; jittering hand rays; no visual "tracked / lost" state.

---

## D4 — Gaze: from MAGIC pointing to gaze+pinch

**Key literature.** Jacob (1991), *The use of eye movements in human-computer interaction techniques*, ACM ToIS 9(2):152–169 [canonical]. Zhai, Morimoto & Ihde (1999), *Manual and gaze input cascaded (MAGIC) pointing*, CHI '99 [canonical] — gaze for target acquisition, hands for confirmation. Pfeuffer, Mayer, Mardanbegi & Gellersen (2017), *Gaze + Pinch Interaction in Virtual Reality*, SUI '17, pp. 99–108 [verified]. Wagner, Lystbæk, Manakhov, Grønbæk, Pfeuffer & Gellersen (2023), *A Fitts' Law Study of Gaze-Hand Alignment for Selection in 3D User Interfaces*, CHI '23 [verified] — throughput ordering **Gaze&Handray 2.09 > Gaze&Pinch 2.06 > Gaze&Finger 1.86**; Gaze&Pinch fits Fitts' Law well (R²≈0.90); preference ranking Gaze&Handray 9, Gaze&Pinch 7. Gabel, Schmidt, Pfeuffer & Steinicke (2024), *Guiding Handrays in Virtual Reality: Comparison of Gaze- and Object-Based Assistive Raycast Redirection* [verified]. *Pinch, Click, or Dwell: Comparing Different Selection Techniques for Eye-Gaze-Based Pointing in VR*, ETRA '21 [verified]. Pfeuffer (2024), *Design Principles and Issues for Gaze and Pinch Interaction*, arXiv:2401.10948 [verified].

**Consensus.**
1. **Gaze is an exceptional target-acquisition channel and a poor confirmation channel** — the Midas touch problem. Eyes point faster than hands travel; they cannot reliably signal commitment.
2. **The winning architecture is a cascade: gaze selects, hand confirms.**
3. **Dwell is the worst of the three confirmation methods** — slowest, lowest throughput — but the most error-tolerant and the only fully hands-free option. ETRA '21 found pinch slower and less accurate than a button click, but **not significantly so**, while dwell was significantly slower with lower throughput.
4. **Gaze+pinch throughput (~2.06 bits/s) is competitive with — and in some conditions preferred over — free-hand raycasting**, and far better than head-directed selection.
5. **Gaze+pinch scales across interaction classes** — one- vs two-handed, single vs multiple targets — by re-engaging pinch. That is what makes it a *system*, not a technique.

**Contested / open.** Handling the Midas touch in always-on gaze UIs. Whether gaze suits continuous manipulation (weak) versus discrete selection (strong). Gaze-assisted *redirection* (invisible gain) — effective but perceptually and ethically contentious. And a genuine 2026 gap: **almost no published work on gaze+pinch on a 70° FoV device**, because the devices are new.

**Competition translation.** Highest-return domain in the review, for three reasons. (1) **It is named in the rubric** — Technical Implementation says *"gaze interactions (Unity/Unreal ISDK v207+)"*, the only specific SDK version named anywhere in the criteria. (2) **It is new enough to be rare** — v207 shipped at Connect six weeks before the deadline; most entrants will still ship hand-raycast-only. (3) **It is cheap** — ISDK provides the Eye Gaze Interaction Building Block; Unity's Quick Actions menu adds Gaze Interaction to a UI Canvas in a couple of clicks; the rig auto-switches between gaze and raycast by device.

**Concrete design rules.** (1) **Adopt gaze-select / pinch-confirm as your default UI architecture**, with hand-raycast as automatic fallback where eye tracking is absent. (2) **Size gaze targets generously, space them widely** — recall Wagner et al.'s finding that image-plane techniques fit Fitts' Law less well; layout beats hitbox precision. (3) **Reject dwell as primary confirm** except as an accessibility alternative (keep it as a toggle — also your cheapest Accessibility play). (4) **Gate gaze on intent**: hover on gaze, commit on pinch, with a short confirm window to suppress Midas touch. (5) **Handle tracking loss** — eye tracking drifts, is sensitive to glasses, fails with some eye conditions; always keep a complete hand-only path. (6) **Verify the rig's device switching explicitly** — a silent fallback to raycast is invisible to you and visible to the judge.

**Failure modes.** Gaze-only selection; pinpoint 8 pt gaze targets; dwell as the only path; gaze hover with no confirm; gaze enabled on Quest 3 without validating it is *actually* active; forgetting VR Glasses have no controller at all, so the fallback is hands, not a trigger.

---

## D5 — FoV-aware design

**Key literature.** Arthur, K. W. (2000), *Effects of field of view on performance with head-mounted displays*, PhD dissertation, UNC Chapel Hill [verified] — the foundational empirical treatment: FoV affects spatial-task performance, search, and perceived presence, with diminishing returns above ~60° and distinct costs below it. Meta, *FOV simulation* documentation [verified]. Meta, *Get started with Meta VR Glasses* (Oct 2026) [verified] — VR Glasses have higher resolution per eye and a narrower FoV than Quest 3: **roughly 70° × 66° vs 110° × 96°**. Meta's Connect blog notes FoV simulation with head-direction gaze is controllable **via command line** on Quest 3/3S [verified]. A 2026 study on HMD position found small FoV reduced efficiency in perceiving procedural instructions even when task performance was preserved [verified] — users *cope* while quietly working harder.

**Consensus.** Horizontal FoV strongly modulates spatial-task performance and search time. Peripheral vision carries spatial updating and orientation; removing it shifts cost onto head movement and memory. Users adapt behaviourally and under-report the cost. **Content placed in the peripheral regions of a wide-FoV layout simply does not exist on a narrow-FoV device.**

**Contested / open.** The *right* comfortable FoV target for UI is unsettled; ~60° is a common working assumption but device- and task-dependent. And there is essentially **no published HCI research on 70° tethered glasses** — precisely why Meta wrote the criterion: they are asking the field a question it has not answered.

**Competition translation.** FoV-aware design is the **cheapest points available**, and almost nobody will claim them. Judges test on **Quest 3/3S** (110° × 96°), so they see your app with a *wide* FoV and cannot directly verify the glasses design. Therefore FoV-awareness must be **demonstrated, not merely present**: a VR Glasses device profile in XR Simulator, a FoV-simulation screenshot, a video segment showing the same scene in a ~70° frame, and one sentence naming the decision ("all critical UI is held within a 55° cone and scales by device profile; secondary panels reflow rather than crop"). Meta provides the tools free: VR Glasses profile in Meta XR Simulator (Unity and Unreal), in-headset FoV simulation, and IWSDK emulator FoV simulation.

**Concrete design rules.** (1) **Two-zone layout** — *critical zone* ≤ ~55–60° cone (primary actions, status, the thing the user must see); *ambient zone* 60–110° (decoration, presence cues, spatial audio sources). Nothing load-bearing in the ambient zone. (2) **Reflow, never crop** — on narrow FoV, secondary panels move (stack, tray, wrist menu). (3) **Drive layout from a device profile**, not hardcoded angles; the v207 rig already switches interaction by device — make UI follow. (4) **Test at 70° × 66° in XR Simulator on day one**, not day eleven. (5) **Keep text inside the critical zone** at minimum angular size; narrow FoV + high PPD means text is *sharper* but *less peripheral* — a different failure profile. (6) **Document it** — one screenshot pair (Quest-FoV vs Glasses-FoV) beats a paragraph of claims.

**Failure modes.** UI laid across 110°; HUD elements at far edges; "it works, I checked on Quest" (the judge's exact scepticism); no artefact proving the design.

---

## D6 — Comfort, cybersickness, and seated design

**Key literature.** Kennedy, Lane, Berbaum & Lilienthal (1993), *Simulator Sickness Questionnaire*, Int. J. Aviation Psychology 3(3):203–220 [canonical]. Rebenitsch & Owen (2016), *Review on cybersickness in applications and visual displays*, Virtual Reality 20:101–125 [canonical]. Stanney, Kennedy et al. on the finding that *cybersickness is not simulator sickness* (distinct symptom profiles) [canonical]. Fernandes & Feiner (2016), *Combating VR sickness through subtle dynamic field-of-view modification*, IEEE 3DUI [canonical]. Meta, *Comfort* guidance and *Hands: Limitations & Mitigations* (lighting, tracking volume, movement speed) [verified].

**Consensus.** Sickness is driven mainly by **vection** (perceived self-motion without physical motion), **latency**, and **vection/frame-rate interaction** — not by immersion per se. Mitigations with the strongest support: eliminate or minimise artificial locomotion; snap rather than smooth turning; hold frame rate; add a static reference frame (cockpit, table, fixed vignette); reduce FoV during acceleration. Seated experiences are dramatically lower-risk.

**Contested / open.** The magnitude of "FoV restriction as comfort" (real but modest, and itself reduces presence). Whether hand-tracking-based locomotion (arm-swing) is better or worse than thumbstick. How sickness interacts with long-duration productivity use — the biggest open question for the Productivity track.

**Competition translation.** The ruleset has *already solved* the sickness problem by mandating seated, low-roomscale, non-controller design — the design guidelines are effectively an anti-sickness protocol. Residual risks: **accidental vection** from a moving environment (a train window, a scrolling feed, a camera pan — even a background pan can trigger it); **latency-induced discomfort** from an under-optimised build (sickness and frame drops are the same failure seen from two angles); and **session length**, since productivity apps invite 45-minute sessions where comfort evidence is thin and judges will treat fatigue as an Experience Design flaw.

**Concrete design rules.** (1) **No artificial locomotion** — if movement is needed use *scene change* (teleport/dissolve/cut), not continuous translation; in entertainment a cut is both comfortable and cinematic. (2) **Anchor the user to a stable visual frame** — a table, plinth, console, room; reads as intentional design, not a comfort patch. (3) **Design natural break points at 8–12 minutes** (doubles as the one-bus-stop test). (4) **Ship a comfort settings panel** — comfort vignette, snap turn, background motion off, seated-height calibration; cheap, reads as professionalism, feeds the Accessibility special. (5) **Never let the horizon rotate** — move small objects, not the world.

**Failure modes.** A "cinematic" dolly; a scrolling environment; a space flythrough in a Productivity app; 45 fps in a moving scene.

---

## D7 — Passthrough, scene understanding, and spatial anchoring

**Key literature.** Sra, Garrido-Jurado, Schmandt & Maes (2016), *Procedurally generated virtual reality from 3D reconstructed physical space*, VRST '16, pp. 191–200 [verified] — generating VR from the real room as a template; the intellectual ancestor of room-aware MR. Simeone, Velloso & Gellersen (2015), *Substitutional reality: using the physical environment to design virtual reality experiences*, CHI '15 [canonical]. Lindlbauer & Wilson (2018), *Remixed reality* [canonical]. Meta, **MR Utility Kit (MRUK)** documentation — scene understanding, detected planes, room geometry, anchors [verified]. Meta, *Passthrough Camera Access (PCA)* API, and its 2025 special award lineage [verified]. And Meta's 2026 judging text supplies the sharpest formulation: *"holding up in rooms the developer never tested."*

**Consensus.** Passthrough MR's value is **not** aesthetic. It is (i) safety and comfort, (ii) letting the physical world become the content substrate, (iii) reducing the setup cost of a virtual environment. Room understanding turns the player's space into **level geometry** — the most durable "couldn't exist elsewhere" argument available. Anchors (spatial, persistent) convert a one-session demo into an ongoing relationship with the room — the technical basis for "reason to come back."

**Contested / open.** Passthrough *quality* perception (lighting mismatch, colour temperature, occlusion artefacts) has little published design guidance. Camera-access privacy is a live policy question. And there is almost no literature on **passthrough experiences on narrow-FoV glasses**, where the passthrough window *is* the product.

**Competition translation.** Very gameable judging instruction: *"the player's real room should meaningfully change the experience… rather than a scene that plays identically with passthrough off."* That is a **test a judge can run in 20 seconds** — toggle passthrough, see if anything changes. Design for that test explicitly. Note the second half of Technical Implementation: *"holding up in rooms the developer never tested"* — Meta knows developers hardcode one tidy living room. Defend with degenerate cases: a cluttered desk, a bed instead of a table, a room with no horizontal surface, a 1 m² room, a wall at 0.5 m.

**Concrete design rules.** (1) **The room must be load-bearing** — at minimum one detected surface hosts content, and removing passthrough breaks the premise. (2) **Handle the failure case first** — build a fallback that works with *zero* detected planes (a virtual surrogate table), then escalate. (3) **Use MRUK for plane classification and anchoring**, and say so in your description. (4) **Re-anchor on room change** — if the user moves rooms (travel mode!), content must re-seat; this is where entries silently break. (5) **Guard physical safety** — never spawn content where a wall or person is; respect the boundary; show it in the video. (6) **If using PCA + AI**, remember P5: *perception → model → real-world consequence*. Not a caption. A consequence.

**Failure modes.** Passthrough as wallpaper; hardcoded table height; a room-with-no-table crash; content that doesn't re-anchor; occlusion artefacts making virtual objects float at the wrong depth.

---

## D8 — Onboarding, learnability, and the first five minutes

**Key literature.** Nielsen (1993/1994), *Usability Engineering* / *Response time limits* — the 0.1 s / 1 s / 10 s perception thresholds [canonical]. Krug (2014), *Don't Make Me Think*, 3rd ed. [canonical]. The games-onboarding literature (tutorial design, "show don't tell," embedded teaching) is practitioner-dominated; the academically relevant anchor is the **directive versus emergent tutorial** distinction, with the robust finding that *contextualised, just-in-time* instruction outperforms upfront instruction blocks. In XR the strongest evidence base is Meta's **hands limitations/mitigations** and **hands best practices** documentation, plus the "adapting from controllers" guidance [verified], which encodes a decade of observational learning about what users fail to discover. Pfeuffer (2024) on gaze+pinch design issues [verified] is also relevant: gaze+pinch's learnability advantage is that *look at a thing, pinch at it* is inferable without explanation.

**Consensus.** (1) **Upfront tutorials are the worst possible onboarding for a 10-minute experience** — they consume the very minutes being judged. (2) **Just-in-time, in-context instruction dominates** — teach grab when the user first needs to grab. (3) **Affordance first, text last** — if you need a wall of text, the interaction design failed. (4) **The first 30–60 s determine persistence**; if the user doesn't cause something interesting in the first minute, they remove the headset. (5) **Time-to-first-success is the metric**, not time-to-comprehension.

**Contested / open.** Whether hands-first interfaces need *any* explicit teaching — the "natural interaction" hypothesis is only partly supported; naturalness helps, but discovery of *which* gesture maps to *which* action does not come free. Whether gaze UI needs a calibration ritual. Whether onboarding should be skippable by default.

**Competition translation.** "Best First Five Minutes" is an entire $25,000 award dedicated to this literature, and "clear onboarding" is written into Experience Design. Meta's video guidance — *"open with real gameplay that makes your hands-first design clear in the first 30 seconds"* — is the same principle applied to the judging artefact. **Your video's first 30 s and your app's first 60 s should be storyboarded by the same person, together, against the same plan.**

**Concrete design rules.** (1) **Cold start to first meaningful action in <15 s** — no logo animation, no menu, no "calibrate your room," no press-anything. (2) **Teach by consequence** — the first object touched should do something delightful and thereby teach the grammar. (3) **Ghost hands / mirror cue in the first 10 s** so users see their hands are tracked; the single most effective hands-first onboarding device and costs a day. (4) **One gesture at a time, introduced at the moment of need**, with a persistent legend available on a wrist menu (opt-in). (5) **Zero-text tutorial** — if a sentence is necessary, deliver it as a spatial cue or a sound. (6) **Design a recovery beat** — users will lose tracking, drop objects, or grab the wrong thing in the first minute; make recovery graceful and self-evident. (7) **Test with someone who has never seen it, without coaching** — Meta's own advice, and the highest-value hour you will spend. Watch where they *stop moving*; that is where the design failed.

**Failure modes.** A 90-second tutorial before any play; instructions in a floating panel; hand tracking not visibly active at start (user thinks it's broken); a menu system requiring an untaught gesture.

---

## D9 — Retention, motivation, and progression design

**Key literature.** Eyal (2014), *Hooked: How to Build Habit-Forming Products* [canonical, practitioner] — trigger → action → variable reward → investment. Fogg (2009), *A behavior model for persuasive design* — B = MAT [canonical]. Ryan & Deci (2000), *Self-determination theory*, American Psychologist 55(1):68–78 [canonical] — autonomy, competence, relatedness. Rigby & Ryan (2011), *Glued to Games* / the PENS model [canonical]. Csikszentmihalyi (1990), *Flow* [canonical]. Deci, Koestner & Ryan (1999), *A meta-analytic review of experiments examining the effects of extrinsic rewards on intrinsic motivation*, Psychological Bulletin 125(6):627–668 [canonical] — the crucial caveat.

**Consensus.** Durable engagement comes from **autonomy, competence, relatedness**, not from points. **Variable reward** is the engine of return visits; predictable reward is not. **Investment** — the user putting something in that persists — is what converts a session into a habit, and it is the mechanism most VR apps omit entirely. **Extrinsic rewards can crowd out intrinsic motivation** (Deci et al.): leaderboards and currency can *reduce* the desire to return for the activity itself — a genuine tension with naive gamification. Flow requires a **difficulty curve**, which requires designed escalation.

**Contested / open.** Whether Hook-model design is ethical or manipulative — live debate, and relevant because Meta's criterion is "habit-forming purpose." And how retention works in *spatial* products where the user must physically don a device: friction is much higher than a phone, so the trigger must come from something other than a notification. **This is the central unsolved problem of VR retention and almost no one in the field has good answers.** Notably, VR Glasses is a media-consumption device *because* media has a built-in trigger (a queue, an episode).

**Competition translation.** "Best Reason to Come Back" is explicitly *"built for longevity, not a one-time demo"* and rewards *"visible growth between sessions."* Innovation & Creativity mentions "repeat usage" as the purpose of leveraging device capabilities. Note how hard this is to fake: judges evaluate *design intent*, so the artefact you need is not a live service but a **visible progression structure** — something that persists, something that grows, something left undone.

**Concrete design rules.** (1) **Ship a persistence artefact** — a saved creation, a growing garden, a streak, a collection, a room that remembers; it must survive app restart. Cheapest possible "reason to come back," and most entries will not have it. (2) **End the first session with an unfinished thing** — a visible next step, an open loop. (3) **Make the return visit cheaper than the first** — instant resume directly into the persistent artefact, no menus, no re-tutorial. (4) **Prefer autonomy-supportive progression** (unlock expressive options) over extrinsic currency; Deci et al. is the citation that makes this arguable in a submission. (5) **Design a visible growth signal** the user can see across sessions — size, complexity, completeness, streak. (6) **Keep the difficulty curve legible in the first 10 minutes** — three beats: trivially easy success → small failure → earned success. (7) **Do not build a leaderboard** unless multiplayer is core; it reads as filler and invites the extrinsic-motivation critique.

**Failure modes.** A "session recap" screen instead of progression; progression that resets; a demo loop with no persistent state; cosmetic-only unlocks with no autonomy value.

---

## D10 — Social presence, co-presence, and multiplayer

**Key literature.** Oh, Bailenson & Welch (2018), *A systematic review of social presence: Definition, antecedents, and implications*, Frontiers in Robotics and AI 5:114 [canonical] — the definitive synthesis. Biocca, Harms & Burgoon (2003), *Toward a more robust theory and measure of social presence* [canonical]. Freeman & Maloney (2021), *Body, avatar, and me: The presentation and perception of self in social virtual reality*, PACM HCI (CSCW) [canonical]. Slater & Sanchez-Vives (2016) on shared presence [canonical]. Meta's 2025 Social Game category validates the commercial shape [verified].

**Consensus.** Social presence has three antecedents: **co-presence** (being together), **behavioural engagement** (mutual responsiveness), and **co-construction** (doing something together). **Responsiveness matters more than avatar fidelity** — a crude avatar that responds quickly produces more social presence than a photoreal one that lags. **Gaze and gesture read as social signals**; avatar eye contact and hand gestures are the primary social cues in VR, which makes hands-first design *structurally* advantaged for social presence. **Asymmetric roles** (one person in VR, one on phone/PC) dramatically lower the adoption barrier and are under-explored.

**Contested / open.** Whether hand tracking alone (without head/eye tracking on the observer side) is sufficient. Whether spatial audio's contribution is additive or multiplicative. Content moderation. And almost nothing on **social experiences where every participant is hand-only** — new with VR Glasses.

**Competition translation.** "Best Social & Multiplayer Experience" names four shapes: *co-watching, cooperative play, social spaces, asymmetric multiplayer.* Three observations: **co-watching** is the natural fit for Entertainment *and* for VR Glasses (a media device), and is easiest to demo in 3 minutes — two avatars, one screen, shared reactions; **asymmetric multiplayer** is the highest-leverage option for a solo developer, since you need only one hand-tracking build while the second player is on a phone or web page, which also bypasses the chicken-and-egg problem of needing two headsets; and **hands-first + social** is rare in the corpus — most social VR is controller-based, so being the hands-first social entry is a free differentiator.

**Concrete design rules.** (1) **Prioritise responsiveness over fidelity**; sub-100 ms gesture propagation beats a high-poly model. (2) **Render hands and gaze on the remote avatar** — these are the social signals; a floating head is not enough. (3) **Design one shared object** — co-presence without a shared manipulable is a chat room. (4) **Prefer asymmetric if solo** — one headset plus one browser client. (5) **Handle the empty-room problem** — judges test alone; if the experience is broken solo you lose. (6) **If you cannot do realtime, do asynchronous social** (ghost trails, time-shifted reactions, shared artifacts) — under-used and cheap.

**Failure modes.** Multiplayer requiring two headsets to demo; avatars without hands; a shared space with nothing to do together; babysitting a networking stack instead of designing an experience.

---

## D11 — Agentic AI and voice-first interaction

**Key literature.** Park et al. (2023), *Generative Agents: Interactive Simulacra of Human Behavior*, UIST '23 [canonical] — LLM-driven agents with memory, reflection, planning. Yao et al. (2022), *ReAct: Synergizing reasoning and acting in language models* [canonical] — the tool-use pattern underlying "agentic" behaviour. Wei et al. (2022), *Chain-of-thought prompting* [canonical]. Cassell, Sullivan, Prevost & Churchill (2000), *Embodied Conversational Agents* [canonical] — the pre-LLM foundation for gesture/gaze/voice-integrated agents, and still the best design reference for *embodiment*. Pearl (2016), *Designing Voice User Interfaces* [canonical]. Nielsen (1993) response-time limits [canonical]. Meta's 2025 corpus: **all three PCA-with-AI winners were perception-grounded, none conversational** [verified]. Meta's `metavr` CLI exposes an **MCP server** for agentic device control [verified], and Meta programmed an "Agentic Workflows" session [verified].

**Consensus.** Embodied conversational agents increase engagement and social presence when **nonverbal behaviour is synchronised** with speech (Cassell) — voice alone, disembodied, loses most of the benefit. **Latency destroys the illusion**: Nielsen's 1 s threshold is roughly the conversational tolerance, and 3–5 s of LLM thinking plus TTS is a perceptual cliff; the mitigation is *staged response* — immediate acknowledgement, then content. **Agents need memory to feel real** — Park et al.'s contribution was memory + reflection, not dialogue quality. **Tool use is what makes an agent agentic** — perceiving, deciding, and *acting on* the environment rather than producing text.

**Contested / open.** Whether LLM latency can ever be hidden well enough for realtime spatial interaction. Whether users trust spatial agents with actions in their physical room. Safety and failure modes when an agent misidentifies a real object. Evaluation methods for agent quality in XR are essentially absent.

**Competition translation.** "Best Agentic Interaction" defines the space precisely: *"spatial agents, voice-first interaction, procedural content, context-aware assistance."* Three of the four avoid the latency trap:

| Shape | Latency risk | Demo-ability | 2025 precedent |
|---|---|---|---|
| Spatial agent (avatar that acts in the room) | High | High | Tarot VR (runner-up) |
| Voice-first interaction | High | Medium | — |
| **Procedural content** | **Low** (async, cached) | **High** | — |
| **Context-aware assistance** | **Medium** | **High** | PCA+AI cluster |

The 2025 evidence and the latency literature converge: **put the AI where it changes the world, not where it talks.** Perception-grounded and procedurally generative approaches both survive latency. A conversational companion in realtime is the riskiest possible choice.

**Concrete design rules.** (1) **Bind AI to a spatial consequence** — the output must change something in 3D: place an object, alter geometry, generate a level, annotate the room, drive a character. (2) **Hide latency architecturally** — pre-generate on load, stream with a visible in-world "thinking" affordance, acknowledge in <200 ms with a non-verbal beat. (3) **Give the agent persistent memory** — cheap (a JSON blob), enormously legible as "agentic." (4) **Synchronise nonverbal behaviour** — an agent that gestures, looks at you, and moves in space reads as present; one emitting audio from a fixed point reads as a speaker. (5) **Constrain the action space** — agentic action in a physical room needs a hard allow-list; a hallucinated action that spawns geometry inside a wall is both a bug and a safety issue. (6) **Always ship a non-AI path** — voice in a shared/quiet room, accent variance, and model outages are all real; doubles as accessibility. (7) **Respect the anti-wrapper rule** — "ChatGPT in VR" fails the take-it-away test by construction; ask whether the experience would still be a design if the model were replaced by a scripted system.

**Failure modes.** A text box in space; 4-second waits with no feedback; an agent with no memory and no action; voice-only with no visual presence; a demo depending on live inference over a judge's Wi-Fi.

---

## D12 — Accessibility in XR

**Key literature.** Mott, Cutrell, Gonzalez-Franco, Holz, Ofek, Stoakley & Ringel Morris (2019), *Accessible by Design: An Opportunity for Virtual Reality*, IEEE ISMAR Workshop on Mixed Reality and Accessibility [verified] — five considerations: **content, interaction, device, inclusive representation, application** accessibility; argues that post-hoc patches are inferior to ability-based design from the start. The same group's **SeeingVR** (14 tools for low vision in VR) [verified]. AbleGamers' *Includification* [canonical]. W3C **XR Accessibility User Requirements (XR AUR)** and WCAG 2.2 as adapted for XR [canonical]. Meta's accessibility guidance for Quest [verified]. For the eyes-only/limited-mobility case, D4 is directly load-bearing.

**Consensus.** Design for **ability, not disability** — the mismatch between user ability and interface demand is the design target. The **five axes** give a complete audit checklist, unusually convenient for a short build. Concretely effective, low-cost measures: **subtitles/captions, high-contrast modes, colour-blind-safe palettes, adjustable text size, seated-height calibration, one-handed modes, voice alternatives, eyes-only navigation, adjustable audio, reduced-motion options.** Accessibility features designed for disabled users **benefit everyone** (the curb-cut effect) — the strongest argument for prioritising them in a competition.

**Contested / open.** Whether hand tracking is itself an accessibility *barrier* (excluding users with motor impairments) or an accessibility *win* (removing the need to grip a controller). Both are true; the resolution is to ship alternatives. Almost no literature on accessibility of **gaze+pinch** specifically, and none on narrow-FoV glasses accessibility.

**Competition translation.** "Best Accessibility Forward Experience" names its own success criteria: *"voice alternatives for hand interactions, eyes-only navigation for limited mobility, high-contrast modes, inclusive by default."* A **$25,000 award with an unusually explicit rubric and very low implementation cost** — arguably the best effort-to-probability ratio in the competition alongside "Best First Five Minutes." It is judged *across* all tracks, so the field is not deep.

**Concrete design rules.** (1) **Ship an input-modality matrix** — hands+touch, hands+voice, gaze+pinch, eyes-only (+dwell), single-hand only; even partially implemented, the *architecture* is what's scored. (2) **Subtitles on by default**, with size and contrast controls, plus captions for non-speech audio cues — this is what "inclusive by default" actually means. (3) **High-contrast / large-text mode** as a single top-level toggle, not buried. (4) **Seated-height and reach calibration** — one slider that re-solves all interaction distances; also serves the airplane-seat test. (5) **Reduced-motion setting** disabling ambient animation and optional camera motion. (6) **Haptic/audio confirmation** for every action, so visual confirmation is never the sole channel. (7) **Document the audit** as a short table in your submission — judges reward evidence of systematic thinking, and a table is legible.

**Failure modes.** An "accessibility mode" that is a font-size slider; voice commands requiring precise phrasing with no fallback; treating accessibility as an afterthought bullet in the description.

---

## D13 — Media, spatial storytelling, and spatial audio

**Key literature.** Ryan (2001), *Narrative as Virtual Reality* [canonical] — immersion vs interactivity as competing axes. Mateas & Stern (2003), *Facade: An experiment in building a fully-realized interactive drama* [canonical]. Mateer (2017), *Directing for cinematic virtual reality: how the traditional film director's craft applies to immersive environments and notions of presence*, Journal of Media Practice [canonical] — the practical craft literature on guiding attention in 360° without cutting. Begault (1994), *3-D Sound for Virtual Reality and Multimedia* [canonical] — the standard spatial-audio reference. Vosmeer & Schouten (2014), *Interactive cinema: engagement with a self-deforming story* [canonical]. Väljamäe's work on auditory-induced presence [canonical]. Meta's spatial-audio and media-app design documentation [verified].

**Consensus.** **The director's problem in VR is attention, not framing.** You cannot control where the viewer looks, so you control what *matters* through audio, light, motion, and gaze cues — attention direction replaces editing. **Spatial audio is one of the most reliable presence multipliers** and the cheapest way to direct attention off-screen (doubly important on narrow-FoV devices). **Interaction and narrative are in tension** — every interaction hands narrative control to the user (Ryan); the resolution is either *authored agency* (constrained choices that all work) or *environmental storytelling* (narrative in the space, agency in movement). **Cutting is disorienting in 360°**; dissolve, fade, and spatial transition are preferred.

**Contested / open.** Whether "cinematic VR" is a genre or an oxymoron. Optimal guided-attention techniques. Whether 360° video's passivity is a dead end — relevant, since the Entertainment brief includes both passive (spatial cinema) and interactive content.

**Competition translation.** The Entertainment brief's own wording — *"media companion apps (e.g., commentary tracks, popup production notes, social comments)"* — is the most under-exploited phrase in the ruleset. It reframes the problem from "make a VR film" (expensive, crowded) to "make the layer on top of media" (cheap, empty). A companion app is inherently hands-first (you point at things in a scene and comment), inherently seated, inherently a "reason to come back" (episodic content), demo-able in 90 seconds, naturally social (co-commentary), and structurally *better* on glasses than a headset (you are watching, not playing). Note also P7: music visualization is the winningest theme in the corpus — latency-tolerant, hands-tolerant, seated-native, infinitely replayable.

**Concrete design rules.** (1) **Direct attention with audio first, light second, motion third** — never a UI arrow. (2) **Design a "watched" and an "explored" mode** — let users opt into agency rather than forcing it. (3) **Spatialise everything, including UI sounds**; non-diegetic stereo audio breaks presence. (4) **Build to a 10-minute three-act structure**, not a 60-minute runtime — judges have 10 minutes and so do real users on a bus. (5) **If your content is third-party media, apply the take-it-away test aggressively** — your layer must be the product, not the video. (6) **Prefer generated visuals over licensed footage** — avoids rights risk and demonstrates craft.

**Failure modes.** A 360° video player with a pause button; no attention direction (user misses the content); stereo audio in a spatial scene; a companion layer thin enough that the underlying media is the experience.

---

## D14 — Productivity, text legibility, and virtual workspaces

**Key literature.** This is the **thinnest literature of any domain in this review**, and that is the most important finding. What exists: ISO 9241-3xx on display ergonomics and text legibility [canonical]; the HMD text-legibility literature (angular pixel density, contrast, reading speed and comprehension in HMDs vs desktop, generally finding a penalty that narrows with resolution); the multi-monitor/virtual-display literature; and a large **industry** evidence base (Workrooms, Immersed, enterprise deployments) that is largely unpublished and commercial. Meta's design documentation on text, window layout, and the **Travel Mode** context [verified] is currently the most actionable material. The 2025 corpus supplies the strongest single data point: **OnTheGoVR won on Travel Mode** — a *context* [verified].

**Consensus (such as it is).** **Text legibility in HMDs is angular-resolution-limited** — pixels-per-degree, not resolution; and more important than either: **contrast, font weight, and viewing-distance stability.** **Long-form reading in VR underperforms flat screens** on speed and comfort; short-form glanceable information is where VR competes. **The unique affordances are spatial** — unlimited virtual displays, infinite canvas, 3D arrangement, diegetic information placement; competing with a laptop on 2D-app parity is a losing strategy. **Context determines value** — Travel Mode, commute, wind-down; the value comes from the situation, not the app. **Postural load is the hidden cost** — arms up, neck down; a productivity app must be usable with hands *resting*.

**Contested / open.** Everything about long-session comfort, text at scale, keyboard input without a physical keyboard, and whether VR productivity produces *better outcomes* rather than merely more screens. **No credible published study establishes a productivity gain for VR multi-monitor work.** That is a genuine research gap and, for a competitor, an opportunity: you cannot be beaten on an evidence base that does not exist, so *demonstrated workflow* in your video is the whole argument.

**Competition translation.** Two structural facts. (1) **The Productivity winner prize is $70,000, not $100,000** — same effort, less money, unless the field is thinner, which is genuinely plausible because productivity is harder to demo, harder to make hands-first, and attracts fewer hobbyist entrants (Part IV models this). (2) **The brief has been rewritten to favour a *moment*, not a *tool***: *"designed around specific daily moments… habits tied to a recurring context (morning routine, commute, wind-down)."* That is a deliberate escape hatch from competing with Microsoft Word. "A morning briefing in your kitchen" is a better competition entry than "a VR IDE."

**Concrete design rules.** (1) **Design for one recurring moment**, named explicitly in your tagline — "Your 7 a.m. briefing, on your wall," not "a spatial workspace." (2) **Hands-rested interaction** — everything reachable from a seated, arms-down posture with a wrist menu; a literal application of the airplane-seat test, and where productivity entries fail. (3) **Maximise PPD; minimise text volume** — hierarchy, icons, glanceable chunks; put long text where the user chooses to lean in, off the critical path. (4) **Exploit spatiality in a way 2D cannot** — persistent spatial layout (my calendar lives *there*, always), information attached to real surfaces, glanceable peripheral status; this is your "couldn't exist elsewhere" argument. (5) **Persist layout across sessions** — a workspace you must rebuild is not a workspace; anchored remembered layout is the retention mechanism and the technical flex in one. (6) **Ship a keyboard story** — voice, gesture-dictation, spatial keyboard, or physical keyboard pairing; silence here is a credibility gap. (7) **Show the workflow end-to-end in the video** — task start to task complete in 90 s; since no literature will persuade a judge on your behalf, the demo is the evidence.

**Failure modes.** A flat 2D app floating in space (fails the take-it-away test and the Boldest Concept test simultaneously); dense text panels; arms-up typing; a workspace that doesn't persist; generic "productivity" with no moment.

---

## D15 — Performance engineering for mobile XR

**Key literature.** Meta, *VRC Quest performance criteria* and *Performance and hardware* documentation — **60 fps minimum**, as cited in the 2026 judging criteria [verified]. Meta documentation on **App Spacewarp, foveated rendering, fixed foveated rendering, render scale, FrameSync, CPU/GPU levels, boosting, memory/RAM, thermal management, quality fallbacks, battery saver, and compatibility mode** [verified]. LaValle (2023), *Virtual Reality*, 2nd ed. [canonical] — motion-to-photon latency fundamentals. Abrash (2012), *Latency — the sine qua non of AR and VR* [canonical]. The asynchronous timewarp / reprojection literature, and Meta's *Iteration Speed Best Practices* (Unreal) and **Immersive Debugger** (Unity) [verified].

**Consensus.** **Frame rate and latency dominate perceived quality** far more than polygon count or shader complexity; a 60 fps build with modest art beats a 45 fps build with great art, in every study. **Thermal throttling is the hidden failure** — a build holding 72 fps for 5 minutes and dropping to 50 at 12 will fail a longer session. **Spacewarp/reprojection is a legitimate tool, not a cheat**, but it degrades with fast motion — and hands moving quickly through the frame are exactly the case where reprojection artefacts appear. **Fixed foveated rendering is basically free** on these devices and undervalued.

**Contested / open.** Whether 72 or 90 fps is the right target on Quest 3/3S for hands-first content (fast hand motion benefits disproportionately from higher rates). The interaction between eye-tracked foveated rendering and gaze UI. Real thermal envelopes for long sessions.

**Competition translation.** Technical Implementation is 25% of the score and names a number: *"must be performant on Meta Quest hardware (min 60 fps)."* **A number in a rubric is a gift** — it is binary-legible; a judge can enable a frame-rate overlay or simply *feel* it. And judges test on **Quest 3 and 3S**; the 3S is the weaker device and is a listed judging device, so testing only on a 3 is a self-inflicted wound.

**Concrete design rules.** (1) **Profile on a Quest 3S, cold, from fresh boot, with a frame-rate overlay, and record it** — put the overlay in your video for three seconds; evidence beats assertion. (2) **Set a per-frame ms budget and enforce it daily.** (3) **Budget for hands** — hand tracking costs CPU and rendered hands cost draw calls; a hand-tracking app has less headroom than a controller app on the same scene. (4) **Use fixed foveated rendering and render-scale tuning before optimising art**, and verify AppSW behaviour under fast hand motion (if hands smear, adjust). (5) **Watch textures and draw calls before triangles** — mobile XR is bandwidth- and submission-bound, not vertex-bound. (6) **Set quality fallbacks** so a slow device degrades gracefully instead of stuttering. (7) **Measure thermal over 15 minutes**, not 90 seconds. (8) **Keep a safe-mode build** — if you must choose between one more feature and 12 more fps on day 11, choose fps.

**Failure modes.** 45 fps at the finale; a hitch on first interaction (shader compilation — warm your shaders); hands that smear under reprojection; great on a 3, dead on a 3S; memory-pressure crash after 8 minutes.

---

## D16 — The meta-literature: how innovation is assessed and how hackathons are judged

**Key literature.** Briscoe & Mulligan (2014), *Digital innovation: The hackathon phenomenon* [canonical] — hackathons as rapid innovation instruments and their characteristic failure (demos optimised for the room). Creativity assessment: Amabile's **Consensual Assessment Technique** [canonical] — domain experts' independent subjective judgements converge into a reliable measure of creativity, and *originality is judged relative to the field, not absolutely*. Runco & Jaeger (2012) on the standard definition of creativity as **originality + effectiveness** [canonical]. The design-jury literature on how **presentation quality contaminates judgements of substance** (halo effects). Nielsen's heuristics [canonical] as usability signalling.

**Consensus.**
- Expert panels using CAT-style consensus produce *reliable* but *field-relative* originality judgements. **You are not judged against an abstract standard; you are judged against the other submissions the panel has seen.** Tactical implication: the value of a novel concept depends on the density of similar concepts. Music visualization won four awards in 2025 — so a music-visualization entry in 2026 faces a *higher* originality bar, because the judges have seen it win.
- **Originality = novelty × effectiveness.** A strange idea that doesn't work scores as low as a boring idea that works. The 2026 language mirrors this exactly: *"Rewards the idea that couldn't exist anywhere else… Judged on originality and on whether the experience would fall apart on any other platform."* That is a **portability test** — a formalisation of novelty-by-necessity.
- **Presentation quality contaminates substance judgements.** Uncomfortable but the most actionable finding: a strong build presented badly loses to a good build presented brilliantly. Hence Polish & Presentation being a full 25% — it is not decoration, it is a scored criterion equal in weight to technical implementation.
- **AI-assisted judging changes legibility requirements.** Where an LLM may assist, structured, factual, keyword-rich descriptions naming specific platform features are safer than lyrical prose.

**Contested / open.** Whether hackathon judging systematically over-rewards novelty and under-rewards maintainability (it does — and that's fine here, because maintainability isn't scored). Whether CAT generalises to very large pools where no judge sees everything.

**Competition translation.** Four tactics. (1) **Saturate your concept against the 2025 corpus and against VR Glasses launch coverage** — if your idea is adjacent to a 2025 winner, either out-execute decisively or move. (2) **Make portability your novelty argument** — don't say "innovative"; say *why it cannot exist on a phone*: the room is the level, your hands are the instrument, the depth is the information. Boldest Original Concept is literally a portability test; answer it in one sentence. (3) **Treat polish as 25%, not as the last 10%** — art-direction consistency, sound design, UI state design, and a clean video are a quarter of your score; budget them as such. (4) **Write the description for both a human expert and an LLM-assist filter** — name MRUK, ISDK v207, gaze interaction, FoV profiles, scene understanding, 60 fps, Quest 3S. Concrete nouns score.

**Failure modes.** A concept adjacent to a 2025 winner with no differentiator; "innovative" as an adjective with no argument; brilliant build, grainy video; description written as a poem.

---

## 3.17 Cross-cutting synthesis: five tensions the literature reveals

The domains above are not independent. Five tensions cut across them, and resolving each explicitly is what distinguishes a designed experience from an assembled one.

**Tension 1 — Naturalness vs precision.** Hand tracking is preferred and more accurate for pinch but slower for grasp (D3); gaze is fast for selection, poor for confirmation (D4). *Resolution:* multimodal cascade — gaze to select, pinch to confirm, and never ask hands to do what eyes do better.

**Tension 2 — Presence vs comfort.** Wide FoV increases presence; narrow FoV increases comfort and compatibility (D5, D6). *Resolution:* spend the FoV budget deliberately — wide ambient, narrow critical — and treat the glasses' 70° as the design target and the Quest's 110° as headroom.

**Tension 3 — Accessibility vs depth.** Fewer, simpler inputs (one-handed, eyes-only) reduce depth and richness (D12, D3). *Resolution:* ability-based design — build the simplest complete path first, then layer optional depth on top; the accessible version becomes the tutorial, the expert version becomes the retention.

**Tension 4 — Retention vs scope.** Progressive systems, persistence, and content breadth create return visits (D9) and all three are expensive in 12 days. *Resolution:* one persistent artefact with visible growth beats three half-built systems; persistence is disproportionately cheap and disproportionately legible.

**Tension 5 — AI capability vs latency vs authenticity.** Agentic AI is a named award (D11), but realtime latency breaks conversational illusion and thin wrappers are explicitly penalised. *Resolution:* perception-grounded or procedural AI, applied asynchronously, with a scripted fallback — AI that changes the world, not AI that talks about it.

---

# PART IV — SYNTHESIS: THE RUBRIC MATHEMATICS

## 4.1 A scoring model you can self-audit against

Because the four criteria are equal and the ruleset permits multi-round judging, model your entry as a weighted sum and be brutally honest per criterion. Below is a calibrated interpretation — the *anchor descriptions* are extrapolated from the published criterion language plus the 2025 winner corpus.

| Score | Innovation & Creativity | Experience Design | Technical Implementation | Polish & Presentation |
|---|---|---|---|---|
| **9–10** | Concept portability is self-evident; the room or the hands are the mechanic | Zero-friction entry, teaches itself, habit-forming, passthrough is load-bearing | Gaze+pinch, MRUK, anchored, 72 fps on a 3S, survives untested rooms | Cohesive art direction, designed sound, a video that could ship as a trailer |
| **7–8** | Clear differentiator, one genuinely novel interaction | Clear onboarding, satisfying loop, some persistence | Hands-first complete, 60–72 fps, minor rough edges | Consistent art, good audio, competent video |
| **5–6** | Competent take on a known genre | Works, but the tutorial carries the load | Hands-first but raycast-only; 60 fps with dips | Mixed asset styles, stereo audio, adequate video |
| **3–4** | Store-adjacent clone | Needs explanation; no persistence | Hand tracking breaks outside ideal conditions; <60 fps | Inconsistent; video is a screen recording |
| **1–2** | Could exist on a phone | Broken entry path | Controller actually required somewhere; crashes | Unusable |

**Diagnostic:** score yourself in each column, then score your *three nearest competitors* (from the public project gallery) in each column. If you are not ≥8 in at least two columns, you do not win a track prize — and you should be aiming your remaining days at the columns where the gap is smallest, not at the one you enjoy most.

## 4.2 Marginal-value analysis: where the next 24 hours buys the most points

Ranked by *points gained per day of work*, given a functional hands-first build in place:

| Rank | Investment | Days | Criteria moved | Why it's efficient |
|---|---|---|---|---|
| 1 | **Rewrite the description + tagline + video script** | 1 | Polish (25%), and it is the only artefact *every* judge sees | Judges score the submission as much as the build; you are currently losing points you could collect in an afternoon |
| 2 | **Re-record the video properly** (1920×1080, hook in 3 s, real gameplay first 30 s, frame-rate overlay 3 s) | 1 | Polish + Innovation (framing) + Technical (evidence) | 3 minutes of video carries your entire 25% presentation score |
| 3 | **Gaze+pinch on the primary UI, validated on device** | 1–2 | Technical (named in rubric), Experience Design, Innovation | The single most rubric-aligned technical upgrade available |
| 4 | **FoV pass: 70° test + reflow + screenshot pair** | 1 | Technical (named), Experience Design | Cheapest named criterion in the document |
| 5 | **Onboarding rebuild (ghost hands, zero text, <15 s to first action)** | 1–2 | Experience Design (25%), Best First Five Minutes | Directly targets an entire award plus a quarter of the score |
| 6 | **Persistence artefact + visible progression** | 1–2 | Experience Design, Best Reason to Come Back | Turns a demo into a product in judges' eyes |
| 7 | **Quest 3S performance pass** | 1–2 | Technical (binary, min 60 fps) | Binary criteria fail loudly |
| 8 | **Accessibility panel (5 toggles) + audit table** | 0.5 | Best Accessibility Forward | Highest award-probability per hour in the whole build |
| 9 | **Art consistency pass** (one palette, unified materials) | 1 | Polish | Fixes the "yard sale" failure Meta's own webinar addresses |
| 10 | New feature | 2+ | Innovation (maybe) | Lowest efficiency; highest risk. Do this last or not at all |

Meta's own Week-6 guidance says the same thing in fewer words: *"Finish the core experience… Test with fresh eyes… Give your assets a consistent style."*

## 4.3 Track and division expected value

Because submission counts per track/division are unpublished, the model below is **directional, not precise** — but the *structure* of the conclusion is robust to reasonable changes in the inputs.

**Inputs.** 5,371 registered participants. In 2025, ~2,900 registered produced 650+ submissions (≈22% conversion). Applying 20–25% conversion to 2026 implies **≈1,100–1,350 submissions**. Assume 5–10% fail Stage One, leaving ~1,000–1,250 scored entries competing for 12 track prizes and 8 specials.

**Structural asymmetries, ranked by confidence:**

1. **Gaming will be the most crowded track.** Highest hobbyist appeal, largest existing skill base, most existing assets. Expect 45–55% of submissions.
2. **Entertainment will be mid-crowded (~30–35%).** The brief is broad, and media app builders are numerous — but the "media companion" sub-genre is nearly empty.
3. **Productivity will be the thinnest (~12–20%).** Harder to demo, harder to make hands-first, no genre precedent, and the brief demands a specific *moment* rather than a tool. Prize is 30% lower ($70k) but the field may be 2–3× thinner → **net EV is plausibly better than Gaming, and definitely better than Entertainment.** This is the most under-considered strategic move available.
4. **The New division is where small teams win; the Adapted division is where scope wins.** 2025 evidence: the four track winners were a 2-person team, a 3-person team, a small studio, and a small studio — but two of them won *on updates*. If you have a mature app and can ship a genuine modality upgrade, Adapted is the higher-probability route to $100k because you skip the "is this even finished?" objection. If you're starting now, New is the only honest option and the pool is comparably immature — which is an advantage, not a disadvantage.
5. **Special awards have a better hit rate than track prizes.** 8 specials vs 12 track prizes, and specials are judged across all tracks — but note the **double-dip trap** (§4.5): a special is a *floor*, not a bonus.

**Sensitivity conclusion.** For a solo or 2–3 person team starting on 7 November with no existing app:

> **Target Productivity (New) or Entertainment (New, in the media-companion sub-genre). Build explicit hooks for Best Accessibility Forward + Best First Five Minutes + Best Reason to Come Back.** Those three specials are the ones a *small, finished, thoughtful* build can win against a *large, ambitious* one — and they are exactly the three the literature says are cheap.

**Avoid:** Gaming (New) unless you have a genuinely novel hand-mechanic (P3), because you will be competing against 500+ game entries with existing assets and years of practice.

## 4.4 Award-targeting matrix — design choices that fire multiple awards at once

The efficient strategy is **not** to chase one award. It is to pick design decisions that are simultaneously load-bearing for several.

| Design decision | Track prize | Social | Agentic | Come back | First 5 min | Accessibility | Boldest |
|---|---|---|---|---|---|---|---|
| Room is the level (MRUK, real surfaces) | ●●● | ● | | ● | ● | | ●●● |
| Gaze-select / pinch-confirm | ●●● | | | | ●● | ●●● | ●● |
| Single-primitive interaction (pinch only) | ●●● | | | | ●●● | ●● | ●● |
| Persistent artefact that grows | ●●● | ● | | ●●● | | | ● |
| One-handed-only capable | ●● | | | | ●● | ●●● | |
| Zero-text onboarding + ghost hands | ●●● | | | | ●●● | ●● | |
| Asymmetric multiplayer (headset + browser) | ●● | ●●● | | ●● | | | ●● |
| Perception-grounded AI (camera → model → spatial consequence) | ●●● | | ●●● | ●● | ● | | ●● |
| Procedural content generation | ●● | | ●●● | ●●● | | | ●●● |
| Spatial audio as attention direction | ●●● | ● | | | ●● | ●●● | ●● |
| Seated-height / reach calibration slider | ●● | | | | ●● | ●●● | |
| Travel-mode / context-aware design | ●● | | | ●●● | | ●● | ●● |

Read this table as a portfolio: **room-as-level + gaze/pinch + persistence + zero-text onboarding + one-handed** fires on all four criteria and four of eight specials. That combination is achievable in twelve days. Adding a full multiplayer stack is not.

## 4.5 The double-dip trap — the most commonly misread rule

> *"Limit one (1) prize per Entry… If in consideration for multiple prizes, you will be awarded the highest prize value."*

Consequences entrants routinely get wrong:

1. **Winning a special does not add to a track prize.** Best New Gaming ($100k) + Best Social ($25k) = **$100k**, not $125k.
2. **Therefore specials are optimised as *floors*.** If you think you have a ~20% shot at a track prize, the specials are your insurance. If you have a high shot at a track prize, deliberate special-chasing is mostly wasted effort — build for the track and let specials fall out.
3. **The counter-case is real, though:** an entry that is *not* top-2 in its track but wins Best First Five Minutes gets $25k. That is a genuinely valuable outcome, and it is achievable by a build that is small, finished, and exquisitely onboarded but not especially ambitious. **Know which game you are playing.** A 12-day solo build almost certainly cannot win Best New Gaming against a 6-person studio — but it can plausibly win Best First Five Minutes.
4. **Judge's Choice (2 × $20k) is the true wildcard** — discretionary, explicitly for entries that "don't fit neatly into a single track or category." If your concept is genuinely strange, this is the lane; but note the rules' own warning that a *wrapper* is "highly discouraged," so strange ≠ unoriginal.

---

# PART V — THE 12-DAY EXECUTION PLAN (IST)

**Assumptions:** entry period closes 19 Nov 01:30 IST. Internal freeze: **17 Nov 23:59 IST**. Nov 7 is a Saturday. Adjust to your situation; the *sequence* is the point, not the dates.

| Day | Date | Milestone | Definition of done |
|---|---|---|---|
| 1 | **Sat 7 Nov** | **Decide and de-risk.** Confirm Start membership badge. Create the `Competition` release channel. Pick track + division from §4.3. Write tagline (≤140 ch) and the one-sentence portability claim. Set up the gaze rig + FoV profile in XR Simulator | A blank scene builds, installs, launches on the target device *and* on Quest 3S; invite URL works from a second account |
| 2 | **Sun 8 Nov** | **Core loop skeleton.** One interaction primitive, one scene, ghost hands on launch | A stranger can start the app and cause the primary action within 15 s |
| 3 | **Mon 9 Nov** | **Content pass 1.** The actual experience: level/scene/data, sound bed, spatial audio | The experience is *complete but ugly* end-to-end. Record a rough 60 s capture |
| 4 | **Tue 10 Nov** | **Gaze + FoV integration.** Gaze-select/pinch-confirm on all primary UI; device profile drives layout | Same build works in the 70° profile and the Quest profile. Screenshot pair captured |
| 5 | **Wed 11 Nov** | **Persistence + progression.** Saved state, visible growth, instant resume | Kill and relaunch: state survives; return visit is faster than the first |
| 6 | **Thu 12 Nov** | **Accessibility panel** + one-handed capability audit | Five toggles shipping; full playthrough with one hand only |
| 7 | **Fri 13 Nov** | **External playtest.** Three people who have never seen it, no coaching | Written list of *every* point where a tester stopped moving. Fix the top three |
| 8 | **Sat 14 Nov** | **Performance day.** Quest 3S, cold boot, thermal over 15 min, fixed foveated rendering, render scale | Stable ≥72 fps on 3/3S with a frame-rate overlay recording |
| 9 | **Sun 15 Nov** | **Art + audio unification.** One palette, unified materials, designed sound pass. No new features | Screenshots look like one product |
| 10 | **Mon 16 Nov** | **Video shoot + edit.** Hook in 3 s, real hands-first gameplay in the first 30 s, FoV pair, overlay 3 s, 1920×1080, <3:00 | Final video uploaded public on YouTube; link tested in incognito |
| 11 | **Tue 17 Nov** | **Submission assembly + freeze.** All form fields, description ≤500 words, invite URL re-tested, **submit early** | Entry submitted. APK frozen. Nothing touched after this |
| 12 | **Wed 18 Nov** | **Buffer only.** Re-verify the invite URL from a clean account; do not edit the build | A quiet day. This is the plan working |

**Three rules for the plan.**

1. **Submit on day 11, not day 12.** The deadline lands at 01:30 IST; the effective working day ends the 18th. Submitting 30 hours early costs nothing and protects against Devpost load, Zoom-fatigue errors, and YouTube processing.
2. **No new features after day 6.** Everything after is polish, performance, and packaging — the three things worth 75% of the score.
3. **The playtest on day 7 is non-negotiable.** It is Meta's own single strongest recommendation and the only intervention on this list that cannot be done by an AI agent.

---

# PART VI — SUBMISSION-PACKAGE CRAFT

Polish & Presentation is 25% of the score, and it is scored largely on artefacts — the video, the images, the text. This is the section most worth reading twice.

## 6.1 The video: a 150-second storyboard

Constraints: **<3:00**, public on YouTube/Vimeo, device or XR Simulator footage, **1920×1080**, judges not required to watch past 3:00, hook in the first 3 seconds, hands-first design clear in the first 30 seconds, **no AI-generated video carrying the pitch**.

| Time | Beat | Content | Why (literature) |
|---|---|---|---|
| 0:00–0:03 | **Hook** | The single most arresting thing your hands do, at full frame. No logo, no title card | XRCC packaging: hook in 3 s; D8 first-30-seconds |
| 0:03–0:12 | **The premise in one line** | Card or voiceover: "Your kitchen table is the board." Then the hands doing it | D16: portability claim stated, not implied |
| 0:12–0:30 | **Hands-first proof** | Uninterrupted real-time interaction, no cuts, hands visibly tracked, gaze reticle visible once | Judging: *"show real gameplay that honestly represents the experience"* |
| 0:30–1:00 | **The core loop** | One complete cycle: start → action → reward → next. Show the difficulty step | D9: difficulty curve; D8: teach-by-consequence |
| 1:00–1:20 | **The room is load-bearing** | **Passthrough on/off comparison** — the same scene, visibly different | Judging: *"not a scene that plays the same with passthrough off"* — this shot alone answers the criterion |
| 1:20–1:35 | **FoV + devices** | Split screen: Quest FoV vs VR Glasses FoV (~70°), UI reflowing | D5: demonstrates the named criterion |
| 1:35–1:50 | **The reason to come back** | The persistent artefact, the growth between sessions | D9 / Best Reason to Come Back |
| 1:50–2:05 | **Accessibility** | One-handed flow, eyes-only dwell selection, high-contrast toggle — each for ~3 s | D12 |
| 2:05–2:20 | **Technical evidence** | 3 s of frame-rate overlay on a Quest 3S at ≥72; MRUK room mesh visualisation; gaze interaction firing | D15: evidence beats assertion |
| 2:20–2:35 | **Craft montage** | Art direction, sound design, UI states | Polish |
| 2:35–2:50 | **Close** | The one sentence: what this is, why it could only exist here, when it ships | D16: originality = novelty × effectiveness |

**Five video rules.**
1. **No half-real footage.** Every frame should be a build you can install. Judges are explicitly told to punish misrepresentation.
2. **No music-only audio.** Show the *designed* sound — spatial cues, interaction feedback. Sound design is a named Polish sub-criterion and the most commonly omitted.
3. **No AI-generated footage.** Explicit prohibition-adjacent guidance.
4. **Legible without sound.** Caption the key claims; judges may watch muted.
5. **Thumbnail matters.** Curiosity, not a logo. It is the first thing seen in the gallery.

## 6.2 The description: a 500-word template

Suggested structure, ~480 words. Every bracketed item is a place to name a specific platform tool — this is what makes it legible to both expert and LLM-assisted review.

> **[Project name]** — [tagline, ≤140 chars]
>
> **The idea.** [1–2 sentences. State the portability claim: *why this cannot exist on a phone, a console, or a desktop.* Name the mechanism — the room, the hands, the eyes, the depth.]
>
> **How hands-first works.** [3–4 sentences. Name the primary primitive. State explicitly: *the entire experience is completable without ever pairing a controller.* Describe gaze-select / pinch-confirm if used; name Interaction SDK v207. Mention one-handed capability.]
>
> **How the room becomes the level.** [2–3 sentences. Name MRUK; name which surfaces host content; state what breaks if passthrough is off; state the failure-case fallback for rooms with no suitable surface. This sentence is directly responsive to the stated criterion *"holding up in rooms the developer never tested."*]
>
> **FoV-aware by design.** [1–2 sentences. Critical UI within a ~55–60° cone; secondary panels reflow, never crop; validated in the VR Glasses profile in XR Simulator at ~70° × 66° against Quest 3's ~110° × 96°.]
>
> **Performance.** [1 sentence + number. *Sustains ≥72 fps on Quest 3S across a 15-minute thermal run, verified with [tool].*]
>
> **Why you'll come back.** [2 sentences. The persistent artefact, the visible growth, the untaken next step.]
>
> **Accessibility.** [2 sentences. The input-modality matrix; one-handed; eyes-only via dwell; high contrast; captions on by default.]
>
> **How I built it.** [2–3 sentences. Engine, SDK versions, Meta VR CLI/agentic workflow if genuinely used, iteration approach.]
>
> **What's next.** [1–2 sentences + **target launch date** — a required field and a credibility signal.]
>
> **Team.** [Names, emails, roles.]

**Three writing rules.**
1. **Name the tools.** MRUK, ISDK v207, gaze interaction, XR Simulator VR Glasses profile, fixed foveated rendering, AppSW, spatial anchors, Immersive Debugger. The rubric's own vocabulary.
2. **Use numbers.** 60 fps, 70°, 55°, 2 ft, 15 s, 5 toggles. Numbers are unfalsifiable-by-aesthetics and read as engineering.
3. **Do not use the word "immersive" more than once.** It is the most-used and least-informative word in XR submissions.

## 6.3 Images

Five screenshots is the standard allowance. Choose them to mirror the video beats, not to show off the prettiest frame:

1. Hero shot — the core interaction in progress, hands visible
2. Passthrough on/off pair (counts as two, or composite)
3. FoV comparison pair — Quest vs glasses profile
4. Progression / persistence artefact
5. One accessibility mode active

## 6.4 Build-access verification protocol

Do this on **17 November**, not the 18th, and again on the 18th. In order:

1. Open the invite URL in a **private browser window, signed out** of your developer account.
2. Redeem on a **Quest 3S** (not your dev device), signed into a **non-developer** account.
3. Confirm the app appears with the name you expect — not a placeholder, not "Untitled."
4. Install, launch **cold**, and complete the experience **with controllers removed from the room**.
5. Note the install size and time. A 2 GB APK on a judge's throttled connection is a real risk.
6. Confirm the build does not require a Meta-funded entitlement, a dev-mode flag, or a beta sign-up that has expired.
7. Screenshot the success and keep it. If a judge reports a problem, you want evidence.

---

# PART VII — RESEARCH GAPS: WHERE ORIGINALITY IS CHEAP

The literature review's most useful product is its holes. A gap is a place where (a) no competitor can claim authority, (b) judges cannot compare you to precedent, and (c) your own demonstrated solution *becomes* the evidence. These are ranked by the ratio of opportunity to risk.

**G1 — Gaze+hand interaction on narrow-FoV (≈70°) devices.** Essentially unpublished. Any rigorous documentation of a gaze+pinch UI reflowed for a 70° field is, right now, a first-of-its-kind contribution. **Low risk, high reward.** This is the single most defensible claim available in 2026.

**G2 — Retention mechanics in spatial computing.** The phone-habit literature does not transfer: the friction of donning a device breaks the notification-trigger loop entirely. Nobody has a validated model of VR return visits. Demonstrating a *persistent, growing artefact* is therefore original by default. **Medium risk, high reward.**

**G3 — Long-duration comfort in productivity VR.** No credible study establishes benefit for multi-monitor VR work, and postural load is unmeasured at scale. A 20-minute session-comfort protocol with measured break points is both a design contribution and a marketing asset. **Low risk, medium reward.**

**G4 — Accessibility of gaze-based interaction.** Hand tracking's accessibility status is contested (barrier vs enabler); gaze+pinch's is unstudied. An input-modality matrix — hands+touch / hands+voice / gaze+pinch / eyes-only / one-hand — is a small, publishable artefact. **Very low risk, high reward.**

**G5 — Asymmetric hands-first multiplayer.** Almost all social VR research assumes controllers and symmetric hardware. One-headset-plus-browser, with hand and gaze social signalling, is genuinely novel. **High implementation risk, high reward.**

**G6 — Latency-tolerant agentic spatial AI.** The unsolved problem is hiding LLM latency in realtime spatial action. A staged-response architecture (immediate non-verbal acknowledgement → streamed spatial consequence) is a design pattern nobody has formally described for XR. **Medium risk, high reward.**

**G7 — Passthrough on narrow-FoV glasses.** Where the passthrough window *is* the product, all existing MR design guidance (assumed wide FoV, assumed headset) fails. Nobody has written the design rules. **Low risk, medium reward** (because you can reason about it without a device, using the simulator).

**G8 — Evaluating agent quality in XR.** No instrument exists. If you invent even a crude behavioural evaluation — does the agent's action match intent; does the user retry; does trust survive a wrong action — you have contributed a method.

**G9 — Music/instrument interaction as a hands-first paradigm.** Despite winning four awards in 2025, the HCI literature on spatial instrument design is thin. The 2025 corpus is the state of the art. **Medium risk, medium reward** — but note the field-relative originality penalty (D16): the judges have seen this win, so you need a *new* musical interaction, not a good one.

**G10 — Onboarding metrics for hands-first interfaces.** "Time-to-first-success" is asserted everywhere and measured nowhere. Reporting a simple, honest measurement (n testers, median seconds, where they stalled) is a genuine contribution and a strong submission artifact.

**How to use this section.** Pick exactly one gap as your *claim*, and make it the spine of your 500-word description and your video's technical beat. Do not claim three. The judges are scoring "creative ambition" and "technical expertise" — one clearly-articulated gap answered rigorously scores higher than three gestured at.

---

# PART VIII — BIBLIOGRAPHY

Confidence markers: **[verified]** = checked against a primary source during preparation. **[canonical]** = a standard, well-established work cited from knowledge; the substance is reliable, but verify volume/page details before formal quotation.

## Primary sources — competition and platform

1. Meta VR Start Developer Competition 2026, Devpost challenge 31024 — Overview, Rules, Judging Criteria & Process, FAQs, Special Awards, Resources, Updates, Webinars & Office Hours, Schedule. **[verified]**
2. Meta, *Meet the Meta Horizon Start Developer Competition Winners* (23 Dec 2025). **[verified]**
3. Meta, *VR is Expanding. Start Building for Meta VR Glasses Today* (24 Sep 2026). **[verified]**
4. Meta, *Get started with Meta VR Glasses* (updated 13 Oct 2026). **[verified]**
5. Meta, *Hands: Limitations & Mitigations* (updated 14 Sep 2026). **[verified]**
6. Meta, *AI tooling overview*, *Work with devices and apps* (Meta VR CLI), *Get started with Android apps*. **[verified]**
7. Meta, *Meta Horizon Start developer program* / *Developer programs and support*. **[verified]**
8. UploadVR, *Meta Launches $1.5 Million Competition For New Quest Apps & Significant Updates* (5 Nov 2025). **[verified]**
9. UploadVR, *Meta VR Glasses Officially Announced, Shipping Spring 2027 For $1300*. **[verified]**
10. Meta Newsroom, *Introducing Meta VR Glasses: A Cinema, Courtside Seat, and Workspace in Just 100 Grams*. **[verified]**

## Presence, embodiment, social presence

11. Slater, M. & Sanchez-Vives, M. V. (2016). Enhancing our lives with immersive virtual reality. *Frontiers in Robotics and AI*, 3:74. **[canonical]**
12. Slater, M. (2009). Place illusion and plausibility… *Phil. Trans. R. Soc. B*, 364(1535), 3549–3557. **[canonical]**
13. Witmer, B. G. & Singer, M. J. (1998). Measuring presence in virtual environments. *Presence*, 7(3), 225–240. **[canonical]**
14. Cummings, J. J. & Bailenson, J. N. (2016). How immersive is enough? *Media Psychology*, 19(2), 272–309. **[canonical]**
15. Biocca, F. (1997). The cyborg's dilemma. *JCMC*, 3(2). **[canonical]**
16. Kilteni, K., Groten, R. & Slater, M. (2012). The sense of embodiment in virtual reality. *Presence*, 21(4), 373–387. **[canonical]**
17. Gonzalez-Franco, M. & Peck, T. C. (2018). Avatar embodiment: Towards a standardized questionnaire. *Frontiers in Robotics and AI*, 5:74. **[canonical]**
18. Yee, N. & Bailenson, J. (2007). The Proteus effect. *Human Communication Research*, 33(3), 271–290. **[canonical]**
19. Oh, C. S., Bailenson, J. N. & Welch, G. F. (2018). A systematic review of social presence. *Frontiers in Robotics and AI*, 5:114. **[canonical]**
20. Biocca, F., Harms, C. & Burgoon, J. K. (2003). Toward a more robust theory and measure of social presence. *Presence*, 12(5), 456–480. **[canonical]**
21. Freeman, G. & Maloney, D. (2021). Body, avatar, and me. *PACM HCI (CSCW)*. **[canonical]**
22. Jerald, J. (2015). *The VR Book: Human-Centered Design for Virtual Reality*. ACM / Morgan & Claypool. **[canonical]**

## 3D interaction, selection, gaze

23. Bowman, D. A. & Hodges, L. F. (1997). An evaluation of techniques for grabbing and manipulating remote objects… *I3D '97*. **[canonical]**
24. Poupyrev, I., Billinghurst, M., Weghorst, S. & Ichikawa, T. (1996). The go-go interaction technique. *UIST '96*. **[canonical]**
25. Mine, M. (1995). *Virtual Environment Interaction Techniques*. UNC TR95-018. **[canonical]**
26. Pierce, J. S. et al. (1997). Image plane interaction techniques in 3D immersive environments. *I3D '97*. **[canonical]**
27. Bowman, D. A., Kruijff, E., LaViola, J. J. & Poupyrev, I. (2004). *3D User Interfaces: Theory and Practice*. **[canonical]**
28. Hinckley, K. et al. (1994). Passive real-world interface props for neurosurgical visualization. *CHI '94*. **[canonical]**
29. MacKenzie, I. S. (1992). Fitts' law as a research and design tool in HCI. *HCI*, 7(1), 91–139. **[canonical]**
30. Jacob, R. J. K. (1991). The use of eye movements in HCI techniques. *ACM ToIS*, 9(2), 152–169. **[canonical]**
31. Zhai, S., Morimoto, C. & Ihde, S. (1999). Manual and gaze input cascaded (MAGIC) pointing. *CHI '99*. **[canonical]**
32. Pfeuffer, K., Mayer, B., Mardanbegi, D. & Gellersen, H. (2017). Gaze + Pinch interaction in virtual reality. *SUI '17*, 99–108. **[verified]**
33. Wagner, U., Lystbæk, M. N., Manakhov, P., Grønbæk, J. E., Pfeuffer, K. & Gellersen, H. (2023). A Fitts' Law study of gaze-hand alignment for selection in 3D user interfaces. *CHI '23*. **[verified]**
34. Gabel, J., Schmidt, S., Pfeuffer, K. & Steinicke, F. (2024). Guiding handrays in virtual reality. **[verified]**
35. *Pinch, Click, or Dwell: Comparing Different Selection Techniques for Eye-Gaze-Based Pointing in Virtual Reality*. *ETRA '21*. **[verified]**
36. Pfeuffer, K. (2024). *Design Principles and Issues for Gaze and Pinch Interaction*. arXiv:2401.10948. **[verified]**
37. *Reaching further in VR: a comparative study with a novel velocity-based technique*. *Virtual Reality* (Nov 2025). **[verified]**
38. *Comparing Haptic Feedback Across Hand Tracking and Controllers in VR Object Interaction Tasks* (2026). **[verified]**
39. *Virtual reality rehabilitation using hand tracking: interaction system design and usability tests*. *Virtual Reality* (Dec 2025). **[verified]** — a good primary source on occlusion-related interaction-rule design.
40. Sharp, T. et al. (2015). Accurate, robust, and flexible real-time hand tracking. *CHI '15*. **[canonical]**
41. Taylor, J. et al. (2016). Efficient and precise interactive hand tracking… *SIGGRAPH*. **[canonical]**
42. Müller, F. et al. (2017). Real-time hand tracking under occlusion from an egocentric RGB-D sensor. *ICCV*. **[canonical]**

## Field of view

43. Arthur, K. W. (2000). *Effects of Field of View on Performance with Head-Mounted Displays*. PhD dissertation, UNC Chapel Hill. **[verified]**
44. Meta, *FOV simulation* and *VR Glasses device profile* documentation. **[verified]**
45. *(2026) Effects of head-mounted display position on procedural task performance* — small FoV reduced efficiency of instruction perception despite preserved task performance. *Archives of Design Research*. **[verified]**

## Comfort and cybersickness

46. Kennedy, R. S., Lane, N. E., Berbaum, K. S. & Lilienthal, M. G. (1993). Simulator Sickness Questionnaire. *Int. J. Aviation Psychology*, 3(3), 203–220. **[canonical]**
47. Rebenitsch, L. & Owen, C. (2016). Review on cybersickness in applications and visual displays. *Virtual Reality*, 20, 101–125. **[canonical]**
48. Fernandes, A. S. & Feiner, S. K. (2016). Combating VR sickness through subtle dynamic field-of-view modification. *IEEE 3DUI*. **[canonical]**
49. Stanney, K. M., Kennedy, R. S. et al. on cybersickness ≠ simulator sickness. **[canonical]**

## Passthrough, scene understanding, anchoring

50. Sra, M., Garrido-Jurado, S., Schmandt, C. & Maes, P. (2016). Procedurally generated virtual reality from 3D reconstructed physical space. *VRST '16*, 191–200. **[verified]**
51. Simeone, A. L., Velloso, E. & Gellersen, H. (2015). Substitutional reality. *CHI '15*, 3307–3316. **[canonical]**
52. Lindlbauer, D. & Wilson, A. D. (2018). Remixed reality. *CHI '18*. **[canonical]**
53. Meta, *MR Utility Kit (MRUK)* documentation. **[verified]**

## Motivation, retention, learning

54. Ryan, R. M. & Deci, E. L. (2000). Self-determination theory and the facilitation of intrinsic motivation. *American Psychologist*, 55(1), 68–78. **[canonical]**
55. Deci, E. L., Koestner, R. & Ryan, R. M. (1999). A meta-analytic review of experiments examining the effects of extrinsic rewards on intrinsic motivation. *Psychological Bulletin*, 125(6), 627–668. **[canonical]**
56. Rigby, S. & Ryan, R. M. (2011). *Glued to Games*. **[canonical]**
57. Csikszentmihalyi, M. (1990). *Flow*. **[canonical]**
58. Eyal, N. (2014). *Hooked: How to Build Habit-Forming Products*. **[canonical]**
59. Fogg, B. J. (2009). A behavior model for persuasive design. *Persuasive '09*. **[canonical]**
60. Krug, S. (2014). *Don't Make Me Think*, 3rd ed. **[canonical]**
61. Nielsen, J. (1993). *Usability Engineering*, and *Response Time Limits*. **[canonical]**

## Agents and voice

62. Park, J. S. et al. (2023). Generative agents: Interactive simulacra of human behavior. *UIST '23*. **[canonical]**
63. Yao, S. et al. (2022). *ReAct: Synergizing reasoning and acting in language models*. **[canonical]**
64. Wei, J. et al. (2022). *Chain-of-thought prompting elicits reasoning in LLMs*. **[canonical]**
65. Cassell, J., Sullivan, J., Prevost, S. & Churchill, E. (2000). *Embodied Conversational Agents*. **[canonical]**
66. Pearl, C. (2016). *Designing Voice User Interfaces*. **[canonical]**

## Accessibility

67. Mott, M., Cutrell, E., Gonzalez-Franco, M., Holz, C., Ofek, E., Stoakley, R. & Ringel Morris, M. (2019). Accessible by design: An opportunity for virtual reality. *IEEE ISMAR Workshop on Mixed Reality and Accessibility*. **[verified]**
68. Microsoft Research, *SeeingVR: A Set of Tools to Make Virtual Reality More Accessible to People with Low Vision*. **[verified]**
69. W3C, *XR Accessibility User Requirements (XR AUR)*; WCAG 2.2. **[canonical]**
70. AbleGamers, *Includification*. **[canonical]**

## Media, narrative, audio

71. Ryan, M.-L. (2001). *Narrative as Virtual Reality*. **[canonical]**
72. Mateas, M. & Stern, A. (2003). Facade: An experiment in building a fully-realized interactive drama. **[canonical]**
73. Mateer, J. (2017). Directing for cinematic virtual reality. *Journal of Media Practice*. **[canonical]**
74. Begault, D. R. (1994). *3-D Sound for Virtual Reality and Multimedia*. **[canonical]**
75. Vosmeer, M. & Schouten, B. (2014). Interactive cinema. **[canonical]**

## Performance and latency

76. LaValle, S. M. (2023). *Virtual Reality*, 2nd ed. **[canonical]**
77. Abrash, M. (2012). *Latency — the sine qua non of AR and VR*. **[canonical]**
78. Meta, *VRC Quest performance criteria*, and *Performance and hardware* (App Spacewarp, foveated rendering, render scale, FrameSync, thermal management, quality fallbacks). **[verified]**

## Innovation and judging

79. Amabile, T. M. The Consensual Assessment Technique. **[canonical]**
80. Runco, M. A. & Jaeger, G. J. (2012). The standard definition of creativity. *Creativity Research Journal*, 24(1), 92–96. **[canonical]**
81. Briscoe, G. & Mulligan, C. (2014). *Digital Innovation: The Hackathon Phenomenon*. **[canonical]**

---

# APPENDIX A — PRE-SUBMISSION CHECKLIST

**Eligibility**
- [ ] Forum flair shows "Start Member" or "Start Partner"
- [ ] Developer Access enabled on the submitting Meta account
- [ ] Not resident/domiciled in Brazil, Quebec, or a sanctioned region
- [ ] 18+ and age of majority
- [ ] Project has **not** received direct Meta funding of any kind
- [ ] Only **one** Entry from this individual (solo *or* team representative, not both)
- [ ] All team members' names, emails, roles listed

**Build & access**
- [ ] New release channel named exactly `Competition`
- [ ] APK uploaded and installs cleanly on **Quest 3S**
- [ ] Invite URL verified from a **signed-out, non-developer** account
- [ ] Full playthrough with **controllers physically absent**
- [ ] Cold start works; no shader-compile hitch on first interaction
- [ ] ≥60 fps (target 72) verified with an overlay, 15-minute thermal run
- [ ] App remains installable and unmodified through 11 Dec
- [ ] No unreleased Meta SDK or feature used

**Hands-first**
- [ ] Primary interaction is a single learnable primitive
- [ ] One-handed completion possible end-to-end
- [ ] All interaction within a ~2 ft / 0.25–0.6 m shell
- [ ] No bilateral hand overlap required
- [ ] Tracked/lost state visibly indicated

**Gaze & FoV**
- [ ] Gaze-select / pinch-confirm on primary UI (or a documented decision not to)
- [ ] Hand-raycast fallback verified on a device without eye tracking
- [ ] 70° × 66° VR Glasses profile tested; UI reflows, does not crop
- [ ] Screenshot pair captured (Quest FoV vs glasses FoV)

**Experience**
- [ ] First meaningful action in <15 s, zero text
- [ ] Ghost hands or equivalent tracking cue at launch
- [ ] Persistent state survives app restart
- [ ] A visible "next thing" at the end of session one
- [ ] Passthrough (if used) is load-bearing — test with it off
- [ ] Works with zero detected planes / hostile room
- [ ] Tested by ≥3 people who had never seen it, uncoached

**Accessibility**
- [ ] One-handed mode; eyes-only/dwell mode; high contrast; large text; captions on by default
- [ ] Seated-height / reach calibration
- [ ] Audit table written for the description

**Content & policy**
- [ ] English, or English subtitles
- [ ] No identifiable person other than entrants
- [ ] No brand names, logos, slogans, recognisable commercial products
- [ ] No implication of Meta association
- [ ] Age rating compliant (10+/13+/18+)
- [ ] All third-party assets licensed; no infringement
- [ ] Original — or public domain / Meta-provided

**Submission artefacts**
- [ ] Tagline ≤140 characters
- [ ] Track selected; division selected
- [ ] Description ≤500 words, names MRUK / ISDK v207 / gaze / FoV / fps
- [ ] Adapted summary + changelog written (if applicable)
- [ ] Target launch date supplied
- [ ] Video <3:00, public, 1920×1080, device or simulator footage, hook in 3 s
- [ ] Video link verified in incognito
- [ ] 5 screenshots uploaded
- [ ] **Entry submitted at least 24 hours before the deadline**

---

# APPENDIX B — JUDGE-SIMULATION SCRIPT

Run this yourself, on a Quest 3S, out loud, before you submit. It takes 25 minutes and it is the closest you will get to the real panel.

**Stage One (pass/fail) — 5 minutes**
1. Open the Devpost submission. Is there a video and a build link? (If no → fail.)
2. Click the build link from a signed-out browser. Does it redeem? (If no → fail.)
3. Install. Launch. Does it reach an interactive state without a controller? (If no → fail.)
4. Is it obviously a hands-first VR/MR experience? (If no → fail.)
5. Is it plausibly in the track and division claimed? (If no → fail.)

**Stage Two (four criteria) — 20 minutes**
6. **0:00–1:00 Innovation.** Say out loud what is new and why it couldn't exist on a phone. If you hesitate, the judge will too. *Score 1–10.*
7. **1:00–6:00 Experience Design.** Don't get any help. When did you first *do* something meaningful? Did you ever stop moving? Did you understand the goal without being told? *Score 1–10.*
8. **6:00–12:00 Technical.** Toggle passthrough off; does the experience change? Look at your hands; do they lag? Walk into a different room (or reset the boundary); does anything break? Check the frame rate. *Score 1–10.*
9. **12:00–18:00 Polish.** Cover the video. Does the app feel like one product — one palette, one sound world, one UI logic? Play the video muted; is it legible? *Score 1–10.*
10. **18:00–20:00 Return.** Kill the app. Relaunch. Is there something waiting for you? *Would you open it tomorrow?* If the honest answer is no, that is the highest-value finding of the whole exercise.

**Then write one sentence per criterion, as a judge would, and re-read it.** If your own sentence is unflattering, you have ~24 hours of work identified.

---

# APPENDIX C — KEY NUMBERS, ONE PAGE

| Fact | Value |
|---|---|
| Prize pool | $1,000,000 (New $405k + Adapted $405k + specials $150k + Judge's Choice $40k) |
| Number of prizes | 20 (12 track + 8 special) |
| Track prize tiers | Gaming/Entertainment $100k + $50k · Productivity $70k + $35k |
| Special awards | $25k each × 6 · Judge's Choice $20k × 2 |
| One prize per entry | Yes — highest value only |
| Entry deadline | 18 Nov 2026, 12:00 PM PT = **19 Nov 2026, 01:30 IST** |
| Winners announced | 11 Dec 2026, 1:00 PM EST = 11 Dec, 11:30 PM IST |
| Entry period opened | 24 Sep 2026 (the day after Meta Connect) |
| Registered participants | 5,371 |
| 2025 benchmark | ~2,900 devs → 650+ submissions → 32 awards |
| Tracks | Entertainment · Gaming · Productivity |
| Divisions | New Experience · Adapted/Significantly Updated |
| Judging | Stage 1 pass/fail → Stage 2, four criteria at 25% each |
| Minimum frame rate | 60 fps (target 72 on Quest 3/3S) |
| Judging device | Meta Quest 3 / **Quest 3S** |
| Quest 3 FoV | ~110° × 96° |
| Meta VR Glasses FoV | **~70° × 66°** |
| Meta VR Glasses | ~100 g + ~300 g puck · 5K Infinite Display · ~37 PPD · $1,299 · ships spring 2027 · eyes+hands only |
| SDK | Meta XR SDK v81+ (v207) · Unreal 5.8 + SDK v207+ · ISDK v207 for gaze |
| Video | <3:00 · public YouTube/Vimeo · 1920×1080 |
| Description | ≤500 words suggested |
| Tagline | ≤140 characters |
| Design tests | Airplane seat (2 ft) · one bus stop (10 min) · take it away (no wrappers) |
| Excluded regions | Brazil · Quebec · Crimea · Russia · Cuba · Iran · North Korea · Syria · OFAC-designated |
| Support | Start mentors · Monday Unblock · playtests · forum · priority support · software credits |

---

*End of document. Prepared from primary competition documentation, Meta platform documentation, and the published 2025 winner corpus. Where sources conflict — notably the Meta VR Glasses chipset (Snapdragon Reality Elite vs XR2 Gen 3) — the conflict is flagged rather than resolved.*
