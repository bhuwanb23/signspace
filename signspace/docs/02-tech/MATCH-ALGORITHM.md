# Match Algorithm
**Status:** ✅ v2 — **v1 retired 15 Nov 2026** · **Owner:** W2 · **Phase:** P2 · **Gate:** G2
**Measured by:** `scripts/tune_match.py` · **Tuning artefact:** [`phases/p2-core-loop/matcher-tuning.md`](../../phases/p2-core-loop/matcher-tuning.md)

> **What changed in v2 and why:** v1 measured a weighted mean of per-joint angles across the whole
> hand. That metric cannot see finger spread: U and V differ in one joint, and the mean diluted an
> 18° spread to 0.3° of signal. The near-miss for U/V therefore **could never fire** — and the
> near-miss is the product's entire identity. v2 measures each finger's chain separately and
> combines the fingers with a max. U-V went from 3.1° to 26.0°.

---

## 1. The one design decision

**The output is a distance in degrees, never a percentage.** "You are 9° off" is actionable.
"You are at 82%" is a number to optimise. Degrees describe a shape.

## 2. The metric

```
For each finger f in {thumb, index, middle, ring, little}:
    chain_f = the accumulated rotation of that finger's chain
    d_f     = angle( inverse(target_chain_f) * current_chain_f )

distance = max(d_f)                       # worst finger: a handshape is wrong if any finger is wrong
wrist    = angle( target_wrist , current_wrist )   # palm orientation, applied separately
```

**Where the chain rotation comes from.** A joint's stored local rotation already contains every
ancestor's rotation, so the accumulated chain rotation is exactly the stored rotation of the
chain's **last** joint:

```
stored[distal] = d_base · d_prox · d_inter · d_distal  =  chain rotation
```

One quaternion comparison per finger, and **no parent map** — so it makes no assumption about how
the rig parents the fingers. The terminal (tip) joint is excluded: it carries no shape information
for these six letters and it carries tracking noise.

**The wrist is a gate, not a score.** Rotating the wrist the wrong way must break the match even
when every finger is perfect (P2.6). But a wrist angle is not comparable with a finger angle, so
it is applied as a slack on its own term:

```
orientation      = max( palm_angle , wrist_angle )
orientation_cost = max( 0 , orientation - orientationSlackDeg )
effective        = max( distance , orientation_cost )
```

Both the palm and the wrist are checked because the two SDK hand layouts disagree about which one
carries the whole-hand orientation.

## 3. The numbers, and where they come from

| parameter | value | measured / reason |
|---|---|---|
| `lockedDeg` | **12°** | noise p95 = 8.5°, nearest intended pair = 26.0°. Sits between them. |
| `closeDeg` | **20°** | a wrong shape at 26° must read as *warming*, not *close*. |
| `nearMissMaxDeg` | **26°** | exactly the nearest intended pair, so a deliberate wrong shape can always be named. |
| `orientationSlackDeg` | **20°** | wrist and palm error forgiven below this. |
| `holdSeconds` | 0.7 s | unchanged from the plan. |
| `bandHysteresisSeconds` | 150 ms | the meter must never strobe. |
| `confidenceFloor` | 0.7 | below this the exercise **pauses**; it never fails. |
| `dropoutGraceSeconds` | 0.2 | a dip shorter than this is ignored entirely. |
| `resetAfterSeconds` | 0.5 | leaving the locked band for longer than this resets progress. |
| `nearMissMarginDeg` | 2° | the wrong shape must beat the target by this much. |
| `nearMissConfidenceWindowSeconds` | 0.4 | confidence must have held for this long before we are allowed to name a shape. |
| `nearMissCooldownSeconds` | 20 | minimum gap between two near-miss messages. |

**Nearest intended pair: 26.0° (U-V). Nearest accidental pair: 70.1° (C-O).** There is a 44°
gap between the nearest pair we *want* to fire on and the nearest pair we *never* want to.

## 4. Bands

| band | condition | what the user sees |
|---|---|---|
| Locked | `d ≤ lockedDeg` | the shape starts to fill |
| Close | `d ≤ closeDeg` | the meter leans toward the target |
| Warming | `d > closeDeg` | nothing, and that is correct |

**There is no failure state.** No red, no X, no "try again". See `UX-STATES.md`.

## 5. The hold

- 0.7 s inside the Locked band before the shape counts.
- Confidence below the floor → **pause**: the timer neither advances nor resets.
- Dips shorter than 0.2 s are ignored entirely.
- Leaving the band for more than 0.5 s resets progress.
- After a lock the state latches until the hand leaves the band, so a lock never double-fires.

## 6. The near-miss

The signature feature. Fires when **all** of these hold:

