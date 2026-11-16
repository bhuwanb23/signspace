# The lock payoff — the shape becomes an object

**Phase:** P2 · **Task:** P2.5 · **Gate:** G2 · **Status:** built and timed; the on-device capture is
the one thing that cannot be done here
**Timing check:** `python3 scripts/payoff_previs.py` → `phases/p2-core-loop/payoff-timing.txt`
**Storyboard:** `phases/p2-core-loop/lock-payoff-storyboard.png` ·
**Previs:** `assets/raw/video/lock-payoff-previs.mp4` (**a diagram — not footage**)

> The first lock is the product's first reward, and it happens inside 75 seconds. If it doesn't feel
> like an event, the whole loop is flat. If it feels like a cutscene, the third repetition skips it and
> the loop is flatter. Those two constraints are 1.2 s apart, and this document is about living inside
> that gap.

---

## 1. The timeline

| Phase | From | To | Length |
|---|---|---|---|
| The reference hand dissolves — scale, fade, **and brightens** | 0.00 | 0.40 | 0.40 s |
| The object starts forming **inside the hand** | 0.30 | 0.52 | 0.22 s |
| The object arcs to the anchor | 0.30 | 0.75 | 0.45 s |
| Overshoot and settle | 0.75 | 0.87 | 0.12 s |
| The reference hand returns for the next attempt | 0.87 | 0.99 | 0.12 s |
| **Total** | | | **0.99 s** |

**0.99 s of a 1.20 s budget — 82% used, 0.21 s of headroom.** The number is not a hope: it is computed
twice, once by `LockPayoff.TotalSeconds` (which `SIGNSPACE/P2.5 Check payoff timing` reads) and once by
`scripts/payoff_previs.py`, which fails if either the budget or the overlap check breaks.

The timeline is a **pure function of one clock**, not a chain of nested coroutines. That matters for two
reasons: a dropped frame cannot stretch the payoff (the sequence ends when the clock says so), and the
duration is available *before* the animation runs, so the budget can be checked in the editor and in CI
instead of being discovered on the headset.

---

## 2. Transformation, not deletion

The done-when asks that the dissolution read as transformation. Three separate things do that work, and
all three are timing decisions rather than effects:

1. **The hand brightens as it shrinks.** `_EmissionColor` ramps 0 → 1.6 while scale falls 1.0 → 0.12. A
   thing that gets brighter as it gets smaller reads as concentrating; a thing that only fades reads as
   being switched off. A brief 12° turn on the same curve makes it *move* rather than *dim*.
2. **The object starts forming 0.10 s before the hand is gone** (`objectFormStart 0.30` vs
   `dissolveSeconds 0.40`). There is therefore **no frame in the whole sequence with nothing on screen** —
   and that gap, invisible in stills, is exactly what makes an audience read a cut. This is checked as a
   hard assertion in the timing script, not left to taste.
3. **The object crossfades in through the dissolving hand, then lands with a settle.** It reaches ~90%
   opacity while the hand is still faintly there, so the same matter appears to re-arrange. At the anchor
   it overshoots by 6% and returns over 0.12 s, which is what an object that has been *placed* does.

The growth curve was changed during this task. A back-ease was tried first and rejected on inspection:
it reaches **77% of full size in the first fifth of the formation**, which reads as a pop rather than as
something assembling. The shipped curve (`1-(1-t)³ + 0.08·sin(πt)`) decelerates smoothly with a slight
bulge, and the overshoot was moved to where it belongs — the settle.

---

## 3. The trap: it must not become a cutscene

The task names it exactly: *making it longer because it looks nice — it will be skipped by the third
repetition.* Two defences, one structural and one behavioural:

- **Structural**: the budget is 1.2 s and the payoff is 0.99 s. The 0.21 s of headroom exists so that a
  heavier object mesh or a slow frame does not silently push it over.
- **Behavioural**: after **3** locks the whole sequence is compressed to **×0.55 → 0.54 s**, keeping its
  proportions. The reward is not removed — removing it would say the achievement stopped mattering — it
  is made quick, which is what a person wants from a thing they have already seen three times.

The counter is `_repetitions` on the component; a future phase may want it per-shape (the fourth lock of
a *new* shape is a first, and should feel like one). That is noted in §8 as a deliberate open question
rather than silently implemented.

---

## 4. The sound and the "haptic"

**The chime** is the signature sound from `SOUND-MAP.md` §3: a perfect fifth (C–G) with a soft octave
above, struck in sequence 120 ms apart, 1.6 s with a natural decay, wooden partials that die faster than
the fundamental, and a three-tap small-room tail (~11/19/29 ms) rather than a cathedral.

