using UnityEngine;
using Signspace.Core;

namespace Signspace.Gaze
{
    /// <summary>
    /// Routes a completed dwell to the object's <see cref="DwellAction"/> when eyes-only is on (P6.3).
    ///
    /// WHY THE ROUTER IS A SEPARATE OBJECT FROM THE DWELL
    /// --------------------------------------------------
    /// `DwellSelector` knows how long you looked; it must not know whether looking means "open the
    /// mirror" today. Keeping the mapping here means eyes-only can be switched off without the dwell
    /// timer being disabled - the dwell ring still fills, because it is also the pinch-alternative's
    /// progress display - and it means the checks can call <see cref="TryDispatch"/> with a synthetic
    /// target instead of simulating gaze.
    ///
    /// WHAT EYES-ONLY DOES NOT DO
    /// --------------------------
    /// It does not lock a shape. Locking IS the measured skill - the product teaches a handshape, and
    /// a gaze-only unlock would record progress the hand never made. So the eyes-only path covers every
    /// control around the loop (select, inspect, panels, switches, calibration) and the lock stays a
    /// physical act. ACCESSIBILITY.md §6 states that gap rather than hiding it.
    /// </summary>
    public class EyesOnlyRouter : MonoBehaviour
    {
        public DwellSelector dwell;
        public GazeHighlighter highlighter;

        [Tooltip("Optional: used only to report which source the path is running on.")]
        public EyeTrackingProbe probe;

        /// <summary>How many dwells have actuated. The checks assert on it.</summary>
        public int HandledCount { get; private set; }

        /// <summary>Last action name and outcome, for the caption and the checks.</summary>
        public string LastAction { get; private set; } = "none";

        public bool IsActive
        {
            get
            {
                var s = A11ySettings.Instance;
                return s != null && s.Data.eyesOnly;
            }
        }

        public string ModeLine => probe != null ? probe.StatusLine : "gaze: unprobed";

        void OnEnable()
        {
            if (dwell != null) dwell.OnDwellComplete += OnDwell;
        }

        void OnDisable()
        {
            if (dwell != null) dwell.OnDwellComplete -= OnDwell;
        }

        void OnDwell(GameObject target)
        {
            if (!IsActive) return;
            TryDispatch(target);
        }

        /// <summary>
        /// Actuates the action on <paramref name="target"/> or its parents. Returns false when there is
        /// no action there - a finger highlight or the empty room - which is the common case and must
        /// stay silent.
        /// </summary>
        public bool TryDispatch(GameObject target)
        {
            if (target == null) return false;
            // Eyes-only is a mode, not an always-on input. A pinch-reachable block must not also fire
            // because the user happened to look at it for a second with eyes-only switched off.
            if (!IsActive) return false;

            var action = target.GetComponentInParent<DwellAction>();
            if (action == null) return false;

            if (!action.IsWired)
            {
                LastAction = action.kind + " (not wired)";
                Debug.LogWarning("[EyesOnlyRouter] '" + action.kind + "' on " + action.name +
                                 " is not wired; the dwell did nothing.");
                return false;
            }

            action.Trigger();
            HandledCount++;
            LastAction = action.kind + ": " + action.LastResult;
            return true;
        }
    }
}
