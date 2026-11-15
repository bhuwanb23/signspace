# P1.1 — hand visual log

**Task:** P1.1 · custom hand visual running at frame rate · **Gate fed:** G1
**Started:** 15 Nov 2026 · **Status:** 🟡 code complete, **device verification pending**

> This file is the P1.1 record. P1's own trap list says an unwritten verdict is a rumour by Day 4, so
> every claim below is either a number from the code, a decision with a reason, or explicitly marked
> **unverified**.

---

## 1. What the phase found before writing anything

P1.1 assumed a pipeline existed and needed a better mesh. It did not. Two greps, before any edit:

| Grep | Result | Consequence |
|---|---|---|
| callers of `SkeletonPoseSource.AutoBind(…)` | **0** | no tracked skeleton had ever been bound; the matcher was being fed an empty joint array |
| implementations of `IHandStateProvider` | **0** (interface + 3 tooltips only) | `ReferenceHand.IsPinching()` always returned `false` → **grab could never fire** |

So P1.1 was re-scoped, with a reason: the deliverable is not "a prettier hand", it is **a tracked hand
that reaches the matcher and the grab**. The mesh already existed: `HandVisual.cs` generates one
procedurally from the same canonical joints the pose files use, which is also why P1.1's "import a
low-poly mesh" step is not followed — importing one would put a second, differently-scaled hand in the
project and require the 26→20 bone remap that P1.1's own trap list calls "silently mangling the
fingers".

**The comment in `HandVisual.cs` was also wrong**, and is corrected: it claimed 25 renderers per hand.
`SegmentLengths` has 20 entries (palm + 4 per long finger + 3 thumb), so it is **20 per hand, 40 for
both** — a third of the 120 draw-call budget rather than the "~50" the comment asserted.

## 2. What was built

| File | Change | Why |
|---|---|---|
| `Scripts/Hands/HandVisual.cs` | `bones[]`, `ParentIndices[]`, **`FitToSkeleton(Transform[])`**, `SetVisible`, `CountCost()` | the visual could only replay authored *rotations*; a tracked hand needs real bone *positions* and real bone *lengths*, or the drawn hand is our constants rather than the user's hand |
| `Scripts/Hands/MetaHandSource.cs` | **new** — SDK discovery, canonical mapping, palm synthesis, geometric pinch, confidence, joint-report file | this is the bridge that did not exist. Implements `IConfidenceProvider` and `IHandStateProvider`, which is what finally makes grab possible |
| `Scripts/Hands/SkeletonPoseSource.cs` | `BindCanonical(Transform[], string)` | SDK bones are named for the SDK, so name matching maps nothing; the source now accepts an array already in canonical order and still reports every joint it could not place |
| `Scripts/Hands/SkeletonPoseSource.cs` | dangling joint → `continue` instead of `return` | one destroyed transform used to abort the whole matcher update, silently |
| `Scripts/UI/HandConfidenceReadout.cs` | **new** — dev-only IMGUI readout | P1.1 step 4. Shows tracked/confidence/pinch/mapped joints/cost, so the two P1.1 unknowns are answerable by looking rather than by pulling a file |
| `Editor/HandRigBuilder.cs` | **new** — emits `Prefabs/HandVisual.prefab`, builds both hands, wires the reference hand | the named P1.1 artefact, generated rather than hand-written YAML |
| `Editor/SceneBuilder.cs` | calls `HandRigBuilder.BuildHandRig()` | a batch build must produce a rig with hands, not one without |
| `docs/02-tech/POSE-FORMAT.md` | new §1.1 | the joint-name question, with the two SDK layouts side by side and the `adb` command that pulls the authoritative list |

## 3. Decisions, with reasons

**Reflection, not `using Oculus.Interaction.Input`.** `OVRSkeleton` has moved namespaces across SDK
versions, and the project's stated rule (`SkeletonPoseSource` header) is that an SDK upgrade costs zero
build time. Reflection keeps the project compiling with no SDK present and survives a rename. The price
is a runtime error instead of a compile error if a member changes — which is why the discovery failure
is logged loudly and the joint report is written to a file. Cost: resolved once; the per-frame path is
array copies.

