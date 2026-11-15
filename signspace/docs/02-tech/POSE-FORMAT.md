# Pose Format

**Status:** ✅ frozen (v1) · **Owner:** W2 · **Phase:** P2 · **Gate:** G2
**Validator:** `python3 scripts/validate_poses.py`

One file per handshape, in `unity/Assets/StreamingAssets/Signs/`. Read by `HandPose.FromJson()`, written by the capture pipeline, checked by the validator before anything ships.

---

## 1. Why this format

| Requirement | Consequence in the schema |
|---|---|
| Must diff cleanly in git | One shape per file, numbers to 4 decimal places, stable key order |
| Must survive an SDK upgrade | Joints are **named**, not indexed; `jointCount` is data, not an assumption |
| Must be human-readable in a pinch | Flat float array so nothing nests |
| Must carry its own provenance | `source`, `neutralCaptured`, `handedness` travel with the shape |
| Must be loadable by Unity's `JsonUtility` | **Flat `q` array** — `JsonUtility` cannot deserialise nested arrays |

### 1.1 What the device actually calls them *(added in P1.1)*

The canonical names above are **ours**. Meta's SDK uses its own, and it has used two different sets:

| Layer | What it calls joint 7 | Bone count per finger | Palm joint |
|---|---|---|---|
| OpenXR `XR_EXT_hand_tracking` | `IndexProximal` | 5 (metacarpal + 3 + tip) | yes |
| Legacy Oculus | `Hand_Index1` | 4 (3 + tip) | **no** |

Rather than pick one and hope, the mapping is **by name with an alias table**
(`JointNames.Canonicalize`, which strips `Hand_`, `_01`, separators and case), and the mismatch is
**reported, never guessed**:

| Where | What it does |
|---|---|
| `JointNames.cs` | alias table, mirroring `scripts/hand_model.py`; `Thumb1` ≡ `ThumbMetacarpal`, `Pinky2` ≡ `LittleProximal`, … |
| `SkeletonPoseSource.ResolveJointMapping()` | maps by `Transform.name`; logs `MISSING:` and `UNRECOGNISED:` for every joint that does not land |
| `MetaHandSource` | maps by the SDK's `OVRBone.Id` enum name, falls back to the transform name, and **synthesises a `Palm`** at the wrist↔middle-metacarpal midpoint only when the device genuinely reports none |
| `MetaHandSource` | dumps the device's own bone list to `persistentDataPath/hand-joint-report.txt` on first successful bind |

