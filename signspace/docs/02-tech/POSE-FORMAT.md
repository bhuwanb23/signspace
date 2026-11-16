# Pose Format

**Status:** ✅ **frozen at schema version 2** (P2.1, 15 Nov 2026) · schema 1 still readable through a
documented migration · **Owner:** W2 · **Phase:** P2 · **Gate:** G2
**Validator:** `python3 scripts/validate_poses.py` — `--self-test` proves it catches broken files
**One implementation of the rules:** `scripts/pose_schema.py`

One file per handshape, in `unity/Assets/StreamingAssets/Signs/`. Read by `HandPose.FromJson()`, written by the capture pipeline, checked by the validator before anything ships.

---

## 1. Why this format

| Requirement | Consequence in the schema |
|---|---|
| Must diff cleanly in git | One shape per file, numbers to 4 decimal places, stable key order |
| Must survive an SDK upgrade | Joints are **named**, not indexed; `jointCount` is data, not an assumption |
| Must be human-readable in a pinch | Flat float array so nothing nests |
| Must carry its own provenance | `source`, `signer`, `captured`, `vantage`, `handedness`, `confidence` all travel with the shape |
| Must be loadable by Unity's `JsonUtility` | **Flat `rot` array** — `JsonUtility` cannot deserialise nested arrays |
| Must survive a rename of its own rules | One definition (`pose_schema.py`), version field, and a migration path (P2.1) |

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

## 2. Schema — frozen at version 2 (P2.1)

```json
{
  "version": 2,
  "id": "B",
  "handedness": "right",
  "signer": "synthetic-neutral-v1",
  "captured": "2026-11-15T00:00:00Z",
  "vantage": "authored-neutral",
  "jointCount": 26,
  "joints": ["Wrist", "Palm", "ThumbMetacarpal", "…"],
  "rot": [0,0,0,1,  0,0,0,1,  "… 4 floats per joint, x,y,z,w, in joints order"],
  "source": "reference-authored",
  "confidence": { "authoring": "high", "measured": null, "note": "…" },
  "family": "spread",
  "description": "Flat hand, fingers extended and together, thumb folded across the palm.",
  "notes": "…"
}
```