**Pinch is geometric, not `OVRHand.GetFingerIsPinching`.** Thumb-tip to index-tip distance with
hysteresis (close ≤ 25 mm, open > 35 mm). It works identically on the legacy rig, the OpenXR rig and
the simulator, and it cannot be silently unavailable the way an unassigned provider was. A single
threshold chatter the grab at the boundary; two thresholds do not.

**Palm is synthesised only when absent, and loudly.** The legacy layout has no palm joint. Rather than
map a finger bone into its slot — which would put a palm capsule on a knuckle — the palm is created at
the wrist↔middle-metacarpal midpoint, and the report says `SYNTHESISED: Palm`. This is precisely the
26-vs-20 trap, handled by reporting instead of guessing.

**Two materials.** Reference hand warm neutral, tracked hand cool pale. The exercise is "make yours
look like this one", so they must be distinguishable at a glance. Neither is red — `UX-STATES.md` §
reserves red for a failure state this product does not have.

**Prefab generated, not committed as YAML.** A `.prefab` is GUID-bearing YAML; hand-writing one is an
afternoon spent debugging an asset. `HandRigBuilder` emits it, so the repo holds a recipe and the
prefab is an artefact of running it — the same rule `SceneBuilder` already follows.

## 4. Measured vs unverified

| Claim | Status |
|---|---|
| 20 renderers per hand, 40 for both | ✅ **counted** in `SegmentLengths` |
| Triangle count | 🟡 printed at runtime by `CountCost()` and shown in the readout — **not written down here**, because a guessed triangle count in this file is the same mistake as the "25 renderers" comment |
| Draw-call cost against the 120 budget | 🟡 40 of 120 before any content; expected to fall further under SRP batching, unmeasured |
| Both hands track with no visible lag | ⬜ **device only** — this is G1 criterion 1 |
| Frame-time impact | ⬜ **device only** — OVR Metrics Tool, both hands + passthrough, 10 min |
| Joint count and names | 🟡 **instrumented**; the expected values are now documented from Meta's own table — see §4a. `hand-joint-report.txt` confirms them on the device |
| The whole file compiles | ⬜ **no Unity in the build sandbox** — first compile is on the owner's machine |

## 4a. The two numbers, answered from documentation (15 Nov 2026)

The blank was: *what are N and M?* They are not free parameters — Meta publishes both skeletons, and
`OVRSkeleton.BoneId` carries the ids for each. So the two possible answers are known in advance, and the
device run only has to say **which** one is happening.

Meta's *Hands Setup* page, "Choose a Hand Skeleton Version" (updated 14 Oct 2026):

| | **OpenXR Hand Skeleton** | **OVR Hand Skeleton** |
|---|---|---|
| Forearm | not included | included |
| Palm | **included** | **not included** |
| Index / Middle / Ring | **metacarpal** + proximal, intermediate, distal, tip | proximal, intermediate, distal, tip (**no metacarpal**) |
| Pinky | metacarpal + proximal, intermediate, distal, tip | metacarpal + proximal, intermediate, distal, tip |
| Thumb | metacarpal, proximal, distal, tip | **trapezium**, metacarpal, proximal, distal, tip |
| **Total** | **26 joints** | **24 joints** |
| Alignment | z-forward, y-up, both hands | x-forward, y-up right; mirrored left |
| Default | **the default since SDK v71** — so what a v207 build gets unless the dropdown is changed | opt-in via *OVRManager → Hand Skeleton Version* |

That gives a **predicted** log line for each case:

| If the device reports | `M` (device bones) | `N` (canonical mapped) | Missing | Verdict |
|---|---|---|---|---|
| **OpenXR (expected)** | **26** | **26/26** | none | green — P1.1's target |
| OVR legacy | 24 | **23/26** | IndexMetacarpal, MiddleMetacarpal, RingMetacarpal (+ Palm synthesised from wrist/middle, so it is counted) | **amber, and a real problem** — see below |
| anything else | whatever it says | `N < 26` with names listed | named in the report | investigate before tuning |

