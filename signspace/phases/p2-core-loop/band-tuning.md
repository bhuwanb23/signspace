# Band tuning — three bands, no failure state

**Phase:** P2 · **Task:** P2.3 · **Gate:** G2 · **Status:** thresholds measured and shipped; the
30-second device test is the one thing left open
**Raw output:** `phases/p2-core-loop/band-sim-raw.txt` (regenerate with `python3 scripts/band_sim.py`)

> There is no fourth band. There is no "wrong". The distance is always expressing one of three things —
> *I can't see you yet*, *you're on your way*, *you're here* — and the learner is never told that they
> failed, because in this product failing does not exist as a state (ADR-003, `UX-STATES.md`).

---

## 1. The three bands as shipped

| Band | Entered at | Left at | Colour | Motion | Sound |
|---|---|---|---|---|---|
| **Warming** | `D > 24°` | — | amber `#C77529` | slow breath, 0.25 Hz, ±2% scale | none — waiting is silent |
| **Close** | `D ≤ 24°` | `D > 26°` | teal `#29A6A1` | pulse 0.8 Hz rising to full amplitude as `D` falls; ring contracts `1.00 → 0.42` | a soft rising two-note interval, 300 ms |
| **Locked** | `D ≤ 12°` | `D > 14°` | warm gold `#F2E3C2` | one 120 ms snap on arrival, then **completely still** | a single high tone, 120 ms |

Everything is a colour and a motion. **There is not one string in the feedback path** — no "close!",
no "try again", no number the learner has to read while their hands are busy being the thing they are
concentrating on. The reason is in §5.

Two asymmetries, both deliberate:

- **Stillness is the reward.** Warming breathes, close pulses, locked does not move at all. A meter that
  keeps moving after success makes success feel like continued effort, and the one thing a newly locked
  hand needs to feel is *done*.
- **Leaving costs more than entering** (`release = 2°`). Entering a band uses the plain edge; leaving it
  needs the hand 2° past it. §4 is the evidence for why.

---

## 2. What a degree means — the calibration

The thresholds are only meaningful if `D` reads back something a person can act on. Measured
(`band_sim.py` §0): take a target hand, rotate every joint of one finger by a known error, add the
shipped 2°/joint tracking noise, and measure `D`.

| True per-joint error of the worst finger | Measured `D` | sd (one finger off) | sd (whole hand off) |
|---|---|---|---|
| 0° | **2.4°** | 0.51 | 0.51 |
| 4° | 4.3° | 0.56 | 0.48 |
| 8° | 8.2° | 0.60 | 0.47 |
| **12°** | **12.1°** | 0.60 | 0.46 |
| 16° | 16.1° | 0.60 | 0.46 |
| **24°** | **24.1°** | 0.60 | 0.46 |
| **26°** | **26.1°** | 0.60 | 0.46 |
| 30° | 30.0° | 0.60 | 0.45 |

Three facts fall out of that column, and they set everything below:

1. **`D` is calibrated.** A 12° edge means "every joint of my worst finger is about 12° off". The
   weighted mean within the finger (P2.2) cancels to a straight read-back of the error.
2. **A perfect hand reads 2.4°, not 0.0°.** The noise floor of the metric is ~2.4° even with the hand
   exactly right, so no threshold below ~3° could ever be meaningful. This is also why the overlay's
   readout is never treated as "0 = perfect" in any copy.
3. **The jitter is small — 0.46–0.60° sd, not the 6.7° p95 in `matcher-tuning.md`.** That larger figure
   was the noise floor of a *near-zero* distance, where the max-over-fingers term jumps between five
   noisy fingers. With a real error present the max is pinned to the finger that is actually wrong, and
   the metric is quiet. Which of the two matters depends on the question: for *detecting* a shape the
   p95 is the honest number, for *holding a band at an edge* the 0.6° sd is.

---

## 3. The lower edge (lock) — 12°

`D < 12°` per the design, and the measurement says the edge means what it says: a hand parked within
12° banks the 0.7 s hold essentially every time, and a hand parked 0.5° outside it never does.

| Parked at | Hold completes within 2 s |
|---|---|
| 4° | 100% |
| 8° | 100% |
| 10° | 100% |
| 11° | 100% |
| **12°** | **100%** |
| 12.5° | 0% |
| 13° | 0% |
| 14° | 0% |

