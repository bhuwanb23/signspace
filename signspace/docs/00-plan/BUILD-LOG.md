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
| 16 Nov 2026 | **P3/P4 re-integration.** A rewrite replaced the tree and reverted the integration fixes. Restored the package manifest, fixed 10 project errors, deleted the orphaned panel builder, restored the payoff and the missing K/P authoring path, fixed the budget script's encoding. P4.2's three open boxes measured by `ShelfCheck`; P3's evidence re-run unchanged. APK v13. Details in §8.10. | agent |
| 16 Nov 2026 | **Phase 4 completed at the desk.** P4.3: five cross-process relaunches and leave/return pass, and the probe found a duplicate-object bug in `RestoreUnlocked`. P4.4: the plinth is a four-part prefab with generated gradient/shadow and a pinch-only surface offer. P4.5: a dev-only passthrough switch. P4.6: measured FoV reflow (110° → 6 columns, 70° → 4, no scaling). APK rebuilt. Device rows, clips and captures remain. Details in §8.11. | agent |

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

### 8.7 The four software gaps the P0–P2 audit found — 🟡 CLOSED (not device-tested)

The audit (`phases/P0-P2-VALIDATION.md`) separated hardware measurements from software that was still
missing. Four were software; all four are now built and checked without a headset:

| Gap | Fix | Evidence |
|---|---|---|
| Reference hand drew every finger from the palm's centre — `Build()` never set a joint's `localPosition` | `HandVisual.RestOffsets`: metacarpals at their knuckles, every other joint at its parent bone's tip; capsule geometry now follows the offset instead of hanging +Y | `HandRigCheck`: base spread **45 mm**, proximal **17 mm**, tips **18 mm**, wrist→middle tip **192 mm** (the three spreads were **0**) |
| Honest mirror was text-only while G3 requires correct shape renders | `HonestMirror.RenderExcludedShapes()` renders M/N/T/R at start from their own probe pose files into 256² textures; empty slots stay dim rather than guessed | 4 `Shape_*` slots + labels in `Main.unity`; 20 renderers per thumb, destroyed after render |
| No session rate cap and no runtime off switch | `Matcher.nearMissSessionCap = 3`, `NearMissEnabled`, `ResetNearMissSession()`; `NearMissToggle` block beside the mirror handle, dims when off | `near_miss_sim.py`: **3 fires in 5 min** of constant attempts, **0** with naming off, cooldown still bounds a 1-min session |
| No authored pair differed in palm facing, so P2.6's term had no content | K and P authored in `StreamingAssets/Facing/` (probe-only): identical fingers, 90° pronation. `hand_model` gained a `twist` axis and optional `wrist` spec — Wrist/Palm were hard-coded identity | `facing_pair_sim.py`: finger distance **0.00°**, O **90.00°**, total **75.00°**; with the palm term they would lock. Largest O among the shipped six: **0.00°** |

Two decisions worth keeping:

- **K/P do not ship yet.** They are reference content in `Facing/`, not `Signs/`. Shipping them changes
the six-shape commitment, the demo and P5's capture plan, so that is a P5 decision, not a side effect of
a bug fix.
- **The switch does not refill the cap.** Toggling naming off and on cannot farm hints, or the cap is
decoration.

### 8.8 Traps found in this pass, for the next person

