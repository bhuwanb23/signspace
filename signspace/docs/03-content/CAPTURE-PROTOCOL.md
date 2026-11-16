# Capture Protocol — SIGNSPACE

**Print this. It is the whole session.** Walk through §2 (the one-pager) with the signer before anything
is recorded, then follow §§3–9 in order. §§10–13 are consent, compensation, post-session and
contingency — read §10 aloud before the first take.

**Status:** complete and printable · **Owner:** W3 · **Phase:** P5 · **Gate:** G5
**Ceiling:** 90 minutes. Hands tire after that and the data gets worse. If you are past 80 minutes,
drop the blind-spot set to two vantages and stop.

---

## §0 — COMPRESSED MODE (fallback, read first if the calendar is short)

If only 60 minutes are available, use the compressed protocol instead: six shapes (B, U, V, W, C, O),
one call, no mocap hardware. The poses are authored first from published ASL references; the call
becomes (1) consent and credit wording, (2) the signer holds each shape to camera for ~10 s front-on
and ~30° turned, (3) review of each authored 3D shape with a yes/no per shape. Full detail in §10–§12.
The rest of this document (§§1–§9) is the full protocol. **Use §0 only if you have to.**

---

## §1 — Overview (full protocol)

**Goal.** Every shipped handshape traces to a named, credited Deaf signer who confirmed it reads
correctly. The blind-spot set (M, N, T, R, S, E) is captured *correctly* so the honest mirror can
show them by name and reason — not as a guess.

**Scope.**
- **12 shipped shapes** (SIGNSET §§2–4): B, U, V, W (spread) · A, C, O, D (closure) · I, L, Y, F (extension).
- **6 blind-spot shapes** (honest mirror): M, N, T, R, S, E.
- **18 shapes total**, 3 vantages each for the shipped set, 2 minimum (front + side) for the blind-spot set.

**What makes one session enough.** The protocol, the naming convention and the manifest schema below.
Nothing is ad hoc on the day.

---

## §2 — The printable one-pager (what the signer sees)

> **SIGNSPACE — handshape capture, ~90 minutes**
>
> **What this is.** A small VR practice app for ASL handshapes. 12 fingerspelling letters appear as
> life-size 3D hands you can turn with your own wrist and copy with your own hand. It tells you which
> sign you accidentally made. It is practice software, not a course, not a translator, and not a
> replacement for a Deaf teacher.
>
> **What I need from you**
> 1. **Consent** (first 10 min) — credit wording agreed before anything is recorded.
> 2. **Your hands** (about 40 min) — hold each shape, one at a time, three angles.
> 3. **Your eye on my shapes** (about 25 min) — I show you each 3D handshape I built; you say yes
>    or no. This is the part that matters most.
> 4. **A neutral pose** (1 min) — a relaxed open hand, twice.
>
> **The 12 shapes, in order** (grouped by family; C and O back to back):
>
> | # | Shape | Family | Hold |
> |---|---|---|---|
> | 1 | B | Spread | 3 s |
> | 2 | U | Spread | 3 s |
> | 3 | V | Spread | 3 s |
> | 4 | W | Spread | 3 s |
> | 5 | A | Closure | 3 s |
> | 6 | C | Closure | 3 s |
> | 7 | O | Closure | 3 s |
> | 8 | D | Closure | 3 s |
> | 9 | I | Extension | 3 s |
> | 10 | L | Extension | 3 s |
> | 11 | Y | Extension | 3 s |
> | 12 | F | Extension | 3 s |
>
> **Then the blind-spot set** (for the honest mirror — these are the shapes we deliberately do not
> ship, and we need them *correct*): M, N, T, R, S, E. Same 3 s hold, front + side.
>
> **Between takes:** relax your hand fully, shake it out for two seconds, then reset to the next shape.
> If a finger cramps, say so — we pause, not push through.
>
> **What you get.** Your name and city in the app's credits and in the submission, a link to the
> submitted entry, and a fixed fee paid on the day, regardless of outcome.
>
> **What I will not do.** I will not claim your name on anything you have not seen, and I will not
> ship a shape you have not said yes to.

---

## §3 — Equipment

