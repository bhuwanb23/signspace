import sys, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from _phasegen import T, write_phase

p7 = dict(id="P7", name="Polish", slug="polish", day="8 (Sat 14 Nov)", hours=8, gate_no=7,
          why_here="Polish is 25% of the score and it is the cheapest 25% in the competition. This phase exists because the "
                   "difference between a good idea and a winning entry is almost entirely here.")

p7_tasks = [
 T("P7.1","sound","Sound design: every state change has a sound","2 h","P3.5","W1",
   "Sound is a scored sub-criterion and the cheapest emotional upgrade in the build. It is also how we replace text without "
   "adding words.",
   ["Build the event→sound map from `SOUND-MAP.md` and fill any gaps found.",
    "Design the signature lock chime — it plays hundreds of times, so it must be short, warm, and non-fatiguing.",
    "Give the near-miss a distinct, gentler sound — informational, not corrective.",
    "Spatialise the shelf and reference-hand sounds; keep UI sounds head-locked.",
    "Mix: nothing above −6 dBFS peak, no music bed, silence is a deliberate choice in three states."],
   ["Every state in `UX-STATES.md` has a sound or a documented silence","Lock chime tested for fatigue over 50 plays","Mix levels recorded"],
   ["`assets/audio/` populated","`docs/04-design/SOUND-MAP.md` (complete)"],
   ["Adding ambient music — it dates the product and competes with the chime"]),

 T("P7.2","zero-text","The zero-text onboarding pass","2 h","P4.6","W1",
   "Best First Five Minutes is winnable on design alone, and every sentence on screen is a failure of spatial teaching. "
   "The target is ≤3 words in the first five minutes.",
   ["Count every string visible in the first five minutes.",
    "For each one, ask: can a spatial cue replace it? (glow, pulse, motion toward the target, a ghost hand.)",
    "Replace, then re-count. Target ≤3 words.",
    "Ensure the three survivors are the credit line, the 'not a replacement' line, and one near-miss template.",
    "Record the cold-start-to-first-interaction time: target <15 s."],
   ["≤3 words of text in the first five minutes","Cold start to first interaction <15 s","Count documented"],
   ["`phases/p7-polish/text-budget.md`","`assets/raw/video/onboarding-take.mp4`"],
   ["Replacing words with icons that need a legend — that's just words with extra steps"]),

 T("P7.3","fov-verify","Verify the FoV reflow in the VR Glasses profile","45 min","P4.6","W2",
   "FoV-awareness is a named criterion and it is demonstrated, not asserted. The split-screen shot is 8 seconds of the video.",
   ["Run the full loop in the VR Glasses profile (70°×66°).",
    "Confirm no critical element leaves the ~55° safe cone at any point in the loop.",
    "Confirm the reflow triggers at the right threshold and reverses cleanly.",
    "Capture the Quest-vs-Glasses split-screen for the video."],
   ["Full loop playable at the narrow profile","Safe cone respected throughout","Split-screen captured"],
   ["`assets/raw/video/fov-split.mp4`","`docs/02-tech/MRUK-SHELF.md` §6 verified"],
   ["Testing only the geometry, not the interaction — gaze targets behave differently at the edges"]),

 T("P7.4","perf-harden","Performance hardening on Quest 3S","1.5 h","P1.8","W2",
   "60 fps is the rubric floor; 72 is ours. This is where the budget in the tech-stack document becomes a measurement with "
   "a recording attached.",
   ["Enable fixed foveated rendering (medium); test MSAA 4× vs 2× and keep what holds 72.",
    "Audit materials and textures: no realtime shadows, zero realtime lights, textures ≤512 except one hero.",
    "Run a 15-minute thermal test with both hands tracked, 12 shelf objects present, and passthrough on.",
    "Record the OVR Metrics overlay for the whole run.",
    "Fill the measured table in `docs/02-tech/PERFORMANCE.md` §2."],
   ["≥72 fps sustained for 15 min on Quest 3S","Thermal curve recorded","Overlay recording captured for the video"],
   ["`docs/02-tech/PERFORMANCE.md` (measured)","`assets/raw/video/perf-overlay.mp4`"],
   ["Measuring the first 60 seconds — thermal throttling is a minutes-scale phenomenon"]),

 T("P7.5","cold-boot","Cold boot and stability sweep","45 min","P7.4","W2",
   "A judge launches the app once, cold, possibly impatient. The first ten seconds decide whether they see the product or a "
   "loading screen.",
   ["Measure cold boot to first interactive frame, five times. Median <15 s.",
    "Remove or shorten any splash; no logo animation longer than 1 s.",
    "Background and foreground the app mid-session ×5; confirm the loop resumes.",
    "Confirm the shelf and calibration restore correctly after each backgrounding."],
   ["Median cold boot <15 s","5/5 background/foreground cycles resume cleanly","No splash over 1 s"],
   ["`phases/p7-polish/boot-times.csv`"],
   ["First-launch MRUK detection blocking the whole app — the loop must be playable before the shelf resolves"]),

 T("P7.6","crash-sweep","Crash sweep","1 h","P7.5","W2",
   "A single crash in front of a judge outweighs every good decision in the build. This is the cheapest insurance available.",
   ["20 cold launches, back to back, with a script of actions each time.",
    "5 mid-session backgroundings, 2 device sleeps, 1 reboot.",
    "Try the four hostile conditions: no surface, no eye tracking, tracking lost entirely, room changed.",
    "Fix everything that crashes; document anything that merely misbehaves.",
    "Record the results as a table."],
   ["0 crashes over 20 launches","0 crashes across the hostile conditions","Misbehaviours documented"],
   ["`phases/p7-polish/crash-sweep.md`"],
   ["Only testing the happy path — judges are not gentle"]),

 T("P7.7","a11y-regression","Accessibility regression pass","45 min","P6.5","W1",
   "Seven days of changes are the natural enemy of a working checkbox. Re-run every modality path after the polish pass.",
   ["Play the full loop once per toggle, plus one-handed, eyes-only, reduced-motion and calibrated paths.",
    "Confirm each still completes and each still persists its setting.",
    "Re-record any path whose behaviour changed."],
   ["All eight paths pass post-polish","Recordings refreshed where behaviour changed"],
   ["`phases/p6-accessibility/toggle-matrix.md` updated"],
   ["Assuming polish didn't touch accessibility — it touched everything"]),
]

