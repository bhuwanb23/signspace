# The visible pause *(P3.5)*

**Artefact:** `unity/Assets/_Project/Scripts/UI/TrackingConfidence.cs` · **Evidence:** `scripts/pause_sim.py`
**Rule:** ADR-003 — *pause rather than penalise*. Below the confidence floor the app stops measuring and
says nothing.

## What a learner sees

A small lamp beside the reference hand dims and breathes slowly (0.55 Hz) while the tracker is unsure.
The band's halo dims with it. No words, no number, no red, no cross. When confidence returns it clears.

Two asymmetries are deliberate:

| | |
|---|---|
| **Enters on the first untrustworthy frame** | being honest a frame late is being dishonest |
| **Leaves only after 0.25 s above floor + 0.05** | a hand hovering at the floor would otherwise strobe the room, which reads as a broken app |
| **Never dims to black** | a dark screen is indistinguishable from a dead app |
| **Never changes colour** | the band owns colour; if the lamp changed hue too, "teal" would stop meaning one thing |

## Where each part of the behaviour lives

Review caught a real defect in the first version of this task, worth recording because it is the
archetype of "looks implemented":

> **The match display could not be dimmed by the readout.** `BandFeedback` writes the halo's colour
> through a `MaterialPropertyBlock` **every frame**. `TrackingConfidence` was setting the material's
> `_BaseColor`, so its dim was overwritten on the next Update — implemented, compiled, invisible. The
> dim now lives in `BandFeedback` itself (`pauseDim`), which is the component that owns the block.
>
> **And the first dim would not have dimmed.** It scaled **alpha**, and an unlit material is opaque:
> alpha on an opaque surface changes nothing on screen. Both the lamp and the halo now dim by scaling
> **RGB**, which is what a renderer actually shows.

So the behaviour is split by ownership, not by topic:

| Part | Lives in | Why there |
|---|---|---|
| the freeze | `Matcher` + `HoldTimer` | the rule; nothing visual may gate it |
| a still, dimmed meter | `BandFeedback.pauseDim` | it owns the property block that paints the halo |
| the lamp | `TrackingConfidence` | brightness and breath only; no state a learner must read |
| the hysteresis | `TrackingConfidence` | only the display needs entering fast and leaving slow |

While paused the meter **stops breathing** (`_clock` does not advance) and holds its scale at 1: a
breathing meter is a measuring meter, and the whole point is that the app is visibly *not* measuring.
Colour alone is left, dimmed 40% — the band still says where you were, it no longer says "close".

## What the app does underneath

`TrackingConfidence` owns **no timing that matters**. The freeze is the matcher's:

- `Matcher.Tick` computes `trustworthy = confidence >= confidenceFloor` (0.70) and calls `SetPaused`;
- `HoldTimer.Tick` receives `trustworthy` and, while it is false, advances nothing — frozen inside the
  200 ms grace, then bleeding at 0.5/s, never locking;
- the band display is frozen in the same frame, because a distance computed from a lost hand is garbage
  and a band that follows garbage is garbage.

Delete `TrackingConfidence` and the pause still happens. That is the property that makes it safe to ship
next to a deadline.

## Evidence — `scripts/pause_sim.py`

`HoldTimer.cs` cannot be compiled here, so the script replays `scripts/hold_sim.py` (a line-for-line
mirror of the shipped state machine) over the three degradations the task names, with the hand parked at
**8.0°** — inside the 12° lock band the whole time. That is adversarial: the distance is perfect, so the
only thing between the hand and a lock is whether the timer is allowed to count.

| Scenario | Locks | Locks on a frame below the floor | Same, floor removed (control) | Hold resumed |
|---|---|---|---|---|
| Hand occluded behind the other, 0.5 s | 1 | **0** | **1** | yes |
| Fast motion, joint detail in patches | 1 | **0** | **1** | yes |
| Low light, 1.8 s sustained | 1 | **0** | **1** | yes |

The control is the point of the control: with the floor removed, **each of the three would have awarded
a lock while the tracker was blind** — the app would have celebrated a shape it could not see. With the
floor, the same frames produce nothing, and the lock arrives only once the hand is readable again. A
pause, not a penalty.

Strobing, 4 s of confidence wandering either side of the floor:

| | Visible state changes |
|---|---|
| No exit window | 20 |
| 0.25 s window, +0.05 margin | **1** |

## Run sheet — three scenarios, on a headset (step 4)

Ten minutes, one person, and the only part of this task that cannot be checked here.

| # | Scenario | How | What must be true |
|---|---|---|---|
| 1 | **Occluded** | hold the target shape, then pass the left hand in front of the right for about half a second | hold freezes rather than resetting; lamp dims and breathes; the meter stops breathing; no lock fires while the hand is hidden; the hold completes afterwards |
| 2 | **Fast motion** | hold the shape and sweep the arm across the room, then settle | nothing locks during the sweep; when the hand settles, the hold resumes from where it was, not from zero |
| 3 | **Low light** | turn the room lights down until tracking degrades | the app sits in the waiting state without looking broken, without a number, without red; bringing the lights up clears it within a quarter second |

If any of the three locks during the blind window, the response is **not** to tune the display: it means
the matcher is not receiving `trustworthy`, and that is a P3.2-class defect. Disable, then fix.

## Owed

- **All three scenarios on hardware** (occlude behind the other hand, move fast, drop the lights) —
  simulated here, unmeasured on a Quest 3S. Fold into P3.6.
- The lamp's placement (0.20, 1.42, 0.62) is a build-time guess; it has never been seen in a headset.
- `TrackingLamp.mat` is created by the builder on first run (`Editor/BuildMaterials.cs`), so the scene
  never ships a default-grey lamp — but its colour has never been seen in a headset, and 0.55/0.68/0.78
  was chosen on a monitor.
