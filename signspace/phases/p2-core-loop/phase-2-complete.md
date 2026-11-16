# Phase 2 — Core loop: closing report

**Phase:** P2 · Day 3 · 10 h budget · **Gate:** G2 · **Workstream:** W2
**Status:** **all seven tasks built and measured** · **G2 not yet evaluated** (needs one stranger and one headset)
**Commits:** `1586d2f` P2.1 · `1cb5255` P2.2 · `3e7dbf3` P2.3 · `d4de382` P2.4 · `4dd66b1` P2.5 ·
`a5571d6` P2.6 · `f790322` P2.7
**Deadline:** 19 Nov 01:30 IST / 18 Nov 15:00 EST

> **G2 — can a stranger lock one handshape, uncoached, without anyone telling them what to do?**
> Everything below is in service of that one question. If it slips, the time comes from P7, never from P3.

---

## 1. Scoreline

| | |
|---|---|
| Tasks built | **7 / 7** |
| Gate criteria evidenced by measurement in this repository | **5 / 8** |
| Gate criteria that cannot be true without hardware | **3 / 8** (20/20 on device, the filmed payoff, the stranger) |
| C# files that have ever been compiled | **0 of 56** — there is no Unity editor in this environment |
| Simulators that regenerate a phase number | **7** (`tune_match`, `band_sim`, `hold_sim`, `payoff_previs`, `palm_sim`, `mirror_sim`, `lint_copy`) |

The phase did what a phase should do: it produced numbers, and **four of those numbers contradicted the
plan that asked for them** (§5). Every constant in the core loop is now traceable to a script that can be
re-run in seconds.

---

## 2. What each task shipped, and what it measured

### P2.1 — the pose format, frozen (`1586d2f`)
**Shipped:** schema 2 in `docs/02-tech/POSE-FORMAT.md`; `scripts/pose_schema.py` (validator + writer +
mirror), `validate_poses.py`; six authored poses in `StreamingAssets/Signs/`; three deliberately broken
fixtures the self-test must reject.
**Measured:** `--self-test` passes **29 cases**; 26 shapes, 0 problems. Capture-neutral is expressed as
code (`relative_to_neutral`) rather than prose, so "a different rest angle is compared as a shape, not as
a different wrist" is a function, not a promise.
**Why it mattered later:** two of the phase's later findings (§5) are only *expressible* because the
format carries `handedness`, `vantage` and `source`.

### P2.2 — the matcher, and the honest number (`1cb5255`)
**Shipped:** `PoseMath`/`Matcher`, weights data-driven in `Matching/Weights.asset` (Wrist+Palm 0.40 ·
Metacarpal 0.40 · Proximal 0.70 · Intermediate 1.00 · Distal 1.00 · Tip 0.00), the overlay's **three worst
joints**, `tune_match.py`.
**Measured:** nearest pair **26.0°**, noise p95 **6.7°** at 2°/joint — a **3.89×** headroom between "this
hand is right" and "this hand is a different shape". Both P2.2 traps avoided: no equal averaging over 26
joints, no normalising by joint count. Every number is degrees; **no percentage exists in the build**.
**Debt:** the header comment still claimed `closeDeg 20` until it was corrected at phase close (§8).

### P2.3 — three bands, as colour and motion (`3e7dbf3`)
**Shipped:** warming / close / locked, thresholds measured rather than rounded — **warming > 24°, close
12–24°, locked < 12°** with 2° release hysteresis and 150 ms dwell; colour + motion + sound, never text;
`lint_copy.py` to prove it.
**Measured:** 100% band survival at ≤ 12°, **0%** at ≥ 12.5°, **0.0% false close**; copy lint **0
violations** across 17 user-facing strings.
**Why the thresholds are those thresholds:** 24 is not "about 25" — the nearest wrong shape sits at 26.0°,
so any threshold above it would call a different word "close".

### P2.4 — the hold survives a dropout (`d4de382`)
**Shipped:** 0.7 s hold, 200 ms freeze grace, 0.5/s decay, 500 ms out-of-band forget, a **single latching
lock** (no re-fire), and `lock-log.csv` so a session leaves evidence.
**Measured:** **20/20 intentional locks, 0/20 false flicks**; **max survivable dropout 1.50 s**.
**Design rule honoured:** pause rather than penalise (ADR-003) — the app waits, it never says anything
went wrong.

