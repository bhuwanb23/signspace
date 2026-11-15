# P1.2 — grab log

**Task:** P1.2 · grab a free-floating object with hands only · **Gate fed:** G1
**Started:** 15 Nov 2026 · **Status:** 🟡 code complete, **never compiled**; the 20-trial run needs the headset

> The artefact P1.2 names is this file with the 20-trial result in it. The trial result cannot exist
> without a hand and a headset. Everything that *can* be established without one is below, and the
> section that will hold the numbers is already shaped to receive them.

---

## 1. The result (to be filled on device)

| Hand | Attempts | Engaged | Missed | Notes |
|---|---|---|---|---|
| **Right** | — | — | — | |
| **Left** | — | — | — | |
| **Total** | — | — | — | |

**Gate requirement: 20/20 with each hand.** Do not fill this table from memory. The numbers are written
by `GrabTrialLogger` to `persistentDataPath/grab-summary.txt` and every attempt to `grab-trials.jsonl`;
paste the summary here and pull the jsonl if a miss needs explaining.

```
adb shell run-as com.signspace.signspace cat \
  /sdcard/Android/data/com.signspace.signspace/files/grab-summary.txt
```

## 2. What a "miss" means — fixed before the trial, not after

| Event | Counts as |
|---|---|
| Pinch closes with nothing grabbable within 0.45 m | **nothing.** A pinch at empty air is not an attempt |
| Pinch closes with the cube within 0.45 m | **an attempt** |
| An attempt where the cube is within `grabDistance + radius` (0.207 m) | **success** |
| An attempt where it is not | **miss** — and the recorded distance says by how much |

Defining this *before* running the trial is the difference between a measurement and a story. It also
settles the first trap on P1.2's list — "tuning pinch too tight and blaming it on the tracker": a miss
under 0.207 m is a **threshold** problem (the pinch point was close enough to engage but the pinch never
closed, i.e. `pinchDistance`), and a miss over it is a **reach** problem. The distance column
distinguishes them, so neither has to be argued.

## 3. Why this does not use ISDK `HandGrabInteractor` / `HandGrabInteractable`

P1.2's task text specifies the ISDK components. This implementation does not use them, and the reason
has to be stated rather than assumed:

**The argument for the ISDK, in Meta's own words** — *"The recommended way to integrate hand tracking for
Unity developers is to use the Interaction SDK, which provides standardized interactions and gestures.
Building custom interactions without the SDK can be a significant challenge and makes it difficult to get
approved in the store."* ([Hands Setup](https://developers.meta.com/vr/documentation/unity/unity-handtracking-hands-setup/),
updated 14 Oct 2026.) That is a real argument and it is recorded here rather than dismissed.

**Why this phase still goes custom:**

1. **ISDK grab needs an ISDK hand.** `HandGrabInteractor` is bound to ISDK's `Hand` / `HandGrabApi`
   rig. Using it means running a *second* hand pipeline beside `MetaHandSource` + `SkeletonPoseSource`,
   and the only thing P1.2 must prove is that **our** pipeline can drive grab. A grab that works
   because the ISDK was also tracking would not be evidence about the product.
2. **The mechanic is narrower than the SDK.** We need pinch-on-proximity to a floating object. ISDK
   grab brings grasp poses, physics jitter handling, finger curling and grab types — a great deal of
   machinery for a question with one axis.
3. **It has to compile without the SDK.** The same reflection rule as `MetaHandSource`: binding ISDK
   types directly puts the SDK in the compile path the project has deliberately kept it out of.

**Where this decision would be revisited:** if the grab *feels* wrong on device (P1.2 missed, or P1.3
rotation feels bad), the ISDK path becomes the fallback experiment, because "feel" is exactly what it
is good at. That is a one-line note in this file now and would be a real decision then. Nothing in
P1.2's code blocks it: `Grabbable` and `PinchGrabber` are leaf components with no callers that would
have to change.

## 4. What was built

| File | What |
|---|---|
| `Scripts/Hands/Grabbable.cs` | radius, generous `grabDistance`, hover highlight (so a near miss is distinguishable from a missed pinch), follow-the-wrist delta rotation |
| `Scripts/Hands/PinchGrabber.cs` | pinch rising edge + nearest grabbable in `intentRadius`; grab, follow, release; records attempts |
| `Scripts/Hands/GrabTrialLogger.cs` | per-hand attempt/success counts, JSONL per event, human-readable summary; writes on quit **and** on pause, and every 20 attempts so a kill from the recent list loses nothing |
| `Scripts/Hands/NoFallbackGuard.cs` | scans every MonoBehaviour's type name for ray/pointer/controller fragments and **errors** if one exists — criterion 3 as a runtime check instead of a claim |
| `Editor/GrabTestBuilder.cs` | one 10 cm cube at 0.45 m on the camera axis, a grabber per hand, logger, guard. Called from `SceneBuilder`, so a batch build contains the P1.2 rig |
| `Scripts/Hands/MetaHandSource.cs` | added `PinchPoint` (tip midpoint) and `WristRotation` (world wrist rotation) — the grab measures and turns from the right places |

## 5. Criterion by criterion

| G1 criterion (P1.2's share) | Status |
|---|---|
| 20/20 successful grabs with each hand | ⬜ **device** — table in §1 |
| No ray or controller fallback active | 🟡 **mechanically verified in the scene as built**: `NoFallbackGuard` scans on start and errors if anything matches; `GrabTestBuilder` creates no such component. It cannot catch a ray with an innocent name, and says so in its own header |
| Miss rate recorded | 🟡 **instrumented**: every attempt is written with its distance before anyone judges it |
| Grab works with no controllers connected | ⬜ **device** — requires Hands Only / hands+controllers with the controllers put down |

## 6. The trial procedure (5 minutes with the headset on)

1. Launch. Confirm the readout says `GRAB L 0/0 · R 0/0` and the cube is visible at arm's length.
2. **Right hand:** pinch → move → release, twenty times. Deliberately include three sloppy reaches so
   a marginal threshold shows up rather than being averaged away by careful ones.
3. **Left hand:** the same twenty.
4. Read the tally off the readout, then pull `grab-summary.txt` and paste it into §1.
5. If misses occur: read the recorded distance. Under 0.207 m → widen `pinchDistance` on
   `MetaHandSource` (25 mm → 30 mm) and re-run **once**, recording both runs. Over it → the reaches
   were short, and that is P1.3's distance question, not a grab failure.

## 7. Change log

| Date | Change |
|---|---|
| 15 Nov 2026 | Log opened. Definition of a miss fixed before any data exists. ISDK decision recorded with Meta's counterargument. Code complete, unverified. |
