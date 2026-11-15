# Capture Protocol

## 0. COMPRESSED MODE — signer-verified, not motion-captured (read first if the calendar is short)

We do not need mocap hardware. The integrity requirement is that every shipped handshape traces to a named, credited Deaf signer who confirmed it reads correctly — not that it came off a rig.

### The 60-minute remote protocol — one call, not two

**The sequence was inverted on 15 Nov 2026.** The original protocol needed the signer's recording
*before* we could author the poses, and their approval *after* that — two synchronous calls, with a
Day-3 evening call leaving no room for the second one.

Now the poses are authored **first**, from published ASL references, with no signer involved
(`scripts/make_pose.py`). So the call becomes:

1. **The call** — consent and credit wording agreed; the signer holds each of the six shapes to
   camera for ~10 s, front-on and turned ~30°, as ground truth; and we show them each 3D shape we
   authored, rotating, and they answer yes/no per shape.
2. **Async, if needed** — a rejected shape is re-authored and sent back as a clip. A rejection does
   not need another meeting.

**One call. Sixty minutes.** That is the whole ask.

**Preserved:** signer authorship, signer verification, real credit.
**Cost:** fidelity — hand-authored poses are slightly less natural than retargeted mocap. Acceptable at six shapes; not at sixty.
**Non-negotiable:** the signer still sees every shape before it ships. A pose we shaped that a Deaf signer has never seen is exactly the failure mode R1 exists to prevent.

**Status:** ✅ written · **Owner:** W3 · **Phase:** P5 · **Gate:** G5
**The ask, ready to send:** [`../../phases/p0-foundation/OWNER-TASKS.md`](../../phases/p0-foundation/OWNER-TASKS.md) §E

---

## 1. The session plan — the one-pager for the signer

Print this. It is the whole thing the signer needs in front of them.

> **SIGNSPACE — handshape review, ~45 minutes**
>
> **What this is.** A small VR practice app for ASL handshapes, built for a Meta developer
> competition. Six fingerspelling letters — B, U, V, W, C, O — as 3D hands you can turn with your
> own wrist and copy with your own hand. It tells you which sign you accidentally made.
>
> **What it is not.** Not a course. Not a translator. Practice software — and the app itself says
> *"This is practice software, not a replacement for a Deaf teacher."*
>
> **What I need from you**
> 1. **Consent** — that I may use the handshapes in a public competition entry, and credit you by
>    name and city. (We can do the credit wording first, in the first ten minutes.)
> 2. **Your hands** — hold each of the six shapes for about ten seconds, once facing the camera and
>    once turned about thirty degrees. Six shapes, about five minutes.
> 3. **Your eye on my shapes** — I will show you each 3D handshape I built, turning slowly. Tell me
>    yes or no: is that the correct handshape? This is the part that matters most.
> 4. **A credit line** — the exact words that appear in the app and in the competition entry.
>
> **What you get.** Your name and city in the app's credits and in the submission, and a link to the
> submitted entry. If you would rather try the build first, say so and I will send it.
>
> **What I will not do.** I will not claim your name on anything you have not seen, and I will not
> ship a shape you have not said yes to.

## 2. Equipment and room

| | Requirement | Why |
|---|---|---|
| Camera | any laptop or phone camera, **landscape**, 1080p if possible | the shapes are read from the front and from ~30° |
| Lighting | **front light, no strong backlight.** A window in front of you is ideal; a lamp behind the laptop is the enemy | a silhouette hides the depth the whole product is about |
| Background | anything plain | we are reading the hand, not the room |
| Rest | the signer's own hand, wrist visible | the thumb's position across the palm is a distinguishing feature |
| Recording | record the call with the signer's explicit consent, stated on the call | the consent itself is on the recording |

**The one thing to get right:** the shapes are distinguished by *depth*, not outline. C and O are
nearly identical from the front and obviously different from the side. That is why the second angle
exists.

## 3. The take list

| Shape | Family | Front take | Turned take | Notes for the signer |
|---|---|---|---|---|
| B | spread | ⬜ | ⬜ | fingers straight and together, thumb folded across the palm |
| U | spread | ⬜ | ⬜ | index and middle up and together, ring and little folded down |
| V | spread | ⬜ | ⬜ | index and middle up and **spread apart** |
| W | spread | ⬜ | ⬜ | index, middle and ring up and spread, little folded |
| C | closure | ⬜ | ⬜ | fingers curved into a C, **not touching** the thumb |
| O | closure | ⬜ | ⬜ | fingers curved into a closed circle, thumb touching |

**Also capture, if the signer is willing:** the **neutral** — a relaxed open hand, palm down, held
still for two seconds. It is the reference every relative joint angle is measured against. If the
signer's neutral differs from ours, the relative angles still hold; it is a nice-to-have, not a
blocker.

**The blind-spot set — M, N, T, R, S, E** — is *not* captured in compressed mode. Those shapes are
the ones today's tracking cannot resolve, and the honest mirror describes them by name and reason.
Capturing them is a P5 task for the 60-sign version.

**Order matters:** capture B, U, V, W first (the spread family, fast), then C and O. **C and O
together, back to back**, because the difference between them is the whole point and the signer's
eye will be sharpest on the second one.

## 4. Naming and the manifest

Every take is traceable to a person and a date. That is what makes the credit defensible.

```
assets/masters/<signer-slug>/<YYYY-MM-DD>/
    B.front.mp4
    B.turned.mp4
    U.front.mp4
    ...
    neutral.mp4
    MANIFEST.md
```

