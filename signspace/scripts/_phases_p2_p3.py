import sys, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from _phasegen import T, write_phase

p2 = dict(id="P2", name="Core loop", slug="core-loop", day="3 (Mon 9 Nov)", hours=10, gate_no=2,
          why_here="This is where the product stops being a demonstration and becomes a system. Everything after "
                   "this phase is feedback, polish or packaging — so the loop has to be right, not merely present.")

p2_tasks = [
 T("P2.1","pose-format-final","Finalise the pose format and the capture-neutral contract","1.5 h","P1.4","W2",
   "Every downstream system — matching, capture, the shelf, near-miss — reads this one file format. Getting it wrong on Day 3 "
   "means rewriting on Day 8, when there is no room.",
   ["Freeze the schema: `version`, `id`, `handedness`, `signer`, `captured`, `vantage`, `joints[]` (26 names), "
    "`rot[]` as XYZW quaternions **relative to that signer's neutral capture**, `confidence` metadata.",
    "Define the neutral capture procedure precisely: relaxed open hand, palm down, held 2 s, averaged over 60 frames.",
    "Add a schema validator script (`scripts/validate_poses.py`) that fails loudly on joint-count or normalisation errors.",
    "Generate three sample poses and check them in as fixtures.",
    "Write `docs/02-tech/POSE-FORMAT.md` in full, including the left-hand mirroring rule."],
   ["Schema frozen and documented","Validator script runs and catches a deliberately broken file","Three fixture poses committed"],
   ["`docs/02-tech/POSE-FORMAT.md` (complete)","`scripts/validate_poses.py`","`unity/Assets/StreamingAssets/Signs/*.json` fixtures"],
   ["Storing absolute world rotations — user hand size then changes the shape","Forgetting `version` and having no migration path on Day 9"]),

 T("P2.2","matcher-v1","Build the matcher: relative-angle distance with per-joint weights","2.5 h","P2.1","W2",
   "The matcher decides whether the app feels like a mirror or a bouncer. It is the highest-consequence arithmetic in the build.",
   ["For each of the 26 joints compute the bone-local delta angle between the current pose and the target: "
    "`θ_j = 2·acos(|w|)` of the relative quaternion.",
    "Apply per-joint weights: proximal joints (wrist, CMC) low (~0.4), mid-phalangeal high (~1.0). Store weights in a serialisable asset so they can be tuned without recompiling.",
    "Compute weighted mean angle `D` in degrees across the fingers of interest.",
    "Expose `D` live in the Immersive Debugger overlay, plus per-joint contributions for the three worst joints.",
    "Verify against the three fixture poses: holding a fixture with your own hand should give a visibly lower `D` than a wrong shape."],
   ["A live, per-frame distance value in degrees","Per-joint weights data-driven, not hardcoded","Debug overlay showing the three worst joints"],
   ["`unity/Assets/_Project/Scripts/Matching/Matcher.cs`","`Matching/Weights.asset`","`phases/p2-core-loop/matcher-tuning.md`"],
   ["Averaging all 26 joints equally — the little finger then cancels out the thumb","Normalising by joint count instead of by total weight, which hides which fingers are wrong"]),

 T("P2.3","bands","Three bands, no failure state","1 h","P2.2","W2",
   "The product never tells a learner they are wrong in the abstract — it tells them how far they are and in which direction. "
   "That behaviour is a hard design commitment (ADR-003), and it lives in this threshold set.",
   ["Define three bands over `D`: **warming** (D > 24°), **close** (12° ≤ D ≤ 24°), **locked** (D < 12°).",
    "Map each band to a distinct visual (colour + motion, never text) and a distinct sound (see `SOUND-MAP.md`).",
    "Tune the thresholds against your own hand across all four fixtures; record the tuning table.",
    "Add hysteresis: a band only changes after 150 ms in the new state, so the display never strobes.",
    "Delete every string in the UI that could read as an error."],
   ["Three bands with tuned, recorded thresholds","No hysteresis flicker over a 30 s test","Zero error strings in the build"],
   ["`phases/p2-core-loop/band-tuning.md` with the threshold table and the evidence for it"],
   ["Picking round numbers because they look tidy — thresholds are measured, not chosen","Skipping hysteresis and shipping a strobing meter"]),

 T("P2.4","hold-and-dropout","The 0.7 s hold, with dropout tolerance","1 h","P2.3","W2",
   "A shape must be *held*, not touched. And a half-second tracking dropout must never reset a nearly-successful attempt — "
   "that single behaviour is what makes hand tracking feel broken in most apps.",
   ["Implement a hold timer that only accumulates while `D` is inside the locked band.",
    "Add a dropout grace of 200 ms: if tracking confidence dips, freeze the timer rather than reset it.",
    "On lock, fire the lock event once and latch until the hand leaves the band for >500 ms.",
    "Test: 20 locks, then 20 deliberate flicks in and out of the band to confirm no false locks.",
    "Log every lock with a timestamp to `phases/p2-core-loop/lock-log.csv`."],
   ["20/20 intentional locks","0/20 false locks from flicks","Hold timer visibly correct in the debug overlay"],
   ["`unity/Assets/_Project/Scripts/Matching/HoldTimer.cs`","`phases/p2-core-loop/lock-log.csv`"],
   ["Resetting the timer on a single dropped frame — this is the most common defect in hand-tracking apps"]),

 T("P2.5","lock-payoff","The lock payoff: the shape becomes an object","1.5 h","P2.4","W1",
   "The first lock is the product's first reward, and it happens inside 75 seconds. If it doesn't feel like an event, "
   "the whole loop is flat.",
   ["On lock: the reference hand dissolves (scale + fade over ~400 ms) and re-forms as a small object.",
    "The object travels along a gentle arc to a temporary anchor point (the shelf arrives in P4).",
    "Play the signature lock chime; add a brief controller-free haptic substitute (a subtle pulse in the hand visual).",
    "Test the timing: the whole payoff must complete in under 1.2 s so it never feels like a cutscene.",
    "Record it — this is the 0:45 shot in the submission video."],
   ["Lock payoff completes in <1.2 s","The dissolution reads as *transformation*, not deletion","Footage captured for the video"],
   ["`unity/Assets/_Project/Prefabs/LockPayoff.prefab`","`assets/raw/video/lock-payoff-take01.mp4`"],
   ["Making it longer because it looks nice — it will be skipped by the third repetition"]),

 T("P2.6","palm-orientation","Weight palm orientation separately from finger angles","1 h","P2.2","W2",
   "Several sign pairs differ mostly in which way the palm faces. If palm orientation is folded into the finger average, "
   "the app will accept a correct-shape/wrong-facing attempt — which for a sign learner is a different word.",
   ["Compute a palm basis: wrist forward vector plus index-knuckle plane normal. Compare against the target's basis.",
    "Produce a separate orientation error `O` in degrees.",
    "Combine: `D_total = D_fingers + k · max(0, O − O_slack)` with `O_slack` ≈ 15° and `k` tuned.",
    "Test with the two most orientation-sensitive shapes in the set (C/O pair and A/B).",
    "Record the sensitivity curve in the tuning notes."],
   ["Palm orientation contributes independently","A deliberately mis-facing hand fails to lock","`k` and `O_slack` recorded"],
   ["`unity/Assets/_Project/Scripts/Matching/PalmOrientation.cs`","`phases/p2-core-loop/palm-tuning.md`"],
   ["Using Euler angles — gimbal ambiguity will produce phantom errors at certain wrist rotations"]),

 T("P2.7","left-hand","Left-hand support, or an explicit decision not to","1.5 h","P2.4","W2",
   "Left-handers are a third of the population and one-handed play is part of the accessibility story. But mirrored poses are "
   "not always valid, and half-support is worse than none.",
   ["Mirror the three fixtures: negate the X axis of each relative quaternion and swap handedness.",
    "Test whether mirrored poses lock correctly with the left hand.",
    "If yes: enable left hand, and add a handedness toggle in the debug overlay.",
    "If no: disable left hand cleanly, show a one-line explanation, and log it as a known limitation for the honest mirror.",
    "Record the decision and the reason in `docs/04-design/UX-STATES.md`."],
   ["A definite answer, tested, with the evidence","No half-broken left-hand path left in the build"],
   ["`phases/p2-core-loop/left-hand-decision.md`"],
   ["Assuming mirroring is symmetric — thumb opposition usually breaks first"]),
]

