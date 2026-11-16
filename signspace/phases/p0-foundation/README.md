# P0 — Foundation & compliance

**Day 1 (Sat 7 Nov) · Budget 5 h · Gate:** G0 — Does an empty build install from the `Competition` channel, and does the invite URL work for a stranger?
**Status:** 🔄 In progress — **5 of 9 gate criteria met · 2 partial · 2 open, both open boxes waiting on the headset** · P0.1 ✅ · P0.6 ✅ · P0.7 🟡 (devices.md written, no headset in hand) · P0.8 🟡 (DoD written, unsigned) · **toolchain installed on E:, SDK v207.0.0 pinned, six shapes authored, metric redesigned and measured** · **app id 1444571625411296 · package `com.signspace.signspace` · channels created** · **licence + EULA cleared 15 Nov** · **devpost draft, app + channel + upload, invite URL all confirmed 15 Nov** · blocked on **the headset arriving** (2 gate boxes) and **a signer reply** · open items itemised in [`P0-AUDIT.md`](P0-AUDIT.md)

## Objective
Get from "nothing" to "a stranger can install our build and open our entry form". No product work happens in this phase, and no product work may start until this phase's gate is green.

## Why it sits here
Every item here is unglamorous and every one is fatal if missing. An APK that cannot reach a judge is indistinguishable from no APK at all.

## Sub-phases

| # | Task | Est | Workstream | Status |
|---|---|---|---|---|
| [P0.1](tasks/p0.1-verify-start-badge.md) | Verify Meta Horizon Start membership and eligibility | 20 min | W5 | ✅ |
| [P0.2](tasks/p0.2-devpost-draft.md) | Create the Devpost draft entry and save the submission ID | 20 min | W4 | ✅ — draft exists; **submission ID + 6 form answers still to record** |
| [P0.3](tasks/p0.3-app-and-channel.md) | Create the Horizon OS app and the `Competition` release channel | 45 min | W5 | ✅ — app, channels and a first upload done 15 Nov |
| [P0.4](tasks/p0.4-toolchain.md) | Install the toolchain and get an empty scene onto the headset | 2 h | W2 | 🟡 — toolchain ✅ · licence + EULA ✅ 15 Nov · **build waits only on the headset** |
| [P0.5](tasks/p0.5-invite-url.md) | Verify the Invite URL from a clean, signed-out, non-developer account | 30 min | W5 | ✅ — verified 15 Nov |
| [P0.6](tasks/p0.6-repo-hygiene.md) | Repo hygiene, baseline commit, and branch policy | 30 min | W5 | ✅ |
| [P0.7](tasks/p0.7-device-matrix.md) | Lock the device matrix and the test rig | 20 min | W5 | 🟡 — headset **arriving**; day to pin, room/lighting rows still ⬜ |
| [P0.8](tasks/p0.8-risks-and-dod.md) | Sign off the risk register and the Definition of Done | 30 min | W5 | 🟡 — DoD written, needs a signature |
| [P0.9](tasks/p0.9-signer-pipeline.md) | Open the signer pipeline today (kill-condition lead time) | 60 min | W3 | 🟡 — **sent 15 Nov, awaiting a reply**; still the only thing that can end this project |

**The owner's list is one file:** [`OWNER-TASKS.md`](OWNER-TASKS.md) — every task that needs an
account, a headset, or another person's calendar, with the URLs and the exact steps.

**Budget:** 5 h of the 82 h total. If this overruns by more than 2 h, that time comes out of P7 polish, not out of P1.

## Inputs required before starting
- A Meta developer account with Start membership
- The competition's Devpost page
- A Quest 3 and/or Quest 3S
- ~2 h of uninterrupted install time

## Outputs — the phase is done when all of these are true
- [~] A build on the primary device, installed from the competition channel — **an upload exists; an install does not yet**
- [x] A documented, screenshot-backed invite flow *(path verified; screenshot to file)*
- [x] A verified toolchain with every version pinned — `ProjectVersion.txt` = 6000.3.28f1, `manifest.json` = Meta XR 207.0.0, pins listed in [`toolchain.md`](toolchain.md) §5; the last `[VERIFY]` in `TECH-STACK.md` was cleared 16 Nov
- [x] A signer brief in three inboxes *(sent 15 Nov, no reply yet)*

## Working artefacts

| File | Task | Purpose |
|---|---|---|
| [`toolchain.md`](toolchain.md) | P0.3, P0.4 | Package-name convention, exact Player Settings, the five-minute smoke test, version record |
| [`devpost-draft.md`](devpost-draft.md) | P0.2 | Paste pack + the six things only the live form can answer |
| [`accounts.md`](accounts.md) | P0.1, P0.2, P0.3 | Accounts, eligibility self-check, evidence log, linkage trap, application escalation clock |
| [`evidence/`](evidence/README.md) | all | Screenshot proof, one file per gate claim, naming + redaction rules |

## Gate
[`gate.md`](gate.md) — Does an empty build install from the `Competition` channel, and does the invite URL work for a stranger?

## Traps specific to this phase
- Spending Day 1 on the concept instead of the plumbing — the concept is already decided
- Discovering the invite flow only works for developers on Day 10
- Leaving the signer pipeline until P5 because that's where it's filed

## Related documents
- [`docs/01-idea/CONCEPT.md`](../../docs/01-idea/CONCEPT.md)
- [`docs/02-tech/TECH-STACK.md`](../../docs/02-tech/TECH-STACK.md)
- [`docs/05-build/RISKS.md`](../../docs/05-build/RISKS.md)

---
*Part of the [master plan](../../docs/00-plan/MASTER-PLAN.md).*
