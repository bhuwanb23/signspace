# Phase 8 review

**Written:** 17 Nov 2026 · **Phase:** P8 · **Gate:** G8 · **Companion to:** `PLAYTEST-LOG.md`, `phases/p8-playtest/`

> P3's review (`phase-3-review.md`) set the pattern: every task against its evidence, the gate
> criterion by criterion, what is done and what is owed, and an honest "what we knowingly shipped
> broken" section. This is the same shape, because the honesty is the point.

---

## 1. What P8 is

P8 is the phase where the product meets a stranger. Its entire output is a number — time to first
lock, under 75 seconds, by someone who has never seen the app — and a friction log with at least ten
rows. Nothing in this phase is a feature. Everything here is measurement.

## 2. What is built

### 2.1 The instrumentation

| File | What it is | Verified |
|---|---|---|
| `Scripts/Core/PlaytestTimeline.cs` | Pure data structure. Records once-per-session events (first grab, first lock, first near-miss, stalls). A second lock cannot overwrite the first-lock time. `MeetsFirstLockTarget` is a predicate over a recorded number, not a value the class fills in. | 17/17 checks |
| `Scripts/Core/PlaytestProbe.cs` | MonoBehaviour. Records a session to CSV. Stall detection: 10 s at <2 cm/s with no input. Source is always "device", "editor", or "fixture" — never guessed. Writes to `phases/p8-playtest/` in the editor, `persistentDataPath` on device. | 3/3 checks |
| `scripts/playtest_report.py` | Turns CSVs into the G8 numbers. Exits 2 with no device session. `--gate` exits 1 unless the latest device run locked inside 75 s. `--fixture` writes a synthetic session, clearly marked. | contract verified |
| `Editor/PlaytestBuilder.cs` | Wires the probe into the scene with `matcher`, `referenceHand`, `settings`, `pathOverride`. | compiles |
| `Editor/P8Checks.cs` | 47 desk checks: timeline once-semantics, CSV round-trip, stall detection, source honesty, report script contract, wiring. | 47/47 |

### 2.2 The artefacts

| File | What it is | Status |
|---|---|---|
| `phases/p8-playtest/testers.md` | Method, consent, hardware checklist, session notes template | written |
| `phases/p8-playtest/session-kit.md` | The one-sentence brief, consent, recording checklist, three questions | written |
| `phases/p8-playtest/fixes.md` | Template for the top three, plus the known-broken section | written |
| `docs/05-build/PLAYTEST-LOG.md` | Friction log template, 10+ rows, known-broken section | template, data blank |

### 2.3 What is NOT built

- **No actual session data.** The CSVs do not exist. The friction log is empty. The numbers are all blank.
- **No `P8Checks` integration into `SceneBuilder`** — the probe is added by `PlaytestBuilder` as a separate step, not by the scene builder. This is deliberate: the probe is a debug tool, not a product feature.
- **No `session-kit.md` in p8** — it was referenced by the task text but did not exist. It does now.

## 3. The gate — G8

**Verdict: not yet evaluated.**

| Criterion | Status | Evidence |
|---|---|---|
| Three uncoached testers recorded | not run | needs strangers |
| Friction log has 10+ ranked rows | not run | needs sessions |
| Top three friction points fixed | not run | needs the log |
| First lock now <75 s with a fresh stranger | not run | needs a stranger |
| No accessibility or performance regression | not run | needs the fixes to test |
| The near-miss was experienced organically, not demonstrated | not run | needs a stranger |

**The gate cannot be evaluated without a headset.** The desk work is done. The remaining work is
measurement, and measurement needs a person in a room with a headset.

## 4. What the desk work buys

If the headset never arrives, the desk work is still worth something:

1. **The probe is verified.** P8Checks = 47/47. The timeline's once-semantics are tested. The CSV
   round-trips. The source field is honest. The report script refuses to produce a number from a
   fixture.
2. **The friction log has a home.** The template, the method, the three questions, the
   known-broken section — all written and ready to fill.
3. **The simulator can retire most of the risk.** The Meta XR Simulator (Quest 3 profile) can run the
   core loop: hands track, the matcher produces a live distance, the band moves, the near-miss
   fires, the reference hand rotates. That retires matcher/near-miss/gaze risk without hardware.
4. **The disclosure is specific.** PERFORMANCE.md already names the 3S gap. ACCESSIBILITY.md
   already has the non-claims section. TRACKABILITY.md already says the numbers come from a
   headset. The disclosure is written; only the numbers are missing.

## 5. What we knowingly shipped broken

| Issue | Why it is still broken | What we would do with a day more |
|---|---|---|
| No actual playtest data | no headset, no strangers | run the sessions |
| The friction log is empty | no sessions to log | run the sessions |
| The near-miss has never been experienced organically | no strangers | run the sessions |
| ReadConfidence() returns 1f | the SDK's confidence API is not wired | one line, needs the device |
| The App ID is not in the project | needs the dashboard number | five minutes |
| G7 has 6 device-owed criteria | no headset | run the 15-minute thermal run |

## 6. Change log

| Date | Change | By |
|---|---|---|
| 17 Nov 2026 | File created. Instrumentation verified (47/47). Artefacts written. Drift fixed. | agent |
