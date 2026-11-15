# WHY THIS HACKATHON EXISTS
## A First-Principles Research Dossier on the Meta VR Start Developer Competition 2026

**Prepared:** 7 November 2026
**Purpose:** To establish, from primary financial and platform evidence, *why* Meta runs this competition, *what* problems it is solving, and *why every individual rule exists* — so that any concept we choose later is aimed at the actual target rather than at the visible target.
**Status:** Research phase complete. No concept proposed. The output of this document is a **decision filter**, not an idea.

---

## HOW TO READ THIS DOCUMENT

Every substantive claim is tagged with its epistemic status. This matters more than usual here, because a competition ruleset is a *designed artefact* — it is a set of incentives somebody chose — and it is very easy to mistake inference about intent for fact.

| Tag | Meaning |
|---|---|
| **[DATA]** | Verifiable, sourced to a primary document or a named tracker. Cited. |
| **[INFERENCE]** | My reasoning from the data. Defensible, but it is reasoning, not a quote. |
| **[SPECULATION]** | Plausible, unpriced. Flagged so we don't build on it. |

**Structure**

| Part | Contents |
|---|---|
| **I** | Executive diagnosis — the seven problems, in one page |
| **II** | The question bank — 108 questions, organised into 11 domains, with answer status |
| **III** | The research framework — method, sources, triangulation, confidence rules |
| **IV** | Findings: Meta's actual material position (all the data) |
| **V** | Diagnosis: the seven problems, each with evidence |
| **VI** | The decoded rulebook — every rule, its purpose, what it rewards, what it implies |
| **VII** | The ROI model — what $1,000,000 actually buys Meta |
| **VIII** | The decision filter — how the above constrains concept selection |
| **IX** | Open questions — what we still cannot answer, and how we'd answer it |
| **X** | Data appendix and source register |

---

# PART I — EXECUTIVE DIAGNOSIS

**One paragraph.** Meta is spending roughly $19 billion a year on Reality Labs to hold a VR platform whose hardware sales are falling, whose store revenue is flat, and whose only growth line is AI glasses that are not VR. It just told investors it will cut those losses to ~$15 billion in 2027 by putting AI first, and that it will only ship new VR hardware on "major breakthroughs." Its next VR device — the $1,299 Meta VR Glasses, shipping spring 2027 — has **no controllers at all**, which means it launches with whatever hands-first software exists at that moment. Its own content organisation has been shrinking, and 86% of the time people spend in headsets is already spent in third-party apps. So Meta needs a specific, otherwise-unavailable thing: **a body of finished, hands-first, seated, media-and-productivity software built by people it does not have to pay salaries to, ready before spring 2027.** That is what the competition is for.

**The seven problems, ranked by severity.**

| # | Problem | Core evidence | What the competition does about it |
|---|---|---|---|
| **1** | **The install base is shrinking while the tooling must be justified** | Quest sales −42.3% in 2025 (IDC); VR headset shipments −17% Q1'26, −18% Q2'26 (Counterpoint); Quest 3 price raised to $599.99 in April 2026 | Recruits *supply* while demand is weak — the one lever Meta fully controls |
| **2** | **Reality Labs is being starved in favour of AI** | Q3'26 revenue $443M (−6%), operating loss $4.13B; 2026 guided $18–19B loss, 2027 ~$15B; cumulative losses ≈$92B | A $1M competition is a rounding error on $150M of annual dev-programme spend — it buys *direction*, not *funding* |
| **3** | **The next device launches with no controllers and no content** | Meta VR Glasses: ~100g, eyes+hands only, $1,299, spring 2027; v207 SDK shipped 24 Sep 2026 | A hard hands-first requirement across all tracks manufactures a launch catalogue before the hardware exists |
| **4** | **Content is third-party or nothing** | "86% of the effective time people spend in their VR headsets is with third-party apps" — Samantha Ryan, VP Content, Feb 2026 | Meta cannot staff its way out; it must *recruit and steer* third parties |
| **5** | **The audience Meta is betting on is not the audience it has** | Named cohorts: "VR Elites" spending less, "Teens" most active, "Mainstream Adults" expected to arrive via **media consumption first**, who "enjoy hand tracking" and "probably play seated without controllers" | The tracks (Entertainment / Gaming / Productivity), the seated guideline and the hands-first mandate are all literally this paragraph |
| **6** | **Developer trust is damaged and discoverability is broken** | Studio closures incl. Meta's own first-party studios; Horizon Worlds removed from Store shelves after the retention hypothesis "had not been proven out"; a documented "VR visibility crisis" | A competition re-engages a demoralised community with status, cash and attention rather than policy change |
| **7** | **Meta's internal content bets failed and it is retreating** | Horizon Worlds going "almost exclusively mobile" (45M downloads, ~$1.1M lifetime consumer spend); Horizon OS third-party programme paused; Quest for Business closed to new customers | Retreat from first-party means *more* dependence on third-party supply — reinforcing #4 |

**The single most important sentence in the entire evidence base**, for our purposes:

> *"We also continue to see casual adult consumers who gravitate toward entertainment and enjoy streaming their favorite shows, movies, and sports. **This group enjoys hand tracking and is interested in games and experiences that don't require controllers. We expect this category of experiences to expand over time.**"*
> — Samantha Ryan, VP of Content, Reality Labs, *Our Renewed Focus in 2026*, 19 Feb 2026 **[DATA]**

Every hard requirement in the 2026 ruleset — hands-first, seated, ten-minute sessions, no wrappers, Entertainment/Gaming/Productivity — is a restatement of that paragraph. The competition is not primarily a talent search or a technology showcase. **It is an instrument for manufacturing the specific software category that Meta's next device requires and its existing audience is expected to grow into.**

---

# PART II — THE QUESTION BANK

108 questions. This is the deliverable half of "ask as many questions as possible." The bank is designed so that each question is *answerable* and *decision-relevant* — a question that cannot change a decision is a question we should not spend time on.

**Status key:** ✔ answered in this document · ◐ partially answered · ○ open / needs primary access we don't have

---

## Domain A — Corporate and financial purpose (Q1–Q10)

| # | Question | Why it matters | Status |
|---|---|---|---|
| A1 | How much is Reality Labs losing, per quarter and cumulatively? | Establishes how much patience exists for VR | ✔ |
| A2 | Is Reality Labs revenue growing or shrinking, and from which products? | Tells us which device Meta is actually betting on | ✔ |
| A3 | Has Meta given a date for Reality Labs profitability? | Determines the length of the runway our build has to matter within | ✔ |
| A4 | What has Meta said about the cadence of future VR hardware? | Tells us whether Quest 4 is a realistic target for our work | ✔ |
| A5 | How does Reality Labs compare to Meta's total business in size? | Establishes internal political weakness of VR | ✔ |
| A6 | What is Meta's capex picture, and what does it imply about discretionary spend? | Explains why the prize is $1M and not $10M | ✔ |
| A7 | Has Meta cut VR headcount, studios or programmes? | Evidence of organisational retreat from first-party content | ✔ |
| A8 | What does Meta tell investors vs. what it tells developers? | Divergence reveals the internal thesis | ✔ |
| A9 | Is the $1M prize a meaningful investment, or a marketing line item? | Determines whether the competition is *funding* or *signalling* | ✔ |
| A10 | What other developer programmes exist, and how big are they? | Calibrates the competition against Publishing, Ignition, Start, Horizon+ | ✔ |

## Domain B — Market and competitive purpose (Q11–Q22)

| # | Question | Why it matters | Status |
|---|---|---|---|
| B1 | Is the VR headset market growing or contracting, by units? | Demand-side reality check | ✔ |
| B2 | What is Meta's share, and is it rising or falling? | Competitive position | ✔ |
| B3 | Is "XR" growing even if "VR" shrinks — and which segment? | Explains Meta's pivot and the Glasses timing | ✔ |
| B4 | Where did Quest pricing go in 2026, and why? | Margin pressure → fewer subsidies → more dependence on dev supply | ✔ |
| B5 | What did Valve do in September 2026, and does it threaten Meta? | Steam Frame shipped 9 days before Connect | ✔ |
| B6 | What is Apple's position in 2026? | Is spatial computing a real second platform? | ✔ |
| B7 | What is Google/Samsung doing with Android XR? | The platform war Meta may lose by default | ✔ |
| B8 | Did Meta's attempt to license Horizon OS succeed? | Tests whether Meta can be the "Android of XR" | ✔ |
| B9 | What is the installed base, and how much of it is active? | A big installed base that doesn't use it is not an asset | ◐ |
| B10 | Which content categories actually make money on Quest? | Tells us where demand is real | ✔ |
| B11 | Is there evidence that hand-tracked media consumption drives purchases? | Tests the "Mainstream Adults" thesis | ◐ |
| B12 | What is the competitive set for a *productivity* app on Quest? | Determines whether the Productivity track is empty or contested | ◐ |

## Domain C — Platform and store economics (Q23–Q34)

| # | Question | Why it matters | Status |
|---|---|---|---|
| C1 | What is Meta Horizon Store revenue doing? | The platform's health metric | ✔ |
| C2 | What is the revenue split, and has it changed? | Affects whether our build could ever earn | ✔ |
| C3 | How many titles earn meaningfully? | Reveals the power-law we'd be entering | ✔ |
| C4 | What is Meta Horizon+ and what does it pay? | An under-discussed actual revenue channel for a competition entrant | ✔ |
| C5 | How does discoverability work on the Store in 2026? | The #1 complaint from developers | ✔ |
| C6 | What did Meta change about Worlds in the Store, and why? | A live case study in Meta admitting a retention hypothesis failed | ✔ |
| C7 | What are the named audience cohorts, and what does Meta believe about each? | Direct route to the brief | ✔ |
| C8 | What does Meta say about where customers *discover* apps? | Changes what "launch" means | ✔ |
| C9 | What is the role of short-form video in Quest discovery? | Implies our submission video is the product's real acquisition channel | ✔ |
| C10 | What monetisation mechanics exist in 2026 (season passes, bundles)? | Relevant to "Best Reason to Come Back" | ✔ |
| C11 | Is free-to-play dominant in time spent? | Determines business-model realism | ✔ |
| C12 | What proportion of effective VR time is in third-party apps? | The single most important platform statistic | ✔ |

## Domain D — Developer supply and community (Q35–Q46)

| # | Question | Why it matters | Status |
|---|---|---|---|
| D1 | How many developers registered for the 2025 vs 2026 competition? | Measures the competition as a funnel | ✔ |
| D2 | How many actually submitted? | Conversion rate → competition density estimate | ✔ |
| D3 | What is the Start programme, and what does membership give? | The gate we must pass and the support we can use | ✔ |
| D4 | What do developers publicly complain about? | Tells us what judges are primed to see as a fix | ✔ |
| D5 | Are studios leaving VR? | Supply-side attrition | ✔ |
| D6 | What did Meta do to repair developer relations in 2026? | Reveals the intent behind the competition | ✔ |
| D7 | How much did Meta spend on developer programmes in 2025? | Denominator for the $1M | ✔ |
| D8 | Who won in 2025, and were they solo, small teams, or studios? | Calibrates what's achievable in 8 weeks | ✔ |
| D9 | Did 2025 winners convert into Store titles? | Tells us if the competition has a pipeline function | ○ |
| D10 | What is the mentor/mentorship structure in 2026? | A free capability multiplier most entrants ignore | ✔ |
| D11 | Is there a "career ladder" from competition → Start Partner → Publishing? | Determines whether the competition is also talent scouting | ◐ |
| D12 | What is the Ignition programme's outcome? | Evidence Meta funds *new studios* as a strategy | ✔ |

## Domain E — Hardware strategy: VR Glasses (Q47–Q58)

| # | Question | Why it matters | Status |
|---|---|---|---|
| E1 | What exactly are Meta VR Glasses? | Defines the real design target | ✔ |
| E2 | When do they ship, at what price, to which markets? | Sets our deadline context | ✔ |
| E3 | What is the input model? | Explains hands-first as a hard gate | ✔ |
| E4 | What is the field of view? | Explains FoV-aware design as a criterion | ✔ |
| E5 | What are the launch content partnerships? | Reveals Meta's own answer for the device | ✔ |
| E6 | How many games are confirmed at launch? | Tells us how contested the launch slate is | ✔ |
| E7 | What is the compute architecture, and what does it constrain? | Tethered puck → thermal/power budget | ✔ |
| E8 | How does the OS/Store relationship work? | Same store, same SDKs → our build is portable | ✔ |
| E9 | Does the Store favour controller-free titles for these users? | A direct distribution incentive Meta stated | ✔ |
| E10 | Why announce in Sept 2026 and ship in spring 2027? | The gap *is* the competition window rationale | ✔ |
| E11 | What did Meta NOT ship at Connect 2026 on the VR side? | Absence is evidence: no Quest 4, no price cut | ✔ |
| E12 | How does Meta VR Glasses relate to AI glasses (Ray-Ban Display)? | Distinguishes two developer tracks; explains why AI Glasses apps are excluded | ✔ |

## Domain F — Input and interaction strategy (Q59–Q70)

