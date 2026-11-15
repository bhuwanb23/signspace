import sys, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from _phasegen import T, write_phase

p4 = dict(id="P4", name="Spatial shelf", slug="shelf", day="5 (Wed 11 Nov)", hours=8, gate_no=4,
          why_here="The shelf is the only part of the product that survives the session, which makes it the argument for "
                   "coming back — and the only place MRUK and passthrough do real work rather than decorative work.")

p4_tasks = [
 T("P4.1","shelf-anchor","Anchor the shelf to a real surface","2 h","P1.6","W2",
   "The product's promise is that your room accumulates your progress. That promise is one anchor and one layout rule.",
   ["Query MRUK for horizontal surfaces within a 0.3–1.0 m band and 60° of forward.",
    "Score candidates by: proximity to the user, height (waist-level preferred), and free area for a 0.45 m shelf.",
    "Place the shelf at the chosen anchor, rotated to face the user, with a 2 cm clearance above the surface.",
    "If no surface qualifies, go straight to the plinth path (P4.4) — never block.",
    "Log which condition was chosen for every launch (useful for P8 and for the video)."],
   ["Shelf lands on a real surface and faces the user","Selection logic logged","No-surface case routes to the plinth without delay"],
   ["`unity/Assets/_Project/Scripts/Spatial/ShelfAnchor.cs`","`docs/02-tech/MRUK-SHELF.md` §1–3"],
   ["Placing the shelf wherever the anchor origin is — MRUK anchors have meaningful orientation, use it"]),

 T("P4.2","objects","Mastered shapes become shelf objects","2 h","P4.1","W1",
   "The shelf is a progress bar you can touch. If the objects don't read as *the shape you learned*, it's just a counter.",
   ["Design one placeholder object per shape family (12 distinct silhouettes are not needed on Day 5).",
    "On lock (P2.5), spawn the object at the hand and fly it to its shelf slot on a 500 ms arc.",
    "Give each object a resting state: a slow, subtle idle (breathing) that reads as 'alive' but costs no CPU.",
    "Add the empty-slot affordance: the next slot is visible and dimmed from the first launch.",
    "Confirm 12 objects + 12 slots stays inside the draw-call and triangle budget."],
   ["Objects spawn and settle without overlap","Empty slot visible from launch","Budget still met with 12 objects present"],
   ["`unity/Assets/_Project/Prefabs/ShelfObject.prefab`","`phases/p4-shelf/budget-check.md`"],
   ["Animating 12 objects continuously — idle at <1% CPU or drop the idle"]),

 T("P4.3","persistence","Persistence across relaunch, reboot and room","1.5 h","P4.2","W2",
   "If the shelf doesn't survive, the entire retention argument in the submission is false. This is the cheapest feature "
   "with the highest claim attached to it.",
   ["Persist: room UUID, anchor GUID, per-shape unlock timestamps, layout variant, and a schema version.",
    "On launch: re-resolve the room, then the anchor, then restore objects in unlock order.",
    "Handle the three degraded cases explicitly: same room / different room / room not found.",
    "Test: kill and relaunch ×5, headset reboot ×1, walk to another room ×1, return ×1.",
    "Record the full matrix in `phases/p4-shelf/persistence-matrix.md`."],
   ["Shelf restores in the same room across 5 relaunches and 1 reboot","Different-room behaviour is defined and non-destructive","Schema version stored"],
   ["`unity/Assets/_Project/Scripts/Core/SaveSystem.cs`","`phases/p4-shelf/persistence-matrix.md`"],
   ["Saving the anchor as a world transform — works until the user moves a chair"]),

 T("P4.4","plinth-fallback","The plinth: never show an error, never depend on a surface","1 h","P4.1","W2",
   "Many judges will test sitting on a sofa, on a bed, or on a plane tray. A product that requires a desk fails the "
   "airplane-seat test for reasons that have nothing to do with the idea.",
   ["Build a virtual plinth that reads as a physical object: it has a shadow-catching gradient, a slight tilt, and it "
    "sits at the same visual height as a real surface would.",
    "Route to it silently when MRUK finds nothing, with no message.",
    "If a surface appears mid-session, offer a single quiet affordance to move to it — never auto-migrate.",
    "Test in a room with the lights off and nothing detectable."],
   ["Plinth appears instantly when no surface exists","No error string, no spinner, no timeout","Migration is user-initiated only"],
   ["`unity/Assets/_Project/Prefabs/Plinth.prefab`","`docs/02-tech/MRUK-SHELF.md` §5"],
   ["Auto-migrating when a surface appears — it teleports the user's progress in front of them"]),

 T("P4.5","passthrough-toggle","Passthrough on/off comparison, built for the video","45 min","P4.1","W4",
   "The rubric asks whether the experience would play the same with passthrough off. We build the comparison rather than "
   "arguing it, because a judge can watch it in three seconds.",
   ["Add a debug-only toggle (not shipped in the default build) that flips passthrough.",
    "Film: same shot, same hand pose, 3 s with passthrough on, 3 s with it off.",
    "The 'off' version must visibly lose the grounding of the shelf and the hands-on-table relationship.",
    "Store the two clips in `assets/raw/video/passthrough-{on,off}.mp4` for P9.4."],
   ["Two matched 3 s clips captured","The difference is visible without narration"],
   ["`assets/raw/video/passthrough-on.mp4`, `passthrough-off.mp4`"],
   ["Staging the comparison in a way that flatters us dishonestly — the judge must be able to tell it's the same scene"]),

 T("P4.6","fov-reflow","Shelf reflow for narrow field of view","1 h","P4.1","W2",
   "FoV-awareness is a named rubric technology, and the VR Glasses profile is 70°×66° against Quest's 110°×96°. "
   "A shelf designed for a wide cone simply disappears on the narrow one.",
   ["Measure the current shelf extents in the XR Simulator at the VR Glasses profile.",
    "Implement a reflow: beyond ~50° of horizontal offset, the shelf folds into a two-row tray closer to the body.",
    "Confirm every object remains selectable and readable in both profiles.",
    "Draw the safe cone (~55°) in the simulator for the video, then remove the debug draw."],
   ["All 12 slots legible at 70°×66°","Reflow transition is smooth, not a pop","Safe-cone diagram captured for the video"],
   ["`docs/02-tech/MRUK-SHELF.md` §6","`assets/raw/video/fov-split.mp4`"],
   ["Scaling the shelf down instead of reflowing it — scaling shrinks the tap targets below usable size"]),
]

