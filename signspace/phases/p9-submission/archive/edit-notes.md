# Edit notes

**Written:** 19 Nov 2026 · **Phase:** P9 · **Task:** P9.8 · **Gate:** G9
**Status: ⬜ NOT EDITED.** There is no cut. This file records the decisions the edit must follow, and the
one asset that exists, so nobody later mistakes a plan for a finished video.

---

## 1. Footage inventory, as it actually is

| Asset | What it is | Usable as submission footage? |
|---|---|---|
| `assets/raw/video/lock-payoff-previs.mp4` | previsualisation of the lock payoff | **No.** It is a previs: not device capture, no real hands, and `VIDEO-SCRIPT.md` §5 forbids it. |
| — | device capture of any of the 14 shots | **None exists.** Requires the headset, the room, and `metavr capture`. |

So the edit has not started, and no timecode in `shot-list.csv` is marked `captured = 1`. That column is
the honest one: it is a claim about footage, and filling it without footage is the one thing this archive
exists to prevent.

## 2. Decisions already made (do not re-litigate them at the timeline)

1. **2:45, not 3:00.** The limit is 3:00 and a rejected video is worth less than a 15-second margin.
2. **The near-miss is shot 1 and shot 7.** It is the product's only line no 2D app can say. The cold open
   states it, and shot 7 proves it in both directions (U→V and V→U) so it cannot read as one lucky take.
3. **Shot 3 is one continuous take.** The reference hand turning with the user's wrist is the argument
   against video; an edit there destroys the argument.
4. **Shot 8 carries the fps overlay** because it is otherwise static. The number is the device's own
   (OVR Metrics Tool), never a caption.
5. **Shots 9 and 12 are A/B pairs from one camera position.** A moved camera turns a comparison into two
   unrelated shots.
6. **No music bed.** The app's own audio is the sound design; the chime lands the lock.
7. **Captions on by default.** Most judges watch muted first.

## 3. What gets cut, in this order, if the budget slips

| Order | Cut | Why this one first |
|---|---|---|
| 1 | Shot 4 (the video split-screen) | Shot 3 already makes the "you cannot rotate a video" point with real hands. |
| 2 | Shot 10 (C→O depth pair) | The depth argument is also made in shot 7's near-miss copy. |
| 3 | Shot 12 (FoV split-screen) | Strong for the rubric, but it is the only shot that needs a second render profile. |

**Never cut:** shots 1, 3, 5, 7, 11. Hook, rotation, first lock, near-miss, honest mirror. Remove any one of
those and the description above it becomes a claim with nothing under it.

## 4. Rules the edit must not break

- **No fake UI.** Every panel came out of the build.
- **No speed ramps.** A ramp that hides frame rate is a disqualification risk.
- **No AI-generated footage, in any frame.** Not a background, not a transition.
- **One line of text per shot, maximum.**
- **If a shot does not work on the day: cut it and re-balance.** Never fake it.

## 5. When the cut exists

Append here, do not rewrite: the date, the runtime, which shots were captured, which were cut, and the
frame rate the device actually held. `shot-list.csv` gets its `captured` column filled from the timeline,
not from memory.
