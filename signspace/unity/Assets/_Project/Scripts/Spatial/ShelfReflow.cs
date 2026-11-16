using UnityEngine;
using Signspace.Core;

namespace Signspace.Spatial
{
    /// <summary>
    /// Shows the same twelve slots on a 70°×66° display (P4.6).
    ///
    /// A grid that reads beautifully at 110° does not "get smaller" at 70°: its edges leave the
    /// field, and a shelf whose ends you cannot see is a shelf whose ends you cannot tap. So the
    /// presentation changes, not the objects: six columns fold into four (three rows), the tray tilts
    /// up 20° and comes 4 cm closer, and the plate narrows to match. Every object keeps its exact
    /// scale, because the trap P4.6 names is that scaling the shelf shrinks the tap targets.
    ///
    /// WHAT IS MEASURED
    ///   HorizontalFovDegrees   derived from the eye camera's vertical FoV and aspect, or forced by
    ///                          `overrideHorizontalFovDegrees` in a headless check
    ///   HeadDistanceMeters     how far the shelf is from the head, along the shelf's forward axis
    ///   AngularWidthDegrees    the grid's full horizontal angle from the head's position
    ///   MaxSlotAngleDegrees    the widest slot's horizontal offset, which is what the safe cone limits
    ///
    /// The layout is NOT persisted: `layoutVariant` records the grid the save was written against,
    /// and the display decides how it is presented today. That keeps one save valid on both devices.
    /// </summary>
    public class ShelfReflow : MonoBehaviour
    {
        [Tooltip("The shelf to re-arrange.")]
        public ShelfAnchor shelf;

        [Tooltip("The user's head. Left empty, the shelf's own userHead is used.")]
        public Transform head;

        [Tooltip("The eye camera. Left empty, one is found under the head.")]
        public Camera eye;

        [Header("Measured profile")]
        [Tooltip("Below this horizontal FoV the shelf folds into the tray. The VR Glasses profile is 70°; " +
                 "Quest 3S is ~110°.")]
        public float narrowFovDegrees = 85f;
        [Tooltip("Force a field of view. 0 = measure the eye camera. Used by the headless check.")]
        public float overrideHorizontalFovDegrees = 0f;

        [Header("Layout")]
        public int wideColumns = 6;
        public int trayColumns = 4;
        public float trayTiltDegrees = 20f;
        public float trayPullInMeters = 0.04f;

        [Header("Safe cone")]
        [Tooltip("Everything critical must be inside this cone. P4.6 uses ~55°.")]
        public float safeConeDegrees = 55f;
        [Tooltip("Draw the cone in the editor/simulator for the video. Never on by default.")]
        public bool showSafeCone = false;

        [Tooltip("Apply automatically at launch. Off leaves the layout alone until Evaluate is called.")]
        public bool autoApply = true;

        public float HorizontalFovDegrees { get; private set; }
        public float HeadDistanceMeters { get; private set; }
        public float AngularWidthDegrees { get; private set; }
        public float MaxSlotAngleDegrees { get; private set; }
        public bool IsTray { get; private set; }
        public int CurrentColumns => shelf != null ? shelf.slotColumns : 0;
        public string LastDecision { get; private set; } = "not evaluated";

        bool _applied;
        bool _waitingLogged;
        float _nextCheck;
        Vector3 _lastHeadLocal;

        void Start()
        {
            if (shelf == null) shelf = FindFirstObjectByType<ShelfAnchor>();
            if (shelf != null && head == null) head = shelf.userHead;
            ResolveEye();
        }

        void Update()
        {
            if (!autoApply || shelf == null) return;
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 0.5f;

            // The shelf restores on a coroutine that waits for MRUK. Reflowing before it has decided
            // would arrange slots around a saved transform that does not exist yet.
            if (shelf.Result == ShelfAnchor.Outcome.NotStarted)
            {
                if (!_waitingLogged)
                {
                    _waitingLogged = true;
                    LastDecision = "waiting for the shelf to resolve";
                }
                return;
            }

            if (head == null) head = shelf.userHead;
            ResolveEye();
            if (head == null) return;

            Vector3 local = shelf.transform.InverseTransformPoint(head.position);
            if (_applied && (local - _lastHeadLocal).sqrMagnitude < 0.05f * 0.05f) return;

            Evaluate();
        }

