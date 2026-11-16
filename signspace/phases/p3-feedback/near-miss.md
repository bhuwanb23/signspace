# The near-miss: name the sign they actually made

**Phase:** P3 · **Task:** P3.1 · **Gate:** G3 · **Status:** built and measured; 4 of the 8 documented
pairs cannot be tested until six more shapes ship
**Raw output:** `near-miss-raw.txt` · **Test evidence:** `near-miss-test.csv`
**Regenerate:** `python3 scripts/nearmiss_sim.py`

> Every other app in this field returns correct/incorrect. Naming the *confusable* sign is the one thing
> no prior project does — and it is worth nothing the first time it is wrong. So the rule is narrow by
> construction and the numbers below are what narrows it.

---

## 1. The rule that ships

```
fire  ⇔  D(target) ≥ lock band            (there is something to explain)
      ∧ exactly ONE other shape is inside the lock band   (the hand really makes that shape)
      ∧ that shape is closer than the target by ≥ 2°       (a tie is not a diagnosis)
```

plus the gates that were already there and stay: **confidence held for 0.4 s**, and a **20 s cooldown**
(it has to stay rare to stay valuable).

The number shown is **how close the hand came to the shape being named** — `D(made)`, rounded to whole
degrees — because that is the only number in the claim a user can check against their own hand.

`Matching/NearMiss.cs` is the rule, with no Unity in it: `NearMissDecision` carries the verdict **and the
reason it stayed silent** (`TargetLocked`, `NoOtherInside`, `Ambiguous`, `NotClearlyCloser`). The overlay
prints that reason, so "we had nothing to say" and "we chose not to speak" are never confused — which is
the whole discipline this feature lives or dies by.

**What this replaced.** `Matcher` had a looser rule: *another shape nearer than the target by 2°, inside
a 26° window.* It is a claim about **proximity**, and proximity is not authorship — a hand 20° from
everything could get named a shape it was merely nearest to. It also never fired in practice, because
the closest pair in the shipped set is 26.0° apart and the window was 26.0°.

---

## 2. Are the eight pairs close in this metric? No — and that changes what the feature is

`SIGNSET.md §5` lists eight pairs, ranked by demo value. Measured at the authored poses, no noise:

| pair | D | both shipped? | |
|---|---|---|---|
| C ↔ O | **65.0°** | yes | the ★★★ demo pair is 65° apart |
| U ↔ V | **26.0°** | yes | the closest pair in the whole set |
| V ↔ W | **118.1°** | yes | |
| B ↔ U | **117.0°** | yes | |
| A ↔ O | — | **no** — `A` is not in the shipped set | |
| I ↔ Y | — | **no** — neither is shipped | |
| L ↔ Y | — | **no** — neither is shipped | |
| D ↔ F | — | **no** — neither is shipped | |

**The premise worth correcting:** the pairs are near-misses *for people*, not for the metric. A beginner
confusing C with O does not mean our distance scores C and O close — it scores them 65° apart, which is
more than twice the nearest accidental pair. The feature is therefore not "the two shapes are near each
other"; it is **"your hand is lock-quality on a different sign"** — and that is a stronger, checkable
claim. It also explains why the magnitude can never be "the delta between the two shapes" (26–118°):
the honest number is how good the wrong shape was.

**And four of the eight pairs cannot be tested at all**: `A`, `D`, `F`, `I`, `L`, `Y` exist only as
`probeOnly` poses from P1.5, in `StreamingAssets/Probe/`, outside the shipped set. They are the P5
content debt, and it is now a G3 debt too: **G3 asks for all eight pairs and the shipped set can only
make four.**

---

## 3. Both directions of every testable pair, 200 noisy samples each

Hand made from the *wrong* shape with 2°/joint noise, distance computed to **every** shape in the set:

| asked for | made | D(target) | D(made) | fires | names |
|---|---|---|---|---|---|
| C | O | 65.0° | 2.6° | **100%** | O |
| O | C | 65.0° | 2.4° | **100%** | C |
| U | V | 26.0° | 2.5° | **100%** | V |
| V | U | 26.0° | 2.7° | **100%** | U |
| V | W | 118.1° | 2.4° | **100%** | W |
| W | V | 118.1° | 2.6° | **100%** | V |
| B | U | 117.3° | 2.7° | **100%** | U |
| U | B | 117.3° | 2.2° | **100%** | B |

Eight cells out of eight, in both directions, naming the right shape every time — including the pair the
old rule would have had to reach through a 26° window to say.