p7_links = ("- [`docs/04-design/FIRST-FIVE-MINUTES.md`](../../docs/04-design/FIRST-FIVE-MINUTES.md)\n"
            "- [`docs/04-design/SOUND-MAP.md`](../../docs/04-design/SOUND-MAP.md)\n"
            "- [`docs/02-tech/PERFORMANCE.md`](../../docs/02-tech/PERFORMANCE.md)\n"
            "- [`docs/03-content/ACCESSIBILITY.md`](../../docs/03-content/ACCESSIBILITY.md)")

write_phase(p7, p7_tasks,
  gate_q="Does someone who has never touched a headset get from launch to their first mastered shape without you saying a word?",
  gate_criteria=[
    "Every state change has a sound or a documented silence",
    "≤3 words of text in the first five minutes; cold start to first interaction <15 s",
    "Full loop playable at the 70°×66° profile with nothing outside the safe cone",
    "≥72 fps sustained for 15 minutes on Quest 3S, recorded",
    "Median cold boot <15 s; 5/5 background cycles clean",
    "0 crashes across 20 launches and 4 hostile conditions",
    "All eight accessibility paths pass after the polish pass"],
  gate_red="If G7 is red, Day 9 becomes a design day, not a test day: fix the first-five-minutes problem before testing with "
           "strangers, because a friction log taken on a broken onboarding teaches you nothing you don't already know.",
  objective="Turn a working build into a finished one: sound, zero-text onboarding, verified FoV, measured performance, and a "
            "build that cannot crash in front of a judge.",
  inputs=["All prior phases green","Sound assets","OVR Metrics Tool","XR Simulator VR Glasses profile"],
  outputs=["A polished, measured, crash-free build","The performance overlay, FoV split and onboarding recordings",
           "A text budget under three words"],
  traps=["Polishing visuals before performance — on a 3S that order is backwards",
         "Adding 'just one' new feature; the freeze is at Day 11 and scope only moves down"],
  budget="8 h. This is the phase that absorbs overruns from everywhere else, which means it will overrun. Protect P7.4 and P7.6 above all.",
  links=p7_links)