p4_links = ("- [`docs/02-tech/MRUK-SHELF.md`](../../docs/02-tech/MRUK-SHELF.md)\n"
            "- [`docs/01-idea/COMPETITION-FIT.md`](../../docs/01-idea/COMPETITION-FIT.md) — FoV and passthrough rows\n"
            "- [`docs/02-tech/TECH-STACK.md`](../../docs/02-tech/TECH-STACK.md) §4")

write_phase(p4, p4_tasks,
  gate_q="Kill the app, relaunch — is the shelf still on the same real surface, with the same objects?",
  gate_criteria=[
    "Shelf anchors to a real surface and faces the user",
    "Objects spawn, settle and idle within budget",
    "Persistence survives 5 relaunches and 1 reboot",
    "Plinth fallback appears instantly with no error state",
    "Matched passthrough on/off clips captured",
    "All 12 slots legible at the VR Glasses profile extent",
    "**A judge can flip passthrough off and watch the experience lose its ground**"],
  gate_red="Persistent failure → make the plinth the permanent design and remove surface-dependence from the pitch (the claim "
           "weakens but does not break). Never ship a shelf that silently resets: a progress artefact that forgets is worse than "
           "no artefact at all.",
  objective="Make the room hold the user's progress: anchor a shelf to real furniture, let mastered shapes accumulate on it, "
            "and make it survive everything a judge can do to it.",
  inputs=["MRUK proven (P1.6, P1.7)","Working lock event (P2.5)","Twelve placeholder object models","XR Simulator with the VR Glasses profile"],
  outputs=["A persistent, anchored shelf","A plinth fallback that can never fail","Passthrough and FoV footage for the video"],
  traps=["Rendering a beautiful shelf that costs 4 ms of GPU — it is 12 small objects, keep it cheap",
         "Treating the fallback as a rare path and not testing it"],
  budget="8 h. Runs parallel with P5 from Day 5.",
  links=p4_links)

p5 = dict(id="P5", name="Content & capture", slug="content", day="5–7 (Wed 11 – Fri 13 Nov)", hours=12, gate_no=5,
          why_here="The legitimacy phase, and the mitigation for the only fatal risk in the project. It also has the longest "
                   "lead time of anything here, which is why the pipeline opened on Day 1.")

