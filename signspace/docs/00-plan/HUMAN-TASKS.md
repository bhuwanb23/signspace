# Human tasks — everything the build cannot do for you

**Status:** living document · **Owner:** you · **Last updated:** 2026-11-17

> **Read this first.** Every phase from P3 to P7 is implemented, compiled, and desk-checked. The rows
> below are the ones that need a **body, a headset, a Deaf signer, or a stranger** — nothing on this
> page can be completed by a script, and the docs deliberately do **not** claim any of them.
>
> Work them in any order. Each row names its acceptance test and the evidence file the phase docs
> already point at, so finishing a row is "do the thing, save the file, tick the box".

**Build under test:** `unity/Builds/Android/signspace.apk`
· 64,584,347 bytes
· `sha256 6a62ced4b81a9ab4112b07a9a64d5a66a149d2d52ebc49611dec5df1ae84f41f`
· built 2026-11-17 from `main`

---

## 1. Signer and content (P5) — needs a Deaf signer

These block the content claims, not the app. The app runs; the signing is unvalidated without them.

| # | Task | Steps | Done when | Evidence | Status |
|---|---|---|---|---|---|
| H1 | **Recruit and consent a Deaf signer** | send the brief and consent form in `assets/masters/`; agree compensation | a signed consent form on file, credit name chosen | `assets/masters/consent-*.pdf` | ⬜ |
| H2 | **Run the capture session** | follow `assets/masters/capture-protocol.md`: 12 poses, two angles, neutral first | 12 takes per pose, named per the manifest | `assets/raw/p5/<pose>/` | ⬜ |
| H3 | **Signer confirms the 6/6 final set** | show the six shipped shapes; ask which read as intended in isolation | signer's own words on each of the six, in writing | `phases/p5-content/final-set-decision.md` | ⬜ |
| H4 | **Signer approves the credit and the copy** | read `set-meta.json` credit line and the blind-spot copy aloud; change anything that reads wrong | written approval or the corrected wording | `phases/p5-content/session-kit.md` | ⬜ |
| H5 | **Capture the 6 deferred shapes** (optional, for P7) | the six in `Signs/deferred/` | 6 takes; then `python scripts/validate_poses.py` and `check_trackability.py` | `Signs/deferred/` | ⬜ |
| H6 | **Measured trackability** for the 70% cut rule | with the signer, mark which shapes are trackable from the headset's own view | the 70% rule can fire; re-run the final-set decision | `phases/p5-content/final-set-decision.md` | ⬜ |
| H7 | **Approved copy for the blind-spot panel** | `docs/03-content/ACCESSIBILITY.md` §7 and the mirror's line | a Deaf reader says it is not condescending | mirror copy | ⬜ |

## 2. Headset and body (P3, P4, P6) — needs the APK on a Quest 3 / 3S

Install with `adb install -r unity/Builds/Android/signspace.apk`. Record with the headset's capture
(video + mic) and keep the raw file; the phase docs name each expected filename.

