# Video Script
**Status:** ✅ v1 · **Owner:** W4 · **Phase:** P9 · **Gate:** G9 · **Hard limit:** under 3:00

> **Three minutes is the product as far as most judges are concerned.** This is the shot list with a
> hard timecode budget. Every rubric claim in the description has to be *visibly evidenced* here, or
> it comes out of the description.

---

## 1. The hard rules

| Rule | Why |
|---|---|
| **Under 3:00.** Aim for **2:45** so there is room for the frame to breathe. | The competition limit. A video over 3:00 is not a video, it is a rejection. |
| **Hands-first by 0:30.** Real hands, real gameplay, from the first second. | A hard test. |
| **The hook is in the first 3 seconds.** | Most judges decide in the first few seconds whether to keep watching. |
| **1920×1080.** | Judged on a laptop screen at a table. |
| **A 3-second frame-rate overlay** somewhere in the middle. | Proves the 72 fps claim without a word. |
| **Real-time capture. No fake UI. No speed ramps that hide frame rate.** | The competition disqualifies AI-generated footage, and a rigged demo is the one thing a technical judge will look for. |
| **Legible with the sound off.** | Most judges watch muted first. |
| **Do not use "immersive" more than once.** | It is the word every VR video reaches for, and it means nothing. |

## 2. The first four seconds

The near-miss, stated at the viewer. Not the logo, not the title card, not the room establishing
shot — **the product's one line that no 2D app can say**:

> **"That's not C. That's O — your thumb is 8° too far around the palm."**

Then a half-second of the room, and we are in.

## 3. The shot list

**Total: 2:45. Every row is a shot to capture, not a sentence to write.**

| # | Timecode | Shot | Source | What it proves | Dur |
|---|---|---|---|---|---|
| 1 | **0:00–0:06** | **Cold open.** The near-miss line on screen, over a close-up of a hand holding a slightly wrong shape. Cut on the chime. | Real-time device capture | Innovation · the hook · the one thing nobody else does | 0:06 |
| 2 | 0:06–0:14 | Your real room, passthrough on. Both hands fade in already mirroring. No logo, no splash. | Device | Experience Design · "the room is the level" · TTFI | 0:08 |
| 3 | **0:14–0:26** | **The discovery.** The reference hand appears ~40 cm away holding **B**. You pinch it and it turns **with your own wrist**. The camera stays still; the hand turns. | Device, hands-first | Innovation · "you cannot rotate a video" | 0:12 |
| 4 | 0:26–0:36 | **Split screen.** Left: a flat ASL video of the same handshape, turning is impossible. Right: our hand, rotating freely. Both hands in frame on the right. | Device + reference clip | Innovation · the medium is the idea | 0:10 |
| 5 | 0:36–0:52 | You shape your hand. The meter leans. **First lock at ~0:45**: the shape dissolves and becomes an object. | Device | Experience Design · "the first payoff" | 0:16 |
| 6 | 0:52–1:06 | The object arcs to the shelf on your real desk. The chime. The shelf has one object on it. | Device | Experience Design · reason to come back · MRUK | 0:14 |
| 7 | **1:06–1:22** | **The near-miss, properly.** Target is **U**, you make a **V**. The line fires, the copy lands. Then the reverse: target **V**, you make **U**. | Device | Innovation · the signature feature · **both directions, so it is not a one-trick shot** | 0:16 |
| 8 | 1:22–1:34 | **Gaze.** Look at a finger → it highlights, with its position named. No hands used. | Device (Quest) **and** the VR Glasses simulator profile | Technical · gaze · "both hands are busy, you cannot point" | 0:12 |
| 9 | **1:34–1:52** | **Passthrough comparison.** The same view with passthrough **on**, then **off**. The shelf loses its ground; the objects float. | Device, two clips | Technical · passthrough · *"the room is the level"* | 0:18 |
| 10 | 1:52–2:08 | The **C → O** pair, side by side. Nearly identical from the front. Obviously different in depth. | Device | Innovation · the depth argument | 0:16 |
| 11 | 2:08–2:22 | **The honest mirror.** The panel: the shapes today's cameras cannot see, greyed, with the reason. The number. *Copy and count line: `SUBMISSION-COPY.md` §7.2; the reasoning behind them: `TRACKABILITY.md` §2.4 and §4.* | Device | Innovation · "we publish our blind spots" | 0:14 |
| 12 | 2:22–2:34 | **FoV split-screen.** The same frame at Quest's ~110°×96° and at the VR Glasses' ~70°×66°. The shelf reflows to a tray in the narrow one. | Simulator + device | Technical · FoV-aware · VR Glasses | 0:12 |
| 13 | 2:34–2:42 | **The credits.** The signer's name and city, or the state-B line. *"This is practice software, not a replacement for a Deaf teacher."* | Screen capture | Innovation · integrity · the rule that never ships | 0:08 |
| 14 | 2:42–2:45 | End card: **SIGNSPACE**, the tagline, and the install link. | Graphic | — | 0:03 |

