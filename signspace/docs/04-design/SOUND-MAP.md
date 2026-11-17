# Sound Map
**Status:** ✅ v1 · **Owner:** W1 · **Phase:** P7 · **Gate:** G7 · **Priority:** cut ladder rung 4

> **Every state change has a sound, and there is no music bed.** Sound is a scored sub-criterion and
> the cheapest emotional upgrade in the build — a 3D audio source on an event is ten minutes of work
> and it is the difference between an app that feels alive and one that feels like a spreadsheet.
>
> But it is rung 4 on the cut ladder (*"visual charm only"*), so if the day is bad it goes first and
> nothing else changes. **Everything else in this document is a design decision, and the design
> decisions are what get kept.**

---

## 1. The rules

| Rule | Why |
|---|---|
| **No music bed.** Not one note of background music. | Music under a spoken or silent interaction is the sound of every VR demo. The sounds are the sounds of the thing: a hand, a surface, an object landing. |
| **Every state change has a sound, or a documented silence.** | A state that changes silently feels broken. A state that changes with a sound feels answered. |
| **Spatialised, except the ones that are about you.** | A sound that comes from the object reinforces that the object is in the room. A sound that comes from your own hand should not be spatialised, or it will move as you move and become uncountable. |
| **Short.** Every sound is under 400 ms except the lock chime. | Long sounds overlap and turn into noise. |
| **Quiet by default.** | The room is real. The app should not raise its voice over a conversation. |
| **Never a sound that means "error".** | No buzzer, no descending tone, no "nope". `UX-STATES.md`: there is no failure state. The near-miss sound is a **question**, not a verdict. |

## 2. The event → sound table

| Event | Sound | Spatialised | Character | Why |
|---|---|---|---|---|
| **Boot — hands found** | a soft single sine tone, 440 Hz, 180 ms, with a slow 60 ms fade in | no | warm | "I see you." Not a chime — a chime is a reward, and nothing has been achieved yet |
| **Reference hand appears** | a very quiet filtered noise sweep, 300 ms | yes, at the hand | airy | the hand *arriving* in the space |
| **Grab the reference hand** | a soft click, 90 ms | yes | tactile | a physical grip has a click. This one is not optional: it is the affordance |
| **Release** | a quieter, lower click | yes | tactile | |
| **Rotate the reference hand** | a faint granular texture whose brightness tracks the rotation speed | yes | continuous | rotation *sounds* like turning something. This is the discovery, and it should feel physical |
| **Band: warming → close** | a soft rising interval, two notes, 60 ms apart | no | inviting | the meter leaning in. Quiet enough to be felt rather than heard |
| **Band: close → locked** | a single short high tone, 120 ms | no | clear | the gate opening. **Distinct from the lock** — this is "you are in", not "you did it" |
| **Band: locked → warming** (left the band) | nothing, or the reverse of the rising interval at lower volume | no | neutral | **losing the band is not a failure and gets no punishment sound.** It just stops sounding like progress |
| **Lock — the payoff** | **the signature chime.** Three notes, 120 ms apart: a clean fifth with a soft octave on top, 1.6 s total, gentle decay | yes, at the object | **earned** | this is the product's signature sound. It appears in the video, on the thumbnail, and it is the sound the user will remember |
| **Lock → the object settles** | a soft wooden knock, 140 ms, low-passed | yes, at the shelf | physical | the object landing on the shelf. It is a *thing being put down*, not a notification |
| **Near-miss fires** | **a question.** Two notes a third apart, 180 ms, soft — the same interval as the rising "close" interval but inverted and quieter | no | curious | **this sound must not sound like the lock and must not sound like an error.** It sounds like someone raising an eyebrow. If the user hears it and thinks "I did something wrong", the copy and the sound are both wrong and must be rewritten |
| **Paused — tracking lost** | nothing | — | — | **documented silence.** Pausing is the absence of judgement, and it must not be interrupted by a sound that says something went wrong |
| **Un-paused** | nothing | — | — | |
| **Surface found (MRUK)** | a very quiet low rumble with a soft edge, 500 ms | yes, at the surface | grounding | the shelf has a floor under it |
| **Shelf full** | the lock chime, but one octave up and 60% quieter | yes | small | the last slot. It should be a variation of the reward, not a new sound |
| **Credits opened** | nothing | — | — | reading is silent |
| **Honest-mirror panel opened** | nothing | — | — | reading is silent (§5) |
| **A switch flips** *(added P6.5/P7.1)* | a 36 ms click, 1600 Hz, −14 dBFS | no | tactile | a switch has a click; this is the same "a choice was made" sound as a fired dwell |
| **Calibration starts** *(added P6.4/P7.1)* | a soft 330 Hz tone, 160 ms | no | quiet | it is a transition into a measuring mode, not a reward |
| **Calibration ends** *(added P6.4/P7.1)* | two notes rising, 392→523 Hz, 400 ms | no | settled | the profile is now the user's own hand |

