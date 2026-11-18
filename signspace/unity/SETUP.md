# Unity setup — from empty project to the P1 smoke test

**Time:** ~60 min if the toolchain is installed. **Produces:** the G0 build, then everything P1 needs.

---

## 1. Project

| Step | Setting |
|---|---|
| Template | 3D (URP) |
| Location | `signspace/unity/` (this folder already exists) |
| Player → Package name | the package name recorded in `phases/p0-foundation/toolchain.md` |
| Player → Minimum API | Android 12 (API 32) |
| Player → Target API | Android 14 (API 34) |
| Player → Scripting backend | IL2CPP · ARM64 |
| Player → Graphics APIs | Vulkan → GLES3 (or GLES3 only if unstable) |
| Player → Color space | Linear |
| Player → Active input handling | **Both** (ISDK needs the new input system) |
| XR Plug-in Management → Android | Meta XR features enabled; hand tracking + passthrough checked |

## 2. SDK

Import **Meta XR All-in-One v207**. Record the resolved versions in `docs/02-tech/TECH-STACK.md` §1.

> The competition page states: *"Unity: Meta XR SDKs v81+ (v207) All-in-One XR."* v207 is correct for this competition.

## 3. Scene → `Assets/_Project/Scenes/Main.unity`

| # | Object | Setup |
|---|---|---|
| 1 | **OVRCameraRig** | Drag the Meta XR rig prefab into an empty scene. On `CenterEyeAnchor` set clear flags appropriately and enable passthrough on the rig's `OVRPassthroughLayer` |
| 2 | **Hands** | Enable hand tracking on the rig. **Replace the stock hand visual** with your own 26/21-joint skinned mesh (P1.1) |
| 3 | **ReferenceHand** | A second instance of the hand mesh, positioned ~0.4 m in front, not driven by tracking. This is the shape the user rotates |
| 4 | **SkeletonPoseSource (right)** | Assign the tracked hand's joint transforms **in SDK order, wrist first**. Assign `matcher` reference |
| 5 | **SignSetLoader** | Assign `matcher`. Leave `folder = Signs` |
| 6 | **Matcher** | Add the component and assign `Weights.asset`; joints are mapped by name (see §4) |
| 7 | **DebugOverlay** | A world-space canvas inside the 55° cone showing: current distance in degrees, best-other distance, band, hold progress. **Build this before anything visual** — you cannot tune what you cannot see |
| 8 | **ShelfAnchor** | Empty GameObject with the MRUK anchor logic (P4.1). For P1, just a test cube |

## 4. Joint index arrays — **no longer a manual step**

**You do not paste joint indices into the Matcher any more.** `SkeletonPoseSource` reads the joint
transforms' **names**, matches them to the canonical joint set in `JointNames.cs`, and builds the
mapping itself. The legacy Oculus hand layout and the OpenXR one differ (the wrist and the palm are
swapped, and "Pinky" is called "Little"), and the name matching handles both.

What to do instead:

1. Assign the tracked hand's joint transforms to `SkeletonPoseSource.joints`. **Order does not
   matter** — only the names do.
2. Read the log. It prints how many joints were mapped and lists any it did not recognise.
   A clean log means the mapping is right.
3. The debug overlay shows `joints OK` or `joints PARTIAL`.

**If a joint is reported MISSING or UNRECOGNISED**, add the spelling to the alias table in
`JointNames.cs` and to `scripts/hand_model.py`. That is a two-line fix, and it is the only reason a
shape would ever silently fail to match.

The per-joint weight arrays are gone from the Matcher. They were the mechanism by which the finger
spread signal was diluted — see [`../phases/p2-core-loop/matcher-tuning.md`](../phases/p2-core-loop/matcher-tuning.md).

## 5. The P1 smoke test — do this before any product work

| # | Test | Pass |
|---|---|---|
| 1 | Build to the **3S**. Hands visible, passthrough visible | ⬜ |
| 2 | `SignSetLoader` logs `6 shapes loaded, 0 failed` | ⬜ |
| 3 | Move a hand into a B shape while the target is `B` → the debug overlay's distance **rises and falls** | ⬜ |
| 4 | Grab and rotate a free-floating object **with the wrist** — write the verdict to `phases/p1-derisk/rotation-verdict.md` | ⬜ |
| 5 | OVR Metrics overlay: note fps in the empty scene | ⬜ |
| 6 | The debug overlay says `joints OK` — if it says `PARTIAL`, fix `JointNames.cs` before anything else | ⬜ |
| 7 | Hold a **V** while the target is **U** → the near-miss fires and names `V` | ⬜ |

**If step 3 fails**, the cause is one of: joint names not matching (step 6 will say so), rotations
read in world instead of local, or the rig's rest pose not being identity (dump it with
`RestPoseDump` and regenerate the poses with `make_pose.py --neutral`). Fix in that order.

**If step 7 fails** and step 3 passes, the metric is fine and the near-miss rule is mis-tuned —
inspect the actual `NearMiss.Evaluate` lock-band, ambiguity and stable-window conditions against the measured distances.

## 6. What exists in code already

| File | State | Notes |
|---|---|---|
| `Scripts/Matching/HandPose.cs` | ✅ | JSON model + `SetMeta` |
| `Scripts/Matching/PoseMath.cs` | ✅ | **v2 metric** — per-finger chain angle, max across fingers |
| `Scripts/Matching/Matcher.cs` | ✅ | Bands, hysteresis, hold, confidence pause, near-miss |
| `Scripts/Matching/JointNames.cs` | ✅ | **new** — name-based joint alignment for both SDK layouts |
| `Scripts/Matching/SignSetLoader.cs` | ✅ | **rewritten** — `UnityWebRequest`, works inside the apk on Android |
| `Scripts/Hands/SkeletonPoseSource.cs` | ✅ | Reads local rotations, maps joints by name. **One TODO**: `ReadConfidence()` |
| `Scripts/Core/AppStateMachine.cs` | ✅ | The 16-state machine |
| `Scripts/Core/SaveSystem.cs` | ✅ | Anchor-keyed persistence |
| `Scripts/UI/DebugOverlay.cs` | ✅ | The live numbers, in the cone |
| `Scripts/Gaze/*`, `Spatial/*` | implemented | P4/P6 code exists; device verification remains open |
| `Editor/SceneBuilder.cs` | ✅ | **new** — builds the scene from a script |
| `Editor/BuildScript.cs` | ✅ | **new** — every Player setting as code, plus the APK hash |
| `Editor/RestPoseDump.cs` | ✅ | **new** — dumps the rig rest pose |

### Building it

```powershell
# once the Unity licence is active
pwsh -File scripts/setup_unity_project.ps1          # create the project, merge packages, compile
"E:\unity\Editors\6000.3.28f1\Editor\Unity.exe" -batchmode -quit -projectPath unity `
  -executeMethod Signspace.Editor.BuildScript.BuildAndroid
```

The scene and the build are both commands, so the submission APK on Day 4 is produced by exactly
the same process as the test APK on Day 1.