p8 = dict(id="P8", name="Playtest & iterate", slug="playtest", day="9 (Sun 15 Nov)", hours=6, gate_no=8,
          why_here="Every assumption about how the first five minutes land is untested until someone who isn't us touches it. "
                   "One day of watching strangers is worth more than any further internal polish.")

p8_tasks = [
 T("P8.1","recruit-testers","Recruit three testers with different hands","45 min","P7","W1",
   "Three testers with different hand sizes catch most of what a judge will experience. One with limited dexterity catches "
   "things no other tester can.",
   ["Recruit: one small-handed, one large-handed, one with limited dexterity or a hand difference if findable.",
    "None of them may have seen the app or read anything about it.",
    "Brief them with exactly one sentence: *'Put this on and see what you think.'* Nothing else.",
    "Get consent to record hands, audio and the passthrough view."],
   ["Three testers confirmed, none briefed","Recording consent obtained"],
   ["`phases/p8-playtest/testers.md` (first names or initials only)"],
   ["Testing on teammates — they know what it's supposed to do and will unconsciously complete it"]),

 T("P8.2","run-sessions","Run the sessions without helping","2 h","P8.1","W1",
   "The value of a playtest is proportional to how quiet the tester is left. Every intervention destroys a data point.",
   ["For each tester: cold launch, then hands off. Say nothing except to prevent injury or confusion about hardware.",
    "Record: passthrough view, hand footage, and audio.",
    "Note the time to first grab, first lock, first near-miss, and the point they stop moving.",
    "If they stop for more than 10 seconds, note it as friction and still say nothing.",
    "Afterwards, ask three questions: what did you think it was? what did it tell you when you were wrong? would you come back?"],
   ["Three recorded sessions","Time-to-first-lock measured for each","The answers to the three questions captured"],
   ["`assets/raw/playtest/*.mp4`","`phases/p8-playtest/notes-<date>.md`"],
   ["Explaining 'you have to pinch it' — that sentence is the bug you're looking for"]),

 T("P8.3","friction-log","Write the friction log","1 h","P8.2","W1",
   "A friction log is only useful if it is uncomfortable. Ten rows minimum, and the worst ones first.",
   ["List every moment a tester stopped moving or made a wrong assumption.",
    "For each: what they did, what they expected, what the app showed.",
    "Rank by how much damage it does to a judge in the first 90 seconds.",
    "Cross-check against `FIRST-FIVE-MINUTES.md` — if the design doc predicted the friction, that's a design-doc failure worth noting."],
   ["≥10 rows, ranked","Each row has behaviour + expectation + app response","Fixes scoped to the top three"],
   ["`docs/05-build/PLAYTEST-LOG.md` (complete)"],
   ["Softening the log — it is the only honest measure in the project"]),

 T("P8.4","fix-top-three","Fix the top three, and only the top three","2 h","P8.3","W1",
   "Fixing three things properly beats fixing ten things partially, and Day 10 is a submission day, not a build day.",
   ["Fix the top three friction points, smallest viable change each.",
    "No refactors. No new systems. No new assets unless the friction is an asset problem.",
    "Re-run the exact scenario that produced each friction point.",
    "If a fix introduces a new problem, revert it — a known friction is better than an unknown defect."],
   ["Three fixes shipped and verified","No new defect introduced","Nothing else touched"],
   ["`phases/p8-playtest/fixes.md`"],
   ["Fixing five because they were quick — that's how a build breaks on Day 10"]),

 T("P8.5","retest","Re-test with one fresh stranger","45 min","P8.4","W1",
   "The only measure that matters: does the first lock now happen inside 75 seconds, uncoached?",
   ["Run one fresh tester, uncoached, cold launch.",
    "Measure time to first lock and time to first near-miss.",
    "Confirm nothing regressed in accessibility paths or performance.",
    "Record the final number in `PLAYTEST-LOG.md`."],
   ["First lock <75 s","First near-miss experienced organically (or a note on why not)","No regression"],
   ["`docs/05-build/PLAYTEST-LOG.md` §5"],
   ["Running the re-test yourself — you cannot be a stranger"]),
]