`MANIFEST.md` per capture session:

| Field | Value |
|---|---|
| Signer | name, as it should be credited |
| City | as it should be credited |
| Date | ISO-8601 |
| Consent | the consent form reference, and where the signed copy lives |
| Takes | one row per file, with the take number and any note |
| Approved shapes | the shapes the signer said yes to on the call |
| Rejected shapes | the shapes they said no to, and what was changed |
| Credit line | the exact agreed wording |
| Session length | actual minutes |

The manifest never goes into git if it contains personal data beyond the agreed credit. **The
agreed credit line and the approval table do** — those are the audit trail.

## 5. Consent — script and rights scope

**Spoken on the call, before anything is recorded:**

> "Before we start — I'm recording this call so I have a record of what we agree, and I'll keep
> the recording. I'll use the handshape clips and, if you approve the shapes, your name and city, in
> a public competition entry for a Meta VR developer competition. The app is free and the entry is
> public. I'm not asking for anything that isn't in that sentence, and you can tell me to delete
> anything at any point. Are you happy for me to record, and happy with that use?"

**The rights asked for, and nothing more:**

| Right | Asked for? |
|---|---|
| Use the handshape clips as reference material to author 3D shapes | **yes** |
| Use the approved 3D shapes in a public competition entry | **yes** |
| Credit by name and city | **yes**, wording agreed on the call |
| Use the clips in future commercial releases | **ask separately, later, in writing.** Not on this call. |
| Use the signer's likeness, voice, or face in promotional material | **no.** Not asked for, not used. |
| Any use as training data for a model | **no.** The app has no AI and no network (`TECH-STACK.md` §8). |

**If the signer declines the credit by name** — say nothing else about it, use the anonymous credit
line *"Handshapes verified by a Deaf ASL signer"* — and ship that. The verification is what matters;
the name is a bonus.

## 6. Compensation and credit

**Compensation.** The plan's budget for a signer is a fixed fee, paid on the day, regardless of
outcome. Whatever the number is, it is the same whether the shapes are approved or not — the work
is the work. **Never make payment conditional on approval.** That would turn a review into a
purchase, and the review is the entire point.

**Credit — two states, both honest** (`ADR-007`):

| State | `creditLine` | When |
|---|---|---|
| **A · signer-verified** | *"Handshapes verified by [name], a Deaf ASL signer in [city], on [date]. This is practice software, not a replacement for a Deaf teacher."* | The signer approved every shipped shape on video |
| **B · reference-authored** | *"Handshapes authored against published ASL references. Deaf signer review in progress — corrections ship in the next build. This is practice software, not a replacement for a Deaf teacher."* | Anything less than full approval at submit time |

The credit is **data**, not copy: `Assets/StreamingAssets/Signs/set-meta.json`. Flipping between
states is a five-minute edit and a rebuild. It is never a silent change — the description and the
video must match whichever state ships.

**If the signer approves only some shapes**, the state is B, and the approved subset is named
explicitly. Partial truth, stated partially.

## 7. After the session

| # | Step | Tool | Fails if |
|---|---|---|---|
| 1 | transcribe the approval table from the recording | — | the recording is unclear → re-watch before authoring |
| 2 | author or re-author the approved shapes | `scripts/make_pose.py` | — |
| 3 | regenerate the poses against the rig's rest pose | `make_pose.py --neutral rest-pose.json` | the rest pose is not identity → regenerate |
| 4 | validate every pose file | `python3 scripts/validate_poses.py` | any malformed pose → fix before it ships |
| 5 | re-measure the metric against the changed shapes | `python3 scripts/tune_match.py` | a near-miss pair falls below 26° → re-tune or fix the shape |
| 6 | update `set-meta.json` to state A or B | one JSON file | — |
| 7 | update the description's integrity paragraph to match the state | `SUBMISSION-COPY.md` | the description and the app disagree → **fix before submit** |
| 8 | archive the manifest | `assets/masters/<signer-slug>/` | — |

**Step 5 is not optional.** Changing a shape changes the pairwise distances, and the near-miss
threshold depends on them. The tuner takes two minutes.

## 8. Contingency — if a take is unusable

| Situation | Response |
|---|---|
| A shape is captured badly | Re-ask on the call. There is time in the 60 minutes. |
| The signer cannot make a shape | **Do not fake it.** If the signer cannot make W, we do not ship W. Drop it, and say so in the honest mirror: *"W is not in this build because our signer's hand does not make it the way the alphabet shows it."* That is a better sentence than a wrong shape. |
| The call is cut short | Take what was approved, ship state B, and ask to finish the rest later. The build is not blocked by the credit. |
| No signer at all | Ship state B, say so plainly in the credits, the description and the video, and **write the date the review is expected by.** A silent state B is not acceptable. |
| The signer asks to be removed later | Remove the name, flip to state B, rebuild, re-upload. The credit line is a promise, and the switch is designed to be cheap. |

## Inputs
`SIGNSET.md`, `POSE-FORMAT.md`, `MATCH-ALGORITHM.md`, `scripts/make_pose.py`, `scripts/tune_match.py`

## Output / done when
- [x] A signer can be booked, briefed and captured in under 90 minutes
- [x] The manifest survives an audit: every shape traces to a person, a date and an approval
- [x] The consent script is written and does not ask for more than the app will use
- [x] Both credit states are pre-written and the switch is one file
- [x] The contingency table says what to do when a take is unusable — including **dropping the shape**
- [ ] **the manifest for the actual session exists**
- [ ] **`set-meta.json` states the truth about the actual session**
