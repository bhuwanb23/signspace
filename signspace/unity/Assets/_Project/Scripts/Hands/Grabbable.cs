using System;
using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>
    /// Something a hand can pinch and pick up.
    ///
    /// Deliberately not an ISDK HandGrabInteractable. P1.2's task text names the ISDK components, and
    /// the reason this does not use them is written down rather than implied: ISDK grab needs its own
    /// HandGrabInteractor bound to an ISDK `Hand` / `HandGrabApi` rig, which would mean a second,
    /// parallel hand pipeline living beside MetaHandSource and SkeletonPoseSource - and the one thing
    /// P1.2 must prove is that OUR pipeline can drive grab. See ../phases/p1-derisk/grab-log.md §3.
    ///
    /// What is kept from the ISDK idea: a grab is a PINCH on a nearby object, never a ray, and the
    /// object keeps its own transform while held rather than being glued to a fixed offset.
    /// </summary>
    public class Grabbable : MonoBehaviour
    {
        [Tooltip("Collision radius around the pinch point, in metres. A cube of side 0.1 has a " +
                 "circumscribed radius near 0.087.")]
        public float radius = 0.06f;

        [Tooltip("How far the pinch point may be from the object's centre for a grab to engage. " +
                 "Generous on purpose: P1.2 is testing whether grab works at all, and a tight " +
                 "threshold would test the threshold instead.")]
        public float grabDistance = 0.12f;

        [Tooltip("While held, the object is moved to keep this offset from the pinch point.")]
        public bool followRotation = true;

        [Tooltip("Multiply the interaction distance in the direction the palm faces.")]
        public float forwardBias = 1f;

        public bool IsHeld { get; private set; }
        public MetaHandSource HeldBy { get; private set; }

        public event Action<MetaHandSource> OnGrabbed;
        public event Action<MetaHandSource> OnReleased;

        /// <summary>Whether a pinch at this point would engage. Used for hover feedback and for the
        /// trial logger's definition of intent.</summary>
        public bool IsWithinGrabRange(Vector3 pinchPoint)
        {
            return Vector3.Distance(pinchPoint, transform.position) <= grabDistance + radius;
        }

        internal void Grab(MetaHandSource hand, Vector3 pinchPoint)
        {
            if (IsHeld) return;
            IsHeld = true;
            HeldBy = hand;
            _grabOffset = transform.position - pinchPoint;
            _grabRotation = transform.rotation;
            _grabHandRotation = hand.transform.rotation;
            SetHover(false);
            if (OnGrabbed != null) OnGrabbed(hand);
        }

        internal void Follow(Vector3 pinchPoint, Quaternion handRotation)
        {
            if (!IsHeld) return;
            transform.position = pinchPoint + _grabOffset;
            if (!followRotation) return;

            // Only the DELTA since the grab is applied. Taking the hand's absolute rotation would
            // snap the object to the hand's frame the instant it was grabbed, which reads as a
            // glitch rather than as picking something up.
            Quaternion delta = handRotation * Quaternion.Inverse(_grabHandRotation);
            transform.rotation = delta * _grabRotation;
        }

        internal void Release()
        {
            if (!IsHeld) return;
            IsHeld = false;
            var hand = HeldBy;
            HeldBy = null;
            if (OnReleased != null) OnReleased(hand);
        }

        Vector3 _grabOffset;
        Quaternion _grabRotation;
        Quaternion _grabHandRotation;

        MaterialPropertyBlock _block;
        Renderer _renderer;
        Color _baseColor;
        bool _haveBase;

        void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
            if (_renderer != null)
            {
                _block = new MaterialPropertyBlock();
                _renderer.GetPropertyBlock(_block);
                _baseColor = _renderer.sharedMaterial != null && _renderer.sharedMaterial.HasProperty("_BaseColor")
                    ? _renderer.sharedMaterial.GetColor("_BaseColor")
                    : (_renderer.sharedMaterial != null ? _renderer.sharedMaterial.color : Color.white);
                _haveBase = true;
            }
        }

        /// <summary>
        /// Hover feedback. Without it, a near-miss is indistinguishable from a missed pinch, and
        /// P1.2's whole question is "did it fail to grab, or did I fail to pinch?".
        /// </summary>
        public void SetHover(bool hovering)
        {
            if (_renderer == null || _block == null || !_haveBase) return;
            _block.SetColor(_renderer.sharedMaterial.HasProperty("_BaseColor") ? "_BaseColor" : "_Color",
                            hovering ? Color.Lerp(_baseColor, Color.white, 0.45f) : _baseColor);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
