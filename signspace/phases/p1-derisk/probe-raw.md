# P1.5 — probe raw notes

**Task:** P1.5 · twenty candidate handshapes, real hands, two lightings, both hands · **Gate fed:** G1
**Written:** 15 Nov 2026 · **Status:** 🟡 rig and twenty reference poses built, **no results yet — needs the headset**

> This is the raw-notes file P1.5 names. The finished numbers go into
> [`docs/03-content/TRACKABILITY.md`](../../docs/03-content/TRACKABILITY.md) §3; this page holds what
> led to them, including what was wrong with the probe before it was run.

---

## 1. What exists now that did not before

| | Before | Now |
|---|---|---|
| Candidate poses | 6 (the shipped set) | **20** — the six plus A, D, E, F, G, I, L, M, N, R, S, T, X, Y, in `StreamingAssets/Probe/`, probe-only and not shippable |
| Measurement | manual judgement from memory | automatic per 10 s hold: **tracked %**, **lock %**, **wrong-shape %**, **dropouts**, mean distance — *plus* the operator's judgement, captured at the moment of judging |
| "Tracks as a different shape" | inferred from a rate-limited near-miss event (once per 20 s) | `Matcher.BestOtherId` / `BestOtherDegrees`, sampled per frame |
| Harness | none | `ProbeController` + `Editor/ProbeBuilder.cs` |

## 2. The twenty, and what each is expected to do

Order matters: if a session is cut short, the consequential shapes are already measured.

| # | Shape | Authored confidence | Expected (from `SIGNSET.md` §7) | Nearest other, by our own metric |
|---|---|---|---|---|
| 1 | **B** | shipped | High | I, 94.0° |
| 2 | **U** | shipped | High — watch finger separation | **R, 14.6°** |
| 3 | **V** | shipped | High | U, 26.2° |
| 4 | **W** | shipped | Medium-high — ring finger is the weak link | R, 95.4° |
| 5 | **C** | shipped | Medium — curvature without contact | O, 70.2° |
| 6 | **O** | shipped | Medium-high — contact may *help* | E, 46.5° |
| 7 | A | high | High — thumb alongside vs across is the ambiguity | T, 50.8° |
| 8 | **M** | low | **expected to fail** — depth ambiguity | T, 29.5° |
| 9 | **N** | low | **expected to fail** | O, 70.1° |
| 10 | **T** | low | **expected to fail** | M, 29.5° |
| 11 | **R** | low | **expected to fail** | **U, 14.6°** |
| 12 | **S** | low | **expected to fail** | M, 41.8° |
| 13 | D | medium | Medium — ring/pinky-to-thumb contact | T, 106.1° |
| 14 | E | low | **expected to fail** — thumb position unresolvable | O, 46.5° |
| 15 | F | low | Low-medium — hardest in the set | X, 96.0° |
| 16 | G | low | **expected to fail** — orientation-dependent | L, 21.6° |
| 17 | I | high | Medium — isolated pinky is the weakest joint | B, 94.0° |
| 18 | L | high | High | G, 21.6° |
| 19 | X | medium | Medium | O, 80.0° |
| 20 | Y | high | High | T, 96.6° |