1. the band is not Locked (you have not got it, so there is something to say),
2. the best other shape is within `nearMissMaxDeg` (26°),
3. it is strictly closer than the target,
4. it is strictly closer than the **runner-up** other shape,
5. it beats the target by at least `nearMissMarginDeg` (2°),
6. confidence has held above the floor for the last 0.4 s,
7. the 20 s cooldown has elapsed.

**Conditions 4 and 5 are the "ambiguity is not a diagnosis" rule.** If the hand sits between two
shapes, or is only marginally closer to one of them, the app says nothing. Silence is the default
on any doubt.

> **A trap worth naming.** The rule is *not* "how many shapes fall inside the 18° window". U and V
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

**This is what `POSE-FORMAT.md` promised from the start** — *"Joints are named, not indexed"* — and
the code now actually does it. `SkeletonPoseSource` logs what it could not map, loudly, because a
mis-mapped joint is a shape that never matches and no error appears anywhere.

## 8. Deliberate non-goals

- Absolute joint positions. Relative rotations only.
- User scale normalisation. The comparison is already scale-free.
- A trained ML classifier. Deterministic joint comparison is provably sufficient for 12 static shapes.
- Motion or trajectory matching (ADR-004).
- A pass/fail boolean (ADR-003).

## 9. Tuning procedure

1. `python3 scripts/tune_match.py` — measures the pairwise matrix and the noise floor.
2. Run the P1.5 probe on device to measure the **real** per-joint noise.
3. `python3 scripts/tune_match.py --sigma <measured>` — re-measures the thresholds.
4. Update §3 if the numbers moved, and commit the change.

## 10. Measured values

| date | device | σ | lockedDeg | closeDeg | nearMissDeg | notes |
|---|---|---|---|---|---|---|
| 15 Nov 2026 | **none — simulated** | 2.0° | 12 | 20 | 26 | offline, `scripts/tune_match.py`. **Not yet measured on a device.** |
| 19 Nov 2026 | **none — simulated** | 1.5–4.0° | 12 | 20 | 26 | per-shape margins, `scripts/noise_margin_sim.py`. Still no device row. |
| | | | | | | P1.5 probe fills this row |

## 11. Per-shape margins — what can be measured without a headset

`tune_match.py` reports aggregates and `band_sim.py` reports band behaviour; neither says whether a
*specific* shape has room inside the 12° band. `scripts/noise_margin_sim.py` does, with the same noise
model and the same shipped v3 metric:

```bash
python3 scripts/noise_margin_sim.py --csv phases/p2-core-loop/noise-margins.csv
```

For each shape it perturbs the target pose, then reports the correct-shape distance (p95) against the
worst-case closeness of any rival (p05). A positive margin means the gap between the two is wider than
noise; `wrong-lock %` counts frames where another shape is nearer *and* the hand is still inside 12°.

| shape | σ 2°: self p95 | rival p05 | margin | σ 4°: self p95 | rival p05 | margin |
|---|---|---|---|---|---|---|
| B | 6.8 | 101.6 | 94.8 | 13.5 | 101.5 | 88.0 |
| U | 6.9 | 23.3 | 16.4 | 13.4 | 21.1 | 7.7 |
| V | 6.7 | 23.2 | 16.4 | 13.4 | 21.0 | 7.7 |
| W | 6.7 | 113.7 | 106.9 | 13.6 | 110.5 | 97.0 |
| C | 6.7 | 62.3 | 55.6 | 13.5 | 59.8 | 46.3 |
| O | 6.6 | 62.4 | 55.8 | 13.5 | 59.8 | 46.3 |

Three readings:

- **No shape can be confused with another by noise alone.** Wrong-lock is 0.00% at every level tested, on
  2,000–4,000 trials per shape: the nearest rival is U/V at ~21–24°, and jitter at these magnitudes does
  not close a 20° gap.
- **B, U and V are the tight ones, and only they move.** Their margins halve between σ 2° and σ 4°, while
  W/C/O lose a few degrees. If the P1.5 probe measures worse than ~3°/joint, those three are where the
  hold will feel late.
- **The 12° band is calibrated for σ ≤ 3°.** At σ 4° a correct hand's own p95 is 13.5° — outside the band—
  so honest locks would drop before wrong ones appeared. That is the correct failure direction, and it is
  the number to watch on device: if the probe reports ≈4°, widen the band or narrow the shapes, do not
  blame the learner.

**What this is not.** It is a model of jitter, not a measurement of a Quest. It cannot show frame rate,
thermal drift, occlusion, or how a real hand's noise is correlated across joints (ours is independent per
joint, which is optimistic). TRACKABILITY.md requires a device row for any claim, and P1.5 is still owed.
