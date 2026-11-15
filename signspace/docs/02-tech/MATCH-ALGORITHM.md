# Match Algorithm

**Status:** ✅ v1 shipped in code · **Owner:** W2 · **Phase:** P2 · **Gate:** G2
**Implementation:** `unity/Assets/_Project/Scripts/Matching/{PoseMath,Matcher}.cs`

---

## 1. The one design decision

**The output is a distance in degrees, never a percentage.** A percentage invites the user to optimise a number; degrees describe a shape. It also makes the bands explicable: "you are 9° off" is a sentence a person can act on.

## 2. The distance

For every joint, the angle between its stored local rotation and the user's:

```
θ_j = 2 · acos( | w | )  ·  (180/π)        where (x,y,z,w) = q_target⁻¹ · q_current
```

`|w|` handles the q ≡ −q ambiguity — without it, roughly half of all correct poses report ~360° of error.

Then a weighted mean over the finger joints:

```
D_fingers = Σ( w_j · θ_j ) / Σ( w_j )
```

and an **independent** palm-orientation term, because several sign pairs differ mainly in which way the palm faces:

```
D = ( D_fingers · Σw  +  w_palm · max(0, θ_palm − slack) ) / ( Σw + w_palm )
```

### Weights

| Group | Default | Why |
|---|---|---|
| Proximal (wrist-adjacent) | **0.40** | Least informative — the whole hand rotates together |
| Mid phalanx | **0.70** | Distinguishes curl from extension |
| **Distal / tip** | **1.00** | Carries the shape. This is where U→V lives |
| Thumb | **0.80** | Slightly down-weighted: it is the noisiest joint and the most often occluded |
| Palm orientation | **w_palm = 1.5**, **slack = 15°** | Slack because wrist angle varies legitimately with seating position |

All of these are inspector fields on `Matcher`, so tuning never requires a recompile.

## 3. Bands

| Band | Distance | Visual | Never |
|---|---|---|---|
| **Locked** | ≤ **12°** | settles, warms, begins to fill | — |
| **Close** | ≤ **24°** | leans toward the target | — |
| Warming | > 24° | neutral, attentive | — |

**Hysteresis: 150 ms.** The band only changes after the new state has held for 150 ms. Without it the meter strobes on a shaky hand, and a strobing meter reads as a broken app.

**There is no failure state.** No red, no X, no "try again". The band can only ever be *further from* or *closer to* locked.

## 4. The hold

A shape must be **held**, not touched.

| Rule | Value | Reason |
|---|---|---|
| Time inside the locked band to count | **0.7 s** | Long enough to exclude a pass-through, short enough not to feel like a wait |
| Tracking below the confidence floor | **pause** — timer neither advances nor resets | A single dropped frame must never erase 0.6 s of correct work |
| Confidence dips shorter than | **0.2 s** | Ignored entirely |
| Leaving the locked band for longer than | **0.5 s** | Resets progress — otherwise a user could bank progress by flicking through |
| After a lock | latched until the hand leaves the band | Prevents double-firing |

## 5. The near-miss

The product's signature behaviour. Every frame, the distance to **all six shapes** is computed — the cost is microseconds (6 × ~21 joint angles) and it buys the whole feature.

```
fire if:  band != Locked
     and  D(bestOther) <= 18°
     and  D(bestOther) <  D(target)
     and  exactly one other shape satisfies the two conditions above
     and  confidence stayed above the floor for the last 0.4 s
     and  cooldown (20 s) has elapsed
```

**Ambiguity is not a diagnosis.** If two or more shapes qualify, stay silent.

**Silence is the default on any doubt.** A wrong diagnosis teaches a false correction to someone learning a language — strictly worse than saying nothing. The cooldown exists so it stays rare enough to stay valuable.

## 6. What the matcher deliberately does not do

| Not done | Why |
|---|---|
| Absolute joint **positions** | Hand size and arm position would break it. Rotations only |
| Scale normalisation of the user | Unnecessary — the comparison is already scale-free (see `POSE-FORMAT.md` §4) |
| A trained ML classifier | Six static shapes do not need one; a model would add latency, opacity and a failure mode we cannot debug on Day 4 |
| Motion / trajectory matching | Out of scope in v1 (`ADR-004`) |
| A pass/fail boolean | `ADR-003` — forgiveness over accuracy |

## 7. Tuning procedure (do this, don't guess)

1. Load the six shapes. With one target, hold it correctly and log `D` for 30 s → gives `D_correct` (mean + 2σ).
2. Hold it deliberately wrong in three ways (thumb, spread, curl) → gives `D_wrong_min`.
3. Set `lockedDeg` = `D_correct` mean + 2σ, and `closeDeg` halfway to `D_wrong_min`.
4. Record both numbers in §8 with the date and the hand used.

**Priors to start from** (they will move): locked 12°, close 24°.

## 8. Measured values

| Date | Device | lockedDeg | closeDeg | Notes |
|---|---|---|---|---|
| 14 Nov 2026 | — | 12 (prior) | 24 (prior) | not yet measured on device |
