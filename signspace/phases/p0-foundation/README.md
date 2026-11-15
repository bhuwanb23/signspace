# P0 — Foundation & compliance

**Day 1 (Sat 7 Nov) · Budget 5 h · Gate:** G0 — Does an empty build install from the `Competition` channel, and does the invite URL work for a stranger?
**Status:** 🔄 In progress — P0.1 ✅ · P0.2 🟡 prepared (needs 20 min of your time) · next P0.3

## Objective
Get from "nothing" to "a stranger can install our build and open our entry form". No product work happens in this phase, and no product work may start until this phase's gate is green.

## Why it sits here
Every item here is unglamorous and every one is fatal if missing. An APK that cannot reach a judge is indistinguishable from no APK at all.

## Sub-phases

| # | Task | Est | Workstream | Status |
|---|---|---|---|---|
| [P0.1](tasks/p0.1-verify-start-badge.md) | Verify Meta Horizon Start membership and eligibility | 20 min | W5 | ✅ |
| [P0.2](tasks/p0.2-devpost-draft.md) | Create the Devpost draft entry and save the submission ID | 20 min | W4 | 🟡 |
| [P0.3](tasks/p0.3-app-and-channel.md) | Create the Horizon OS app and the `Competition` release channel | 45 min | W5 | ⬜ |
| [P0.4](tasks/p0.4-toolchain.md) | Install the toolchain and get an empty scene onto the headset | 2 h | W2 | ⬜ |
| [P0.5](tasks/p0.5-invite-url.md) | Verify the Invite URL from a clean, signed-out, non-developer account | 30 min | W5 | ⬜ |
| [P0.6](tasks/p0.6-repo-hygiene.md) | Repo hygiene, baseline commit, and branch policy | 30 min | W5 | ⬜ |
| [P0.7](tasks/p0.7-device-matrix.md) | Lock the device matrix and the test rig | 20 min | W5 | ⬜ |
| [P0.8](tasks/p0.8-risks-and-dod.md) | Sign off the risk register and the Definition of Done | 30 min | W5 | ⬜ |
| [P0.9](tasks/p0.9-signer-pipeline.md) | Open the signer pipeline today (kill-condition lead time) | 60 min | W3 | ⬜ |

**Budget:** 5 h of the 82 h total. If this overruns by more than 2 h, that time comes out of P7 polish, not out of P1.

## Inputs required before starting
- A Meta developer account with Start membership
- The competition's Devpost page
- A Quest 3 and/or Quest 3S
- ~2 h of uninterrupted install time

## Outputs — the phase is done when all of these are true
- [ ] A build on the primary device, installed from the `Competition` channel
- [ ] A documented, screenshot-backed invite flow
- [ ] A verified toolchain with every version pinned
- [ ] A signer brief in three inboxes

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