| Field | Type | Rule |
|---|---|---|
| `version` | int | **2**. Bump only on a breaking change; unknown versions are refused by the loader and the validator |
| `id` | string | The shape's canonical id. **Must equal the file name** (checked — catches a copied file) |
| `handedness` | string | `right` \| `left` |
| `signer` | string | **Whose neutral these rotations are relative to.** Never empty. `synthetic-neutral-v1` for the authored set (identity neutral by construction); a per-signer label for anything captured from a person — see §4.1 |
| `captured` | ISO-8601 | When that neutral was captured |
| `vantage` | enum | `authored-neutral` \| `first-person-palm-down` \| `first-person-other` \| `third-person` \| `mirrored` |
| `jointCount` | int | Must equal `joints.Length`, `rot.Length / 4`, and the canonical 26 |
| `joints` | string[] | Unique, **canonical** order (§3) |
| `rot` | float[] | **Flat, x,y,z,w per joint**, `jointCount * 4` values, each normalised within 1%, written in canonical form (`w ≥ 0`) |
| `source` | enum | `reference-authored` \| `probe-reference` \| `signer-verified` |
| `confidence` | object | `authoring`: `high`\|`medium`\|`low` (the author's own certainty). `measured`: `null` until a P1.5 session measured it, then `High`\|`Medium`\|`Low`\|`Never`. `note`: free text. **A guessed `measured` value is an error, not a warning** |
| `family` | string | `spread` \| `closure` — used by the honest mirror |
| `description` | string | One line. Working notes, not product copy |
| `probeOnly` | bool | `true` on P1.5 probe references. **The validator rejects this flag inside the shipped set** |

### 2.1 What changed from version 1, and why it was worth the churn

Version 2 was frozen in P2.1 because P2.1 is the last moment a rename is cheap — the task's own
argument: every downstream system reads this one file, so getting it wrong on Day 3 means rewriting on
Day 8.

| Version 1 | Version 2 | Why |
|---|---|---|
| `jointNames` | **`joints`** | pairs with `rot`; the old name described the encoding, the new one describes the thing |
| `q` | **`rot`** | `q` did not say the order was XY**ZW**, and one ambiguous axis order is a whole day of debugging |
| `neutralCaptured` | **`captured`** | it is the capture date of the *neutral*, and §4.1 now defines that capture precisely |
| — | **`signer`** | the single most important addition. A relative rotation is relative to *somebody*, and a file that does not say who cannot be re-captured, re-verified or trusted |
| — | **`vantage`** | first-person versus third-person changes what a shape looks like when the thumb hides behind the fingers |
| — | **`confidence`** | replaces the probe set's ad-hoc `authoringConfidence`, and adds `measured` (filled by a session, never estimated) |

### 2.2 Migration: version 1 files are still readable

The task's second trap is *"forgetting `version` and having no migration path on Day 9"*. Two migration
paths exist, and they agree:

| Path | What happens | Where |
|---|---|---|
| **On disk** | `python3 scripts/validate_poses.py --migrate` renames the three fields and adds the missing provenance, writing schema 2 | `pose_schema.migrate_v1()` |
| **On load** | `HandPose.FromJson` accepts version 1, applies the same renames in memory, logs a warning, and reports `MigratedFromV1` | `HandPose.MinReadableVersion = 1` |

Two fields **cannot be invented** from a v1 file — `signer` and `vantage`. They are set to the explicit
labels `legacy-v1-file` / `authored-neutral` and the confidence note says *"migrated from schema 1; no
signer review recorded"*. A plausible-looking guess would make an uncertified shape look certified,
which is worse than an obviously-missing value.

The in-memory migration was **verified against a real file**, not just described: a schema-1 `B.json`
was written to a scratch folder, validated (accepted, with the migration warning), migrated with
`--migrate`, and re-validated as schema 2.

### 2.3 What the loader actually enforces

The schema above is a contract. This is the part of it the running code checks — and, for the older
rows, the part it did **not** check before P1.4:

| Rule from §2 | Enforced by | Before P1.4 / P2.1 |
|---|---|---|
| `version` is 1 or 2; anything else refused | `HandPose.MinReadableVersion` / `SupportedVersion`; validator | ⚠️ **not enforced before P1.4** — a version-2 file would have been read as if it were 1, which is the one failure a version field exists to prevent |
| `jointCount == joints.Length == rot.Length / 4` | `HandPose.FromJson`, validator | ✅ |
| every joint name must be a known name | `JointNames.Canonicalize` → `IndexOf` | ⚠️ **not enforced before P1.4** — rotations were read *positionally* |
| **joints are applied in canonical order regardless of file order** | name → canonical index mapping | ⚠️ **the P1.4 find**: a reordered file put every rotation on the wrong joint, silently, while the log said "26/26 loaded" |
| quaternions normalised to 1% | `HandPose.FromJson`, validator | ✅ |
| duplicates rejected | `HandPose.FromJson`, validator | ✅ |
| a file may omit joints; omitted joints stay at **identity (neutral)** | `HandPose.FromJson` warns and leaves them neutral | new in P1.4 |
| **`signer` must be non-empty** | `HandPose.FromJson`, validator | new in P2.1 — a relative rotation with no named neutral cannot be re-captured or trusted |
| **`id` must equal the file name** | validator | new in P2.1 — catches the copied-and-forgotten shape |
| **`confidence.measured` may not be guessed** | validator (enum, no free text) | new in P2.1 — an estimated trackability number is the one thing worse than a blank one |
| **`probeOnly` may not appear in the shipped set** | validator (`folder_is_shipped`) | new in P2.1 — makes P1.5's "probe shapes are not content" decision mechanical instead of remembered |

The lesson from P1.4 is worth keeping next to these: the format's *relative* rotations were designed
carefully and the *order* was never checked, because the authoring tool happened to always emit
canonical order. **The authoring tool having good habits is not the same as the loader enforcing
anything.** P2.1's response was to reduce the number of places the rules are written down to one
(`scripts/pose_schema.py`) and to prove that one place still bites (§6).

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

## 4. Rotations are RELATIVE TO NEUTRAL — and the capture-neutral contract

This is the decision that makes the product work on anyone's hand.

`rot[i]` is the **local rotation of joint i relative to that same joint in the signer's neutral pose**:

```
rot[i] = inverse(q_neutral[i]) * q_captured[i]
```

Consequences:

- A user with shorter fingers produces the same relative angles. **Hand size stops mattering** — the
  trap the task names ("storing absolute world rotations — user hand size then changes the shape") is
  answered by this one equation.
- The neutral capture is per-signer and must be stored with the shape (`signer` + `captured`), or the
  numbers are meaningless.
- A joint left out of the file stays at identity, which *is* neutral (§2.3), so a partial file degrades
  into a partial shape rather than into noise.

### 4.1 The capture-neutral contract, stated exactly

A pose file's numbers are in the signer's neutral frame. For the numbers to be comparable, **the thing
they are compared against must be in the same frame.** There are only two ways that can be true, and
which one applies is decided by the file's `signer` field — not by an assumption hidden in the code:

| `signer` | What the neutral is | What the runtime must do |
|---|---|---|
| `synthetic-neutral-v1` (**all six shipped shapes**) | **identity by construction** — `hand_model.py` authors rotations directly in the canonical joint frame, so the neutral is every joint at `(0,0,0,1)` | **nothing.** Live local rotations are already in that frame, so they are compared directly. This is why `N5` calibration turned out to be optional rather than essential |
| a named person (`signer-01-…`) | a real capture, per §4.2 | **capture the user's own neutral and divide it out** before comparing: `rel = inverse(q_neutral_user) * q_live`. Without that step a real-signer file is compared against a frame it was not written in, and every user is offset by their own wrist angle |

**So the earlier claim "no normalisation of the user is needed at all" is true of the current data and
not of the format.** It holds because every shipped file declares the identity neutral — a property of
the content, verified by `signer` — and it would stop holding the moment a pose is captured from a real
signer, which is exactly what the signer-review step produces. `NeutralCapture` is therefore a **P2.2
deliverable**, not a P2.1 one, and it is specified here so it is not invented later.

Why the identity neutral is a legitimate choice rather than a dodge: OpenXR defines hand joint
orientations anatomically (a joint's frame is built from its own landmarks), so the *local* rotations a
conforming runtime reports are already user-independent to first order. Hand **size** changes joint
*positions*, which this format never stores. That is the reason this format is robust, and it is a
better reason than "it seemed to work".

### 4.2 The neutral capture procedure, precisely

The task fixes four things — relaxed open hand, palm down, 2 s, averaged over 60 frames — and one
arithmetic detail has to be settled to make them compatible: **at 72 Hz a 2 s hold is ~144 frames, so
60 frames is a subset, not a synonym.** The window is the **last 60 frames of the 2 s hold**, which
discards the settle transient at the start of the hold and ends while the hand is still verifiably
still.

| Step | Rule | Why |
|---|---|---|
| Posture | relaxed open hand, fingers straight and comfortably apart, **palm down** (palm normal pointing at the floor) | "relaxed" keeps the capture reachable by everyone; palm-down fixes the wrist angle that the whole format depends on |
| Hold | **2.0 s** with no wrist rotation, tracked continuously | long enough to average, short enough that nobody drifts |
| Window | **the last 60 frames** of the hold (~0.83 s at 72 Hz) | removes the settle transient; 60 samples is enough for the angular noise to average out |
| Averaging | component-wise mean, **after sign-aligning each sample to the first one** (`dot < 0 → negate`), then renormalise | q and −q are the same rotation; averaging without sign alignment can cancel a quaternion to near-zero and produce a meaningless "neutral" |
| Tracking gate | reject the capture if the hand was untracked for **> 20%** of the window, or if `spreadDeg` (the RMS angular deviation of the samples from their own mean) exceeds **3°** | an untracked or moving hand produces a neutral that biases *every* match until the next capture — the most expensive silent failure in the pipeline |
| Failure handling | one automatic re-ask, then fall back to **no user neutral** (treat as identity) and log it | a refused capture must never block the app; a wrong neutral silently degrades matching, an absent one degrades nothing |
| Storage | `persistentDataPath/neutral-<hand>.json`, per hand, written once and reused until re-captured | the neutral belongs to the person, not the session; recapturing every launch would make two sessions incomparable |
| Format | the neutral file is a pose file plus a quality record (§4.2.1), validated by `pose_schema.validate_neutral` | a neutral is the input to every match, so it gets the same scrutiny as content |

#### 4.2.1 The neutral file

```json
{
  "version": 2,
  "handedness": "right",
  "signer": "signer-01-2026-11-15",
  "captured": "2026-11-15T10:12:04Z",
  "frameCount": 60,
  "heldSeconds": 2.0,
  "joints": ["Wrist", "Palm", "…"],
  "rot": [0,0,0,1, "…"],
  "spreadDeg": 0.8,
  "droppedFrames": 0
}
```

`frameCount` must be ≥ 60 and `spreadDeg` non-negative; both are checked, because they are the two
numbers that say whether the capture is worth trusting.

### 4.3 The left-hand mirroring rule

The shipped set is right-handed. Rather than author a second set by hand, a left pose is derived from a
right one by **mirroring across the sagittal plane** (`x = 0`):

```
rot  (x, y, z, w)  ->  (x, -y, -z, w)
positions (x, y, z) -> (-x, y, z)
```

This is a derivation, not folklore: a reflection in the plane `x = 0` is `M = diag(-1, 1, 1)`, a
rotation `R` becomes `M R M` — still a rotation — and conjugating a unit quaternion by that reflection
gives exactly the form above. `pose_schema.mirror_right_to_left()` implements it.

**The assumption, stated because it is testable:** this holds when the left hand's joint frames are
themselves mirrored copies of the right hand's, which is how skeletons are usually defined. If OpenXR
keeps the right-hand axis convention on left-hand joints instead, a mirrored file is subtly wrong in a
way only the device can show.

**So it gets a test, and a fallback:**

1. Mirror `B` and `V`, set `handedness: "left"`, `source: "reference-authored"`, and add
   `"mirrorOf": "B"` to `notes`.
2. On device, perform the *right*-hand `B` with the left hand. It should **not** lock — if it does, the
   mirror is a no-op and the joints are already same-handed.
3. Perform the mirrored left `B`. It should lock. If it does not, the axis convention differs: set
   `handedness` honestly and **author the left poses directly** with `make_pose.py --left`, rather than
   shipping a mirror that is wrong for everybody.

Until that test runs, **no mirrored file ships** — this section carries the rule, P2.7 carries the
decision, and the validator would accept a wrong mirror without complaint because a mirrored quaternion
is still a perfectly normalised quaternion. That is precisely the class of error a schema cannot catch.

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
python3 scripts/validate_poses.py              # both folders: Signs (shipped) and Probe
python3 scripts/validate_poses.py --self-test  # prove the rules still bite (29 cases)
python3 scripts/validate_poses.py --migrate    # upgrade schema-1 files in place
```

**What the folder run checks:** parseable JSON · every required field from §2 · `id` matches the file
name · `joints` length == `jointCount` == `rot.Length / 4` · no duplicate joint names · the joint set is
canonical **and in canonical order** · every quaternion normalised to 1% · `signer` non-empty ·
`vantage`/`source`/`confidence` inside their enums · `confidence.measured` is `null` or a measured
rating (never prose) · `probeOnly` never appears in the shipped set · **`index.json` lists exactly the
shapes that are present**.

**A malformed pose does not crash the app — it silently never locks.** That is the worst available
failure, which is why the validator runs before the build, not after.

### 6.1 Why `--self-test` exists

P2.1's done-when says *"validator script runs and catches a deliberately broken file"*. Asserting that
by hand once is not a control: it proves the validator worked on the day somebody typed the command.
`--self-test` asserts it every time, and it does so two ways:

| Layer | Cases | What it proves |
|---|---|---|
| **Mutated payloads** | 23 | each frozen rule is exercised by breaking exactly that rule against a known-good payload: missing `signer`/`vantage`/`confidence`/`captured`, bad enums, `jointCount 25`, short `rot`, duplicate joint, unknown joint, swapped order, unnormalised quaternion, `NaN`, a legacy key in a v2 file, an unknown version, `id` mismatched to the filename, `probeOnly` in the shipped set |
| **Committed broken fixtures** | 3 | the on-disk files in `scripts/fixtures/broken/` are rejected: `broken-joint-count.json`, `broken-unnormalized-quat.json`, `broken-no-version.json` |
| **Behaviour that must NOT fail** | 3 | an unmutated payload passes; `w < 0` warns without rejecting; a schema-1 payload is **readable**, reported, and migrates into something that validates |

It has already earned its keep: the first run failed on the schema-1 case, because `validate()` checked
the version-2 required fields *before* migrating — i.e. it would have rejected every legacy file for
missing fields that migration exists to supply. That bug is in a code path no shipped file exercises,
which is exactly the kind of thing a hand-run test never finds.

### 6.2 Current state

```
28 shapes, 0 problems        # 6 shipped + 20 probe references + 2 facing probes (K/P)
SELF-TEST PASSED: 29 cases
```

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