| Item | Requirement | Why |
|---|---|---|
| Camera 1 | front, landscape, 1080p if possible | primary reference angle |
| Camera 2 | ~30° to one side (left or right), landscape, 1080p | depth is the whole product — C vs O is invisible from the front |
| Lighting | **front light, no strong backlight.** A window in front of you is ideal; a lamp behind the camera is the enemy | a silhouette hides the depth the whole product is about |
| Background | anything plain | we are reading the hand, not the room |
| Signer's hand | wrist visible; no rings, watches or reflective jewellery | rings occlude joints and confuse the reference |
| Rest | the signer's own hand, relaxed between takes | the thumb's position across the palm is a distinguishing feature |
| Recording | record the call with the signer's explicit consent, stated on the call | the consent itself is on the recording; the approval table is transcribed from it |

**The one thing to get right:** the shapes are distinguished by *depth*, not outline. C and O are
nearly identical from the front and obviously different from the side. That is why the second camera
exists. If only one camera is available, capture front + a manual ~30° turn on the same camera,
back to back.

---

## §4 — Takes

| Take element | Spec |
|---|---|
| Hold duration | **3 s** per shape, per vantage, hand held steady |
| Vantages | **3 per shipped shape:** front · side-left (~30°) · side-right (~30°) |
| Blind-spot set | **2 minimum** (front + one side); 3 if time permits |
| Neutral | **2 s** of a relaxed open hand, palm down, held still — once at session start, once at session end |
| Order | B, U, V, W · A, C, O, D · I, L, Y, F · then M, N, T, R, S, E |
| Between takes | relax fully, shake out for 2 s, reset to the next shape |
| C and O | captured **back to back**, because the difference between them is the whole point and the signer's eye is sharpest on the second one |

**Also capture, if the signer is willing:** a **neutral** per family reset (2 s, palm down). Nice to
have, not a blocker — the relative joint angles hold regardless.

---

## §5 — Naming convention

Every take is traceable to a person, a date and a vantage. Folder layout:

```
assets/masters/<signer-slug>/<YYYY-MM-DD>/
    B.front.mp4
    B.side-left.mp4
    B.side-right.mp4
    U.front.mp4
    ...
    neutral.start.mp4
    neutral.end.mp4
    MANIFEST.md
```

**File name:** `<SHAPE>.<vantage>.mp4` where `<SHAPE>` is the uppercase letter and `<vantage>` is
one of `front`, `side-left`, `side-right`. Neutral takes are `neutral.start.mp4` and `neutral.end.mp4`.

**Slug rule:** `<signer-slug>` is the signer's first name lowercased, or `anon-<n>` if they chose
anonymous credit. One folder per session; re-captures go in a new dated folder, never overwriting.

---

## §6 — Manifest row (one per take)

`MANIFEST.md` in the session folder, one row per file:

| Field | Value |
|---|---|
| Signer | name, as it should be credited |
| City | as it should be credited |
| Date | ISO-8601 |
| Consent | the consent form reference, and where the signed copy lives |
| Takes | one row per file: shape · vantage · take number · file name · duration · approved? (yes/no) · note |
| Approved shapes | the shapes the signer said yes to on the call |
| Rejected shapes | the shapes they said no to, and what was changed |
| Credit line | the exact agreed wording |
| Session length | actual minutes |

**Approval table (fill after the session):**

| Shape | Family | Front | Side-L | Side-R | Approved? | Notes |
|---|---|---|---|---|---|---|
| B | Spread | ⬜ | ⬜ | ⬜ | ⬜ | |
| U | Spread | ⬜ | ⬜ | ⬜ | ⬜ | |
| V | Spread | ⬜ | ⬜ | ⬜ | ⬜ | |
| W | Spread | ⬜ | ⬜ | ⬜ | ⬜ | |
| A | Closure | ⬜ | ⬜ | ⬜ | ⬜ | |
| C | Closure | ⬜ | ⬜ | ⬜ | ⬜ | |
| O | Closure | ⬜ | ⬜ | ⬜ | ⬜ | |
| D | Closure | ⬜ | ⬜ | ⬜ | ⬜ | |
| I | Extension | ⬜ | ⬜ | ⬜ | ⬜ | |
| L | Extension | ⬜ | ⬜ | ⬜ | ⬜ | |
| Y | Extension | ⬜ | ⬜ | ⬜ | ⬜ | |
| F | Extension | ⬜ | ⬜ | ⬜ | ⬜ | |
| M | Blind-spot | ⬜ | ⬜ | ⬜ | ⬜ | honest mirror |
| N | Blind-spot | ⬜ | ⬜ | ⬜ | ⬜ | honest mirror |
| T | Blind-spot | ⬜ | ⬜ | ⬜ | ⬜ | honest mirror |
| R | Blind-spot | ⬜ | ⬜ | ⬜ | ⬜ | honest mirror |
| S | Blind-spot | ⬜ | ⬜ | ⬜ | ⬜ | honest mirror |
| E | Blind-spot | ⬜ | ⬜ | ⬜ | ⬜ | honest mirror |

