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
| 6 | **Matcher** | Add the component. Set `palmJointIndex = 0`. Populate the proximal/mid/distal/thumb joint index arrays (see §4) |
| 7 | **DebugOverlay** | A world-space canvas inside the 55° cone showing: current distance in degrees, best-other distance, band, hold progress. **Build this before anything visual** — you cannot tune what you cannot see |
| 8 | **ShelfAnchor** | Empty GameObject with the MRUK anchor logic (P4.1). For P1, just a test cube |

## 4. Joint index arrays — fill these in once

After step 2, the skeleton prints its joint order. Paste the indices into the Matcher:

| Array | Contains |
|---|---|
| `proximalJoints` | wrist-adjacent bones of each finger |
| `midJoints` | first phalanx of each finger |
| `distalJoints` | the outermost bone of each finger |
| `thumbJoints` | all four thumb bones |
| `palmJointIndex` | the wrist |

Whatever is left over keeps the `midWeight` default — that is a safe place to be wrong.

## 5. The P1 smoke test — do this before any product work

| # | Test | Pass |
|---|---|---|
| 1 | Build to the **3S**. Hands visible, passthrough visible | ⬜ |
| 2 | `SignSetLoader` logs `1 shapes loaded` (the EXAMPLE file is skipped) | ⬜ |
| 3 | Move a hand into a B shape while the target is `B` → the debug overlay's distance **rises and falls** | ⬜ |
| 4 | Grab and rotate a free-floating object **with the wrist** — write the verdict to `phases/p1-derisk/rotation-verdict.md` | ⬜ |
| 5 | OVR Metrics overlay: note fps in the empty scene | ⬜ |

**If step 3 fails**, the cause is one of: joint order mismatch between the skeleton and the pose file, rotations read in world instead of local, or the pose file's `jointCount` not matching the skeleton. Fix in that order — the validator catches the third.

## 6. What exists in code already

| File | State | Notes |
|---|---|---|
| `Scripts/Matching/HandPose.cs` | ✅ | JSON model + `SetMeta` |
| `Scripts/Matching/PoseMath.cs` | ✅ | Joint angle, weighted distance, bands |
| `Scripts/Matching/Matcher.cs` | ✅ | Bands, hysteresis, hold, confidence pause, near-miss |
| `Scripts/Matching/SignSetLoader.cs` | ✅ | StreamingAssets → Matcher |
| `Scripts/Hands/SkeletonPoseSource.cs` | ✅ | Reads local rotations; **one TODO**: `ReadConfidence()` |
| `Scripts/Gaze/*`, `Spatial/*`, `UI/*` | ⬜ | P4 and P6 |