p5_tasks = [
 T("P5.1","recruit-and-consent","Recruit the signer and get consent signed","2 h","P0.9","W3",
   "**If this fails, the concept is replaced.** A hearing team shipping a sign-language product without Deaf participation "
   "is the one version of this idea that fails badly — and a Deaf judge will know within a minute.",
   ["Confirm the session booking (target: Day 5) from the Day-1 outreach.",
    "Send the brief in advance so the signer knows exactly what will be asked.",
    "Sign consent covering: competition submission, the demo video, commercial release, and the right to name them — "
    "with an opt-out to be credited anonymously if preferred.",
    "Agree compensation up front, in writing. Not exposure.",
    "Agree the credit line wording together, before the session."],
   ["Consent signed and stored","Compensation agreed in writing","Credit wording agreed by the signer"],
   ["`assets/masters/CONSENT-<initials>-<date>.pdf`","`assets/masters/MANIFEST.md` row 1"],
   ["Leaving credit wording to the end — it must be agreed, not imposed","Recording first and asking later"]),

 T("P5.2","protocol","Write the capture protocol before the session","1.5 h","P5.1","W3",
   "A capture session that has to be repeated costs a week we don't have. The protocol is what makes one session enough.",
   ["Write the printable one-pager the signer will see: the 12 shapes, the order, how long, what to do between takes.",
    "Specify equipment: two cameras (front and 30° side), even lighting, plain background, no rings or reflective jewellery.",
    "Define takes: per shape, 3 s held, 3 vantage points, plus 2 s of neutral at the start and end.",
    "Define the naming convention and the manifest row for each take.",
    "Include the blind-spot set (M, N, T, R, S, E) — we need them *correct* for the honest mirror.",
    "Include the retake rule: a take is rejected if the hand is out of frame, rotated past 30°, or unlit."],
   ["`docs/03-content/CAPTURE-PROTOCOL.md` complete and printable","Naming convention and manifest schema defined","Session scheduled to fit in 90 minutes"],
   ["`docs/03-content/CAPTURE-PROTOCOL.md` (complete)"],
   ["Over-scoping the session — 90 minutes is the ceiling before hands get tired and data gets worse"]),

 T("P5.3","capture-session","Run the capture session","3 h","P5.2","W3",
   "This is the only task in the project that depends on another person being present. It happens once.",
   ["Set up and test the rig 30 minutes before the signer arrives.",
    "Capture neutral first — every pose is relative to it.",
    "Work through the 12 shapes in family order, then the blind-spot set.",
    "Review each take on the spot; retake immediately if it fails the retake rule.",
    "At the end, play back two takes to the signer and confirm they read as the correct shapes.",
    "Record the signer's own description of each shape in their words — this becomes the in-app copy."],
   ["All 12 shapes captured from 3 vantage points","Blind-spot set captured","Signer has confirmed each take reads correctly","Signer's own descriptions recorded"],
   ["`assets/masters/<signer>/` populated","`assets/masters/MANIFEST.md` complete"],
   ["Rushing the last three shapes — they will be the ones that look wrong later","Not capturing the signer's own words; paraphrase loses the register"]),

 T("P5.4","retarget-clean","Retarget and clean the takes into shipped poses","3 h","P5.3","W2",
   "Raw capture is not a product asset. The retarget step is where a take becomes something the matcher can compare against.",
   ["Import takes, retarget to the 26-joint shipped rig, fix any joint flips by hand.",
    "Average 30 frames of the held steady portion of each take to reduce noise.",
    "Export to the frozen JSON schema from P2.1; run `scripts/validate_poses.py` on every file.",
    "Preview each pose on the reference hand and eyeball against the video take.",
    "Store one canonical pose per shape, plus the vantage variants for the honest mirror."],
   ["12 canonical poses, validated","3 vantage renders per shape available for the mirror","No joint flips in preview"],
   ["`unity/Assets/StreamingAssets/Signs/*.json`","`assets/hand/renders/`"],
   ["Averaging the whole 3 s including the approach motion — average only the steady hold"]),

 T("P5.5","credits","Put the signer in the product, not in the small print","1 h","P5.3","W4",
   "Credit is not a compliance artefact. A Deaf signer's hand is the product's content, and the product should say so where "
   "the user can see it.",
   ["Add a credits object on the shelf that, when touched, shows the signer's name, city, and the date of capture.",
    "Include the line: *'This is practice software, not a replacement for a Deaf teacher.'*",
    "Show it within the first minute of first launch — not buried in a menu.",
    "Confirm the wording with the signer before shipping."],
   ["Credits reachable in the first minute","Wording approved by the signer","The 'not a replacement' line present"],
   ["`unity/Assets/_Project/Prefabs/Credits.prefab`"],
   ["Putting credits in a settings menu — hidden credit is no credit"]),

 T("P5.6","final-12","Select the final shipped set from the trackability data","1 h","P5.4","W3",
   "The set must be chosen by evidence, not by ambition. The product is allowed to shrink; it is not allowed to include a "
   "shape that fails.",
   ["Join the trackability table (P1.5) with the captured poses (P5.4).",
    "Any shape below ~70% lock rate in normal light is removed from the set and moved to the honest mirror.",
    "Recheck that the near-miss pairs survive the cut — if the cut breaks a pair, note which demo is lost.",
    "If fewer than 8 shapes survive, fall back to 6 shapes / 1 family and say so in the description.",
    "Update `SIGNSET.md` §2–5 and the near-miss pair table to match reality."],
   ["Final set decided with a documented reason per exclusion","Near-miss pairs re-verified after the cut","Document matches the build"],
   ["`docs/03-content/SIGNSET.md` updated","`phases/p5-content/final-set-decision.md`"],
   ["Keeping a shape because it's in the video plan — re-plan the video"]),

 T("P5.7","blindspot-copy","Write the blind-spot copy with the signer","1 h","P5.3","W3",
   "Explaining a platform limitation in front of a Deaf audience is delicate. The words must be right, and the signer is the "
   "person to get them right.",
   ["Draft the panel copy: what the exclusion is, why it happens, and what changes it.",
    "Have the signer read it and rewrite anything that sounds like an excuse or a simplification.",
    "Keep the tone factual: *the cameras cannot see a finger behind a finger.*",
    "Include the forward-looking line about future hardware, without promising a date."],
   ["Copy approved by the signer","Factual, no apology, no over-promise","One sentence per excluded shape maximum"],
   ["`docs/03-content/TRACKABILITY.md` §4","`unity/Assets/_Project/Prefabs/HonestMirror.prefab` copy"],
   ["Writing it as marketing — the panel's power is that it reads as engineering"]),
]

