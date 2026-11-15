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
  "jointCount": 21,
  "neutralCaptured": "2026-11-15T18:20:00Z",
  "source": "reference-authored",
  "jointNames": ["WristRoot", "Thumb1", "Thumb2", "Thumb3", "Index1", "…"],
  "q": [0,0,0,1,  0,0,0,1,  "… 4 floats per joint, in jointNames order"]
}
```

| Field | Type | Rule |
|---|---|---|
| `version` | int | `1`. Bump only on a breaking change; the loader refuses unknown versions |
| `id` | string | The shape's canonical id: `B`, `U`, `V`, `W`, `C`, `O` |
| `handedness` | string | `right` or `left` |
| `jointCount` | int | Must equal `jointNames.Length` and `q.Length / 4` |
| `neutralCaptured` | ISO-8601 | When that signer's neutral was recorded. **All rotations are relative to it.** |
| `source` | string | `reference-authored` \| `signer-verified` — mirrors `set-meta.json`, per shape |
| `jointNames` | string[] | Unique, SDK order, wrist first |
| `q` | float[] | **Flat**: `x,y,z,w` per joint, `jointCount * 4` values. Each must be normalised to 1% |

## 3. The joint set

Meta's hand tracking reports the wrist plus four bones per finger. **The exact count is `[VERIFY ON DEVICE]` and the schema does not care** — `jointCount` is authoritative, and every consumer iterates rather than assuming a length.

Expected names, in SDK order:

```
WristRoot
Thumb1  Thumb2  Thumb3
Index1  Index2  Index3
Middle1 Middle2 Middle3
Ring1   Ring2   Ring3
Pinky1  Pinky2  Pinky3
```

Plus whatever terminal/forearm joints the SDK adds — include them, name them, and weight them low.

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

Checks: parseable JSON · required keys · `jointNames` length == `jointCount` · `q` length == `jointCount × 4` · no duplicate joint names · every quaternion normalised to 1%.

**A malformed pose does not crash the app — it silently never locks.** That is the worst available failure, which is why the validator runs before the build, not after.