| # | Question | Why it matters | Status |
|---|---|---|---|
| F1 | Why does Meta want hands-first so badly? | The central design mandate | ✔ |
| F2 | What is v207, and what did it add? | The tool the rubric names | ✔ |
| F3 | Why is **gaze** a named criterion? | Because the new device is eyes+hands | ✔ |
| F4 | What are the documented limits of hand tracking? | Defines the design envelope | ✔ |
| F5 | What does Meta recommend for hands design? | The rules encode this | ✔ |
| F6 | Is hand tracking default/encouraged on Quest 3S? | Adoption reality | ◐ |
| F7 | What is the Neural Band / EMG story, and does it matter here? | Possible future input, adjacent to the mandate | ◐ |
| F8 | What is "audio to expressions" and why does face tracking matter? | Constrains avatar/social design on Glasses | ✔ |
| F9 | What does Meta's microgesture support imply? | A second interaction vocabulary is available | ✔ |
| F10 | What is Meta's stated view on controller-free demand? | Direct quote exists | ✔ |
| F11 | What are the FoV numbers for each device? | Drives the two-zone layout requirement | ✔ |
| F12 | What is the relationship between hands-first and accessibility? | Explains the Accessibility special award | ✔ |

## Domain G — Content and audience strategy (Q71–Q80)

| # | Question | Why it matters | Status |
|---|---|---|---|
| G1 | What audience does Meta say it has, and what does it expect to gain? | The brief's origin | ✔ |
| G2 | What does Meta say "Mainstream Adults" will buy first? | Media, then games | ✔ |
| G3 | Why did the "Lifestyle" track become "Productivity"? | A deliberate narrowing with a new brief | ✔ |
| G4 | Why does the Entertainment brief mention "media companion apps"? | A specific, under-served sub-genre | ✔ |
| G5 | Why does Productivity mention "morning routine, commute, wind-down"? | Meta wants *contexts*, not tools | ✔ |
| G6 | What does Meta believe about the teen→adult cohort pipeline? | Determines 2027–2029 content bets | ✔ |
| G7 | Why is there no "Social" track, only a special award? | Hierarchy of priorities | ✔ |
| G8 | Why is AI/agentic only a special award, not a track? | Meta's view of AI's role in VR | ✔ |
| G9 | What content partners did Meta sign for Glasses launch? | Shows where Meta will spend on premium content | ✔ |
| G10 | What is Horizon Create/Studio, and what does AI-assisted creation imply for supply? | Meta is also trying to manufacture supply without developers | ✔ |

## Domain H — Competition design and incentives (Q81–Q92)

| # | Question | Why it matters | Status |
|---|---|---|---|
| H1 | Why 8 weeks? | Long enough to ship, short enough to stay news | ◐ |
| H2 | Why these three tracks? | Maps to the cohort thesis | ✔ |
| H3 | Why a New / Adapted split, and why equal prize money? | Meta values shipped apps as much as new ones | ✔ |
| H4 | Why is Productivity worth 30% less? | Reveals Meta's own confidence in that category | ◐ |
| H5 | Why are there no technical/build-path special awards this year? | Shift from capability to behaviour | ✔ |
| H6 | Why "one prize per entry"? | Prevents a single entrant sweeping and demoralising the field | ◐ |
| H7 | Why is there only one entry per person? | Throughput vs. quality; also fairness | ✔ |
| H8 | Why does Meta exclude its own funded apps? | Conflict of interest + it wants *new* supply | ✔ |
| H9 | Why two-stage judging with a pass/fail screen? | 1,000+ submissions cannot all be deeply reviewed | ✔ |
| H10 | Why is AI permitted to assist judging? | Scale, and consistency | ✔ |
| H11 | Why the explicit "don't lean on AI-generated video"? | Counter-positioning against AI slop; protects signal quality | ◐ |
| H12 | Why does the deadline fall when it does relative to Connect and ship date? | Timing is a design choice | ✔ |

## Domain I — Rules, risk and governance (Q93–Q100)

| # | Question | Why it matters | Status |
|---|---|---|---|
| I1 | Why exclude Brazil, Quebec and sanctioned regions? | Legal/banking feasibility of paying a prize | ✔ |
| I2 | Why require English or subtitles? | Judging panel language | ✔ |
| I3 | Why the "take it away" anti-wrapper rule? | Protects the platform from low-value apps | ✔ |
| I4 | Why judging on Quest 3/3S specifically, not Glasses? | Nobody owns Glasses | ✔ |
| I5 | Why must the build be a release-channel link, not an APK? | Security, versioning, revocation | ✔ |
| I6 | Why freeze the build through judging? | Fair comparison at a single point in time | ✔ |
| I7 | Why grant Meta a perpetual marketing licence while the entrant keeps IP? | Meta needs promotional rights, not ownership | ✔ |
| I8 | Why forbid unreleased SDKs? | Fairness + embargo risk | ✔ |

## Domain J — Our strategic position (Q101–Q108)

|#| Question | Why it matters | Status |
|---|---|---|---|
| J1 | What is Meta *actually* paying for — and are we willing to sell it? | Alignment check before we invest 12 days | ✔ |
| J2 | Which track/division maximises our probability given the above? | Core strategic call | ✔ |
| J3 | What does "aligned with Meta's thesis" look like in a submission? | The filter | ✔ |
| J4 | Where is the competition *not* looking, that its own strategy needs? | The opportunity gap | ✔ |
| J5 | Which special awards are cheapest given the thesis? | Efficiency | ✔ |
| J6 | What would make us look naive to a Meta judge? | Avoidance list | ✔ |
| J7 | What can a small team build in 12 days that a studio cannot? | Our only structural advantage | ✔ |
| J8 | What do we still not know, and what would it change? | Honest limits | ✔ |

---

# PART III — THE RESEARCH FRAMEWORK

## 3.1 Method

The question "why does this competition exist?" cannot be answered by reading the competition. A ruleset is a *conclusion*. To find the premise, you have to look one level up: at the sponsor's financials, its stated platform strategy, its competitive position, and its developer relations record. So the method was a **descending evidence ladder**:

```
Level 5  THE RULE          "Hands-first is required end to end"
   ↑ why?
Level 4  THE COMPETITION   8 weeks, 3 tracks, $1M, 20 prizes
   ↑ why?
Level 3  THE PROGRAM       Start: mentorship, credits, priority support
   ↑ why?
Level 2  THE PLATFORM      Horizon OS, Store, 86% third-party time
   ↑ why?
Level 1  THE CORPORATION   Reality Labs: −$4.13B/quarter, AI-first reallocation
```

Each level explains the one below it and is explained by the one above. A rule that cannot be traced to Level 1 or 2 is probably boilerplate; a rule that traces cleanly to Level 1 is load-bearing and should be treated as a design instruction.

## 3.2 Source hierarchy

| Tier | Source type | Examples | Weight |
|---|---|---|---|
| **T1** | Sponsor primary financial filings & earnings materials | Meta Q3 2026 results, 10-Q, CFO commentary | Highest |
| **T2** | Sponsor primary platform strategy publications | *Our Renewed Focus in 2026* (S. Ryan), *State of VR at GDC 2026*, Connect 2026 keynote material, developer docs | Highest |
| **T3** | The competition's own primary documents | Official Rules, FAQs, Judging, Special Awards, Updates | Highest for rules |
| **T4** | Independent market trackers | IDC, Counterpoint Research, Appfigures, Valve Steam Survey | High, with definitional caveats |
| **T5** | Specialist trade press | UploadVR, Road to VR, Skarred Ghost, VR.org | Medium — corroboration only |
| **T6** | Secondary aggregation / SEO statistics pages | Various "statistics 2026" sites | Low — used only to locate T1–T4, never cited alone |

**A rule I applied throughout:** where T4 trackers disagree (and they do — IDC's "XR" includes glasses, Counterpoint's "VR" does not), I report both and explain the definitional gap rather than picking the flattering number.

## 3.3 The four analytical techniques

**1. Financial triangulation.** Establish what Meta earns, loses and spends, then locate the competition in that frame. This converts "a hackathon" into a budget line with a purpose.

**2. Absence analysis.** What a company *stops* doing is stronger evidence than what it says. Meta paused the Horizon OS third-party programme, closed Quest for Business to new customers, removed Worlds from Store shelves, shuttered first-party studios, and announced no new Quest at Connect 2026. Five retreats in one year is a thesis.

**3. Rule-to-problem mapping.** Every hard requirement in the ruleset is a response to an observed failure. "Honestly represent the experience" implies judges have been deceived. "Holding up in rooms the developer never tested" implies hardcoded rooms. "Don't lean on AI-generated video" implies AI slop in prior submissions. Reading rules as *post-mortems* is the highest-yield technique in this entire document.

**4. Incentive simulation.** For each rule, ask: *what behaviour does this make rational?* If the behaviour it makes rational is the behaviour Meta needs, the rule is instrumental. If not, it is boilerplate or defensive.

## 3.4 Confidence discipline

Because we are reasoning about intent, I separated three kinds of claim:

- **[DATA]** — a number or quote from T1–T4. Verifiable.
- **[INFERENCE]** — a causal claim I draw. Should be falsifiable, and I state what would falsify it where it matters.
- **[SPECULATION]** — plausible but unpriced. Never used as a basis for a recommendation.

**Where the document is weakest.** We have no access to: internal Meta strategy documents; submission counts and category distribution for 2026; judge panel composition or scoring distributions; the actual conversion of 2025 winners into shipped Store titles. Part IX lists these honestly.

---

# PART IV — FINDINGS: META'S ACTUAL MATERIAL POSITION

## 4.1 The financial picture [DATA]

**Meta Platform, Q3 2026 (reported 21 October 2026)**

| Metric | Q3 2026 | Q3 2025 | Change |
|---|---|---|---|
| Total revenue | $64.60B | $51.24B | +26% |
| Advertising revenue | $63.06B | $50.08B | +26% |
| **Reality Labs revenue** | **$443M** | **$470M** | **−6%** |
| **Reality Labs operating loss** | **−$4.13B** | **−$4.43B** | 7% narrower |
| Income from operations | $12.99B | $20.54B | −37% (incl. $9.89B legal charge) |
| Operating margin | 20% | 40% | — |
| Capex (incl. finance leases) | $34.91B | $19.37B | +80% |
| Free cash flow | **−$2.12B** | $10.63B | — |
| Headcount | 66,775 | — | −15% YoY |

**Reality Labs trajectory**

| Metric | Value |
|---|---|
| Nine-month 2026 revenue | $1.28B (+2% YoY) |
| Nine-month 2026 operating loss | $12.78B |
| Full-year 2026 loss guidance | **$18–19B** (2025: $19.19B) |
| Full-year 2027 loss guidance | **~$15B**, "trending towards profitability from there" |
| Cumulative RL losses | ≈$92.2B through 30 June 2026 (per one tally of filings) |
| RL as share of Meta revenue | **<1%** |

**Attribution, verbatim.** CFO Susan Li attributed the decline to "lower Meta Quest sales," partly offset by "continued strong growth in AI glasses revenue." Zuckerberg: "glasses sales continue to grow rapidly."

**The strategic pivot, stated plainly.** Meta now expects Reality Labs to cut annual losses by $3–4B between 2026 and 2027, and says it will "pace its virtual reality spending to put AI first." Zuckerberg's framing: Meta will only ship new VR devices for **"major breakthroughs."** **[DATA]**

> **Why this matters to us [INFERENCE].** There is no Quest 4 on the horizon. The 2026 competition is being run in a year when Meta shipped no new headset, raised Quest prices, and told investors VR is no longer the priority inside Reality Labs. A competition launched *in that exact year*, aimed specifically at an unreleased device, is not a celebration of the platform's momentum. It is **infrastructure-building during a retrenchment.** That tells us what Meta wants: not spectacle, but *shippable supply*.

## 4.2 The demand picture [DATA]

| Measure | Value | Source |
|---|---|---|
| Meta Quest shipments, 2025 | **−42.3% YoY** (vs. total XR +44.4%) | IDC |
| Quest units shipped, first three quarters 2025 | ~1.7M (−16% YoY) | IDC |
| VR headset shipments, Q1 2026 | **−17% YoY**, −39% QoQ | Counterpoint |
| VR headset shipments, Q2 2026 | **−18% YoY**, −16% QoQ | Counterpoint |
| VR headset shipments, H1 2025 | −14% YoY | Counterpoint |
| VR headset shipments, Q4 2025 | −44% YoY | Counterpoint |
| AR glasses shipments, Q1 2026 | **+136% YoY** | Counterpoint |
| Display-less smart glasses, Q1 2026 | **+210% YoY** | Counterpoint |
| Total XR shipments, 2025 | +44.4% YoY | IDC |
| Meta share of XR shipments (incl. glasses), Q2 2026 | 68.7% | IDC |
| Meta share of AR/VR headsets, Q2 2026 | 54% (from 72% in Q2 2025) | Counterpoint |
| Quest 3 US price from 19 Apr 2026 | **$599.99** (+$100) | Meta / TechCrunch |
| Quest 3S US price from 19 Apr 2026 | $349.99 / $449.99 | Meta / TechCrunch |
| Quest 3 share of SteamVR, July 2026 | 26.25% (2nd, behind Quest 2 at 26.66%) | Valve |
| Steam users with a VR headset, Jan 2026 | 1.77% | Valve |

**The one counter-signal [DATA].** Meta told GDC 2026 that *"Quest usage hit an all-time high in 2025… we hit our all-time highest numbers of unique users ever in our history."* So the installed base is being **used more** while hardware sales **fall**. That distinction is the whole game: Meta's problem is not that people won't use VR, it is that **not enough new people buy into it, and the ones who do don't buy enough software.**

