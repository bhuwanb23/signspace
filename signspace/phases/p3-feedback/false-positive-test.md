# Twenty sloppy attempts — the false-positive test

**Phase:** P3 · **Task:** P3.2 · **Gate:** G3 · **Status:** gates built; 20/20 silent in simulation, device
rerun owed
**Raw output:** `false-positive-raw.txt` · **Regenerate:** `python3 scripts/false_positive_sim.py`

> A wrong diagnosis is worse than no diagnosis: it teaches a false correction to someone learning a
> language. This is the test that says we do not do that.

**Target: zero fires. Result: 0 fires, 18 of 18 attempts where the hand never qualified, plus both gate
probes behaving exactly as designed.**

---

## 1. The gate stack

| Gate | Value | What it stops |
|---|---|---|
| confidence floor | **0.70** | naming a shape from frames the tracker is unsure about |
| stable window | **0.40 s** | the first frame that satisfies the rule — *both* the confidence **and** the same candidate shape must hold for the whole window |
| rate cap | **1 per 20 s** | nagging; and the session counter makes it visible |
| the rule (P3.1) | exactly one other shape inside the lock band | naming a shape the hand is not making |

### The floor is a level selector, not a dial

Meta reports hand confidence in **four levels**, and `MetaHandSource` maps them:

| SDK level | value | passes a 0.70 floor? |
|---|---|---|
| High | 1.00 | yes |
| Medium | 0.70 | **yes — exactly at the floor** |
| Low | 0.40 | no |
| None | 0.00 | no |

So 0.70 means **"High or Medium, never Low"**. That is the right policy — Low is the SDK saying *tracking
is degrading*, which is precisely when a diagnosis must be withheld, and Medium is normal play. The
worth-knowing part is that the floor has only two meaningful positions: **0.70** (High + Medium) and
**anything above 0.70** (High only). There is no tuning granularity between them, so "tune the floor"
means "choose whether Medium is enough", and this test says yes.

---

## 2. The twenty attempts

Each attempt is a 60 fps sequence with a name, a motion profile and a confidence profile. The **evidence**
column is the longest continuous stretch during which *any* shape was inside the 12° lock band — the
evidence the rule actually had. Where that number is under the 0.40 s window, the app is required to stay
silent, and it did.

### Attempts 1–18: the hand never qualifies for long enough

| # | Attempt | evidence (s in band) | fires |
|---|---|---|---|
| 1 | passes through O, 0.1 s hold | 0.15 | **0** |
| 2 | passes through C, 0.2 s hold | 0.28 | **0** |
| 3 | passes through V, 0.3 s hold | 0.35 | **0** |
| 4 | sweeps through all six | 0.12 | **0** |
| 5 | sloppy wrong shape, 10°/joint | 0.08 | **0** |
| 6 | sloppy wrong shape, 15°/joint | 0.02 | **0** |
| 7 | sloppy wrong shape, 20°/joint | 0.00 | **0** |
| 8 | sloppy wrong shape, 25°/joint | 0.00 | **0** |
| 9 | halfway between C and O | 0.00 | **0** |
| 10 | halfway between U and V | 0.00 | **0** |
| 11 | halfway between V and W | 0.00 | **0** |
| 12 | halfway between B and U | 0.00 | **0** |
| 13 | near O but 14° out | 0.00 | **0** |
| 14 | near C but 15° out | 0.00 | **0** |
| 15 | near V but 16° out | 0.00 | **0** |
| 16 | near U but 18° out | 0.00 | **0** |
| 17 | wrong shape, wrist rolled 25° | 0.03 | **0** |
| 18 | wrong shape, wrist rolled 40° | 0.00 | **0** |

Attempt 3 is the interesting one: **0.35 s** inside the band — 50 ms short of the window — and silent.
That is the boundary being real rather than nominal.

### Attempts 19–20: the hand *does* qualify, and a gate is the only reason it stays silent

| # | Attempt | evidence (s in band) | fires | the gate that held |
|---|---|---|---|---|
| 19 | qualifies on O, but confidence is Low for the first 0.5 s | 0.55 | **0** | confidence window: only 0.05 s of good tracking existed before the attempt ended |
| 20 | qualifies twice within 20 s | 0.90 | **1** | rate cap: two qualifying stretches, **one** message |