p8_links = ("- [`docs/05-build/PLAYTEST-LOG.md`](../../docs/05-build/PLAYTEST-LOG.md)\n"
            "- [`docs/04-design/FIRST-FIVE-MINUTES.md`](../../docs/04-design/FIRST-FIVE-MINUTES.md)\n"
            "- [`docs/05-build/DEFINITION-OF-DONE.md`](../../docs/05-build/DEFINITION-OF-DONE.md)")

write_phase(p8, p8_tasks,
  gate_q="Does a stranger reach their first mastered shape in under 75 seconds, with nobody helping?",
  gate_criteria=[
    "Three uncoached testers recorded",
    "Friction log has ≥10 ranked rows",
    "Top three friction points fixed",
    "First lock now <75 s with a fresh stranger",
    "No accessibility or performance regression",
    "**The near-miss was experienced organically, not demonstrated**"],
  gate_red="If the re-test still misses 75 s, ship anyway with the honest friction log — the log itself is evidence of rigour, "
           "and a late redesign on Day 9 does more damage than a slow first minute. Note the gap in the post-mortem, not in the "
           "submission.",
  objective="Watch three strangers use it, write down everything that goes wrong, and fix only the three worst things.",
  inputs=["A polished build (P7 green)","Three uncoached testers","Recording setup"],
  outputs=["A friction log with real behavioural evidence","Three fixes","A measured time-to-first-lock"],
  traps=["Helping","Testing on friends who have seen the video","Fixing more than three things"],
  budget="6 h. Never extend: Day 10 is submission, and it cannot absorb a slip.",
  links=p8_links)

p9 = dict(id="P9", name="Submission", slug="submission", day="10–11 (Mon 16 – Tue 17 Nov)", hours=10, gate_no=9,
          why_here="Three minutes of video and five form fields decide the return on eleven days. This phase is treated as a "
                   "deliverable in its own right, with its own budget, not as something that happens after the build.")