The transition is a cliff rather than a slope, and that is a property of the *timer*: at 12.5° a hand
is inside the band on about 20% of frames, which banks ~0.4 s in a two-second attempt — short of the
0.7 s required. The timer does not need a long run, but it does need enough frames, and 12° is where
that stops being true.

> **Correction (P2.4, 16 Nov).** An earlier version of this section reported that the *effective* lock
> edge was about 10° rather than 12°, that a hand at 11° completed the hold only 28% of the time, and
> that the fix belonged in P2.4's grace window. That was a bug in the simulation, not a finding: the
> survival test gave the simulated hand exactly 0.7 s of wall time to accumulate 0.7 s of in-band time,
> which silently required *every* frame to be inside the band — a run, not a timer. The matcher
> accumulates cumulatively (and always has), so the correct question is whether the hand banks 0.7 s
> within a couple of seconds, and with a two-second window the answer is 100% all the way out to the
> edge. `band_sim.py` now measures it correctly; the hand-off note to P2.4 is withdrawn.

The design's 12° therefore stands on its own, and the only number in this document that still needs a
device is the upper edge's noise margin (§5).

## 4. Hysteresis — and the number that decides it is not 150 ms

Step 4 of the task asks for a 150 ms dwell. 150 ms is necessary and, on its own, not sufficient.
Measured: a hand parked exactly on the 12° edge for 30 s at 72 Hz, 300 trials, counting **displayed**
band changes.

| Damping | Mean changes / 30 s | Worst trial |
|---|---|---|
| none (`band = f(D)`) | **1083.3** | 1153 |
| 150 ms time hysteresis *(the spec)* | 0.45 | **4** |
| 150 ms + 1° release | 0.00 | **0** |
| **150 ms + 2° release** *(shipped)* | **0.00** | **0** |
| 150 ms + 3° release | 0.00 | 0 |

Without damping the meter changes band **36 times a second** — not a strobing meter, a strobe light.
The 150 ms dwell takes the mean to 0.45, which passes a naive "less than 1 per 30 s" reading of the
done-when, but the *worst* trial still strobes: four visible flips inside half a minute. Two degrees of
release removes it entirely — no change in any of the 300 runs.

That is the whole argument for shipping both: the time constant kills the high-frequency judder, and
the degree offset kills the low-frequency "sitting on the line" crawl that survives any time constant.

**A hysteresis that is not a time constant at all: the pause.** A tracking dropout produces garbage
rotations, and the matcher previously let those update the band. It no longer does — while confidence is
below the floor, the band is **frozen** and the pending change is discarded. A lost frame is not a
verdict (ADR-003: pause rather than penalise), and a band that jumps on a dropout is the cheapest strobe
of all, because no time constant can hide a one-frame garbage distance.

And the positive control — a hand approaching smoothly from 40° to 10° over 3 s, 300 trials:

| Damping | Changes (mean / max) | Reverse steps |
|---|---|---|
| 150 ms only | 1.52 / 2 | 0 |
| 150 ms + 2° release | 1.52 / 2 | **0** |

Two changes is the target: warming → close → locked, once each, in one direction. Not one trial went
backwards. The bands read as progress rather than as noise.

---

## 5. The upper edge (close) — 24°, and the honest reason it survives

The trap in this task is choosing round numbers because they look tidy. 24° is a round number, so it
had to earn its place, and the test it had to pass is the one that matters: **can a hand making the
*neighbouring sign* be told it is close?**

The nearest pair in the content is U–V at **26.0°** (`matcher-tuning.md` §8). So a learner holding V
while the target is U sits 26° from the target, and the close edge must not swallow them.

Rule applied: an edge must sit **≥ 2.33 sd** from the neighbouring sign (1% one-tailed).

| Noise multiplier | sd at D = 26° | Margin at close 24° | Margin at close 20° | Reading |
|---|---|---|---|---|
| **1.0× (model)** | **0.60°** | **3.3σ** | 9.9σ | **24° holds** |
| 1.5× | 0.91° | 2.2σ | 6.6σ | 24° fails → drop to 20° |
| 2.0× | 1.21° | 1.7σ | 5.0σ | 24° fails → drop to 20° |
| 3.0× | 1.81° | 1.1σ | 3.3σ | 24° fails → drop to 20° |