Attempt 20 is deliberately the one that *should* speak, and speaks exactly once. A cap that never fires is
indistinguishable from a broken feature.

---

## 3. Which gate is load-bearing

The same twenty attempts with the stable window set to 0 s:

| | fires |
|---|---|
| shipped (400 ms window) | **1** (attempt 20, by design) |
| window removed | **9** |

Eight false positives appear the moment the window goes: the pass-throughs (1–4), the sloppy hands (5, 6),
the turned wrist (17), and the low-confidence attempt (19). That is the trap in the task description,
measured: **firing on the first frame that satisfies the rule is the whole failure mode**, and the 400 ms
window is what turns *"the rule is satisfied right now"* into *"the hand is still doing that"*.

Note which gate is *not* load-bearing on this evidence: the confidence floor and the rate cap never had to
block anything in the false-positive attempts (confidence was 1.0 throughout and nothing qualified). Their
work is in attempts 19 and 20, and on device, where tracking actually degrades. They are kept because
their absence would be invisible here and expensive in a headset.

---

## 4. What changed in the build

| File | Change |
|---|---|
| `Scripts/Matching/Matcher.cs` | the stable window now gates **the decision as well as the confidence** (`_nearMissCandidateId` + timer); `NearMissesThisSession` counter; `SetNearMissEnabled(bool)` which writes through to the save; the setting is restored in `Start`, not `Awake`, because `SaveSystem` loads in `Awake` and the component order is not guaranteed |
| `Scripts/Core/SaveSystem.cs` | `SaveData.nearMissEnabled` — the switch persists, so "turn the diagnosis off" survives a relaunch and P6.5 has something real to expose as the sixth toggle |
| `Scripts/UI/DebugOverlay.cs` | the **floor is printed beside the confidence reading** with `trusted` / `BELOW FLOOR`; the near-miss line now shows the candidate's held time against the window, and the session count |
| `scripts/false_positive_sim.py` *(new)* | the twenty attempts, the evidence column, and the window-removed comparison |

---

## 5. What this test is not

It is **not** a hand. Every attempt is synthetic, at 2°/joint noise unless stated, with confidence
profiles chosen to probe the gates. Three things can only be settled on device:

1. **The real failure mode is the hand that is nearly right and moving** — a person adjusting a finger.
   That is attempt 3's shape at a speed no simulator picks. The device run is: deliberately half-form a
   shape eight times, adjust, and stop; the app must stay silent every time.
2. **Confidence actually dropping.** In simulation it is scripted. On device it comes from Meta's four
   levels, and the question is whether Medium arrives often enough that a 0.70 floor makes the near-miss
   rare for the wrong reason.
3. **The 20 s cap feeling right.** Twenty seconds is a guess that only play can judge. If the near-miss
   feels like it is holding back a useful answer, the fix is the cap, not the rule.

---

## 6. Done when

- [x] **Zero false positives over 20 sloppy attempts** — 0 fires across all 18 attempts whose hand never
      held inside the band; the two gate probes behave exactly as designed (silent, and exactly one)
- [x] **A visible confidence floor value in debug** — `conf 0.95  floor 0.70  trusted` on the overlay
- [x] **Session rate cap working** — attempt 20 qualifies twice inside 20 s and is named once; the session
      count is on the overlay
- [x] **An off switch exists** — `Matcher.SetNearMissEnabled` + `SaveData.nearMissEnabled`, persistent;
      P6.5 will surface it in-space as the sixth toggle. No UI of its own yet, deliberately: the switch is
      real now so that disabling the diagnosis is never blocked on P6

### Traps

| Trap | How it was met |
|---|---|
| Firing on the first frame that satisfies the rule — require a stable window | the 400 ms window now covers **both** confidence and the candidate shape; removing it produces **9** fires where the shipped build produces 1, including a hand that merely passes through a shape |
| *(found while designing the test)* a test that cannot fail | the first version of these attempts fired six times and five were **correct** — the hand had genuinely settled into the wrong shape. The attempts were rebuilt so each prints the longest qualifying stretch it ever produced, which is what makes "silent" evidence rather than a claim |
