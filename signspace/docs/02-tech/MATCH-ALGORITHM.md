# Match Algorithm
**Status:** ✅ v3 — **v2 and v1 retired 16 Nov 2026** · **Owner:** W2 · **Phase:** P2/P3 · **Gate:** G2
**Measured by:** `scripts/tune_match.py` · **Raw output:** [`matcher-tuning-raw.txt`](../../phases/p2-core-loop/matcher-tuning-raw.txt) · **Tuning artefact:** [`matcher-tuning.md`](../../phases/p2-core-loop/matcher-tuning.md)

> **What changed in v3 and why:** v1 measured a weighted mean of per-joint angles across the whole
> hand. That metric cannot see finger spread: U and V differ in one joint, and the mean diluted an
> 18° spread to 3.1° of signal. The near-miss for U/V therefore **could never fire** — and the
> near-miss is the product's entire identity. v2 fixed that by measuring each finger's chain
> separately and combining fingers with a max (U-V: 26.0°), but a chain rotation is all-or-nothing:
> one noisy joint at the end of a finger moves the whole finger. **v3 keeps the per-finger split and
> replaces the chain angle with a weighted mean of the joints inside the finger**, then takes the
> worst finger. U-V stays at 26.0°, and the noise floor drops from 8.5° to 6.7°.

---

## 1. The one design decision

**The output is a distance in degrees, never a percentage.** "You are 9° off" is actionable.
"You are at 82%" is a number to optimise. Degrees describe a shape.

## 2. The metric

```
for each finger f in {thumb, index, middle, ring, little}:
    d_f = Σ (w_j · angle(target_j, current_j)) / Σ w_j      over joints j in f with w_j > 0

distance = max(d_f)                 # a handshape is wrong if any finger is wrong
flat     = Σ (w_j · angle_j) / Σ w_j   # every weighted joint; reporting only, never a threshold
```

**The denominator is the sum of the weights, never the count of joints.** With a count denominator
every distance scales by (total weight / count), so editing one weight in `Weights.asset` would
silently move every distance and every threshold would look broken for no visible reason.

The weights are an asset (`Matching/Weights.asset`), read by both the C# matcher and the Python
tuner: 21 weighted joints, total weight 15.30. The five tip joints carry weight 0 — a fingertip
carries no shape information for these six letters and it carries the most tracking noise. The
weight bands are documented in `matcher-tuning.md` §2.

