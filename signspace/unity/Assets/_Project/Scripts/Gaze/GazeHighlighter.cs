using System;
using UnityEngine;

namespace Signspace.Gaze
{
    /// <summary>
    /// Resolves what the user is looking at and drives the finger highlight.
    ///
    /// Two facts decide the design (GAZE-INPUT.md §6):
    ///   1. Eye tracking is not available on Quest 3 or 3S. On those devices this must fall back to
    ///      head gaze *visibly and honestly* — one line, once — not silently.
    ///   2. Both hands are busy holding and shaping, so gaze is not a shortcut here; it is the only
    ///      input available at the moment the user wants to inspect a finger.
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
        [Tooltip("Set by EyeTrackingProbe. False on Quest 3 / 3S.")]
        public bool eyeTrackingAvailable;

        [Header("Targets")]
        public LayerMask targetMask = ~0;
        public float maxDistance = 2f;

        [Header("Highlight")]
        public Renderer highlightRenderer;          // optional: a rim/halo mesh
        public Color highlightColor = new Color(1f, 0.85f, 0.4f, 1f);
        [Range(0f, 1f)] public float dimOthersTo = 0.35f;

        [Header("Finger groups (fallback when single fingers are unreliable)")]
        public bool useGroups;

        public event Action<int, string> OnFingerFocused;   // fingerIndex, label
        public event Action<GameObject> OnTargetFocused;    // the object, for dwell
        public event Action OnFocusedNothing;

        public GameObject CurrentTarget { get; private set; }

        /// <summary>True when the highlight is following the head rather than the eyes.</summary>
        public bool IsHeadFallback => !eyeTrackingAvailable;

        MaterialPropertyBlock _mpb;

        void Awake() => _mpb = new MaterialPropertyBlock();

        void Update()
        {
            var src = gazeSource != null ? gazeSource : (xrCamera != null ? xrCamera.transform : null);
            if (src == null) return;

            GameObject hitTarget = null;
            FingerTarget hitFinger = null;

            if (Physics.Raycast(src.position, src.forward, out var hit, maxDistance, targetMask,
                                QueryTriggerInteraction.Collide))
            {
                hitFinger = hit.collider.GetComponentInParent<FingerTarget>();
                hitTarget = hitFinger != null ? hitFinger.gameObject : hit.collider.gameObject;
            }

            if (hitTarget == CurrentTarget) return;

            CurrentTarget = hitTarget;

            if (hitFinger != null)
            {
                if (useGroups && hitFinger.UsesGroup)
                    OnFingerFocused?.Invoke(-1, "fingers " + hitFinger.group);
                else
                    OnFingerFocused?.Invoke(hitFinger.fingerIndex, hitFinger.label);
                OnTargetFocused?.Invoke(hitTarget);
            }
            else if (hitTarget != null)
            {
                OnTargetFocused?.Invoke(hitTarget);
            }
            else
            {
                OnFocusedNothing?.Invoke();
            }
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
        /// VERIFY ON DEVICE: replace with the SDK's eye-tracking availability check.
        /// Returning false is the safe default — head gaze with a larger dwell target always works,
        /// whereas claiming eye tracking we do not have is the failure mode UX-STATES.md forbids.
        /// </summary>
        public static bool DetectEyeTracking() => false;
    }
}
