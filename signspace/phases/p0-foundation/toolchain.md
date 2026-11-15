# P0.3 + P0.4 execution kit

**Purpose:** everything decided in advance so tonight is clicking, not deciding. Fill the blank fields as you go; this file is the evidence for G0.

---

## 1. P0.3 — the app and the channel

### Naming (decide once, it is permanent)

| Field | Value | Rule |
|---|---|---|
| **Package name** | **`com.signspace.signspace`** ✅ fixed 15 Nov 2026 | Reverse-DNS, lowercase. **Cannot be changed after the first upload.** Must match `unity/Assets/_Project/package-name.txt` byte for byte — that file is what `BuildScript.cs` reads |
| **Meta app ID** | **`1444571625411296`** | Created in the developer dashboard |
| App display name | `SIGNSPACE` | |
| Release channel 1 | **`competition`** created · **should read `Competition`** | ⚠️ The rules say *a channel named "Competition"*. Rename it if the dashboard allows mixed case; screenshot the result either way |
| Release channel 2 | **`internal`** | Day-to-day test builds; keeps the competition channel clean |
| Category | Productivity (or Education if forced) | |
| Age rating | 10+ | Content is non-violent, non-social, no UGC |

### Steps

1. ✅ App created — **app ID `1444571625411296`**.
2. ✅ Package name chosen — **`com.signspace.signspace`** — and written to `unity/Assets/_Project/package-name.txt`, which `BuildScript.cs` reads. The dashboard only displays it on **Distribution → Builds → Build Details** *after* the first upload; until then the file is the source of truth and must match what the upload reports.
3. ✅ Channels exist: `competition`, `internal`.
4. ⬜ **Rename `competition` → `Competition`** if mixed case is accepted; otherwise screenshot the refusal.
5. ⬜ Upload the first APK to it to prove the path works **and** to surface the package name on the dashboard, then confirm the dashboard string equals the file.
6. ⬜ Screenshot the channel list → `evidence/p0.3-competition-channel-<date>.png`.

**Done when:** `Competition` exists and has accepted an upload, and the package name is written down in two places (here and `accounts.md`).

---

## 2. P0.4 — the Unity build

### Versions — resolved from the competition page, 14 Nov 2026

| Item | Value | Status |
|---|---|---|
| SDK | **Meta XR All-in-One, v207** (*"Unity: Meta XR SDKs v81+ (v207) All-in-One XR"* — the competition page states it). UPM package `com.meta.xr.sdk.all` | ✅ **v207.0.0** — confirmed on the public Unity registry, installs without the Asset Store EULA |
| Unity | **6.3 LTS (6000.3.28f1)**, released 3 Nov 2026, supported to Dec 2027 | ✅ **installed** |
| Android min API | 32 | ✅ |
| Android target API | 34 *(if the SDK complains, move to 35 and record it)* | ⚠️ confirm on install |
| Scripting backend | IL2CPP | ✅ |
| Architecture | ARM64 only | ✅ |
| Graphics API | Vulkan, then GLES3 — or GLES3 only if Vulkan is unstable. Record the choice. | ⚠️ measure |

> **The version trap, avoided.** Unity 6.0 LTS (`6000.0.85f2`) reached the end of its standard
> support on **16 Oct 2026** — a month before this build. "Unity 6 LTS" in the old docs meant a
> stream that is now retired. Unity 6.6 (`6000.6.9f1`) is the newest release but is an *Update*
> stream, supported only until 6.7 ships. **6.3 LTS is the only choice that is both current and
> stable**, and it clears Meta's floor of 6000.0.66f2.
>
> The SDK is also the constraint: `com.meta.xr.sdk.all` v207.0.0 declares `unity: 6000.0`.

### Toolchain location — everything heavy on E:

```
E:\unity\
├── Unity Hub\        Unity Hub 3.22.2 (was already installed here)
├── Editors\          6000.3.28f1 + Android Build Support + SDK/NDK + OpenJDK  (~18 GB)
├── cache\            Unity download cache + UPM global cache
└── MetaXR\           Meta XR Simulator 207.1
```

**Why E:** C: had only **27.9 GB free** and Unity Hub's editor install path defaults to
`C:\Program Files\Unity\Hub\Editor`. An editor plus the Android modules is ~18 GB, so the path is
moved first, before the download, by `scripts/setup_toolchain.ps1`.

Already on this machine and reused:

