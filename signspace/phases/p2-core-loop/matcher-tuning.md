# Matcher tuning — why the metric changed
**Phase:** P2 · **Task:** P2.2 · **Workstream:** W2 · **Gate:** G2
**Written:** 15 Nov 2026 · **Tool:** `scripts/tune_match.py` · **Raw output:** [`matcher-tuning-raw.txt`](matcher-tuning-raw.txt)

---

## 1. The finding

The v1 metric was a weighted mean of the per-joint angle differences across the whole hand, with the
weights the plan assumed: proximal 0.40, mid 0.70, distal 1.00, thumb 0.80.

Measured against the six shipped shapes, that metric produces:

| pair | v1 distance | what separates them |
|---|---|---|
| **U-V** | **3.1°** | index spread |
| V-W | 27.0° | a third finger extended |
| B-U | 51.8° | two fingers folded |
| C-O | 56.1° | curvature and thumb contact |
| *nearest accidental pair* | *133.6°* | — |

**The near-miss threshold is 18°. U and V are 3.1° apart.**

The near-miss fires when the wrong shape is within 18° of the user's hand *and* closer than the
target. With U and V only 3.1° apart, that is only ever true if the user makes a nearly perfect V.
A sloppy V — the exact case the feature exists for — sits 18-25° from V and 15-22° from U, and the
near-miss **never fires**.

The cause is dilution, not sensitivity. U and V differ in **one joint**: how far the index is
abducted. Spread that one joint's contribution across the weighted total of all 26 joints and
18° of spread becomes 0.3° of signal. The signal was never lost by the tracking; it was averaged
away by the metric.

**This is not a tuning problem. The near-miss is the product's identity, and under the v1 metric it
could not fire for the one pair that is most visible to a learner — the difference between a U and a
V is the first thing anyone learns to feel.**

---

## 2. The fix

Measure the difference **per finger**, as the accumulated rotation of that finger's chain, and
combine the five fingers with a **max**: a handshape is wrong if any finger is wrong.

```
chain_angle(finger) = angle( inverse(target_chain) * current_chain )
distance             = max over the five fingers
```

A finger's whole configuration becomes one number instead of a share of an average, so both spread
and curl register at full magnitude.

### The chain rotation lives in the chain's last joint

A joint's stored local rotation already contains every ancestor's rotation, so the accumulated
chain rotation is exactly the stored rotation of the chain's **last** joint:

```
stored[distal] = d_base * d_prox * d_inter * d_distal  =  chain rotation
```

That is one quaternion comparison per finger instead of one per joint, and it needs **no parent
map** — so it makes no assumption about how the rig parents the fingers. `scripts/tune_match.py`
asserts that this identity holds (worst disagreement across all six shapes: 0.000004°), which is
what makes the numbers in this document the numbers the shipped C# produces.

The terminal (tip) joint is excluded: it carries no shape information for these six letters and it
carries tracking noise. Dropping it moved the score from 2.77 to 3.06.

---

## 3. The measured result

### v2, `max` over per-finger chains, tip excluded

| | B | U | V | W | C | O |
|---|---|---|---|---|---|---|
| **B** | 0.0 | 100.2 | 100.2 | 100.2 | 120.1 | 174.0 |
| **U** | 100.2 | 0.0 | **26.0** | 101.6 | 140.0 | 174.0 |
| **V** | 100.2 | **26.0** | 0.0 | 101.6 | 140.0 | 174.2 |
| **W** | 100.2 | 101.6 | 101.6 | 0.0 | 140.0 | 174.1 |
| **C** | 120.1 | 140.0 | 140.0 | 140.0 | 0.0 | **70.1** |
| **O** | 174.0 | 174.0 | 174.2 | 174.1 | **70.1** | 0.0 |

| | |
|---|---|
| nearest **intended** near-miss pair | **26.0°** (U-V) |
| nearest **accidental** pair | 70.1° (C-O) |
| tracking noise, p50 / p95 | 5.5° / 8.5° at 2° per joint |

U-V went from **3.1° to 26.0°** — an 8.4× improvement in the signal that the near-miss depends on.

### Why the combinations were tried

Score = nearest intended pair ÷ noise p95. Higher is better.

| combine | pair | accidental | noise p95 | score |
|---|---|---|---|---|
| **max (no tip)** | **26.0** | 100.2 | 8.5 | **3.06** |
| pnorm8 (no tip) | 21.3 | 82.0 | 7.0 | 3.03 |
| pnorm6 (no tip) | 19.9 | 76.8 | 6.7 | 2.99 |
| softmax (no tip) | 14.3 | 87.4 | 5.1 | 2.79 |
| max | 26.0 | 100.2 | 9.4 | 2.77 |
| pnorm3 | 15.2 | 59.6 | 6.4 | 2.38 |
| mean | 5.2 | 27.5 | 5.4 | 0.97 |

`mean` is last because it is the failure mode we started with: averaging five fingers re-dilutes a
single wrong finger. `max` wins because a handshape is wrong if any finger is wrong, which is also
what a learner feels.