p2_links = ("- [`docs/02-tech/MATCH-ALGORITHM.md`](../../docs/02-tech/MATCH-ALGORITHM.md)\n"
            "- [`docs/02-tech/POSE-FORMAT.md`](../../docs/02-tech/POSE-FORMAT.md)\n"
            "- [`docs/04-design/UX-STATES.md`](../../docs/04-design/UX-STATES.md)\n"
            "- [`docs/08-decisions/ADR-003-forgiveness.md`](../../docs/08-decisions/ADR-003-forgiveness.md)")

write_phase(p2, p2_tasks,
  gate_q="Can a stranger lock one handshape, uncoached, without anyone telling them what to do?",
  gate_criteria=[
    "Pose format frozen, documented, validated by script, with three committed fixtures",
    "A live per-joint distance computed in degrees, weights data-driven",
    "Three bands with tuned thresholds and no hysteresis flicker",
    "20/20 intentional locks and 0/20 false locks",
    "Lock payoff completes in under 1.2 s and has been filmed",
    "Palm orientation contributes independently and a mis-facing hand fails",
    "Left hand works, or is disabled with a documented reason",
    "**A person who has never seen the app locks one shape without instruction**"],
  gate_red="A red G2 is a *feel* problem, not a feature problem. Do not add features to fix it. In order: widen the locked band, "
           "increase `O_slack`, lengthen the hold to 0.9 s. If it still fails after those three, the disambiguation between "
           "'near' and 'locked' is wrong and is redesigned on Day 4 before P3 begins.",
  objective="Turn the Day-2 spikes into a coherent loop: a reference hand you can rotate, a matcher that measures you against it, "
            "three bands that never say 'wrong', and a payoff that makes the first success feel like an event.",
  inputs=["A working grab-and-rotate gesture (P1.3 verdict)","A pose loader (P1.4)","The trackability results (P1.5)",
          "Two or three real handshapes to test with — placeholders are fine"],
  outputs=["A playable loop, one shape deep","A tuned distance metric with recorded evidence",
           "A filmed lock payoff","A documented decision on left-hand support"],
  traps=["Tuning thresholds by taste instead of against fixtures",
         "Letting the UI show a percentage — percentages invite optimisation and destroy the felt sense of a shape",
         "Fixing feel problems by adding features"],
  budget="10 h — the longest single phase. If it slips, take the time from P7, never from P3.",
  links=p2_links)