> **[INFERENCE]** A platform whose usage is at an all-time high but whose hardware sales are down 42% and whose store revenue is flat has a *monetisation and retention* problem, not an engagement problem. Which is exactly why the 2026 judging criteria reward **"repeat usage," "habit-forming purpose," "reason to come back"** — the vocabulary of retention, not the vocabulary of immersion.

## 4.3 The platform economics [DATA]

| Metric | Value |
|---|---|
| Horizon Store gross revenue, 2025 | "up very slightly" YoY, in a no-hardware-launch year |
| Total platform payment volume, 2025 | "similar year-over-year" |
| IAP revenue growth, 2025 | **+13%** (Meta, Feb 2026); "+over 10%" (Meta, Mar 2026) |
| IAP apps reaching ≥$500k, 2025 | **+20%** |
| Titles generating **≥$1M** gross, 2025 | **>100** |
| Apps that have earned ≥$1M (cumulative, Connect 2025) | **>300** |
| Apps that have earned ≥$50M (cumulative) | **10** |
| Meta Horizon+ subscribers | **>1M** |
| Meta Horizon+ payouts to developers, 2025 | **~$20M** |
| Meta Horizon+ catalogue | 100+ games |
| Revenue split | 70/30 (developer/Meta) |
| Horizon Worlds UGC effective take rate | ~47.5% (30% Store + 25% Horizon on the remainder) |
| Free-to-play share of Quest time spent | ~70% |
| Oculus Publishing titles shipped, 2025 | **140+** |
| Meta spend on VR developer programmes, 2025 | **~$150M** |
| Developers prioritising Quest/Horizon Store | ~63% |
| **Share of effective VR time spent in third-party apps** | **86%** |

**Three derived facts that matter enormously:**

**(a) The competition is 0.67% of Meta's own developer-programme spend.**
$1,000,000 ÷ $150,000,000 = 0.67%. The competition is not how Meta funds VR content. Oculus Publishing (140+ titles in 2025) and Ignition (21 startups sponsored) are how Meta funds content. **[INFERENCE]** Therefore the competition's purpose is not capital allocation. At ~1,100 expected submissions, Meta is paying roughly **$900 per prototype** — versus millions per Oculus Publishing title. That is not funding; it is **broad-spectrum search**. Meta is buying *many cheap attempts* to discover which hands-first ideas work, at a price where failure is free.

**(b) The 86% figure is the platform's defining dependency.** Meta shut down its own first-party studios, and 86% of headset time now goes to software it does not make. **[INFERENCE]** A platform owner in that position has exactly one strategic instrument left: *steer the third parties.* Not fund them at scale (0.67%), not build it itself (it tried and retreated) — **steer** them, with prizes, rules and status.

**(c) The power law is brutal and the middle is thin.** >300 apps have earned $1M+ cumulatively, but only ~100 did so *in 2025*, and just 10 have passed $50M. Meanwhile Store revenue was flat and the number of $500k IAP apps grew 20%. **[INFERENCE]** The distribution is long-tailed with a small viable middle. A competition entrant should assume they will never reach the top of that tail; the strategic play is a **special award or a runner-up**, not a $50M hit.

## 4.4 The developer-relations picture [DATA]

Direct quotes, in order of evidentiary weight:

> *"The game industry is changing, and it's painful… VR is not immune… **We've even had to shutter some of our own first party studios**, who were frankly amongst the most seasoned and experienced VR developers in the world."*
> — Meta, *State of VR at GDC 2026*

> *"As our CTO… recently acknowledged, **the VR industry hasn't grown as much or as quickly as we'd hoped.** But it is growing."*
> — Samantha Ryan, Feb 2026

> *"The decision to include Worlds in the Store was based on a hypothesis that it would increase device retention… **we concluded that our hypothesis about Worlds' impact on device retention had not been proven out.**"*
> — Meta, *State of VR at GDC 2026*

> *"This particular design choice was **universally unpopular with our developer community**. We heard the feedback loud and clear… we've removed them from the Store shelves."*
> — ibid.

> *"While we're proud of the world-class work from Oculus Studios over the years, among 1P and 3P apps, **86% of the effective time people spend in their VR headsets is with third-party apps.**"*
> — Samantha Ryan, Feb 2026

> *"In 2025, we invested **nearly $150 million in VR developer programs**, including our Start Developer Competition."*
> — ibid.

> *"With this narrower, more targeted focus on our VR platform, **we'll do fewer things — and do them better**."*
> — ibid.

**Independent developer sentiment [DATA].** A documented "VR visibility crisis" (GDC 2025 panel): store placement is "no longer the be-all end-all"; marketing support "dried up"; one developer said *"the Quest Store is not on a good path. As a result, we will see a good amount of VR studios close or pivot to non-VR in the next years."* Another: *"our sales numbers so far are lower than our lowest expectations."* Meta's counter at the time: platform spend increased 12% in 2024.

> **[INFERENCE]** This is the political function of the competition. After two years of retreat, store changes, studio closures and price rises, Meta needs to be seen investing in developers. A $1M competition with heavy programming (weekly office hours, mentors, playtest playgrounds, named guest experts) is an extremely efficient way to generate *perceived* reinvestment — because the unit cost of attention is low and the cultural visibility is high. The competition is simultaneously a **supply instrument** and a **trust repair instrument.** Both are real; they are not in conflict.

## 4.5 The Horizon Worlds retreat [DATA]

| Metric | Value |
|---|---|
| Horizon Worlds mobile app downloads (cumulative to Mar 2026) | **45 million** |
| 2026 downloads to date (Mar 2026) | ~1.5M (+53% YoY) |
| **Lifetime consumer spending on the app** | **~$1.1 million** |
| Horizon mobile MAU growth, 2025 | **>4×** |
| Mobile-only worlds, 2025 → 2026 | **0 → 2,000+** |
| Creators past $1M lifetime revenue (Feb 2026) | 4 |
| Creators earning six figures in 2025 | ~100 |
| Worlds removed from Quest Store shelves | 31 Mar 2026 |
| VR support for Worlds | Planned removal 15 Jun 2026, **reversed 19 Mar 2026** |
| Horizon Create / Horizon Studio | AI-prompt game creation, mobile app + web studio, waitlist |

> **45 million downloads and $1.1 million of lifetime consumer spend is $0.024 per download.** That is the single most instructive number in this dossier. **[INFERENCE]** It tells us that Meta has learned the hard way that **free reach does not create a paying ecosystem.** The 2026 competition's emphasis on *"repeat usage," "habit-forming purpose,"* and *"rewarding, habit-forming purpose"* is a direct correction: Meta no longer wants attention, it wants **retention that converts.** Any concept we propose should be legible as a *habit with a monetisable shape*, not as an experience that wows once.

## 4.6 The competitive picture [DATA]

| Competitor | Position in late 2026 |
|---|---|
| **Valve Steam Frame** | Shipped 14–18 Sept 2026, **$1,059 (256GB) / $1,299 (1TB)**, 440g, 2160×2160/eye, Snapdragon 8 Gen 3, SteamOS, 16GB RAM, controllers **included**, standalone + foveated PC streaming. Reservation-lottery distribution. |
| **Apple Vision Pro (M5)** | $3,499. ~45,000 units in 2025 (IDC). Low-single-digit unit share. Reports of reduced parts of its Vision Pro operation. |
| **Samsung Galaxy XR** | $1,799, available in the US. Android XR. Premium productivity/MR. |
| **Pico (ByteDance)** | ~5–6% headset share. |
| **Google Android XR** | Primary beneficiary of Meta's Horizon OS third-party retreat; powers Galaxy XR and XREAL Project Aura. |
| **Meta Quest 3 / 3S** | $599.99 / $349.99–$449.99. No new model at Connect 2026. |

**Timing collision.** Valve priced the Steam Frame on **14 September 2026**. Meta announced VR Glasses on **23 September**. Meta opened the competition on **24 September**. Nine days apart, both companies made their VR move for the year.

> **[INFERENCE]** This is not coincidence; it is a positioning response. Valve shipped a **$1,059 headset with controllers, aimed at PC gamers.** Meta announced a **$1,299 device with no controllers, aimed at media and productivity.** Meta's competition is designed to prove the second thesis — that the future of VR is controller-free, glasses-shaped, seated, and media-led — *before the hardware exists to prove it.* If Meta can point at 1,000 hands-first apps in March 2027, the thesis has evidence. If it cannot, the device launches into the same "no content" critique that has dogged every premium headset.
>
> **This reframes what the judges are actually doing.** They are not just picking winners. They are **accumulating proof of a strategic thesis.** A judge who sees our submission is asking, implicitly: *does this strengthen or weaken the case that controller-free, seated VR is a real category?* That is our real evaluation criterion, and it is more useful than any rubric.

## 4.7 The organisational picture [DATA]

| Event | Date | Significance |
|---|---|---|
| First-party VR studios shuttered | 2025–26 | Meta admits its own content bet underperformed |
| Horizon OS third-party programme **paused** (ASUS, Lenovo never shipped) | Dec 2025 | Meta cannot yet be the "Android of XR" |
| Quest for Business / Horizon Managed Services closed to new customers | 20 Feb 2026 | Enterprise exit |
| Worlds separated from Quest; Store shelves cleared | 19 Feb / 31 Mar 2026 | Worlds deprioritised in VR |
| Quest prices raised | 19 Apr 2026 | Margin defense, memory-cost driven |
| Reality Labs loss guidance cut for 2027 | 21 Oct 2026 | AI-first reallocation |
| No new Quest at Connect 2026 | 23 Sep 2026 | Hardware cadence paused |
| Horizon Create / Horizon Studio | Sep 2026 | Attempting to manufacture supply without traditional developers |

> **[INFERENCE]** Read as a sequence, this is a platform narrowing its surface area to survive. Enterprise gone, third-party hardware gone, UGC-in-VR gone, first-party studios reduced, hardware cadence paused. **What remains is exactly four things: Quest 3/3S, the Store, third-party developers, and the VR Glasses bet.** The competition targets the third of those, to de-risk the fourth. That is the whole strategic logic, and it is coherent.

---

# PART V — DIAGNOSIS: THE SEVEN PROBLEMS

Each problem is stated as a testable claim, with evidence, and with the specific mechanism the competition uses to address it.

---

## Problem 1 — Demand is contracting, and price rises are making it worse

**Claim.** Meta's VR hardware business is in structural decline, and Meta's own pricing decisions are accelerating it.

**Evidence [DATA].** Quest shipments −42.3% (2025, IDC); VR headset shipments −17%/+Q1'26 and −18% Q2'26 (Counterpoint); Quest 3 price +$100 to $599.99 in April 2026; Counterpoint explicitly attributes part of the Q2 decline to *"Meta's decision to increase VR headset prices amid the global memory price surge."* No new Quest at Connect 2026; Zuckerberg: ship only on "major breakthroughs."

**Mechanism.** A competition cannot fix unit economics. But it can increase the **value of each unit sold** by increasing the software available — which is the only variable Meta controls without cutting price.

**What this implies for us [INFERENCE].** Meta is not looking for a demo that flatters the hardware. It is looking for software that makes a $599 headset feel worth $599. **Value density per install** is the implicit ask.

---

## Problem 2 — Reality Labs is being defunded relative to AI, so every dollar must be leveraged

**Claim.** VR is no longer the priority inside Reality Labs. Everything Meta does in VR now must be cheap, high-signal, and self-justifying.

**Evidence [DATA].** Q3 2026 RL revenue $443M vs. loss $4.13B; 2026 loss guidance $18–19B → 2027 ~$15B; "pace its virtual reality spending to put AI first"; capex $34.9B/quarter, free cash flow −$2.12B; headcount −15%. Meanwhile Meta Model API, Muse, Muse Code, Muse Glimmer, 1,500+ connectors, and $349–$449 AI glasses are where the growth and the narrative are.

**Mechanism.** $1M in prizes + ~$150M dev-programme spend = a leveraged programme. The competition generates 1,000+ artefacts for roughly the cost of one mid-size Oculus Publishing deal.

**What this implies [INFERENCE].** Meta's competition budget rewards **leverage, not luxury**. A build that looks expensive to produce will look *wasteful* to a sponsor that is cutting. A build that looks like it punches above its budget reads as a proof point. **Elegance under constraint is the aesthetic Meta currently rewards.**

---

## Problem 3 — The next device has no controllers and no catalogue

**Claim.** Meta VR Glasses cannot launch successfully with the software that exists today, and Meta knows it.

**Evidence [DATA].** ~100g glasses + puck; **eyes and hands as primary input, no controllers at all**; $1,299.99; spring 2027; FoV ~70°×66° vs Quest 3's 110°×96°; v207 SDK shipped at Connect; **75+ VR games confirmed at launch**, with 70+ games supporting hand tracking including a Beat Saber hand-tracking mode; content partners include YouTube, Prime Video, Disney+, ESPN, IMAX Enhanced, 100+ live sports events/year; Store priority for controller-free titles for users who own no controllers.

**Mechanism.** The competition's hard hands-first requirement, applied to *all three tracks and both divisions*, means every single submission is a candidate for the launch catalogue.

