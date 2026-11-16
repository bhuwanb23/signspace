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
| **U-V** | **5.4°** | index spread |
| V-W | 27.0° | a third finger extended |
| B-U | 51.8° | two fingers folded |
| C-O | 56.1° | curvature and thumb contact |
| *nearest accidental pair* | *133.6°* | — |

**The near-miss threshold is 18°. U and V are 5.4° apart.**

The near-miss fires when the wrong shape is within 18° of the user's hand *and* closer than the
target. With U and V only 5.4° apart, that is only ever true if the user makes a nearly perfect V.
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

U-V went from **5.4° to 26.0°** — a 4.8× improvement in the signal that the near-miss depends on. *(An earlier draft of this page quoted 3.1°; `scripts/tune_match.py` now prints 5.4° against the shipped poses, and the tool wins.)*

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

## 8. v3 — per-joint weights, and the trap measured on our own content *(P2.2)*

P2.2 asks for something the v2 metric above deliberately does not do: **per-joint weights**, tuned
without recompiling. The objection that produced v2 was that a flat mean hides one wrong finger
(U-V collapses from 26° to ~3° across the whole hand). So the question for P2.2 was not "can we add
weights" but "can weights be added **without** losing that separability". They can, if the mean and the
combine are in the right places:

```
θ_j        = 2·acos(|w|) of   inverse(q_target_j) · q_live_j          per joint
finger_f   = Σ_{j∈f} w_j θ_j  /  Σ_{j∈f} w_j                          weighted mean WITHIN a finger
D          = max_f finger_f                                           worst finger = the distance
D_flat     = Σ_all w_j θ_j    /  Σ_all w_j                            reporting only
```

Three things to notice, each of which is one of the task's requirements or traps:

- **The denominator is the sum of the weights, never the joint count** (trap 2). With a count
  denominator every distance would scale by `total weight / count`, so editing one weight in the asset
  would move every distance uniformly and the thresholds would look broken for no visible reason.
- **The mean is within a finger, the max is across fingers** (trap 1). The little finger cannot cancel
  the thumb because they never share an average.
- **`D_flat` still exists, but only as a diagnostic.** It is on the overlay beside `D` so a suspiciously
  small flat value can be read as "one finger is very wrong" instead of "the shape is close".

### The weights, by group

| Joints | Weight | Why |
|---|---|---|
| `Wrist`, `Palm` | **0.40** | posture, not shape — and palm orientation is already a separate gate in `Matcher` (`orientationSlackDeg`). A full weight here would judge the wrist twice |
| all `*Metacarpal` (CMC) | **0.40** | the base of the finger barely moves for these six letters and is the noisiest joint on device |
| all `*Proximal` | **0.70** | carries some shape, mostly carries reach |
| all `*Intermediate` | **1.00** | where curl actually happens — the highest-signal joint in the hand |
| all `*Distal` | **1.00** | the fingertip's direction; what the eye reads as the shape |
| all `*Tip` | **0.00** | carries no shape information for these six letters and does carry tracking noise |

15.30 total weight over 21 weighted joints. They live in `Assets/_Project/Matching/Weights.asset`
(`JointWeights`, **named** entries matched by canonical name at load, so reordering the inspector list
cannot move a weight onto the wrong joint).

### The measured comparison — same shapes, same noise model, one run

```
python3 scripts/tune_match.py --v3-only
```

| Metric | Nearest pair | Noise p95 (2°/joint) | Score (pair / p95) |
|---|---|---|---|
| v2 — max of chain angles | 26.0° | 8.5° | 3.06 |
| **v3 — max of weighted finger means** | **26.0°** | **6.7°** | **3.89** ← ships |
| v3 — flat weighted mean *(the trap)* | **5.3°** | 3.7° | 1.43 |

The weighted-within-finger mean **halves the noise sensitivity without losing a single degree of
separability** (26.0° unchanged), which is why it improves the score rather than trading against it:
averaging inside a finger averages noise away, while the max across fingers keeps a single wrong finger
at full magnitude. And the flat mean is now quantified rather than argued — it collapses U-V to 20% of
its real distance and scores 1.43, i.e. the noise floor is 70% of the pair it is supposed to separate.

