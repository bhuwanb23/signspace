import sys, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from _phasegen import T, write_phase

# ─────────────────────────── P0 ───────────────────────────
p0 = dict(id="P0", name="Foundation & compliance", slug="foundation", day="1 (Sat 7 Nov)",
          hours=5, gate_no=0,
          why_here="Every item here is unglamorous and every one is fatal if missing. "
                   "An APK that cannot reach a judge is indistinguishable from no APK at all.")

p0_tasks = [
 T("P0.1","verify-start-badge","Verify Meta Horizon Start membership and eligibility","20 min","nothing","W5",
   "The competition is only open to Start program members. If the badge isn't active on the submitting account, "
   "nothing else in this repo matters — and it is the kind of thing that takes a week to fix if you discover it on Day 10.",
   ["Log in to the Meta developer dashboard with the account that will submit.",
    "Open the Meta Horizon Start program page and confirm active membership (badge/status visible).",
    "Confirm the Devpost account email matches, or is linked to, that developer account.",
    "Record the exact account email, organisation name, and member ID in `phases/p0-foundation/accounts.md`.",
    "If membership is missing or pending: apply immediately and note the expected turnaround."],
   ["Start badge visible and screenshot archived","Submitting account email recorded","If applying: application ID + expected date recorded"],
   ["`phases/p0-foundation/accounts.md` (create if absent)"],
   ["Assuming Devpost and Meta accounts are linked when they aren't","Using a personal account that isn't the program member"]),

 T("P0.2","devpost-draft","Create the Devpost draft entry and save the submission ID","20 min","P0.1","W4",
   "The draft locks the entry record early, makes the form's real field list visible (word limits, required uploads), "
   "and removes all Day-11 uncertainty about where to submit.",
   ["Open the competition on Devpost and click *Enter a submission* → *Save draft*.",
    "Enter the project name **SIGNSPACE** and the current tagline from `docs/06-submission/SUBMISSION-COPY.md`.",
    "Screenshot or copy the **full field list** — every required field, every character limit, every upload slot.",
    "Write that field list into `docs/06-submission/CHECKLIST.md` as the authoritative form map.",
    "Save the submission ID in `phases/p0-foundation/accounts.md`."],
   ["Draft exists and is retrievable","The real field list is documented, not assumed","Submission ID recorded"],
   ["`docs/06-submission/CHECKLIST.md` §1 form map","`phases/p0-foundation/accounts.md`"],
   ["Trusting the rules page instead of the actual form — they differ","Filling the form from memory on Day 11"]),

 T("P0.3","app-and-channel","Create the Horizon OS app and the `Competition` release channel","45 min","P0.1","W5",
   "The submission requires the APK on a release channel literally named `Competition`. Creating the channel empty, "
   "on Day 1, proves the upload path works before there is anything worth uploading.",
   ["Create a new app in the Meta developer dashboard for Quest (Horizon OS), category and rating placeholder.",
    "Set the app's package name/bundle ID and record it — this becomes `Application.identifier` in Unity and cannot be changed later.",
    "Create a release channel named exactly `Competition` (case-sensitive).",
    "Create a second channel `Internal` for day-to-day test builds so `Competition` stays clean.",
    "Upload any throwaway APK to `Competition` to prove the path, then leave it there until P9 replaces it.",
    "Record the app ID, package name, and both channel names."],
   ["`Competition` channel exists and has accepted at least one upload","Package name recorded and matches the Unity setting","`Internal` channel exists separately"],
   ["`phases/p0-foundation/accounts.md` updated","A throwaway APK on the `Competition` channel"],
   ["Renaming the package later — it is effectively permanent","Uploading real builds to `Competition` before the freeze, polluting the version list"]),

 T("P0.4","toolchain","Install the toolchain and get an empty scene onto the headset","2 h","P0.3","W2",
   "The single most common way to lose two days on a VR project is discovering toolchain mismatches late. "
   "Prove the pipeline on an empty scene while your motivation is still high.",
   ["Install Unity 6 LTS — the exact patch listed as supported by Meta XR SDK v207. Record the patch version.",
    "Install Android build support (SDK/NDK/OpenJDK) via Unity Hub.",
    "Create a new 3D (URP) project at `unity/` and set Build Target to Android.",
    "Player settings: IL2CPP, ARM64 only, package name from P0.3, minimum API 32, target API 34, "
    "Graphics APIs = Vulkan then GLES3 (or GLES3 only if Vulkan is unstable — record the choice).",
    "Import **Meta XR All-in-One SDK v207**. Record the exact package versions resolved.",
    "Add the Meta XR rig to the scene, enable hand tracking and passthrough, build, install, run.",
    "Record every version in `docs/02-tech/TECH-STACK.md` §1, replacing the `[VERIFY]` tags."],
   ["A build installs and runs on a Quest 3S","Hands and passthrough are visible in an empty scene","No `[VERIFY]` remains untagged in the tech stack table"],
   ["`unity/` project committed","`docs/02-tech/TECH-STACK.md` version table filled","`phases/p0-foundation/toolchain.md` with exact versions"],
   ["Mixing SDK versions across Core/Interaction/MRUK","Skipping the empty-scene test and importing content first","Not recording the Unity patch — it will bite during an SDK upgrade"]),

 T("P0.5","invite-url","Verify the Invite URL from a clean, signed-out, non-developer account","30 min","P0.3","W5",
   "Judges are not developers. If the invite flow requires a developer account, a sideload, or any special step, "
   "the entry is judged as uninstallable. This is checked on Day 1 and again in P9.7, because it can silently regress.",
   ["Generate the invite URL / release channel link for the app.",
    "Open it on a phone or browser in a **private window, signed out of everything Meta**.",
    "Follow the flow as a non-developer would: sign in, accept, install to a headset.",
    "Record every step of the flow in `phases/p0-foundation/invite-flow.md`, with screenshots.",
    "Note the number of taps and any point where a user could fall out."],
   ["A signed-out stranger can reach the install prompt","The number of required steps is documented","Any blocker (developer mode, sideload) is identified now, not later"],
   ["`phases/p0-foundation/invite-flow.md` with screenshots"],
   ["Testing the URL while signed in as the developer — it always works for you","Assuming an invite link survives a channel re-upload; re-verify at P9.7"]),

 T("P0.6","repo-hygiene","Repo hygiene, baseline commit, and branch policy","30 min","nothing","W5",
   "This repo is the project's memory. On Day 9 at 2 a.m. the difference between shipping and not is whether the "
   "state of the build is legible.",
   ["Confirm `.gitignore` covers Unity `Library/`, `Temp/`, `Build/`, `Logs/`, keystores except debug.",
    "Create `phases/` structure (this folder tree) and commit it.",
    "Adopt the branch policy: `main` always buildable, one branch per phase named `pN-slug`.",
    "Add the two helper scripts to the workflow: `scripts/status.py` and `scripts/count.py`.",
    "Make the baseline commit with a message naming the phase state."],
   ["`git log` shows a baseline commit","`main` builds (or is honestly empty at this point)","Branch policy written down"],
   ["Baseline commit","`docs/05-build/DEFINITION-OF-DONE.md` branch rules"],
   ["Committing `Library/` — it is gigabytes and will destroy clone time","Working directly on `main` from Day 3 onward"]),

 T("P0.7","device-matrix","Lock the device matrix and the test rig","20 min","P0.4","W5",
   "Judges test on Quest 3 **and** 3S. We build against the weaker device so the strong one is free. "
   "Without a named device list, nobody knows what 'it works' means.",
   ["List every physical headset available, with owner and location.",
    "Declare the **primary build device** (Quest 3S — the harder target).",
    "Declare the **verification device** (Quest 3).",
    "Declare the room/lighting conditions that represent the judge's environment (a normal living room at 7 p.m., not a lab).",
    "Note whether any device supports eye tracking — if none does, P6 becomes simulator-dependent and that must be known now."],
   ["Both devices named, or the gap explicitly documented","Lighting conditions written down","Eye-tracking availability known"],
   ["`phases/p0-foundation/devices.md`"],
   ["Only owning a Quest 3 and discovering 3S thermal limits on Day 10","Testing only in daylight"]),

 T("P0.8","risks-and-dod","Sign off the risk register and the Definition of Done","30 min","nothing","W5",
   "The risks and the done-criteria already exist as documents. Signing them off turns them from prose into commitments, "
   "and gives every later gate something objective to test against.",
   ["Read `docs/05-build/RISKS.md` and confirm each risk has an owner and a trigger date.",
    "Read `docs/05-build/DEFINITION-OF-DONE.md` and fill in any `[TBD]` in the per-phase criteria.",
    "Confirm the cut ladder order in `MASTER-PLAN.md` §6 is still the order you'd actually cut in.",
    "Record the sign-off date at the bottom of both documents."],
   ["Both documents signed and dated","Cut ladder explicitly agreed","No risk without an owner"],
   ["Sign-off line in `RISKS.md` and `DEFINITION-OF-DONE.md`"],
   ["Treating the risk register as decoration — it is read at every gate"]),

 T("P0.9","signer-pipeline","Open the signer pipeline today (kill-condition lead time)","60 min","nothing","W3",
   "**This is the only task in P0 that can kill the project.** Capturing named Deaf signers is the mitigation for the "
   "one fatal risk, it depends on another person's calendar, and its lead time is longer than any technical task.",
   ["Write the one-page brief for a prospective signer: what we're building, what we'd ask them to do, time commitment, "
    "compensation, and how they'd be credited.",
    "Draft the consent and rights scope: competition submission, video, and future commercial release.",
    "Identify three routes to a signer: local Deaf association, ASL tutor networks, Deaf-led creator communities.",
    "Send the brief to at least three contacts today.",
    "Put the booking target in the calendar: **session must happen by Day 5 (Wed 11 Nov)**.",
    "If no route responds by the end of Day 2, escalate to the fallback-concepts decision (see `RISKS.md` R1)."],
   ["Brief written and sent to ≥3 contacts","Consent draft ready to sign","Calendar holds the Day-5 capture slot","Escalation date set"],
   ["`docs/03-content/CAPTURE-PROTOCOL.md` §1–2 (brief + consent)","`phases/p0-foundation/signer-pipeline.md` with contact log"],
   ["Treating this as a P5 task because the master plan files it there — the *lead time* is P0","Offering exposure instead of payment","Waiting for a perfect consent template instead of sending the brief"]),
]

