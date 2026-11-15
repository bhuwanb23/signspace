# Gaze Input
**Status:** ✅ v1 · **Owner:** W2 · **Phase:** P6 · **Gate:** G6

> **The uncomfortable fact, stated first because it changes the plan.** Neither the Quest 3 nor the
> Quest 3S has eye-tracking hardware. Gaze is a *named* Technical Implementation criterion and the
> judges' hardware cannot deliver it. So "gaze" in this product means three different things, and
> the build must be honest about which one is running at any moment.

---

## 1. The three kinds of "gaze"

| | What it is | Where it runs | What we can claim |
|---|---|---|---|
| **1. Eye gaze** | the user's actual eyes | **Meta VR Glasses** (2027) and in the **Meta XR Simulator's VR Glasses profile**, which simulates eye gaze by default | "gaze-driven highlight, demonstrated on the VR Glasses profile" |
| **2. Head gaze** | the direction the head is pointing | Quest 3 / 3S, today | "the highlight follows where you are looking" — which is what a Quest user experiences anyway |
| **3. Dwell** | holding the gaze on a target for a fixed time | every device, as the **eyes-only path** | "every action is reachable without a pinch" |

**The rule:** the product never claims eye tracking on hardware that has none, and the build never
silently substitutes one for another. `P6.2` exists precisely because a gaze feature that quietly
falls back to head gaze is worse than no gaze feature at all.

## 2. What gaze is for here

**Beat 3 of the concept:** *gaze isolates the detail.* Both hands are busy — one is holding and
turning the reference hand, the other is shaping. **You cannot point at a finger.** Looking at it is
the only way to say *"that one"*.

So gaze is not a menu system. It is a **highlight layer**: you look at a finger of the reference
hand and it highlights and names its position ("index, proximal").

This matters for the rubric: *"gaze interaction is named in the criteria"* is satisfied by a
highlight layer that is genuinely gaze-driven, not by a gaze-driven settings menu.

## 3. The implementation

### 3.1 The building block

**ISDK Eye Gaze Interaction building block**, v207, from the Interaction SDK.

| | |
|---|---|
| Where it lives | `com.meta.xr.sdk.interaction` (pulled in by `com.meta.xr.sdk.all` 207.0.0) |
| How it is added | the building block's `EyeGazeInteractor` on the finger-highlight layer, added to a world-space canvas or to the ray-interactable highlights |
| Rig switching | **the building block switches rig by device**: eye gaze on the VR Glasses, head gaze on Quest. It is not our job to switch — and if we do it ourselves, we will get it wrong on one device. |
| What we add | the **highlight visuals** and the **position label**. The block does the picking. |

**We do not hand-roll gaze.** `TECH-STACK.md` §3 already says this: hand-roll only what ISDK does not
cover. Gaze picking with correct device rig switching is exactly what the block covers, and it is
one of the two things the SDK v207 added *for* the VR Glasses.

### 3.2 What we build

| Object | What it is |
|---|---|
| `FingerHighlight` | a prefab: an unlit highlight shell that fits one finger bone, plus a world-space label that names the joint |
| `GazeHighlighter` | the component that asks the gaze interactor what it is pointing at, and drives the highlight's on/off state and its label |
| `DwellSelector` | the eyes-only path: a dwell ring that fills over a fixed time and confirms the selection |

**Highlight only. Never a gaze-only menu.** `BATTLE-CARD.md` is explicit: gaze selects, pinch
confirms. Gaze-only menus are the most common XR accessibility failure.

## 4. Proving gaze is really active

`P6.2` — *"Verify gaze is really active — never silently falling back to raycast."*

The test is simple and it is a **binary pass/fail with a recording**:

