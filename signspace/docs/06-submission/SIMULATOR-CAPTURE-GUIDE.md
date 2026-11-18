# Simulator capture guide

**Written:** 17 Nov 2026 · **For:** P9.3 · **Companion to:** `VIDEO-SCRIPT.md`, `phases/p9-submission/shot-list.csv`

> The competition accepts **XR Simulator footage** in the video. This guide maps every shot to the
> simulator profile that produces it, so the footage is captured correctly the first time.

---

## 1. Profiles

| Profile | FoV | Eye gaze | Hand tracking | Passthrough | Use for |
|---|---|---|---|---|---|
| **Quest 3** | ~110°×96° | no | yes (camera-driven) | synthetic environment | Shots 1–7, 9–11, 13, 14 (the core loop + shelf + credits) |
| **Quest 3S** | ~110°×96° | no | yes (camera-driven) | synthetic environment | Same as Quest 3 (weaker device, same FoV) |
| **VR Glasses** | ~70°×66° | **yes** | yes (camera-driven) | synthetic environment | Shots 8 (gaze) and 12 (FoV split-screen) |

**The default profile is Quest 3.** Switch profiles in the simulator's Inputs panel under
*Device info > Device*, then **stop and restart the app** — the profile is captured when the OpenXR
instance is created.

## 2. Capture settings

| | |
|---|---|
| Resolution | 1920×1080 |
| Frame rate | 72 fps if the simulator holds it, otherwise 60 |
| Recording | `metavr capture` (Meta VR CLI) or the simulator's session recorder |
| Audio | Record the real app audio. **No music bed.** |
| Takes | Every shot recorded three times. Take the best of each. |
| Hands | Camera-driven hand tracking (webcam). Enable it in the simulator's Inputs panel. |

## 3. Shot-to-profile map

| Shot | Timecode | Profile | Notes |
|---|---|---|---|
| 1 | 0:00–0:06 | Quest 3 | Near-miss line over a hand holding a slightly wrong shape |
| 2 | 0:06–0:14 | Quest 3 | Real room (synthetic env), passthrough on, both hands fade in |
| 3 | 0:14–0:26 | Quest 3 | Reference hand appears, pinch and turn with your own wrist |
| 4 | 0:26–0:36 | Quest 3 + reference clip | Split screen: flat ASL video vs rotatable hand |
| 5 | 0:36–0:52 | Quest 3 | First lock: shape dissolves, becomes an object |
| 6 | 0:52–1:06 | Quest 3 | Object arcs to the shelf. The chime. |
| 7 | 1:06–1:22 | Quest 3 | Near-miss both directions: U→V and V→U |
| 8 | 1:22–1:34 | **VR Glasses** | Gaze: look at a finger, it highlights with its position. Eyes-only, no hands |
| 9 | 1:34–1:52 | Quest 3 | Passthrough comparison: on, then off. Shelf loses its ground. |
| 10 | 1:52–2:08 | Quest 3 | C → O pair, side by side |
| 11 | 2:08–2:22 | Quest 3 | Honest mirror: greyed shapes, reasons, the number |
| 12 | 2:22–2:34 | **Both** | FoV split-screen: Quest vs VR Glasses, side by side |
| 13 | 2:34–2:42 | Quest 3 | Credits |
| 14 | 2:42–2:45 | — | End card (graphic, no capture) |

## 4. The FoV split-screen (shot 12)

1. Record the same scene in the Quest 3 profile → 1920×1080, 72 fps.
2. Switch to the VR Glasses profile → stop and restart the app → record again.
3. In the edit, place them side by side. The narrow one (VR Glasses) must show the shelf as a
   tray, with everything critical inside the ~55° cone.

**The VR Glasses profile has a ~70°×66° FoV** (37° temporal, 37° nasal, 34° up, 34° down). The
comfortable gaze cone is much narrower than the frame — dwell targets must sit inside it.

## 5. The gaze shot (shot 8)

1. Switch to the VR Glasses profile → stop and restart the app.
2. The simulator simulates eye gaze by default (no controllers).
3. Look at a finger of the reference hand → it highlights with its position.
4. Record the eye-still / head-only proof: move only the eyes → highlight appears. Move only the
   head, eyes fixed → highlight does not appear. Both, side by side, in the video.

## 6. The passthrough comparison (shot 9)

1. In the simulator, enable the synthetic environment (passthrough on).
2. Record the scene with passthrough on.
3. Disable the synthetic environment (passthrough off).
4. Record the same scene again, same camera position.
5. In the edit, crossfade between the two. The shelf must lose its ground and float.

**Note:** the simulator does not composite a real camera feed. The synthetic environment is a
rendered room, not the user's actual room. The description must say this if it claims passthrough.

## 7. The frame-rate overlay

Placed over shot 8 (the gaze shot), which is otherwise static.

- **3 seconds**, bottom right, small.
- Shows the live number from the OVR Metrics overlay.
- The simulator reports its own frame rate, which is the PC's frame rate, not the headset's. **Do
  not claim this is the Quest 3S number** — it is the simulator's number, and the description must
  say so.

## 8. What to disclose

The description already has the anti-claim paragraph. Add one more:

> *"Testing note: this build was developed and verified on the Meta XR Simulator (Quest 3 and VR
> Glasses profiles) rather than on physical hardware. The core loop — hand tracking, the matcher,
> the near-miss, the reference hand rotation — runs in the simulator. Passthrough, MRUK room
> scanning, thermal behaviour, and frame rate on Quest silicon were not measured on device."*

## 9. Change log

| Date | Change | By |
|---|---|---|
| 17 Nov 2026 | File created. Shot-to-profile map, capture settings, disclosure. | agent |