**What this implies [INFERENCE].** This is the most important thing in the dossier. **We are not building for Quest. We are building for a device that ships in five months, is judged on Quest, and whose defining constraint is that it has no controllers.** Any concept that would be *better* or *more natural* without controllers is on-thesis. Any concept that would be *worse* without controllers is off-thesis, no matter how polished.

---

## Problem 4 — Meta cannot supply its own content, and 86% of usage is third-party

**Claim.** Meta's dependence on third-party developers is now structural and admitted.

**Evidence [DATA].** "86% of the effective time people spend in their VR headsets is with third-party apps" (S. Ryan, Feb 2026); first-party studios shuttered; Oculus Publishing shipped 140+ titles in 2025 but is a funding programme, not internal production; Oculus Studios reduced.

**Mechanism.** Steer, don't fund. The competition's *rules* — tracks, divisions, hard requirements, judging criteria, special awards — are a **steering apparatus**. Each rule is a steering signal.

**What this implies [INFERENCE].** Read the rules as a **specification document Meta wrote for its own supply chain.** "Hands-first," "seated," "10 minutes," "not a wrapper," "reason to come back" — that is a product brief disguised as a rulebook. And the special awards are the places where Meta admits it doesn't yet know what works ("Best First Five Minutes," "Boldest Original Concept") and is asking the market to tell it.

---

## Problem 5 — The audience Meta needs is not the audience it has

**Claim.** Meta's revenue depends on converting a future, not-yet-arrived cohort, and its entire 2026 content brief is written for that cohort.

**Evidence [DATA] — the cohort taxonomy, verbatim:**

| Cohort | Meta's own characterisation |
|---|---|
| **VR Elites** (Quest 2-era early adopters) | "still here, but **spending less** than they used to" |
| **Teens** (most active) | "currently the most active audience using Quest, but a large portion of **discovery is happening outside the headset**" |
| **Mainstream Adults** (the bet) | "expect more to enter VR **through media consumption first**, then discover games once they are in-headset… They purchase VR as a **TV replacement first**… They are mid-core, love Meta Horizon+, and **probably play seated without controllers**" |

Plus: *"casual adult consumers who gravitate toward entertainment and enjoy streaming their favorite shows, movies, and sports. **This group enjoys hand tracking and is interested in games and experiences that don't require controllers.** We expect this category of experiences to expand over time."*

**Mechanism.** Three tracks that map onto the cohort journey: **Entertainment** (the entry point — media first), **Gaming** (what they discover next, mid-core, seated), **Productivity** (the daily-habit layer that justifies the purchase). The seated guideline, the 10-minute guideline, and hands-first are the cohort's physical and behavioural profile.

**What this implies [INFERENCE].** **The tracks are not genres; they are stages of a funnel.**
- Entertainment = *acquisition*
- Gaming = *expansion*
- Productivity = *retention*

Meta's ideal submission is not "a great game." It is a product that could plausibly **move someone from media consumption to daily use.** If we can articulate that journey in our submission, we are speaking Meta's own language. That is the highest-leverage positioning available, and it costs nothing to do.

---

## Problem 6 — Developer trust is damaged, and discoverability is broken

**Claim.** Meta's developer community has been alienated by store changes, marketing withdrawal and studio closures, and Meta is actively repairing it.

**Evidence [DATA].** "Universally unpopular with our developer community" (Meta, on Worlds in Store); Worlds removed after the retention hypothesis "had not been proven out"; "we heard the feedback loud and clear"; the "VR visibility crisis" panel; "our sales numbers are lower than our lowest expectations"; "we will see a good amount of VR studios close or pivot to non-VR"; Meta's counter-investments: Deals tab, Developer Profiles, Dev Posts, analytics improvements, Feedback Center with a voting board, season passes, featured bundles, Horizon+ self-serve applications, Ignition Programme (21 startups, 90% continuing), Start mentorship.

**Mechanism.** The competition is a **visible, nameable, shareable act of investment.** It produces winners, blog posts, community sessions, and social proof — all of which are cheap and all of which signal "we are still here."

**What this implies [INFERENCE].** Two-thirds of the value of this competition to Meta is **narrative**, not product. That means: (a) entries that tell a good story about *the developer* are rewarded, because the story is the point; (b) the 500-word description and 3-minute video are not administrative overhead, they are the deliverable Meta most wants; (c) being a credible human with a credible story is an asset. **This is a competition where a solo developer with a clear story beats a studio with an unclear one** — and the 2025 winner list confirms it.

---

## Problem 7 — Meta's own content bets failed, so it is outsourcing the search

**Claim.** Meta has stopped trying to *know* what the killer VR app is, and has started paying small amounts to many people to find out.

**Evidence [DATA].** Horizon Worlds mobile: 45M downloads, $1.1M lifetime spend. Worlds removed from Store. Horizon OS third-party paused. Quest for Business closed. First-party studios shuttered. Horizon Create/Studio = AI-assisted creation to increase supply without developers. And the 2026 special awards are notably *exploratory*: "Best First Five Minutes," "Best Reason to Come Back," "Boldest Original Concept," "Best Agentic Interaction" — these are **questions**, not categories.

**Mechanism.** A cheap, wide portfolio of attempts. $1M buys Meta ~1,100 probes. The winners tell Meta which hands-first loops retain people. That knowledge is worth far more than the prize money.

**What this implies [INFERENCE].** Meta is explicitly saying **it does not know what works.** That is an invitation, and it is also a warning: an entry that is a safe, known quantity answers a question Meta has already answered. An entry that *tests a hypothesis* is answering the question Meta is actually asking. **The boldest defensible concept is the most on-thesis concept** — provided it is *finished*, because the whole point of the exercise is that Meta wants working evidence, not pitches.

---

## 5.8 The synthesis: what single sentence explains the whole competition?

> **[INFERENCE]** Meta is trying to buy, cheaply and at scale, a **launch catalogue of finished, seated, controller-free, habit-forming experiences** that will (a) justify a $1,299 headless-input device in spring 2027, (b) convert media consumers into daily users, and (c) prove to a sceptical developer community and a sceptical investor base that VR's next form factor has supply — and it is doing this in the one year it cannot afford to fund content properly.

Every rule follows from that sentence. Part VI proves it, rule by rule.

---

# PART VI — THE DECODED RULEBOOK

**Technique.** Every rule in this competition is treated as a **post-mortem**: something went wrong, and this sentence is the correction. A rule that cannot be traced to a plausible past failure is either boilerplate or a legal requirement. A rule that traces cleanly to a failure is a *design instruction from the sponsor*, and should be read as such.

The format for each entry is:

> **The rule** → **What problem it solves** → **What failure it responds to** → **What behaviour it makes rational** → **What it implies for us**

---

## A. THE HARD REQUIREMENTS

### A1 — "Hands-first: fully usable with hands, end to end. Can someone complete the entire experience without ever pairing a controller?"

**Problem solved.** Meta VR Glasses have **no controllers at all**. A device launching in spring 2027 with zero controller-free software has no reason to exist. **[DATA]** — Meta VR Glasses: "Nones included; gesture and hand tracking only."

**Failure it responds to.** The 2025 competition made hand interactions a *special award* ($50k × 3). That was a nudge. One year later, Meta's next flagship makes controllers physically impossible, so the nudge became a gate. **[INFERENCE]** Between the two competitions, Meta went from *rewarding* hands-first to *requiring* it — because a hardware deadline arrived.

**Behaviour it makes rational.** Lose the controller as a safety net. That is a genuinely difficult constraint, and it forces real design work: no thumbstick locomotion, no trigger-confirm, no grip-to-grab. You must solve confirmation, locomotion, and manipulation with fingers and eyes only.

**What it implies for us.** The *test* is a judge-literalisable binary: can they finish without a controller? Design for that test as an acceptance criterion, not a goal. And note the deeper strategic point: **the constraint is the opportunity.** Every competitor is being forced into the same unfamiliar design space, which means the field is levelled. A team that has genuinely internalised hands-first design has an advantage that cannot be bought with budget.

---

### A2 — "Policy compliance: adherence to Meta VR/Start policies, Community Standards, Conduct in VR, Developer App Policies, Content Guidelines."

**Problem solved.** Meta is a regulated platform under sustained scrutiny (Q3 2026 alone included a $9.89B legal charge relating to teen mental health litigation and a separate $25B state penalty under appeal). **[DATA]** A competition that promotes content violating its own standards would be a legal and reputational hazard.

**Failure it responds to.** Unrated, unsafe, or adult content reaching a promotional stage.

**Behaviour it makes rational.** Conservative content choices. Age ratings of 10+/13+/18+ are explicitly in scope.

**What it implies for us.** Avoid edgy content, avoid adult themes, avoid anything requiring an 18+ rating. A **13+ rating is the safest default** — it covers teens (the most active cohort) without triggering the compliance surface of 18+.

---

### A3 — "Target hardware: Quest 3 / Quest 3S. Backward compatibility not judged. Judges review on Quest 3 or 3S."

**Problem solved.** Judges need a common device. Meta VR Glasses don't exist in anyone's hands. **[DATA]**

**Failure it responds to.** In 2025, entrants optimising for Quest 2/Pro may have produced builds that ran poorly on the judging device.

**Behaviour it makes rational.** Optimise for the *weakest* listed judging device — the 3S — and treat the Quest 3's extra headroom as margin.

**What it implies for us.** Two things. First, **performance floor is 60 fps on a 3S, and the target should be 72.** Second — and this is the subtle one — **we design for the Glasses' 70° FoV but ship and validate on Quest.** Meta cannot see our FoV work directly, so it must be *documented*. That asymmetry is the single cheapest scoring advantage in the competition.

---

### A4 — "Minimum 60 fps on Meta Quest hardware."

**Problem solved.** Reliability at scale with 1,000+ submissions reviewed by a distributed panel.

**Failure it responds to.** Builds that look good in a recording and stutter in hand. **[INFERENCE]** A sponsor cutting costs cannot afford judges wasting time on crashes, and frame rate is the one quality signal that requires zero expertise to assess.

**Behaviour it makes rational.** Art-direction restraint; aggressive LOD; fixed foveated rendering; render-scale tuning.

**What it implies for us.** A number in a rubric is a **gift**: it is binary, verifiable, and cheap to satisfy. Treat 60 fps as a minimum to *exceed visibly*, and put a frame-rate overlay in the video for three seconds. Evidence beats assertion.

---

### A5 — "Seated-optimized: limit roomscale, no large physical movement. The airplane seat test — does every interaction work in a two-foot radius?"

**Problem solved.** Three problems at once: (i) the target cohort plays seated; (ii) Meta VR Glasses are a tethered-glasses form factor where standing movement is awkward; (iii) roomscale apps cannot be reliably judged in unknown rooms.

**Failure it responds to.** Entrants building standing, roomscale experiences that fail in a judge's small apartment — and, more importantly, that fail the "Mainstream Adults" cohort thesis outright.

**Behaviour it makes rational.** A compact interaction shell; wrist/palm menus; no locomotion.

**What it implies for us.** The 2 ft radius is a **hard geometric constraint that eliminates most VR game genres.** It also — crucially — eliminates most *competitors*, because most hobbyist developers instinctively build standing, roomscale experiences. **The constraint is a filter that works in our favour.**

---

### A6 — "Easy to get into and out of: fast cold start, clean pause/resume, satisfying in ≤10 minutes. The one bus stop test."

**Problem solved.** Retention. **[DATA]** Meta's own retention history is the platform's core weakness: 6.37M monthly actives against ~20M headsets sold in Oct 2022; Zuckerberg acknowledging retention was an issue; the Worlds-in-Store retention hypothesis explicitly **failing**. The cohort Meta wants — media-first adults — will not tolerate a 90-second onboarding, because they are comparing the experience to pressing play on Netflix.

**Failure it responds to.** Apps that take longer to get into than the value they deliver. **[INFERENCE]** The bus-stop framing is borrowed from mobile F2P design, which tells you what mental model Meta is now applying to VR: *session economics*, not *immersion*.

**Behaviour it makes rational.** Ruthless time-to-first-meaningful-action; save states; instant resume; one-sitting scope.

**What it implies for us.** Design the **first 60 seconds as the product**, not the tutorial. And design the *return* visit to be faster than the first — which is what "clean pause/resume" actually asks for.

---

### A7 — "Original, not a wrapper. Thin wrappers around YouTube, ChatGPT etc. are highly discouraged. The take-it-away test."

**Problem solved.** Platform value density. **[DATA]** With 45M Horizon Worlds downloads generating $1.1M in lifetime spend, Meta has direct evidence that high-volume, low-value content does not build an economy. A headset full of wrappers makes the Store look worthless and gives no reason to buy hardware.

**Failure it responds to.** **[INFERENCE]** Almost certainly a real pattern in the 2025 submission pool: "ChatGPT in VR," "YouTube in a virtual cinema," "Netflix but you're in a room." These are trivially buildable, demo well, and are worthless to the platform.

**Behaviour it makes rational.** Build the *core experience* ourselves. Use third-party services as inputs at most.

**What it implies for us.** This rule is the single most useful **concept filter** in the entire document. Before committing to any idea, apply it mechanically: *delete the third-party service. Is there still a product?* If the answer is "a worse version of the product," discard. Note the nuance: Meta does not forbid using media — the Entertainment track *invites* media companion apps. It forbids **the third party being the experience.**

---

### A8 — "No pre-existing codebases in the New division. Adapted division requires a *significant* update."

