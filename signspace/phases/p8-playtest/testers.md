# P8 — testers

**Owner:** W1 · **Task:** P8.1 · **Phase:** P8 → gate G8 · **Written:** 17 Nov 2026

> Three strangers. Three different hands. Nobody briefed. Nobody helped.
> A path that can select but cannot lock is not a path.

---

## 1. The method

| Rule | Why |
|---|---|
| **Uncoached** | the moment we explain something is the moment we find out the design is unclear. We do not explain |
| **Recorded** | we watch it again afterwards, and so does the tester |
| **At least three different hand sizes** | a shape app that only works on one hand is not a shape app |
| **One tester with limited dexterity, if we can find one** | `ACCESSIBILITY.md` path 4 |
| **Their own room, their own light** | not our desk under a lamp |
| **We say nothing unless they ask a question twice** | and if they ask twice, we write it down as a friction row |

**We write down every moment a tester stops moving.** Not the moments we expected — the moments they
actually hesitated. `PlaytestProbe` does this mechanically so the observer can watch the person
instead of the clock.

## 2. The three post-session questions

Asked **after** the session, not during. In this order:

1. *"What did the app tell you when that happened?"* — tests whether the near-miss landed
2. *"What were you trying to do when you stopped?"* — tests where the friction was
3. *"Would you open this again tomorrow?"* — tests reason to come back

## 3. Tester profiles

**Filled on the day.** Initials only — never a full name in the repo.

| # | Initials | Hand size | Dominant hand | Dexterity notes | Lighting | Room | Own headset? |
|---|---|---|---|---|---|---|---|
| 1 | | | | | | | |
| 2 | | | | | | | |
| 3 | | | | | | | |

## 4. Consent

Every tester signs a one-line consent before the session:

> *"I consent to being recorded (hands, audio, and passthrough view) for the purpose of improving this
> software. I understand the recording is for internal use only and will not be published without my
> further consent. I can stop at any time."*

**Recording scope: hands, audio, passthrough view.** Not the face. Not the room beyond what the
passthrough shows. The consent is the same as `CAPTURE-PROTOCOL.md` §5 — ask for nothing the app does
not use.

## 5. The hardware checklist

| # | Item | Status |
|---|---|---|
| 1 | Headset on, hands tracked, passthrough on | ⬜ |
| 2 | `PlaytestProbe` wired (path = `phases/p8-playtest/`) | ⬜ — `SIGNSPACE/P8 Build playtest rig` |
| 3 | Recording setup (screen capture + audio) | ⬜ |
| 4 | `PlaytestProbe` CSV header verified (`python scripts/playtest_report.py`) | ⬜ |
| 5 | Consent forms signed | ⬜ |
| 6 | The one-sentence brief written down (not improvised) | ⬜ — see `session-kit.md` |

## 6. Session notes template

**Per tester. Verbatim quotes, especially the wrong words.**

### Tester 1
- **What they did first:**
- **What they said:**
- **Where they stopped:**
- **What they tried that we did not intend:**
- **What they did not notice:**

### Tester 2
- **What they did first:**
- **What they said:**
- **Where they stopped:**
- **What they tried that we did not intend:**
- **What they did not notice:**

### Tester 3
- **What they did first:**
- **What they said:**
- **Where they stopped:**
- **What they tried that we did not intend:**
- **What they did not notice:**

## 7. Change log

| Date | Change | By |
|---|---|---|
| 17 Nov 2026 | File created. Method, consent, hardware checklist, session notes template. | agent |