At the modelled noise, 24° clears the rule with a 3.3σ margin and would survive ~40% more noise than
the model predicts. **This is the one number in this document that is not yet verified on hardware**:
the real device noise figure is P1.8/P1.5 work, and if it turns out to be more than ~1.4× the model the
edge comes down to 20°, which holds to 3×. That is a one-field inspector change and the tripwire is
written down here so nobody has to re-derive it.

Also verified directly (300 × 30 s runs at the neighbour distance): the band displays **Close on 0.0% of
frames** at edge 24 — and at 3× noise a neighbouring hand is inside the raw edge on about 13% of
*single frames*, but never for the eleven consecutive frames the dwell timer requires. Two mechanisms,
each covering the other's weak case.

### Why "close" and the near-miss can fire at the same time, and why that is right

A hand at 24.5° against a U target is inside the close band and is also 1.5° from being V — so it will
be teal *and* the near-miss may speak ("that's V, not U"). That is not a contradiction, it is the two
layers doing their jobs: the band says **how near**, the near-miss says **near to what**. The colours
carry the gradient; the one line of copy carries the diagnosis. Neither is a verdict.

---

## 6. The visual mapping — why colour and motion, and never text

| | Warming | Close | Locked |
|---|---|---|---|
| **Colour** | amber, low saturation | teal, mid saturation | warm gold, high value, low saturation |
| **Motion** | breath, 0.25 Hz | pulse 0.8 Hz + ring contracting with `D` | one snap, then still |
| **What it says** | "something is happening, keep going" | "you are getting closer" — the *rate* of pulse carries the gradient | "you are here" |
| **What it never says** | anything about being wrong | | |

Three constraints shaped that table:

1. **Colour alone is not enough** — colour-blind users (about 8% of men) cannot separate amber from teal
   reliably, so every band has a **motion** signature as well. That is the accessibility requirement in
   `ACCESSIBILITY.md` made concrete, and it is why the three bands differ in *kind of motion* and not
   just in hue.
2. **No red, at any distance.** The worst band is amber, which is the colour of "warming up" rather than
   the colour of an alarm. Amber→teal→gold is a sunrise, not a traffic light.
3. **The gradient lives in the motion, not the colour.** In the close band the pulse amplitude and the
   ring radius both track `(D − lockedDeg) / (closeDeg − lockedDeg)`, so a learner feels themselves
   getting closer inside a band without any of the three bands having to be sub-divided. That is how a
   three-state design still gives continuous feedback.

Implementation: `Scripts/UI/BandFeedback.cs` — `MaterialPropertyBlock` (no material instances, no
per-frame allocations), colour blended over 0.14 s so a band change is a transition and not a cut, and
the ring driven off the matcher's live distance.

---

## 7. The sound mapping

Straight from `SOUND-MAP.md` §2, which had already specified these three rows. All band sounds are
**non-spatialised** (`spatialBlend = 0`) because they are about the learner's own hand, and a sound that
moves when you move cannot be counted. One pooled `AudioSource`, no per-event sources.

| Transition | Sound | Volume | Status |
|---|---|---|---|
| warming → close | soft rising two-note interval, 60 ms apart, 300 ms | 0.30 | **placeholder shipped** (`Audio/SFX/band_close.wav`, C5→G5) |
| close → locked | single short high tone, 120 ms | 0.34 | **placeholder shipped** (`band_lock_in.wav`, C6) |
| locked → warming | the same interval inverted, at half volume — or **silence** | 0.16 | **placeholder shipped** (`band_release.wav`) |

The placeholders are synthesised to the sound map's *description*, not its timbre: they are sine tones
and the real ones are struck wood (`SOUND-MAP.md` §3). They exist so the mapping is wired and audible
now, and P7 replaces the clips — which is an inspector change, not a code change, because
`BandAudio.cs` knows nothing about the files.

The third row is the one to be careful with. `SOUND-MAP.md` allows silence when a learner leaves a band,
and the design principle is that **losing a band is not a failure and gets no punishment sound.** The
shipped clip is the rising interval played backwards at half volume: it reads as the sound *un-happening*
rather than as a buzzer. If P7 cannot make that sound gentle, the correct answer is to delete the clip
and take the documented silence — the one thing that must never appear is a descending "you lost it".

