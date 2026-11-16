# BUILD LOG
## The running record of what was built, what is verified, and what is not

**Owner:** W2 · **Started:** 15 Nov 2026 · **Purpose:** one place that answers *what does this repository actually contain right now, and how do I reproduce it.*

> **Why this file exists.** The plan (`MASTER-PLAN.md`) says what we intend to build. This says
> what exists, what has been *proven*, and what has only been *asserted*. Three phases in a row have
> been written by two different hands, and the gap between "documented" and "built" has grown large
> enough to be dangerous: P2's own documents say *"nothing in this repository has ever been
> compiled"*, which was true when they were written and stopped being true the moment the Unity
> licence was activated — but nobody re-ran the build to find out.
>
> **The rule this file enforces:** every claim carries one of three labels, and the label is never
> omitted.
>
> | Label | Means |
> |---|---|
> | ✅ **VERIFIED** | reproduced by a command in this repository, with the output committed or reproducible in one step |
> | 🟡 **BUILT** | the code or the document exists and is consistent with its neighbours; not yet run against hardware |
> | 🔴 **CLAIMED** | asserted somewhere, with no evidence behind it yet |

---

## 1. How to reproduce everything

Four commands, in order. Each is idempotent and each produces the same result every time.

```powershell
# 1. the toolchain, onto E:
pwsh -File scripts/setup_toolchain.ps1

# 2. the project: create, resolve packages, configure, compile
pwsh -File scripts/setup_unity_project.ps1

# 3. build the APK
"E:\unity\Editors\6000.3.28f1\Editor\Unity.exe" -batchmode -quit -projectPath unity `
  -executeMethod Signspace.Editor.BuildScript.BuildAndroid

# 4. check what went into it
& "C:\Users\Bhuwan\AppData\Local\Android\Sdk\build-tools\36.1.0-rc1\aapt2.exe" `
  dump permissions unity/Builds/Android/signspace.apk
```

**The machine:**

