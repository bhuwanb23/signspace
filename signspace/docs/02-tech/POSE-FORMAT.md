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
