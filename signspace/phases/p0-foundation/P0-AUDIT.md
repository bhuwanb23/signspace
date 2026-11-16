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
| **Asset Store EULA** (gates the Meta XR package download) | ✅ done 15 Nov — owner-confirmed | `Packages/manifest.json` resolves `com.meta.xr.sdk.all` 207.0.0 from the **public** registry — the EULA turned out not to be needed for the SDK at all |
| **Unity Personal licence** (`unity.exe -batchmode` refuses without it) | ✅ done 15 Nov — owner-confirmed, **now verified**: `unity license` reports `Unity Personal / Assigned` | Every command-line build, `scripts/setup_unity_project.ps1`, and the editor automation in `Editor/BuildScript.cs` |
| **Licence file on disk** | ✅ **verified** — it lives at `C:\Users\Bhuwan\AppData\Local\Unity\licenses\UnityEntitlementLicense.xml`, **not** `C:\ProgramData\Unity\` | The first `-batchmode` run prints the licence line; that log is the proof, and it has been captured |

**Consequence, stated plainly:** P0.4's *software* half is finished, and so is the build itself.
There is no longer any licence-shaped item between us and an APK.

### 1a. What the licence actually let us finish (15 Nov, later)

The project did not exist. There was no `ProjectSettings`, no `Packages/manifest.json`, no
`Library` — **nothing had ever been compiled**. Now:

| | |
|---|---|
| Unity project | ✅ created, `ProjectSettings/` committed |
| Packages resolved | ✅ `com.meta.xr.sdk.all` 207.0.0 · `com.meta.xr.sdk.core` 207.0.0 · `com.meta.xr.mrutilitykit` 207.0.0 · `com.meta.xr.sdk.interaction` 207.0.0 · `com.unity.xr.openxr` 1.18.0 · `com.unity.xr.management` 4.6.1 · `com.unity.render-pipelines.universal` 17.3.0 · `com.unity.inputsystem` 1.20.1 (transitive) |
| Compiles | ✅ **0 errors, 0 warnings** |
| Render pipeline | ✅ URP assets created and assigned to GraphicsSettings + all 6 quality levels |
| XR loader | ✅ OpenXR assigned for Android, Meta XR feature set enabled |
| Input handling | ✅ Both (the Meta SDK needs the new input system) |
| Passthrough | ✅ enabled, and `horizonos.permission.HEADSET_CAMERA` verified in the built manifest |
| Scene | ✅ `Assets/_Project/Scenes/Main.unity`, built from the Meta XR camera rig by script |
| **APK** | ✅ **62 MB**, `com.signspace.signspace`, versionCode 7, minSdk 32, targetSdk 34, ARM64, label `SIGNSPACE` |
| **Installed on a headset** | ⬜ **still not done** — needs the headset |

**Four things had no public API in Unity 6 and are handled explicitly**, because each one produces a
build that succeeds and is wrong:

| Thing | What happens without it |
|---|---|
| The render pipeline | magenta on the headset |
| The XR loader | installs, launches, **black screen** |
| The active input handling | nothing responds to a pinch |
| The passthrough camera-access flag | installs, launches, **no room** |

---

## 2. The open list — 6 items, down from 11

The owner closed five rows on 15 Nov: the channel upload, the invite URL, the Devpost draft, the Meta
dashboard work, and the EULA/licence pair. What is left is **one artefact and three strings.**

### 2a. The one artefact — an installed APK (3 gate boxes, 1 P1 task)

**The APK now exists.** `unity/Builds/Android/signspace.apk`, 62 MB, built from a script and
reproducible with one command. What it has never done is **run on a headset**.

| # | Item | Closes when | Owner needs |
|---|---|---|---|
| 1 | **Install the built APK on the headset** and launch it | `adb install` succeeds, the app opens | the headset |
| 2 | **Hands and passthrough visible** in that build | a 10 s capture from the headset | the headset |
| 3 | **72 fps** on real silicon | OVR Metrics Tool trace, hands + passthrough on | the headset |
| 4 | **P1.5 twenty-shape trackability probe**, daylight *and* lamplight | results pasted into `TRACKABILITY.md` | the headset *(this one is P1, not P0 — listed because it shares the blocker and the Mon 16 Nov deadline)* |

> **All four are the same blocker and the same afternoon.** No code has to change for any of them —
> the build is ready and waiting. What is left is hardware, and one `adb install` away from being
> evidence instead of a hope.
>
> **One thing to check first on the headset:** `OVRManager → Hand Skeleton Version` must be
> **Default (OpenXR)**. The legacy OVR skeleton has no metacarpals on index/middle/ring — exactly the
> joints that carry finger spread, which is what distinguishes U from V. On the wrong skeleton the
> shipped poses lose the joint carrying their most important signal, and it presents as *"the sensor
> cannot tell U from V"*. It is a configuration bug, not a sensor result.

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
| P0 was the long pole, 5 of 9 boxes open | **6 of 9 met, 1 partial** — the 2 open boxes are one installed build |
| The licence was the risk | irrelevant — cleared, and **verified** |
| The signer could end the project and had not been contacted | contacted; the residual risk is now *a reply in time*, not *a stranger saying yes* |
| Invite URL unproven | proven — the entry reaches a stranger |
| "The build is a thing we might get to" | **the build exists**: 62 MB, 0 errors, 0 warnings, reproducible with one command. It has never been on a headset |

**The project's critical path is now: headset arrives → `adb install` → P1.5 probe.** Nothing else in
P0 is on it. That is why P1 can be worked in parallel rather than after P0 — which is exactly what
the plan does next.

### 3a. One string still missing, and it is not a blocker

The **App ID** (`1444571625411296`, already on the dashboard) is not in the project yet.

**It is not needed for the competition deliverable.** The APK installs and runs on a headset from a
release channel without it. It is needed for **Meta platform services** — entitlement checks, deep
links, and anything that talks to the platform rather than to the device.

So it is worth five minutes, but it does not stand between us and the submission.

| | |
|---|---|
| **In the editor** | `Window → Meta → Platform → Edit Settings` → paste the App ID into the **Meta Quest** row |
| **In the file** | `unity/Assets/Resources/OculusRuntimeSettings.asset` — the per-platform app id field |

The file is committed, so once it is set it stays set.

**And one thing to check on the headset:** `OVRManager → Hand Skeleton Version` must be **Default
(OpenXR)**. The legacy OVR skeleton has no metacarpals on index/middle/ring — exactly the joints that
carry finger spread, which is what distinguishes U from V. On the wrong skeleton the shipped poses
lose the joint carrying their most important signal, and it presents as *"the sensor cannot tell U
from V"*. It is a configuration bug, not a sensor result.

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
