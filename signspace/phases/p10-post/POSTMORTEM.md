# Post-mortem

**Phase:** P10 · **Task:** P10.5 · **Written:** 18 Nov 2026 · **Gate:** G10

> The hour counts are the value, and the project does not have them. That is the first finding, not a
> disclaimer: nobody logged time, so every "cost" below is either a **measured** artefact count or a
> **derived** window from commit timestamps. The two are labelled, and neither is presented as effort.

---

## 1. What the eleven days actually cost

The plan allocated eleven days and **82 h** across eleven phases. The phase READMEs' own budgets sum to
**86 h** — the internal inconsistency was never reconciled, and it is the first sign that the schedule was
written as an aspiration.

| Measure | Value | Kind |
|---|---|---|
| Calendar days with commits | **4** (15–18 Nov 2026) | measured |
| Commits | **57** | measured |
| Tracked files at the end | **723** | measured |
| Lines: inserted / deleted | **116,280 / 35,869** across 1,219 file-changes | measured (git shortstat) |
| Commit-window span per day | 15th 11:13–16:43 · 16th 15:12–22:34 · 17th 19:00–22:58 · 18th 08:00 | derived (first→last commit) |
| Sum of those windows | **≈16.8 h** | derived, a **lower bound** — it excludes every hour that produced no commit |
| Hours actually worked | **NOT RECORDED** | nobody timed it |
| Planned hours per phase | P0 5 · P1 8 · P2 10 · P3 8 · P4 8 · P5 12 · P6 7 · P7 8 · P8 6 · P9 10 · P10 4 | from the phase READMEs |

**What the table says.** The build was compressed into four calendar days against a plan of eleven, and the
commit windows account for under seventeen hours of it. Either the work was faster than planned, or — more
likely — the measured artefact is not the labour: the phases are dense in checks and documents rather than
in elapsed time. Without a timer, the honest statement is *"four days, 57 commits, hours unknown"*, and
that is what G10's "real hour counts" criterion gets: a FAIL, recorded as such.

## 2. Which gate was closest to red

**G8, by a distance — and it is red.** The playtest phase produced the instrument (`PlaytestProbe`, a
131-assertion desk suite in P7 and 47 in P8), the session kit and the friction-log structure, but no
stranger ever put the headset on. G8's number — first lock under 75 s, uncoached — therefore does not
exist, and the phase that was supposed to be the project's only honest measure of whether the first five
minutes work is the one that was run as a code task.

**Second closest: G9/G10.** The artifact is genuinely frozen — the APK on disk still hashes to
`81b60582…`, the version constants match, and a rebuild never happened after the freeze. The *record* of
the submission is empty, the archive is a template, there is no tag, and the only video is a previs. The
freeze protected the bytes and nobody protected the paperwork.

**G7** sits behind both: six of its seven criteria are device rows (frame rate, thermals, cold boots,
crash sweep, hostile conditions, FoV on hardware) and none were run. The desk measurements are real and
narrow — 0 realtime lights, 0 shadow casters, 48 renderers, 24,130 vertices, worst slot 19.0° at 70°×66°,
tray 26.0°, 0 instructional words — but a frame rate is not among them.

## 3. Three carry-forward lessons

**1. Assert the user-visible path, not the component.**
Every defect P7 found was a wiring defect that every unit-level check had passed: the high-contrast switch
reached two panels and not the other three; the near-miss component existed with no object in the scene;
the chime was over the mix ceiling the map specified. A component that is present, correct and unconnected
looks identical to a working feature in a check that inspects components. *Carry forward:* every
user-facing feature gets one assertion that starts from the user's action and ends at the pixels, not at
the field being set.

**2. A freeze without a tag and a commit hash protects nothing.**
The bytes survived; the identity did not. `apk-hash.txt` has an empty `commit:` line, there is no
`v1.0-competition` tag, and three paperwork commits landed after the frozen-at timestamp. Anyone asking
"what code produced this APK?" in three weeks has a hash and no commit. *Carry forward:* the freeze record
is written by a script that refuses to write it if the tag is missing, and the same script runs in CI.

**3. Desk verification quietly replaces the thing it stands in for.**
P7's 131 assertions and P8's 47 are real work and they made the build better. They also made it possible
to finish two phases without doing the one thing those phases were for — a stranger, and a headset. The
substitution was never deliberate; it was the next available task. *Carry forward:* a phase whose gate
names a human or a device gets scheduled as an appointment with a person, not as a checklist item.

## 4. What we would do with 30 days instead of 11

| Days | Use | Why it is first |
|---|---|---|
| 1–3 | Device truth on Quest 3S: 15-min frame rate, thermals, 20 cold launches, five hostile conditions | converts G7 from posture to numbers; a Store prerequisite |
| 4–8 | Three strangers, uncoached, then fix only the top three | G8 is the only measure of whether the product works |
| 9–11 | Re-test with a fresh stranger; verify the near-miss landed organically | closes the loop the phase exists for |
| 12–18 | Content to 40 shapes with a signer under contract | the launch promise is content, not code |
| 19–24 | Video, channel, clean-account install path, archive | P9's real cost, which the 11-day plan gave 10 h |
| 25–30 | Freeze properly: tag, commit, hash, three rehearsals of the verify script | the last week is where the entry was actually lost |

## 5. What we got wrong

**About the idea.** The medium argument is strong and the learning claim is deliberately narrow, but the
product's centre of gravity is content review by Deaf signers, and that dependency was treated as a
one-off favour rather than a contract. A handshape app without a reviewer is a shape-matching demo.

**About the tech.** The desk could not answer any of the questions the entry's description answers with
numbers. Building a probe for every unmeasured claim was the right instinct applied too late: the probes
were written in P7 and P8, after the claims had already been phrased as if measured.

**About the competition.** Submission mechanics were scheduled last, budgeted at 10 h, and are the reason
G9 and G10 are red while the build is green. In a competition judged on a video and an installable
invite, the conversion work is not paperwork after the product — it *is* the deliverable.

## 6. What survived

The artifact is byte-identical to its freeze. The checks are reproducible from the command line. Every
measurement in the docs carries the method that produced it, and every unmeasured claim is marked NOT RUN
in `HUMAN-TASKS.md` rather than smoothed over. That habit — the refusal to let a document be more
confident than the build — is the part worth carrying to the next project.
