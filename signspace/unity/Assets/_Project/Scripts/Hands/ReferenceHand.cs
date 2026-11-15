using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>
    /// The reference hand: the thing you grab and turn with your own wrist.
    ///
    /// This is beat 2 of the concept and it is the three-second demo. "You cannot rotate a video"
    /// is only true if the thing in front of you moves when YOUR hand moves, and the hand you move
    /// is the one that is busy holding it.
    ///
    /// HOW THE ROTATION WORKS
    /// ---------------------
    /// The reference hand is NOT tracked - it is our geometry, driven by the pose file. When the user
    /// grabs it, its orientation follows the tracked hand's WRIST rotation, measured relative to the
    /// wrist rotation at the moment of the grab. Measuring relatively means there is no jump when
    /// the grab starts: the hand stays where it is and starts following.
    ///
    /// So: turn your wrist, the reference hand turns with it. That is the whole trick.
    ///
    /// Grab detection is delegated: assign a component that implements IHandStateProvider to
    /// read the SDK's pinch. Without one, the reference hand follows the wrist continuously, which
    /// still demonstrates the mechanic but loses the "you have to hold it" affordance.
    /// </summary>
    public class ReferenceHand : MonoBehaviour
    {
        [Header("Parts")]
        public HandVisual visual;
        public Transform pivot;

        [Header("Where it comes from")]
        [Tooltip("The tracked hand. Supplies the wrist rotation to follow.")]
        public SkeletonPoseSource trackedHand;

        [Tooltip("Optional. Implements IHandStateProvider to read the SDK's pinch state.")]
        public MonoBehaviour handStateProvider;

        [Header("Motion")]
        [Tooltip("Degrees per second the reference hand drifts when it is not being held.")]
        public float idleDegreesPerSecond = 2f;

        [Tooltip("How far the idle drift is allowed to wander, in degrees.")]
        public float idleRangeDegrees = 6f;

        [Tooltip("Smoothness of the follow. Lower is snappier.")]
        public float followSmoothness = 12f;

        [Header("Placement")]
        [Tooltip("Metres in front of the camera. 0.40 is the design value.")]
        public float distance = 0.40f;

        /// <summary>True while the user is holding the reference hand.</summary>
        public bool IsGrabbed { get; private set; }

        Quaternion _grabOffset;        // wrist rotation at grab time, relative to the pivot
        Quaternion _pivotAtGrab;
        float _idleAngle;
        bool _idleDirection = true;

        void Start()
        {
            if (pivot == null) pivot = transform;
            if (visual != null && visual.joints == null) visual.Build();
            PlaceInFront();
        }

        void PlaceInFront()
        {
            var cam = Camera.main;
            if (cam == null) return;
            transform.position = cam.transform.position + cam.transform.forward * distance;
            transform.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
        }

        /// <summary>Shows a handshape on the reference hand.</summary>
        public void ShowPose(Quaternion[] canonicalLocalRotations)
        {
            if (visual != null) visual.ApplyPose(canonicalLocalRotations);
        }

        /// <summary>Back to the neutral open hand.</summary>
        public void ShowNeutral()
        {
            if (visual != null) visual.ApplyNeutral();
        }

        void Update()
        {
            bool pinching = IsPinching();

            if (pinching && !IsGrabbed)
            {
                Grab();
            }
            else if (!pinching && IsGrabbed)
            {
                Release();
            }

            if (IsGrabbed)
            {
                FollowWrist();
            }
            else
            {
                IdleDrift();
            }
        }

        void Grab()
        {
            IsGrabbed = true;
            _pivotAtGrab = pivot.rotation;
            if (trackedHand != null)
            {
                _grabOffset = Quaternion.Inverse(trackedHand.WristRotation) * pivot.rotation;
            }
            else
            {
                _grabOffset = Quaternion.identity;
            }
        }

        void Release()
        {
            IsGrabbed = false;
            _idleAngle = 0f;
        }

        void FollowWrist()
        {
            if (trackedHand == null) return;
            Quaternion target = trackedHand.WristRotation * _grabOffset;
            pivot.rotation = Quaternion.Slerp(pivot.rotation, target, 1f - Mathf.Exp(-followSmoothness * Time.deltaTime));
        }

        void IdleDrift()
        {
            // a very slight drift, so the hand reads as an object in the room rather than a picture
            _idleAngle += idleDegreesPerSecond * Time.deltaTime * (_idleDirection ? 1f : -1f);
            if (Mathf.Abs(_idleAngle) > idleRangeDegrees)
            {
                _idleAngle = Mathf.Sign(_idleAngle) * idleRangeDegrees;
                _idleDirection = !_idleDirection;
            }
            pivot.localRotation = Quaternion.Euler(0f, _idleAngle, 0f);
        }

        bool IsPinching()
        {
            var provider = handStateProvider as SkeletonPoseSource.IHandStateProvider;
            if (provider != null) return provider.IsPinching;
            return false;
        }
    }
}