| Step | On the VR Glasses profile (or the simulator's VR Glasses profile) |
|---|---|
| 1 | Put the highlight target in the **periphery** of the view |
| 2 | **Move only the eyes** toward it. The head stays completely still. |
| 3 | The highlight must appear |
| 4 | Repeat with **only the head** moving, eyes fixed straight ahead. The highlight must **not** appear |

**Step 3 passing and step 4 failing proves eye tracking.** Record both, side by side, in the
submission video (shot 8 of `VIDEO-SCRIPT.md`).

**On Quest 3 / 3S, the honest claim is head gaze.** The same test, but the video says *"on the VR
Glasses profile"* and the description says the same. Never both.

**A runtime log line, every frame, in the debug overlay:**

```
gaze mode: eye | head | none
```

If it ever says `head` when the profile says eye, that is a bug and the overlay should say so in
red — the one place red is allowed, because it is the build talking to us and not the user.

## 5. The eyes-only path

**`P6.3`: a full loop completable with eyes and one hand still.** Every action must be reachable:

| Action | Without a pinch |
|---|---|
| Select a shape from the shelf | dwell on it |
| Inspect a finger | gaze highlight (this *is* the interaction) |
| Confirm the lock | dwell on the reference hand |
| Read the near-miss | it appears; it is not dismissible |
| Open the honest mirror | dwell |
| Read the credits | dwell |

**Dwell tuning.** The defaults in `GAZE-INPUT.md` are placeholders until measured:

| Setting | Starting value | Notes |
|---|---|---|
| Dwell time | **1.2 s** | long enough to not fire on a glance, short enough not to feel slow |
| Dwell radius | **2.5°** | smaller than the target, so the finger has to be actually on it |
| Cancel | **move away, or look at nothing for 0.3 s** | a cancel must never need a pinch |
| Re-trigger | **must be able to re-trigger the same target** | without this the dwell is one-shot and the path is unusable |

The **VR Glasses profile is ~70°×66°**, and the *asymmetry* of that FoV (37° temporal, 37° nasal,
34° up, 34° down) means the comfortable gaze cone is much narrower than the frame. Dwell targets
must sit inside the ~55° cone, not inside the frame.

## 6. The fallback chain

| Condition | What happens | What we say |
|---|---|---|
| Eye tracking active | eye gaze | nothing, it just works |
| Eye tracking unavailable (Quest 3/3S) | head gaze, with **larger** dwell targets | one line, once: *"Eye tracking isn't available on this headset — highlighting follows your head."* |
| User declines eye tracking in the OS | head gaze, same as above | the same line |
| No eyes and no head (impossible on a headset) | pointer | — |

**The one line is honest and it appears once.** It is not an error state, so it is worded as
information, not as an apology. `UX-STATES.md` calls this state `NoEyes`.

## 7. Known limits — written down, not discovered in a review

| Limit | Consequence | What we do |
|---|---|---|
| **15 mm finger separation** at arm's length | two adjacent fingers may be hard to hit individually with a dwell | the highlight supports **finger groups** as well as single fingers, if P6.1 shows single fingers are unreliable |
| **Arm's length** is the worst case for precision | the highlight is less precise the further away | the reference hand sits at ~0.40 m, inside the airplane-seat shell |
| **No eye tracking on the judging hardware** | the eye-gaze claim can only be evidenced on the simulator profile | the description says exactly that |
| **Narrow FoV** on the glasses | peripherals are less reachable | the highlight layer is always inside the ~55° cone |
| **Dwell fatigue** | long dwells are tiring | dwell is only for the accessibility path, not the default |

## 8. Inputs
`TECH-STACK.md` §3 · `UX-STATES.md` (`NoEyes`) · `COMPETITION-FIT.md` (Technical Implementation) ·
`FIRST-FIVE-MINUTES.md` (the 4:00 gaze inspection beat) · the Meta XR Simulator VR Glasses profile

## 9. Output / done when
- [x] which building block, and which rig it picks on which device
- [x] how eye tracking is proven active rather than silently substituted
- [x] the eyes-only path, with every action reachable without a pinch
- [x] the fallback chain and the one line that goes with it
- [x] the known limits, written down
- [ ] **the on-device proof is recorded** — eyes-only, eyes-still / head-only, side by side
- [ ] **the dwell times are measured, not assumed**
- [ ] **the fallback line is tested on a Quest 3S**