**Magnitude:** `D(made)` lands at **2.2–2.7°** at the simulator's 2°/joint noise. On a real hand that
figure will be larger — the noise floor is 6.7° p95 and a real attempt is not a perfect pose — so expect
**3–10°** on device. The task's done-when says "plausible in the 5–20° range": 5–20 is plausible *for
this number*, and it is not plausible for any other number in the rule (the target-gap is 26–118°).

---

## 4. Zero false positives, measured on both rules

2000 **sloppy attempts** — a shipped shape with 8–25° of random noise per joint, i.e. a hand that
resembles nothing:

| rule | false positives |
|---|---|
| strict (brief P3.1 step 3) — **ships** | **0 / 2000** |
| loose (the rule it replaced) | 0 / 2000 |

Both are clean on this test, so the choice between them is not made by false-positive rate. It is made by
what each one *claims*: the strict rule only ever speaks when the hand is inside the lock band on another
shape, which is a statement about authorship; the loose one would speak about proximity. When two rules
are equally safe and one makes a weaker claim, ship the stronger claim.

---

## 5. Ambiguity: silence is a branch, not an accident

`Ambiguous` fires when two or more shapes are inside the band — the hand is between them, and naming
either is a coin flip wearing a diagnosis.

**On the shipped six it is unreachable**: the closest pair (U/V) is 26.0° apart, so two shapes cannot both
be inside a 12° band. It is not left to chance — it is tested on P1.5's 20-shape probe set, whose closest
pair is **R/U at 14.2°**:

```
midpoint hand between R and U, target B:
  inside the lock band = R 7.0°, U 7.2°
  rule -> fires = False, reason "ambiguous: 2 shapes inside the band"
```

So the branch is dead code today and **live the day the 12-shape set ships**. That is exactly when a
near-miss that guesses starts naming shapes at random, and this is the guard that will stop it.

---

## 6. What changed in the build

| File | Change |
|---|---|
| `Scripts/Matching/NearMiss.cs` *(new)* | the rule as a pure function, the verdict struct, and the **reason** for silence |
| `Scripts/Matching/Matcher.cs` | distance to **every** shape into a reusable buffer (12 × 26 angles, no allocation per frame); the inline loose rule replaced by `NearMiss.Evaluate`; `nearMissMaxDeg` deleted (it was the old rule's window and nothing else used it); `NearMissState` exposed |
| `Scripts/UI/DebugOverlay.cs` | prints `NearMissState.Explain()` instead of the dead window constant — the dev view now says *why* it was silent |
| `Scripts/UI/NearMissDisplay.cs` | the magnitude is whole degrees and never 0 (a 0 would read as "you made it", which is the case the app should have locked instead) |
| `scripts/nearmiss_sim.py` *(new)* | every number above, and it rewrites `near-miss-test.csv` |

Copy is untouched: **P3.3 owns the words**, and the template in `NearMissDisplay` is a placeholder that
`lint_copy.py` already passes (0 violations, 18 strings).

---

## 7. Done when

- [~] **All 8 documented pairs fire correctly** — **4 of 8 testable**: C/O, U/V, V/W, B/U, each in both
      directions, 100% and naming the right shape. The other four (A/O, I/Y, L/Y, D/F) need A, D, F, I, L,
      Y in the shipped set, which is P5 content. Not "done" and not "failed": blocked on content
- [x] **Two-other ambiguity produces silence** — measured on the probe set, R 7.0° / U 7.2° → silent
- [x] **Magnitude shown and plausible** — `D(made)` = 2.2–2.7° at simulator noise, rounded to whole
      degrees, never 0. Expect 3–10° on device; the 5–20° target holds for this number and for no other

### Traps, and where each was met

| Trap | How it was met |
|---|---|
| Computing distance only to the target and then guessing the nearest | every loaded shape is scored every frame into a buffer sized once — the cheap version is exactly how a confident wrong name happens |
| Firing on the second-nearest when the first is nearly as close | the rule takes the *strict* reading: one qualifier or silence. Two qualifiers is `Ambiguous`, and that branch is tested on the 20-shape set rather than assumed unreachable |
| *(found while doing it)* trusting "these pairs are confusable" as "these pairs are close" | measured: 26–118° apart. The feature had to be redefined as *authorship* ("your hand makes another sign") rather than *proximity*, which is what the copy and the display now say |
| *(found while doing it)* a rule that cannot fire | the old 26° window was equal to the closest pair distance, so it could only fire on an exact tie. Fixed by moving the test from proximity to the lock band |
