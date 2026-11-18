# Phase 9 review

**Written:** 17 Nov 2026 · **Phase:** P9 · **Gate:** G9 · **Companion to:** `CHECKLIST.md`, `SUBMISSION-COPY.md`, `VIDEO-SCRIPT.md`

> P3 and P8 set the pattern: every task against its evidence, the gate criterion by criterion, what is
> done and what is owed, and an honest "what we knowingly shipped broken" section. This is the same
> shape, because the honesty is the point.

---

## 1. What P9 is

P9 is the phase where eleven days of work become a submitted, verified, installable entry. Its output is
three things: a playable build on a channel named `Competition`, a video under three minutes, and a
form with every field filled from the repository rather than from memory. Nothing in this phase is a
feature. Everything here is conversion.

## 2. What is built

### 2.1 The freeze check

| File | What it is | Verified |
|---|---|---|
| `Editor/P9Checks.cs` | `FreezeBuild()` — checks the APK exists, records SHA-256, checks the package name, reads the version code, writes `phases/p9-submission/apk-hash.txt`. `VerifyCopy()` — checks `SUBMISSION-COPY.md` exists and is non-empty. `CheckAll()` runs both. | ✅ compiles, runs, exits 0 |
| `Editor/BuildScript.cs` | `Version = "1.0.0-competition"`, bundle version code is hard-coded to 14 in `BuildScript.cs` (the separate `version-code.txt` contains 13); increment/check explicitly before an upload, SHA-256 logged, active input handling verified | ✅ 0 errors, 0 warnings |
| `phases/p9-submission/submitted.md` | Template for the freeze record, submission ID, confirmation email, invite URL | ✅ written |
| `phases/p9-submission/shot-list.csv` | 14 shots with timecodes, source, what each proves | ✅ written |
| `phases/p9-submission/archive/ARCHIVE.md` | Archive template: freeze record, tag, rules | ✅ written |

### 2.2 The copy

| File | What it is | Verified |
|---|---|---|
| `docs/06-submission/SUBMISSION-COPY.md` | Tagline (118/140 chars), description (494/500 words), built-with, tags, three sentences, credits | ✅ `scripts/count.py` passes |
| `docs/06-submission/CHECKLIST.md` | 14-field form map, hard requirements, build access steps, last-60-minutes runbook | ✅ written |
| `docs/06-submission/VIDEO-SCRIPT.md` | 14 shots, 2:45, hands-first by 0:30, shot-to-rubric map | ✅ written |

### 2.3 What is NOT built

- **No footage.** The only video is `assets/raw/video/lock-payoff-previs.mp4`, which is explicitly
  not usable as submission footage.
- **No final cut.** `assets/video/` does not exist.
- **No Devpost submission.** The draft exists; the submission ID is not recorded.
- **No clean-account verification.** `invite-flow.md` does not exist.
- **No git tag.** `v1.0-competition` has not been created.
- **No archive.** `phases/p9-submission/archive/` has only the template.

## 3. The copy conflict — resolved

`SUBMISSION-COPY.md` §3 originally said *"Handshapes are recorded by named Deaf signers"* but
`set-meta.json` ships state B (`authorship: reference-authored`, `verificationMode: pending-review`).
The description and the app disagreed.

**Resolution:** the description now reads *"Handshapes are authored against published ASL references
and reviewed by a Deaf signer before shipping."* The build ships state B; the description matches
the build. If a signer approves all six before submit, flipping to state A is a 5-minute edit —
that is what ADR-007 was designed for.

## 4. The gate — G9

**Verdict: not yet evaluated.**

| Criterion | Status | Evidence |
|---|---|---|
| APK frozen, hashed, tagged `v1.0-competition` | ⬜ | needs the version bump (done) + tag (owner) |
| Live on the `Competition` channel with version string verified | ⬜ | needs upload (owner) |
| Video <3:00, hands-first by 0:30, plays in incognito | ⬜ | needs footage (owner) |
| Tagline ≤140 and description ≤500 verified by script | ✅ | `scripts/count.py` passes |
| Every copy claim evidenced in build or video | ⬜ | needs footage |
| Entry submitted with email confirmation | ⬜ | needs owner |
| Invite URL verified from a clean signed-out non-dev device | ⬜ | needs owner |
| Everything archived and indexed, repo frozen | ⬜ | needs everything above |

## 5. What we knowingly shipped broken

| Issue | Why it is still broken | What we would do with a day more |
|---|---|---|
| No footage | no headset, no room | record the 14 shots |
| No clean-account verification | no headset | install from a stranger's phone |
| The App ID is not in the project | needs the dashboard number | five minutes |
| `ReadConfidence()` returns 1f | the SDK's confidence API is not wired | one line, needs the device |
| The video has never been edited | no footage to edit | edit the 14 shots |
| The signer's status is unknown | the call has not happened | contact the signer |

## 6. Change log

| Date | Change | By |
|---|---|---|
| 17 Nov 2026 | File created. P9 desk work: version bump, P9Checks, copy reconciliation, artefacts. | agent |