p0_links = ("- [`docs/01-idea/CONCEPT.md`](../../docs/01-idea/CONCEPT.md)\n"
            "- [`docs/02-tech/TECH-STACK.md`](../../docs/02-tech/TECH-STACK.md)\n"
            "- [`docs/05-build/RISKS.md`](../../docs/05-build/RISKS.md)")

write_phase(p0, p0_tasks,
  gate_q="Does an empty build install from the `Competition` channel, and does the invite URL work for a stranger?",
  gate_criteria=[
    "Meta Horizon Start membership is active on the submitting account (screenshot archived)",
    "Devpost draft exists; the real form field list is documented in `CHECKLIST.md`",
    "App created; release channel named exactly `Competition` has accepted an upload",
    "A build of the empty scene installs and runs on the primary device (Quest 3S)",
    "Hand tracking and passthrough are visible in that build",
    "The Invite URL reaches an install prompt from a signed-out, non-developer account",
    "Every toolchain version is recorded; no `[VERIFY]` left in the tech stack table",
    "Baseline commit exists; branch policy written",
    "Signer brief sent to ≥3 contacts, with a Day-5 capture slot in the calendar"],
  gate_red="**Stop everything.** Nothing downstream has value if an APK cannot reach a judge. Fix the plumbing, "
           "then re-run the gate. If the signer pipeline has had no response by end of Day 2, open the "
           "fallback-concepts decision the same night (see `RISKS.md` R1).",
  objective="Get from \"nothing\" to \"a stranger can install our build and open our entry form\". "
            "No product work happens in this phase, and no product work may start until this phase's gate is green.",
  inputs=["A Meta developer account with Start membership","The competition's Devpost page","A Quest 3 and/or Quest 3S",
          "~2 h of uninterrupted install time"],
  outputs=["A build on the primary device, installed from the `Competition` channel",
           "A documented, screenshot-backed invite flow","A verified toolchain with every version pinned",
           "A signer brief in three inboxes"],
  traps=["Spending Day 1 on the concept instead of the plumbing — the concept is already decided",
         "Discovering the invite flow only works for developers on Day 10",
         "Leaving the signer pipeline until P5 because that's where it's filed"],
  budget="5 h of the 82 h total. If this overruns by more than 2 h, that time comes out of P7 polish, not out of P1.",
  links=p0_links)

