# Palm orientation, weighted separately from the fingers

**Phase:** P2 · **Task:** P2.6 · **Gate:** G2 · **Status:** built, measured and shipped; the device
hand-roll test is the one thing left open
**Raw output:** `phases/p2-core-loop/palm-sim-raw.txt` (regenerate with `python3 scripts/palm_sim.py`)

> Several sign pairs differ mostly in which way the palm faces. If that is folded into the finger
> average, the app accepts a correct-shape/wrong-facing attempt — which, to a sign learner, is a
> different word. So the palm gets its own measurement and its own weight, and **the two errors add.**

---

## 1. The shipped numbers

| Quantity | Value | Where it came from |
|---|---|---|
| `O_slack` | **15°** | the task's figure, and it survives the measurement: wrist jitter for a *correct* hand is sd **1.41°**, p99 **6.43°**, so 15 is ≈ 10σ of noise — a correct hand is never penalised |
| `k` | **1.00** | the smallest weight that stops a 30° mis-face from locking from a *perfect* finger position (§4) |
| cap | **none** | §5 |
| `D_total` | `D_fingers + k · max(0, O − O_slack)` | task step 3 |

`k = 1` has a plain reading, which is why it is the value that ships: **a degree of facing error costs
a degree of match.** No unit conversion between two things that cannot be compared, and no second
scale to explain.

---

## 2. The trap: no Euler angles, anywhere

Any Euler triple reports a large error at certain wrist orientations for a hand that is exactly right,
so the tuning starts chasing phantom errors that appear only at particular angles. The way out is not
a better Euler order — it is to never decompose into angles at all.

The palm is described by a **basis** built from the hand itself, and compared as a rotation:

```
forward  = the palm frame's +Y     (where the hand points)
normal   = the palm frame's +Z     (out of the back of the hand)
basis    = (x = normal × forward,  y = normal,  z = forward)
O        = 2·acos|dot(basis_target, basis_live)|     … in degrees
```

The same `2·acos|w|` trick the joint angles use, for the same reason: it is the angle of a rotation, so
half a turn is 180°, a quarter turn is 90°, and there is no preferred axis and no wrapping.

**Verified, not asserted.** Sweep the wrist through a full turn about each of the three axes in 5° steps
and check that the reported error never jumps by more than one step:

| axis turned | O at rest | largest step-to-step change | verdict |
|---|---|---|---|
| +Y (pronation / supination) | 0.0° | **5.0°** | smooth |
| +X (flexion / extension) | 0.0° | **5.0°** | smooth |
| +Z (deviation) | 0.0° | **5.0°** | smooth |

A step is 5°, so "≤ 5° per step" is the strongest statement available — an Euler decomposition spikes
past it wherever two axes cross. The overlay also shows **swing** (tilt) and **twist** (roll) split by
swing-twist decomposition, which splits about the palm's *own* forward axis; those two numbers are
diagnostics and never part of the distance.

---

## 3. The basis that the task asked for, and why the shipped one is different

The task specifies "wrist forward vector plus index-knuckle plane normal". The plane through the
metacarpal heads is a **positional** quantity, and the pose format carries rotations only — no bone
lengths, no joint positions. The only proxy available is the cross product of the index and little
metacarpal axes, so that proxy was built first and then measured. It does not survive:

| shape | metacarpal axes | cross-product amplification (1/sin θ) | O sd under 2°/joint noise — palm frame | — knuckle plane |
|---|---|---|---|---|
| **B** | 11.0° apart | **5.2×** | **0.00°** | **5.80°** |
| **U** | 86.0° apart | 1.0× | 0.00° | **88.63°** |
| **V** | 86.5° apart | 1.0× | 0.00° | 37.48° |
| **W** | 86.3° apart | 1.0× | 0.00° | 45.87° |
| **C** | 2.4° apart | 24.2× | 0.00° | 24.44° |
| **O** | 1.8° apart | 31.0× | 0.00° | 26.03° |

Two independent failures:

- **The rig curls fingers by rotating the metacarpals.** So on every closed hand the "knuckle plane"
  is a plane through two *curled* bones: 86° apart on U/V/W, and its normal is 88–90° away from the
  palm's actual dorsal axis. U and B came out **180° apart** the first time this was measured, purely
  from a cross-product sign.
