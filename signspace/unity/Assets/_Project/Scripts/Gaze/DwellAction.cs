using UnityEngine;
using Signspace.Core;
using Signspace.Hands;
using Signspace.Matching;
using Signspace.UI;

namespace Signspace.Gaze
{
    public enum DwellActionKind
    {
        SelectShape = 0,
        OpenMirror = 1,
        CloseMirror = 2,
        OpenCredits = 3,
        CloseCredits = 4,
        ToggleNearMiss = 5,
        ToggleA11y = 6,
        ResetPose = 7,
        StartCalibration = 8,
        ToggleMirror = 9,
        ToggleCredits = 10,
    }

    /// <summary>
    /// An object a gaze-dwell can actuate (P6.3). Put it on the thing you want to be reachable
    /// without a pinch: a shelf silhouette, the mirror handle's plate, a toggle block.
    ///
    /// WHY A COMPONENT AND NOT A SWITCH IN THE ROUTER
    /// ----------------------------------------------
    /// The router must not know what every object in the scene means. The object declares its own
    /// action, so adding a gaze-reachable thing later is one component and no router edit. It is also
    /// what makes the P6 check able to ask "is every action in the loop mapped?" by enumerating
    /// components rather than by reading a list the code could drift from.
    ///
    /// Every action here is *also* reachable by pinch. Nothing is gaze-only except the dwell itself,
    /// so a user with limited mobility gains a path and a pinch user loses nothing.
    /// </summary>
    public class DwellAction : MonoBehaviour
    {
        public DwellActionKind kind = DwellActionKind.SelectShape;

        [Header("SelectShape")]
        public string shapeId = "";
        public Matcher matcher;
        public ReferenceHand referenceHand;

        [Header("Panels")]
        public HonestMirror mirror;
        public CreditsPanel credits;

        [Header("Switches")]
        public A11yToggle toggle = A11yToggle.Captions;

        [Header("Calibration")]
        public Calibration calibration;

        /// <summary>Last outcome, for the in-world caption and the checks. Never a silent no.</summary>
        public string LastResult { get; private set; } = "not used yet";

        /// <summary>How many times this action has actuated. The checks assert on it.</summary>
        public int Fired { get; private set; }

        public string Label
        {
            get
            {
                switch (kind)
                {
                    case DwellActionKind.SelectShape: return "make " + (string.IsNullOrEmpty(shapeId) ? "this shape" : shapeId);
                    case DwellActionKind.OpenMirror: return "open what the sensor cannot see";
                    case DwellActionKind.CloseMirror: return "close";
                    case DwellActionKind.OpenCredits: return "open credits";
                    case DwellActionKind.CloseCredits: return "close credits";
                    case DwellActionKind.ToggleNearMiss: return "near-miss naming";
                    case DwellActionKind.ToggleA11y:
                        return A11ySettings.Title(toggle);
                    case DwellActionKind.ResetPose: return "neutral hand";
                    case DwellActionKind.StartCalibration: return "calibrate my hand";
                    case DwellActionKind.ToggleMirror:
                        return mirror != null && MirrorOpen ? "close" : "open what the sensor cannot see";
                    case DwellActionKind.ToggleCredits:
                        return credits != null && credits.IsOpen ? "close credits" : "open credits";
                    default: return kind.ToString();
                }
            }
        }

