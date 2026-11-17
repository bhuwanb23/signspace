using System;
using UnityEngine;

namespace Signspace.Gaze
{
    /// <summary>
    /// Resolves what the user is looking at and drives the finger highlight (P6.1).
    ///
    /// Two facts decide the design (GAZE-INPUT.md §6):
    ///   1. Eye tracking is not available on Quest 3 or 3S. On those devices this must fall back to
    ///      head gaze *visibly and honestly* — one line, once — not silently. <see cref="EyeTrackingProbe"/>
    ///      owns the answer; this class only obeys it and republishes the explanation.
    ///   2. Both hands are busy holding and shaping, so gaze is not a shortcut here; it is the only
    ///      input available at the moment the user wants to inspect a finger.
    ///
    /// THREE TARGET GRANULARITIES, ONE RAY
    /// -----------------------------------
    /// A fingertip collider is ~10 mm across and two adjacent fingers are ~15 mm apart at arm's length,
    /// so single-finger picking is at the edge of what a head ray can resolve. Rather than decide the
    /// question in a comment, every finger is its own target AND carries a group index: when
    /// `useGroups` is on, a hit highlights its whole group, so the same scene supports both answers and
    /// the choice is one inspector tick. `FingerTarget.group` default is -1 ("no group"), and the
    /// builder assigns the coarse grouping index/middle/ring+little/thumb when asked.
    ///
    /// This component deliberately does not use ISDK's EyeGazeInteractor. That building block owns
    /// rig switching and device profiles; if it is added later, this class is the seam to replace —
    /// nothing else in the product knows how gaze is obtained.
    /// </summary>
    public class GazeHighlighter : MonoBehaviour
    {
        [Header("Source")]
        [Tooltip("The pose the ray starts from. Eye anchor when eye tracking is live, head otherwise.")]
        public Transform gazeSource;
        public Camera xrCamera;

        [Tooltip("Owns the eye-vs-head answer. When assigned, this component never guesses.")]
        public EyeTrackingProbe probe;

        [Tooltip("Set by EyeTrackingProbe. False on Quest 3 / 3S.")]
        public bool eyeTrackingAvailable;

        [Header("Targets")]
        public LayerMask targetMask = ~0;
        public float maxDistance = 2f;

        [Tooltip("Ignore hits closer than this: the head ray starts inside the user's own hand often.")]
        public float minDistance = 0.08f;

        [Header("Highlight")]
        public Renderer highlightRenderer;          // optional: a rim/halo mesh
        public Color highlightColor = new Color(1f, 0.85f, 0.4f, 1f);
        [Range(0f, 1f)] public float dimOthersTo = 0.35f;

        [Header("Finger groups (fallback when single fingers are unreliable)")]
        public bool useGroups;

        public event Action<int, string> OnFingerFocused;   // fingerIndex, label
        public event Action<GameObject> OnTargetFocused;    // the object, for dwell
        public event Action OnFocusedNothing;

        /// <summary>One line about the fallback, fired when the mode resolves or changes.</summary>
        public event Action<string> OnFallbackExplanation;

        public GameObject CurrentTarget { get; private set; }
        public FingerTarget CurrentFinger { get; private set; }

        /// <summary>The label the in-world caption shows. Empty when nothing is focused.</summary>
        public string FocusLabel { get; private set; } = "";

        /// <summary>Index of the highlighted group, or -1 when a single finger (or nothing) is focused.</summary>
        public int CurrentGroup { get; private set; } = -1;

        /// <summary>True when the highlight is following the head rather than the eyes.</summary>
        public bool IsHeadFallback => !eyeTrackingAvailable;

        /// <summary>The probe's status line, for the debug overlay and the checks.</summary>
        public string ModeLine => probe != null ? probe.StatusLine :
                                  (eyeTrackingAvailable ? "gaze: eye" : "gaze: head");

        MaterialPropertyBlock _mpb;

        void Awake() => _mpb = new MaterialPropertyBlock();

        void Start() => BindProbe();

        /// <summary>
        /// Resolves the source. Public because the scene is built and checked outside play mode, where
        /// Start never runs: the check has to be able to reach the same code the build reaches.
        /// </summary>
        public void BindProbe()
        {
            if (xrCamera == null) xrCamera = Camera.main;

            if (probe != null)
            {
                probe.OnModeChanged -= OnModeChanged;
                probe.OnModeChanged += OnModeChanged;
                probe.OnExplanation -= Explain;
                probe.OnExplanation += Explain;
                if (probe.eyeAnchor != null) gazeSource = probe.eyeAnchor;
                probe.Resolve("highlighter bind");
                OnModeChanged(probe.Mode);
            }
            else
            {
                // No probe: keep the safe default and say so once. A quiet `false` here is how the
                // silently-head-raycasting failure starts, so it is a log, not a field.
                eyeTrackingAvailable = DetectEyeTracking();
                Debug.Log("[GazeHighlighter] no EyeTrackingProbe assigned; assuming head gaze");
            }
        }