| Trap | Symptom | Fix |
|---|---|---|
| BOM in a JSON file | Package Manager: *Non-whitespace before {[ … Char: 65279*; **No packages loaded** | write JSON without a BOM; never `Out-File` it |
| `summary.totalSize` | a 62 MB APK logged as 1256 MB | use the file's length for the artefact's size |
| Cold `AssetDatabase` | a committed prefab loads as null and is silently omitted from the scene | `AssetDatabase.Refresh()` + regenerate fallback |
| Serialised field vs code default | editor shows 24, the app runs 20 | generator writes values; build asserts them |
| `Object` with `using System;` | CS0104 | qualify `UnityEngine.Object` |
| Short-circuit + `out` struct | CS0170 on every field read after the guard | initialise to `default` |

### 8.10 The P3/P4 tree replaced the integrated one — ✅ RE-INTEGRATED, VERIFIED

**16 Nov 2026, second pass.** The working tree was replaced with a P3/P4 rewrite: 148 changed paths,
74 with real content. It contained substantial new work — `NearMiss.cs`, `Trackability.cs`,
`HonestMirror.prefab`, `ShelfSilhouettes/ObjectView/Idle`, schema 3, six new Python checks — and it
undid every integration fix from §8.1–8.3. Nothing in it had been compiled.

| What was wrong | Why it happened | Fix | Evidence |
|---|---|---|---|
| **144 CS1069** in Meta packages: Terrain, Animation, AssetBundle modules "not found" | `manifest.json` had been replaced by a three-package list (OpenXR, XR Management, Meta SDK all); every built-in module, URP and uGUI were gone | restored the committed `manifest.json` + `packages-lock.json` | package errors **144 → 0**; only project errors remained |
| `OrientationPenaltyDegrees` declared twice | merge scar in `Matcher.cs` | removed the duplicate | CS0102 gone |
| `out BasisNote` on a property; `SurfaceCandidate best` unassigned behind a short-circuit; `shown * glow` (`Light`); bare `Object` ambiguous | the four §8.1 fixes, reverted | local + assign, `default`, `glowRatio`, `UnityEngine.Object` | 10 project errors → **0** |
| `NearMissToggle` used `matcher.NearMissEnabled`, which did not exist | new toggle expected a property the rewritten `Matcher` only had as a field | added a property that routes the setter through `SetNearMissEnabled` (so the switch still persists) | compiles; the off switch keeps its save path |
| `FeedbackUiBuilder` (previous runtime panel) no longer matched `HonestMirror` | two panel designs coexisted: the new prefab builder and the old runtime builder | deleted the orphan; `SceneBuilder` already calls `HonestMirrorBuilder` | no references remain |
| `PlayerSettings.bundleVersionCode` / `activeInputHandler` do not exist | the same regression §8.1 fixed | Android-scoped version code; input handling is **asserted** from `ProjectSettings.asset` (must be `2`) and fails the build otherwise | settings apply, `EXIT=0` |
| **No lock payoff in the scene** | cold batch import returned null for the committed prefab, exactly as in §8.3 | `AssetDatabase.Refresh()` + `LockPayoffBuilder.Build()` fallback | log: *did not load; regenerating* → *payoff wired, 1.04 s of 1.20 s* |
| `shelf_budget.py`: "PERFORMANCE.md has no draw_calls ceiling" | the script read the doc with the Windows locale codec, so `≤` became mojibake and every ceiling looked absent | read **and** write UTF-8 | budget check PASS 26/120, 912/150k; report no longer contains replacement characters |
| The reference hand was flat again: all 26 joints at their parent's origin | `HandVisual`'s rest offsets were lost in the swap, so capsules radiated from the palm centre and every honest-mirror specimen was a bundle of fingers | restored `RestOffsets` (knuckles across the palm at 45 mm span; every other joint at its parent bone's tip) and made the capsules follow the offset | `HandRigCheck`: base **0.045 m**, proximal **0.020 m**, closest tips **0.019 m**, wrist→tip **0.215 m**, all PASS |
| Matcher's tuned constants and the build-time tuning check were gone | the rewrite predates §8.2; scene values were still 12/24/15 only by luck | restored `Matcher.Tuned*`, made `SceneBuilder` write them explicitly, and `BuildScript.VerifySceneTuning()` now opens the scene and aborts on any disagreement | log: *matcher tuning ← locked 12 / close 24 / release 2 / slack 15 / hold 0.70 s* then *scene tuning matches*; a scene edited to 20 would fail the build |
| K/P facing pair had no authoring path | the rewrite dropped `twist` and the wrist spec, and `local_rotations_from_deltas` hard-coded Wrist/Palm to identity — the original P2.6 gap | restored twist, optional wrist/palm specs, and real wrist/palm locals (finger locals stay local, so a wrist turn cannot look like finger error) | `facing_pair_sim.py`: fingers **0.00°**, O **90.00°**, total **75.00°**, reverse identical, shipped six O **0.00°** |

**P3 re-validated, not re-claimed.** Every P3 simulator was re-run against this tree: near-miss 8/8
correct shapes, 0/2000 false positives, 9 fires without the 400 ms window and 1 with it, pause PASS
with 1 blind lock per scenario as control, copy lint 0 over 68 literals, trackability 0 violations.
No number in the P3 documents changed. `validate_poses.py` now reports **28 shapes** because the K/P
facing probes are in `Facing/`; `validate_poses` was taught that folder is probe-only.

**P4.1/P4.2 closed at the desk.** `shelf_sim.py` passes seven rooms and the area-cap property.
A new `Editor/ShelfCheck.cs` builds the shelf and twelve shipped objects and measures what the task
had only asserted:

```text
slot grid 0.425 x 0.115 m inside 0.500 x 0.160 m      PASS
widest silhouette 0.064 x 0.041 in a 0.075 x 0.065 cell PASS
markers: 1 dim at launch; 3 filled + 1 dim            PASS
overlapping object pairs: 0 of 66                      PASS
12 objects: 26 draw calls, 864 worst-case triangles    PASS
```

**Build:** APK **v13**, 62 MB compressed, version code **13** so it installs over v12; scene contains
the shelf, the payoff, the panel and its plinth. Included in `assets/`: `Probe/` (20), `Facing/` (2)
and `trackability.json`.

**Still not measured:** every device number (frame time, idle CPU, MRUK, relaunch/reboot), a real hand
firing the near-miss, the non-signer read, a stranger, and a signer's eye on the four excluded shapes.
G3 and G4 remain open for those reasons, not for missing code.

### 8.11 Phase 4 finished as work — P4.3 to P4.6 ✅ DESK-VERIFIED, DEVICE OWED

**16 Nov 2026, third pass.** P4.1/P4.2 were already closed at the desk (§8.10). This pass finished the
other four tasks and found two real defects while doing it.

| Task | What was missing | What was built | Evidence |
|---|---|---|---|
| **P4.3** | The editor check had never been run; the relaunch claim had only source checks | `Editor/PersistenceRelaunch.cs`: `Init`, `Verify` ×5 (each a **separate Unity process**), `LeaveRoom`, `ReturnRoom` | `P4.3 Check persistence` **PASS**; relaunches **5/5** `restore 1..5, order U,B,C, objects 3`; `leave` → `DifferentRoom`, plinth, home kept, `W` saved; `return` → `Restored`, `U,B,C,W`, 4 objects |
| **P4.3 defect** | `RestoreUnlocked` called `Destroy`, which is deferred; a second restore in the same frame left the old objects beside the new | deactivate before destroy (`DestroyObject`), used by restore, slot replacement and adoption | probe went from `RestoredUnlocked duplicated objects: 6` to `objects 3` |
| **P4.4** | The "plinth" was a tinted cube, and the mid-session surface offer existed only as a private flag nobody could raise | `Editor/PlinthBuilder.cs` builds a 4-part prefab (gradient top, lip, transparent shadow, seam) with its top at local y=0 and a 4° tilt; `Scripts/Spatial/SurfaceOffer.cs` is a hidden bead whose pinch is the only way to accept; `ShelfAnchor.PollSurfaceOffer` raises the offer only when there is no home | `P4.4 Check plinth` **PASS**: parts, queue 3000 shadow, top y=0, tilt, no Canvas, no error copy, offer does not move the shelf, accepting saves the anchor, a second surface cannot replace the home |
| **P4.4 defect** | Accepting the offer after a fallback left `Result == Plinth`, so the launch log reported the wrong state | a resolved placement now reports `PlacedFresh` when the previous state was `Plinth` | check asserts `PlacedFresh` after `AcceptSurfaceOffer` |
| **P4.5** | No passthrough switch existed at all | `Scripts/UI/PassthroughToggle.cs`: `P` in development builds flips `OVRPassthroughLayer.hidden` by reflection; shares a GameObject with `DevOnly` so a release build removes it | `P4.5 Check passthrough toggle` **PASS**: off/on flip one sink, two changes → two writes, a missing layer reports failure without writing |
| **P4.6** | No FoV awareness; the shelf was whatever width 6 columns produced | `Scripts/Spatial/ShelfReflow.cs` measures horizontal FoV and the grid's angular width; below 85° it folds to a 4×3 tray, tilts it 20°, pulls it 4 cm and narrows the plate; `ShelfAnchor.SetLayout` moves objects in world space and eases them over 0.35 s. Objects are never scaled | `P4.6 Check FoV reflow` **PASS**: 110° → 6 cols / **51.6°** grid, widest object edge 27.5°; 70° → 4 cols / **35.1°** grid, widest object edge **22.2°** inside the 27.5° half-cone; 12 slots, 4 objects, no scale change, idempotent, reversible |

**Why the reflow triggers on FoV and not on a fixed 50°.** The same 51.6° grid is comfortable at 110°
and cramped at 70°; the display is the variable that changed between the two products. The task's 50°
cap is still enforced, as an assertion that the *tray* is under 50° wide (it is 35.1°). The table in
`MRUK-SHELF.md` §7 was rewritten to the measured numbers.

**Full check set run after the change** (all `-batchmode`, Unity 6000.3.28f1):

```text
P4.3 Check persistence        PASS    schema 1/2/3, room branches, write protection
PersistenceRelaunch Init      PASS    schema 3, order U,B,C
PersistenceRelaunch Verify×5  PASS    restore 1..5, objects 3, localPos (0.120, -0.030, 0.210)
PersistenceRelaunch Leave     PASS    DifferentRoom, plinth, home kept, W saved
PersistenceRelaunch Return    PASS    Restored, order U,B,C,W, objects 4
P4.4 Check plinth             PASS    4 parts, y=0 top, 4° tilt, silent, offer gated
P4.5 Check passthrough toggle PASS    off/on, missing layer reported
P4.6 Check FoV reflow         PASS    51.6° wide, 35.1° tray, 22.2° widest object
ShelfCheck                    PASS    12 objects, 26 draw calls, 0/66 overlaps
HandRigCheck                  PASS    base 0.045 / proximal 0.020 / tips 0.019 / wrist-tip 0.215 m
```

Python, unchanged and re-run: `validate_poses` 28/0, `nearmiss_sim`, `false_positive_sim`, `pause_sim`,
`facing_pair_sim`, `shelf_sim`, `shelf_budget` PASS, `test_shelf_wiring` OK,
`test_persistence_contract` OK, `check_trackability`, `lint_copy` 0 violations.

**Build:** APK **v13**, versionCode **13**, 64,438,133 bytes,
`sha256 d8ce961bd6e810d42720f52b188773824f28fb859811efb142685fc95505b06c`. The scene contains the
plinth prefab instance (inactive until a fallback), `SurfaceOffer`, `ShelfReflow` and
`PassthroughToggle` + `DevOnly`.

**Still not run, and not implied by any of the above:** an Android launch or force-stop, a headset
reboot, real MRUK room/anchor UUIDs, physical position against a reference, walking between real
rooms, the two passthrough clips, the safe-cone capture, the XR Simulator's VR Glasses profile, the
dark-room plinth, and visual legibility of the tray. G4 does not close on desk evidence.

---

### 8.12 Phase 5 as work — P5.1 to P5.7 🟡 FIVE OF SEVEN DONE, SIGNER STILL MISSING

Phase 5 is the legitimacy phase: every shipped handshape traced to a named, credited Deaf signer. The
one thing it cannot do from a desk is meet the signer, so the work split cleanly into what could be
built and what is genuinely blocked.

| Task | Status | What exists |
|---|---|---|
| **P5.1** recruit + consent | 🟡 | consent, compensation, credit-option and brief documents in `assets/masters/`; contact log `phases/p0-foundation/signer-pipeline.md`; the send/book/sign steps are human |
| **P5.2** protocol | ✅ | `docs/03-content/CAPTURE-PROTOCOL.md` complete and printable: one-pager, 12+6 shapes, 3 vantages, 3 s holds, naming, manifest row per take, retake rule, 90-minute schedule |
| **P5.3** capture session | ⛔ | `phases/p5-content/session-kit.md` (pre-flight, call plan, 18-shape take sheet, approval table, signer-words sheet, playback); the session needs a person and did not run |
| **P5.4** retarget/clean | 🟡 | all **12** poses authored and validated; six shipped, six deferred in `StreamingAssets/Signs/deferred/`; retarget of real takes and vantage renders blocked |
| **P5.5** credits | 🟡 | `Credits.prefab`, `CreditsPanel` + `CreditsHandle`, bead on the shelf, opens itself once at 20 s, wording read from `set-meta.json`; `P5.5 Check credits` **PASS**; signer approval owed |
| **P5.6** final set | ✅ | 6 shipped / 6 deferred, reason per shape, pairs measured with metric v3 (`phases/p5-content/final-set-decision.md`) |
| **P5.7** blind-spot copy | 🟡 | copy shipped in the panel and documented verbatim in `TRACKABILITY.md` §4.6, forward line with no promised date; signer rewrite owed |

**The set decision (P5.6), and why it is six.** The 70% cut rule cannot fire because `trackability.json`
is still `measured: false`. A rule that needs a measurement cannot promote a shape on a guess, so the
six design-`fine` shapes ship and the six `may-resolve` shapes stay authored, validated and out of
`index.json`. Measured with metric v3 on the actual pose files: **U↔V 26.0°** (exactly on the 26°
threshold), **C↔O 65.0°**, V↔W 118.1°, B↔U 117.0°. All four shipped pairs survive the cut; the deferred
pairs (A↔O 116.7°, I↔Y 133.8°, L↔Y 115.3°, D↔F 133.5°) are not claimed.

**What the credits object does, and does not.** It shows a name only when `set-meta.json` carries one
*and* the state is `signer-verified`; today it shows the state-B sentence instead, which is the truth.
The fixed sentence *"This is practice software, not a replacement for a Deaf teacher."* is appended if
the data omits it, and the check fails if it is missing. The panel is not a settings screen; it rides
the shelf and is opened by pinching its bead.

**Checks run:** compile 0 errors; `P5.5 Check credits` PASS; `validate_poses.py` **34 shapes, 0
problems** (adds `Signs/deferred` to the validated set); `check_trackability.py` 0 violations;
`lint_copy.py` 70 literals in 9 files, 0 violations; scene rebuilt with `Credits` following the shelf.

**Build:** APK 64,470,043 bytes,
`sha256 6366e09a41083ba76ad7975e5f6b553d4b113fd16b10bb1c529623abd40f5d3b`.

**Not run, and not implied:** the signer session, consent signature, compensation payment, credit
wording approval, the signer's descriptions, the signer's rewrite of the blind-spot copy, the vantage
renders, the joint-flip preview, and every device row. G5 stays open; it is the kill-condition gate.

### 8.13 Phase 6 as work — P6.1 to P6.7 ✅ DESK-VERIFIED, DEVICE OWED

**What was built.** The gaze layer (`EyeTrackingProbe`, `GazeHighlighter`, `FingerTarget`, `FingerTargetsBuilder`, `DwellSelector`, `DwellAction`, `EyesOnlyRouter`, `GazeCaption`), the accessibility owner
(`A11ySettings`), the calibration (`Calibration`), the seven-switch tray (`A11yToggleTray`, `A11yToggleBlock`, `CalibrationHandle`), the audio captions (`A11yCaptionPanel`), the HUD (`A11yHud`), and the
scene wiring (`A11yBuilder`). Two new save fields sets: the six switches plus `SaveData.calibration`.

**Three decisions worth reading.**

1. **Not the ISDK building block.** Its `EyeGazeInteractor` needs an eye pose; on Quest 3/3S there is
   none, so it would fall back silently — the exact failure P6.2 is about. The picking is 120 lines we
   own and can make honest.
2. **Eye mode without an eye pose is refused.** The probe may say "eye tracking is enabled" while this
   project binds no eye anchor. `GazeHighlighter.BindProbe` keeps the head ray and says why, so the
   product cannot claim an eye path it is not running.
3. **Calibration stretches the rotation, not the distance.** The first implementation multiplied the
   per-finger mismatch by the calibration factor; a 45°-away hand became 90° away, not 0. The stretch
   now happens to the user's joint rotation before comparison (`Matcher.ScaleRotation`), and the check
   proves a half-range hand reads 0.00° against a 90° target. `bandScale` widens the band ≤1.35 for
   the noise the stretch also enlarges.

**Eyes-only stops short, on purpose.** Dwell reaches every control; it does not lock. A gaze-only
unlock would bank a shape the hand never made. Stated in `ACCESSIBILITY.md` §5/§7, `GAZE-INPUT.md` §5,
and the task's build-status block.

**Checks run:** compile 0 errors; `P6Checks.CheckAll` **106 passed, 0 failed** (gaze 15, honesty 10,
eyes-only 9, calibration 13, toggles 32, reduced motion 11, doc 16). Regressions on the changed matcher
and payoff: `P5.5 Check credits`, `P5.7 Check blind-spot copy`, `ShelfCheck`, `HandRigCheck`,
`PersistenceP43Checks` all pass. Scene rebuilt: `Accessibility` object, 8 tray blocks, 5 fingertip
targets, both panel handles gaze-reachable.

**Build:** APK 64,510,983 bytes,
`sha256 54d6858a0daee6a755e4bb2a9943e5c4291ea4c2aaa28b43cf2a1b80bd087fce`.

**Not run, and not implied:** the 0.4 m/0.6 m finger-precision test, the eyes-still/head-still
discriminator, the eyes-only recording, the restricted-hand calibration video, the per-switch loop
recordings, the reduced-motion recording, the 5-minute quiet session, and the frame-rate target. Every
one is enumerated with its acceptance test in **`docs/00-plan/HUMAN-TASKS.md`** (H14–H22), which is now
the single place the human work lives.

### 8.14 Phase 7 as work — P7.1 to P7.7 🟡 DESK-VERIFIED, DEVICE ROWS OWED

**What was built.** A 16-clip sound set synthesised from SOUND-MAP by `scripts/make_sfx.py` (596 kB,
every peak measured at or under −6 dBFS) with `SoundDirector` playing twelve cues from three pooled
sources; the one spatial onboarding cue (`OnboardingCue`); `PerfSettings` (72 fps target, no shadows, no
reflection probes, FFR medium + dynamic via `OVRManager` reflection) plus MSAA 4× → 2× and every
renderer switched to `ShadowCastingMode.Off`; `BootProbe` writing a real `boot-times.csv` on device;
`HostileGuard` with five written answers (no camera, no surface, no eye tracking, tracking lost, room
changed); the near-miss panel finally placed in the scene; `P7Checks` (131 assertions); and the phase's
three evidence files (`text-budget.md`, `boot-times.csv`, `crash-sweep.md`).

**Four defects the phase found, all fixed.**

1. **The chime was over the map's own ceiling** at −4.1 dBFS, and **ten states were silent**. Ten of
   SOUND-MAP's rows had no clip at all; the chime was a bare tone with no wooden partials and no tail.
2. **The accessibility tray did not know about the field of view.** At 70°×66° it sat ~40° off-axis in
   two directions — reachable by hand, invisible without turning the head, which defeats the eyes-only
   user it exists for. It now has a narrow pose at 26.0°, and `P7.3` measures both profiles.
3. **The near-miss had no object in the scene.** The component, the copy template and eight authored
   hint pairs existed; nothing instantiated them, so the product's signature sentence could not appear.
4. **High contrast was half-wired.** It changed the caption panel and the gaze caption, and nothing
   else — not the HUD lines, not the tray, not the near-miss copy. P7 connected all three and `P7.7`
   asserts the switch reaches them and releases them.

**Also hardened.** The tray's shader lookup could throw `new Material(null)` at `Start` on a stripped
shader; the boot probe's CSV write is wrapped so a probe cannot be the crash it measures; the splash is
static (Unity's logo cannot be removed on this licence, but it can stop animating).

**Checks run:** compile 0 errors; `P7Checks.CheckAll` **131 passed, 0 failed** — which includes the whole
P6 suite inline (106 assertions) — plus `P5.5`, `P5.7`, `ShelfCheck`, `HandRigCheck`, `PersistenceP43`
green in the P6 run. Scene rebuilt with `Polish_Sound`, `Polish_Performance`, `Polish_Boot`,
`Polish_HostileGuard`, `Polish_OnboardingCue`, `NearMiss`.

**Measured on the desk:** 0 realtime lights; 0 shadow-casting renderers; 48 renderers / 24,130 vertices;
biggest texture 64 px; MSAA 2×; worst shelf slot 24.3° at 110° and 19.0° at 70°; narrow tray 26.0°
(1.5° spare); HUD 19.2°; **0 instructional words**; 16/16 clips within the mix ceiling.

**Build:** APK 64,584,347 bytes,
`sha256 6a62ced4b81a9ab4112b07a9a64d5a66a149d2d52ebc49611dec5df1ae84f41f` (version code 14).

**Not run, and not implied:** the 15-minute 3S frame-rate run and 1% low, the thermal curve and the
overlay recording, the 4×-vs-2× comparison, five cold boots and five background cycles, twenty launches
and the hostile conditions on hardware, the VR Glasses loop and the split-screen capture, the chime
heard 50 times, the recorded wooden timbre, and the eight refreshed accessibility recordings. All of
them are **H34–H40** in `docs/00-plan/HUMAN-TASKS.md`. G7 stays unevaluated: six of its seven criteria
are device rows.

### 8.15 Phase 10 as work — P10.1 to P10.5 🟡 DESK-SIDE DONE, GATE NOT EVALUABLE

**What was built.** `scripts/freeze_check.py` — sixteen rows that compare the repository to the freeze
record: APK bytes, version constants, tag, commits after the frozen-at timestamp, tree cleanliness,
archive contents, submission record, video master — and print **NOT RUN** for the three questions that
need an account and a browser. `phases/p10-post/reverify-log.md` with a real dated row. A one-page
judging note. A one-page launch sketch. A post-mortem that does not have hour counts and says so first.
`P10Checks` (41 assertions) guards the artefacts.

**What the first run of the guard found — 4 PASS, 9 FAIL, 3 NOT RUN.**

1. **The artifact is intact.** The APK still hashes to `81b60582…`, version name and code still match the
   record, and nothing was rebuilt. This is the one thing G10 exists to protect and it is green.
2. **The freeze record cannot name its own source.** `apk-hash.txt` was written with `commit: ` and
   nothing after it. Root cause: `P9Checks.ReadCommitHash()` looks for `.git` in the current directory,
   which is `unity/`, while the repository root is one level above. Deliberately **not fixed** — editing
   the freeze tool after the artifact was built is the "one small fix" the archive rule forbids.
3. **The freeze was respected in the bytes and violated in the letter.** Three commits landed after the
   frozen-at timestamp before the phase started (`e232956`, `3f77e69`, `de3c160`) and a fourth
   (`4dab82b`) carried these P10 artefacts, all paperwork, none touching the APK. There is
   also no `v1.0-competition` tag. The log records both rather than reinterpreting them as fine.
4. **There is no entry to protect.** `submitted.md` has no submission ID, invite URL or confirmation
   email; the archive is a template; the only video is a previs. G9 and G10 are red for the same reason:
   the build was scheduled first and the conversion work was budgeted last.
5. **The description understates the build.** `SUBMISSION-COPY.md` says "6 shapes today"; the build ships
   **twenty** (`StreamingAssets/Probe/index.json`: B U V W C O A M N T R S D E F G I L X Y`). Recorded as
   an owner decision: fix before upload if not yet submitted, leave and note if it has been.

**Checks run:** compiles with 0 errors; `P10Checks.CheckAll` green; `P7Checks` 131/131, `P6Checks`
106/106 and `P8Checks` 47/47 unchanged, confirming the phase added no risk to the build. **No APK was
rebuilt** — the artifact's hash and mtime are unchanged, which is the point of a freeze.

**Not run, and not implied:** the channel listing, the invite URL from a clean account, whether the video
is public, and the three weekly rows. All of them are **H41–H48** in `docs/00-plan/HUMAN-TASKS.md`. G10's
verdict is **not evaluable**, which is not the same as failed: there is nothing submitted to measure.

---

## 9. What is next

1. **Headset (or simulator) session** — measure σ per joint (P1.5), then re-run `tune_match.py
   --sigma <measured>` and move the thresholds only if the tripwire fires. This is still item 1.
2. **P3 people/device checks** — 20 sloppy attempts for false positives, the three degradation
   scenarios, one stranger for the near-miss, the copy read to a non-signer.
3. **P4 shelf** — code-complete and desk-verified (§8.11). What remains is device evidence: five
   relaunches + reboot, a real room walk, the passthrough pair, and the FoV capture. The panel is
   still opened by the provisional `HonestMirrorPlinth`; re-homing it onto the shelf is intentionally
   not half-built — the binding is one event, so the panel does not change when it moves.
4. **Decide whether K/P ship** (`StreamingAssets/Facing/` → `Signs/`). The measurement says they are a
   clean facing-only pair; the cost is a seven-shape set, a new pair in the demo and a capture-plan edit.
4. **POSE-FORMAT §4.3** — the left-hand frame test; until it runs, `HandSupport.LeftHandSupported`
   stays false and the honest mirror says so.

**Open hardware dependencies, unchanged:** the real tracking-noise figure, 20/20 locks and 0/20
false locks, the payoff filmed, a stranger locking a shape uncoached, the App ID, and
`OVRManager → Hand Skeleton Version = Default (OpenXR)`. The simulator can retire the launch,
tracking, matcher and near-miss risks; it cannot produce noise, passthrough, MRUK, thermal or
silicon fps.