There is no sound anywhere for "the shape did not lock", because **there is no such event.**

---

## 8. Step 5 — zero error strings, as a check rather than a promise

`python3 scripts/lint_copy.py` — 0 violations, 12 user-facing strings in 7 files, 2 dev views verified
gated.

What the audit found and what changed:

| Finding | Action |
|---|---|
| `HandConfidenceReadout` printed **`LOST`** for an untracked hand | reworded to **`not tracked`**, and the whole readout is now removed from release builds |
| `DebugOverlay` contains `no matcher`, `PAUSED`, raw numbers | dev view; now self-removes from a release build (`Scripts/Core/DevOnly.cs`, `Debug.isDebugBuild`) |
| `Matcher` inspector tooltip: "the hand is held **wrong**" | reworded — "the hand is not oriented to the shape" |
| `NearMissDisplay`: "That's not C. That's O." | **kept.** It is the product's signature line, ≤ 12 words, names a shape rather than a failure, and never fires without an unambiguous closer match |
| `HonestMirror`: "The cameras cannot see a finger that is behind another finger." | **kept.** An explanation of the hardware, not a judgement of the learner |
| Probe/trial tooling: `wrongShapeFrames`, `"append failed"`, `LOST` in CSVs | **kept and exempt** — measurement columns, not copy. Renaming them would make the research data harder to read for no product gain |

The linter is the part that matters. A rule that lives only in a document decays the first time someone
adds a label in a hurry; a banned-word list with a non-zero exit code does not. It also checks the dev
views for their release-build gate, so a future panel that says something verdict-shaped fails CI
unless it is provably gated.

Banned in user-facing copy: `wrong, incorrect, error, invalid, failed, failure, try again, sorry, oops,
nope, bad, denied, rejected, missed, not quite, too far, keep trying` and the symbols `✗ ✘ ❌ × ✕`.

---

## 9. Done when

- [x] **Three bands with tuned, recorded thresholds** — 12° / 24°, plus a 2° release, each with the
      measurement that chose it (§2–§5). Every candidate in the tables was evaluated; 24° survived and
      20° is the documented fallback if the device is noisier than modelled.
- [x] **No hysteresis flicker over a 30 s test — offline.** 0 changes in 300 × 30 s runs with the
      shipped damping, against 1083 changes/30 s undamped (§4). The device version of this test cannot
      run in this sandbox; the overlay now shows a live **`changes/30s`** counter so it is a five-second
      read rather than a judgement call.
- [x] **Correction applied** — §3's hold-survival table was re-measured in P2.4 after a simulation bug
      was found; the "effective edge is 10°" claim is withdrawn.
- [x] **Zero error strings in the build** — executable check (`scripts/lint_copy.py`), 0 violations, and
      the two dev views that do contain verdict-shaped words are gated out of release builds.

### The one test that needs the headset

1. Build, open the overlay, set the target to **B**.
2. Park a hand *just* inside the lock edge (~11–12°) and hold still for **30 s**. Watch `changes/30s`:
   it must stay at 0 or 1. Anything that climbs is a flicker this document got wrong.
3. Park a hand *just* outside the close edge (~25°) for 30 s. Same read. This is the edge the design
   picked, so it is the one to be suspicious of.
4. Hold the true shape and confirm the **band colours and motions** go amber → teal → gold, that locked
   is visibly *still*, and that the close pulse visibly speeds up as the hand arrives.
5. Listen to the three band transitions, in order, twice. Confirm the release sound does not read as a
   buzzer — if it does, delete the clip (§7) and take the silence.
6. Record the device noise figure from P1.8/P1.5 against the §5 tripwire: **above ~1.4× the modelled
   noise, lower `closeDeg` to 20.**

### Traps, and where each one was met

| Trap | How it was avoided |
|---|---|
| Picking round numbers because they are tidy | 12° and 24° were both run against the neighbouring-sign test and the noise rule before being kept; 20° is written down as the fallback, and the tripwire that would force it is a measured sigma threshold, not a feeling |
| Skipping hysteresis and shipping a strobing meter | 150 ms alone still strobed on the worst of 300 trials — the measurement is in §4, and 2° of release is what took it to zero |