p5_links = ("- [`docs/03-content/CAPTURE-PROTOCOL.md`](../../docs/03-content/CAPTURE-PROTOCOL.md)\n"
            "- [`docs/03-content/SIGNSET.md`](../../docs/03-content/SIGNSET.md)\n"
            "- [`docs/03-content/TRACKABILITY.md`](../../docs/03-content/TRACKABILITY.md)\n"
            "- [`docs/05-build/RISKS.md`](../../docs/05-build/RISKS.md) — R1")

write_phase(p5, p5_tasks,
  gate_q="Is every shipped handshape traced to a named, credited Deaf signer — and does the product describe itself as practice?",
  gate_criteria=[
    "Consent signed, compensation agreed, credit wording agreed",
    "Capture protocol written before the session",
    "All 12 shapes captured from 3 vantages + the blind-spot set",
    "Signer confirmed the takes read as the correct shapes",
    "12 canonical poses validated by script",
    "Credits visible within the first minute, with the 'not a replacement' line",
    "Final set selected from measured data, with a reason per exclusion",
    "Blind-spot copy written with the signer",
    "**No shipped handshape lacks a credited signer**"],
  gate_red="**This gate is the kill condition.** If no signer has been captured, do not build further on this concept: "
           "stop, and re-open the fallback concepts (COURTSIDE 73.5, THE ROOM REMEMBERS 73.3) the same day. Shipping this "
           "without Deaf participation is the one failure mode that would damage the product, the entry and the team's name.",
  objective="Get legitimate content into the build: real handshapes, from real signers, credited in the product, and a set "
            "chosen by measurement rather than ambition.",
  inputs=["A booked signer with signed consent (from P0.9)","The frozen pose format (P2.1)","The trackability probe (P1.5)","A capture rig"],
  outputs=["Twelve (or six) validated poses","Signer credits in-app","A final content decision documented","Blind-spot copy"],
  traps=["Compressing the session to save an hour and losing half the takes",
         "Paraphrasing the signer instead of quoting them",
         "Choosing the final set before the data is in"],
  budget="12 h across three days, overlapping P4 and P6. The session itself is a fixed 90-minute appointment — everything else flexes around it.",
  links=p5_links)

