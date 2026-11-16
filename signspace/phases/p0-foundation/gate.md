# GATE G0 — Does an empty build install from the `Competition` channel, and does the invite URL work for a stranger?

**Phase:** P0 · **Verdict:** 🟡 **not yet evaluated — 5 of 9 met, 2 partial, 2 open.** The two open boxes need the headset. Itemised in [`P0-AUDIT.md`](P0-AUDIT.md)

## Criteria — all must be true
- [x] Meta Horizon Start membership is active on the submitting account (screenshot archived)
- [x] Devpost draft exists; the real form field list is documented in `CHECKLIST.md`
- [~] App created ✅ (id `1444571625411296`) · channel **not yet named exactly `Competition`** · no upload yet
- [ ] A build of the empty scene installs and runs on the primary device — **headset arriving**
- [ ] Hand tracking and passthrough are visible in that build — **needs the same headset**
- [x] The Invite URL reaches an install prompt from a signed-out, non-developer account _(owner-confirmed 15 Nov — screenshot still wanted in `evidence/`)_
- [x] Every toolchain version is recorded; no `[VERIFY]` left in the tech stack table
- [x] Baseline commit exists; branch policy written
- [~] Signer brief **sent** 15 Nov; capture slot not yet agreed — the reply is the remaining half

### What is blocking each remaining box

| Box | Status | Who |
|---|---|---|
| App created · channel accepted an upload | ✅ 15 Nov | owner — `OWNER-TASKS.md` §C |
| Builds and runs on the headset | ⬜ **the headset, nothing else** | owner — `OWNER-TASKS.md` §D |
| Hands and passthrough visible | ⬜ same build | owner |
| Invite URL | ✅ 15 Nov | owner — `OWNER-TASKS.md` §G |
| Signer brief sent | 🟡 sent, awaiting reply | **owner** — `OWNER-TASKS.md` §E |

**Both open boxes are one artefact: an installed APK.** The moment the headset is in hand, G0 is a
single afternoon of `adb install` and two screenshots — no code has to change for it.

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
