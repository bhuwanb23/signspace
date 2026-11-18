# Judging note — the argument in one page

**Phase:** P10 · **Task:** P10.2 · **Audience:** a judge who has four minutes and has read forty entries.

---

## The three sentences

1. **You cannot rotate a video.** Sign is three-dimensional; every resource that teaches it is flat, seen from one angle.
2. **The content is your own hand.** A reference hand you turn with your wrist, and your tracked hand beside it — no controller, because the instrument being trained is holding it.
3. **It tells you which sign you accidentally made.** When your shape lands closer to a different sign, it names that sign and the number of degrees — the one sentence video cannot say.

## Where the four criteria are answered

| Criterion | The claim | Where a judge can check it |
|---|---|---|
| **Innovation** | Rotation-as-inspection and near-miss diagnosis are only possible with joint-level tracking | the reference hand and the near-miss panel in the build; `NOVELTY-AND-PRIOR-ART.md` names eight prior projects and what each lacked |
| **Experience** | Near-miss hook first, first lock inside a minute, zero instructional words, never a failure state | `text-budget.md`: **0** instructional words at start; the band, the hold and the pause-not-penalise loop |
| **Technical** | Hand tracking, gaze, passthrough, MRUK anchoring, FoV-aware reflow, measured performance posture | 20 handshapes from `StreamingAssets/Probe`, shelf anchored to a real surface, reflow at 70°×66°: worst slot **19.0°**, tray **26.0°**, HUD **19.2°**; **0** realtime lights, **0** shadow casters, 48 renderers / 24,130 vertices, MSAA 2× |
| **Polish** | Every claim in the description is a number the repository contains | `P7 Check all` **131/131**, P6 suite inline **106/106**, P8 **47/47**; 16 audio clips, every peak at or under −6 dBFS |

## The two questions, answered honestly

**"Isn't this just Language IRL for sign language?"**

No, and the difference is the mechanic rather than the medium. Language IRL puts a tutor *in the room* to
speak to. SIGNSPACE puts a **hand** in the room to be inspected — the reference is turnable, the mirror is
yours, and the feedback is geometric: *"that's O, your thumb is 8° too far around"*. There is no
conversation, no avatar, no teacher. That is also its limit: this is a handshape practice companion, not
a language course, and the entry says so in the description and in the credits.

**"Can you actually teach ASL in twelve shapes?"**

The build ships **twenty**, not twelve (`StreamingAssets/Probe/index.json`), and no, twenty handshapes is
not ASL. The claim is narrower and testable: a learner can inspect a 3D handshape from any angle and be
told which sign they actually produced. Both are impossible with video, and both are what the first
minute demonstrates. Fluency is not claimed anywhere in the entry, and the 2020 RCT that found no
significant VR effect on immediate ASL recall is cited in the description rather than hidden.

## What we will not claim in the room

| Thing | Status |
|---|---|
| Frame rate and thermals on Quest 3S | **NOT RUN.** The posture is built and measured on the desk (0 lights, 0 shadows, MSAA 2×, FFR medium); the 15-minute run needs a headset. |
| Three uncoached strangers under 75 s | **NOT RUN.** The instrumentation, the session kit and the friction-log structure exist; the sessions have not happened, so G8 is unevaluated. |
| Video, channel listing and invite URL | **Owner-only.** The freeze record identifies the artifact; the submission record is still blank. |
| Accuracy across all twenty shapes on every hand | **Partly measured.** The matching is a weighted relative-angle rubric with published tolerances; per-shape accuracy across hand sizes is not measured. |

A judge who asks "what did you not test?" gets a straight answer and a list. That is the entry's
position: it is more defensible than a claim that collapses under one follow-up question.

## If there is time for one more sentence

*The app is honest about the sensor — shapes the camera cannot resolve are greyed out with the reason,
and a lost hand pauses the exercise instead of blaming the learner.*