p3 = dict(id="P3", name="Feedback layer", slug="feedback", day="4 (Tue 10 Nov)", hours=8, gate_no=3,
          why_here="This phase is the submission's identity, not its polish. Two of the five novelty moves live here, and "
                   "the video's first four seconds are shot in this phase.")

p3_tasks = [
 T("P3.1","near-miss-table","Build the near-miss: name the sign they actually made","2.5 h","P2.2","W2",
   "Every competitor returns correct/incorrect. Naming the *confusable* sign is the single thing no prior project does "
   "(see `NOVELTY-AND-PRIOR-ART.md` N1), and it is the product's best three seconds of video.",
   ["For every target shape, precompute the pairwise distance from that target to each of the other 11 shapes.",
    "At runtime, compute the user's distance to *all* 12 at once (cost is trivial: 12 × 26 joint angles).",
    "Rule: if `D(target) > locked_band` **and** `D(other) < locked_band` for exactly one other shape, fire the near-miss naming `other`.",
    "If two or more others qualify, stay silent — ambiguity is not a diagnosis.",
    "Add the magnitude to the message: the measured delta between the two shapes at that moment, rounded to whole degrees.",
    "Test by deliberately making each of the 8 documented near-miss pairs from `SIGNSET.md` §5."],
   ["All 8 documented pairs fire correctly","Two-other ambiguity produces silence","Magnitude shown and plausible in the 5–20° range"],
   ["`unity/Assets/_Project/Scripts/Matching/NearMiss.cs`","`phases/p3-feedback/near-miss-test.csv`"],
   ["Computing distance only to the target and then guessing the nearest — compute all 12, it is free",
    "Firing on the second-nearest when the first is nearly as close"]),

 T("P3.2","confidence-gate","Confidence gating: silence is the default","1.5 h","P3.1","W2",
   "A wrong diagnosis is worse than no diagnosis — it teaches a false correction to someone learning a language. The gate "
   "that prevents it is the difference between a delightful feature and a harmful one.",
   ["Pull hand-tracking confidence per frame; define a floor (start at 0.7, tune).",
    "Near-miss may only fire if confidence stayed above the floor for the preceding 400 ms.",
    "Add a per-session cap: no more than one near-miss per 20 s, so it never becomes nagging.",
    "Add an easy off switch in the accessibility toggles (P6.5).",
    "Run a 20-attempt false-positive test with fast, sloppy, wrong-but-not-close hands. Target: **zero** fires."],
   ["Zero false positives over 20 sloppy attempts","A visible confidence floor value in debug","Session rate cap working"],
   ["`phases/p3-feedback/false-positive-test.md`"],
   ["Firing on the first frame that satisfies the rule — require a stable window"]),

 T("P3.3","copy","Write the near-miss copy","45 min","P3.1","W4",
   "The line appears at the emotional low point of a learner's attempt. It must be warm, short, and specific — a scolding "
   "or a paragraph will undo the whole mechanic.",
   ["Draft 10 variants of the line. Constraints: ≤12 words, no 'incorrect', no 'error', no exclamation marks.",
    "Test each on a non-signing friend and ask what they think the app wants them to do next.",
    "Pick the clearest; keep a second for the accessibility (high-contrast, text-only) path.",
    "Record the final string in `docs/06-submission/SUBMISSION-COPY.md` under 'in-product copy'."],
   ["A final string ≤12 words","Understood by a non-signer in one read","No negative framing"],
   ["`phases/p3-feedback/copy-variants.md`"],
   ["Explaining the linguistics in the UI — save that for the video"]),

 T("P3.4","honest-mirror","Build the honest mirror: publish the shapes we can't see","1.5 h","P1.5","W3",
   "No competition entry admits its platform's limits. Ours does, visibly, with the reasons — which builds trust with a Deaf "
   "judge, signals engineering depth, and flatters Meta's roadmap (see N2).",
   ["Take the 'Never' and 'Low' rows from `TRACKABILITY.md` (expected: M, N, T, R, and possibly S, E).",
    "Build a panel: each excluded shape shown correctly (from the signer capture), greyed, with one line of why.",
    "Include the count line: *'X of Y handshape variants are robustly trackable today.'* — with real numbers from P1.5.",
    "Reach it from the shelf (a physical object that opens it), not from a settings menu.",
    "Run it past the signer if possible — the shapes must be shown *correctly* even though they're excluded."],
   ["Panel ships with correct shape renders","Count line uses measured numbers","Reachable from within the space, not a menu"],
   ["`unity/Assets/_Project/Prefabs/HonestMirror.prefab`","`docs/03-content/TRACKABILITY.md` §4"],
   ["Turning it into an apology — it is a demonstration of rigour","Showing a shape wrong because 'it's excluded anyway'"]),

 T("P3.5","confidence-readout","Live confidence readout that pauses instead of failing","1 h","P3.2","W2",
   "When tracking degrades the app must visibly stop measuring, not quietly mis-measure. This is the behaviour that makes "
   "the fragility honest instead of confusing.",
   ["Add a subtle indicator that dims when confidence is below the floor.",
    "Freeze the hold timer, dim the match display, and show *'watching'* state (no words if possible — a visual pulse).",
    "Resume automatically when confidence returns.",
    "Test by occluding a hand behind the other, moving fast, and leaving the room's lights low.",
    "Confirm no lock can ever be awarded while confidence is below the floor."],
   ["Pause behaviour proven in three degradation scenarios","No lock awarded below the floor","No error string anywhere"],
   ["`unity/Assets/_Project/Scripts/UI/TrackingConfidence.cs`"],
   ["Showing a score during a pause — a frozen number reads as a stalled app"]),

 T("P3.6","numbers","Verify the honest-mirror numbers on device","30 min","P3.4","W3",
   "The panel's credibility rests on its numbers being true. An invented count is the one thing that would turn the honest "
   "mirror into a marketing claim.",
   ["Count the shape variants actually tested in P1.5 and the ones rated High/Medium.",
    "Re-test the borderline shapes (S, E) three times each in lamplight.",
    "Write the final count into the panel copy and into `TRACKABILITY.md` §4.",
    "Add a date to the panel: *'measured on Quest 3S, <date>'* — limits change with SDK versions."],
   ["The count in the panel matches the count in the doc","Borderline shapes resolved or the count phrased as a range","Measurement date shown"],
   ["`docs/03-content/TRACKABILITY.md` §4 final"],
   ["Rounding the count upward to look better — the precision is the feature"]),

 T("P3.7","trackability-publish","Publish the trackability document","30 min","P3.6","W3",
   "The measurement is only an asset if it is written down where the submission can draw on it.",
   ["Fill every table in `docs/03-content/TRACKABILITY.md`.",
    "Write the failure taxonomy with one real example per class.",
    "State the decision that followed from the data (which shapes shipped, which were excluded).",
    "Cross-link from `SIGNSET.md` §7."],
   ["Document complete with no `[TBD]`","Decision traceable to a specific row of data"],
   ["`docs/03-content/TRACKABILITY.md` (complete)"],
   ["Leaving it as raw notes — the doc is what the video script reads from"]),
]