### P2.5 — the shape becomes an object (`4dd66b1`)
**Shipped:** on lock the reference hand dissolves (scale + fade, 0–0.40 s), the object forms and arcs to
a temporary anchor, a chime plus a controller-free haptic substitute, and the hand returns. `LockPayoff`
asserts its own budget at runtime; `payoff_previs.py` renders the timing without Unity; a 6-frame
storyboard.
**Measured:** **0.99 s of a 1.20 s budget (82%)**; overlapping stages buy 0.10 s; after 3 locks the payoff
scales to 0.54 s so mastery does not feel slower than learning.
**Owed:** the footage. `lock-payoff-take01.mp4` does not exist; the previs is a previs.

### P2.6 — the palm is not a finger (`a5571d6`)
**Shipped:** `Matching/PalmOrientation.cs` — the palm described as a **basis** and compared as a rotation,
never decomposed into Euler angles.
**Measured:** inside 15° of slack the term contributes **exactly 0.0°**, so a differently-resting wrist is
not punished; from **30° of mis-facing up, 0.0% of frames can lock**; up to 20°, 100% lock. `k` was chosen
by arithmetic, not taste: `k·(30−15) > 12` ⇒ **k > 0.8**, so **k = 1.0** (margin +3.0°; k = 0.75 still
locks at 11.3°).
**The task's premise was wrong twice** (§5): the knuckle plane the brief specified is unbuildable on this
rig, and no authored pair differs in palm facing at all.

### P2.7 — an explicit decision not to (`f790322`)
**Shipped:** right-hand-only, decided and written down; the mirror derived (`(x, y, z, w)` →
`(x, −y, −z, w)`), the policy in one constant, three mirrored fixtures parked outside `StreamingAssets`,
the user-facing line in the honest mirror, and `UX-STATES.md §4.1` carrying the decision.
**Measured:** the mirror is **exact** (all 26 bones within 0.01°); the metric is **mirror-invariant to the
bit** (36 ordered pairs, wrist turns included); a mirrored pose locks **100%** where an unmirrored left
hand locks **0%** and scores **97–176°** on a perfect attempt.
**And a core-loop bug that had nothing to do with handedness:** both hands were bound to **one matcher**,
so any visible left hand overwrote the right hand's distance every frame and **the 0.7 s hold could never
complete**. Fixed, guarded and logged.

---

## 3. The constants, and where each one comes from

| Constant | Value | Source | Re-run |
|---|---|---|---|
| `lockedDeg` | 12° | above the noise (p95 6.7°), below the nearest pair (26.0°) | `tune_match.py` |
| `closeDeg` | 24° | nearest wrong shape 26.0°, so 24 is the largest honest "close" | `tune_match.py` |
| `bandReleaseDeg` | 2° (+150 ms) | flicker at the band edge | `band_sim.py` |
| `holdSeconds` | 0.7 s | long enough to be deliberate, short enough to feel immediate | `hold_sim.py` |
| freeze grace / decay / forget | 0.2 s / 0.5 per s / 0.5 s | a dropped frame must not restart the hold | `hold_sim.py` |
| payoff total | 0.99 s of 1.20 s | 82% of budget, after 3 locks 0.54 s | `payoff_previs.py` |
| `orientationSlackDeg` | 15° | ~10σ of a correct hand's wrist jitter (sd 1.41°, p99 6.43°) | `palm_sim.py` |
| `orientationWeightK` | 1.0 | smallest weight that keeps a 30° mis-face out of the lock band | `palm_sim.py` |
| weights | 0.40 / 0.40 / 0.70 / 1.00 / 1.00 / 0.00 | data-driven asset, never hardcoded | `tune_match.py` |
| copy | 0 banned strings | no failure language anywhere in the build | `lint_copy.py` |

---

## 4. Cross-cutting decisions made in this phase

- **One hand plays.** Right-hand-only, one place, with the reason attached (P2.7) — and a matcher that
  refuses a second driver rather than silently averaging two hands.
- **The palm is measured separately and added, not maxed** (P2.6). The old `max()` was a floor: a perfect
  hand facing 40° the wrong way read the same as one 12° off.
- **Nothing is tuned by taste.** Four times this phase the numbers disagreed with the brief and the numbers
  won (§5). Where a value had to be a judgement — 24 rather than 25, 15 rather than 20 — the judgement is
  written down next to the measurement that bounds it.