**Problem solved.** Fairness between very different kinds of entrant, and an incentive for shipped apps to upgrade their modality.

**Failure it responds to.** An entrant submitting a finished, years-old commercial product into a "new" category and winning against 8-week builds.

**Behaviour it makes rational.** Either build fresh and scope tightly, or take an existing app and add a *modality* (hands, multiplayer, MR, UGC tool).

**What it implies for us [INFERENCE].** The qualifying examples are revealing: *"implementing hand interactions as a new feature; adding multiplayer support; introducing a mixed reality interaction mode; releasing a new user generated content creation tool."* **All four are the same thing — a new input or social dimension.** Meta is saying: the value it lacks is not more content, it is **more modality**. Which is exactly the VR Glasses problem restated.

---

### A9 — "Only publicly released SDKs and features. No unreleased Meta tools."

**Problem solved.** Fairness, plus embargo and liability risk.

**Behaviour it makes rational.** v207 and below only.

**What it implies for us.** The newest *permitted* technology is v207 — Gaze Interaction in ISDK. That is the frontier available to everyone and used by almost no one. **The technical ceiling of the competition is a specific API, and it is named in the rubric.** That is an unusually strong signal.

---

## B. THE TRACKS AND DIVISIONS DECODED

### B1 — Why Entertainment is listed first and described in the most detail

**The rule.** "Lean-back media and content experiences — spatial cinema, interactive video, music visualization, immersive storytelling, spatial audio, **media companion apps (e.g., commentary tracks, popup production notes, social comments)**. Content designed to be consumed in any environment."

**Problem solved.** Problem 5 — the "Mainstream Adults" cohort buys VR "as a TV replacement first." **[DATA]** And Problem 3 — the Glasses are explicitly pitched as **"a cinema, courtside seat, workspace, and console,"** with YouTube, Prime Video, Disney+, ESPN, IMAX Enhanced, and 100+ live sports events a year as launch content. **[DATA]**