# ─────────────────────────── P1 ───────────────────────────
p1 = dict(id="P1", name="De-risk spike", slug="derisk", day="2 (Sun 8 Nov)",
          hours=8, gate_no=1,
          why_here="Everything the concept assumes is true gets tested here, in one day, on real hardware. "
                   "If the core gesture doesn't feel good, we must know on Day 2 — not Day 8.")

p1_tasks = [
 T("P1.1","hand-visual","Custom hand visual running at frame rate","45 min","P0.4","W2",
   "The stock ISDK hand reads as a demo. Ours is the product's main character — and a custom visual is also the "
   "cheapest way to prove we control the tracking pipeline rather than inheriting whatever the sample did.",
   ["Import a low-poly hand mesh with at least 5 fingers and a palm, skinned to 26 joints.",
    "Wire `OVRSkeleton` (or the OpenXR hand equivalent under the v207 rig) to drive joint transforms each frame.",
    "Assign the mesh to both hands; verify wrist, finger spread and thumb opposition read correctly.",
    "Add a confidence readout from the tracker's hand confidence value, visible only in a debug build.",
    "Measure: triangle count, draw calls, and frame time with both hands tracked."],
   ["Both hands render and track with no visible lag","Joint count and names documented","Frame time impact measured, not estimated"],
   ["`unity/Assets/_Project/Prefabs/HandVisual.prefab`","Joint list added to `docs/02-tech/POSE-FORMAT.md` §1"],
   ["Using the stock hand 'for now' — it becomes the product by accident","Re-skinning 26 joints to a 20-bone rig and silently mangling the fingers"]),

 T("P1.2","grab-interaction","Grab a free-floating object with hands only","45 min","P1.1","W2",
   "Grab is the input for the entire core mechanic. It must work on a hand-held object in mid-air, with no ray and no controller.",
   ["Add an ISDK `HandGrabInteractable` to a test cube plus a `HandGrabInteractor` on each hand.",
    "Configure grab type to pinch, with a generous pinch threshold for reliability.",
    "Disable ray interaction in the scene so nothing can silently fall back.",
    "Test: can you grab, move, and release a floating cube with either hand, 20 times, without a miss?",
    "Log miss rate. If >2 misses in 20, widen the grab threshold before proceeding."],
   ["20/20 successful grabs with each hand","No ray or controller fallback active","Miss rate recorded"],
   ["`phases/p1-derisk/grab-log.md` with the 20-trial result"],
   ["Tuning pinch too tight and blaming it on the tracker","Leaving ray interaction enabled and not noticing the fallback"]),

 T("P1.3","wrist-rotate","Rotate the grabbed object with your own wrist","1.5 h","P1.2","W2",
   "**This is the make-or-break test of the whole concept.** If grabbing a floating hand and turning it with your wrist "
   "feels wrong, the product's core gesture is wrong and we re-plan on Day 2 rather than Day 8.",
   ["Make the grabbed object a proxy for a hand (a simple hand block or the P1.1 mesh).",
    "Attach rotation to wrist quaternion delta, with position held stable near the grab point.",
    "Test three variants and rate each 1–5: (a) direct 1:1 wrist coupling, (b) damped 1:1 with slight lag, "
    "(c) hold-still-to-rotate with the hand held in place.",
    "Try all three at arm's length (~0.4 m) and at lap distance (~0.6 m) — the two distances the product will use.",
    "Write a verdict: which variant, and whether the gesture is good enough to build a product on."],
   ["A chosen rotation variant that feels natural at both distances","A written go/no-go with a reason","Position drift under rotation measured (should be near zero)"],
   ["`phases/p1-derisk/rotation-verdict.md` — the most important page produced on Day 2"],
   ["Testing only with a cube — a hand has a familiar shape and your brain rejects wrong rotation far more harshly",
    "Following through to the next task without writing the verdict down"]),

 T("P1.4","reference-hand-and-pose","Load a posed reference hand from JSON","1 h","P1.1","W2",
   "The product's content is data. Proving a stored pose reproduces faithfully and quickly is what separates a product from a demo.",
   ["Define a minimal pose JSON: 26 joints, relative quaternions against a neutral capture, plus metadata fields.",
    "Write a loader that applies the pose to the reference hand at runtime from `StreamingAssets`.",
    "Hand-author two poses (any two, e.g. a fist and a spread hand) and confirm they reproduce visually.",
    "Measure load time and pose-apply time on device — both should be negligible, but measure.",
    "Draft `docs/02-tech/POSE-FORMAT.md` §1–3 from what actually worked."],
   ["Two poses load from disk and apply correctly","Load time measured","Schema version field present"],
   ["`unity/Assets/_Project/Scripts/Hands/PoseProvider.cs`","`docs/02-tech/POSE-FORMAT.md` §1–3 drafted"],
   ["Baking absolute world quaternions instead of relative — breaks the moment a user has a different hand size"]),

 T("P1.5","trackability-probe","Probe 20 candidate handshapes on real hands, in real rooms","2 h","P1.4","W3",
   "The whole content scope depends on what the sensor can actually see. Guessing here is how projects ship a shape that "
   "never locks — the worst possible demo failure.",
   ["Pick 20 candidate ASL handshapes: the 12 from `SIGNSET.md`, the 5 suspected blind spots (M, N, T, R, S), plus E, X and one wildcard.",
    "In a daylight room, hold each shape for 10 s and record: does it track? does it flicker? does it track *as a different shape*?",
    "Repeat in lamplight only.",
    "Repeat both lighting conditions with your non-dominant hand.",
    "Score each shape: High / Medium / Low / Never, and note the failure mode.",
    "Record the results directly into `docs/03-content/TRACKABILITY.md` §2."],
   ["20 shapes × 2 lighting conditions tested","Failure taxonomy recorded (never / flickers / wrong shape)","A first honest count of trackable shapes"],
   ["`docs/03-content/TRACKABILITY.md` §2 filled","`phases/p1-derisk/probe-raw.md` raw notes"],
   ["Testing in ideal light only — judges don't live in a lab","Judging a shape by one attempt; reliability is about repeatability"]),

 T("P1.6","mruk-surface","Find a horizontal surface and anchor a cube to it","1 h","P0.4","W2",
   "MRUK is a named rubric technology and the shelf depends on it. Detection must be proven before the shelf is designed around it.",
   ["Enable MRUK in the project and initialise it on scene start.",
    "Query the current room for horizontal surfaces at a comfortable height.",
    "Spawn a test cube at the nearest suitable surface, offset slightly above it.",
    "Test in four conditions: a clean desk, a lap, a bed, and a cluttered table.",
    "Record which conditions work and how long detection takes from launch."],
   ["Cube sits on real furniture in at least 2 of 4 conditions","Time-to-detection measured","Failure conditions documented"],
   ["`phases/p1-derisk/mruk-log.md`"],
   ["Assuming detection is instant — it is not, and launch flow must account for it","Requiring a surface; the product must never depend on one"]),

 T("P1.7","persistence","Survive a relaunch: the cube is still there","45 min","P1.6","W2",
   "The shelf's entire value is temporal — it only means something if it's still there tomorrow. Persistence is cheap now and expensive on Day 9.",
   ["Save the anchor reference (room UUID + anchor GUID) plus a local position offset to disk on placement.",
    "On launch, re-resolve the anchor and restore the cube's transform.",
    "Kill the app from the recent-apps menu, relaunch, confirm the cube returns to the same physical spot.",
    "Repeat after a headset reboot.",
    "Record how long re-resolution takes and what happens when the room can't be re-found."],
   ["Cube restores after a kill/relaunch","Cube restores after a headset reboot","Behaviour when the room is not found is documented"],
   ["`unity/Assets/_Project/Scripts/Spatial/ShelfAnchor.cs` (first version)","`phases/p1-derisk/persistence-log.md`"],
   ["Saving a world position instead of an anchored one — it survives the app but not the room"]),

 T("P1.8","perf-baseline","Measure the baseline frame budget on Quest 3S","45 min","P1.1","W2",
   "Forty percent of the remaining plan can be wrecked by a performance surprise on Day 8. Establish the floor now, "
   "when there is still time to change an architectural decision.",
   ["Install OVR Metrics Tool on the device and enable its overlay.",
    "With both hands tracked, one anchored object, and passthrough on, run for 10 minutes.",
    "Record: average fps, 1% low, GPU level, and the time at which thermal throttling starts (if any).",
    "Compare against the budget in `docs/02-tech/TECH-STACK.md` §4 and note the margin.",
    "If below 72 fps, list the top three costs from the Unity profiler immediately."],
   ["A measured fps number on the weaker device, written down","Thermal onset time recorded (or \"none in 10 min\")","Margin against the 72 fps target stated"],
   ["`phases/p1-derisk/perf-baseline.md`","`docs/02-tech/PERFORMANCE.md` §2 first row"],
   ["Measuring in the editor or via the simulator","Measuring for 60 seconds and calling it thermal-stable"]),
]