p6 = dict(id="P6", name="Gaze & accessibility", slug="accessibility", day="7 (Fri 13 Nov)", hours=7, gate_no=6,
          why_here="Gaze is a named rubric technology and the mechanism that makes finger-level inspection possible with both "
                   "hands busy. Accessibility is simultaneously a $25k award, a rubric beat, and the moral centre of the product.")

p6_tasks = [
 T("P6.1","gaze-highlight","Eye-gaze interaction on the finger-highlight layer","2 h","P2.2","W2",
   "You cannot point at a finger while both hands are occupied holding and matching. Gaze is not a gimmick here — it is the "
   "only input available at that moment.",
   ["Add the ISDK Eye Gaze Interaction building block to the rig.",
    "Make each finger a gaze target with a generous hit volume (a capsule, not a plane).",
    "On gaze, highlight the finger and show its position label (from the signer's own description where possible).",
    "Add a dwell timer (start 700 ms) as the confirmation path, with pinch as the alternative.",
    "Test at 0.4 m and 0.6 m: fingers are ~15 mm apart, which is the real precision question."],
   ["Highlight follows gaze accurately at both distances","Dwell and pinch both confirm","No accidental highlights when looking around"],
   ["`unity/Assets/_Project/Scripts/Gaze/GazeHighlighter.cs`","`docs/02-tech/GAZE-INPUT.md` §1–4"],
   ["Making fingers individual targets at 15 mm spacing — if it's jittery, group them (index / middle / ring+pinky / thumb)"]),

 T("P6.2","prove-eye-tracking","Prove eye tracking is actually active — not silently head-raycasting","1 h","P6.1","W2",
   "The most common way to fail the gaze criterion is to think you implemented gaze while the rig quietly fell back to head "
   "direction. A judge who moves their head and sees the highlight follow will know instantly.",
   ["Log the active input source at runtime (eye vs head) to the Immersive Debugger.",
    "Test the discriminator: hold your head still and move only your eyes. The highlight must move.",
    "Test the reverse: hold your eyes on one finger and move your head. The highlight must **not** follow the head.",
    "Add a visible forced-fallback state and its explanation if eye tracking is unavailable or declined.",
    "Record a video of the head-still/eye-move test for the submission."],
   ["Eye-only movement moves the highlight","Head-only movement does not move it","Fallback state exists and is honest"],
   ["`docs/02-tech/GAZE-INPUT.md` §3","`assets/raw/video/gaze-proof.mp4`"],
   ["Testing with the headset's gaze cursor visible — it hides which source is active"]),

 T("P6.3","eyes-only","The eyes-only path","1 h","P6.1","W1",
   "A user with limited hand mobility should be able to complete the whole loop with gaze alone. It is also the strongest "
   "single accessibility demonstration we can put in a video.",
   ["Define the complete loop in gaze terms: select a shape, inspect fingers, confirm, return to shelf.",
    "Make dwell the primary confirm in this mode, with a longer, clearly animated dwell ring.",
    "Lengthen the hold requirement so accidental dwells can't lock a shape.",
    "Play the whole loop through with your hands in your lap and record it end to end."],
   ["Full loop completable with no hand movement","Recorded end to end","No accidental locks in a 5-minute session"],
   ["`assets/raw/video/eyes-only-playthrough.mp4`","`docs/03-content/ACCESSIBILITY.md` §1"],
   ["Making eyes-only a crippled version instead of a complete one"]),

 T("P6.4","hand-calibration","30-second calibration that normalises to the user's hand","1.5 h","P2.2","W1",
   "Every prior system compares the user to a canonical model and fails if they aren't canonical. We invert that: the "
   "reference adapts to their range (N5). It is also the most defensible Best Accessibility Forward argument available.",
   ["Add a calibration flow: open hand, closed fist, thumb-to-each-fingertip. ~30 s, skippable.",
    "Record the user's min/max per joint and store it with the save file.",
    "Normalise incoming angles into the user's own range before matching, and re-normalise the target too.",
    "Test with a deliberately restricted hand (tape two fingers together, or just hold them rigid) and confirm a shape can still lock.",
    "Confirm the calibration persists and can be re-run."],
   ["Restricted-range hand can still lock a shape","Calibration persists across sessions","Skippable without breaking the default path"],
   ["`unity/Assets/_Project/Scripts/Hands/Calibration.cs`","`assets/raw/video/ calibration-with-nonstandard-hand.mp4`"],
   ["Claiming clinical accessibility — we say 'works with non-standard hands', never 'clinically validated'"]),

 T("P6.5","toggles","The five accessibility toggles","1 h","P6.3","W1",
   "Accessibility cannot be a settings panel at the end; but it also has to be reachable. Five toggles, all persistent, all played back.",
   ["Implement: captions on/off (default **on**), high contrast, speed (hold duration + dwell), one-handed mode, eyes-only mode.",
    "Make every toggle reachable inside the space (a physical object), not from a system menu.",
    "Persist all five; restore on launch.",
    "Add the near-miss off switch (from P3.2) as a sixth.",
    "Test each toggle by playing the full loop with it on."],
   ["Six toggles, all working, all persistent, all reachable in-space","Full loop playable with each enabled"],
   ["`docs/03-content/ACCESSIBILITY.md` §4","`phases/p6-accessibility/toggle-matrix.md`"],
   ["Burying toggles in a settings menu that requires a controller to open"]),

 T("P6.6","reduced-motion","Reduced motion path","30 min","P4.2","W1",
   "The shelf's settle animation and the lock payoff are the two places motion could cause discomfort. Both are trivially "
   "reducible and both are visible in the video.",
   ["Add a reduced-motion toggle that replaces arcs and scales with short cross-fades.",
    "Remove any camera-relative motion (there should be none — confirm).",
    "Test the full loop with reduced motion on."],
   ["Reduced-motion loop works end to end","No camera motion anywhere in the product"],
   ["`docs/03-content/ACCESSIBILITY.md` §5"],
   ["Assuming there's no motion problem because the user is seated"]),

 T("P6.7","a11y-doc","Write the accessibility doc to the build, not to the aspiration","1 h","P6.5","W3",
   "The submission will be read against what shipped. A doc that overstates is a liability in front of a panel that might "
   "actually test it.",
   ["List each accessibility path and its actual playthrough evidence (recording filename).",
    "State the claims we are **not** making: no clinical validation, no diagnosis, no medical use.",
    "Note known gaps honestly (e.g. two-handed shapes out of scope).",
    "Cross-check every sentence against the build."],
   ["Every claim traceable to a recording","Explicit non-claims section","No aspirational language"],
   ["`docs/03-content/ACCESSIBILITY.md` (complete)"],
   ["Letting the doc describe the design intention instead of the shipped behaviour"]),
]

