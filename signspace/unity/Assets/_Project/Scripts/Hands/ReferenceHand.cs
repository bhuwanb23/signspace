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

        /// <summary>
        /// WHICH of the three rotation variants is active. P1.3 is a comparison of these three, and
        /// the comparison only means anything if they run in the same build against the same hand
        /// mesh, switched at the moment of testing rather than across three separate builds.
        ///
        ///   A · Direct    the object's rotation IS the wrist's rotation, scaled 1:1. No smoothing,
        ///                 no lag. If this feels right, the gesture is as immediate as a real object.
        ///   B · Damped    1:1 with an exponential ease (this was the only behaviour the file had
        ///                 before P1.3, hard-coded, never compared against anything).
        ///   C · HoldStill the object's POSITION is frozen at the grab point, and rotation is applied
        ///                 only while the hand is not moving through space. The bet: users can twist
        ///                 a wrist much more precisely than they can hold a hand still mid-air, so
        ///                 keying rotation off the wrist removes the position noise from the input.
        /// </summary>
        public enum RotationMode
        {
            Direct = 0,
            Damped = 1,
            HoldStill = 2
        }

        [Tooltip("Which of the three P1.3 variants is active. Change it at runtime to compare.")]
        public RotationMode rotationMode = RotationMode.Damped;

        [Tooltip("HoldStill only: wrist speed, in metres per second, below which rotation is applied. " +
                 "Above it the object keeps its angle - that is the whole idea of the variant.")]
        public float holdSillSpeed = 0.12f;

        [Tooltip("The typed tracked hand. Needed for wrist POSITION (HoldStill) as well as rotation; " +
                 "the MonoBehaviour provider above is typed only to the interface.")]
        public MetaHandSource trackedSource;

        /// <summary>True while the user is holding the reference hand.</summary>
        [Tooltip("P6.5 one-handed mode: the reference hand follows the tracked wrist with no pinch, so " +
                 "the whole loop runs with one hand. Set by A11ySettings.")]
        public bool oneHanded;

        public bool IsGrabbed { get; private set; }

        /// <summary>
        /// The rotation the wrist is asking for this frame, in world space. Exposed so the trial
        /// controller can measure LAG: the angle between what was asked for and what the object has.
        /// </summary>
        public Quaternion TargetRotation { get; private set; }

        /// <summary>Current angular distance between the object and its target, in degrees.</summary>
        public float LagDegrees { get; private set; }

        /// <summary>How far the object has drifted from where it was when grabbed, in metres.</summary>
        public float PositionDrift => Vector3.Distance(pivot.position, _pivotPositionAtGrab);

        /// <summary>HoldStill only: true while the hand is slow enough for rotation to be applied.</summary>
        public bool IsApplyingRotation { get; private set; } = true;

        Quaternion _grabOffset;        // wrist rotation at grab time, relative to the pivot
        Quaternion _pivotAtGrab;
        Vector3 _pivotPositionAtGrab;
        Vector3 _lastWristPosition;
        float _wristSpeed;
        float _idleAngle;
        bool _idleDirection = true;

        void Start()
        {
            if (pivot == null) pivot = transform;
            if (visual != null && visual.joints == null) visual.Build();
            PlaceInFront();
            _pivotPositionAtGrab = pivot.position;
            TargetRotation = pivot.rotation;
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
            UpdateWristSpeed();
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
            else if (oneHanded)
            {
                PostOneHanded();
            }
            else
            {
                IdleDrift();
            }
        }

        /// <summary>
        /// One-handed mode: the hand is always at the wrist's rotation, offset-free, so shaping works
        /// without a pinch on the reference hand. Position is never written - the hand stays where the
        /// plinth put it, in front of the user, because moving it to the wrist would drag it out of the
        /// comfortable cone on every reach.
        /// </summary>
        void PostOneHanded()
        {
            Quaternion target = WristRotation;
            TargetRotation = target;
            pivot.rotation = Quaternion.Slerp(pivot.rotation, target,
                                              1f - Mathf.Exp(-followSmoothness * Time.deltaTime));
            IsApplyingRotation = true;
            LagDegrees = Quaternion.Angle(pivot.rotation, target);
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

        /// <summary>
        /// Refreshes the hand's speed. HoldStill needs it, and it is cheap: one distance per frame.
        /// Speed is smoothed because a tracker's position jitters by a millimetre or two even when a
        /// hand is perfectly still, and an unsmoothed threshold would flicker rotation on and off at
        /// exactly the moment the user is trying to be precise.
        /// </summary>
        void UpdateWristSpeed()
        {
            if (!HasWrist) { _wristSpeed = 0f; return; }
            Vector3 p = WristPosition;
            if (Time.deltaTime > 0f)
            {
                float instant = Vector3.Distance(p, _lastWristPosition) / Time.deltaTime;
                _wristSpeed = Mathf.Lerp(_wristSpeed, instant, 1f - Mathf.Exp(-12f * Time.deltaTime));
            }
            _lastWristPosition = p;
        }

        bool HasWrist =>
            (trackedSource != null && trackedSource.IsTracked) ||
            (trackedHand != null && trackedHand.joints != null);

        /// <summary>
        /// The wrist's world position. The typed source wins because it has the real joint; the
        /// fallback is this object's own transform, which is where a simulator or a missing rig
        /// leaves us - and a constant position simply means "never moving", which HoldStill needs
        /// to behave sanely rather than spin.
        /// </summary>
        Vector3 WristPosition =>
            trackedSource != null && trackedSource.IsTracked ? trackedSource.WristPosition : transform.position;

        Quaternion WristRotation =>
            trackedSource != null && trackedSource.IsTracked
                ? trackedSource.WristRotation
                : (trackedHand != null ? trackedHand.WristRotation : Quaternion.identity);

        /// <summary>
        /// The three variants, in one place so that what differs between them is visible: only the
        /// line that writes pivot.rotation, and what feeds it.
        /// </summary>
        void FollowWrist()
        {
            Quaternion target = WristRotation * _grabOffset;
            TargetRotation = target;

            switch (rotationMode)
            {
                case RotationMode.Direct:
                    // The object IS the wrist. Any lag here is the tracker's, not ours.
                    pivot.rotation = target;
                    IsApplyingRotation = true;
                    break;

                case RotationMode.Damped:
                    pivot.rotation = Quaternion.Slerp(
                        pivot.rotation, target,
                        1f - Mathf.Exp(-followSmoothness * Time.deltaTime));
                    IsApplyingRotation = true;
                    break;

                case RotationMode.HoldStill:
                    // Position is never written in this mode - it stays exactly where it was grabbed.
                    IsApplyingRotation = _wristSpeed <= holdSillSpeed;
                    if (IsApplyingRotation)
                    {
                        pivot.rotation = Quaternion.Slerp(
                            pivot.rotation, target,
                            1f - Mathf.Exp(-followSmoothness * Time.deltaTime));
                    }
                    break;
            }

            LagDegrees = Quaternion.Angle(pivot.rotation, target);
        }

        /// <summary>
        /// Places the reference hand in front of the camera at the given distance and resets the
        /// comparison state. P1.3 tests at arm's length (0.4 m) and lap distance (0.6 m); switching
        /// must not carry a stale grab offset or drift reference across, or the second distance is
        /// measured with the first one's numbers.
        /// </summary>
        public void SetDistance(float metres)
        {
            if (pivot == null) pivot = transform;   // called from the editor before Start
            distance = Mathf.Clamp(metres, 0.15f, 1.2f);
            IsGrabbed = false;
            _grabOffset = Quaternion.identity;
            _idleAngle = 0f;
            PlaceInFront();
            _pivotPositionAtGrab = pivot.position;
            TargetRotation = pivot.rotation;
            LagDegrees = 0f;
        }

        /// <summary>Switches variant and clears the per-variant state, so a trial starts clean.</summary>
        public void SetMode(RotationMode mode)
        {
            if (pivot == null) pivot = transform;
            rotationMode = mode;
            IsGrabbed = false;
            _grabOffset = Quaternion.identity;
            _pivotPositionAtGrab = pivot.position;
            LagDegrees = 0f;
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