        public void Trigger()
        {
            switch (kind)
            {
                case DwellActionKind.SelectShape: Select(); break;
                case DwellActionKind.OpenMirror: Panel(mirror, true); break;
                case DwellActionKind.CloseMirror: Panel(mirror, false); break;
                case DwellActionKind.OpenCredits: Credits(true); break;
                case DwellActionKind.CloseCredits: Credits(false); break;
                case DwellActionKind.ToggleNearMiss:
                    var settings = A11ySettings.Instance;
                    if (settings != null) settings.Toggle(A11yToggle.NearMiss);
                    else if (matcher != null) matcher.NearMissEnabled = !matcher.NearMissEnabled;
                    LastResult = "near-miss " + (matcher != null && matcher.NearMissEnabled ? "on" : "off");
                    break;
                case DwellActionKind.ToggleA11y:
                    if (A11ySettings.Instance != null)
                    {
                        A11ySettings.Instance.Toggle(toggle);
                        LastResult = A11ySettings.Title(toggle) + " " + A11ySettings.Instance.ValueLabel(toggle);
                    }
                    else LastResult = "no settings owner";
                    break;
                case DwellActionKind.ResetPose:
                    if (referenceHand != null) referenceHand.ShowNeutral();
                    LastResult = "neutral";
                    break;
                case DwellActionKind.StartCalibration:
                    if (calibration != null) calibration.Begin();
                    LastResult = calibration != null ? "calibration started" : "no calibration component";
                    break;
                case DwellActionKind.ToggleMirror:
                    if (mirror != null) mirror.Toggle();
                    LastResult = mirror != null ? (MirrorOpen ? "mirror open" : "mirror closed") : "no mirror";
                    break;
                case DwellActionKind.ToggleCredits:
                    if (credits != null) credits.Toggle();
                    LastResult = credits != null ? (credits.IsOpen ? "credits open" : "credits closed") : "no credits panel";
                    break;
            }

            Fired++;
            Debug.Log("[DwellAction] " + kind + " -> " + LastResult);
        }

        bool MirrorOpen => mirror != null && mirror.content != null && mirror.content.activeSelf;

        void Select()
        {
            if (matcher == null || string.IsNullOrEmpty(shapeId))
            {
                LastResult = "nothing to select";
                return;
            }

            var pose = FindPose(shapeId);
            if (pose == null)
            {
                LastResult = "shape '" + shapeId + "' is not loaded";
                return;
            }

            matcher.SetTarget(pose.Id);
            if (referenceHand != null) referenceHand.ShowPose(pose.LocalRotations);
            LastResult = "target " + pose.Id;
        }

        HandPose FindPose(string id)
        {
            if (matcher == null || matcher.shapes == null) return null;
            for (int i = 0; i < matcher.shapes.Count; i++)
            {
                var p = matcher.shapes[i];
                if (p != null && string.Equals(p.Id, id, System.StringComparison.OrdinalIgnoreCase)) return p;
            }
            return null;
        }

        void Panel(HonestMirror m, bool open)
        {
            if (m == null) { LastResult = "no mirror"; return; }
            if (open) m.Open(); else m.Close();
            LastResult = open ? "mirror open" : "mirror closed";
        }

        void Credits(bool open)
        {
            if (credits == null) { LastResult = "no credits panel"; return; }
            if (open) credits.Open(); else credits.Close();
            LastResult = open ? "credits open" : "credits closed";
        }

        /// <summary>
        /// True when the action has everything it needs. The checks call this so a half-wired action is
        /// a failure at build time instead of a dwell that does nothing on device.
        /// </summary>
        public bool IsWired
        {
            get
            {
                switch (kind)
                {
                    case DwellActionKind.SelectShape: return matcher != null && !string.IsNullOrEmpty(shapeId);
                    case DwellActionKind.OpenMirror:
                    case DwellActionKind.CloseMirror: return mirror != null;
                    case DwellActionKind.OpenCredits:
                    case DwellActionKind.CloseCredits: return credits != null;
                    case DwellActionKind.ToggleNearMiss: return matcher != null || A11ySettings.Instance != null;
                    case DwellActionKind.ToggleA11y: return A11ySettings.Instance != null;
                    case DwellActionKind.ResetPose: return referenceHand != null;
                    case DwellActionKind.StartCalibration: return calibration != null;
                    case DwellActionKind.ToggleMirror: return mirror != null;
                    case DwellActionKind.ToggleCredits: return credits != null;
                    default: return false;
                }
            }
        }
    }
}