        /// <summary>Measure, decide, and apply. Idempotent: calling it twice changes nothing.</summary>
        public void Evaluate()
        {
            if (shelf == null) { LastDecision = "no shelf"; return; }
            if (head == null) head = shelf.userHead;
            if (head == null) { LastDecision = "no head"; return; }

            HorizontalFovDegrees = MeasureHorizontalFov();
            Vector3 local = shelf.transform.InverseTransformPoint(head.position);
            HeadDistanceMeters = Mathf.Max(0.2f, local.z);

            bool tray = HorizontalFovDegrees < narrowFovDegrees;
            int columns = tray ? trayColumns : wideColumns;
            float halfWidth = (columns - 1) * 0.5f * shelf.slotSpacing + 0.03f;
            AngularWidthDegrees = 2f * Mathf.Atan2(halfWidth, HeadDistanceMeters) * Mathf.Rad2Deg;

            if (!_applied || tray != IsTray)
            {
                IsTray = tray;
                int target = tray ? trayColumns : wideColumns;
                shelf.SetLayout(target, tray ? trayTiltDegrees : 0f, tray ? trayPullInMeters : 0f);
                _applied = true;
                _lastHeadLocal = local;
                RefreshSlotMeasurements();
                LastDecision = "tray " + target + " columns at " + HorizontalFovDegrees.ToString("0") +
                               "° FoV, grid " + AngularWidthDegrees.ToString("0.0") + "°, widest slot " +
                               MaxSlotAngleDegrees.ToString("0.0") + "°";
                Debug.Log("[ShelfReflow] " + LastDecision);
            }
            else
            {
                _lastHeadLocal = local;
                RefreshSlotMeasurements();
                LastDecision = "kept " + (IsTray ? "tray" : "wide") + " at " +
                               HorizontalFovDegrees.ToString("0") + "° FoV, grid " +
                               AngularWidthDegrees.ToString("0.0") + "°, widest slot " +
                               MaxSlotAngleDegrees.ToString("0.0") + "°";
            }
        }

        /// <summary>The widest horizontal offset of any slot, in degrees, from the head's position.</summary>
        public void RefreshSlotMeasurements()
        {
            MaxSlotAngleDegrees = 0f;
            if (shelf == null || head == null) return;

            Vector3 headLocal = shelf.transform.InverseTransformPoint(head.position);
            for (int i = 0; i < shelf.slotCount; i++)
            {
                Transform slot = shelf.SlotTransform(i);
                if (slot == null) continue;
                Vector3 slotLocal = shelf.transform.InverseTransformPoint(slot.position);
                float forward = Mathf.Max(0.05f, headLocal.z - slotLocal.z);
                // The object's half-width, not just the slot centre: what must stay inside the cone
                // is the whole object, and a slot whose centre is inside can still clip its edge.
                float angle = Mathf.Atan2(Mathf.Abs(slotLocal.x - headLocal.x) + 0.03f, forward) *
                              Mathf.Rad2Deg;
                if (angle > MaxSlotAngleDegrees) MaxSlotAngleDegrees = angle;
            }
        }

        /// <summary>Any slot outside the cone is a slot the narrow profile cannot show.</summary>
        public bool AllSlotsInsideSafeCone => MaxSlotAngleDegrees <= safeConeDegrees * 0.5f;

        float MeasureHorizontalFov()
        {
            if (overrideHorizontalFovDegrees > 1f) return overrideHorizontalFovDegrees;
            if (eye == null) ResolveEye();
            if (eye != null && eye.fieldOfView > 1f && eye.aspect > 0.2f)
                return 2f * Mathf.Atan(Mathf.Tan(eye.fieldOfView * 0.5f * Mathf.Deg2Rad) * eye.aspect) *
                       Mathf.Rad2Deg;
            return 110f;   // an unmeasured scene is assumed to be the wide profile, never the narrow one
        }

        void ResolveEye()
        {
            if (eye != null) return;
            if (head != null) eye = head.GetComponentInChildren<Camera>();
            if (eye == null) eye = Camera.main;
        }

        void OnDrawGizmos()
        {
            if (!showSafeCone || !DevOnly.Allowed || head == null) return;
            Vector3 origin = head.position;
            Vector3 forward = head.forward; forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) return;
            forward.Normalize();
            float half = safeConeDegrees * 0.5f;
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.85f);
            for (int i = 0; i < 2; i++)
            {
                float angle = i == 0 ? half : -half;
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                Gizmos.DrawLine(origin, origin + dir * 2.5f);
            }
            Gizmos.DrawWireSphere(origin + forward * 2.5f, 0.02f);
        }
    }
}