## 3. The lock chime

The product's one memorable sound. It has to survive being played six times in a row without becoming
annoying, and it has to be distinguishable from the near-miss at a glance.

| | |
|---|---|
| **Shape** | a perfect fifth (C-G) with a soft octave (C) above, struck in sequence |
| **Length** | ~1.6 s with a natural decay. Not a bell — a bell is long and metallic |
| **Timbre** | a struck-wood or a soft mallet, not a synth bell. **Wooden, because the objects are wooden and the shelf is real** |
| **Reverb** | a very short, small-room tail. It happens on a desk, not in a cathedral |
| **Spatialised** | yes, at the object, so the reward comes *from the shelf* |
| **Volume** | the loudest sound in the app, and it is still not loud |

**The near-miss sound must not be a subset of the lock chime.** If the near-miss chime is a
truncated version of the lock chime, the user learns to dread the shorter one. They are different
intervals: the lock is a rising fifth with an octave, the near-miss is a falling third.

## 4. Spatialisation rules

| Sound | Spatialised? | Why |
|---|---|---|
| Object landing, surface found, grab, rotate | **yes** | these come from a place in the room, and spatialising them is what makes the room feel real |
| Band changes, lock chime, near-miss | the lock chime **yes** (at the object), the band changes **no** | band changes are about *your* hand, and a sound that moves when you move is uncountable |
| Hands found, pause, unpause | **no** | |
| Near-miss | **no** | it is a comment on what you did, not a thing in the room. If it were spatialised it would appear to come from the reference hand, which would be confusing |

**The reference hand's audio is the interesting one.** It is at 0.40 m, in front, and it rotates with
your wrist. A spatialised rotation sound therefore moves *with your hand* — which is exactly right:
you are turning it.

## 5. The silence policy

**Some things deliberately have no sound:**

| Silence | Why |
|---|---|
| Tracking lost / paused | silence is the absence of judgement. A sound here would say "something is wrong", and nothing is wrong — you moved your hand out of frame |
| Leaving the locked band | not a failure. It just stops sounding like progress |
| The shape failing to lock | **there is no such state.** No sound because there is no event |
| The honest mirror panel opening | it is reading. Reading is silent |
| The app idling | the room is quiet, the app is quiet |

## 6. Implementation

| | |
|---|---|
| **System** | Unity's built-in `AudioSource`. **No FMOD, no Wwise** (`TECH-STACK.md` §1) |
| **Spatialisation** | `AudioSource.spatialBlend = 1.0` for room sounds, `0.0` for body sounds |
| **3D settings** | default rolloff, `minDistance` ~0.3 m, `maxDistance` ~5 m |
| **Mixing** | every sound is a one-shot on a pooled source. No two sounds fight |
| **Files** | short WAVs, mono, 44.1 kHz, under 300 kB each. **Total under 2 MB** |
| **Where** | `unity/Assets/_Project/Audio/SFX/` |
| **Generated, not hunted** | `scripts/make_sfx.py` writes all sixteen clips from this table |
| **The owner** | `Scripts/UI/SoundDirector.cs` (room/body/rotate sources) + `BandAudio.cs` (bands) |

### 6.1 What actually ships (P7.1)

Sixteen mono 44.1 kHz WAVs, **596 kB total** against the 2 MB budget. Every clip is synthesised by
`scripts/make_sfx.py`, so the frequency, envelope and target peak of each are a line of code and the
whole set can be retuned in one command — a binary in a repository is a sound nobody can adjust.