**Why the legacy case is amber and not cosmetic.** `hand_model.py` puts the abduction (spread) on the
*metacarpal* of each long finger (`ABDUCTION_JOINT = {f: chain[0] ...}`). Those are exactly the three
joints the legacy skeleton does not have, and spread is what distinguishes U from V — the 5.4° → 26.0°
improvement the whole matcher was redesigned around. So on the legacy skeleton the shipped poses lose
the joint that carries their most important signal, and it would present as "the sensor cannot tell U
from V". It is a configuration bug, not a sensor result. **The fix is OVRManager → Hand Skeleton
Version → OpenXR, not a threshold change.**

### The bug this found in code that was already shipped

`JointNames.Canonicalize` stripped `hand_`, `finger_` and `joint_` — but **not `xrhand_`**. Since v207
reports `XRHand_Palm`, `XRHand_IndexProximal`, …, that would have mapped **0 of 26** joints and looked
exactly like total tracking failure. Fixed, in both `JointNames.cs` and the Python mirror in
`scripts/hand_model.py`, along with a second error in the same table: it mapped `Hand_Index1` to
`IndexMetacarpal`, but per Meta's table the legacy `Index1` **is the proximal phalange** — so every
finger would have been rotated one joint off its real axis. Both tables now agree, verified by script:
`python3 scripts/validate_poses.py` still reports 6 shapes, 0 problems.

Sources: [Hands Setup — Choose a Hand Skeleton Version](https://developers.meta.com/vr/documentation/unity/unity-handtracking-hands-setup/) ·
[OVRSkeleton.BoneId](https://developers.meta.com/horizon/reference/unity/v72/class_o_v_r_skeleton/) ·
[Interactions Setup — hand skeleton bone ids](https://developers.meta.com/vr/documentation/unity/unity-handtracking-interactions/) ·
[Khronos `XrHandJointEXT`](https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#XR_EXT_hand_tracking)

## 5. The device procedure (about 15 minutes, when the headset is in hand)

1. Build and install (P0.4's path). `adb logcat -s Unity` while it starts.
2. Expect, in the log:
   `[MetaHandSource:Right] hand Right · layout OpenXR (26-joint) · bound 26/26 canonical joints from 26 device bones`
   — match it against §4a. If it says `OVR legacy (24-joint)` and `23/26`, the skeleton version is
   wrong, not the tracker.
3. Pull the authoritative list:
   `adb shell run-as com.signspace.signspace cat /sdcard/Android/data/com.signspace.signspace/files/hand-joint-report.txt`
   Paste the `--- what the device reported ---` block into `POSE-FORMAT.md` §1.1. **That closes
   "joint count and names documented".**
4. Look at the readout: both hands `tracked`, confidence 1.00, `joints 26/26`, pinch toggling `CLOSED`.
5. OVR Metrics Tool, 10 minutes, both hands tracked, passthrough on → the fps number for P1.8.

**If N < 26:** the missing list names the joints. Do not tune thresholds — a shape whose joints are
missing can never lock, and `TRACKABILITY.md` will show it as a failure of the *device* rather than of
the pose.

## 6. Left open on purpose

- **`com.unity.render-pipelines.universal` is not in `Packages/manifest.json`**, though
  `TECH-STACK.md` names URP as the pipeline. The material helper therefore probes for a unlit shader in
  order and logs which one it used. Adding URP needs a version that matches Unity 6000.3.28f1 exactly;
  pinning a wrong version breaks the build, so it is flagged rather than guessed.
- **Rest pose.** The matcher reads `localRotation` from the device bones, but the pose files assume an
  identity rest pose. `Editor/RestPoseDump.cs` + `make_pose.py --neutral` is the existing answer; it
  becomes a real step the moment the device reports its rest rotations.
- **`HandVisual.Build()` is called at edit time by the builder and at runtime only if `joints` is
  empty**, so the serialised prefab is used on device with no rebuild cost.

## 7. Change log

| Date | Change |
|---|---|
| 15 Nov 2026 | Log opened. Code complete and unverified; measurement instruments in place; the two numbers that close P1.1 are `N/26` mapped joints and the device bone count `M`. |
