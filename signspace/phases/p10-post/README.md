# P10 — Post-submission

**Day Wed 18 Nov → 11 Dec · Budget 4 h · Gate:** G10 — Is the submitted entry still intact, installable and unchanged three weeks later?
**Status:** 🟡 **Desk-side done; G10 NOT EVALUABLE.** `scripts/freeze_check.py` runs 19 rows: **9 PASS / 7 FAIL / 3 NOT RUN**. The APK (`a1587624…`) is byte-identical to the freeze record, its packaged `versionCode` is 14, the record names the commit that built it (`3e5333c`) and `v1.0-competition` points at that commit. Judging note, launch sketch, post-mortem and re-verification log written. **Done since:** `v1.0-competition` is tagged at `3e5333c` (local, not pushed). **Owed:** a submitted entry, the video, and three weekly rows.

## Objective
Protect the submission for three weeks, prepare the one-page argument judges will re-tell, and capture what the eleven days actually taught.

## Why it sits here
Judging runs for three weeks after submission. What happens in those weeks can still be lost — a changed channel, a broken invite link, a judge who asks a question nobody prepared for.

## Sub-phases

| # | Task | Est | Workstream | Status |
|---|---|---|---|---|
| [P10.1](tasks/p10.1-reverify.md) | Morning re-verification | 15 min | W5 | 🟡 desk row written, account rows owed |
| [P10.2](tasks/p10.2-judging-prep.md) | Judging-prep note: the three sentences and the two hard questions | 45 min | W4 | ✅ |
| [P10.3](tasks/p10.3-launch-sketch.md) | Store-launch sketch for Q1 2027 | 1 h | W4 | ✅ |
| [P10.4](tasks/p10.4-freeze-guard.md) | Guard the freeze | 5 min/week | W5 | 🟡 guard written, weekly rows owed |
| [P10.5](tasks/p10.5-postmortem.md) | Post-mortem | 1 h | W5 | 🟡 written, hour counts NOT RECORDED |

**Budget:** 4 h spread across three weeks.

## Inputs required before starting
- A submitted, verified entry (G9 green)

## Outputs — the phase is done when all of these are true
- [ ] A verified, unchanged entry at judging time — **no submitted entry exists to verify**
- [x] A judging note — `judging-note.md`
- [x] A launch sketch — `launch-sketch.md`
- [x] A post-mortem — `POSTMORTEM.md` (hours NOT RECORDED, stated in the document)

## Gate
[`gate.md`](gate.md) — Is the submitted entry still intact, installable and unchanged three weeks later?

## Traps specific to this phase
- Improving things
- Assuming the channel is stable because it was on Tuesday

## Related documents
- [`docs/00-plan/MASTER-PLAN.md`](../../docs/00-plan/MASTER-PLAN.md) §8
- [`phases/p9-submission/ARCHIVE.md`](../p9-submission/tasks/p9.8-archive.md)

---
*Part of the [master plan](../../docs/00-plan/MASTER-PLAN.md).*

## Where P10 actually stands (18 Nov 2026)

| Question | Answer | Evidence |
|---|---|---|
| Is the artifact unchanged? | **Yes** — byte-identical to the freeze record, and its packaged `versionCode` is 14 | `freeze_check.py` rows 1–6 |
| Is the entry intact and installable for a judge? | **No** — no submission ID, invite URL, channel screenshot or video master | `reverify-log.md` rows 1–4 |
| Was the freeze respected? | **Partly** — the tag exists at the producing commit and the record names it, but commits landed after the frozen-at timestamp | `reverify-log.md` rows 4–5 |
| Is the record truthful about the APK? | **Yes** — sha, code 14 and commit all match the artifact and the tag | `reverify-log.md` rows 4–5 |

The P10 artefacts are recent work on a frozen repository; that is itself the drift row 8 records, and it
is left visible rather than papered over.