- **No percentages, no failure state, no red** (ADR-003), enforced by a lint rather than by memory.

---

## 5. Where the measurements overturned the plan

This is the part of the phase worth keeping. Each of these was a *premise in a task description*, and each
cost less to disprove than to build.

1. **The knuckle plane does not exist on this rig** (P2.6). The brief specified "wrist forward plus
   index-knuckle plane normal". Measured: this rig curls a finger **by rotating its metacarpal**, so on
   every closed shape the plane runs through two curled bones — its normal is **88–90°** from the palm's
   dorsal axis — and even on the flat hand the 11° fan amplifies tracking noise **5.2×**. Shipped: the palm
   frame, with the knuckle plane kept as a guarded refinement no shipped shape qualifies for.
2. **No two authored shapes differ in palm facing** (P2.6). All six face identically (0.0° across every
   pair), so the C/O and A/B orientation test the brief asked for **cannot be run** — A is not even in the
   set. The orientation term was tested with synthetic wrist turns instead, which is stronger; the *content
   gap* is flagged for P3 (**K vs P**).
3. **"Negate the X axis" is a rotation, not a reflection** (P2.7). Taken literally it puts bones up to
   **89.6°** from the true mirror and moves a thumb tip **44 mm** — while every finger still curls the way
   it should, so it looks like a hand. The derived form is exact. The brief's trap ("thumb opposition
   breaks first") is only partly right: the thumb is worst on **1 shape of 6**; what breaks is *any*
   rotation about Y or Z.
4. **A wired hand is not a supported hand** (P2.7). The build had both hands on one matcher, which would
   have blocked every right-hand lock whenever the left hand was visible — a G2-killing bug hiding inside
   a feature that was never claimed to work.

---

## 6. Pending — the device queue

One session, in this order. Everything here needs a Quest 3S and about 90 minutes; nothing here is design
work.

| # | Task | What to do | Pass looks like |
|---|---|---|---|
| 1 | P2.1 | `SIGNSPACE/Build Main Scene`, launch, check the joint report on device | 26/26 canonical joints mapped, OpenXR layout |
| 2 | P2.2 | Capture your own hand in each of the six shapes; read the distance | Locked band reached for all six; the honest-mirror numbers are these captures, not estimates |
| 3 | P2.3 | 30 s hold at the band edge, watching for flicker | no flicker; ≤ 2 transitions in 30 s |
| 4 | P2.4 | 20 deliberate locks, then 20 deliberate wrong shapes | **20/20 and 0/20** on hardware, `lock-log.csv` pulled |
| 5 | P2.5 | Record the 0:45 payoff shot | `assets/raw/video/lock-payoff-take01.mp4` exists and is the real thing |
| 6 | P2.6 | Hold B, lock, then roll the wrist 30–45° with the fingers still | band leaves Locked, overlay shows `O ≈ 30–45`, rolling back re-locks |
| 7 | P2.6 | **Also on this run:** does `O` stay 0.0° while the wrist visibly rolls? | If yes, the rig reports an identity wrist and the palm frame has collapsed — record it |
| 8 | P2.7 | `POSE-FORMAT §4.3` steps 2–3, then two hands in frame | mirrored pose locks; the right-hand lock survives a visible left hand |
| 9 | G2 | One stranger, no coaching, no explanation | they lock one shape, twice |

---

## 7. Limitations — stated, not smoothed over

**Unverified by construction**
- **No C# has ever been compiled.** 56 files, ~7 tasks of code, zero editor runs. The most likely
  first-run failures are ordinary: a missing `.meta`, a prefab reference that resolves to null, a
  namespace slip in the two newest files, `LockPayoff.prefab` opening with a missing mesh.
- **No device measurement exists for anything.** Every "measured" number in this report comes from a
  simulator that models the hand and a 2°/joint noise floor. The simulators are good — they have already
  caught four wrong premises — but they are not a headset.
- **`LockPayoff.prefab` is hand-authored YAML.** Guid-matched and brace-checked; never imported.

**Known content gaps**
- **No two shapes differ in palm facing**, so the app cannot yet distinguish two signs that differ *only*
  in orientation — even though the machinery that would is built and measured (P2.6). P3 should author
  **K vs P**.
