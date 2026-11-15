# P0 AUDIT — every open item, and what closes it

**Written:** 15 Nov 2026 (Day 1) · **Purpose:** answer one question — *what is actually left before G0 can go green?*
**Companions:** [`gate.md`](gate.md) (the 9 criteria) · [`OWNER-TASKS.md`](OWNER-TASKS.md) (the steps) · [`README.md`](README.md) (the phase)

> **Why this file exists.** The user reports the licensing work finished end to end. That clears the two
> items that were blocking *software*, so the P0 list is now short enough to itemise rather than
> describe. Each row below is one binary check. Nothing here is "in progress" — it is done or it is not,
> and where it is not done the row says exactly what piece of evidence turns it green.

---

## 1. What the licence/EULA completion actually unblocked

| Was blocked by | Now | What it enables |
|---|---|---|
| **Asset Store EULA** (gates the Meta XR package download) | ✅ done 15 Nov — owner-confirmed | `Packages/manifest.json` can resolve `com.meta.xr.sdk.all` 207.0 and the SDK imports cleanly |
| **Unity Personal licence** (`unity.exe -batchmode` refuses without it) | ✅ done 15 Nov — owner-confirmed | Every command-line build, `scripts/build_apk.py`, and the editor automation in `Editor/BuildScript.cs` |
| **Licence file on disk** (`C:\ProgramData\Unity\`) | 🟡 inferred, not verified | The first `-batchmode` run prints the licence line; that log is the proof |

**Consequence, stated plainly:** P0.4's *software* half is finished. Its *hardware* half is not. There is no
longer any licence-shaped item between us and an APK.

---

## 2. The open list — 6 items, down from 11

The owner closed five rows on 15 Nov: the channel upload, the invite URL, the Devpost draft, the Meta
dashboard work, and the EULA/licence pair. What is left is **one artefact and three strings.**

### 2a. The one artefact — an installed APK (3 gate boxes, 1 P1 task)

| # | Item | Closes when | Owner needs |
|---|---|---|---|
| 1 | Empty-scene APK **installs and runs** on the primary device | `adb install` succeeds, app opens with hands + passthrough visible | the headset |
| 2 | **Hands and passthrough visible** in that build | a 10 s capture from the headset | the headset |
| 3 | **72 fps** on real silicon | OVR Metrics Tool trace, empty scene, hands + passthrough on | the headset |
| 4 | **P1.5 twenty-shape trackability probe**, daylight *and* lamplight | results pasted into `TRACKABILITY.md` | the headset *(this one is P1, not P0 — listed because it shares the blocker and the Mon 16 Nov deadline)* |

> **All four are the same blocker and the same afternoon.** No code has to change for any of them.

### 2b. The three strings only the owner can read off a screen

| # | Item | Where it is | Why it matters |
|---|---|---|---|
| 5 | **The exact channel name** as the dashboard spells it | Distribution → Channels | if it reads `competition`, the deliverable asks for `Competition` — rename while it is still free |
| 6 | **The package name the dashboard reports** | Distribution → Builds → Build Details | must equal `unity/Assets/_Project/package-name.txt` = `com.signspace.signspace`. **Now readable, because an upload exists.** This is the last moment a mismatch is fixable |
| 7 | **Devpost submission ID** + the six live-form answers | the draft entry | the ID goes in `accounts.md` §5; the answers go in `CHECKLIST.md` §2 |

### 2c. One reply and two signatures

| # | Item | Closes when | Owner needs |
|---|---|---|---|
| 8 | **Signer capture slot** | a date and time in the reply | the signer's calendar — *brief already sent 15 Nov* |
| 9 | **DoD + risk register signed**; `devices.md` §1/§3/§4 ticked | `SIGN-OFF.md` signed, matrix filled | 5 minutes |

---

## 3. What this changes about the plan

| Was | Now |
|---|---|
| P0 was the long pole, 5 of 9 boxes open | **6 of 9 met.** The 2 open boxes are one installed build |
| The licence was the risk | irrelevant — cleared |
| The signer could end the project and had not been contacted | contacted; the residual risk is now *a reply in time*, not *a stranger saying yes* |
| Invite URL unproven | proven — the entry reaches a stranger |

**The project's critical path is now: headset arrives → install → P1.5 probe.** Nothing else in P0 is
on it. That is why P1 can be worked in parallel rather than after P0 — which is exactly what the plan
does next.

---

## 4. Three things to send when convenient

Not blocking anything, but each closes a row permanently:

1. **Which day the headset lands** → rows 1–4, and it sets the `devices.md` dates.
2. **What the channel and package name actually read on the dashboard** → rows 5–6.
3. **The Devpost submission ID** → row 7 (the six form answers can follow).

---

## 5. Change log

| Date | Change |
|---|---|
| 15 Nov 2026 | File created. Licence + EULA marked complete on owner confirmation; P0 reduced to 11 binary items with 4 device-blocked, 5 browser/inbox-blocked, 2 signature-blocked. G0 re-scored: **4 of 9 met, 1 partial** — the earlier "5 of 9" in the README was an overcount that treated app creation and the channel upload as one criterion. |
| 15 Nov 2026 (later) | Owner confirmed the dashboard work (channel + upload), the Devpost draft and the invite URL as done, a headset is **arriving**, and the signer brief was **sent**. Open list cut from 11 to **6**: one installed APK (3 boxes + the P1.5 probe), three strings, one reply, two signatures. **G0: 6 of 9 met, 1 partial, 2 open.** |