**Every peak is at or under −6 dBFS**, measured from the imported clips by `P7.1 Check sound`, not
from the generator's intent. The first real measurement found the old chime at **−4.1 dBFS** (over
the map's own ceiling) and ten states silent; both are fixed.

| Cue | Clip | Peak | Length | Spatial |
|---|---|---|---|---|
| Hands found | `boot_hands` | −10.5 | 180 ms | no |
| Reference hand appears | `hand_appear` | −18.4 | 300 ms | yes |
| Grab | `grab_click` | −9.4 | 90 ms | yes |
| Release | `release_click` | −14.9 | 80 ms | yes |
| Rotate | `rotate_texture` (loop, speed-driven volume) | −20.0 | 1.0 s | yes |
| Band warming → close | `band_close` | −10.5 | 340 ms | no |
| Band close → locked | `band_lock_in` | −9.4 | 120 ms | no |
| Band locked → warming | `band_release` | −15.9 | 300 ms | no |
| **Lock — the payoff** | `lock_chime` (C5–G5–C6, 120 ms apart, wooden partials, small-room tail) | **−6.4** | 1.72 s | yes, at the slot |
| Object settles | `object_settle` | −11.1 | 144 ms | yes |
| Shelf full | `shelf_full` (octave up, quieter) | −14.0 | 1.2 s | yes |
| Near-miss | `near_miss` (falling third) | −11.1 | 400 ms | no |
| Surface found | `surface_found` | −15.9 | 400 ms | yes |
| Switch flip / dwell fire | `toggle_click` | −14.0 | 36 ms | no |
| Calibration start | `calibrate_start` | −15.9 | 160 ms | no |
| Calibration end | `calibrate_done` | −10.5 | 400 ms | no |

**The chime is the loudest thing in the app and it is −6.4 dBFS.** The near-miss is 5 dB below it and
a falling third rather than a rising fifth; the check verifies that the two are not the same interval
by comparing their spectral ratio, so a later "tidier" rewrite of the near-miss cannot quietly make it
a transposed chime.

**Silences are listed in code** (`SoundDirector.DocumentedSilences`), not only in this document, so a
missing clip on a real cue cannot be excused as a deliberate silence.

**One audio source per kind of sound, pooled.** A new `AudioSource` per event is a per-frame
allocation waiting to happen, and `PERFORMANCE.md` §5 says allocations are the first thing to check.

## 7. Priority order if the day runs out

Rung 4 of the cut ladder is *sound polish*. But not all sounds are equal:

| Keep | Cut first |
|---|---|
| **The lock chime** — it is the product's signature | the rotation texture |
| **The grab click** — it is the affordance for the whole interaction | the surface rumble |
| **The near-miss question** — the signature feature needs an audible shape | the band-change interval |
| **The object landing** — it makes the shelf physical | the hands-found tone |
| The rest | the shelf-full variant |

**If all sound is cut, the product still works.** Nothing in the design depends on hearing it. That
is the test of whether a sound is doing real work: if removing it breaks the product, it is
essential; if it only makes it nicer, it is polish.

## 8. Inputs
`UX-STATES.md` (the state list — every state here maps to one) · `FIRST-FIVE-MINUTES.md` (the lock
chime is the payoff at 0:30) · `TECH-STACK.md` §1 (no FMOD, no Wwise) · `VIDEO-SCRIPT.md` (the chime is
the cold open at 0:00)

## 9. Output / done when
- [x] every state in the UX state list has an assigned sound or a documented silence
- [x] the near-miss sound is distinguished from the lock
- [x] the spatialisation rules
- [x] the silence policy
- [x] the priority order for cutting
- [x] **the sounds exist and are wired** — 16 clips, 12 cues into `SoundDirector`, 3 bands into
      `BandAudio`, 0 cues without a clip, every peak measured at or under −6 dBFS (`P7.1 Check sound`)
- [ ] **the lock chime has been listened to six times in a row and is not annoying** — **NOT RUN**.
      The synthesiser can prove the interval, the length and the peak; it cannot judge fatigue. This is
      an ears row, and it is `HUMAN-TASKS` H28.
- [ ] **the real wooden timbre** — the clips are spec-shaped placeholders (see §6.1). Replacing them is
      a clip swap and no code change, which is why the wiring was worth doing first.