        void OnDestroy()
        {
            if (probe != null)
            {
                probe.OnModeChanged -= OnModeChanged;
                probe.OnExplanation -= Explain;
            }
        }

        void OnModeChanged(GazeMode mode)
        {
            // Eye tracking being available is not the same as having an eye POSE to cast from. This
            // project binds no eye anchor (Quest 3 / 3S provide no eye pose at all), so claiming the eye
            // path here would be exactly the silent head-raycast P6.2 exists to prevent.
            bool haveEyePose = probe != null && probe.eyeAnchor != null;
            eyeTrackingAvailable = mode == GazeMode.Eye && haveEyePose;

            if (mode == GazeMode.Eye && !haveEyePose)
                Explain("Eye tracking is available but no eye pose is bound; the highlight follows your head.");
            else if (mode == GazeMode.Head)
                Explain(probe != null ? probe.Explanation : "This headset has no eye tracking. The highlight follows your head.");

            if (mode == GazeMode.None) ClearFocus();
        }

        void Explain(string line)
        {
            Debug.Log("[GazeHighlighter] " + line);
            OnFallbackExplanation?.Invoke(line);
        }

        void Update()
        {
            var src = gazeSource != null ? gazeSource : (xrCamera != null ? xrCamera.transform : null);
            if (src == null) return;

            if (probe != null && probe.Mode == GazeMode.None)
            {
                if (CurrentTarget != null) ClearFocus();
                return;
            }

            GameObject hitTarget = null;
            FingerTarget hitFinger = null;

            // One non-allocating ray per frame. The near plane starts outside the user's own body, and
            // `RaycastAll` would allocate a fresh array every frame for a result we do not need: the
            // first hit IS the nearest one, so the collection never bought anything.
            float near = Mathf.Max(0f, minDistance);
            float far = Mathf.Max(near, maxDistance);
            if (Physics.Raycast(src.position + src.forward * near, src.forward, out var hit,
                                far - near, targetMask, QueryTriggerInteraction.Collide))
            {
                hitFinger = hit.collider.GetComponentInParent<FingerTarget>();
                hitTarget = hitFinger != null ? hitFinger.gameObject : hit.collider.gameObject;
            }

            if (hitTarget == CurrentTarget)
            {
                // The label can change without the object changing (a finger's description follows the
                // current pose), and the dwell must not restart for that. Republish the label only.
                if (hitFinger != null && CurrentFinger == hitFinger)
                {
                    string label = LabelFor(hitFinger);
                    if (label != FocusLabel) FocusLabel = label;
                }
                return;
            }

            CurrentTarget = hitTarget;
            CurrentFinger = hitFinger;
            CurrentGroup = hitFinger != null && useGroups && hitFinger.UsesGroup ? hitFinger.group : -1;

            if (hitFinger != null)
            {
                FocusLabel = LabelFor(hitFinger);
                OnFingerFocused?.Invoke(CurrentGroup >= 0 ? -1 : hitFinger.fingerIndex, FocusLabel);
                OnTargetFocused?.Invoke(hitTarget);
            }
            else if (hitTarget != null)
            {
                // Walking around, you look at the shelf, the wall, your own arm. None of that is a
                // highlight: only a thing that declares an action gets a caption, so the label cannot
                // flicker through object names while the user simply looks around (P6.1 done-when 3).
                var action = hitTarget.GetComponentInParent<DwellAction>();
                FocusLabel = action != null ? action.Label : "";
                OnTargetFocused?.Invoke(hitTarget);
            }
            else
            {
                FocusLabel = "";
                OnFocusedNothing?.Invoke();
            }
        }

        string LabelFor(FingerTarget target)
        {
            if (CurrentGroup >= 0) return "fingers " + CurrentGroup;
            return string.IsNullOrEmpty(target.label) ? "finger " + target.fingerIndex : target.label;
        }

        void ClearFocus()
        {
            if (CurrentTarget == null && FocusLabel.Length == 0) return;
            CurrentTarget = null;
            CurrentFinger = null;
            CurrentGroup = -1;
            FocusLabel = "";
            OnFocusedNothing?.Invoke();
        }

        /// <summary>Applied by the label/halo view. Kept here so the highlight logic is in one place.</summary>
        public void ApplyHighlight(Renderer r, bool on)
        {
            if (r == null) return;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor", on ? highlightColor : new Color(1f, 1f, 1f, dimOthersTo));
            r.SetPropertyBlock(_mpb);
        }

        /// <summary>
        /// The platform question, asked by name. Kept as a static so the probe and the checks share one
        /// implementation; it is deliberately a "definitely yes or no" and never an optimistic default.
        /// </summary>
        public static bool DetectEyeTracking() => EyeTrackingProbe.TryPlatformEyeTracking(out _);
    }
}
