# Validation without a headset

Every phase gate in this repository has rows that say *needs a headset*. Those rows are real and stay
unrun until someone puts a device on. But "needs a headset" was being used as a blanket excuse: most of the
chain from pose to band to shelf is ordinary C# that runs perfectly well on a laptop, and the only thing
missing was something to put a pose in.

This document is the honest boundary. Everything here is **evidence about wiring and logic** and **none of
it is evidence about a Quest**.

---

## 1. What runs at a desk

| Method | Command | Answers |
|---|---|---|
| Metric + noise floor | `python scripts/tune_match.py` | Do the six shapes separate? What lock band survives 2°/joint? |
| Band behaviour | `python scripts/band_sim.py` | Do the bands flicker? Where is the 2.33σ edge? |
| Palm term | `python scripts/palm_sim.py` | Does a mis-facing hand get penalised, and a resting wrist not? |
| Hold + latch | `python scripts/hold_sim.py` | Does a dropout reset progress or freeze it? |
| False positives | `python scripts/false_positive_sim.py` | How often does the wrong shape read as the target? |
| Mirror | `python scripts/mirror_sim.py` | Is the reflected left hand the same shape? |
| **Per-shape margins** | `python scripts/noise_margin_sim.py --csv …` | For each shape: self p95 vs the nearest rival's p05 |
| **Scripted session** | `Unity.exe -batchmode … -executeMethod Signspace.Editor.ScriptedSession.RunBatch` | Does the real Matcher lock, latch, and name a near-miss? |
| Play Mode driver | `SIGNSPACE/Dev/Add scripted hand driver` | The whole scene, by eye, with a hand that never tires |

### The scripted session

`ScriptedHandSource` (dev-only, disables itself in a release build) writes a canonical 26-joint
`localRotation` array from a shipped pose file. `SkeletonPoseSource` reads exactly that array, so the
driver is not a reimplementation of the matcher — it is the same code path with a different source of
quaternions.

Batch result, 19 Nov 2026, fixed 1/72 s step:

```
  PASS  correct shape locks (B)                          locked=B at 0.71s, band changes=1
  PASS  wrong shape never locks (target B, hand O)        locked=no, nearMiss=O at 0.40s
  PASS  near-miss names the shape actually made (B ← V)   locked=no, nearMiss=V at 0.40s
  PASS  2 deg/joint noise still locks (U)                 locked=U at 0.71s
  PASS  4 deg/joint noise is the honest edge (B)          locked=B at 0.71s
```

What those five rows actually prove: the hold ends at 0.71 s (0.7 s + one frame) rather than early or late;
the latch does not fire on a hand that is 133° from the target; the near-miss reports the shape the hand
*is*, not the shape it was asked for; and deterministic noise does not stop a correct hand from locking.
They are re-runnable by anyone with the repo and Unity, which is the point.

The Play Mode driver adds what batch cannot: the drawn hand, the shelf, the audio and the probe's JSONL,
moving with a pose rather than with a real hand. It proves the scene is wired. It proves nothing about how
the scene feels.

---

## 2. What none of this can answer

- **Frame rate and thermals.** No simulation knows the GPU. The 72 fps budget is a design constraint until
  measured (`PERFORMANCE.md`).
- **Real tracking noise.** Our model jitters each joint independently at a stated σ. Real hands have
  correlated error, occlusion and dropped frames. `TRACKABILITY.md` requires a device row precisely because
  this model is optimistic.
- **Comfort.** Seated, one-handed, 10-minute session — a claim only a person can make.
- **Strangers.** G8 is three uncoached testers, and a scripted hand is not a stranger.
- **Installability.** Only `adb install` on real hardware settles that.

So the rule stands unchanged: **a device row in `TRACKABILITY.md` is filled by a measurement, never by a
simulation.** What changed is that the other rows are now cheap to run instead of being blocked behind the
same headset.

---

## 3. Why this matters for the submission

A judge reads claims. The strongest thing this repository can say is not "it works" — it is "here is what
works, here is the command that shows it, and here is what we refuse to claim until a device says so".
`DESK-VALIDATION.md` plus `reverify-log.md` is that sentence in two files: one proves the logic, the other
records that the entry itself is still owed.