- **A narrow fan is ill-conditioned.** Even on the flat hand, 11° of separation amplifies the tracker's
  own noise by 5.2×: the reported facing moves 5.8° when the hand has not moved.

So the **palm frame** is what runs: forward = the palm frame's +Y, normal = its +Z, both from
`Wrist × Palm`. Its inputs are the two joints that *do* carry facing, so its jitter is **0.00°** under
finger noise. The knuckle-plane normal is still computed every frame, still used when it is well
conditioned (axes 25–45° apart, agreeing with the palm normal within 12°, sign flipped onto it), and
always reported — `PalmBasis` and `BasisNote` on the overlay say which one a frame used and why. **No
shipped shape qualifies**, which is the honest state of the content rather than a reason to delete the
code: a future rig with bind positions, or a shape authored with a genuinely flat fan, gets the
knuckle plane for free.

There is one device-dependent assumption, and it belongs in P1.8's notes rather than being buried:
**the wrist joint must be root-relative, not identity.** If the Meta rig feeds a Wrist local rotation
that is always identity, the palm frame collapses to the palm joint alone and a wrist *turn* becomes
invisible. §7's step 2 is the test for it.

---

## 4. Choosing k

`O_slack = 15`, so the term is `k · max(0, O − 15)`. What k has to achieve: **a deliberately mis-facing
hand must fail to lock** (the done-when), including from a perfect finger position, which is the hardest
case.

| k | D_total at O = 30° | vs the 12° lock band | D_total at O = 20° | D_total at O = 15° |
|---|---|---|---|---|
| 0.25 | 3.8° | **locks** (−8.2) | 1.3° | 0.0° |
| 0.40 | 6.0° | **locks** (−6.0) | 2.0° | 0.0° |
| 0.50 | 7.5° | **locks** (−4.5) | 2.5° | 0.0° |
| 0.60 | 9.0° | **locks** (−3.0) | 3.0° | 0.0° |
| 0.75 | 11.3° | **locks** (−0.7) | 3.8° | 0.0° |
| **1.00** | **15.0°** | **fails (+3.0)** ← ships | 5.0° | **0.0°** |

Only `k = 1` clears the lock band, and it clears it by 3.0° — a gate that puts a wrong hand *just* over
the line is a coin flip once tracking noise is added, which is exactly what a slack-less threshold
teaches you to expect.

With noise, the whole picture (2°/joint, 500 trials per row):

| palm turned | O | readings inside the **lock** band | inside the **close** band |
|---|---|---|---|
| 0° | 0° | 100.0% | 100.0% |
| 15° | 15° | 100.0% | 100.0% |
| 20° | 20° | 100.0% | 100.0% |
| **30°** | **30°** | **0.0%** | 100.0% |
| 45° | 45° | **0.0%** | 0.0% |
| 60° | 60° | **0.0%** | 0.0% |

That is the done-when: **from 30° of mis-facing up, the lock is unreachable**, and up to 20° the hand
locks exactly as it should.

---

## 5. The consequence in the bands, and why it is left alone

Because the term is additive and uncapped, a hand with perfect fingers but 45° of wrong facing reads
`D_total ≈ 30°` — which is **warming**, not close. The app says nothing about how near the fingers are.

This was considered and accepted. Two alternatives were rejected:

- **Cap the penalty** at the close band so a mis-faced hand still reads "close". Tempting, and it makes
  the band honest about the fingers — but with `lockedDeg = 12` and a cap of 12, a 30° mis-face lands
  *exactly* at 12.0° and **locks**. A cap has to be tuned against the lock edge to the degree, and any
  future change to `lockedDeg` breaks it silently. Fragile in the worst place.
- **Add a fourth band** ("close, but facing wrong"). P2.3 established three bands and no failure state;
  a fourth state would be a diagnosis, and the diagnosis machinery already exists — it is the near-miss,
  and it is spoken, not coloured.

If playtesting finds "the meter is amber while my hand is nearly right" confusing, the fix is a facing
*hint* in the same voice as the near-miss, not a fourth band and not a cap. That is written down here so
it is not re-derived as a bug.