p1_links = ("- [`docs/02-tech/TECH-STACK.md`](../../docs/02-tech/TECH-STACK.md) §6 (the five unknowns)\n"
            "- [`docs/02-tech/POSE-FORMAT.md`](../../docs/02-tech/POSE-FORMAT.md)\n"
            "- [`docs/03-content/TRACKABILITY.md`](../../docs/03-content/TRACKABILITY.md)\n"
            "- [`docs/03-content/SIGNSET.md`](../../docs/03-content/SIGNSET.md)")

write_phase(p1, p1_tasks,
  gate_q="Does grab-and-rotate feel good on device, and do at least 8 of 20 probed handshapes track reliably?",
  gate_criteria=[
    "A custom hand visual tracks both hands at frame rate",
    "Grab succeeds 20/20 with each hand, with no ray or controller fallback in the scene",
    "A rotation variant is chosen and rated acceptable at both 0.4 m and 0.6 m",
    "Two poses load from JSON and reproduce faithfully",
    "20 handshapes probed in two lighting conditions, results recorded",
    "**≥8 of 20 shapes rated High or Medium in normal light**",
    "MRUK anchors a cube to a real surface in ≥2 of 4 room conditions",
    "The anchor survives a relaunch and a headset reboot",
    "Baseline fps measured on Quest 3S with a thermal run of ≥10 min"],
  gate_red="**P1.3 red (rotation feels wrong):** do not proceed. Test the hold-still-to-rotate variant for one more hour; "
           "if it still fails, the core gesture is wrong and the concept is re-opened the same day.\n\n"
           "**P1.5 red (<8 shapes):** cut the content to 6 shapes / 1 family immediately (`SIGNSET.md` §7 decision rule) "
           "and continue — this is a scope cut, not a concept change.\n\n"
           "**P1.6/P1.7 red:** make the virtual plinth the permanent design and remove all surface-dependence from the pitch.",
  objective="Prove, on real hardware, in one day, the four things the concept depends on: hands render well, grab feels good, "
            "wrist rotation is a viable core gesture, and the sensor can actually resolve enough handshapes to make a product. "
            "Plus the two platform bets — MRUK anchoring and persistence — and a measured frame budget.",
  inputs=["A working empty-scene build on the primary device (P0.4)","A 26-joint hand mesh","Meta XR SDK v207 imported",
          "OVR Metrics Tool installed","Two hours of daylight and one of lamplight"],
  outputs=["A written go/no-go on the core gesture","A measured trackability table for 20 handshapes",
           "A working anchored-and-persistent object","A baseline frame time on the weaker device",
           "The first two real poses in JSON"],
  traps=["Spending the day making it pretty — nothing in P1 survives into the build except the knowledge",
         "Not writing verdicts down: an unwritten verdict is a rumour by Day 4",
         "Testing rotation with a cube instead of a hand",
         "Probing handshapes with one hand in ideal light"],
  budget="8 h. This is the highest value-per-hour day in the project. If it needs 10 h, take them from P7.",
  links=p1_links)

print("P0 + P1 written")