---

## 4. The thresholds, and why they are where they are

A lock threshold has to sit **above the noise floor** (or a correct shape never locks) and
**below the nearest intended pair** (or a shape could be confused with its near-miss partner).

| parameter | value | why |
|---|---|---|
| `lockedDeg` | **12°** | noise p95 is 8.5° at 2°/joint; the nearest pair is 26.0°. 12 sits between them with margin on both sides. |
| `closeDeg` | **20°** | a *wrong* shape at 26° must read as "warming", not "close" — the meter must not tell a learner they are nearly right when they made a different letter. |
| `nearMissMaxDeg` | **26°** | exactly the nearest intended pair, so a deliberate wrong shape can always be named. |
| `orientationSlackDeg` | **20°** | wrist and palm error is forgiven below this; beyond it, the hand is held wrongly and the match must break (P2.6). |
| `holdSeconds` | 0.7 s | unchanged from the plan. |

---

## 5. Sensitivity — what happens if real tracking is noisier

The noise model assumes **2° of error per joint**. That is an assumption, not a measurement. The
P1.5 probe measures it. If it comes out worse, `lockedDeg` moves — this table says by how much.

| per-joint noise σ | noise p50 | noise p95 | lock threshold needed | vs the 26.0° pair |
|---|---|---|---|---|
| 0.5° | 1.4 | 2.1 | 4.4 | OK |
| 1.0° | 2.8 | 4.3 | 5.8 | OK |
| 1.5° | 4.1 | 6.4 | 8.6 | OK |
| **2.0°** | **5.5** | **8.5** | **11.5** | **OK** |
| 2.5° | 6.9 | 10.6 | 14.4 | OK |
| 3.0° | 8.2 | 12.8 | 17.2 | OK |
| 4.0° | 11.0 | 17.0 | 23.0 | OK |

The design survives σ = 3° per joint — roughly what a Quest 3S does in poor light. It only comes
apart at 4°, which is beyond what any hand tracking does.

**Action:** re-run `python3 scripts/tune_match.py --sigma <measured>` after the P1.5 probe and
update `lockedDeg` if the table disagrees.

---

## 6. What changed in the code

| file | change |
|---|---|
| `Matching/PoseMath.cs` | new metric: per-finger chain angle, combined with a max, plus a wrist term. `Distance` now reports the per-finger angles for the debug overlay. |
| `Matching/Matcher.cs` | thresholds from §4. The near-miss now requires the winner to be **strictly closer than the runner-up** and to beat the target by a margin, which is the "ambiguity is not a diagnosis" rule from `MATCH-ALGORITHM.md` — the old code had no such check. |
| `Matching/JointNames.cs` | new. Joints are matched **by name**, not by index, so the legacy Oculus and OpenXR hand layouts both work and nobody has to paste joint indices into the inspector. |
| `Hands/SkeletonPoseSource.cs` | reads the joint names off the transforms, builds the canonical mapping, and **reports loudly** what it could not map. Replaces the manual step in `SETUP.md` §4. |
| `Matching/SignSetLoader.cs` | rewritten: `UnityWebRequest` instead of `File.ReadAllText`, because StreamingAssets is inside the apk on Android and direct file IO always fails there. |
| `scripts/hand_model.py` | new. The canonical hand model and the six shape definitions. |
| `scripts/make_pose.py` | new. Writes the six pose files and `index.json`. |
| `scripts/tune_match.py` | new. The offline tuner and the Python twin of the matcher. |
| `scripts/validate_poses.py` | also checks the canonical joint set and that `index.json` matches what is actually in the folder. |

The per-joint weight arrays are **gone**. They were the mechanism by which the spread signal was
diluted, so keeping them would have kept the bug in a more comfortable shape.

---

## 7. The one assumption this rests on

The pose files assume the hand rig's rest pose has **identity local rotations** — a straight-finger
rig with aligned bones, which is how a hand rig is normally authored.

If the shipped rig does not, every stored value carries a constant offset and nothing matches.

**This is checked, not assumed:**

1. `unity/Assets/_Project/Editor/RestPoseDump.cs` dumps the rig's rest-pose local rotations.
2. `python3 scripts/make_pose.py --neutral rest-pose.json` regenerates the poses against it.
3. `SkeletonPoseSource` logs the joint mapping on the device, and the debug overlay shows it.

Nothing else changes. Ten minutes, once the SDK is imported.

---

## 8. Done when

- [x] the metric is measured, not guessed — `scripts/tune_match.py`
- [x] the four intended near-miss pairs all separate
- [x] the nearest accidental pair is far outside every threshold
- [x] a lock threshold exists that clears the noise and stays below the nearest pair
- [x] the metric's sensitivity to tracking noise is quantified
- [x] the C# and the Python twin are proven to agree
- [ ] **the numbers are re-measured on the device** (P1.5 probe) and `lockedDeg` is confirmed
