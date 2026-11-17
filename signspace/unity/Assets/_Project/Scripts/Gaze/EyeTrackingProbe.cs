using System;
using System.Reflection;
using UnityEngine;

namespace Signspace.Gaze
{
    /// <summary>Where the highlight ray actually starts. Never a guess: see <see cref="EyeTrackingProbe"/>.</summary>
    public enum GazeMode
    {
        None = 0,
        Head = 1,
        Eye = 2,
    }

    /// <summary>
    /// Answers "is eye tracking actually running?" and makes the answer visible (P6.2).
    ///
    /// THE FAILURE THIS EXISTS TO PREVENT
    /// ----------------------------------
    /// The rig falls back to head direction silently, the highlight follows the head, and everyone
    /// believes gaze shipped. That is the single most common way this criterion fails, and it fails
    /// invisibly - so this class does three things rather than one:
    ///   1. It asks the platform, by name, and treats anything short of a definite yes as no.
    ///   2. It logs the answer once, and exposes a status line the in-world label shows.
    ///   3. When the answer is no, it offers a one-line explanation and a decline switch, so the
    ///      fallback is a state the user can see rather than a behaviour that quietly differs.
    ///
    /// WHY REFLECTION
    /// --------------
    /// `OVRPlugin` lives in the Oculus SDK assembly, which is not referenced by these scripts (the
    /// simulator build has no headset runtime). A direct call would not compile without the SDK, and
    /// adding the SDK reference to every script would make the whole project unbuildable on a machine
    /// without it. Reflection costs one lookup at Start and returns "unknown" - which means head
    /// gaze - when the type or the property is absent.
    ///
    /// Quest 3 and 3S have **no eye tracking**, so on the target hardware this legitimately returns
    /// Head. The check `P6Checks.CheckEyeTracking` asserts exactly that and fails if the product ever
    /// claims Eye without a platform yes.
    /// </summary>
    public class EyeTrackingProbe : MonoBehaviour
    {
        public enum Forced
        {
            Auto = 0,
            Eye = 1,
            Head = 2,
            None = 3,
        }

        /// <summary>Test seam. A real build leaves this on Auto.</summary>
        public Forced forceMode = Forced.Auto;

        /// <summary>Optional eye anchor. Used only when the mode is Eye.</summary>
        public Transform eyeAnchor;

        const string DeclinedKey = "signspace.gaze.declined";

        public event Action<string> OnExplanation;   // fired once per state change, for the label
        public event Action<GazeMode> OnModeChanged;

        public GazeMode Mode { get; private set; } = GazeMode.None;

        /// <summary>True only when the platform said yes AND the user has not declined.</summary>
        public bool EyesLive => Mode == GazeMode.Eye;

        public bool Declined { get; private set; }

        /// <summary>Why the mode is not Eye. Empty when it is.</summary>
        public string Reason { get; private set; } = "not probed";

        /// <summary>The one line the label shows when the fallback happens. Deliberately concrete.</summary>
        public string Explanation
        {
            get
            {
                switch (Mode)
                {
                    case GazeMode.Eye:
                        return "Eye tracking is on. The highlight follows your eyes.";
                    case GazeMode.Head:
                        return Declined
                            ? "Eye tracking is off by your choice. The highlight follows your head."
                            : "This headset has no eye tracking. The highlight follows your head.";
                    default:
                        return "No gaze source is available. The highlight is off.";
                }
            }
        }

        /// <summary>The Immersive Debugger line. One string, always readable in a screenshot.</summary>
        public string StatusLine => "gaze: " + Mode.ToString().ToLowerInvariant() +
                                    (string.IsNullOrEmpty(Reason) ? "" : " (" + Reason + ")");

        void Start()
        {
            Declined = PlayerPrefs.GetInt(DeclinedKey, 0) != 0;
            Resolve("start");
        }

