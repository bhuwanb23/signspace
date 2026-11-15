# COMPETITION FIT
## Mapping SIGNSPACE to the rubric, the three hard tests, and the named technologies

*Every row below is an assertion a judge could check against the build. If a row can't be checked on Day 11, it comes out of the description.*

---

## 1. THE FOUR CRITERIA (25% EACH)

### 1.1 Innovation & Creativity — 25%

| What they're scoring | Our evidence | Where it's visible |
|---|---|---|
| Novelty of concept | Handshape practice where **your own hand is the content**, not the controller | The whole video |
| Would it fall apart on another platform? | Faces it directly, in one sentence: *"a controller version is not a worse product, it is not a product"* | 0:25–0:35 |
| Creative use of the medium | Rotation-as-inspection; gaze where both hands are busy; **the near-miss** | 0:00–0:10, 0:25–0:50 |
| Doesn't feel like a thin wrapper | Nothing is ported. The content is 3D geometry of a moving hand | Implicit throughout |

**Weakest link in this criterion:** a judge who has seen VR sign-language demos before. **Answer:** `NOVELTY-AND-PRIOR-ART.md` §2 — we name the eight prior projects ourselves, then show what none of them did.

### 1.2 Experience Design — 25%

| What they're scoring | Our evidence | Where it's visible |
|---|---|---|
| First five minutes | Near-miss hook at 0:00; first lock at ~0:45; zero-text tutorial | Beat sheet, whole video |
| Interaction comfort | Seated, near-field, no locomotion, one-handed path | Accessibility segment |
| Feedback quality | Coarse tolerance bands, **never a failure state**, pause-not-penalise | 0:25–0:50 |
| Reason to return | The shelf accumulates on your real furniture; one empty slot at the end of session one | 0:50–1:10 |

**Weakest link:** it could read as an *education app* — which judges may treat as low-ambition. **Answer:** lead with the *medium* insight ("you cannot rotate a video"), not the learning outcome.

### 1.3 Technical Implementation — 25%

| Named technology | Implementation | Demonstrated by |
|---|---|---|
| **Hand tracking** | ISDK v207, 26-joint, weighted relative-angle matching | Whole build |
| **Gaze** | ISDK Eye Gaze Interaction building block on the finger-highlight layer | Gaze-highlight segment |
| **Passthrough** | Load-bearing: real hands on real table, shelf on real furniture | On/off comparison at ~1:00 |
| **Spatial anchoring** | MRUK surface detection + persistent shelf anchor | Relaunch demonstration |
| **FoV-aware** | ~55° safe cone; shelf reflows to tray at 70°×66° vs Quest 110°×96° | Split-screen at ~1:10 |
| **MRUK** | Surface detection, anchor, fallback plinth | Shelf segment |
| Performance | **≥72 fps on Quest 3S**, 15-minute thermal run, recorded overlay | 3 s of overlay footage |

**Weakest link:** hand tracking is famously fragile (occlusion, lighting, unusual anatomy). **Answer:** we built the fragility into the product (`N2` honest mirror, confidence-driven pause) and we show the number that survives.

### 1.4 Polish & Presentation — 25%

| What they're scoring | Our evidence |
|---|---|
| Build quality | 20 cold launches, zero crashes; no error states anywhere in the UX |
| Video craft | Real-time capture only; no fake UI; a hook in the first 4 seconds |
| Description quality | ≤500 words, every claim checkable, plus an explicit anti-claim |
| Attention to rules | Hands-first by 0:30, tagline ≤140 verified, APK on the `Competition` channel |

---

## 2. THE THREE HARD TESTS

| Test | Our position | Risk |
|---|---|---|
| **Hands-first** — never ship a controller pair | Controllers are not even implemented. The core gesture is impossible with them | **None.** Strongest possible answer |
| **Airplane seat** — 2 ft radius, ≤10 min | Seated, near-field, no locomotion, session designed at 10 min | Low. Watch the shelf: it must be reachable within the cone |
| **Take-it-away** — no thin wrapper | Content is 3D hand geometry; no 2D source exists | Low. But a judge *may* file it under "learning app" — answer with the medium argument |

---

## 3. AWARD TARGETING

*Verified against the official [special awards page](https://start-developer-competition-26.devpost.com/details/special-awards) — 14 Nov 2026. The names and definitions below are Meta's, not ours.*

| Award | Value | **Meta's own definition** | Our position |
|---|---|---|---|
| **Best New Productivity Experience** | **$70k / $35k** | *"Apps that make you more effective anywhere or are designed around specific daily moments … or habits tied to a recurring context"* | **Primary target.** A 10-minute daily handshape habit is the brief, verbatim |
| **Best Accessibility Forward Experience** | $25k | *"Pushing accessible XR design: voice alternatives for hand interactions, **eyes-only navigation for limited mobility**, high-contrast modes, inclusive by default"* | **Second target.** Eyes-only navigation is named in the definition and is P6.3. Anatomical calibration goes beyond it |
| **Best First Five Minutes** | $25k | *"…how fast it explains itself, teaches controller-free interaction without a wall of text, and delivers a payoff before the headset comes off"* | **Third target.** Zero-text onboarding, first lock <75 s |
| **Best Reason to Come Back** | $25k | *"…visible growth between sessions, a loop that pulls the user back tomorrow"* | **Fourth target.** The MRUK shelf *is* visible growth between sessions |
| **Boldest Original Concept** | $25k | *"Rewards the idea that couldn't exist anywhere else … whether the experience would fall apart on any other platform"* | **Fifth target.** This definition is our §IV.3 argument restated |
| Best Social & Multiplayer | $25k | *"co-watching, cooperative play, social spaces, asymmetric multiplayer"* | ❌ Not competing — no multiplayer, and faking it would cost days |
| Best Agentic Interaction | $25k | *"AI as a core part of the experience"* | ❌ Not competing — deliberately offline and deterministic (ADR-005) |
| Judge's Choice | $20k ×2 | Judges' discretion, "standout entries that don't fit neatly" | Possible, not planned |

**Four of the six named specials are plausibly winnable, and all four fire off the same design decisions.** That is why the near-miss, the shelf and the eyes-only path are on the never-cut list: they are the *same* work measured by four different awards.

**Confirmed prize structure** (20 awards, $1,000,000): each of the three tracks × two divisions has a winner and a runner-up — Gaming/Entertainment at $100k/$50k, **Productivity at $70k/$35k** — plus six $25k specials and two $20k Judge's Choice awards. One prize per entry, so the distribution matters more than the ceiling.

## 5. THE THREE SENTENCES A JUDGE MUST UNDERSTAND

1. **You cannot rotate a video** — that's why sign language learning is broken on 2D.
2. **This is the first app whose content is your own hand** — the tracking isn't a control scheme, it's the subject.
3. **It tells you which sign you accidentally made** — the feedback nobody else can give.

If a judge can repeat those three sentences after 3 minutes, the submission worked.