| | |
|---|---|
| Editor | Unity **6000.3.28f1** (Unity 6.3 LTS), at `E:\unity\Editors\` — supported to Dec 2027 |
| SDK | Meta XR All-in-One **207.0.0** → core / interaction / interaction.ovr / mrutilitykit / platform / audio / voice / haptics |
| XR | OpenXR Plugin **1.18.0**, XR Plug-in Management **4.6.1** |
| Render | URP **17.3.0**, ASTC, Vulkan, Linear, IL2CPP, ARM64, minSdk 32, targetSdk 34 |
| Android | NDK r27c, OpenJDK 17, SDK 34/35/36/37 |
| Also on the box | Meta XR Simulator **207.1** · Meta XR Operator 207.0.1 · OVR Metrics Tool 2.0.1 |

**Why Unity 6.3 and not "Unity 6 LTS":** Unity 6.0 LTS (`6000.0.85f2`) reached the end of its
standard support on **16 Oct 2026** — a month before this work started. "Unity 6 LTS" in the old
documents meant a retired stream. 6.3 is the only line that is both current and stable, and it
clears Meta's floor of 6000.0.66f2.

---

## 2. What is verified

### 2.1 The build itself — ✅ VERIFIED (16 Nov 2026, after the integration pass)

```text
[BuildScript] active input handling = Both
[BuildScript] package name: com.signspace.signspace, version code 10
[BuildScript] scene tuning matches: locked 12 / close 24 / slack 15 / weights 21 joints, total weight 15.30
[BuildScript] result   : Succeeded
[BuildScript] apk size : 61 MB
[BuildScript] duration : 00:00:37
[BuildScript] sha256   : 2879b8d0420bc3fc3191faa4a855f363d8ac1e6979ae3e5a27fa00ecdaf7c4ac
```

From the built APK (`aapt2 dump badging` / `dump permissions`):

```text
package: name='com.signspace.signspace' versionCode='10' versionName='0.9.0'
minSdkVersion:'32'   targetSdkVersion:'34'
application-label:'SIGNSPACE'   native-code: 'arm64-v8a'
uses-permission: name='horizonos.permission.HEADSET_CAMERA'
```

The previous verified build (15 Nov) was version code **8**, 62 MB, sha256 `10b8a944…`. Version
codes are consumed from `Assets/_Project/version-code.txt`, because Meta rejects a duplicate code
without naming the conflicting build. The scene's serialised tuning is now asserted at build time:
a mismatch aborts the build instead of shipping a 20° band on a 24° metric.

### 2.2 The toolchain — ✅ VERIFIED

Installed on **E:** because C: had only 27.9 GB free and Unity Hub's editor install path defaults
to `C:\Program Files\Unity\Hub\Editor`. An editor plus the Android modules is ~18 GB, so the path is
moved *before* the download, not after.

```text
E:\unity\Unity Hub\      Unity Hub 3.22.2   (was already installed here)
E:\unity\Editors\        6000.3.28f1 + Android Build Support + SDK/NDK + OpenJDK   (~18 GB)
E:\unity\cache\          Unity download cache + UPM global cache
E:\unity\MetaXR\         Meta XR Simulator 207.1
```

### 2.3 The metric — ✅ VERIFIED (offline, reproducible)

`python3 scripts/tune_match.py` is the Python twin of the shipped C# matcher, and it asserts that
the two agree. Three generations of metric, measured on the six shipped shapes:

| | metric | nearest intended pair | noise p95 @2°/joint | score |
|---|---|---|---|---|
| v1 | flat weighted mean over every joint | **3.1°** (U–V) | 8.5° | 1.07 |
| v2 | max of per-finger chain angles | 26.0° | 8.5° | 3.06 |
| **v3** | **max across fingers of the weighted-within-finger mean** | **26.0°** | **6.7°** | **3.89** |

**v1 could not see finger spread.** U and V differ in one joint — how far the index is abducted —
and averaging it across the whole hand diluted an 18° spread to 3.1°. With an 18° near-miss
threshold, the near-miss for U/V could never fire, and the near-miss is the product's entire
identity.

**v3 measures the flat mean too, and rejects it by measurement**: the flat weighted mean collapses
U–V to **5.3° — 20% of its real distance**, with the noise floor at 70% of the pair it is supposed
to separate.

**Every one of these numbers rests on a σ = 2.0°/joint noise model, which is an assumption, not a
measurement.** See §5.

### 2.4 The joint-alias tables — ✅ VERIFIED

`python3 scripts/check_alias_tables.py` proves the Python and C# joint-name → canonical-joint tables
agree: **61 spellings, same canonical joint on both sides, zero conflicts.**

This check exists because `validate_poses.py` cannot catch this class of bug: both of the real bugs
were disagreements *between* the two tables.

| Bug | Symptom |
|---|---|
| `xrhand_` prefix not stripped | v207 reports `XRHand_*`; **0 of 26 joints would map**, and it looks exactly like total tracking failure |
| `Hand_Index1` mapped to `IndexMetacarpal` | Meta's own table says the legacy `Index1` **is the proximal phalange** — every finger rotated one joint off its real axis |

### 2.5 The pose files — ✅ VERIFIED

```text
python3 scripts/validate_poses.py     → 26 shapes, 0 problems
python3 scripts/validate_poses.py --self-test → 29 cases
```

---

## 3. The four Unity-6 traps

**Each one produces a build that SUCCEEDS and is wrong.** None of them errors. That is why they are
written down.

| Trap | What goes wrong | How it is handled |
|---|---|---|
| **The render pipeline** | Unity 6's URP template is a Package Manager package and `-createProject` cannot resolve it (*"Package [com.unity.template.urp-blank] cannot be found"*). The default project is Built-in RP | `Editor/SetupRenderPipeline.cs` creates the URP assets and assigns them to GraphicsSettings **and all six quality levels** |
| **The XR loader** | The XR Plug-in Management window does not exist in batch mode, so the build ships with **no XR loader**: installs, launches, **black screen** | `Editor/SetupXR.cs` creates the XR settings asset, assigns the OpenXR loader, enables Meta's feature set, and **verifies it** |
| **The active input handling** | Unity 6 exposes no public API for it; the value lives in `ProjectSettings.asset` as `activeInputHandler`. The template ships **0** (old input manager) and the Meta SDK drives pinch through the new one | `scripts/patch_player_settings.ps1` sets it to **2 (Both)**; `BuildScript` **verifies it by reading the file back** |
| **The passthrough camera-access flag** | `OVRProjectConfig.isPassthroughCameraAccessEnabled` gates Meta's own manifest preprocessor. Without it the build succeeds and the headset shows **no room** | `SceneBuilder.EnablePassthrough` sets it, and the permission is verified in the built APK with `aapt2` |

Two more that only bite in a batch build:

- **The duplicate OpenXR loader.** Meta's Core SDK and the Unity OpenXR Plugin both ship
  `libopenxr_loader.so`, and Android's packager refuses to merge two native libraries with the same
  path (*"2 files found with path 'lib/arm64-v8a/libopenxr_loader.so'"*). Meta's own
  `OVRGradleGeneration` removes the duplicate, but **only when its hook runs, and it does not run in
  batch mode** — no `[Meta]` messages appear in the build log. `SetupXR` does the exclusion
  explicitly.
- **The namespaces moved.** `OVRManager`, `OVRPassthroughLayer` and `OVRProjectConfig` left
  `Oculus.VR` in Core SDK v207. They are in the global namespace now, and `Oculus.VR.OVRManager`
  does not compile.

---

## 4. The things that are BUILT but not yet proven on hardware

Everything below is code that compiles and documents that were written, with **no device run behind
it**. This is the honest list, and it is long.

| Area | What exists | What is missing |
|---|---|---|
| **The whole matching loop** | the matcher, the weights asset, the palm term, the hold timer, the bands, the near-miss line, the honest mirror | one headset session |
| **P1 rigs** | the hand visual, the grab/rotate trial, the rotation trial, the surface probe, the persistence probe, the perf rig, 20 probe poses | the readings |
| **P1 gates** | G1: 9 criteria, **0 met** | a headset |
| **P2 gates** | G2: 8 criteria, **5 ticked on simulation** | 20/20 locks, the payoff filmed, the stranger test |
| **Every threshold in the project** | derived and cross-checked in simulation | the real tracking-noise figure (P1.5) |
| **MRUK, the shelf, persistence** | the code and the rig | a real room |
| **Passthrough** | enabled, permission verified in the APK | a real room |
| **Performance** | the sampler, the thermal timeline, the refusal to certify an editor run | a Quest 3S |

### 4.1 The noise model is the single biggest assumption

**σ = 2.0°/joint** is a placeholder written into the tools with the instruction
*"[REPLACE with the P1.5 measurement]"*. Every threshold in the project rests on it: the 12° lock
edge, the 24° close edge (3.3σ at the modelled noise, and it **fails at 1.5×**), the 15° palm slack
(10.6σ of the modelled wrist jitter), and the hold timing.

The tripwire is written down: **if the measured device noise exceeds ~1.4× the model, `closeDeg`
comes down to 20.**

### 4.2 The simulation is good, and it is still simulation

Over **65,000 simulated hand-frames** across seven simulators, with a real methodology — including
one simulation bug that was found and **withdrawn in writing** (`band-tuning.md` §3: an earlier
claim that the effective lock edge was ~10° turned out to be a bug in the *simulation*, and the
finding was rescinded rather than defended). That is the right instinct. It is still not a sensor.

The log even marks its own rows: `lock-log.csv` has a `source` column reading `simulated`, because
*"a simulation that looks like a measurement is a lie with extra steps."*

---

## 5. What happens if the headset never arrives

This is the plan for that case, and it is not "hope for the best".

**The judges have headsets. We do not need one — we need a build that works on one.** So the
substitute for the headset is not a workaround, it is a different rig with different limits.

| What the headset gives | The substitute | What it cannot give |
|---|---|---|
| hand tracking | **Meta XR Simulator**, Quest 3 profile, camera-driven hands | real tracking noise — the one number everything depends on |
| the app launches and runs | the simulator, in-editor and as a Windows build | passthrough — the simulator does not composite a camera feed |
| the matcher, the bands, the near-miss, the reference hand | **the simulator, with scripted poses** | MRUK — there is no room to scan |
| a 10 s capture | the simulator's own session recorder | thermal — there is no battery |
| 72 fps on Quest silicon | nothing | fps on Quest silicon, full stop |

**What the simulator DOES buy:** the entire core loop becomes testable on this PC — the app
launches, hands track, the matcher produces a live distance, the band moves, the near-miss fires,
the reference hand rotates. That is most of the risk in the product, and it is risk that can be
retired without hardware.

**What it cannot buy, and must therefore be handled by honesty:**
the tracking-noise figure, passthrough, MRUK, thermal, and fps on Quest silicon. Each of those
becomes a **disclosed limitation** rather than a measured claim — and the competition's rubric
honours a clear, specific disclosure far more than a vague one.

---

## 6. The open items, in the order they block

| # | Item | Blocks | Who |
|---|---|---|---|
| 1 | **The real tracking-noise figure** (P1.5) | every threshold in the project | a headset, or the simulator's camera hands as a weak proxy |
| 2 | **20/20 locks and 0/20 false locks** on hardware | G2 | a headset |
| 3 | **The payoff, filmed** | G2 | a headset |
| 4 | **A stranger locks a shape uncoached** | G2, and the video | a headset |
| 5 | **The App ID** in `OculusRuntimeSettings.asset` | Meta platform services — *not* the competition deliverable | the dashboard |
| 6 | **`OVRManager → Hand Skeleton Version` = Default (OpenXR)** | the near-miss, silently | the headset |

---

## 7. Change log

| Date | Change | By |
|---|---|---|
| 15 Nov 2026 | File created. Toolchain installed on E:, Unity project created, first compile (0 errors), first APK (62 MB). The metric redesigned and measured offline. The joint-alias tables reconciled. The nine stub specs written. Passthrough enabled and verified in the built manifest. | agent |
| 16 Nov 2026 | P2 integrated: the v3 metric, the palm term, the bands, the hold timer, the left-hand policy, the schema v2, and ~20 new C# files — none of which had ever been compiled. Restored the Unity engine modules that had been dropped from `manifest.json` (144 compile errors). | agent |
| 16 Nov 2026 | **Integration pass.** The merged tree compiled (8 errors fixed), the scene was regenerated with explicit tuning, the build now asserts it, the silently-missing payoff was restored, the APK was rebuilt at version code 10, `MATCH-ALGORITHM.md` moved to v3, the tuning raw output was regenerated, and the P3 feedback layer was wired into the scene. Details in §8. | agent |

---

## 8. Session log — 16 Nov 2026 (integration pass)

Written in the order the work happened, because the order is the useful part: each defect was
only visible after the previous one was fixed.

### 8.1 The merged tree did not compile — ✅ FIXED, VERIFIED

`P2`'s own documents said the code had never been compiled. That was still true. The first command
returned **8 errors in 4 files**, and three of them were in the P2 code itself:

| File | Error | Cause |
|---|---|---|
| `Matcher.cs` | CS0206 | `out BasisNote` passed a **property** as an out parameter |
| `Matcher.cs` | CS0102 | `OrientationPenaltyDegrees` declared **twice** in the class |
| `SurfaceProbe.cs` | CS0170 ×4 | `SurfaceCandidate best;` left unassigned by a short-circuited `finder != null && …` |
| `ShelfAnchor.cs` | CS0170 ×5 | same pattern on `surface` |
| `BandFeedback.cs` | CS0019 | `Color * Light` — `glowRatio` was meant, `glow` is the Light field |
| `SceneBuilder.cs` | CS0104 | bare `Object` ambiguous with `using System;` |
| `BuildScript.cs` | CS0117 | `PlayerSettings.bundleVersionCode` does not exist (Android-specific) |
| `BuildScript.cs` | CS0117 | `PlayerSettings.activeInputHandler` does not exist — the regression that replaced the file-patch path |

```bash
"E:/unity/Editors/6000.3.28f1/Editor/Unity.exe" -batchmode -quit -projectPath . \
  -logFile compile3.log -executeMethod Signspace.Editor.BuildScript.ApplySettingsOnly