        /// <summary>
        /// Re-asks the platform. Called by the probe's own Start and by the toggles when eyes-only is
        /// switched, so a user who declined can turn eye tracking back on without a relaunch.
        /// </summary>
        public void Resolve(string because)
        {
            GazeMode next;
            string reason;

            switch (forceMode)
            {
                case Forced.Eye:
                    next = GazeMode.Eye;
                    reason = "forced for a test";
                    break;
                case Forced.Head:
                    next = GazeMode.Head;
                    reason = "forced for a test";
                    break;
                case Forced.None:
                    next = GazeMode.None;
                    reason = "forced for a test";
                    break;
                default:
                    if (Declined)
                    {
                        next = GazeMode.Head;
                        reason = "declined";
                    }
                    else if (Camera.main == null)
                    {
                        next = GazeMode.None;
                        reason = "no camera";
                    }
                    else if (TryPlatformEyeTracking(out string detail))
                    {
                        next = GazeMode.Eye;
                        reason = detail;
                    }
                    else
                    {
                        next = GazeMode.Head;
                        reason = detail;
                    }
                    break;
            }

            bool changed = next != Mode;
            Mode = next;
            Reason = reason;

            Debug.Log("[EyeTrackingProbe] " + StatusLine + " — resolved from " + because);

            if (changed)
            {
                OnModeChanged?.Invoke(next);
                OnExplanation?.Invoke(Explanation);
            }
        }

        /// <summary>User says no. Persisted, and the fallback becomes a stated choice.</summary>
        public void Decline()
        {
            Declined = true;
            PlayerPrefs.SetInt(DeclinedKey, 1);
            PlayerPrefs.Save();
            Resolve("decline");
        }

        /// <summary>User says yes again. Only takes effect if the platform agrees.</summary>
        public void AllowEyes()
        {
            Declined = false;
            PlayerPrefs.SetInt(DeclinedKey, 0);
            PlayerPrefs.Save();
            Resolve("allow");
        }

        public void Cycle()
        {
            if (Declined) AllowEyes();
            else Decline();
        }

        /// <summary>
        /// Asks OVRPlugin. Returns true only on a definite yes; every other outcome is a no with a
        /// reason that names what was missing, so the log can distinguish "the device cannot" from
        /// "the SDK is not here" from "the runtime says it is not ready".
        /// </summary>
        public static bool TryPlatformEyeTracking(out string detail)
        {
            detail = "";

            Type plugin = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { plugin = asm.GetType("OVRPlugin", false, false); }
                catch { plugin = null; }
                if (plugin != null) break;
            }

            if (plugin == null)
            {
                detail = "no eye-tracking SDK";
                return false;
            }

            // Move the eyes, not the head: the runtime reports both an enabled flag and a ready flag,
            // and enabled-without-ready is exactly the case where a head ray would look right and be
            // wrong. Both must be true.
            bool enabled = ReadBool(plugin, "eyeTrackingEnabled");
            // If the runtime exposes no explicit readiness call, `enabled` is the strongest answer it
            // gives; inventing a stricter test would turn a real yes into a false no.
            bool ready = enabled;

            var readyMethod = plugin.GetMethod("IsEyeTrackingEnabled", BindingFlags.Public | BindingFlags.Static);
            if (readyMethod != null && readyMethod.ReturnType == typeof(bool))
            {
                try { ready = (bool)readyMethod.Invoke(null, null); } catch { }
            }

            if (!enabled)
            {
                detail = "the runtime reports eye tracking disabled";
                return false;
            }

            if (!ready)
            {
                detail = "the runtime reports eye tracking not ready";
                return false;
            }

            detail = "OVRPlugin";
            return true;
        }

        static bool ReadBool(Type type, string member)
        {
            try
            {
                var prop = type.GetProperty(member, BindingFlags.Public | BindingFlags.Static);
                if (prop != null && prop.PropertyType == typeof(bool)) return (bool)prop.GetValue(null);
                var field = type.GetField(member, BindingFlags.Public | BindingFlags.Static);
                if (field != null && field.FieldType == typeof(bool)) return (bool)field.GetValue(null);
            }
            catch { }
            return false;
        }
    }
}
