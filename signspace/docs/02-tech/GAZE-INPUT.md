# Gaze Input
**Status:** ✅ built · **Owner:** W2 · **Phase:** P6 · **Gate:** G6

> **The uncomfortable fact, stated first because it changes the plan.** Neither the Quest 3 nor the
> Quest 3S has eye-tracking hardware. Gaze is a *named* Technical Implementation criterion and the
> judges' hardware cannot deliver it. So "gaze" in this product means three different things, and
> the build must be honest about which one is running at any moment.
>
> **This document describes the build in `Scripts/Gaze/`, not the plan it started from.** The plan
> said "use the ISDK Eye Gaze Interaction building block". The build does not, and §3.1 says why.

---

## 1. The three kinds of "gaze"

| | What it is | Where it runs | What we can claim |
|---|---|---|---|
| **1. Eye gaze** | the user's actual eyes | a headset with eye tracking **and** an eye pose bound to `EyeTrackingProbe.eyeAnchor` — neither exists on Quest 3 / 3S | "gaze-driven highlight" — only with a recording from that hardware |
| **2. Head gaze** | the direction the head is pointing | Quest 3 / 3S, today | "the highlight follows where you are looking" — which is what a Quest user experiences anyway |
| **3. Dwell** | holding the gaze on a target for a fixed time | every device, as the **eyes-only path** | "every control is reachable without a pinch" |

**The rule:** the product never claims eye tracking on hardware that has none, and the build never
silently substitutes one for another. `P6.2` exists precisely because a gaze feature that quietly
falls back to head gaze is worse than no gaze feature at all.

## 2. What gaze is for here

**Beat 3 of the concept:** *gaze isolates the detail.* Both hands are busy — one is holding and
turning the reference hand, the other is shaping. **You cannot point at a finger.** Looking at it is
the only way to say *"that one"*.

So gaze is not a menu system. It is a **highlight layer**: you look at a finger of the reference
hand and it highlights and names it ("index", "middle", …).

This matters for the rubric: *"gaze interaction is named in the criteria"* is satisfied by a
highlight layer that is genuinely gaze-driven, not by a gaze-driven settings menu.

## 3. The implementation

### 3.1 Why it is not the ISDK building block

The plan was ISDK's `EyeGazeInteractor`. Two facts killed it:

1. **It needs an eye pose to exist.** On Quest 3 / 3S there is none, so the block would fall back —
   correctly — and the product would look like it had gaze while running on head direction. That is
   the exact failure P6.2 is about, and it would be hidden inside a package.
2. **The picking target is a fingertip 15 mm from its neighbour.** The block's ray interactor is
   built for UI canvases and grab targets, not for five sub-centimetre colliders on a hand.

So the picking is ~120 lines we own: one ray, a layer mask, a nearest-hit test, and an explicit
`GazeMode`. The cost is small and the honesty is structural. If the block is adopted later,
`GazeHighlighter` is the single seam to replace — nothing else in the product knows how gaze is
obtained.

### 3.2 What is built

| File | What it is |
|---|---|
| `Scripts/Gaze/EyeTrackingProbe.cs` | asks the platform, by name, once at start and on demand. `GazeMode` is `Eye`, `Head` or `None`; anything short of a definite yes is `Head` with a stated reason |
| `Scripts/Gaze/GazeHighlighter.cs` | one non-allocating ray from the eye anchor or the head; publishes the focused finger, its label, and the fallback explanation |
| `Scripts/Gaze/FingerTarget.cs` | marks a gazeable joint: finger index, label, optional coarse group |
| `Scripts/Gaze/FingerTargetsBuilder.cs` | builds an 11 mm trigger sphere on each fingertip of the reference hand at runtime, on the joints the pose is applied to |
| `Scripts/Gaze/DwellSelector.cs` | the dwell timer: 0.7 s base, ×1.5 on head gaze, ×1.5 at relaxed pace; fires once per acquisition and re-arms when the gaze leaves |
| `Scripts/Gaze/DwellAction.cs` | what a target *does* when dwelt on: select a shape, open the mirror or credits, flip a switch, start calibration |
| `Scripts/Gaze/EyesOnlyRouter.cs` | maps a completed dwell to the target's action, only while eyes-only is on |
| `Scripts/Gaze/GazeCaption.cs` | the in-world label: finger name, dwell ring, one-line fallback explanation |

**Highlight only. Never a gaze-only menu.** Gaze selects; pinch still confirms everything, and every
gaze target is also a pinch target.

**Groups.** Each fingertip also carries a coarse group (thumb / index / middle / ring+little). If the
distance test shows single fingers are not resolvable, `useGroups` on the highlighter switches to
them with one tick — no rebuild. That test is NOT RUN; the default is single fingers.

## 4. Proving gaze is really active

`P6.2` — *"Prove eye tracking is actually active — not silently head-raycasting."*

**What the build does about it, today:**

| Mechanism | Where | What it proves |
|---|---|---|
| A mode resolved by name, never guessed | `EyeTrackingProbe.TryPlatformEyeTracking` — reads `OVRPlugin.eyeTrackingEnabled` / `IsEyeTrackingEnabled()` via reflection | absence of the SDK or a `false` flag can never be read as "eye" |
| A status line | `EyeTrackingProbe.StatusLine`, e.g. `gaze: head (no eye-tracking SDK)` | the debug overlay and a screenshot both show the source |
| A one-line explanation to the user | `GazeCaption` shows `Explanation` for a few seconds on first resolve | the fallback is stated, not silent |
| **Eye mode without an eye pose is refused** | `GazeHighlighter.BindProbe` | if the platform says "eye" but no eye anchor is bound, the highlight stays on head and says so. This is the exact silent-lie case, closed in code |
| Forced modes for testing | `EyeTrackingProbe.forceMode` | the check can drive Eye/Head/None without hardware |

