# UX States

**Status:** ✅ v1 · **Owner:** W1 · **Phase:** P2 · **Gate:** G2

> **The rule that generates this whole document: there is no failure state in this product.** No red, no X, no "try again", no error toast, no spinner over 2 seconds. Every state below is either *informative* or *waiting*. If a design idea needs a failure state to work, the idea is wrong.

---

## 1. First launch

| State | Trigger | What the user sees | Exit |
|---|---|---|---|
| `Boot` | App start | Nothing but the room. No logo, no splash over 1 s | Hands detected |
| `HandsMissing` | No hands tracked after 3 s | Two dim outlines where hands should be; they warm when found | Hands found |
| `TrackingLow` | Confidence below floor | Match display dims; a slow pulse. **No words** | Confidence recovers |
| `Ready` | Hands tracked, no target set | The reference hand fades in holding shape 1 | Grab |

## 2. The core loop

| State | Trigger | Sees | Exit |
|---|---|---|---|
| `Inspect` | Target loaded | Reference hand at conversational distance, rotatable | Gaze or grab |
| `Gazing` | Eye gaze on a finger | That finger highlights with its position label; everything else dims slightly | Gaze off |
| `Rotating` | Pinch on the reference hand | Hand follows the wrist; the depth becomes visible | Release |
| `Warming` | D > 24° | Neutral band, gentle attention cue | D ≤ 24° |
| `Close` | 12° < D ≤ 24° | Visual leans toward the target; hold ring begins to appear | Lock or fall back |
| `Locking` | D ≤ 12° | Hold ring fills over 0.7 s | Complete or reset |
| `Locked` | Hold complete | Dissolve → object → arc to shelf + the signature chime | Next target offered |
| `NearMiss` | Wrong shape is within 18° | *"That's not C. That's O."* + the measured delta. Gentler sound than a lock | Next attempt |

## 3. Spatial

| State | Trigger | Sees | Exit |
|---|---|---|---|
| `SurfaceFound` | MRUK found a horizontal surface | Shelf materialises on real furniture | — |
| `Plinth` | No surface | A virtual plinth in the identical position, with a shadow-catching gradient. **Silent — no message** | A surface appears and the user chooses to move |
| `ShelfFull` | All six mastered | The last slot is filled; the credits object becomes the obvious next thing | — |

## 4. System

| State | Trigger | Sees | Exit |
|---|---|---|---|
| `Paused` | App backgrounded | Freezes in place, no overlay | Foregrounded |
| `NoEyes` | Eye tracking unavailable or declined | Dwell ring on head-gaze, slightly larger targets. One line explaining the fallback | Eye tracking returns |
| `Credits` | Touched the credits object | Signer (or authorship state), city, date, and *"This is practice software, not a replacement for a Deaf teacher."* | Back |
| `LeftHandOff` | The left hand is tracked and this build reads the right hand (P2.7) | **Nothing in the loop.** No message, no dimming, no change of state: 0 words | The user opens the honest mirror, or plays with the right hand |

### 4.1 Handedness — right hand only, and why (P2.7)

**The decision: this build reads the right hand. The left hand is switched off in one place
(`HandSupport.LeftHandSupported`), not half-wired.**

What was measured (`left-hand-decision.md`, `scripts/mirror_sim.py`):

- Mirroring a pose across `x = 0` — `(x, y, z, w)` → `(x, -y, -z, w)` — is **exact** on our data: all
  26 bones within 0.01°, and the shipped metric is mirror-invariant to the bit over all 36 ordered
  pairs, wrist turns included. So a second hand costs the matcher nothing.
- A left hand reaching the matcher **without** mirroring scores **97–176°** on a perfect attempt — the
  coldest state the app has. That is what the build did before this decision, because both hands were
  bound to one matcher (see the second bullet below).
- Whether the mirror is the *right* transform depends on OpenXR handing out left-hand joint frames
  that are already mirrored. No pose file can answer that; only the device test in
  `POSE-FORMAT §4.3` can.

Two consequences that are state, not copy:

1. **The explanation is one line, and it lives in the honest mirror** — "This build reads your right
   hand. Left-hand play is not in this version." Not a toast, not a first-run message: §6 gives the
   first five minutes **3 words total**, and a limitation is something the user chooses to look up.
2. **Only one hand feeds the matcher.** Before P2.7 both hands did, so a visible left hand overwrote
   the right hand's matcher state every frame and the right hand could never hold a lock. Handedness
   is therefore not only a left-handed-user problem; it was a everyone problem, silently.

**Revisit when** POSE-FORMAT §4.3 has run on hardware: flip `LeftHandSupported`, run the two-hand
check, and replace this section with the measured result.

## 5. Forbidden patterns

| Never | Because |
|---|---|
| Red anything | It is the only colour that means failure, and failure does not exist here |
| "Try again" | There is no try; there is a shape and a distance |
| Percentage scores | Invites optimising a number instead of making a shape |
| Error toast / dialog | If the user sees a message, the design failed |
| Spinner over 2 s | Show the room instead; MRUK resolves in the background |
| Tutorial text | Spatial cues first; ≤3 words in the first five minutes |

## 6. Copy budget

| Context | Max words |
|---|---|
| First five minutes, total | **3** |
| Near-miss line | 12 |
| Any state transition | 0 |
| Credits | 60 (it is a credit, not a caption) |