p9_tasks = [
 T("P9.1","build-freeze","Freeze the build","1 h","P8","W5",
   "The last commit before submission is the most dangerous object in the project. Freezing makes it deliberate.",
   ["Bump the version to `1.0.0-competition`.",
    "Confirm keystore is correct and stored somewhere recoverable; confirm IL2CPP + ARM64.",
    "Build a release APK; record its SHA-256 hash and size.",
    "Install that exact APK on both devices and run the full loop once.",
    "Tag the commit `v1.0-competition` and push."],
   ["APK hash recorded","Installs and plays on both devices","Commit tagged"],
   ["`phases/p9-submission/BUILD.md` with the hash","git tag `v1.0-competition`"],
   ["Rebuilding after this point — if you must, you re-run P9.1 in full"]),

 T("P9.2","upload-channel","Upload to the `Competition` release channel","1 h","P9.1","W5",
   "The submission is judged on what is on that channel, not on what is on your laptop.",
   ["Upload the frozen APK to the `Competition` channel with a complete store listing minimum (icon, short description, screenshots placeholder).",
    "Fill every mandatory metadata field required for the channel to publish.",
    "Submit for review if the channel requires it; note the expected review time and whether the competition accepts builds in review.",
    "Verify the version string visible to an installer matches `1.0.0-competition`.",
    "Screenshot the channel state."],
   ["Frozen APK live on `Competition`","Version string verified","Review state known and recorded"],
   ["`phases/p9-submission/channel-state.png`"],
   ["Discovering a mandatory field on the last day — this is why P0.3 created the channel early"]),

 T("P9.3","record-footage","Record all footage against the storyboard","3 h","P9.2","W4",
   "Everything the judge sees was captured today. Real-time, on device, no compositing lies.",
   ["Capture at the target resolution and frame rate, with the metrics overlay available for the perf shot.",
    "Shoot the hook: a hand making a shape, then the near-miss line — captured live, not staged in text.",
    "Shoot the flat-video-vs-rotatable-reference comparison.",
    "Shoot: grab → rotate → gaze highlight → match → lock, unbroken.",
    "Shoot: shelf with passthrough on/off; FoV split; accessibility paths; the signer capture and credit; the perf overlay.",
    "Log every take in a shot list with timecodes and a keep/reject decision."],
   ["Every shot in `VIDEO-SCRIPT.md` has a usable take","The unbroken real-time shot is genuinely unbroken","Perf overlay take shows ≥72 fps"],
   ["`assets/raw/video/` complete","`phases/p9-submission/shot-list.csv`"],
   ["Capturing the hook in post — a judge can tell, and it undermines every other claim"]),

 T("P9.4","edit-video","Edit to under three minutes","2.5 h","P9.3","W4",
   "The edit is where 25% of the score is lost or won. Hands-first by 0:30 is a hard requirement, not a preference.",
   ["Assemble to the storyboard timecodes. Cut anything that doesn't evidence a rubric claim.",
    "Verify: duration <3:00, hands-first visible by 0:30, no fake UI, no motion smoothing that hides frame rate.",
    "Add captions or on-screen text; avoid narration if the visuals carry it.",
    "Design the thumbnail and first frame — the first frame is the near-miss line.",
    "Export at the platform's recommended settings; check file size against Devpost limits."],
   ["Final cut <3:00","Hands-first by 0:30","Plays correctly in an incognito browser"],
   ["`assets/video/SIGNSPACE-submission-final.mp4`","`phases/p9-submission/edit-notes.md`"],
   ["Letting the edit creep past 3:00 — cut the second-best shot, not the evidence"]),

 T("P9.5","copy-verify","Verify every string against its limit","45 min","P9.4","W4",
   "Character limits are checked by script, not by eye. A truncated tagline is a self-inflicted wound on the first thing a "
   "judge reads.",
   ["Run `python3 scripts/count.py` and confirm the tagline ≤140 and description ≤500.",
    "Re-read the description against `COMPETITION-FIT.md` — remove any claim not evidenced in the build or video.",
    "Confirm the anti-claim paragraph is present and still accurate.",
    "Confirm the tagline's promise matches the video's first 10 seconds."],
   ["Both limits pass by script","Every claim evidenced","Anti-claim present and accurate"],
   ["`docs/06-submission/SUBMISSION-COPY.md` final","`phases/p9-submission/count-output.txt`"],
   ["Editing the description after running the count and not re-running it"]),

 T("P9.6","submit","Submit the entry","1 h","P9.5","W4",
   "Submit on Day 11, not Day 12. The buffer day exists for verification, not for building.",
   ["Open the Devpost draft from P0.2 and fill every field from the repository, not from memory.",
    "Upload the video, the screenshots, the thumbnail, the invite URL and the APK reference.",
    "Re-read the rules page for any change since P0.2 and confirm compliance.",
    "Click submit, then archive the confirmation email and a screenshot of the submitted state.",
    "Record the submission ID and timestamp."],
   ["Entry submitted and confirmed by email","Submission ID recorded","Screenshot of the submitted state archived"],
   ["`phases/p9-submission/submitted.md`","confirmation email archived"],
   ["Editing fields after submission without re-verifying the limits"]),

 T("P9.7","clean-verify","Verify the invite URL from a clean account, again","30 min","P9.6","W5",
   "The build changed since Day 1. The install path must be re-proven against the final APK, on a device that has never had "
   "the app.",
   ["On a device that has never installed the app, in a private window signed out of Meta: open the invite URL.",
    "Complete the flow: sign in, accept, install, launch.",
    "Play to first lock on that device.",
    "Note the number of taps from link to first lock, and whether any step requires developer mode."],
   ["A cold device reaches a playable build","Tap count documented","Zero developer steps required"],
   ["`phases/p0-foundation/invite-flow.md` updated with the final state"],
   ["Reusing your own signed-in browser — it will always work for you"]),

 T("P9.8","archive","Archive everything","30 min","P9.7","W5",
   "Judging runs to 11 December. Anything that is lost before then cannot be recovered, and anything that changes after then "
   "cannot be defended.",
   ["Tag and push `v1.0-competition`; archive the APK and its hash.",
    "Archive the video master and the final cut separately.",
    "Archive the raw footage and the playtest recordings.",
    "Write a one-page index of where everything is.",
    "Freeze the repository: no further commits to `main` until judging closes."],
   ["Everything archived and indexed","Repository frozen","Tag on the remote"],
   ["`phases/p9-submission/ARCHIVE.md`"],
   ["Continuing to 'improve' the build after submission — the entry is the submitted build"]),
]