**The flat mean is measured, and it is rejected by measurement.** It is still computed every frame,
because the debug overlay shows it beside the real distance: "one finger very wrong" must not be
readable as "the shape is close". On U-V it collapses 26.0° to **5.3° — 20% of the real distance**,
with its own noise p95 at 3.7° (score 1.43 against v3's 3.89). It is never used for a band, a hold
or a near-miss.

**Palm orientation is its own additive term (P2.6):**

```
O     = angle( basis(target palm) , basis(current palm) )     # a rotation, never three Euler angles
total = distance + k · max(0, O − orientationSlackDeg)
```

with `k = 1.0`, slack **15°**, and no cap (`orientationPenaltyCapDeg = 0`). The basis is the palm
frame (forward = where the hand points, normal = out of the back of the hand); the knuckle-plane
proxy is attempted first and rejected whenever the metacarpal axes are too parallel or too curled
(measured 5.2–31× noise amplification). `PalmBasis` and `BasisNote` report which basis was used, so
a rejected frame is visible rather than silent.

The old v2 rule was `total = max(distance, O − slack)`, which made orientation a **floor**: perfect
fingers plus 40° of wrong facing scored the same as perfect fingers plus 12° once both were inside
the floor. Additive is the behaviour the product needs — the two errors contribute independently and
neither can hide the other.

## 3. The numbers, and where they come from

All values are mirrored in `Matcher.Tuned*` (one source of truth) and written explicitly onto the
scene by `SceneBuilder`; `BuildScript` fails the build when the scene disagrees.

| parameter | value | measured / reason |
|---|---|---|
| `lockedDeg` | **12°** | noise p95 = 6.7°. Sits 1.8× above the noise and 2.2× below the nearest intended pair. |
| `closeDeg` | **24°** | leaves **2.0°** to the nearest pair (U-V, 26.0°): a hand making the wrong shape of that pair reads *warming*, which is the honest display. |
| `bandReleaseDeg` | **2°** | hysteresis on the way **out**: locked exits at 14°, close at 26°. Entering uses the plain edge. 4 band changes/30 s → 0 in `band_sim.py`. |
| `bandHysteresisSeconds` | **150 ms** | the meter must never strobe. |
| `holdSeconds` | **0.7 s** | inside the locked band before the shape counts. |
| `orientationSlackDeg` | **15°** | wrist jitter for a correctly-facing hand has sd 1.4°, p99 6.4° — 15° is ~10σ of noise. |
| `orientationWeightK` | **1.0** | a degree of facing error costs exactly a degree of match. |
| `nearMissMaxDeg` | **26°** | exactly the nearest intended pair, so a deliberate wrong shape can always be named. |
| `confidenceFloor` | **0.7** | below this the exercise **pauses**; it never fails. |
| `dropoutGraceSeconds` | **0.2** | a dip shorter than this is frozen, not reset. |
| `stallDecayPerSecond` | **0.5** | beyond the grace, progress bleeds at half speed rather than being dumped. |
| `resetAfterSeconds` | **0.5** | leaving the band (or holding a lock) longer than this forfeits progress. |
| `nearMissMarginDeg` | **2°** | the wrong shape must beat the target by this much. |
| `nearMissConfidenceWindowSeconds` | **0.4** | confidence must have held before we may name a shape. |
| `nearMissCooldownSeconds` | **20 s** | minimum gap between two near-miss messages. |

**Nearest intended pair: 26.0° (U-V). Nearest accidental pair: 65.0° (C-O).** There is a 39° gap
between the nearest pair we *want* to fire on and the nearest pair we *never* want to.

## 4. Bands

| band | enter | leave | what the user sees |
|---|---|---|---|
| Locked | `d ≤ 12°` | above `14°` | the shape starts to fill |
| Close | `d ≤ 24°` | above `26°` | the meter leans toward the target |
| Warming | `d > 24°` | below `24°` | nothing, and that is correct |

`d` here is the **total**, palm term included. There is no failure state. No red, no X, no "try
again". See `UX-STATES.md`.

## 5. The hold

- 0.7 s inside the Locked band before the shape counts.
- Confidence below 0.7 → **pause**: the timer neither advances nor resets. The band is frozen too —
  a lost frame is not a verdict (ADR-003).
- A dip shorter than 0.2 s is **frozen** (the timer holds, it does not reset).
- Beyond the grace window, banked progress decays at 0.5 s per second rather than being dumped: a
  nearly-finished attempt survives a 1.5 s dropout, a stale one clears.
- Leaving the band (or holding a lock) for more than 0.5 s forfeits progress.
- After a lock the state **latches** until the hand leaves the band, so a lock never double-fires.

## 6. The near-miss

The signature feature. Fires when **all** of these hold:

1. the band is not Locked (you have not got it, so there is something to say),
2. the best other shape is within `nearMissMaxDeg` (26°),
3. it is strictly closer than the target,
4. it is strictly closer than the **runner-up** other shape,
5. it beats the target by at least `nearMissMarginDeg` (2°),
6. confidence has held above the floor for the last 0.4 s,
7. the 20 s cooldown has elapsed.

Conditions 3–5 compare the **finger term only**. Palm facing is judged by the bands, not by the
diagnosis: a correct handshape with a twisted wrist must not be renamed as a different sign. This is
also why the target's total (with palm) is not used here.

**Conditions 4 and 5 are the "ambiguity is not a diagnosis" rule.** If the hand sits between two
shapes, or is only marginally closer to one of them, the app says nothing. Silence is the default
on any doubt.

> **A trap worth naming.** The rule is *not* "how many shapes fall inside the 26° window". U and V
> are exactly 26° apart, so a perfect U is inside the window twice, and counting qualifiers would
> silence a **correct** diagnosis. The test is whether the winner is unambiguous, not how many
> shapes are nearby.

## 7. Joints are matched BY NAME

The two hand layouts Meta ships disagree about the order:

| | legacy Oculus | OpenXR |
|---|---|---|
| 0 | WristRoot | PALM |
| 1 | Palm | WRIST |
| 2-25 | ThumbBase…PinkyTip | THUMB_METACARPAL…LITTLE_TIP |

Only the first two swap, and "Pinky" is called "Little". `JointNames.cs` aligns them **by name**,
so one pose file works on both and nobody has to paste joint indices into the inspector.
`check_alias_tables.py` proves the Python and C# tables agree: 61 spellings, zero conflicts.

**This is what `POSE-FORMAT.md` promised from the start** — *"Joints are named, not indexed"* — and
the code now actually does it. `SkeletonPoseSource` logs what it could not map, loudly, because a
mis-mapped joint is a shape that never matches and no error appears anywhere.

Pose files are **schema 2** (`version: 2`, flat `rot` array, 26 canonical names); schema 1 is still
read through the documented migration. Rotations are relative to the signer's neutral, so hand size
cancels out.

## 8. Left hand

**Right hand only, and the left hand is disabled rather than half-wired.** One constant —
`HandSupport.LeftHandSupported = false` — gates every left-hand path, so there is no way to
half-enable it. The mirror is exact for the *data* (all 36 ordered pairs score identically after
mirroring) but no pose file can say whether OpenXR's left-hand joint frames are already mirrored;
until the POSE-FORMAT §4.3 device test answers that, a left hand would read 97–176° on a perfect
attempt. See `left-hand-decision.md`.

## 9. Deliberate non-goals

- Absolute joint positions. Relative rotations only.
- User scale normalisation. The comparison is already scale-free.
- A trained ML classifier. Deterministic joint comparison is provably sufficient for six static
  handshapes; an ML model would add a failure mode the product cannot explain.
- Motion or trajectory matching (ADR-004).
- A pass/fail boolean (ADR-003).

## 10. Tuning procedure

1. `python3 scripts/tune_match.py` — measures the pairwise matrix, the flat-mean trap and the noise
   floor at the shipped metric; the raw output is committed.
2. Run the P1.5 probe on device to measure the **real** per-joint noise.
3. `python3 scripts/tune_match.py --sigma <measured>` — re-measures the thresholds.
4. If a threshold moves: change `Matcher.Tuned*`, rebuild the scene with `SceneBuilder`, then build.
   The build refuses a scene whose serialised copy disagrees.

## 11. Measured values

| date | device | metric | σ | lockedDeg | closeDeg | nearMissDeg | notes |
|---|---|---|---|---|---|---|---|
| 15 Nov 2026 | **none — simulated** | v1 flat mean | 2.0° | 12 | 20 | 18 | retired: U-V collapsed to 3.1°, near-miss could not fire |
| 15 Nov 2026 | **none — simulated** | v2 chain max | 2.0° | 12 | 20 | 26 | U-V 26.0°, noise p95 8.5°, score 3.06 |
| **16 Nov 2026** | **none — simulated** | **v3 weighted finger mean** | **2.0°** | **12** | **24** | **26** | U-V 26.0°, noise p95 **6.7°**, score **3.89**; flat mean 5.3° (trap). **Not yet measured on a device.** |
| | | | | | | | P1.5 probe fills this row; tripwire at 1.4× model noise moves closeDeg to 20 |