---

## §7 — The blind-spot set (honest mirror)

M, N, T, R, S, E are the shapes today's tracking cannot resolve. They are **not shipped** as playable
shapes, but they **must be captured correctly** so the honest mirror can show them by name and reason.

| Shape | Why it is excluded | What to capture |
|---|---|---|
| M | Thumb tucked under three fingers — a finger behind a finger, invisible to the cameras | front + side, thumb position clear |
| N | Thumb tucked under two fingers — same failure | front + side, thumb position clear |
| T | Thumb between index and middle — occlusion + contact | front + side, the thumb slot visible |
| R | Crossed fingers — crossing is not represented by the tracker | front + side, the crossing visible |
| S | Thumb across the front of the fist — may resolve; competing with A | front + side, thumb-across vs thumb-alongside |
| E | Fingertips on the thumb base — cramped, high self-occlusion | front + side, the contact point |

**If the signer cannot make a shape, do not fake it.** Drop it and say so in the honest mirror:
*"W is not in this build because our signer's hand does not make it the way the alphabet shows it."*
That is a better sentence than a wrong shape.

---

## §8 — Retake rule

A take is **rejected and re-shot** if any of these hold:

- the hand is **out of frame** (any fingertip or the wrist leaves the shot);
- the hand is **rotated past 30°** from the intended vantage;
- the hand is **unlit** (silhouette, strong backlight, shadow across the fingers);
- the signer is wearing **rings, watches or reflective jewellery**;
- there is **motion blur** (hand moved during the 3 s hold);
- the shape made does **not match** the intended shape (signer self-corrects on the next take).

**Re-ask on the call.** There is time in the 90 minutes. A bad take is not a problem; a shipped
bad take is.

---

## §9 — 90-minute schedule

| Time | Block | Minutes |
|---|---|---|
| 0–5 | Setup: cameras, lighting, consent spoken (§10), recording started | 5 |
| 5–10 | Credit wording agreed (§11) + compensation confirmed | 5 |
| 10–12 | Neutral: start take (2 s, 3 vantages) | 2 |
| 12–22 | Family 1 — Spread: B, U, V, W (4 shapes × 3 vantages) | 10 |
| 22–34 | Family 2 — Closure: A, C, O, D (C and O back to back) | 12 |
| 34–46 | Family 3 — Extension: I, L, Y, F | 12 |
| 46–60 | Review of authored 3D shapes, yes/no per shape (§12 step 2) | 14 |
| 60–75 | Blind-spot set: M, N, T, R, S, E (front + side) | 15 |
| 75–80 | Neutral: end take; re-shoot any rejected takes | 5 |
| 80–85 | Transcribe the approval table from the recording | 5 |
| 85–90 | Wrap: consent confirmation, fee paid, link sent, archive | 5 |

**If you are past 80 minutes,** drop the blind-spot set to two vantages and stop. Never compress
the shipped set — a tired hand on B is worse than no M at all.

---

## §10 — Consent (spoken on the call, before anything is recorded)

> "Before we start — I'm recording this call so I have a record of what we agree, and I'll keep
> the recording. I'll use the handshape clips and, if you approve the shapes, your name and city, in
> a public competition entry for a Meta VR developer competition. The app is free and the entry is
> public. I'm not asking for anything that isn't in that sentence, and you can tell me to delete
> anything at any point. Are you happy for me to record, and happy with that use?"

**Rights asked for, and nothing more:**

| Right | Asked for? |
|---|---|
| Use the handshape clips as reference material to author 3D shapes | **yes** |
| Use the approved 3D shapes in a public competition entry | **yes** |
| Credit by name and city | **yes**, wording agreed on the call |
| Use the clips in future commercial releases | **ask separately, later, in writing.** Not on this call. |
| Use the signer's likeness, voice or face in promotional material | **no.** Not asked for, not used. |
| Any use as training data for a model | **no.** The app has no AI and no network. |