One asymmetry worth recording: the **near-miss compares finger distances only** (`PoseMath.Distance`
on the other shapes, no orientation term). A mis-faced hand can therefore be near-missed as a different
shape. That is correct — the near-miss answers "which shape are your fingers making", and the palm gate
answers "is your hand facing the right way" — but it does mean the two can fire together with different
verdicts, which §8's device run checks for.

---

## 6. The C/O and A/B test the task asked for, and what actually happened

The task asks for the two most orientation-sensitive shapes in the set: **C/O** and **A/B**. Measured
first, before anything was tuned:

| | B | U | V | W | C | O |
|---|---|---|---|---|---|---|
| **B** | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 |
| **U** | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 |
| **V** | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 |
| **W** | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 |
| **C** | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 |
| **O** | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 |

**Every authored shape has identical palm facing**, because all six were authored from the same neutral
with finger deltas only. There is also no **A** in the shipped set (the six are B, C, O, U, V, W), so the
A/B pair does not exist to test.

So the pair test cannot be run as specified, and saying otherwise would be inventing evidence. What was
done instead is strictly stronger for the purpose of the gate: **the palm was turned synthetically**
(rotate only the Wrist joint, which is exactly what a real wrist turn changes in a parent-relative
skeleton) and the whole sensitivity curve in §4 came from that.

That simulation found the real content problem, which is worth more than the test it replaced:

> **The app cannot currently tell apart two signs that differ only in palm facing, because no authored
> pair does.** The machinery is here and measured; the content that exercises it does not exist.

**Recommendation for P3 (verbatim from this measurement):** author **K vs P** — the same handshape with
the wrist rotated ~90°, which is the canonical orientation-only minimal pair — and use it as the pair
the device test runs on. Until then, P2.6's gate protects against a *mis-faced attempt* (which is what
the done-when asks) but cannot yet distinguish two *different signs*.

---

## 7. The device test

Nothing here needs hardware to be *correct*, but three things about it do need hardware to be *known*:

1. **The hand-roll test.** Make B, hold until locked, then roll the wrist 30–45° while keeping the
   fingers identical. The band must drop out of Locked; the overlay must read `palm O ≈ 30–45 +15–30`
   with `swing`/`twist` splitting the roll; and rolling back must lock again without moving the fingers.
2. **Is the Wrist joint root-relative?** If `O` stays at 0.0° while the wrist visibly rolls, the rig is
   feeding an identity wrist and the palm frame has collapsed to the palm joint — record it, and the
   forward axis has to come from the palm's own +Y instead. This is the one assumption in §3.
3. **The jitter figure.** `palm_sim.py` models wrist noise at 2°/joint, which is the same number the rest
   of the tuning uses for fingers. Wrist tracking is usually better than finger tracking, so 15° of slack
   should be very generous — but if the overlay shows `O` breathing above ~10° on a still hand, the slack
   is 15 and the p99 was 6.4: measure again before touching `k`.

---

## 8. Done when

- [x] **Palm orientation contributes independently** — `D_total = D_fingers + k·max(0, O − 15)`. Proven,
      not asserted: turning the wrist leaves `D_fingers` at **0.0°** while `O` tracks the turn exactly,
      because a wrist turn changes one local rotation and the finger distance never looks at it.
- [x] **A deliberately mis-facing hand fails to lock** — 0.0% of readings inside the lock band from 30°
      up, at `k = 1` (§4). Device confirmation is §7 step 1.
- [x] **`k` and `O_slack` recorded** — `k = 1.00`, `O_slack = 15°`, with the table that chose `k` and the
      σ-argument for the slack (§1, §4).

### Traps, and where each one was met

| Trap | How it was avoided |
|---|---|
| Using Euler angles — gimbal ambiguity will produce phantom errors at certain wrist rotations | no decomposition at all: a basis compared by `2·acos|dot|`, plus a full-turn sweep per axis as the test that it stays smooth, plus swing-twist for the diagnostics |
| *(found while doing it)* trusting the knuckle-plane normal because the task named it | measured before shipping: 88–90° error on every closed hand and 5.2× noise amplification on the flat one, so the palm frame runs and the knuckle plane is kept as a guarded refinement that no shipped shape qualifies for |