**What is still owed (device only, NOT RUN):**

| Step | Expected |
|---|---|
| 1 | Put a fingertip in the periphery |
| 2 | Move only the eyes. Head still. | highlight must move |
| 3 | Fix the eyes on one fingertip, move the head | highlight must **not** follow the head |
| 4 | Record both | `assets/raw/video/gaze-proof.mp4` |

On Quest 3 / 3S, steps 2–3 are **not performable**: the honest claim is head gaze, and the build says
so at runtime. That is a limitation of the hardware, not of the claim.

## 5. The eyes-only path

`P6.3`. Dwell becomes the primary confirm for every **control**:

| Action | Without a pinch | Built |
|---|---|---|
| Select a shape from the shelf | dwell on its silhouette | ✅ `DwellActionKind.SelectShape` |
| Inspect a finger | gaze highlight (this *is* the interaction) | ✅ |
| Open / close the honest mirror | dwell on its plate | ✅ |
| Open / close the credits | dwell on the bead's plate | ✅ (panel actions wired; bead gets a `DwellAction` when placed) |
| Flip any accessibility switch | dwell on its block | ✅ |
| Start / skip calibration | dwell on the calibrate block | ✅ |
| **Confirm the lock** | — | ❌ **not by gaze.** The lock IS the handshape; see below |

**The gap, stated rather than hidden.** A gaze-only unlock would bank progress the user's hand never
made, and the product teaches a handshape. So eyes-only completes every control around the loop and
the shaping step stays physical. `ACCESSIBILITY.md` §5 and §7 say the same thing; the recording row is
NOT RUN because the shaping step makes "hands in your lap, end to end" false as written.

**Dwell tuning, as built:**

| Setting | Value | Note |
|---|---|---|
| Dwell time | **0.7 s** | P6.1's starting value, not yet measured on device |
| Head-gaze multiplier | **1.5×** | 1.05 s on Quest, because head gaze is noisier |
| Relaxed pace | **1.5×** more | a switch, not a hidden preference |
| Eyes-only | **1.35×** the hold requirement | so an accidental dwell cannot lock a shape |
| Cancel | look away | never needs a pinch |
| Re-trigger | look away and back | fires once per acquisition; holding does not repeat |
| Ring | radial fill in the HUD | present even in the fallback path |

**No accidental highlights when looking around:** only fingertip colliders and objects that declare a
`DwellAction` produce a caption. The shelf, the wall and the user's own arm produce nothing. The
5-minute quiet-session test is NOT RUN.

## 6. The fallback chain

| Condition | What happens | What we say |
|---|---|---|
| Eye tracking live **and** an eye pose bound | eye ray | nothing, it just works |
| Eye tracking live, no eye pose bound | head ray | "Eye tracking is available but no eye pose is bound; the highlight follows your head." |
| Eye tracking unavailable (Quest 3/3S) | head ray | "This headset has no eye tracking. The highlight follows your head." |
| User declined | head ray | "Eye tracking is off by your choice. The highlight follows your head." |
| No camera | no highlight | "No gaze source is available. The highlight is off." |

The line appears once per state change, for a few seconds, in the HUD; the status line stays in the
debug overlay. It is information, not an apology, and it is never an error state. `UX-STATES.md` calls
this state `NoEyes`.

## 7. Known limits — written down, not discovered in a review

| Limit | Consequence | What we do |
|---|---|---|
| **No eye tracking on Quest 3 / 3S** | the eye-gaze claim can only be evidenced on hardware that has it | the build says `gaze: head` and explains; the description will too |
| **No eye pose is bound even where eye tracking exists** | eye mode is refused rather than faked | `BindProbe` falls back and says why; binding an anchor is the one-line upgrade |
| **15 mm finger separation** at arm's length | two adjacent fingers may be hard to hit individually | an 11 mm sphere per fingertip, plus a group mode that is one tick away; the precision test is NOT RUN |
| **Arm's length is the worst case** | the highlight is less precise further away | the reference hand sits at ~0.40 m, inside the airplane-seat shell |
| **Dwell fatigue** | long dwells are tiring | dwell is only the eyes-only path, not the default |
| **No spoken audio to caption** | captions cover chimes and band tones only | the caption panel listens to matcher events, not audio clips |

## 8. Inputs
`TECH-STACK.md` §3 · `UX-STATES.md` (`NoEyes`) · `COMPETITION-FIT.md` (Technical Implementation) ·
`FIRST-FIVE-MINUTES.md` (the 4:00 gaze inspection beat) · `EyeTrackingProbe` and `P6Checks`

## 9. Output / done when
- [x] which approach, and why it is not the ISDK block
- [x] how eye tracking is proven active rather than silently substituted
- [x] the eyes-only path, with every **control** reachable without a pinch
- [x] the fallback chain and the one line that goes with it
- [x] the known limits, written down
- [ ] **the on-device proof is recorded** — eyes-only, eyes-still / head-only, side by side — NOT RUN
- [ ] **the dwell times are measured, not assumed** — NOT RUN
- [ ] **the fallback line is tested on a Quest 3S** — NOT RUN
- [ ] **the 0.4 m / 0.6 m finger-precision test** — NOT RUN