**What the device should report (predicted from Meta's table, 15 Nov 2026):**

| Skeleton version | `OVRSkeleton.Bones` count | Canonical joints mapped | Missing |
|---|---|---|---|
| **OpenXR** (the default since SDK v71) | **26** | **26/26** | none |
| OVR legacy (opt-in via *OVRManager → Hand Skeleton Version*) | 24 | **23/26** | `IndexMetacarpal`, `MiddleMetacarpal`, `RingMetacarpal` |

The legacy case is a **configuration problem, not a sensor result**: the spread that separates U from V
is authored on the metacarpal (`hand_model.py`, `ABDUCTION_JOINT = chain[0]`), and those are exactly the
three joints the legacy skeleton lacks. `MetaHandSource` logs a warning naming the OVRManager dropdown
when it detects `Hand_*` ids instead of `XRHand_*`.

**How the real list gets into this document:** run the app once on the headset and pull the file —

```
adb shell run-as com.signspace.signspace cat     /sdcard/Android/data/com.signspace.signspace/files/hand-joint-report.txt
```

That file is the authority. This table is the map. **Nothing in the pose files changes if the device
turns out to use the legacy layout** — the names are the compatibility layer, which is why §1 said
"named, not indexed" in the first place.

---

## 2. Schema

```json
{
  "version": 1,
  "id": "B",
  "handedness": "right",
  "jointCount": 26,
  "neutralCaptured": "2026-11-15T00:00:00Z",
  "source": "reference-authored",
  "family": "spread",
  "description": "Flat hand, fingers extended and together, thumb folded across the palm.",
  "jointNames": ["Wrist", "Palm", "ThumbMetacarpal", "…"],
  "q": [0,0,0,1,  0,0,0,1,  "… 4 floats per joint, in jointNames order"]
}
```

| Field | Type | Rule |
|---|---|---|
| `version` | int | `1`. Bump only on a breaking change; the loader refuses unknown versions |
| `id` | string | The shape's canonical id: `B`, `U`, `V`, `W`, `C`, `O` |
| `handedness` | string | `right` or `left` |
| `jointCount` | int | Must equal `jointNames.Length` and `q.Length / 4` |
| `neutralCaptured` | ISO-8601 | When the neutral pose was captured. **All rotations are relative to it.** |
| `source` | string | `reference-authored` \| `signer-verified` — mirrors `set-meta.json`, per shape |
| `family` | string | `spread` \| `closure` — used by the honest mirror panel |
| `description` | string | One line. Working notes, not product copy. |
| `jointNames` | string[] | Unique, **canonical** order (see §3) |
| `q` | float[] | **Flat**: `x,y,z,w` per joint, `jointCount * 4` values. Each must be normalised to 1% |

### index.json — the file list

```json
{ "version": 1, "shapes": ["B", "U", "V", "W", "C", "O"] }
```

**Why it exists:** on Android, StreamingAssets live inside the apk, which is a zip archive. A zip
archive cannot be listed at runtime, so the loader is told what is in the folder. It also means the
loader does not have to guess at file names.

`scripts/validate_poses.py` checks that `index.json` lists exactly the shapes that are present, so
the two can never drift apart.

### 2.1 What the loader actually enforces *(verified in P1.4)*

The schema above is a contract. This is the part of it the running code checks, and — as of P1.4 — the
part it did **not** check before:

| Rule from §2 | Enforced by | Before P1.4 |
|---|---|---|
| `version` must be `1`; unknown versions refused | `HandPose.SupportedVersion`; `scripts/validate_poses.py` | ⚠️ **not enforced** — a version-2 file would have been read as if it were 1, which is the one failure a version field exists to prevent |
| `jointCount == jointNames.Length == q.Length / 4` | `HandPose.FromJson`, validator | ✅ |
| every joint name must be a known name | `JointNames.Canonicalize` → `IndexOf` | ⚠️ **not enforced** — rotations were read *positionally* |
| **joints are applied in canonical order regardless of file order** | name → canonical index mapping | ⚠️ **the P1.4 find**: a file whose `jointNames` were reordered would have put every rotation on the wrong joint, silently, while the log said "26/26 loaded" |
| quaternions normalised to 1% | `HandPose.FromJson`, validator | ✅ |
| duplicates rejected | `HandPose.FromJson`, validator | ✅ |
| a file may omit joints; omitted joints stay at **identity (neutral)** | `HandPose.FromJson` warns and leaves them neutral | new — a reduced-joint file loads and the honest mirror can name what was absent instead of inventing it |

The lesson is worth keeping: the format's *relative* rotations were designed carefully and the *order*
was never checked, because the authoring tool happened to always emit canonical order. The authoring
tool having good habits is not the same as the loader enforcing anything.

## 3. The joint set

**26 joints, in the canonical order below.** The order is the OpenXR `XR_EXT_hand_tracking`
layout, because ISDK v207 defaults to the OpenXR hand skeleton.

```
 0  Wrist
 1  Palm
 2  ThumbMetacarpal   3  ThumbProximal   4  ThumbDistal   5  ThumbTip
 6  IndexMetacarpal   7  IndexProximal   8  IndexIntermediate   9  IndexDistal  10  IndexTip
11  MiddleMetacarpal 12  MiddleProximal 13  MiddleIntermediate 14  MiddleDistal 15  MiddleTip
16  RingMetacarpal   17  RingProximal   18  RingIntermediate   19  RingDistal  20  RingTip
21  LittleMetacarpal 22  LittleProximal 23  LittleIntermediate 24  LittleDistal  25  LittleTip
```

**The joint count is not an assumption.** `jointCount` is authoritative and every consumer iterates
rather than assuming a length.

**Joints are matched BY NAME at runtime, not by index.** The legacy Oculus layout differs from the
table above in exactly two ways — the wrist and the palm are swapped, and "Pinky" is called
"Little" — and `JointNames.cs` resolves both. One pose file works on either layout, and nobody has
to paste joint indices into the inspector.

`SkeletonPoseSource` resolves the mapping on the device and **logs loudly** about any joint it
could not map, because a mis-mapped joint is a shape that never matches and no error appears
anywhere.

## 4. Rotations are RELATIVE TO NEUTRAL

This is the decision that makes the product work on anyone's hand.

`q[i]` is the **local rotation of joint i relative to that same joint in the signer's neutral pose**:

```
q_stored[i] = inverse(q_neutral[i]) * q_captured[i]
```

Consequences:

- A user with shorter fingers produces the same relative angles. **Hand size stops mattering.**
- The neutral capture is per-signer and must be stored with the shape, or the data is meaningless.
- The comparison at runtime uses the *user's own* local rotations directly — **no normalisation of the user is needed at all**, which is why `N5` calibration turned out to be optional rather than essential.

**Neutral capture procedure:** relaxed open hand, palm down, held 2 s, averaged over 60 frames, per signer, per session.

## 5. set-meta.json — the authorship switch

Per `ADR-007`. Read at launch, rendered verbatim in the credits object.

```json
{
  "version": 1,
  "authorship": "reference-authored",
  "verificationMode": "pending-review",
  "signerName": "",
  "signerCity": "",
  "verifiedOn": "",
  "creditLine": "…"
}
```

| `authorship` | Meaning | What must also be true |
|---|---|---|
| `reference-authored` | Shaped from published ASL references; no signer has reviewed them | `creditLine` says review is pending. Description must not claim signer authorship |
| `signer-verified` | A named Deaf signer approved every shipped pose on video | `signerName`, `signerCity`, `verifiedOn` filled; description updated |

Flipping the state is a 5-minute edit and rebuild. It is never a silent change: the description and the video must match whatever state ships.

## 6. Validation

```bash
python3 scripts/validate_poses.py
```

Checks: parseable JSON · required keys · `jointNames` length == `jointCount` · `q` length ==
`jointCount × 4` · no duplicate joint names · every quaternion normalised to 1% · **the joint set is
the canonical one the matcher expects, in the canonical order** · **`index.json` lists exactly the
shapes that are present**.

**A malformed pose does not crash the app — it silently never locks.** That is the worst available
failure, which is why the validator runs before the build, not after.

### Authoring

The pose files are generated, never hand-edited:

```bash
python3 scripts/make_pose.py                     # the six shapes + index.json
python3 scripts/make_pose.py --neutral rest.json # fold in a real rig rest pose
```

`scripts/hand_model.py` holds the canonical hand model and the shape definitions;
`scripts/tune_match.py` measures whether the metric can actually tell the shapes apart.

### The one assumption

The authoring tool assumes the hand rig's **rest pose has identity local rotations** — a
straight-finger rig with aligned bones, which is how a hand rig is normally built.

If the shipped rig does not, every stored value carries a constant offset and nothing matches.
This is checked rather than assumed: `unity/Assets/_Project/Editor/RestPoseDump.cs` dumps the rig's
rest pose and `make_pose.py --neutral` regenerates the poses against it. Ten minutes, once.


---

## 7. Measured cost *(P1.4 — filled on device)*

"Content is data" is only a product claim if loading and applying are cheap. Three numbers, measured
separately because they fail for different reasons:

| What | Why it is separate | Measured (Quest) | Budget |
|---|---|---|---|
| **read** | I/O out of the apk — the only one a judge's device can make slow | — | < 20 ms |
| **parse** | JSON + validation + name→canonical mapping | — | < 5 ms |
| **apply** | 26 local rotations onto 26 transforms, single call | — | < 1 ms |
| **warm apply** | mean of 100 applies, because one cold apply reads as 0.0 ms | — | **< 50 µs** |

`PoseProvider` writes these to `persistentDataPath/pose-load-report.txt` on load:

```
adb shell run-as com.signspace.signspace cat \
  /sdcard/Android/data/com.signspace.signspace/files/pose-load-report.txt
```

**Why "warm apply" is the number that matters:** the pose is re-applied whenever the reference hand
changes shape, and a shape sequence in P2 will apply one per lock. At 50 µs the whole content system
costs 0.36% of a 13.9 ms frame even if a pose were applied every single frame.
