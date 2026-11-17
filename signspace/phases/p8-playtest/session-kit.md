# P8 — session kit

**Owner:** W1 · **Task:** P8.2 · **Phase:** P8 → gate G8 · **Written:** 17 Nov 2026
**Companion to:** [`testers.md`](testers.md) · [`docs/05-build/PLAYTEST-LOG.md`](../../docs/05-build/PLAYTEST-LOG.md)

> The one-sentence brief. The consent. The recording checklist. The three questions.
> Everything the tester needs, on one page, so the observer can watch the person instead of the script.

---

## 1. The one-sentence brief

> *"Put this on and see what you think."*

**That is the entire brief.** No explanation of what the app does. No mention of handshapes, sign
language, or the competition. The first action the tester takes is theirs, and the moment we
explain something is the moment we find out the design is unclear.

**If they ask "what do I do?"** — *"whatever you'd like to try."* Write it down. That question is a
friction row.

**If they ask twice** — stop the session, write the friction row, and continue. A question asked
twice is a design failure, not a tester failure.

## 2. The consent

Spoken before the headset goes on:

> *"I'm recording your hands, your voice, and what you see through the headset. The recording is for
> internal use only — improving the software. It will not be published without your further consent.
> You can stop at any time. Are you happy to proceed?"*

**Recording scope: hands, audio, passthrough view.** Not the face. Not the room beyond what the
passthrough shows.

## 3. The recording checklist

| # | Item | Status |
|---|---|---|
| 1 | Headset on, hands tracked, passthrough on | ⬜ |
| 2 | `PlaytestProbe` wired (`SIGNSPACE/P8 Build playtest rig`) | ⬜ |
| 3 | CSV header verified (`python scripts/playtest_report.py`) | ⬜ |
| 4 | Screen capture running (hands + passthrough + audio) | ⬜ |
| 5 | Consent signed | ⬜ |
| 6 | The brief written down (not improvised) | ⬜ |

## 4. The three post-session questions

Asked **after** the session, not during. In this order:

1. *"What did the app tell you when that happened?"* — tests whether the near-miss landed
2. *"What were you trying to do when you stopped?"* — tests where the friction was
3. *"Would you open this again tomorrow?"* — tests reason to come back

## 5. What to write down, and when

| Moment | What to record | Where |
|---|---|---|
| First hand seen | timestamp | `PlaytestProbe` does this automatically |
| First grab | timestamp + which hand | `PlaytestProbe` |
| First band change (warming → close) | timestamp | `PlaytestProbe` |
| First lock | timestamp — **this is the headline number** | `PlaytestProbe` |
| First near-miss | timestamp + what it said | `PlaytestProbe` |
| First accessibility switch | timestamp + which toggle | `PlaytestProbe` |
| Every stall > 10 s | timestamp + what they were doing | `PlaytestProbe` |
| Every moment they stop moving | timestamp + what they were looking at | your notes |
| Every unexpected action | what they tried, and what happened | your notes |
| Every thing they did not notice | what we expected them to see, and they didn't | your notes |

**The observer's job is to watch the person, not the clock.** `PlaytestProbe` handles the clock.

## 6. The post-session report

```bash
python scripts/playtest_report.py --gate
```

- Exits **2** if no device session exists (no false pass)
- Exits **1** on `--gate` unless the latest device run locked inside 75 s
- `--include-fixtures` includes synthetic sessions for comparison only

## 7. Change log

| Date | Change | By |
|---|---|---|
| 17 Nov 2026 | File created. One-sentence brief, consent, recording checklist, three questions. | agent |