p6_links = ("- [`docs/02-tech/GAZE-INPUT.md`](../../docs/02-tech/GAZE-INPUT.md)\n"
            "- [`docs/03-content/ACCESSIBILITY.md`](../../docs/03-content/ACCESSIBILITY.md)\n"
            "- [`docs/01-idea/NOVELTY-AND-PRIOR-ART.md`](../../docs/01-idea/NOVELTY-AND-PRIOR-ART.md) — N5\n"
            "- [`docs/04-design/UX-STATES.md`](../../docs/04-design/UX-STATES.md)")

write_phase(p6, p6_tasks,
  gate_q="Do the one-handed, eyes-only and calibrated-non-standard-hand playthroughs all succeed?",
  gate_criteria=[
    "Gaze highlight accurate at 0.4 m and 0.6 m",
    "Eye tracking proven active (eye-move yes, head-move no), with the test recorded",
    "Eyes-only full loop recorded end to end",
    "Calibration works and a restricted-range hand can still lock a shape",
    "Six toggles implemented, persistent, reachable in-space",
    "Reduced-motion path works",
    "Accessibility doc matches the build with an explicit non-claims section"],
  gate_red="If eye tracking is unavailable on the test hardware and the simulator is the only path, ship the dwell-based "
           "head-gaze fallback **and say so in the description** — an honest partial is worth more than a claimed feature. "
           "If calibration destabilises the matcher, cut it (cut ladder rung 2) rather than ship an inconsistent loop.",
  objective="Make every modality path real: gaze that genuinely reads your eyes, a full eyes-only loop, hand-agnostic "
            "calibration, and five accessibility toggles that all work with the loop rather than around it.",
  inputs=["Working matcher and bands (P2)","Working shelf (P4)","XR Simulator VR Glasses profile","A hand you can restrict deliberately"],
  outputs=["Verified gaze interaction with a proof recording","Eyes-only, one-handed and calibrated playthroughs",
           "Six persistent toggles","An accessibility doc that matches reality"],
  traps=["Believing the rig when it falls back to head gaze",
         "Adding accessibility after the loop is 'done' — it changes the loop",
         "Overclaiming in the doc"],
  budget="7 h. If it overruns, cut the calibration (rung 2), not the gaze.",
  links=p6_links)
print("P4 + P5 + P6 written")
