# GATE G0 — Does an empty build install from the `Competition` channel, and does the invite URL work for a stranger?

**Phase:** P0 · **Verdict:** ⬜ not yet evaluated — **4 of 9 criteria met, 5 blocked on the owner**

## Criteria — all must be true
- [x] Meta Horizon Start membership is active on the submitting account (screenshot archived)
- [x] Devpost draft exists; the real form field list is documented in `CHECKLIST.md`
- [ ] App created; release channel named exactly `Competition` has accepted an upload
- [ ] A build of the empty scene installs and runs on the primary device (Quest 3S)
- [ ] Hand tracking and passthrough are visible in that build
- [ ] The Invite URL reaches an install prompt from a signed-out, non-developer account
- [x] Every toolchain version is recorded; no `[VERIFY]` left in the tech stack table
- [x] Baseline commit exists; branch policy written
- [ ] Signer brief sent to ≥3 contacts, with a Day-5 capture slot in the calendar

### What is blocking each remaining box

| Box | Blocked on | Who |
|---|---|---|
| App created · `Competition` channel | the Meta developer dashboard | **owner** — `OWNER-TASKS.md` §C |
| Builds and runs on the headset | **a Unity licence** (batch mode refuses without one) and **the headset** | **owner** — `OWNER-TASKS.md` §B and §D |
| Hands and passthrough visible | the same build | owner |
| Invite URL | the build, plus a clean signed-out device | owner — `OWNER-TASKS.md` §G |
| Signer brief to ≥3 contacts | the owner's contacts and calendar | **owner** — `OWNER-TASKS.md` §E |

### Everything that does NOT need the owner

Done while the toolchain downloaded, because none of it needed a headset:

- Unity **6000.3.28f1** (6.3 LTS) installed on E: with Android Build Support, SDK/NDK and OpenJDK.
- Meta XR All-in-One **v207.0.0** pinned and resolvable from the public registry — **no Asset Store
  EULA needed**, which took a blocker off the critical path.
- Meta XR Simulator **207.1**, Meta XR Operator **207.0.1** and the OVR Metrics Tool installed.
- The six handshapes **authored and validated** (B, U, V, W, C, O) plus `index.json`.
- The match metric **redesigned and measured offline**, because the v1 metric could not see finger
  spread and the near-miss could therefore never fire for U-V. See
  [`../p2-core-loop/matcher-tuning.md`](../p2-core-loop/matcher-tuning.md).
- `SignSetLoader` rewritten — the old one read StreamingAssets with `File.ReadAllText`, which always
  fails on Android and would have loaded zero shapes on the headset.
- Scene assembly and the APK build turned into two commands.

## If red
**Stop everything.** Nothing downstream has value if an APK cannot reach a judge. Fix the plumbing, then re-run the gate. If the signer pipeline has had no response by end of Day 2, open the fallback-concepts decision the same night (see `RISKS.md` R1).

## Decision record

| Field | Value |
|---|---|
| Verdict | ⬜ PASS / ⬜ FAIL |
| Date evaluated | |
| Evaluated by | |
| Evidence (screenshot / log / recording) | |
| Notes | |
