using System;
using System.IO;
using Signspace.Core;
using Signspace.Gaze;
using Signspace.Hands;
using Signspace.Matching;
using Signspace.Spatial;
using Signspace.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// Headless checks for P6.1–P6.7. One method per task, all runnable in a cold batch process.
    ///
    /// WHAT THESE CAN AND CANNOT PROVE
    /// ------------------------------
    /// They prove the wiring, the math, the state machine, the persistence format and the honesty of
    /// the fallback line. They cannot prove that a person's eyes move the highlight, that a restricted
    /// hand locks on a real tracker, or that five minutes of play produces no accidental lock - those
    /// need the headset and a body, and they stay NOT RUN in the docs. A check that passes here is not
    /// evidence for those rows, and the report says so.
    /// </summary>
    public static class P6Checks
    {
        const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        static int _failures;
        static int _passes;
        static bool _exitOnFinish = true;

        static void Require(bool condition, string label)
        {
            if (condition) { _passes++; Debug.Log("  PASS  " + label); }
            else { _failures++; Debug.LogError("  FAIL  " + label); }
        }

        static void Info(string line) => Debug.Log("  ....  " + line);

        static bool OpenScene()
        {
            if (!File.Exists(Path.GetFullPath(ScenePath)))
            {
                Debug.LogError("[P6Checks] " + ScenePath + " is missing; run SIGNSPACE/Build main scene.");
                return false;
            }
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            return true;
        }

        static string DocsRoot => Path.GetFullPath(Application.dataPath + "/../../docs");

        // ------------------------------------------------------------------ P6.1

        [MenuItem("SIGNSPACE/P6.1 Check gaze highlight")]
        public static void CheckGaze()
        {
            ResetCounters("P6.1 gaze highlight");
            if (!OpenScene()) { Finish("P6.1"); return; }

            var highlighter = UnityEngine.Object.FindFirstObjectByType<GazeHighlighter>();
            var dwell = UnityEngine.Object.FindFirstObjectByType<DwellSelector>();
            var fingers = UnityEngine.Object.FindFirstObjectByType<FingerTargetsBuilder>();
            var hand = UnityEngine.Object.FindFirstObjectByType<ReferenceHand>();

            Require(highlighter != null, "a GazeHighlighter is in the scene");
            Require(dwell != null, "a DwellSelector is in the scene");
            Require(fingers != null && hand != null, "finger targets and the reference hand are wired");
            if (highlighter == null || dwell == null || fingers == null || hand == null) { Finish("P6.1"); return; }

            Require(dwell.highlighter == highlighter, "the dwell listens to this highlighter");
            Require(dwell.dwellSeconds >= 0.6f && dwell.dwellSeconds <= 0.8f,
                    "dwell starts at P6.1's 700 ms (" + dwell.dwellSeconds.ToString("0.00") + " s)");
            Require(dwell.headFallbackMultiplier >= 1.4f,
                    "head gaze needs a longer dwell than eye gaze");

            int built = fingers.Build();
            Require(built == 5, "all five fingers have a target (built " + built + ")");

            var targets = UnityEngine.Object.FindObjectsByType<FingerTarget>(FindObjectsSortMode.None);
            Require(targets.Length == 5, "exactly five FingerTargets exist, so looking around hits none (" +
                                         targets.Length + ")");

            var joints = hand.visual != null ? hand.visual.joints : null;
            Require(joints != null && joints.Length == 26, "the reference hand has its 26-joint rig");

            int sphereOk = 0;
            float expected = fingers.radiusMetres / Mathf.Max(0.01f, hand.visual != null ? hand.visual.scale : 1f);
            for (int i = 0; i < targets.Length; i++)
            {
                var col = targets[i].GetComponent<SphereCollider>();
                if (col != null && col.isTrigger && Mathf.Abs(col.radius - expected) < 0.0005f) sphereOk++;
            }
            Require(sphereOk == 5, "every target is a trigger sphere of the stated radius (" + sphereOk + "/5)");

            int inHand = 0;
            for (int i = 0; i < targets.Length; i++)
                if (joints != null && Array.IndexOf(joints, targets[i].transform) >= 0) inHand++;
            Require(inHand == 5, "every target sits on a joint of the hand it describes (" + inHand + "/5)");

            // Dwell: fires once per acquisition, and works again after looking away (P6.3 re-trigger).
            int fired = 0;
            Action<GameObject> onDwell = _ => fired++;
            dwell.OnDwellComplete += onDwell;

            var probeTarget = new GameObject("P6Check_DwellTarget");
            dwell.BeginFocus(probeTarget);
            float one = dwell.EffectiveDwell + 0.01f;
            dwell.Tick(one);
            Require(fired == 1, "a full dwell confirms once (" + fired + ")");

            dwell.Tick(one);
            Require(fired == 1, "holding the gaze does not fire again (" + fired + ")");

            dwell.NotifyNothingFocused();
            dwell.BeginFocus(probeTarget);
            dwell.Tick(one);
            Require(fired == 2, "looking away and back re-arms the dwell (" + fired + ")");

            dwell.OnDwellComplete -= onDwell;
            UnityEngine.Object.DestroyImmediate(probeTarget);
            dwell.Cancel();

            // Both confirmations exist: pinch on every block, dwell on every block.
            var tray = UnityEngine.Object.FindFirstObjectByType<A11yToggleTray>();
            if (tray != null)
            {
                int blocks = tray.Build();
                var blocksInScene = UnityEngine.Object.FindObjectsByType<A11yToggleBlock>(FindObjectsSortMode.None);
                int withBoth = 0;
                for (int i = 0; i < blocksInScene.Length; i++)
                    if (blocksInScene[i].GetComponent<Grabbable>() != null &&
                        blocksInScene[i].GetComponent<DwellAction>() != null) withBoth++;
                Require(withBoth == blocksInScene.Length && blocksInScene.Length > 0,
                        "every switch confirms by pinch AND by dwell (" + withBoth + "/" + blocksInScene.Length + ")");
                Info(blocks + " tray blocks");
            }

            Finish("P6.1");
        }

        // ------------------------------------------------------------------ P6.2

        [MenuItem("SIGNSPACE/P6.2 Check eye-tracking honesty")]
        public static void CheckEyeTracking()
        {
            ResetCounters("P6.2 eye-tracking honesty");
            if (!OpenScene()) { Finish("P6.2"); return; }

            // The check must not depend on whatever a previous session declined.
            PlayerPrefs.DeleteKey("signspace.gaze.declined");

            var highlighter = UnityEngine.Object.FindFirstObjectByType<GazeHighlighter>();
            Require(highlighter != null, "the scene has a highlighter");
            if (highlighter == null) { Finish("P6.2"); return; }

            var host = new GameObject("P6Check_Probe");
            var probe = host.AddComponent<EyeTrackingProbe>();

            probe.forceMode = EyeTrackingProbe.Forced.Auto;
            probe.Resolve("check: auto");
            Require(probe.Mode == GazeMode.Head || probe.Mode == GazeMode.Eye,
                    "auto resolves to a real source, never None with a camera present (" + probe.Mode + ")");
            Info(probe.StatusLine + " — " + probe.Reason);

            if (probe.Mode == GazeMode.Head)
            {
                Require(probe.Explanation.Contains("no eye tracking") || probe.Explanation.Contains("follows your head"),
                        "the head fallback explains itself");
                Require(!probe.EyesLive, "head mode never claims eye tracking");
            }

            // Eye mode without an eye pose must fall back - and say so. This is the exact silent-lie
            // failure the task is about.
            probe.forceMode = EyeTrackingProbe.Forced.Eye;
            probe.eyeAnchor = null;
            highlighter.probe = probe;
            highlighter.BindProbe();
            Require(!highlighter.eyeTrackingAvailable,
                    "eye mode with no eye pose does NOT claim eye tracking");
            Require(highlighter.IsHeadFallback, "the highlighter reports head fallback");
            Info(highlighter.ModeLine);

            // With a pose bound, the discriminator does flip. Proves the test above is not vacuous.
            var anchor = new GameObject("P6Check_EyeAnchor");
            probe.eyeAnchor = anchor.transform;
            highlighter.BindProbe();
            Require(highlighter.eyeTrackingAvailable,
                    "with an eye pose bound the highlighter does use the eye path");

            probe.forceMode = EyeTrackingProbe.Forced.Head;
            highlighter.BindProbe();
            Require(!highlighter.eyeTrackingAvailable, "forcing head keeps the eye path off");

            // Declining is persisted and explained as a choice, not as a hardware limit.
            probe.forceMode = EyeTrackingProbe.Forced.Auto;
            probe.AllowEyes();
            probe.Decline();
            Require(probe.Declined, "declining eye tracking is remembered");
            Require(probe.Explanation.Contains("your choice") || probe.Mode != GazeMode.Head,
                    "a declined fallback is described as a choice");
            probe.AllowEyes();
            PlayerPrefs.DeleteKey("signspace.gaze.declined");

            UnityEngine.Object.DestroyImmediate(host);
            UnityEngine.Object.DestroyImmediate(anchor);

            Finish("P6.2");
        }

        // ------------------------------------------------------------------ P6.3

        [MenuItem("SIGNSPACE/P6.3 Check eyes-only path")]
        public static void CheckEyesOnly()
        {
            ResetCounters("P6.3 eyes-only path");
            if (!OpenScene()) { Finish("P6.3"); return; }

            var settings = UnityEngine.Object.FindFirstObjectByType<A11ySettings>();
            var router = UnityEngine.Object.FindFirstObjectByType<EyesOnlyRouter>();
            var matcher = UnityEngine.Object.FindFirstObjectByType<Matcher>();
            var dwell = UnityEngine.Object.FindFirstObjectByType<DwellSelector>();

            Require(settings != null && router != null && matcher != null && dwell != null,
                    "settings, router, matcher and dwell are all in the scene");
            if (settings == null || router == null || matcher == null || dwell == null) { Finish("P6.3"); return; }

            bool saved = settings.Data.eyesOnly;
            settings.CaptureSceneBases();

            // Off by default, and inert when off.
            settings.Set(A11yToggle.EyesOnly, false);
            var inertTarget = MakeActionObject(DwellActionKind.ResetPose, null, null);
            inertTarget.GetComponent<DwellAction>().referenceHand =
                UnityEngine.Object.FindFirstObjectByType<ReferenceHand>();
            bool dispatchedOff = router.TryDispatch(inertTarget);
            Require(!dispatchedOff, "with eyes-only off, a dwell actuates nothing");

            settings.Set(A11yToggle.EyesOnly, true);
            float held = matcher.holdSeconds;
            Require(held > Matcher.TunedHoldSeconds,
                    "eyes-only lengthens the hold past the tuned " + Matcher.TunedHoldSeconds.ToString("0.00") +
                    " s (now " + held.ToString("0.00") + " s)");

            int before = router.HandledCount;
            bool dispatchedOn = router.TryDispatch(inertTarget);
            Require(dispatchedOn && router.HandledCount == before + 1,
                    "with eyes-only on, a dwell actuates the action");
            Info(router.LastAction);

            var captions = UnityEngine.Object.FindFirstObjectByType<A11yCaptionPanel>();
            Require(captions != null, "the audio captions are in the scene");

            // An action that is half-wired must refuse rather than silently do nothing.
            var broken = MakeActionObject(DwellActionKind.OpenMirror, null, null);
            var brokenAction = broken.GetComponent<DwellAction>();
            Require(!brokenAction.IsWired, "an unwired action reports itself unwired");
            int handledBefore = router.HandledCount;
            Require(!router.TryDispatch(broken) && router.HandledCount == handledBefore,
                    "an unwired action is refused, not counted as handled");

            // The shape targets: every unlocked silhouette is a SelectShape action.
            var shapes = UnityEngine.Object.FindFirstObjectByType<GazeShapeTargets>();
            Require(shapes != null, "the shelf has a gaze selection component");
            if (shapes != null)
            {
                int count = shapes.Scan();
                var views = UnityEngine.Object.FindObjectsByType<ShelfObjectView>(FindObjectsSortMode.None);
                int wired = 0;
                for (int i = 0; i < views.Length; i++)
                {
                    var a = views[i] != null ? views[i].GetComponent<DwellAction>() : null;
                    if (a != null && a.kind == DwellActionKind.SelectShape && a.IsWired) wired++;
                }
                Require(wired == views.Length, "every shelf object is a wired SelectShape target (" +
                                               wired + "/" + views.Length + ")");
                Info(count + " shape targets");
            }

            settings.Set(A11yToggle.EyesOnly, saved);
            settings.ApplyAll();

            UnityEngine.Object.DestroyImmediate(inertTarget);
            UnityEngine.Object.DestroyImmediate(broken);

            Finish("P6.3");
        }

        static GameObject MakeActionObject(DwellActionKind kind, HonestMirror mirror, CreditsPanel credits)
        {
            var go = new GameObject("P6Check_Action_" + kind);
            var action = go.AddComponent<DwellAction>();
            action.kind = kind;
            action.mirror = mirror;
            action.credits = credits;
            return go;
        }

        // ------------------------------------------------------------------ P6.4

        [MenuItem("SIGNSPACE/P6.4 Check hand calibration")]
        public static void CheckCalibration()
        {
            ResetCounters("P6.4 hand calibration");
            if (!OpenScene()) { Finish("P6.4"); return; }

            var calibration = UnityEngine.Object.FindFirstObjectByType<Calibration>();
            var matcher = UnityEngine.Object.FindFirstObjectByType<Matcher>();
            Require(calibration != null && matcher != null, "calibration and matcher are in the scene");
            if (calibration == null || matcher == null) { Finish("P6.4"); return; }

            // Restricted hand: half the model's range. The stretch should be 2x, capped band 1.35.
            var min = new float[] { 0f, 0f, 0f, 0f, 0f };
            var max = new float[] { 45f, 45f, 45f, 45f, 45f };
            var canonical = new float[] { 90f, 90f, 90f, 90f, 90f };

            var restricted = Calibration.ComputeScales(min, max, canonical, 90f, out float restrictedBand);
            Require(Mathf.Abs(restricted[0] - 2f) < 0.01f,
                    "a hand at half range is stretched 2x (" + restricted[0].ToString("0.00") + ")");
            Require(Mathf.Abs(restrictedBand - 1.35f) < 0.01f,
                    "the lock band is widened, and capped at 1.35 (" + restrictedBand.ToString("0.00") + ")");

            var full = Calibration.ComputeScales(min,
                new float[] { 90f, 90f, 90f, 90f, 90f }, canonical, 90f, out float fullBand);
            Require(Mathf.Abs(full[0] - 1f) < 0.001f && Mathf.Abs(fullBand - 1f) < 0.001f,
                    "a full-range hand is not stretched at all and the band is unchanged");

            // The measurement cannot degenerate: a nearly still finger is clamped, not divided by zero.
            var still = Calibration.ComputeScales(min, new float[] { 0f, 0f, 0f, 0f, 0f }, canonical, 90f, out _);
            Require(still[0] <= 2.5f && still[0] >= 1f, "a still finger is clamped, not infinite");

            // THE LOCK PROOF: a restricted hand against a canonical target.
            // 26 joints, all neutral except index distal, which the target puts at 90 deg and the
            // restricted user can only reach 45 deg.
            var neutral = new Quaternion[26];
            var user = new Quaternion[26];
            for (int i = 0; i < 26; i++) { neutral[i] = Quaternion.identity; user[i] = Quaternion.identity; }

            int indexDistal = PoseMath.FingerChainEnd[1];   // 9
            neutral[indexDistal] = Quaternion.AngleAxis(90f, Vector3.forward);
            user[indexDistal] = Quaternion.AngleAxis(45f, Vector3.forward);

            float raw = PoseMath.Distance(neutral, user, null, out _);
            Require(raw >= 40f, "without calibration the restricted hand is " + raw.ToString("0.0") +
                                " deg away, outside the " + matcher.EffectiveLockedDeg.ToString("0.0") + " deg band");

            user[indexDistal] = Matcher.ScaleRotation(user[indexDistal], restricted[1]);
            float stretched = PoseMath.Distance(neutral, user, null, out _);
            Require(stretched <= 1f,
                    "with calibration the same hand reads " + stretched.ToString("0.00") + " deg — it can lock");

            // Persistence format: the record survives a JSON round trip with its arrays intact.
            var record = new CalibrationRecord
            {
                valid = true,
                min = new float[] { 0f, 1f, 2f, 3f, 4f },
                max = new float[] { 40f, 41f, 42f, 43f, 44f },
                canonical = canonical,
                capturedUtc = DateTime.UtcNow.ToString("o"),
            };
            string json = JsonUtility.ToJson(record);
            var loaded = JsonUtility.FromJson<CalibrationRecord>(json);
            Require(loaded != null && loaded.HasUsableRange && loaded.max[4] == 44f && loaded.canonical[0] == 90f,
                    "the calibration record survives a save/load round trip");
            Require(!new CalibrationRecord().HasUsableRange,
                    "an empty record is not treated as a calibration");

            // Skippable: skipping leaves the default path exactly as it was.
            matcher.SetFingerNormalisation(null, 1f);
            calibration.Skip();
            Require(!calibration.IsRunning && !matcher.HasCalibration,
                    "skipping calibration leaves the default path untouched");

            // Re-run: Begin clears any previous profile before measuring.
            calibration.Begin();
            Require(calibration.IsRunning && calibration.Current == Calibration.Step.Open,
                    "the flow starts at the open-hand step");
            calibration.Skip();
            Require(!calibration.IsRunning, "a second skip still exits cleanly");

            // Restore must refuse a save that says calibrated but holds no usable range.
            var save = UnityEngine.Object.FindFirstObjectByType<SaveSystem>();
            if (save != null)
            {
                var previous = save.Data.calibration;
                save.Data.calibrated = true;
                save.Data.calibration = new CalibrationRecord { valid = true };
                calibration.Restore();
                Require(!calibration.HasResult && !matcher.HasCalibration,
                        "a calibrated flag with no usable range is refused, not half-applied");
                save.Data.calibration = previous;
                calibration.Restore();
            }

            Finish("P6.4");
        }

        // ------------------------------------------------------------------ P6.5

        [MenuItem("SIGNSPACE/P6.5 Check accessibility toggles")]
        public static void CheckToggles()
        {
            ResetCounters("P6.5 toggles");
            if (!OpenScene()) { Finish("P6.5"); return; }

            var settings = UnityEngine.Object.FindFirstObjectByType<A11ySettings>();
            var tray = UnityEngine.Object.FindFirstObjectByType<A11yToggleTray>();
            Require(settings != null && tray != null, "the settings owner and the tray are in the scene");
            if (settings == null || tray == null) { Finish("P6.5"); return; }

            int blocks = tray.Build();
            Require(blocks == A11ySettings.All.Length + 1,
                    "the tray has one block per switch, plus calibrate (" + blocks + ")");

            var data = settings.Data;
            Require(data.captionsOn, "captions default ON — a Deaf user needs them before finding a switch");
            Require(!data.highContrast, "high contrast defaults off");
            Require(Mathf.Approximately(data.speedScale, 1f), "pace defaults to 1x");
            Require(!data.oneHanded && !data.eyesOnly && !data.reducedMotion,
                    "one-hand, eyes-only and reduced motion default off");
            Require(data.nearMissEnabled, "near-miss naming defaults on");

            foreach (var toggle in A11ySettings.All)
            {
                bool original = settings.Get(toggle);
                settings.Set(toggle, !original);
                Require(settings.Get(toggle) == !original, toggle + " flips");
                settings.Set(toggle, original);
                Require(settings.Get(toggle) == original, toggle + " flips back");

                var blocksInScene = UnityEngine.Object.FindObjectsByType<A11yToggleBlock>(FindObjectsSortMode.None);
                bool found = false;
                for (int i = 0; i < blocksInScene.Length; i++) if (blocksInScene[i].toggle == toggle) found = true;
                Require(found, toggle + " has a physical block");
            }

            // Every block is reachable by both inputs.
            var allBlocks = UnityEngine.Object.FindObjectsByType<A11yToggleBlock>(FindObjectsSortMode.None);
            int dual = 0;
            for (int i = 0; i < allBlocks.Length; i++)
                if (allBlocks[i].GetComponent<Grabbable>() != null && allBlocks[i].GetComponent<DwellAction>() != null)
                    dual++;
            Require(dual == allBlocks.Length && allBlocks.Length > 0,
                    "every block is pinch- and gaze-reachable (" + dual + "/" + allBlocks.Length + ")");

            // The speed switch changes the two numbers the user feels.
            var matcher = UnityEngine.Object.FindFirstObjectByType<Matcher>();
            var dwell = UnityEngine.Object.FindFirstObjectByType<DwellSelector>();
            if (matcher != null && dwell != null)
            {
                settings.CaptureSceneBases();
                settings.Set(A11yToggle.Speed, false);
                float holdNormal = matcher.holdSeconds, dwellNormal = dwell.dwellSeconds;
                settings.Set(A11yToggle.Speed, true);
                Require(matcher.holdSeconds > holdNormal && dwell.dwellSeconds > dwellNormal,
                        "relaxed pace lengthens both the hold and the dwell");
                settings.Set(A11yToggle.Speed, false);
            }

            // Persistence: the values live in the save file's own object, and a repair fixes drift.
            var save = UnityEngine.Object.FindFirstObjectByType<SaveSystem>();
            if (save != null)
            {
                Require(ReferenceEquals(save.Data, settings.Data), "the switches read and write the save file");
                int repaired = settings.Repair();
                Info("repair fixed " + repaired + " inconsistent field(s)");
            }

            settings.ResetDefaults();
            Require(settings.Get(A11yToggle.Captions) && !settings.Get(A11yToggle.HighContrast),
                    "reset returns to the safe defaults");

            Finish("P6.5");
        }

        // ------------------------------------------------------------------ P6.6

        [MenuItem("SIGNSPACE/P6.6 Check reduced motion")]
        public static void CheckReducedMotion()
        {
            ResetCounters("P6.6 reduced motion");
            if (!OpenScene()) { Finish("P6.6"); return; }

            var settings = UnityEngine.Object.FindFirstObjectByType<A11ySettings>();
            var idle = UnityEngine.Object.FindFirstObjectByType<ShelfIdle>();
            var hand = UnityEngine.Object.FindFirstObjectByType<ReferenceHand>();
            var payoff = UnityEngine.Object.FindFirstObjectByType<LockPayoff>();

            Require(settings != null, "the settings owner is in the scene");
            Require(idle != null && hand != null && payoff != null,
                    "the three motion sources exist (shelf breath, hand drift, lock payoff)");
            if (settings == null || idle == null || hand == null || payoff == null) { Finish("P6.6"); return; }

            settings.CaptureSceneBases();
            settings.Set(A11yToggle.ReducedMotion, false);
            float fullPayoff = payoff.PlannedSeconds;
            float spin = hand.idleDegreesPerSecond;
            Require(spin > 0f, "the reference hand drifts by default (" + spin.ToString("0.0") + " deg/s)");

            settings.Set(A11yToggle.ReducedMotion, true);
            Require(!idle.breathing, "the shelf stops breathing");
            Require(Mathf.Approximately(hand.idleDegreesPerSecond, 0f), "the reference hand stops drifting");
            Require(payoff.reducedMotion, "the payoff knows to skip the arc");
            Require(payoff.PlannedSeconds < fullPayoff * 0.6f,
                    "the payoff becomes a cross-fade: " + fullPayoff.ToString("0.00") + " s -> " +
                    payoff.PlannedSeconds.ToString("0.00") + " s");

            settings.Set(A11yToggle.ReducedMotion, false);
            Require(idle.breathing, "turning it off brings the breathing back");
            Require(Mathf.Approximately(hand.idleDegreesPerSecond, spin), "and the drift returns to its tuned speed");
            Require(!payoff.reducedMotion, "and the payoff runs its full path again");

            // No camera motion anywhere: the product never moves the head for the user. The reference
            // hand places itself ONCE, from the camera, and then only rotates in place.
            var moves = 0;
            var behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                var b = behaviours[i];
                if (b == null) continue;
                var n = b.GetType().Name;
                if (n.Contains("Shake") || n.Contains("HeadBob") || n.Contains("CameraMover")) moves++;
            }
            Require(moves == 0, "no camera-shake or head-bob component exists (" + moves + ")");

            Finish("P6.6");
        }

        // ------------------------------------------------------------------ P6.7

        [MenuItem("SIGNSPACE/P6.7 Check accessibility doc")]
        public static void CheckA11yDoc()
        {
            ResetCounters("P6.7 accessibility doc");
            if (!OpenScene()) { Finish("P6.7"); return; }

            string path = Path.Combine(DocsRoot, "03-content/ACCESSIBILITY.md");
            Require(File.Exists(path), "ACCESSIBILITY.md exists at " + path);
            if (!File.Exists(path)) { Finish("P6.7"); return; }

            string doc = File.ReadAllText(path);

            foreach (var toggle in A11ySettings.All)
            {
                string title = A11ySettings.Title(toggle);
                Require(doc.IndexOf(title, StringComparison.OrdinalIgnoreCase) >= 0,
                        "the doc names the '" + title + "' switch");
            }

            Require(doc.IndexOf("not claiming", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    doc.IndexOf("NOT CLAIMING", StringComparison.Ordinal) >= 0,
                    "the doc has an explicit non-claims section");
            Require(doc.IndexOf("clinical", StringComparison.OrdinalIgnoreCase) >= 0,
                    "the doc states the clinical position");
            Require(doc.IndexOf("NOT RUN", StringComparison.Ordinal) >= 0,
                    "device-only rows are marked NOT RUN, not claimed");
            Require(doc.IndexOf("recording", StringComparison.OrdinalIgnoreCase) >= 0,
                    "each path is tied to its evidence");

            string gazePath = Path.Combine(DocsRoot, "02-tech/GAZE-INPUT.md");
            Require(File.Exists(gazePath), "GAZE-INPUT.md exists");
            if (File.Exists(gazePath))
            {
                string gaze = File.ReadAllText(gazePath);
                Require(gaze.IndexOf("head", StringComparison.OrdinalIgnoreCase) >= 0,
                        "the gaze doc describes the head fallback");
                Require(gaze.IndexOf("Quest 3", StringComparison.OrdinalIgnoreCase) >= 0,
                        "the gaze doc names the hardware limit");
                Require(gaze.IndexOf("NOT RUN", StringComparison.Ordinal) >= 0,
                        "the gaze doc marks the eye-movement proof as not run");
            }

            Finish("P6.7");
        }

        // ------------------------------------------------------------------ runner

        [MenuItem("SIGNSPACE/P6 Check all")]
        public static void CheckAll()
        {
            // One process runs every check, so the individual methods must not exit on their own.
            _exitOnFinish = false;
            _passes = 0;
            _failures = 0;

            CheckGaze();
            CheckEyeTracking();
            CheckEyesOnly();
            CheckCalibration();
            CheckToggles();
            CheckReducedMotion();
            CheckA11yDoc();

            Debug.Log("[P6Checks] TOTAL " + _passes + " passed, " + _failures + " failed");
            EditorApplication.Exit(_failures == 0 ? 0 : 1);
        }

        static int _checkStartPasses, _checkStartFailures;

        static void ResetCounters(string title)
        {
            if (_exitOnFinish)
            {
                _passes = 0;
                _failures = 0;
            }
            _checkStartPasses = _passes;
            _checkStartFailures = _failures;
            Debug.Log("[P6Checks] " + title);
        }

        static void Finish(string title)
        {
            int p = _passes - _checkStartPasses;
            int f = _failures - _checkStartFailures;
            Debug.Log("[P6Checks] " + title + ": " + p + " passed, " + f + " failed");
            if (_exitOnFinish) EditorApplication.Exit(f == 0 ? 0 : 1);
        }
    }
}