| Item | Version | Path |
|---|---|---|
| Unity CLI | 1.0.0-beta.12 | `C:\Users\Bhuwan\AppData\Local\Unity\bin\unity.exe` |
| JDK | 17.0.18 (Temurin) | `C:\Program Files\Microsoft\jdk-17.0.18.8-hotspot` |
| Android SDK | platforms 34/35/36, build-tools 35/36, NDK 27.0/27.1 | `C:\Users\Bhuwan\AppData\Local\Android\Sdk` |
| adb | from the Android SDK | `…\platform-tools\adb.exe` |
| Meta XR Simulator | 207.1 | `C:\Users\Bhuwan\AppData\Roaming\metavr\tools\xrsim` |
| Meta XR Operator (MCP) | 207.0.1 | `C:\Users\Bhuwan\AppData\Roaming\metavr\tools\xroperator` |
| OVR Metrics Tool | 2.0.1 | `C:\Users\Bhuwan\AppData\Roaming\metavr\tools\ovrmetric` |

The Meta tools are installed by `metavr tools install <name>` and are scriptable — no clicking.

### Player Settings to set before the first build

| Setting | Value | Why |
|---|---|---|
| Package name | from §1 | Must match the dashboard exactly |
| Minimum API level | Android 12 (API 32) | Meta floor |
| Target API level | Android 14 (API 34) | Store requirement band |
| Scripting backend | IL2CPP | ARM64 + performance |
| Target architectures | ARM64 | Quest is ARM64 only |
| Graphics APIs | Vulkan / GLES3 | Record what you ship |
| Multithreaded rendering | On | Free CPU |
| Color space | Linear | Correct lighting on Quest |
| Active input handling | Both | ISDK needs the new input system |
| **Texture compression** | ASTC | Quest default; anything else is slower |
| **Static batching** | On | Cheap win |
| XR Plug-in Management | Meta XR (OpenXR provider as the SDK requires) | Hand tracking and passthrough live here |

### The five-minute smoke test (this *is* the gate)

1. New 3D (URP) project at `unity/`.
2. Import **Meta XR All-in-One v207**.
3. Add the **Meta XR rig** to an empty scene.
4. Enable **hand tracking**; enable **passthrough** on the rig's camera.
5. Build → APK → install on the **3S**.
6. You should see your real room and both hands.

**If passthrough is black or hands are missing:** the cause is almost always one of four things — OpenXR feature not enabled, hand-tracking permission not granted, the rig's camera not set to passthrough, or the wrong graphics API. Fix in that order.

### Record here

| Item | Value |
|---|---|
| Unity version (exact) | **6000.3.28f1** — `E:\unity\Editors\6000.3.28f1\Editor\Unity.exe` |
| Meta XR SDK version (exact, all packages) | **207.0.0** — `com.meta.xr.sdk.all`, resolving core / interaction / interaction.ovr / mrutilitykit / platform / audio / voice / haptics at 207.0.0 |
| OpenXR Plugin | **1.18.0** (Meta's floor for the XR Operator is 1.17.0; Unity's VR Glasses guidance asks for 1.18.0+) |
| XR provider / OpenXR features enabled | ⬜ set in the editor — `XR Plug-in Management → Android`, Meta XR feature group |
| Graphics API shipped | **Vulkan** — `BuildScript.ApplySettings()` pins it, so it cannot drift between builds |
| Editor install path | `E:\unity\Editors` |
| First build: result on 3S | ⬜ pass / ⬜ fail — **licence ✅ cleared 15 Nov; still blocked on the headset** |
| fps in the empty scene | ⬜ |

> **Two things are still needed before the first build can run, and neither is a code problem:**
> 1. ~~**A Unity licence.**~~ ✅ **Cleared 15 Nov 2026.** For reference, the failure it used to produce:
>    `[Licensing::Module] Error: 'com.unity.editor.headless' was not found.`
>    Fix was: `unity auth login`, or sign in to Unity Hub. Already done — if it ever recurs, that is the fix.
> 2. **The Meta XR SDK importing.** It resolves from the public Unity registry, so it needs no
>    Asset Store EULA. `scripts/setup_unity_project.ps1` creates the project, merges the manifest
>    and compiles, all headless.

### The two commands that do the rest

```powershell
# create the Unity project, merge the pinned packages, compile headlessly
pwsh -File scripts/setup_unity_project.ps1

# build the APK (needs the licence)
"E:\unity\Editors\6000.3.28f1\Editor\Unity.exe" -batchmode -quit -projectPath unity `
  -executeMethod Signspace.Editor.BuildScript.BuildAndroid
```

`BuildScript` applies every Player setting in §2 as **code**, so the submission build on Day 4 is
produced by the same command as the test build on Day 1, with the same settings and the same
keystore. It also prints the APK's SHA-256, which is what P9.1 needs to freeze.

**Three lines to send back** — see [`OWNER-TASKS.md`](OWNER-TASKS.md) §I for the full list.

---

## 3. What to send back

Three lines, and I can take P1 and P2 off you immediately:

1. `package name = ____`
2. `Unity = ____ , SDK = ____ , graphics = ____`
3. `hands+passthrough on 3S = yes/no, and the fps you saw`