**Why "media companion apps" is the most under-read phrase in the ruleset [INFERENCE].** Meta has already paid for the *content* (IMAX, ESPN, Disney+, Prime). What it has not paid for is the **layer on top** — commentary, notes, social reactions, analysis, second-screen experiences. That is a gap Meta has left open *deliberately*, because it is the cheapest possible way for a third party to add value to content Meta already owns rights to. A companion app:
- requires no content licensing (we bring the layer, they bring the film)
- is inherently hands-first (point at a thing, annotate it)
- is inherently seated and passive-compatible
- is inherently episodic → a natural "reason to come back"
- is far better on glasses than on a headset (you're watching, not playing)
- demos in 90 seconds

**Behaviour it makes rational.** Build a *layer*, not a *player*.

**What it implies for us.** If we go Entertainment, the strongest on-thesis position is **not** "a VR cinema." It is **the participatory layer over media** — the thing that turns watching into a habit. That is precisely what converts a media-first buyer into a returning user, which is Problem 5's whole thesis.

### B2 — Why Gaming is narrowed to "thrive seated, without controllers"

**The rule.** "Puzzle, strategy, casual, social, narrative — genres that thrive seated, without controllers."

**Problem solved.** Filtering out the enormous mass of VR game design that assumes standing + controllers. **[INFERENCE]** Without this clause, 80% of Gaming submissions would be shooters, rhythm or action games requiring roomscale and triggers — useless for the Glasses and useless for the cohort.

**Behaviour it makes rational.** Tabletop, diorama, puzzle, turn-based, narrative — the "board game" family.

**What it implies for us.** Gaming is the **most crowded** track (highest hobbyist appeal, largest existing asset libraries) and the most constrained. If we enter Gaming we must have a genuinely novel hand *mechanic*, not a novel hand *skin*. The 2025 evidence is unambiguous: the winners treated **the hand itself as the game object** (Hand Survivor, Pocket Lands, Awesome Hand, HandCraft XR) or reduced interaction to a **single primitive** (Tiny Golf: "all you need is a pinch").

### B3 — Why "Lifestyle" became "Productivity," and why the brief names routines

**The rule.** "Apps that make you more effective anywhere or are designed around **specific daily moments** — multi-panel workspaces, task management, creative tools, or **habits tied to a recurring context (morning routine, commute, wind-down)**."

**Problem solved.** Retention. **[DATA]** Meta's own analysis says "Mainstream Adults… purchase VR as a TV replacement first, then discover that it can also play video games… They are mid-core, love Meta Horizon+." A device bought for media needs a *non-media* reason to be worn on a Tuesday morning. Productivity-as-ritual is that reason.

**Failure it responds to.** **[INFERENCE]** "Lifestyle" in 2025 was too vague — it produced a scattered field (Tarot readings, music, meditation). "Productivity" plus named daily moments is a deliberate narrowing toward **habit**, which is the platform's actual deficit.

**Behaviour it makes rational.** A product tied to a *time of day* or *situation*, not a general-purpose tool.

**What it implies for us [INFERENCE].** This is the **least contested and most on-thesis** track. Three reasons:
1. The prize is $70k rather than $100k — a 30% discount that likely under-prices a much thinner field.
2. Productivity is the hardest to fake, so fewer hobbyists attempt it.
3. It is the only track whose brief is *literally* about retention, which is Meta's #1 problem — meaning a strong Productivity entry answers the question the sponsor is most anxious about.

**The risk.** Productivity is the *weakest* category commercially on Quest (no credible published evidence of a productivity gain; Quest for Business was closed to new customers in Feb 2026 **[DATA]**). So Meta's own enterprise retreat is in tension with its consumer productivity ambition. **[SPECULATION]** The resolution is that Meta wants *consumer* productivity — a daily ritual for one person — not enterprise. The brief's language ("morning routine, commute, wind-down") supports that reading.

### B4 — Why the two divisions carry identical prize money

**The rule.** New: $100k/$50k (Gaming, Ent), $70k/$35k (Prod). Adapted: identical.

**Problem solved.** **[INFERENCE]** Meta wants *shipped, store-quality* apps as much as new ones. The 2025 evidence supports this: two of four track winners were updates (Little Critters' hand-tracking update; Saber Punks' month-long MR integration). Equal money tells the market that **an upgrade is worth as much as a new build**, which is exactly what Meta needs before a hardware launch — existing apps retrofitted with hands are faster to ship than new apps built from scratch.

**Behaviour it makes rational.** If you have a pre-existing build, upgrade its modality rather than start over.

**What it implies for us.** With 12 days and no existing app, we are a **New** entry by necessity. But we should be aware that we are competing against other ≤8-week builds, and that judges will apply the same four criteria — meaning **completeness will be read generously if inventiveness is high**, and **scope will be read harshly if polish is low**.

### B5 — Why Productivity is worth 30% less

**The rule.** $70k/$35k vs. $100k/$50k.

**Problem solved.** **[INFERENCE]** A revealed preference. Meta is signalling that it expects fewer, or less strategically valuable, Productivity entries — and it is hedging. Alternatively, it expects the field to be thin and is discounting the prize accordingly to avoid overpaying.

**Behaviour it makes rational.** Either avoid the track (bad money) or flood it (thin field).

**What it implies for us [INFERENCE].** This is a genuine expected-value question, and the answer depends on the field size, which is unpublished. My read: **if Productivity attracts even a third of Gaming's submissions, its EV per entrant is better**, because $70k ÷ (field/3) > $100k ÷ field. The only way it's worse is if the field is nearly as large. Given the difficulty of making a hands-first productivity app, I'd weight the thin-field hypothesis more heavily — but this is the single most consequential unresolved input, and it is listed in Part IX.

---

## C. THE SUBMISSION MECHANICS DECODED

### C1 — Why a release-channel invite URL, and why "APKs will not be considered"

**Problem solved.** Security, version control, revocation, and auditability. Judges installing arbitrary binaries is unacceptable for a company with Meta's legal exposure.

**Failure it responds to.** **[INFERENCE]** Prior years' submissions where the build could not be launched, or where the submitted artefact was a file the judge could not verify the provenance of.

**Behaviour it makes rational.** Early infrastructure work: you must create the release channel before you have something to put in it.

**What it implies for us.** **Do this today.** A release channel named `Competition` and a verified invite URL is the single highest-priority task in the entire build, because everything else is worthless without it. And note the FAQ's escape hatch: if the invite URL fails, a public Store link is accepted — so having a published app is itself a fallback.

### C2 — Why the build freezes through judging

**Problem solved.** A fair comparison at one point in time, and protection against post-deadline gaming.

**Behaviour it makes rational.** Ship your best version before the deadline, then stop.

**What it implies for us.** The last 48 hours should be *verification*, not development. Plan a hard internal freeze 24–30 hours before the deadline.

### C3 — Why the video must be <3:00, public, and device footage

**Problem solved.** Judge throughput. With ~1,100 submissions and a two-stage panel, no judge can watch more than three minutes. And **the video is the artefact that is reviewed first, by everyone.**

**Failure it responds to.** **[INFERENCE]** Over-long videos with a 40-second logo animation; footage that is not representative; footage that is not actually from the device.

**What it implies for us.** The video is not documentation — it is **the primary submission.** It should be storyboarded with the same rigour as the product. And note Meta's own explicit craft guidance from the Week-6 update: *"open your submission video with real gameplay that makes your hands-first design clear in the first 30 seconds."*

### C4 — Why "don't lean on AI-generated video to carry the pitch"

**Problem solved.** **[INFERENCE]** Signal quality. Given 2026 is the year of trivial AI video generation, Meta is pre-empting a flood of beautiful, non-representative trailers. The judging page says: *"show real gameplay that honestly represents the experience… Don't lean on AI-generated video to carry the pitch."*

**Behaviour it makes rational.** Real capture. Ugly-but-true beats pretty-but-false.

**What it implies for us.** Do not generate a single frame of the demonstration. A slightly rough phone-shot-composted POV capture with a frame-rate overlay is *more* persuasive than a polished synthetic trailer, because it reads as evidence.

### C5 — Why the 500-word description and 140-character tagline

**Problem solved.** Judge throughput, comparability, and — since AI may assist judging — machine legibility. **[DATA]**

**What it implies for us.** Write for two readers simultaneously: a domain expert scanning for platform fluency, and an LLM extracting claims. Concrete nouns (MRUK, ISDK v207, gaze interaction, FoV profile, 60 fps, Quest 3S) score with both.

### C6 — Why "target launch date" is a required field

**Problem solved.** **[INFERENCE]** Meta wants to know whether this is a *product* or a *demo*. A launch date is a commitment, and it converts a competition entry into a pipeline lead.

**Behaviour it makes rational.** Stating a real intention to ship.

**What it implies for us.** A near-term, credible date — ideally before the Glasses launch in spring 2027. Stating "Q1 2027" signals you intend to be on the launch slate. That is exactly the signal Meta is buying.

---

## D. THE JUDGING DESIGN DECODED

### D1 — Why four equally weighted criteria, and why these four

**The structure.** Innovation & Creativity 25% · Experience Design 25% · Technical Implementation 25% · Polish & Presentation 25%.

**Problem solved [INFERENCE].** It is a **balanced scorecard designed to prevent any single failure mode from winning.** A beautiful tech demo (Innovation + Technical, weak Experience) cannot win. A polished clone (Polish + Experience, weak Innovation) cannot win. A brilliant unfinished prototype (Innovation, weak Polish + Technical) cannot win.

**What it implies for us.** **The competition rewards the minimum of the four scores, not the sum.** A concept scoring 9/5/5/9 (28) loses to one scoring 7/7/7/7 (28) on a tie-break, and loses clearly to 7/7/7/9 (30). **We should therefore optimise for the weakest axis, continuously, from day one.** Most entrants will do the opposite — pour everything into the axis they enjoy.

### D2 — Why "Innovation" explicitly names *repeat usage*

**The rule.** "…uniquely leverage Meta device capabilities… **to drive repeat usage.**"

**Problem solved.** Problem 6 (retention). **[INFERENCE]** Meta has redefined innovation from "novel" to "novel *and* retentive." That is a commercial definition, not an aesthetic one.

**What it implies for us.** Novelty alone is insufficient; novelty must be *replayable*. A one-time wow is a 5 on this criterion. A wow that has a reason to return is an 8+.

### D3 — Why "Experience Design" names passthrough purposefulness

**The rule.** "Passthrough should be purposeful: the player's real room should meaningfully change the experience, rather than a scene that plays identically with passthrough off."

**Problem solved.** **[INFERENCE]** A flood of submissions that turned passthrough on as a visual style — a floating menu in your living room — without any mechanical consequence. Meta is telling us it has seen this and is unimpressed.

**What it implies for us.** If we use passthrough, we must be able to point at a *specific mechanic* that requires the real room. If we cannot, we should consider not using it — or using it as a deliberate, contrasting mode. Note that this rule is **also a test a judge can run in 20 seconds**, which makes it unusually high-stakes.

### D4 — Why "Technical" names specific APIs

**The rule.** "…particularly the features that define hands-first experiences: **gaze interactions (Unity/Unreal ISDK v207+), hand tracking, passthrough, spatial anchoring, and FoV-aware design.**"

**Problem solved.** Legacy: Meta needs the ecosystem to adopt the new interaction model, not the old one. 

**Why this list is so valuable to us [INFERENCE].** It is a **checklist of five named technologies, and each is individually demonstrable.** A judge evaluating our submission can literally tick them off. That converts a subjective 25% criterion into roughly five yes/no questions plus a quality judgement. **We should be able to tick all five.**

### D5 — Why "holding up in rooms the developer never tested"

**Problem solved.** **[INFERENCE]** Developers hardcoding a single tidy living room, then the app failing in a judge's bedroom. Meta is stating the failure explicitly, which means it has seen it repeatedly.

**What it implies for us.** A **degenerate-case plan**: no detected planes, a bed as the only surface, a 1 m² room, a wall at 0.5 m. And we should say so in the description, because it is directly responsive to stated criterion language.

### D6 — Why "Polish" includes the submission materials themselves

**The rule.** "This includes **high-quality submission materials (text, images, video)** that effectively communicate the project's value."

**Problem solved [INFERENCE].** Judges cannot evaluate what they cannot understand. Meta has made the *explanation* part of the score, which is an extraordinary gift to a small team with a clear story.

**What it implies for us.** A full 25% of the score is available for one day of writing, shooting, and editing. No other 25% is that cheap. This is the single highest-ROI activity in the competition.

### D7 — Why two-stage judging with a pass/fail screen

**Problem solved.** Throughput. **[INFERENCE]** ~1,100 submissions × 4 criteria × careful review is not feasible. Stage One is a cheap compliance filter; Stage Two is the real judgement.

**What it implies for us.** Stage One failure is catastrophic and entirely avoidable. **Every point we would have scored becomes zero if the build doesn't launch.** Compliance is not admin — it is a gating condition.

### D8 — Why AI may assist judging

**The rule.** "AI tools may also be used to assist in the judging, but final decisions will be made by human judges."

**Problem solved.** Scale and consistency.

**What it implies for us [INFERENCE].** Write for retrieval. Structured text, named technologies, explicit claims, quantified statements. Avoid metaphor. A human rewards a beautiful sentence; an extraction model rewards a structured claim. Do both.

### D9 — Why there are eight special awards and no honorable mentions

**The structure.** 2025: $100k/$60k/$30k per category + 12 technical specials. 2026: $100k/$50k per category + 8 *behavioural* specials.

**Problem solved [INFERENCE].** In 2025 the technical specials rewarded *capability* (Spatial SDK, IWSDK, React Native, Android). In 2026, hands-first is mandatory, so rewarding capability is redundant. The specials were repurposed to reward **the unknowns Meta most needs answers to**: will people come back? is the first five minutes easy? can it be social? can it be accessible? is there anything genuinely new?

**What it implies for us.** **The special awards are Meta's open questions.** Answering an open question is worth more than re-answering a settled one. And three of the eight — *First Five Minutes*, *Reason to Come Back*, *Accessibility Forward* — are answerable by a small, finished build without any additional content. That is our lane.

### D10 — Why "one prize per entry, highest value"

**The rule.** "Each Entry is only eligible for one prize. If in consideration for multiple prizes, you will be awarded the highest prize value."

**Problem solved.** Fair spread; prevents one entry sweeping multiple cash awards.

**What it implies for us.** Special awards are a **floor, not a bonus**. This changes strategy: if we believe we have a real shot at a track prize, chasing specials is wasted differentiation. If we do not, a special *is* the outcome. **We must decide which game we are playing before we design.**

---

## E. THE ELIGIBILITY AND LEGAL DECODED

### E1 — Why Start membership is a hard gate

**Problem solved.** **[INFERENCE]** The Start programme is Meta's developer-CRM. Gating on membership means every entrant is *already in the funnel*, contactable, trackable, and known. The competition becomes a re-engagement campaign with a prize, not an open call.

**What it implies for us.** Joining is free and takes ~5 minutes, but verification is asynchronous. **Confirm forum flair this week.**

### E2 — Why Brazil, Quebec and sanctioned regions are excluded

**Problem solved.** Legal and banking feasibility. The rules state it plainly: *"financial institutions are restricted from transmitting funds to certain countries. Under contest laws, it is a regulatory violation to allow individuals to enter a competition if they have no legal path to claim the prize."*

**What it implies for us.** India is eligible. But note the tax consequence: non-US winners receive an **IRS 1042-S** and *"actual prize payment may be less than the total ARV"* — expect withholding. Any budget built on the headline number should be discounted.

### E3 — Why Meta-funded apps are excluded

**Problem solved.** Conflict of interest, and the purpose of the competition. **[DATA]** Meta already spends ~$150M/year on developer programmes including Oculus Publishing (140+ titles in 2025) and Ignition. If a Publishing-funded title could win the competition, the prize would simply be a round-trip of Meta's own money, and the competition would not be discovering anything new.

**What it implies for us.** The competition exists to find things **Meta's existing funding apparatus did not find.** That is a statement about *what kind of idea* is wanted: one that would not have been funded by a publishing deal. **[INFERENCE]** Publishing funds known-good genre bets with proven teams. The competition funds **unknown shapes from unknown teams.** Our advantage is exactly that we are unknown.

### E4 — Why one entry per person, but unlimited team size

**Problem solved.** Prevents portfolio-spamming while allowing genuine collaboration.

**What it implies for us.** One shot. Optimise it.

### E5 — Why the perpetual marketing licence alongside retained IP

**Problem solved.** Meta needs to feature winners in marketing in perpetuity without renegotiating. Entrants keep ownership, so the deal is defensible.

**What it implies for us.** If we win, our footage will likely be used in Meta marketing. Design for that: a build that *photographs well* and has a clear one-line story is more valuable to Meta, and therefore more attractive, than a build that is only good in the hand.

---

## F. THE TIMELINE DECODED

### F1 — Why the competition runs 24 September → 18 November

**The alignment.** Connect 2026 was 23–24 September. VR Glasses were announced on the 23rd. The competition opened on the **24th** — the second day of Connect, the developer day, the day the **v207 SDK became available**. **[DATA]** It closes 18 November. Winners are announced 11 December.

**What this proves [INFERENCE].** The competition is *structurally attached to the SDK release*, not to a calendar slot. It is the **activation campaign for v207.** Its purpose is to get developers onto the new interaction model immediately, at the moment of release, with a deadline that forces them to finish.

### F2 — Why 8 weeks, and why spring 2027 matters

**The arithmetic.** 8 weeks from 24 September = 18 November. Judging to 11 December. Then roughly **four months to the Glasses' spring 2027 launch.**

**What this proves [INFERENCE].** The four-month gap is the point. It is enough time for winners to be polished, published, and prepared for a launch slate; not enough time to build something from scratch. **Meta is selecting for products that can be launch-ready by spring 2027.** Any entry that signals "this will take a year" is off-slate, and therefore off-thesis.

**What it implies for us.** Our target launch date and our scope should both imply *"shippable within months."* That is a positioning statement as much as a scheduling one.

### F3 — Why the prize is $1M and not $1.5M, despite a bigger field

**The fact.** 2025: $1.5M, 32 awards, 2,865 participants, 650+ submissions. 2026: $1M, 20 awards, 5,371 participants. **[DATA]**

**What it proves [INFERENCE].** Participation **grew 87%** while the pool **shrank 33%.** If the competition were a straightforward marketing spend, you would scale the prize with the audience. Cutting it means Meta has learned that the audience is acquired *without* needing more money — the first edition proved the mechanism works. It also matches the Reality Labs cost discipline story. **The prize is now calibrated to what is necessary, not to what is generous.**

**What it implies for us.** The field is roughly **twice as crowded** as last year, for **two-thirds of the money**. Per-dollar competition is ~3× more intense. **We must be more differentiated, not merely better.**

---

## 6.7 The rulebook in one table

| Rule | Problem it solves | What it really says |
|---|---|---|
| Hands-first, end to end | Glasses have no controllers | Design for the 2027 device, not the 2023 one |
| Seated, 2 ft | Target cohort plays seated | Small interaction shell; no locomotion |
| 10 minutes | Retention crisis | Session economics, not immersion |
| Not a wrapper | Low-value supply flood | Build the core experience |
| 60 fps min | Judge throughput + reliability | Be visibly fast |
| Gaze interactions v207+ | New input model adoption | Use the newest permitted API |
| FoV-aware design | 70° device | Two-zone layout, documented |
| Passthrough must be purposeful | Decorative MR | Real room must be load-bearing |
| Holding up in untested rooms | Hardcoded rooms | Degenerate-case plan |
| Korean/English/subtitles | Judge language | Make it legible |
| Release channel + invite URL | Security/verifiability | Infrastructure first |
| Video <3:00, real footage | Judge throughput | The video *is* the submission |
| No AI-generated video | Slop detection | Evidence over beauty |
| One prize per entry | Fair spread | Specials are a floor |
| Adapted = equal money | Shipped apps matter | Modality upgrades are valuable |
| Productivity = $70k | Lower expected value to Meta | Thin field, discounted prize |
| 8 weeks from Connect | SDK activation campaign | Finish fast, ship by spring 2027 |
| Start membership gate | Developer CRM | Re-engagement campaign |
| No Meta-funded apps | Discover what funding misses | Unknown shapes, unknown teams |

---

# PART VII — THE ROI MODEL: WHAT $1,000,000 ACTUALLY BUYS

## 7.1 The most important number in this dossier

| Calculation | Result |
|---|---|
| Reality Labs operating loss, Q3 2026 | $4.13B |
| Days in quarter | ~91 |
| **Reality Labs burn per day** | **≈$45.4M** |
| **Reality Labs burn per hour** | **≈$1.89M** |
| **Total prize pool** | **$1,000,000** |
| **Prize pool expressed as Reality Labs burn time** | **≈32 minutes** |

**[INFERENCE]** The entire prize pool of this competition is equal to **about half an hour of Reality Labs' operating loss.** The whole programme, including administration and staff time, is probably under an hour of burn.

This single fact reorganises everything:

1. **The competition is financially free to Meta.** It is not a budget decision; it is a rounding error. Meta can and will run it annually forever, regardless of VR's fortunes.
2. **It cannot be evaluated on financial ROI.** No plausible return on $1M matters at this scale. **It must be evaluated on information value and narrative value** — the two currencies that *do* matter at this scale.
3. **Meta has almost no incentive to increase the prize.** Doubling it to $2M would add ~32 minutes of burn and probably not double the quality of the field (the 2025→2026 data suggests participation grew 87% while the prize *fell* 33%). The prize is calibrated to *sufficiency*.
4. **The scarce resource is not money — it is attention.** Which means: what Meta is optimising is **how many high-quality, on-thesis artefacts it can extract per unit of its own staff attention.** This explains the heavy programming (20 webinars, office hours, mentors, playtests): staff attention is being spent to raise the *floor* of the field, because a floor-raising intervention is cheaper than a prize increase.

## 7.2 The cost side

| Cost item | Estimate | Basis |
|---|---|---|
| Prize pool | $1,000,000 | Published **[DATA]** |
| Platform / administration (Devpost-managed) | $100k–$200k | **[SPECULATION]** — typical range for managed programmes of this size; not disclosed |
| Meta staff time: ~20 sessions × 2–3 mentors, plus judging panel of unknown size, plus programme management | $150k–$400k | **[SPECULATION]** — assumes 10–25 FTE-months equivalent loaded |
| Guest expert fees (Blender artist, XRCC) | $0–$20k | **[SPECULATION]** |
| Marketing / PR (blog posts, social, Connect mentions) | $50k–$150k | **[SPECULATION]** — largely internal channels |
| **Total programme cost** | **≈$1.3M–$1.8M** | **[INFERENCE]** |
| **As Reality Labs burn time** | **≈41–57 minutes** | **[INFERENCE]** |

## 7.3 The value side

| Deliverable | Volume | Implied unit value | Confidence |
|---|---|---|---|
| **Finished hands-first builds** | ~1,100 expected submissions (est. from 22–25% conversion of 5,371 registrants) | **~$900 per prototype** | **[INFERENCE]** — conversion rate derived from 2025 (650/2,865 = 22.7%) |
| **Adapted/upgraded shipped apps** | Unknown subset; 2025 evidence suggests a meaningful share | — | ○ |
| **Launch-slate candidates** | If 2–5% convert to launch-window titles: **22–55 apps** | **~$18k–$45k per candidate** | **[SPECULATION]** |
| **Hands-first design knowledge** | 1,100 prototypes reveal *which hand loops retain* | Not purchasable at any price | **[SPECULATION]** — but this is the real asset |
| **Developer acquisition** | Registrations 2,865 → 5,371 (**+87%**) | — | **[DATA]** |
| **Competitive signalling** | Counter-position vs. Steam Frame (shipped 9 days earlier) | — | **[INFERENCE]** |
| **Investor narrative** | "Meta remains the single biggest investor in the VR industry" | — | **[DATA]** (S.&nbsp;Ryan) |
| **Marketing content** | Winners' footage, testimonials, case studies | — | **[INFERENCE]** |
| **Trust repair** | Visible reinvestment after two years of retreat | — | **[INFERENCE]** |

## 7.4 The comparison that defines the competition's function

| Meta content-spend mechanism | Approximate unit economics | Function |
|---|---|---|
| **Oculus Publishing** | 140+ titles in 2025; per-title deals typically far larger than $100k | Fund **known-good bets** from proven teams |
| **Ignition Programme** | 21 startups sponsored to build prototypes; 90% continuing | Seed **new studios** |
| **Meta Horizon+** | ~$20M paid to developers in 2025 across 100+ titles | Supplement **revenue** for shipped titles |
| **Start Developer Competition** | ~$1M for ~1,100 prototypes ≈ **$900 each** | **Discover unknown shapes from unknown teams** |

**[INFERENCE]** These four instruments are not redundant — they are a **portfolio at four different risk levels.** Publishing is late-stage; Ignition is seed; Horizon+ is revenue support; the competition is **option-generation.** It is the cheapest, widest, riskiest end of the funnel. Its output is not titles — it is **information about which titles to make.**

Which means: **a competition entry is not being judged as a product. It is being judged as an option.** Options are valued on *upside* and *cheapness of exercise*, not on polish alone. That is why "Boldest Original Concept" exists, and why it is worth the same $25,000 as a category.

## 7.5 What Meta does *not* get from this competition

Being precise about this sharpens the strategy:

- **It does not get revenue.** No revenue share is claimed on winners.
- **It does not get IP.** Entrants retain ownership **[DATA]**. Meta takes a marketing licence only.
- **It does not get exclusivity.** Nothing in the rules requires Store exclusivity or prevents shipping elsewhere.
- **It does not get a workforce.** No hiring commitment exists in the rules. **[SPECULATION]** Talent scouting may happen informally, but it is not a stated mechanism.
- **It does not get guaranteed retention.** A winning entry may never ship.

So the *entire* return is: **information, narrative, and optionality.** Which tells us exactly what to give Meta: **clarity of insight, quality of story, and evidence of shippability.**

---

# PART VIII — THE DECISION FILTER

This is the bridge to the concept phase. It is deliberately **not** a list of ideas. It is the set of tests any candidate must pass, derived directly from the evidence above. A concept that fails any **Gate** test is disqualified regardless of how good it is. A concept's rank among surviving candidates is set by the **Score** tests.

## 8.1 Gate tests — pass/fail, non-negotiable

| # | Gate | Pass condition | Derived from |
|---|---|---|---|
| **G1** | **Controller-absence** | The complete experience is finishable with controllers never paired and never in the room | Rule A1 |
| **G2** | **Thesis alignment** | The concept is *better* without controllers. Not merely *possible* without them. If a controller version would be superior, it is off-thesis | §4.6, Problem 3 |
| **G3** | **Two-foot** | Every interaction sits inside a ~2 ft / 0.25–0.6 m shell from the chest, arms resting | Rule A5 |
| **G4** | **Bus-stop** | A complete, satisfying moment in under 10 minutes; cold start to first meaningful action under 15 seconds | Rule A6 |
| **G5** | **Take-it-away** | Delete every third-party service. A product remains. If not, discard | Rule A7 |
| **G6** | **Repeat-usage** | There is a persistent artefact with visible growth across sessions, and an unfinished thing at the end of session one | Rule D2, Problem 6 |
| **G7** | **Twelve-day buildability** | Buildable to *finished* by a small team in the remaining window. "Finished" is a gate, not an aspiration | §4.2, Part I of the prior dossier |
| **G8** | **Rights-clean** | No licensed content, no branded material, no identifiable real people, no Meta funding history | Rules A2, E3 |
| **G9** | **Condition-neutral** | Works in a degenerate room: no detected planes, a bed for a table, 1 m² of space | Rule D5 |
| **G10** | **Minimum-of-four** | No axis scores below 6. A 9/9/9/5 fails to a 7/7/7/7 | Rule D1 |

## 8.2 Score tests — weighted, for ranking survivors

| # | Test | Question | Weight | Best evidence |
|---|---|---|---|---|
| **S1** | **Cohort fit** | Does this serve the media-first "Mainstream Adult" — or move someone from media consumption toward daily use? | 20% | Problem 5 |
| **S2** | **Unfundable-elsewhere** | Would Oculus Publishing have funded this? If yes, it isn't what the competition is looking for | 15% | Rule E3 |
| **S3** | **Open-question value** | Does it answer one of Meta's eight special-award questions — especially First Five Minutes, Reason to Come Back, or Accessibility Forward? | 15% | Rule D9 |
| **S4** | **Platform-fluency surface** | Can we tick all five named technologies: gaze v207, hand tracking, passthrough, spatial anchoring, FoV-aware? | 15% | Rule D4 |
| **S5** | **Launch-slate plausibility** | Could this credibly be on the Meta VR Glasses launch slate in spring 2027? | 10% | Rule F2 |
| **S6** | **Field thinness** | Is the category under-contested relative to Gaming? | 10% | §4.4, Part I dossier |
| **S7** | **Photographability** | Does it read in a 3-second hook and a 30-second silent video? | 10% | Rule C3, D6 |
| **S8** | **Personal durability** | Can we sustain 12 days of intensity on this specific problem without losing conviction? | 5% | Pragmatic |

## 8.3 Track and division placement logic

**Division.** With no pre-existing app, **New Experience** is the only honest option. **[INFERENCE]** It is also the more favourable pool: we are compared against other ≤8-week builds, so judges adjust scope expectations downward — provided we are genuinely finished.

**Track.** Applying the evidence:

| Track | Field expected | Prize | On-thesis | Verdict |
|---|---|---|---|---|
| **Entertainment** | Mid–high; broad brief, many media app builders | $100k / $50k | **Very high** — the cohort's entry point; the Glasses are pitched as a cinema/courtside | Strong *if* we take the "media companion layer" route rather than building a player |
| **Gaming** | **Highest** — largest hobbyist appeal, most existing assets, most precedent | $100k / $50k | High — but narrowly defined (seated, no controllers, casual/puzzle/tabletop) | Only with a genuinely novel hand *mechanic*, per the 2025 pattern (hand-as-game-object, or single-primitive) |
| **Productivity** | **Lowest** — hardest to fake, no genre precedent, and the brief demands a ritual not a tool | $70k / $35k | **Highest of the three** — it is the literal answer to the retention problem | **Best expected value per entrant, if we can make it demonstrably usable and not boring** |

**The call I would make on current evidence:** Entertainment and Productivity are the two strong routes; Gaming is the weak one unless we have a mechanic nobody has seen. But this is the decision for the *next* phase, and it depends on inputs we do not yet have (§9.1).

## 8.4 The one-sentence thesis any concept must be able to say

> **[INFERENCE]** Based on everything above, the winning shape is:
>
> **"A small, finished, seated, gaze-and-hands experience that turns a passive VR moment into a repeatable daily ritual — demonstrably better without a controller, visibly shipped, and shippable by spring 2027."**

Any candidate concept should be expressible in that sentence. If it cannot be, it is probably off-thesis, and the judges — who have internalised the sponsor's own strategy — will feel that even if they cannot articulate it.

---

# PART IX — OPEN QUESTIONS

Honest accounting of what this research could not establish. Each item is paired with what it would change.

| # | Open question | Why it matters | How we'd answer it | What it changes |
|---|---|---|---|---|
| **1** | How many submissions will there be, and how are they distributed across the 3 tracks × 2 divisions? | Determines the entire expected-value calculus, especially the Productivity bet | Not published until the project gallery fills post-deadline. Could be partly inferred from the public project gallery count now | Whether Productivity's thin-field advantage is real |
| **2** | Who are the 2026 judges, and how do they weight within the "equal 25%" frame? | Judging is said to be equal-weight, but panels have priors | Only one judge, Dilmer V, is publicly listed. Others undisclosed | Whether to optimise for technical or experiential appeal |
| **3** | Did the 2025 winners actually ship to the Store? | The single best test of whether this competition functions as a pipeline | Check each 2025 winner on the Horizon Store | Whether Meta's real criterion is "finished" or "promising" |
| **4** | What is the actual conversion from Start member → published app? | Tells us the value of the gate Meta imposes on us | Not published | Whether Start membership is worth active participation |
| **5** | Is there any published evidence that hand-tracked media consumption drives Quest purchases? | The cohort thesis rests on it | Meta has not published this. Could look for GDC 2025/2026 audience-insights data | Whether Entertainment or Productivity is the safer bet |
| **6** | What is the real size and health of the Productivity category on Quest? | The $70k prize implies low confidence; the brief implies high ambition | Store category data, top-grossing lists, Horizon+ catalogue composition | Whether Productivity's discount is justified |
| **7** | How do judges evaluate builds they cannot install or that fail on their device? | Determines the penalty for technical failure | Not published | How much contingency to build |
| **8** | What is the distribution of prior winners by team size and geography? | Tells us whether solo entrants are structurally disadvantaged | 2025 list gives team names; geography unpublished | Whether to recruit collaborators |
| **9** | Does the "one prize per entry" rule mean specials are effectively only awarded to non-track-winners? | Changes whether specials are a floor or a substitute | Rules are ambiguous; "in consideration for multiple prizes, you get the highest" implies both are evaluated independently | How to allocate the final week of work |
| **10** | What is Meta's actual launch-slate process for the Glasses, and does the competition feed it? | Determines whether "launch-slate plausibility" is real or imagined | No public statement links the competition to the slate explicitly — this is my inference | How heavily to weight S5 |
| **11** | Will there be a 2027 edition, and at what scale? | Relevant to whether participation has option value beyond this prize | Pattern suggests yes; the 2025→2026 participation growth (+87%) makes it likely | Whether to build for this competition or this platform |
| **12** | What does Meta's internal definition of "significant update" actually admit? | Affects the Adapted division, not us directly | Only observable through Meta's own decisions | Low — we are a New entry |

## 9.1 Inputs we need from you before the concept phase

The research is complete enough to filter, but not to *choose*. To narrow to a concept we need four things that are about us, not about Meta:

1. **Build capability.** Which engine, and how fluent? Unity (C#) vs Unreal (C++) vs WebXR/IWSDK vs Android/native is a hard constraint on what is achievable in 12 days. Gaze interaction in v207 is best supported in Unity and Unreal.
2. **Team shape and hours.** Solo, or with collaborators? Full-time for 12 days, or evenings? This sets the G7 gate.
3. **Personal assets.** Do you already have domain knowledge — music, sport, teaching, medicine, a craft, a language, a physical skill? The 2025 winners overwhelmingly built in a domain they knew. Tiny Golf, Inkphony, Saber Punks, Handcraft XR — none are generic.
4. **Tolerance for risk.** Do you want the highest-probability path ($25k special via a small perfect build) or the highest-ceiling path ($100k track win via an ambitious bet)?

Answer those four and the filter in §8 collapses to a shortlist quickly.

---

# PART X — DATA APPENDIX AND SOURCE REGISTER

## 10.1 Master data table

### Reality Labs financials
| Metric | Value | Period | Source |
|---|---|---|---|
| Reality Labs revenue | $443M (−6% YoY) | Q3 2026 | Meta Q3 2026 results |
| Reality Labs operating loss | −$4.13B | Q3 2026 | Meta Q3 2026 results |
| Reality Labs revenue | $470M | Q3 2025 | Meta Q3 2026 results |
| Reality Labs operating loss | −$4.43B | Q3 2025 | Meta Q3 2026 results |
| RL nine-month revenue | $1.28B (+2%) | 9M 2026 | Meta Q3 2026 results |
| RL nine-month operating loss | −$12.78B | 9M 2026 | Meta Q3 2026 results |
| RL full-year loss guidance | $18–19B | 2026 | Meta guidance, 21 Oct 2026 |
| RL full-year loss | $19.19B | 2025 | Meta |
| RL loss guidance | ~$15B | 2027 | Meta guidance, 21 Oct 2026 |
| Cumulative RL losses | ≈$92.2B | through 30 Jun 2026 | Treeview tally of filings |
| Meta total revenue | $64.60B (+26%) | Q3 2026 | Meta Q3 2026 results |
| Meta capex incl. finance leases | $34.91B | Q3 2026 | Meta Q3 2026 results |
| Meta free cash flow | −$2.12B | Q3 2026 | Meta Q3 2026 results |
| Meta headcount | 66,775 (−15% YoY) | Q3 2026 | Meta Q3 2026 results |
| Q3 2026 legal charge | $9.89B (52 state AGs, teen mental health) | Q3 2026 | Meta Q3 2026 results |
| New Mexico penalty (under appeal) | $25B | Oct 2026 | Press |

### Hardware and market
| Metric | Value | Period | Source |
|---|---|---|---|
| Quest shipments | −42.3% YoY | 2025 | IDC |
| Quest units shipped | ~1.7M | Q1–Q3 2025 | IDC (F. Jeronimo) |
| Total XR shipments | +44.4% YoY | 2025 | IDC |
| VR headset shipments | −14% | H1 2025 | Counterpoint |
| VR headset shipments | −44% | Q4 2025 | Counterpoint |
| VR headset shipments | −17% YoY, −39% QoQ | Q1 2026 | Counterpoint |
| VR headset shipments | −18% YoY, −16% QoQ | Q2 2026 | Counterpoint |
| AR glasses shipments | +136% YoY | Q1 2026 | Counterpoint |
| Display-less smart glasses | +210% YoY | Q1 2026 | Counterpoint |
| Meta share, AR/VR headsets | 54% (from 72% in Q2 2025) | Q2 2026 | Counterpoint |
| Meta share, all XR incl. glasses | 68.7% | Q2 2026 | IDC |
| Quest 3 US price | $599.99 | from 19 Apr 2026 | Meta / TechCrunch |
| Quest 3S US price | $349.99 / $449.99 | from 19 Apr 2026 | Meta / TechCrunch |
| Quest 3 SteamVR share | 26.25% | Jul 2026 | Valve |
| Oculus Quest 2 SteamVR share | 26.66% | Jul 2026 | Valve |
| Quest 3S SteamVR share | 11.44% | Jul 2026 | Valve |
| Steam users with VR headset | 1.77% | Jan 2026 | Valve |
| Quest monthly active users | 6.37M (vs ~20M sold) | Oct 2022 | WSJ report |
| Apple Vision Pro units | ~45,000 | 2025 | IDC |
| Meta Quest units | ~5.6M (77% share) | 2024 | IDC / Counterpoint |

### Store and developer economics
| Metric | Value | Period | Source |
|---|---|---|---|
| Horizon Store gross revenue | "up very slightly" | 2025 | Meta GDC 2026 |
| Platform payment volume | "similar YoY" | 2025 | Meta, Feb 2026 |
| IAP revenue growth | +13% / "+over 10%" | 2025 | Meta (Feb & Mar 2026) |
| IAP apps ≥$500k | +20% | 2025 | Meta |
| Titles ≥$1M gross | >100 | 2025 | Meta GDC 2026 |
| Apps ≥$1M cumulative | >300 | to Sep 2025 | Meta Connect 2025 |
| Apps ≥$50M cumulative | 10 | to Sep 2025 | Meta Connect 2025 |
| Horizon+ subscribers | >1M | 2025 | Meta |
| Horizon+ developer payouts | ~$20M | 2025 | Meta |
| Horizon+ catalogue | 100+ games | 2025 | Meta |
| Revenue split | 70/30 | current | Meta |
| Worlds UGC effective take | ~47.5% | current | Analysis |
| Free-to-play share of time | ~70% | current | Industry stats |
| Oculus Publishing titles shipped | 140+ | 2025 | Meta |
| VR developer programme spend | ~$150M | 2025 | S. Ryan, Feb 2026 |
| Share of effective VR time in 3P apps | **86%** | Feb 2026 | S. Ryan |
| Developers prioritising Quest/Store | 63% | 2026 | Industry survey |
| Ignition: startups sponsored | 21 | 2024–25 | Meta |
| Ignition: continuing development | 90% | 2026 | Meta |

### Horizon Worlds
| Metric | Value | Period | Source |
|---|---|---|---|
| Mobile downloads | 45M | to Mar 2026 | Appfigures |
| 2026 downloads to date | ~1.5M (+53% YoY) | Mar 2026 | Appfigures |
| Lifetime consumer spend | **~$1.1M** | to Mar 2026 | Appfigures |
| Mobile MAU growth | >4× | 2025 | Meta |
| Mobile-only worlds | 0 → 2,000+ | 2025 | Meta |
| Creators past $1M lifetime | 4 | Feb 2026 | Meta |
| Creators earning 6 figures | ~100 | 2025 | Meta |
| Worlds removed from Quest Store | 31 Mar 2026 | 2026 | Meta |
| VR support removal | 15 Jun 2026, reversed 19 Mar 2026 | 2026 | Meta |

### Competition
| Metric | 2025 | 2026 |
|---|---|---|
| Prize pool | $1,500,000 | **$1,000,000** |
| Awards | 32 | 20 |
| Registered participants | 2,865 | **5,371** |
| Submissions | 650+ | unknown (est. 1,100–1,250) |
| Duration | 5 weeks (5 Nov–9 Dec) | 8 weeks (24 Sep–18 Nov) |
| Tracks | Entertainment, Lifestyle, Gaming (Casual + Social) | Entertainment, Gaming, Productivity |
| Top prize | $100,000 | $100,000 |
| Runner-up | $60,000 | $50,000 |
| Third tier | $30,000 honorable mention | none |
| Technical specials | 8 categories, $270k | none |
| Behavioural specials | 1 (Judges' Choice) | 6 × $25k |
| Headline hardware | Quest 3 / 3S | Quest 3 / 3S + VR Glasses (spring 2027) |
| Hands-first | Special award category | **Hard requirement** |

**Derived competition metrics**
| Metric | Value | Method |
|---|---|---|
| Registration growth 2025→2026 | **+87%** | (5,371 − 2,865) / 2,865 |
| Prize pool change | **−33%** | ($1.0M − $1.5M) / $1.5M |
| Expected submissions 2026 | ~1,100–1,250 | 22.7% conversion (650/2,865) applied to 5,371 |
| Implied prize per submission | ~$800–$900 | $1M ÷ 1,100–1,250 |
| Competition spend as % of dev-programme budget | **≈0.67%** | $1M ÷ $150M |
| Prize pool as Reality Labs burn time | **≈32 minutes** | $1M ÷ ($4.13B/91/24) |

### Device specifications
| Spec | Quest 3 | Quest 3S | Meta VR Glasses |
|---|---|---|---|
| FoV | ~110° × 96° | ~110° × 96° | **~70° × 66°** |
| Weight | ~515g | ~514g | ~100g + ~300g puck |
| Display | — | — | Micro-OLED "5K Infinite Display", ~2412×2288/eye, ~37 PPD |
| Chip | Snapdragon XR2 Gen 2 | Snapdragon XR2 Gen 2 | Snapdragon Reality Elite (XR2 Gen 3 per Meta dev pages — **sources conflict**) |
| RAM | 8GB | 8GB | 12GB |
| Storage | 512GB | 128/256GB | 128GB + microSD |
| Controllers | Touch Plus included | Touch Plus included | **None — eyes and hands only** |
| Price | $599.99 | $349.99–$449.99 | $1,299.99 |
| Availability | Now | Now | Spring 2027 |
| OS | Horizon OS | Horizon OS | Horizon OS |
| Store | Horizon Store | Horizon Store | Horizon Store (shared) |

## 10.2 Source register

**Tier 1 — Meta financial disclosures**
1. Meta Platforms, Inc., Q3 2026 results release and Form 10-Q (21 Oct 2026) — revenue $64.601B, RL revenue $443M, RL operating loss $4.129B, capex, FCF, headcount.
2. Meta Q3 2026 earnings call — Susan Li on Quest decline and AI-glasses growth; Zuckerberg on "major breakthroughs"; 2027 RL loss guidance ~$15B.
3. Meta Q4 2025 8-K (28 Jan 2026) — Q4 2025 RL loss −$6,(021)M.

**Tier 2 — Meta platform strategy**
4. Samantha Ryan (VP Content, Reality Labs), *Our Renewed Focus in 2026*, developers.meta.com, 19 Feb 2026 — the 86% third-party figure; $150M dev-programme spend; audience cohorts; "casual adult consumers… enjoy hand tracking"; Worlds separation.
5. *The State of VR at GDC 2026: Building a Sustainable Future*, developers.meta.com, 12 Mar 2026 — Store metrics, Horizon+, Publishing 140+ titles, Ignition outcomes, audience cohorts, "hand tracking is weird but magical", Worlds removal rationale.
6. *VR is Expanding. Start Building for Meta VR Glasses Today*, developers.meta.com, 24 Sep 2026 — v207, FoV figures, Store priority for controller-free, competition announcement.
7. Meta Newsroom, *The Biggest News from Connect 2026*, 23 Sep 2026 — Muse, VR Glasses, Ray-Ban Audio/Gen 3, IMAX Enhanced, sports rights.
8. Meta, *Introducing Meta VR Glasses* — specs, price, 70×66 FoV, spring 2027.
9. Meta, *Get started with Meta VR Glasses* (13 Oct 2026) — input model, audio-to-expressions, no camera face tracking.
10. Meta, *Hands: Limitations & Mitigations* (14 Sep 2026) — tracking volume, occlusion, lighting, movement speed.
11. Meta, *AI tooling overview* + Meta VR CLI docs — MCP server, agent skills, LLM docs.
12. Meta, *Meta Horizon Start developer program* — benefits and eligibility.

**Tier 3 — Competition primary documents**
13. Meta VR Start Developer Competition 2026, Devpost challenge 31024 — Overview, Official Rules (7 sections), Judging Criteria & Process, FAQs (4 sections), Special Awards, Resources, Updates, Webinars & Office Hours, Schedule.
14. Meta, *Meet the Meta Horizon Start Developer Competition Winners*, 23 Dec 2025 — full 2025 winner corpus.
15. 2025 competition page, Devpost — $1.5M, 2,865 participants, 5 Nov–9 Dec 2025, 300+ apps >$1M cited.

**Tier 4 — Independent trackers**
16. IDC — Quest shipments −42.3% (2025); ~1.7M units Q1–Q3 2025; 68.7% XR share Q2 2026; headsets at 15.4% of XR shipments Sep 2026; Vision Pro ~45,000 units 2025; 74.6% Meta share 2025.
17. Counterpoint Research — VR headset shipments −14% H1 2025, −44% Q4 2025, −17% Q1 2026, −18% Q2 2026; AR +136/138%; Meta share 54% Q2 2026 (from 72%); price-increase attribution.
18. Valve Steam Hardware Survey — Quest 3 26.25%, Quest 2 26.66%, Quest 3S 11.44% (Jul 2026); 1.77% of Steam users (Jan 2026).
19. Appfigures (via TechCrunch) — Horizon Worlds 45M downloads, ~$1.1M lifetime consumer spend, +53% 2026 downloads.
20. Treeview / Axis Intelligence — cumulative RL losses ≈$92.2B to 30 Jun 2026; Horizon+ ≈≤$20/subscriber/year.

**Tier 5 — Trade press (corroboration)**
21. UploadVR — 2025 competition launch; retention reporting; Half-Life-era developer commentary; Quest 3 retention improvement (GDC 2024); developer concerns (Feb 2025).
22. Road to VR — Horizon OS third-party pause (Dec 2025).
23. Skarred Ghost — Connect 2026 keynote analysis; "OS being made so that it is AI native"; 70+ hand-tracking games incl. Beat Saber Flow.
24. VR.org / Next Reality — Connect 2026 live blog; Counterpoint Q2 2026 detail; Steam Frame launch.
25. Android Central — "VR visibility crisis" GDC panel (Mar 2025).
26. eWeek — Horizon Create/Studio; mobile MAU 4×; 2,000+ mobile-only worlds.
27. Meta / TechCrunch — Quest price rises, 19 Apr 2026; Horizon Worlds mobile pivot, 20 Feb 2026; CTO reversal, 19 Mar 2026.

**Tier 6 — Aggregators (used only to locate T4)**
28. Various "statistics 2026" compilation pages. Not cited as authority. Where they disagree with T1–T4, T1–T4 win.

## 10.3 Known conflicts and how they were handled

| Conflict | Positions | Handling |
|---|---|---|
| **VR Glasses chipset** | Snapdragon Reality Elite (Meta newsroom, UploadVR) vs. XR2 Gen 3 (Meta developer pages) | Reported as unresolved. Immaterial to our strategy. |
| **VR headset decline rate, Q2 2026** | Counterpoint −18%; Axis-derived from IDC −23.1% | Different category definitions (Counterpoint = VR only; IDC = VR/MR). Both reported. |
| **Cumulative Reality Labs losses** | $73B "since 2021"; $83.6B; $92.2B "through Q2 2026" | Different start dates and bases. The $92.2B figure has the clearest stated method. |
| **Meta share of market** | 43%, 45.7%, 54%, 58%, 68.7%, 74.6% | Definitional: headsets-only vs. headsets+glasses; units vs. revenue; quarterly vs. annual. Reported with definitions attached. |
| **Quest installed base** | ">25M", "~20M sold by Mar 2023", "30M+ active" | Meta has not published since 2023. Treated as unknown; only the 6.37M MAU (Oct 2022) datapoint is used. |
| **Who said "10% MAU"** | Chet Faliszek (ex-Valve) on a podcast/tweet | Treated as **rumour**, not data. Not used. |

---

# CLOSING: THE ANSWER TO THE QUESTION WE STARTED WITH

We asked *why does this hackathon exist, and what problem is Meta facing?*

The answer, in five sentences:

> **Meta is running this competition because its next VR device has no controllers, its content organisation has been cut back, 86% of headset time already belongs to third parties, and it cannot afford to fund a launch catalogue properly while it redirects money to AI.**
> **The specific problem is not engagement — usage is at an all-time high — it is that hardware sales are falling, store revenue is flat, and the cohort Meta needs (media-first adults who won't use controllers) has not arrived yet.**
> **So Meta is buying, for roughly thirty-two minutes of Reality Labs' burn, about a thousand finished hands-first prototypes from people it does not employ, in order to discover which seated, controller-free loops actually retain people — and to have something to point at when a $1,299 pair of glasses launches in spring 2027.**
> **Every rule in the ruleset follows from that: hands-first because the device has no controllers; seated and ten-minute because the cohort is media-first; no wrappers because 45 million downloads produced $1.1 million; report repeat usage because retention is the platform's wound; use gaze because v207 just shipped and it is the only interaction model the glasses have.**
> **Which means the competition is not a talent show or a technology demo. It is a supplier-development programme with a prize attached — and the way to win it is to be the most legible possible answer to the question Meta is actually asking.**

**Next step:** the decision filter in Part VIII plus the four inputs in §9.1. Give us those, and the concept phase becomes a shortlist rather than a brainstorm.

---

*Research dossier prepared 7 November 2026. All financial figures are as reported by Meta Platforms, Inc. and independent trackers as of the dates cited. Inferences are labelled as such and are mine, not Meta's.*
