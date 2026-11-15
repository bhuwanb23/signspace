# P1.3 — Rotation verdict

> **The most important page produced on Day 2.** Everything below the line "RESULT" is written
> *before* the test, on purpose.

**Task:** P1.3 · rotate the grabbed object with your own wrist · **Gate fed:** G1
**Written:** 15 Nov 2026 · **Status:** 🟡 rig built, **verdict pending — needs the headset**

---

## 1. What is being decided

**The core gesture.** Grab a floating handshape and turn it with your wrist to compare it against the
shape you are making. If this does not feel natural, everything else in the product is decoration on a
bad interaction, which is why the gate's red branch for P1.3 says *"the core gesture is wrong and the
concept is re-opened the same day"*.

## 2. The three variants under test

| | Variant | Mechanism | The bet it makes |
|---|---|---|---|
| **A** | **Direct** | `object.rotation = wristRotation × grabOffset`. No smoothing. | The gesture should be as immediate as a real object in the hand. Telepresence research and every direct-manipulation interface assume 1:1 is right by default |
| **B** | **Damped** | Same, with an exponential ease (`followSmoothness`, 12/s). | The tracker is noisy; a little lag hides jitter and feels "solid". **This was the file's only behaviour before P1.3 — hard-coded, never compared against anything** |
| **C** | **HoldStill** | Position frozen at the grab point; rotation applied only while wrist speed ≤ 0.12 m/s | Users can twist a wrist more precisely than they can hold a hand still in mid-air, so gating on stillness removes position noise from the rotation input |

All three run against **the reference hand mesh**, not a cube. That is P1.3's first trap: *"a hand has a
familiar shape and your brain rejects wrong rotation far more harshly"*. A cube spinning wrongly looks
fine; a hand spinning wrongly looks broken.

## 3. Pre-registered criteria — fixed before any data exists

This is the part that makes the verdict worth something. Written now, so that the result cannot be
rationalised afterwards.

### 3.1 What counts as "acceptable", per cell

| Metric | Threshold | Why this number |
|---|---|---|
| User score | **≥ 4 / 5** | 3 is "it works but I dislike it", and a product's core gesture cannot be a 3 |
| Mean lag while turning | **≤ 8°** | 8° on a hand is roughly the angle between two adjacent finger positions at arm's length — below that, the hand reads as following you rather than as chasing you |
| p95 lag | **≤ 15°** | An occasional lag spike is tolerable; a fifth of frames lagging is not |
| Position drift | **≤ 2 mm** | The object should stay put. Anything more reads as the hand sliding out of your grip |

### 3.2 What counts as a pass for the phase

| Outcome | Decision |
|---|---|
| One variant scores **≥ 4 at both distances** | **PASS.** That variant becomes the core gesture; the other two are recorded and dropped |
| One variant scores ≥ 4 at one distance only | **PASS with a design constraint.** The product fixes the interaction distance to where it works, and `ReferenceHand.distance` stops being a free parameter |
| No variant reaches 4 anywhere, but one reaches **≥ 3 at both** | **CONDITIONAL.** Spend exactly one more hour on the leading variant (threshold or damping change), then re-test. If it still fails, the concept is re-opened |
| No variant reaches 3 at both distances | **FAIL — re-open the concept**, same day, per the gate. Candidates to fall back to: head-relative comparison instead of wrist-rotation; two-handed pinch-to-scale; or judging the held pose automatically rather than by turning a model |

### 3.3 What would make the measurement invalid

Recorded so that a contaminated run is discarded rather than explained:

- **Hand tracking dropping out mid-trial.** The lag numbers become meaningless; restart the cell.
- **Testing in one lighting condition only.** P1.5 establishes the lamplight case; if lamplight is
  materially worse, the rotation verdict must be taken from the *worse* condition, because a judge may
  well be sitting under a lamp.
- **Rating captured after the fact.** The controller records scores at the moment of judging (2 s
  settle). A score typed from memory later is discarded.