p9_links = ("- [`docs/06-submission/SUBMISSION-COPY.md`](../../docs/06-submission/SUBMISSION-COPY.md)\n"
            "- [`docs/06-submission/VIDEO-SCRIPT.md`](../../docs/06-submission/VIDEO-SCRIPT.md)\n"
            "- [`docs/06-submission/CHECKLIST.md`](../../docs/06-submission/CHECKLIST.md)\n"
            "- [`docs/01-idea/COMPETITION-FIT.md`](../../docs/01-idea/COMPETITION-FIT.md)")

write_phase(p9, p9_tasks,
  gate_q="Is the entry submitted, confirmed by email, with an invite URL that works from a stranger's clean device?",
  gate_criteria=[
    "APK frozen, hashed, tagged `v1.0-competition`",
    "Live on the `Competition` channel with the version string verified",
    "Video <3:00, hands-first by 0:30, plays in incognito",
    "Tagline ≤140 and description ≤500, verified by script after the final edit",
    "Every claim in the copy evidenced in the build or the video",
    "Entry submitted with email confirmation",
    "Invite URL verified from a clean, signed-out, non-developer device",
    "Everything archived and indexed; repository frozen"],
  gate_red="**Submit anyway.** A partial entry with a working install path beats a perfect entry that never leaves the draft. "
           "If the invite URL fails, submit with the failure documented and fix it the same night — the channel can be updated "
           "while the form cannot be submitted twice.",
  objective="Convert eleven days of work into a submitted, verified, installable entry — video, copy, build, and a clean-account "
            "install path that a judge can follow without help.",
  inputs=["A build that passed G8","All footage","The form map from P0.2","The frozen copy"],
  outputs=["A submitted entry","A verified invite URL","A complete archive","A frozen repository"],
  traps=["Submitting on Day 12","Re-recording 'one better shot' after freezing the build",
         "Verifying the install path while signed in as the developer"],
  budget="10 h across two days. The second day is verification only — if you are building on Day 11, the plan already failed somewhere earlier.",
  links=p9_links)

p10 = dict(id="P10", name="Post-submission", slug="post", day="Wed 18 Nov → 11 Dec", hours=4, gate_no=10,
          why_here="Judging runs for three weeks after submission. What happens in those weeks can still be lost — a changed "
                   "channel, a broken invite link, a judge who asks a question nobody prepared for.")