The right-hand column is **measured, not assumed**: every pair of the twenty was run through the v2
chain metric (`scripts/tune_match.py`'s own function, loaded against the written JSON files) before any
device work, to check the probe can tell its own references apart. Anything under the shipped 12° lock
threshold would mean the probe could report "wrong shape" for a reason that has nothing to do with the
sensor.

**That check found a bug in the probe itself.** M and N first came out **10.3° apart** — inside the lock
threshold — because the two references had been authored with effectively identical joint angles. The app
could not have told them apart no matter how well the sensor performed. N was re-authored (ring and
little loosely curled rather than stacked, which is what the shape actually looks like) and the pair
moved to 29.5° / 70.1°. **Two shapes that are indistinguishable in the reference data would have been
reported as a sensor failure.**

Two entries remain deliberately inside the "close" window — **U↔R at 14.6°** and **L↔G at 21.6°**. For
those, an occasional wrong-shape frame is expected and is *not* evidence of sensor failure; the notes
column is where that gets attributed at the time rather than argued about later.

## 3. The four sessions

| Run | `condition` | `hand` | Purpose |
|---|---|---|---|
| 1 | Daylight | Dominant | the baseline everyone quotes |
| 2 | Daylight | Other | hand size changes occlusion — TRACKABILITY.md §2.2 step 5 |
| 3 | Lamplight | Dominant | **the one that matters** — judges don't live in a lab |
| 4 | Lamplight | Other | worst case, for the honest mirror's greyed-out panel |

Relaunch between sessions. It clears the tracker's own adaptation, which is roughly what a judge's first
minute looks like — and it means a session that goes wrong can be thrown away without losing the others.

## 4. Results — to be filled on device

One table per session. `X` = not yet run.

### 4.1 Daylight · dominant hand

| Shape | Manual | Lock % | Wrong % | Dropouts | Confused with | Trackable? |
|---|---|---|---|---|---|---|
| B | | | | | | |
| U | | | | | | |
| V | | | | | | |
| W | | | | | | |
| C | | | | | | |
| O | | | | | | |
| A | | | | | | |
| M | | | | | | |
| N | | | | | | |
| T | | | | | | |
| R | | | | | | |
| S | | | | | | |
| D | | | | | | |
| E | | | | | | |
| F | | | | | | |
| G | | | | | | |
| I | | | | | | |
| L | | | | | | |
| X | | | | | | |
| Y | | | | | | |

### 4.2 Daylight · other hand
*(same table, `probe-daylight-other-summary.txt`)*

### 4.3 Lamplight · dominant hand
*(same table, `probe-lamplight-dominant-summary.txt`)*

### 4.4 Lamplight · other hand
*(same table, `probe-lamplight-other-summary.txt`)*

```
adb shell run-as com.signspace.signspace cat \
  /sdcard/Android/data/com.signspace.signspace/files/probe-daylight-dominant-summary.txt
```

## 5. How the three numbers are defined — fixed before the data exists

| Column | Definition | What it means when it is bad |
|---|---|---|
| **lock %** | fraction of the 10 s whose match distance was inside the lock band (12°) | low = the shape never registers; could be the sensor *or* the reference pose |
| **wrong %** | fraction of the 10 s where a **different** shape was the nearest match | **the dangerous one.** This is what teaches a learner the wrong letter. High wrong % means the shape must not ship |
| **dropouts** | count of tracked → lost transitions during the hold | high = flickers. `TRACKABILITY.md` calls flicker the dangerous category alongside wrong-shape |
| **manual** | High / Medium / Low / Never, from the operator, at the moment of judging | the human half. Where it disagrees with the numbers, that disagreement is the finding |

**Attribution rule, decided now:** a low score on a shape whose reference pose is marked
`authoringConfidence: low` (M, N, T, R, S, E, F, G) cannot on its own distinguish "the sensor cannot see
this" from "our reference for this is wrong". Those rows get written up as **"not separable by this
method"** unless the wrong-shape or dropout numbers point clearly one way. The honest mirror may then
say "we could not measure this reliably" — which is a publishable, defensible statement, and truer than
guessing.

## 6. What the phase needs back

1. ≥ 20 shapes × 2 lighting conditions tested → **G1 criterion 5**
2. Failure taxonomy recorded (never / flickers / wrong shape) → **criterion 6**
3. A first honest count of trackable shapes → `TRACKABILITY.md` §3, and the **≥ 8 of 20** gate line

## 7. Change log

| Date | Change |
|---|---|
| 15 Nov 2026 | Fourteen candidate references authored and validated (20 shapes, 0 problems). Separability checked before device work; M/N bug found and fixed. Harness written. No results. |