- **The near-miss compares finger distances only**, so a mis-faced hand can be near-missed as a different
  shape. Defensible (it answers "what shape are your fingers making"), but it can fire alongside a palm
  verdict; P2.6 §5 records the fix if playtesting hates it.
- **Six shapes, all authored synthetically** from published references, **not yet reviewed by a Deaf
  signer**. `set-meta.json` says so and the credits object repeats it.

**Accessibility**
- **Left-hand play is off** — a third of people are left-handed, and this build gives them nothing until
  they find the honest mirror. It is one device test and one constant away, and that test is #8 above.

**Design debts left deliberately**
- A 45° mis-face reads **warming**, not close, because the palm term is additive and uncapped. Capping was
  tried on paper and rejected: any cap at the lock edge makes a 30° mis-face land at exactly 12.0° and
  lock (P2.6 §5).
- The reference hand's joints carry **no lateral offsets** (`HandVisual` never sets `localPosition`), so
  fingers radiate from one origin. Invisible to the metric — it is rotation-only — and the first thing to
  fix if the hand reads as a starfish on device.

---

## 8. Fixed during this phase that was not in any task

- `Matcher.cs`'s header documented `closeDeg 20` and a noise p95 of 8.5° — both stale, both now the
  shipped figures, with the palm term and the hold documented in the same block.
- Both hands driving one matcher (P2.7) — see §5.4.
- `gate.md` claimed P2.1 as the phase's only completed task while five were finished. Rewritten with an
  explicit rule: a tick means *measured in this repository*, and every tick names the device check it
  still owes.

---

## 9. Phase traps — did they bite?

| Trap | Verdict |
|---|---|
| Tuning by taste instead of against fixtures | **Not once.** Every threshold has a script and a bound (24 sits under the 26.0° nearest pair; 12 sits above the 6.7° noise p95; k = 1.0 is the smallest value that clears the lock band). Where taste entered — "15° of slack" — it was then checked against a measured σ |
| Showing a percentage | **Never.** `D` is degrees everywhere, and `lint_copy.py` treats a percentage score as a forbidden pattern |
| Fixing a feel problem by adding a feature | **Once, and it was rejected in writing.** The 45°-mis-face "amber" case could have been solved with a fourth band or a capped penalty; both were recorded as rejected with the reason (P2.6 §5) |
| *(from the brief)* mirrored poses are not always valid; half-support is worse than none | **Confirmed the hard way** — and the "half-support" turned out to be an active bug in the core loop, not just a missing feature |

---

## 10. What P3 inherits

1. **The device queue in §6** — it is not optional, and items 2, 4, 6 and 8 are the ones that turn this
   phase's simulated numbers into claims that can be defended.
2. **Author K vs P** — the first content that exercises P2.6's palm term for real.
3. **Keep the metric closed under mirroring.** It is, to the bit (P2.7). Any future term that reads
   positions or a cross product must be checked against `scripts/mirror_sim.py` §2 before it ships,
   because that is the property that makes handedness cheap.
4. **The rule that made this phase work:** when a number contradicts a task description, re-derive the
   number and write down why — four times this phase, the task was the thing that was wrong.

---

## 11. Artefact index

| Kind | Where |
|---|---|
| Gate | `gate.md` · tasks `tasks/p2.1 … p2.7` |
| Tuning records | `matcher-tuning.md` · `band-tuning.md` · `hold-and-dropout.md` · `lock-payoff.md` · `palm-tuning.md` · `left-hand-decision.md` |
| Raw simulator output | `hold-sim-raw.txt` · `payoff-timing.txt` · `palm-sim-raw.txt` · `mirror-sim-raw.txt` |
| Evidence on disk | `lock-log.csv` · `lock-payoff-storyboard.png` · `mirrored-fixtures/` (3, validated, not shipped) |
| Code | `Scripts/Matching/*` (metric, weights, palm, hands) · `Scripts/UI/*` (bands, payoff, overlay, honest mirror) · `Scripts/Core/*` (payoff, logging, save) |
| Scripts | `tune_match` · `band_sim` · `hold_sim` · `payoff_previs` · `palm_sim` · `mirror_sim` · `lint_copy` · `pose_schema`/`validate_poses` |
| Owed | `assets/raw/video/lock-payoff-take01.mp4` · four own-hand fixtures · the device log from §6 |