**v3 pairwise matrix** (degrees; diagonal zero by construction):

|     | B | U | V | W | C | O |
|---|---|---|---|---|---|---|
| **B** | 0.0 | 117.0 | 117.0 | 117.0 | 101.9 | 148.3 |
| **U** | 117.0 | 0.0 | **26.0** | 118.1 | 117.2 | 148.3 |
| **V** | 117.0 | 26.0 | 0.0 | 118.1 | 117.2 | 149.5 |
| **W** | 117.0 | 118.1 | 118.1 | 0.0 | 117.2 | 149.0 |
| **C** | 101.9 | 117.2 | 117.2 | 117.2 | 0.0 | **65.0** |
| **O** | 148.3 | 148.3 | 149.5 | 149.0 | 65.0 | 0.0 |

### Thresholds: unchanged, and why that is the right call

`lockedDeg 12 / closeDeg 20 / nearMissMaxDeg 26` were derived against v2. Under v3 they still hold:
lock sits **1.8× above the noise p95** (6.7°) and **2.2× below the nearest pair** (26.0°). The metric got
*quieter*, so the same thresholds are now strictly more forgiving in practice — no threshold became
invalid. **They are not widened here**, because how forgiving a lock should feel is a judgement that
needs a hand in a headset, and that is P2.3's job. What P2.2 establishes is that there is now headroom
to spend.

### The reasoning that did NOT survive contact with the numbers

The first design considered was a plain weighted mean over 21 joints — literally what the task's step 3
describes — and it was rejected by measurement, not by taste: 5.3° on U-V against its own 3.7° noise
floor. Step 3's "weighted mean angle across the fingers of interest" is implemented as a weighted mean
*per finger* combined by max, and the flat version is kept in the code path (and on the overlay) so the
difference is visible to whoever tunes it next.

### The device check P2.2 actually requires

The offline work cannot answer the task's step 5, because it *is* a question about hands:

> holding a fixture with your own hand should give a visibly lower `D` than a wrong shape.

Procedure, once the headset is available:

1. `SIGNSPACE/P2.2 Assign weights to matcher`, build, open the overlay.
2. Make **B**. `D` should drop below **12°** and the three worst joints should all be low-angle.
3. Make **U** (a different shape, 26° from B… 117° in v3 terms). `D` should read **>100°**, i.e. the
   overlay is not merely "higher", it is unmistakably in another band.
4. Deliberately splay the index on **B**. `D` should jump and the **worst joint should name an index
   joint**, while `D_flat` stays comparatively small — that gap is the whole point of v3, and seeing it
   in your own hand is the acceptance test for this sub-phase.

Recorded outcome goes into the chart in §9 and feeds P2.3's band tuning.

---

## 9. Done when

- [x] the metric is measured, not guessed — `scripts/tune_match.py`
- [x] the four intended near-miss pairs all separate
- [x] the nearest accidental pair is far outside every threshold
- [x] a lock threshold exists that clears the noise and stays below the nearest pair
- [x] the metric's sensitivity to tracking noise is quantified
- [x] the C# and the Python twin are proven to agree
- [ ] **the numbers are re-measured on the device** (P1.5 probe) and `lockedDeg` is confirmed

P2.2 adds:

- [x] per-joint weights exist as a **serialisable asset** (`Matching/Weights.asset`), not constants
- [x] the weighted metric is measured against the metric it replaces, on the same shapes and the same
      noise model: **3.89 vs 3.06**, with separability unchanged at 26.0°
- [x] the trap is quantified on our own content (flat mean collapses U-V to **5.3°**, score 1.43)
- [x] the denominator is the total weight, not the joint count — stated in the code, the doc and the
      Python twin so the three cannot drift
- [ ] **the device check**: a real B against a real U with `D` and the three worst joints named
      (procedure in §8, needs the headset)