| # | Task | Acceptance | Evidence | Status |
|---|---|---|---|---|
| H8 | **P3: near-miss on device** — make 8 deliberate near misses, then 2000 s of ordinary use | 8/8 named correctly; 0 false names | `docs/03-content/FEEDBACK-LOOP.md` log | ⬜ |
| H9 | **P3: honest mirror reachable and legible** | opens by pinch, the excluded shapes read as shapes, no text clipped | `assets/raw/video/mirror.mp4` | ⬜ |
| H10 | **P3: near-miss off switch** | one pinch stops the naming for the session and the block dims | same video | ⬜ |
| H11 | **P4: shelf survives a relaunch** — place on a real table, force-quit, relaunch | 5/5 restores in the same physical place | `docs/04-design/SHELF.md` | ⬜ |
| H12 | **P4: leave and return** — take the headset off, walk away, come back | shelf is where you left it; no re-placement prompt | same | ⬜ |
| H13 | **P4: passthrough comparison** | the two clips are the same scene with one variable | `assets/raw/video/passthrough-*.mp4` | ⬜ |
| H14 | **P6.1: finger precision at 0.4 m and 0.6 m** | each fingertip is individually selectable; note any miss | `assets/raw/video/gaze-precision.mp4` | ⬜ |
| H15 | **P6.2: eyes-still / head-still discriminator** | with eye tracking: eyes move → highlight moves; head moves → highlight does not | `assets/raw/video/gaze-proof.mp4` | ⬜ |
| H16 | **P6.2: fallback line on a Quest 3S** | `gaze: head` appears in the overlay; the one-line explanation appears once | screenshot of the overlay | ⬜ |
| H17 | **P6.3: eyes-only playthrough** | every control reached by dwell: select, label, mirror, credits, all switches, calibrate | `assets/raw/video/eyes-only-playthrough.mp4` | ⬜ |
| H18 | **P6.3: 5 quiet minutes with eyes-only on** | no accidental locks and no accidental switch flips | `docs/00-plan/BUILD-LOG.md` note | ⬜ |
| H19 | **P6.4: restricted-hand calibration** — hold two fingers rigid, calibrate, lock a shape | a shape locks that could not before; the calibration persists after relaunch | `assets/raw/video/calibration-with-nonstandard-hand.mp4` | ⬜ |
| H20 | **P6.5: each switch played through** | the full loop runs with each of the seven on (`phases/p6-accessibility/toggle-matrix.md`) | one clip per switch | ⬜ |
| H21 | **P6.6: reduced-motion loop** | shelf still, hand still, payoff a cross-fade; reference hand does not drift | `assets/raw/video/reduced-motion-loop.mp4` | ⬜ |
| H22 | **P6: one-handed loop** | the whole loop with one hand behind the back | `assets/raw/video/one-handed-loop.mp4` | ⬜ |
| H23 | **First-five-minutes timing** | a fresh profile reaches a first lock in under 75 s | stopwatch note + capture | ⬜ |
| H24 | **Frame rate on the 3S** | sustained ≥72 fps in the loop | `docs/02-tech/PERFORMANCE.md` | ⬜ |

## 3. Polish (P7) — needs the headset, a clock, and ears

`SIGNSPACE/P7 Check all` proves the wiring, the mix ceiling, the text count, the safe-cone geometry and
the hostile-condition answers. None of the rows below can be produced by a check: they are a frame
rate, a stopwatch, twenty launches and a judgement about a sound.

| # | Task | Steps | Done when | Evidence | Status |
|---|---|---|---|---|---|
| H34 | **P7.3: the loop in the VR Glasses profile** | set the simulator to 70°×66° (or the glasses, if they exist); play the full loop; watch the edges, not the centre | no critical element leaves the 55° cone; the reflow triggers and reverses cleanly; gaze still picks a fingertip at the edge | `assets/raw/video/fov-split.mp4` | ⬜ |
| H35 | **P7.4: 15 minutes at the frame rate** | OVR Metrics Tool running; both hands tracked; 12 shelf objects; passthrough on; 20 s warmup excluded | ≥72 fps sustained, 1% low recorded, thermal onset noted; also run one block at MSAA 4× to make the 2× choice evidence | `docs/02-tech/PERFORMANCE.md` §3 | ⬜ |
| H36 | **P7.5: five cold boots** | force-stop, relaunch, five times, one after a reboot; then background/foreground five times mid-session | median `interactive_ms` < 15000 in `boot-times.csv`; 5/5 resumes; shelf and calibration restored | `phases/p7-polish/boot-times.csv` | ⬜ |
| H37 | **P7.6: twenty launches and the hostile conditions** | the script in `crash-sweep.md` ×20; then no surface (MRUK denied), tracking lost 60 s, room changed, device sleep ×2, reboot ×1 | 0 crashes anywhere; every misbehaviour written down | `phases/p7-polish/crash-sweep.md` | ⬜ |
| H38 | **P7.1: the chime, by ear** | play the lock 50 times in a row; then the near-miss 20 times, interleaved | you are not tired of it; the near-miss reads as a question, not a verdict. If not, retune `make_sfx.py` and re-run `P7.1 Check sound` | a note in `SOUND-MAP.md` §9 | ⬜ |
| H39 | **P7.2: cold start to first interaction** | fresh install, stopwatch from tapping the icon to the first frame where the hand responds | under 15 s | a row in `boot-times.csv` | ⬜ |
| H40 | **P7.7: refresh the eight accessibility recordings** | re-record any path whose behaviour changed — high contrast now visibly changes the HUD and the near-miss copy | eight clips exist for the post-polish build | `phases/p6-accessibility/toggle-matrix.md` | ⬜ |

**The sound row is the only one that needs taste rather than a device.** If H38 fails, the fix is a
retune of `scripts/make_sfx.py` plus a re-run of the check — the wiring does not change.

## 4. Strangers (P8) — needs three people who are not you