p3_links = ("- [`docs/01-idea/NOVELTY-AND-PRIOR-ART.md`](../../docs/01-idea/NOVELTY-AND-PRIOR-ART.md) — N1 and N2\n"
            "- [`docs/03-content/SIGNSET.md`](../../docs/03-content/SIGNSET.md) §5–7\n"
            "- [`docs/03-content/TRACKABILITY.md`](../../docs/03-content/TRACKABILITY.md)\n"
            "- [`docs/04-design/SOUND-MAP.md`](../../docs/04-design/SOUND-MAP.md)")

write_phase(p3, p3_tasks,
  gate_q="Does the near-miss land in under 30 seconds of play — and does it never once lie?",
  gate_criteria=[
    "All 8 documented near-miss pairs fire correctly",
    "Zero false positives across 20 sloppy attempts",
    "Confidence gate, session rate cap, and an off switch all working",
    "Near-miss copy is ≤12 words and understood by a non-signer in one read",
    "Honest-mirror panel ships, reachable from inside the space, with correct (not shortcut) shape renders",
    "The published count matches measured data and carries a measurement date",
    "No lock can be awarded while confidence is below the floor",
    "**A stranger playing for 30 seconds experiences the near-miss and can explain what it told them**"],
  gate_red="If the near-miss lies even once, disable it rather than tune it live — a silent product is acceptable, a wrong one is not. "
           "If the honest mirror reads as an apology rather than a flex, rewrite the framing before moving on: the panel is a "
           "never-cut item, so a G3 red blocks P4.",
  objective="Build the layer that makes this submission different from every prior project in the field: the near-miss "
            "diagnosis, the honest mirror, and the discipline that keeps both truthful.",
  inputs=["A working matcher with tuned bands (P2)","Measured trackability data (P1.5)","The 12-shape set with validated pairs"],
  outputs=["A near-miss feature with a zero-false-positive record","A shipped honest-mirror panel with real numbers",
           "A complete trackability document","In-product copy signed off"],
  traps=["Adding features after the gate goes green — this phase is where scope creep is most tempting",
         "Making the honest mirror a settings screen instead of a place",
         "Letting the near-miss become frequent; it must stay rare to stay valuable"],
  budget="8 h. On the critical path: P7 cannot start until G3 is green.",
  links=p3_links)
print("P2 + P3 written")