**If the signer declines the credit by name** — say nothing else about it, use the anonymous credit
line *"Handshapes verified by a Deaf ASL signer"* — and ship that. The verification is what
matters; the name is a bonus.

Full consent document: `assets/masters/CONSENT-TEMPLATE.md`.

---

## §11 — Compensation and credit

**Compensation.** Fixed fee, paid on the day, regardless of outcome. Whatever the number is, it is
the same whether the shapes are approved or not — the work is the work. **Never make payment
conditional on approval.** That would turn a review into a purchase, and the review is the entire
point.

**Credit — two states, both honest (ADR-007):**

| State | `creditLine` | When |
|---|---|---|
| **A · signer-verified** | *"Handshapes verified by [name], a Deaf ASL signer in [city], on [date]. This is practice software, not a replacement for a Deaf teacher."* | The signer approved every shipped shape on video |
| **B · reference-authored** | *"Handshapes authored against published ASL references. Deaf signer review in progress — corrections ship in the next build. This is practice software, not a replacement for a Deaf teacher."* | Anything less than full approval at submit time |

The credit is **data**, not copy: `Assets/StreamingAssets/Signs/set-meta.json`. Flipping between
states is a five-minute edit and a rebuild. It is never a silent change — the description and the
video must match whichever state ships.

**If the signer approves only some shapes**, the state is B, and the approved subset is named
explicitly. Partial truth, stated partially.

Compensation agreement: `assets/masters/COMPENSATION-AGREEMENT.md`.
Credit options: `assets/masters/CREDIT-LINE-OPTIONS.md`.

---

## §12 — After the session

| # | Step | Tool | Fails if |
|---|---|---|---|
| 1 | Transcribe the approval table from the recording | — | the recording is unclear → re-watch before authoring |
| 2 | Author or re-author the approved shapes | `scripts/make_pose.py` | — |
| 3 | Regenerate the poses against the rig's rest pose | `make_pose.py --neutral rest-pose.json` | the rest pose is not identity → regenerate |
| 4 | Validate every pose file | `python3 scripts/validate_poses.py` | any malformed pose → fix before it ships |
| 5 | Re-measure the metric against the changed shapes | `python3 scripts/tune_match.py` | a near-miss pair falls below 26° → re-tune or fix the shape |
| 6 | Update `set-meta.json` to state A or B | one JSON file | — |
| 7 | Update the description's integrity paragraph to match the state | `SUBMISSION-COPY.md` | the description and the app disagree → **fix before submit** |
| 8 | Archive the manifest | `assets/masters/<signer-slug>/` | — |

**Step 5 is not optional.** Changing a shape changes the pairwise distances, and the near-miss
threshold depends on them. The tuner takes two minutes.

---

## §13 — Contingency

| Situation | Response |
|---|---|
| A shape is captured badly | Re-ask on the call. There is time in the 90 minutes. |
| The signer cannot make a shape | **Do not fake it.** Drop it, and say so in the honest mirror. |
| The call is cut short | Take what was approved, ship state B, ask to finish the rest later. The build is not blocked by the credit. |
| No signer at all | Ship state B, say so plainly in the credits, the description and the video, and **write the date the review is expected by.** A silent state B is not acceptable. |
| The signer asks to be removed later | Remove the name, flip to state B, rebuild, re-upload. The credit line is a promise, and the switch is designed to be cheap. |

---

## Inputs

`SIGNSET.md`, `POSE-FORMAT.md`, `MATCH-ALGORITHM.md`, `scripts/make_pose.py`, `scripts/tune_match.py`,
`assets/masters/CONSENT-TEMPLATE.md`, `assets/masters/COMPENSATION-AGREEMENT.md`,
`assets/masters/CREDIT-LINE-OPTIONS.md`, `assets/masters/MANIFEST.md`.

## Output / done when

- [x] A signer can be booked, briefed and captured in under 90 minutes
- [x] The manifest survives an audit: every shape traces to a person, a date and an approval
- [x] The consent script is written and does not ask for more than the app will use
- [x] Both credit states are pre-written and the switch is one file
- [x] The contingency table says what to do when a take is unusable — including **dropping the shape**
- [x] Naming convention and manifest schema defined (§5, §6)
- [x] Retake rule defined (§8)
- [x] 90-minute schedule defined (§9)
- [ ] **the manifest for the actual session exists**
- [ ] **`set-meta.json` states the truth about the actual session**