## 4. The RESULT table — to be filled on device

Six cells, visited in a fixed order (A@0.40, A@0.60, B@0.40, B@0.60, C@0.40, C@0.60).

| # | Variant | Distance | Score | Lag mean | Lag p95 | Drift max | Acceptable? |
|---|---|---|---|---|---|---|---|
| 1 | A · Direct | 0.40 m | — | — | — | — | |
| 2 | A · Direct | 0.60 m | — | — | — | — | |
| 3 | B · Damped | 0.40 m | — | — | — | — | |
| 4 | B · Damped | 0.60 m | — | — | — | — | |
| 5 | C · HoldStill | 0.40 m | — | — | — | — | |
| 6 | C · HoldStill | 0.60 m | — | — | — | — | |

Numbers come from `RotationTrialController`, which writes `rotation-trials.jsonl` per cell and
`rotation-summary.txt` at the end:

```
adb shell run-as com.signspace.signspace cat \
  /sdcard/Android/data/com.signspace.signspace/files/rotation-summary.txt
```

## 5. VERDICT

| Field | Value |
|---|---|
| **Chosen variant** | ⬜ A / ⬜ B / ⬜ C / ⬜ none |
| **Go / no-go** | ⬜ GO — build the product on this gesture · ⬜ CONDITIONAL — one hour, then re-test · ⬜ NO-GO — concept re-opened |
| **Reason** | *(one paragraph, referring to the numbers above, not to how it felt in general)* |
| **Distance the product uses** | ⬜ 0.40 m · ⬜ 0.60 m · ⬜ free (both acceptable) |
| **Date evaluated** | |
| **Evaluated by** | |

> **If the verdict is NO-GO:** stop P1. Everything after it presumes the gesture. The fallback list in
> §3.2 is the agenda for that afternoon, and the honest-mirror content (the near-miss, the trackability
> table) survives any of them because none of it depends on how the model is turned.

## 6. What is already known without the headset

| Claim | Status |
|---|---|
| Three variants exist and are switchable in one build | ✅ `ReferenceHand.RotationMode`, `RotationTrialController` |
| All six cells are rated in a fixed order | ✅ controller drives the order; nothing depends on memory |
| Position drift is measurable | ✅ `ReferenceHand.PositionDrift`, sampled every frame while grabbed |
| Lag is measurable | ✅ `ReferenceHand.LagDegrees` = angle between the object and its target, sampled per frame |
| **Drift is zero by construction in A and B** | ✅ position is never written during a grab — so the interesting number is *lag*, not drift. Recording that now stops a zero being read later as evidence that the interaction is tight |
| Variant C actually reduces position noise | ⬜ **device** — the whole point of testing it |
| Any variant feels acceptable | ⬜ **device** — the entire verdict |
| The pose data survives being displayed while rotated | ⬜ **device** — a pose applied to a hand that is also being rotated is a new combination; if the hand twists oddly under rotation, that is a P2 finding, not a P1 failure, but it must be looked for |

## 7. The protocol (10 minutes with the headset on)

1. Launch. The panel says `ROTATION TEST cell 1/6 · A · Direct @ 0.40 m`.
2. Pinch the floating hand with the **right** hand. Turn your wrist through its comfortable range — palm
   up, palm down, half turn, back. Let go.
3. Judge honestly, then **pinch the left hand 1–5 times** and stop for 2 s. The score is recorded and
   cell 2 loads.
4. Repeat for all six. Six cells × about 60 s = ten minutes including the notes.
5. Pull `rotation-summary.txt`, paste the table into §4, write §5 the same sitting. **Not the next
   morning** — P1.3's second trap is following through to the next task without writing the verdict, and
   a verdict written a day later is a reconstruction.

## 8. Change log

| Date | Change |
|---|---|
| 15 Nov 2026 | File created. Criteria, decision rule and invalidation conditions written **before** the test. Three variants implemented in `ReferenceHand` with a controller that drives all six cells and records lag/drift per cell. Verdict blank by design. |
