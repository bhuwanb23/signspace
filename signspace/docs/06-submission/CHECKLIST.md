# Submission Checklist & Form Map

**Owner:** W4 · **Phase:** P9 · **Gate:** G9 · **Built:** 14 Nov 2026 (P0.2) — field list verified from source, not memory
**Status:** 🟡 field map authoritative · Devpost-specific quirks still `[CONFIRM IN FORM]`

Two jobs: the **field map** (§1) and the **last-60-minutes runbook** (§4).

---

## 1. FORM MAP — what the submission actually requires

*Source: the "What to Submit" section of the competition page, read 14 Nov 2026.*

| # | Field | Required | Limit / format | Status |
|---|---|---|---|---|
| 1 | Submission name | ✅ | text | **SIGNSPACE** |
| 2 | Submission tagline | ✅ | **140 characters** | drafted — 118 chars |
| 3 | Track | ✅ | Entertainment / Gaming / **Productivity** | **Productivity** |
| 4 | Division | ✅ | New Experience / Adapted | **New Experience** |
| 5 | Text description | ✅ | *suggested* **≤500 words**: inspiration, how built, future plans | drafted — 494 words |
| 6 | Adapted-division summary | only if Adapted | — | **N/A** (we are New) |
| 7 | **How Hand Interactions are implemented + what excites you about the future of VR** | ⬜ **optional** | free text | **drafted — §2.5. Do not skip** |
| 8 | Target launch date | ✅ | date | **Q1 2027** |
| 9 | Team member(s) | if applicable | name, email, role | `[CONFIRM]` |
| 10 | Demonstration video URL | ✅ | **<3:00**, public on YouTube/Vimeo | not yet |
| 11 | Playable build | ✅ | APK on a channel named exactly **`Competition`** | not yet |
| 12 | **Invite URL** | ✅ | judges must be able to launch it | not yet |
| 13 | Built-with tags | `[CONFIRM IN FORM]` | Devpost tag widget | Unity, Meta XR SDK, MRUK, C# |
| 14 | Screenshots / gallery images | `[CONFIRM IN FORM]` | Devpost gallery | not yet |

### Hard requirements — pass/fail, not fields

- **Hands-first**: *"can someone complete the entire experience without ever pairing a controller?"* → yes; controllers are not implemented.
- **Policy compliance**: Meta VR Start Terms, Community Standards, Code of Conduct in VR, Developer App Policies, Developer Content Guidelines.
- **Airplane seat**: every interaction inside a two-foot radius.
- **One bus stop**: a complete satisfying moment in ≤10 minutes.
- **Take-it-away**: remove any third-party service and a Project still exists → we have none.
- **No changes after the Entry Period ends**: *"Judging is based on the state of the Entry at the submission deadline."*

---

## 2. PRE-FILLED CONTENT — paste-ready

### 2.1 Name
`SIGNSPACE`

### 2.2 Tagline — 118 / 140
```
Every app that teaches sign language is a video with extra steps. This one tells you which sign you accidentally made.
```

### 2.3 Track / Division
`Productivity` · `New Experience`

### 2.4 Description — 494 / 500
Full text in `SUBMISSION-COPY.md` §3. **Do not edit after the final count.**

### 2.5 The optional Hand Interactions field — 168 words, drafted

> Hands are the entire input model, and in SIGNSPACE they are also the subject — the app does not use your hand to move something, it uses your hand to tell you something about your hand.
>
> Three decisions define the implementation. **Grab is the core verb**: you pinch a life-size reference hand and turn it with your own wrist, the only way to see a handshape's depth. **Gaze carries what hands cannot**: both hands are busy holding and shaping, so looking at a finger is the only available way to inspect it — eye tracking drives the highlight, dwell confirms. **Forgiveness beats precision**: matching compares relative joint angles only, in three bands, with a 0.7 s hold, and the exercise pauses rather than failing when tracking confidence drops. MRUK anchors the vocabulary shelf to real furniture; passthrough is what makes that anchoring mean anything.
>
> What excites me about the future of VR is that eyes and hands are finally enough input for something serious. This needs no controller, no keyboard, no desk — and on VR Glasses it becomes something you could practise on during a commute.

**Why it matters:** it is optional, so most entries leave it empty, and it is the only place the form explicitly asks about the technology criterion. Free differentiation.

### 2.6 Target launch date
`Q1 2027`

### 2.7 Team
`[CONFIRM]` — prize money goes to one nominated representative either way.

---

## 3. BUILD ACCESS — the part judges fail on

| Step | Requirement | Verified |
|---|---|---|
| 1 | APK on a release channel **named exactly `Competition`** | ⬜ |
| 2 | **Invite URL** created for that channel | ⬜ |
| 3 | Invite URL opened in a **private window, signed out of Meta** | ⬜ |
| 4 | Flow completed to an install prompt on a **device that never had the app** | ⬜ |
| 5 | Project available and accessible **throughout judging** (18 Nov → 9 Dec) | ⬜ |

> The requirements page: *"Get the build access right — judges can't score what they can't launch."*

---

## 4. THE LAST 60 MINUTES — run in this order

| # | Check | Method | Pass |
|---|---|---|---|
| 1 | Tagline ≤140, description ≤500 | `python3 scripts/count.py` | ⬜ |
| 2 | Video <3:00, hands-first by 0:30 | `ffprobe` + one full watch | ⬜ |
| 3 | Video plays in an incognito window | YouTube/Vimeo public | ⬜ |
| 4 | Invite URL from a clean device | private window, signed out | ⬜ |
| 5 | Every field in §1 filled from the repo, not memory | this document | ⬜ |
| 6 | No claim the build can't demonstrate | read against `COMPETITION-FIT.md` | ⬜ |
| 7 | Submit | Devpost | ⬜ |
| 8 | Archive confirmation email + submitted-state screenshot | `phases/p9-submission/submitted.md` | ⬜ |
| 9 | **Touch nothing after submitting** | — | ⬜ |