| # | Task | Acceptance | Evidence | Status |
|---|---|---|---|---|
| H25 | **Recruit three testers with different hands** | three consenting adults, at least one non-standard hand | `phases/p8-playtest/` | ⬜ |
| H26 | **Run the sessions without helping** | nobody touches the app or explains it; you only observe | session notes | ⬜ |
| H27 | **First mastered shape under 75 s** | measured from app start to first lock | `phases/p8-playtest/friction-log.md` | ⬜ |
| H28 | **Friction log, then fix only the top three** | three fixes, each with a before/after note | same | ⬜ |
| H29 | **Re-test with one fresh stranger** | the three fixes hold | same | ⬜ |

## 5. Submission (P9) — needs accounts, a camera and a clean device

| # | Task | Acceptance | Evidence | Status |
|---|---|---|---|---|
| H30 | **Freeze the build** | tag + the sha above; no rebuilds after | `phases/p9-submission/` | ⬜ |
| H31 | **Record the submission video (3 min)** | the beats in `VIDEO-SCRIPT.md`, including the honest eye-tracking line | `assets/raw/video/submission.mp4` | ⬜ |
| H32 | **Clean-device install test** | a second Meta account installs from the invite URL with no help | screen recording | ⬜ |
| H33 | **Submit the form and confirm by email** | confirmation email on file | `phases/p9-submission/` | ⬜ |

## 6. Post-submission (P10) — needs accounts, a video and a calendar

| # | Task | Acceptance | Evidence | Status |
|---|---|---|---|---|
| H41 | **Tag the frozen build** | `git tag v1.0-competition <the commit that produced 81b60582…>` and push; the tag names the APK's source | `git tag -l` shows it; `freeze_check.py` row 5 goes PASS | ⬜ |
| H42 | **Fix the freeze record's commit field** | on a **branch** (not `main`): point `P9Checks.ReadCommitHash()` at the repo root, then fill `commit:` in `apk-hash.txt` from `git log` for the frozen time | record identifies the code; `freeze_check.py` row 6 PASS | ⬜ |
| H43 | **Channel listing** | `Competition` channel lists `1.0.0-competition` (code 13) built from APK `81b60582…`; screenshot saved | `phases/p9-submission/archive/channel-state.png` | ⬜ |
| H44 | **Fill the submission record** | submission ID, confirmation email, invite URL in `submitted.md` | `freeze_check.py` rows 10–12 PASS | ⬜ |
| H45 | **Video master** | the captured entry video in `archive/video-master/` (or its URL in `submitted.md`) | `freeze_check.py` row 13 PASS | ⬜ |
| H46 | **Clean-account invite test** | a second Meta account, no developer login, installs from the invite URL and launches | screen recording; `freeze_check.py` row 14 PASS | ⬜ |
| H47 | **Three weekly re-verification rows** | run `freeze_check.py` on 25 Nov, 2 Dec and 9 Dec and append a row each time | `reverify-log.md` rows 2–4 | ⬜ |
| H48 | **Reconcile the shape count before upload** | the description says 6 shapes; the build ships 20. Correct it **only if the entry is not yet submitted** | `SUBMISSION-COPY.md` §3 and `Probe/index.json` agree | ⬜ |

**H41 and H44 are the two that make the entry exist at all.** Until they are done, `freeze_check.py`
reports the artifact intact and the submission absent — which is exactly what row 1 of
`reverify-log.md` says.

## 7. How to verify a row before ticking it

| If the row is… | Run this first |
|---|---|
| content / signer | `python scripts/validate_poses.py` and `python scripts/check_trackability.py` |
| gaze / accessibility | `SIGNSPACE/P6 Check all` in Unity (106 assertions) |
| polish (sound, text, FoV, perf posture, boot probe, crash posture) | `SIGNSPACE/P7 Check all` (131 assertions) |
| shelf / persistence | `SIGNSPACE/P4.3 Persistence checks` and `SIGNSPACE/Shelf check` |
| credits / copy | `SIGNSPACE/P5.5 Check credits` and `SIGNSPACE/P5.7 Check blind-spot copy` |
| post-submission / freeze | `python scripts/freeze_check.py` (16 rows; account rows stay NOT RUN) |
| any code change at all | `SIGNSPACE/Build main scene`, then the checks above, then rebuild the APK and update the sha here — **but not after the freeze**: a rebuild trades a verified artifact for an unverified one |

**A row is only done when the evidence file exists.** Marking a row done without it would put this
repository back in the state this document exists to prevent: a doc that describes an intention.