p10_tasks = [
 T("P10.1","reverify","Morning re-verification","15 min","P9.8","W5",
   "The single most avoidable way to lose: the entry is fine on Tuesday night and broken on Wednesday morning.",
   ["Open the invite URL from a clean account.", "Confirm the `Competition` channel still lists the frozen version.",
    "Confirm the video still plays publicly.", "Confirm the description and tagline are unchanged.",
    "Log the check with a timestamp."],
   ["All four checks pass","Logged"],
   ["`phases/p10-post/reverify-log.md`"],
   ["Assuming nothing can change overnight — channel reviews can reject a build"]),

 T("P10.2","judging-prep","Judging-prep note: the three sentences and the two hard questions","45 min","P9.8","W4",
   "Judges and judge-advocates will read the entry quickly. A one-page note keeps the argument intact in every retelling.",
   ["Write the one-pager: the three sentences, the rubric mapping, and the award targets.",
    "Anticipate two questions: *'isn't this just Language IRL for sign language?'* and *'can you actually teach ASL in 12 shapes?'*",
    "Write the honest answers, with evidence references.",
    "Keep it to a single page."],
   ["One-pager written","Both questions answered with evidence"],
   ["`phases/p10-post/judging-note.md`"],
   ["Preparing a long document nobody will read"]),

 T("P10.3","launch-sketch","Store-launch sketch for Q1 2027","1 h","P9.8","W4",
   "The video promises 60 signs at launch in Q1 2027. A one-page plan makes that promise a roadmap instead of a flourish.",
   ["Define what '60 signs' means (which families, which languages).",
    "List what Store submission requires that the competition channel does not.",
    "Define the next content milestone and the partner needed to reach it.",
    "Estimate the work between the competition build and a launch build."],
   ["One-page plan","Milestones dated","Dependencies named"],
   ["`phases/p10-post/launch-sketch.md`"],
   ["Making promises that require a partner we haven't approached"]),

 T("P10.4","freeze-guard","Guard the freeze","5 min/week","P9.8","W5",
   "Three weeks is long enough for an idle 'quick fix' to break the submitted build. The freeze is a commitment.",
   ["No commits to `main` until judging closes.",
    "Any necessary fix goes to a branch and is only merged if the channel is actually broken.",
    "Re-verify P10.1 weekly on the same day."],
   ["Branch untouched","Weekly verification logged"],
   ["`phases/p10-post/reverify-log.md` continued"],
   ["Refactoring in the quiet weeks"]),

 T("P10.5","postmortem","Post-mortem","1 h","10 Dec","W5",
   "The eleven-day process is the reusable asset, not just the app. Write it down while it still hurts.",
   ["What did the 11 days actually cost in hours per phase, against the estimate?",
    "Which gate was closest to red, and what did it teach?",
    "What would we do with 30 days instead of 11?",
    "What did we get wrong about the idea, the tech, or the competition?"],
   ["Post-mortem written with actual hour counts","Three carry-forward lessons"],
   ["`phases/p10-post/POSTMORTEM.md`"],
   ["Writing it in generalities — the hour counts are the value"]),
]

p10_links = ("- [`docs/00-plan/MASTER-PLAN.md`](../../docs/00-plan/MASTER-PLAN.md) §8\n"
             "- [`phases/p9-submission/ARCHIVE.md`](../p9-submission/ARCHIVE.md)")

write_phase(p10, p10_tasks,
  gate_q="Is the submitted entry still intact, installable and unchanged three weeks later?",
  gate_criteria=[
    "Morning-after verification passed",
    "Weekly re-verification logged until judging closes",
    "No commits landed on `main` after the freeze",
    "Judging note ready","Launch sketch written",
    "Post-mortem written with real hour counts"],
  gate_red="There is no red here — only loss. If the channel breaks, fix it on a branch, restore the frozen version string, and "
           "record what happened.",
  objective="Protect the submission for three weeks, prepare the one-page argument judges will re-tell, and capture what the "
            "eleven days actually taught.",
  inputs=["A submitted, verified entry (G9 green)"],
  outputs=["A verified, unchanged entry at judging time","A judging note","A launch sketch","A post-mortem"],
  traps=["Improving things","Assuming the channel is stable because it was on Tuesday"],
  budget="4 h spread across three weeks.",
  links=p10_links)
print("P7 + P8 + P9 + P10 written")