# EXIT=0; [BuildScript] active input handling = Both; [BuildScript] player settings applied
```

### 8.2 The scene ran neither tuned band — ✅ FIXED, VERIFIED

`Main.unity` serialised `closeDeg: 20` and `orientationSlackDeg: 20`. The code said 24 and 15.
Unity's serialised value wins, so the built app ran the older tuning and nothing reported it.

Two changes, structural rather than a YAML edit:

1. **One source of truth** — `Matcher.Tuned*` constants; the fields initialise from them.
2. **The generator writes them explicitly** — `SceneBuilder` assigns every tuned value and logs it,
   instead of relying on a default that a serialised scene silently overrides.
3. **The build refuses a mismatch** — `BuildScript.VerifySceneTuning()` opens the scene, compares
   all seven values (plus the weights asset), and aborts with the field name, the scene value and
   the tuned value if any differ.

```text
[SceneBuilder] matcher tuning <- locked 12 / close 24 / release 2 / slack 15 / hold 0.70 s
[BuildScript] scene tuning matches: locked 12 / close 24 / slack 15 / weights 21 joints, total weight 15.30
```

### 8.3 The lock payoff was silently absent — ✅ FIXED, VERIFIED

`SceneBuilder` warned `Prefabs/LockPayoff.prefab missing` even though the prefab was committed. In a
cold batch run the asset database had not imported it, `LoadAssetAtPath` returned null, and the
scene was written **without the payoff** — half of G2 criterion 5, missing with one warning.

Fixed by `AssetDatabase.Refresh()` before the loads, plus a regeneration fallback
(`LockPayoffBuilder.Build()`) if the load still fails. The scene now logs:

```text
[SceneBuilder] LockPayoff.prefab did not load; regenerating it.
[LockPayoff] wrote Assets/_Project/Prefabs/LockPayoff.prefab (ok), total 0.99 s
[SceneBuilder] lock payoff wired, 0.99 s of a 1.20 s budget
```

### 8.4 Two defects in the build script itself — ✅ FIXED, VERIFIED

- **A UTF-8 BOM in `Packages/manifest.json`** made the Package Manager reject the whole file
  (`"not valid JSON … Char: 65279"`, *No packages loaded*) and Unity exited before compiling a
  single script. The BOM came from a PowerShell write. Fixed by writing the file without one; the
  committed content is byte-identical to the last good manifest.
- **The version code had been reset to 1.** The APK claimed code 1 while the dashboard had code 8,
  and Meta rejects a duplicate code with an error that does not name the build. Restored the
  read-then-increment path from `Assets/_Project/version-code.txt`; this build consumed **9** for a
  check and **10** for the final artefact.
- **The logged size was `summary.totalSize`**, which reported *1256 MB* for a 62 MB APK. It now logs
  the APK file's length (61 MB) and the build total separately, labelled as including intermediates.

### 8.5 The metric evidence was stale — ✅ REGENERATED, VERIFIED

`matcher-tuning-raw.txt` was UTF-16 with no v3 section, and `tune_match.py` still printed
`closeDeg 20` in the shipped-threshold line. Regenerated as UTF-8 with a full v3 section and a
decision block driven by one set of shipped constants:

```text
v3  max of weighted finger means   26.0   6.7   3.89   <- ships
v3  flat weighted mean (the trap)   5.3   3.7   1.43
shipped thresholds lockedDeg 12 / closeDeg 24 / nearMissMaxDeg 26
```

### 8.6 The P3 feedback layer — 🟡 BUILT, NOT DEVICE-TESTED

Before this pass, `NearMissDisplay`, `HonestMirror` and `HandConfidenceReadout` existed as scripts
that nothing instantiated. They are now in `Main.unity`:

| Piece | Evidence |
|---|---|
| Near-miss line | `FeedbackUiBuilder` creates the canvas, `Text`, `CanvasGroup` and binds `NearMissDisplay` |
| Near-miss rule | `scripts/near_miss_sim.py` → 36 pairs, 30 fires (every clean non-target shape), 0 disagreements; U↔V names the other shape |
| Honest mirror | panel + a pinchable physical handle (`HonestMirrorHandle`); count line says *pending* because P1.5 has not run |
| Pause | `TrackingConfidence` dims the panel and breathes a dot; `BandFeedback` goes grey and still while `IsPaused` |
| Copy | 10 variants, final 5-word line — not yet read to a non-signer |

The near-miss now compares the **finger term only**, so a correct shape with a twisted wrist can never
be renamed as a different sign.

### 8.7 Traps found in this pass, for the next person

| Trap | Symptom | Fix |
|---|---|---|
| BOM in a JSON file | Package Manager: *Non-whitespace before {[ … Char: 65279*; **No packages loaded** | write JSON without a BOM; never `Out-File` it |
| `summary.totalSize` | a 62 MB APK logged as 1256 MB | use the file's length for the artefact's size |
| Cold `AssetDatabase` | a committed prefab loads as null and is silently omitted from the scene | `AssetDatabase.Refresh()` + regenerate fallback |
| Serialised field vs code default | editor shows 24, the app runs 20 | generator writes values; build asserts them |
| `Object` with `using System;` | CS0104 | qualify `UnityEngine.Object` |
| Short-circuit + `out` struct | CS0170 on every field read after the guard | initialise to `default` |

---

## 9. What is next

1. **Headset (or simulator) session** — measure σ per joint (P1.5), then re-run `tune_match.py
   --sigma <measured>` and move the thresholds only if the tripwire fires. This is still item 1.
2. **P3 people/device checks** — 20 sloppy attempts for false positives, the three degradation
   scenarios, one stranger for the near-miss, the copy read to a non-signer.
3. **P4 shelf** — replace the provisional `HonestMirrorHandle` placement with the real shelf object;
   the binding is one event, so the panel does not change.
4. **POSE-FORMAT §4.3** — the left-hand frame test; until it runs, `HandSupport.LeftHandSupported`
   stays false and the honest mirror says so.

**Open hardware dependencies, unchanged:** the real tracking-noise figure, 20/20 locks and 0/20
false locks, the payoff filmed, a stranger locking a shape uncoached, the App ID, and
`OVRManager → Hand Skeleton Version = Default (OpenXR)`. The simulator can retire the launch,
tracking, matcher and near-miss risks; it cannot produce noise, passthrough, MRUK, thermal or
silicon fps.