It ships as `Audio/SFX/lock_chime.wav` — **synthesised, so it is correct in structure and wrong in
timbre**: the real one is struck wood and this is additive sine. P7 replaces the file; nothing in the
code changes, because the clip is an inspector reference. It starts 50 ms after the lock, so it lands
with the *object forming* rather than with the input event, and its 1.6 s tail deliberately outlives the
0.99 s visual — the reward is allowed to ring.

**The "haptic" is a lie told honestly.** Controller-free hand tracking has no actuator, so there is
nothing to buzz. What exists is the one channel the tracked hand owns — its own appearance — so
`HandPulse` fires **three 60 ms pulses 90 ms apart at falling amplitude** in the hand that did the thing.
That rhythm is close to what a haptic tick would be and it is localised to the hand that earned it, which
is the property that makes feedback read as feedback rather than as decoration.

---

## 5. What is wired, and where

| Piece | File |
|---|---|
| The sequence, timing, fades, arc, settle, repetition scaling | `Scripts/Core/LockPayoff.cs` |
| The controller-free pulse | `Scripts/UI/HandPulse.cs` |
| The committed prefab (slots + defaults, timings editable without recompiling) | `Prefabs/LockPayoff.prefab` |
| The regeneration path, if the prefab ever misbinds | `SIGNSPACE/P2.5 Build lock payoff prefab` |
| The budget check | `SIGNSPACE/P2.5 Check payoff timing` |
| Scene wiring (matcher, tracked hand, save, anchor) | `Editor/SceneBuilder.cs` |
| The chime | `Audio/SFX/lock_chime.wav` (placeholder; P7 replaces) |

The prefab holds the numbers; the scene wires only what it alone can know. That is why the timing can be
tuned without touching `SceneBuilder`, and why `Check payoff timing` can audit a prefab nobody has opened.

**Temporary anchor.** The shelf arrives in P4, so the object settles on `RestAnchor` and the component
keeps at most 3 settled objects (`maxObjectsOnAnchor`) — a small pile, not a museum. When P4 lands, the
anchor is replaced by the shelf's next free slot and the pile logic is deleted.

---

## 6. The shot (this is the 0:45 shot in the submission video)

**Not yet captured.** There is no Unity in this environment, so no footage of the app exists at all; what
exists is a previs at `assets/raw/video/lock-payoff-previs.mp4`, which is a **diagram of the timeline and
must not appear in the submission video**. The real file is `lock-payoff-take01.mp4`, and it is shot like
this:

1. Quest capture at **1080p/60** (the sequence is under a second; 30 fps would throw away half of it).
2. Frame the hand **from the start**, with the anchor visible in the corner of the shot — the arc is the
   part that reads as "it went somewhere".
3. Make **B**, hold until the object has settled, **then hold one more second** of tail so the edit has
   somewhere to cut.
4. Do it **four times**, because the fourth repetition is the compressed version (0.54 s) and the edit may
   want the quick one — and because the first three tries will have a hand that is not quite the shape.
5. Same camera height and same hand position for every take, so the 0:45 cut matches the surrounding shots.
6. Keep the **chime** in the capture. If the take is captured with system audio muted, the audio must be
   laid back in from `Audio/SFX/lock_chime.wav` — and then it is no longer evidence that the sound played.

---

## 7. Done when

- [x] **Lock payoff completes in < 1.2 s** — 0.99 s of a 1.20 s budget, computed by both the component
      and the timing script, with the overlap and the budget as assertions rather than opinions.
- [x] **The dissolution reads as transformation, not deletion** — brighten-while-shrinking, a 0.10 s
      overlap in which the object is already forming, a crossfade through the hand, and a settle at the
      anchor (§2). Verified frame-by-frame in the previs at 60 fps, which is the strongest check available
      without hardware.
- [ ] **Footage captured for the video** — **blocked**: no Unity, no headset, nothing in this repository
      has ever run. Shot list in §6; the previs is explicitly not a substitute.

---

## 8. Open questions left deliberately open

- **Per-shape repetition counting.** Today the compression counts locks, not locks *of this shape*. The
  fourth lock of a new shape is a first, and arguably should feel like one.
- **The pile.** Three objects on a temporary anchor is a placeholder for P4's shelf; the pop when the
  oldest disappears will be invisible once the shelf has slots, and must not be tuned now.
- **The chime's timbre.** Structure is right, wood is wrong. This is P7's first job, and the only reason
  it is not a defect in this task is that the mapping — which sound, at which moment, at which volume —
  is the part P2.5 owed.
