using Signspace.Core;
using Signspace.Gaze;
using Signspace.Hands;
using Signspace.Matching;
using Signspace.Spatial;
using Signspace.UI;
using UnityEditor;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// Wires the whole P6 accessibility layer into the main scene (P6.1–P6.6).
    ///
    /// One builder, one object. Every component is placed and every reference is assigned here, so the
    /// checks can load the saved scene and assert on the same wiring the build uses rather than
    /// re-creating it. The alternative - assigning references by hand - is exactly the kind of editor
    /// state that a batch rebuild silently loses.
    ///
    /// WHAT IS DELIBERATELY NOT HERE
    /// -----------------------------
    /// No eye anchor. Quest 3 and 3S have no eye tracking, and even on a device that does, this project
    /// binds no eye pose. The probe reports that honestly and the highlight says so in one line; a
    /// fabricated eye anchor would make the gaze criterion look implemented while it was not (P6.2).
    /// </summary>
    public static class A11yBuilder
    {
        public const string RootName = "Accessibility";

        [MenuItem("SIGNSPACE/P6 Build accessibility layer")]
        public static void BuildAccessibilityMenu() => BuildAccessibility();

        public static void BuildAccessibility()
        {
            var matcher = Object.FindFirstObjectByType<Matcher>();
            var save = Object.FindFirstObjectByType<SaveSystem>();
            var hand = Object.FindFirstObjectByType<ReferenceHand>();
            var shelfIdle = Object.FindFirstObjectByType<ShelfIdle>();
            var payoff = Object.FindFirstObjectByType<LockPayoff>();
            var cam = Camera.main;

            if (matcher == null || save == null || hand == null)
            {
                Debug.LogError("[A11yBuilder] the scene is missing the matcher, the save system or the " +
                               "reference hand; the accessibility layer was not built.");
                return;
            }

            // Remove a previous build, including the HUD it spawned under its own name.
            foreach (var old in Object.FindObjectsByType<A11ySettings>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (old != null) Object.DestroyImmediate(old.gameObject);
            foreach (var old in Object.FindObjectsByType<A11yHud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (old != null) Object.DestroyImmediate(old.gameObject);

            var root = new GameObject(RootName);
            var settings = root.AddComponent<A11ySettings>();
            settings.save = save;
            settings.matcher = matcher;
            settings.shelfIdle = shelfIdle;
            settings.payoff = payoff;
            settings.referenceHand = hand;

            var calibration = root.AddComponent<Calibration>();
            calibration.matcher = matcher;
            calibration.save = save;
            calibration.referenceHand = hand;

            var probe = root.AddComponent<EyeTrackingProbe>();
            probe.eyeAnchor = null;      // see the class comment: no eye pose exists in this project

            var highlighter = root.AddComponent<GazeHighlighter>();
            highlighter.probe = probe;
            highlighter.xrCamera = cam;
            highlighter.gazeSource = cam != null ? cam.transform : null;
            highlighter.useGroups = false;

            var dwell = root.AddComponent<DwellSelector>();
            dwell.highlighter = highlighter;
            dwell.dwellSeconds = 0.7f;
            // The pace switch scales this value, so the owner has to hold the reference.
            settings.dwell = dwell;

            var router = root.AddComponent<EyesOnlyRouter>();
            router.dwell = dwell;
            router.highlighter = highlighter;
            router.probe = probe;

            var gazeCaption = root.AddComponent<GazeCaption>();
            gazeCaption.highlighter = highlighter;
            gazeCaption.probe = probe;
            gazeCaption.dwell = dwell;
            gazeCaption.settings = settings;

            var audioCaptions = root.AddComponent<A11yCaptionPanel>();
            audioCaptions.matcher = matcher;
            audioCaptions.settings = settings;

            var hud = root.AddComponent<A11yHud>();
            hud.gazeCaption = gazeCaption;
            hud.audioCaptions = audioCaptions;
            hud.dwell = dwell;
            hud.settings = settings;

            var fingers = root.AddComponent<FingerTargetsBuilder>();
            fingers.visual = hand.visual;

            var tray = root.AddComponent<A11yToggleTray>();
            tray.settings = settings;
            tray.calibration = calibration;

            var shapes = root.AddComponent<GazeShapeTargets>();
            shapes.matcher = matcher;
            shapes.referenceHand = hand;

            // The two panels already have a pinch affordance (P3.4, P5.5). Give those same objects a
            // dwell action so the eyes-only path reaches them on the object the user already knows,
            // rather than growing a second, gaze-only control beside it.
            var mirror = Object.FindFirstObjectByType<HonestMirror>();
            var creditsPanel = Object.FindFirstObjectByType<CreditsPanel>();
            int gazePanels = 0;

            // The mirror is opened by a PanelOpener on its plinth (P3.4); that is the object a user
            // already reaches for, so it is the object the gaze path reuses.
            foreach (var opener in Object.FindObjectsByType<PanelOpener>(FindObjectsSortMode.None))
            {
                if (opener == null) continue;
                var action = opener.gameObject.GetComponent<DwellAction>();
                if (action == null) action = opener.gameObject.AddComponent<DwellAction>();
                action.kind = DwellActionKind.ToggleMirror;
                action.mirror = opener.panel != null ? opener.panel : mirror;
                gazePanels++;
            }

            foreach (var handle in Object.FindObjectsByType<CreditsHandle>(FindObjectsSortMode.None))
            {
                if (handle == null) continue;
                var action = handle.gameObject.GetComponent<DwellAction>();
                if (action == null) action = handle.gameObject.AddComponent<DwellAction>();
                action.kind = DwellActionKind.ToggleCredits;
                action.credits = creditsPanel;
                gazePanels++;
            }

            if (gazePanels < 2)
                Debug.LogWarning("[A11yBuilder] only " + gazePanels + " of the 2 panel handles got a gaze " +
                                 "action; the eyes-only path cannot reach both panels.");

            Debug.Log("[A11yBuilder] accessibility layer wired: probe + highlighter + dwell + router + " +
                      "captions + finger targets + " + A11ySettings.All.Length + "-toggle tray + " +
                      "calibration + shape targets.");

            if (cam == null)
                Debug.LogWarning("[A11yBuilder] no Camera.main: the gaze ray, the HUD and the tray will " +
                                 "fall back to fixed positions. The layer is wired but cannot follow a head.");
        }
    }
}
