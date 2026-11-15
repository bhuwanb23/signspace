using System.Collections.Generic;
using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>
    /// Turns a pinch into a grab. One per hand, next to the MetaHandSource it reads.
    ///
    /// THE RULE, AND WHY IT IS THIS SIMPLE
    /// -----------------------------------
    /// A grab engages when all of these are true in the same frame:
    ///   1. the hand is tracked, and it has a pinch point (thumb tip and index tip both present);
    ///   2. the pinch closes (rising edge, not the held state - otherwise a hand that stays pinched
    ///      would re-grab everything it brushed past);
    ///   3. a Grabbable is within its own grabDistance of that pinch point.
    ///
    /// No ray. No controller. No physics joint, no collider sweep, no layer mask. P1.2 has to answer
    /// "does pinch-grab work on a floating object", and every one of those mechanisms would add a
    /// failure mode that is not grab.
    ///
    /// WHY THE PINCH POINT AND NOT THE WRIST
    /// -------------------------------------
    /// The pinch closes around the midpoint of thumb tip and index tip. Measuring from the wrist
    /// would put the grab origin ~10 cm behind where the user sees their fingers meet, so a user
    /// reaching for a cube would grab it early and the same user would swear the app grabbed late.
    ///
    /// The held object follows the WRIST's world rotation, not this GameObject's: the rig root never
    /// rotates, so following it would pick an object up and then never let it turn in the hand.
    ///
    /// WIDENING BEFORE BLAMING
    /// -----------------------
    /// P1.2's first trap is "tuning pinch too tight and blaming it on the tracker". So the two
    /// thresholds that can cause a miss are exposed and logged: `pinchDistance` on MetaHandSource,
    /// and `grabDistance` on the Grabbable. The trial log records the distance at every attempt, so
    /// a miss can be classified from data - too far to engage, or never pinched at all.
    /// </summary>
    public class PinchGrabber : MonoBehaviour
    {
        [Tooltip("The tracked hand. Pinch state, pinch point and wrist rotation all come from here.")]
        public MetaHandSource source;

        [Tooltip("How far a pinch is treated as an ATTEMPT at grabbing, in metres. Attempts are " +
                 "counted for the P1.2 trial log; they are deliberately wider than any Grabbable's " +
                 "grabDistance, because an attempt is an act of intent, not a success.")]
        public float intentRadius = 0.45f;

        [Tooltip("Layer mask for grabbables. Everything by default; narrow it once there is scenery.")]
        public LayerMask grabbableLayers = ~0;

        [Tooltip("Log every attempt and its outcome.")]
        public bool logTrials = true;

        [Tooltip("Optional. Counts attempts and successes across both hands.")]
        public GrabTrialLogger logger;

        public Grabbable Held { get; private set; }

        /// <summary>True while this hand is pinching and engaged.</summary>
        public bool IsHolding => Held != null;

        bool _pinchWasClosed;
        readonly Collider[] _overlap = new Collider[16];
        Grabbable _hover;

        void Update()
        {
            if (source == null) return;

            bool closed = source.IsTracked && source.IsPinching && source.HasPinchPoint;
            bool rising = closed && !_pinchWasClosed;
            _pinchWasClosed = closed;

            Vector3 point = source.HasPinchPoint ? source.PinchPoint : source.transform.position;

            if (Held != null)
            {
                if (closed)
                {
                    Held.Follow(point, source.WristRotation);
                }
                else
                {
                    Held.Release();
                    Held = null;
                }
                SetHover(null);
                return;
            }

            var candidate = NearestGrabbable(point);
            SetHover(candidate);

            if (!rising) return;

            // An attempt is a pinch that closes with something grabbable inside the intent radius.
            // Pinching at nothing is not a miss - it is a pinch at nothing.
            bool intended = candidate != null &&
                            Vector3.Distance(point, candidate.transform.position) <= intentRadius;
            bool engaged = false;

            if (candidate != null && candidate.IsWithinGrabRange(point))
            {
                candidate.Grab(source, point);
                Held = candidate;
                engaged = true;
                SetHover(null);
            }

            if (logTrials && intended && logger != null)
            {
                logger.Record(source, candidate, engaged, Vector3.Distance(point, candidate.transform.position));
            }
        }

        /// <summary>Nearest grabbable in range of the hand, or null. Cheap and allocation-free.</summary>
        Grabbable NearestGrabbable(Vector3 point)
        {
            int n = Physics.OverlapSphereNonAlloc(point, intentRadius, _overlap, grabbableLayers,
                                                 QueryTriggerInteraction.Collide);
            Grabbable best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var g = _overlap[i] != null ? _overlap[i].GetComponentInParent<Grabbable>() : null;
                if (g == null || g.IsHeld) continue;
                float d = Vector3.Distance(point, g.transform.position);
                if (d >= bestDistance) continue;
                bestDistance = d;
                best = g;
            }
            return best;
        }

        void SetHover(Grabbable g)
        {
            if (_hover == g) return;
            if (_hover != null) _hover.SetHover(false);
            _hover = g;
            if (_hover != null) _hover.SetHover(true);
        }
    }
}