**Totals: 2:45.** Two shots (7 and 10) are the ones that carry the novelty. They are worth the
budget they get.

## 4. The frame-rate overlay

Placed over **shot 8** (the gaze shot), which is otherwise static.

- **3 seconds**, bottom right, small.
- Shows the live number: **`72 fps`**.
- **OVR Metrics Tool** is already installed (`metavr tools install ovrmetric`) — it puts the real
  counter on the device's screen, so the overlay is the device's own number and not a caption.
- If the number is not 72, **do not fake it**. Lower the claim in the description to match, and
  disclose. A technical judge will check.

## 5. What must be real-time, and what must not be staged

| Must be real-time | Must not be staged |
|---|---|
| Every shot of the user's own hands | The reference hand turning with the wrist — it must be one continuous take, not an edit |
| The near-miss firing | The order of the shots — the near-miss lands **before** the second shape is mastered, because that is the designed order |
| The lock | The room — the shelf must sit on real furniture, and the passthrough comparison must be the same camera position, twice |
| The fps counter | The **UI**. No composited panels, no fake meters. |

**If a shot does not work on the day, cut it and re-balance the timecode budget.** Never fake it.

## 6. The FoV split-screen

| | |
|---|---|
| **What it shows** | The same frame of the same scene, at two simulated fields of view |
| **How** | Meta XR Core SDK **v207** ships *FOV Simulation for Meta VR Glasses* in `OVRManager` and the Immersive Debugger, so the narrow view can be masked **on the device**. The Meta XR Simulator's VR Glasses profile does the same on the desktop. |
| **Which pair** | Quest ~110°×96° vs VR Glasses ~70°×66° |
| **What must be visible in the narrow one** | The reference hand, the match meter, the shelf as a **tray**. Nothing critical outside the safe cone. |
| **Why it matters** | Meta unveiled the glasses the day before the competition opened. FoV-aware design is a named criterion, and this pair is the evidence. |

## 7. Capture settings

| | |
|---|---|
| Resolution | 1920×1080 |
| Frame rate | 72 fps if the device holds it, otherwise 60 and the description says so |
| Recording | Device screen recording via `metavr capture` (Meta VR CLI), or the Quest's own capture |
| Audio | Record the real app audio. **No music bed** — the sound design *is* the audio. |
| Lighting | The conditions written down in [`../../phases/p0-foundation/devices.md`](../../phases/p0-foundation/devices.md) §3 |
| Hands | The right hand for the reference, both hands in frame for the tracking shot |
| Takes | **Every shot recorded three times.** Take the best of each. The third take is always better than the first. |

## 8. Edit rules

1. **No fake UI.** Every panel on screen came out of the build.
2. **No speed ramps.** A ramp that hides frame rate is a disqualification.
3. **Cut on motion.** Every cut happens during a hand movement.
4. **One line of text per shot, maximum.** The near-miss line is the only one that gets words.
5. **Captions on by default** — the video must be legible muted.
6. **No logo animation.** The end card is a still.
7. **No AI-generated footage, in any frame.** Not a background, not a texture, not a transition.

## 9. The thumbnail and the first frame

| | |
|---|---|
| The thumbnail | The user's own hand, mid-rotation of the reference hand, with the C/O near-miss line over it |
| The first frame | A hand, holding something slightly wrong. No title card. |
| The title | `SIGNSPACE — it tells you which sign you accidentally made` |

## 10. Shot → rubric map

Every criterion, and where it is visibly evidenced:

| Criterion | Shot |
|---|---|
| **Innovation & Creativity** | 1 (near-miss), 3 (rotation), 4 (flat vs rotatable), 7 (both near-miss directions), 10 (C vs O), 11 (honest mirror), 13 (credits) |
| **Experience Design** | 2 (no splash, immediate agency), 5 (first lock), 6 (the shelf and the first object), 14 (the end card) |
| **Technical Implementation** | 8 (gaze), 9 (passthrough + MRUK), 12 (FoV), and the fps overlay on shot 8 |
| **Polish & Presentation** | 1 (the hook), 5 (the payoff), 14 (the end card) |

**Every claim in the 494-word description must point at a shot.** If it does not, it comes out of the
description — not out of the video.

## 11. Before upload

- [ ] Under 3:00 (target 2:45) — check with `ffprobe`
- [ ] 1920×1080
- [ ] Hands-first by 0:30
- [ ] The hook lands in the first 3 seconds
- [ ] Plays in an incognito window
- [ ] Legible muted
- [ ] The fps overlay shows the real number
- [ ] No fake UI, no speed ramps, no AI-generated frames
- [ ] The credit state in the video matches the credit state in the app **and** in the description
- [ ] The install link at the end works
- [ ] Thumbnail and title set

> **The last check is the one people skip.** If the video shows a verified signer and the app says
> "review in progress", the entry is dishonest. `ADR-007` exists so this is a five-minute fix — not
> an argument at 1 a.m.
