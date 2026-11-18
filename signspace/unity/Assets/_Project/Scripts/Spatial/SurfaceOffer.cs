using Signspace.Matching;
using UnityEngine;

namespace Signspace.Spatial
{
    /// <summary>
    /// The one quiet affordance that offers a surface which appeared after launch (P4.4 step 3).
    ///
    /// It exists because the opposite is worse: auto-migrating teleports the user's progress in
    /// front of them, and a message ("surface detected!") turns a fallback into a failure state.
    /// So the ledge is silent, the shelf stays where it is, and a small soft bead on the plinth
    /// becomes touchable. Pinch it and the shelf moves; do nothing and nothing happens.
    ///
    /// The bead is also the only thing that can accept the offer. `ShelfAnchor` raises
    /// `HasPendingSurfaceOffer`, this component shows the bead, and only a pinch inside
    /// `reachMetres` calls `AcceptSurfaceOffer`.
    /// </summary>
    public class SurfaceOffer : MonoBehaviour
    {
        [Tooltip("The shelf whose offer this bead represents.")]
        public ShelfAnchor shelf;

        [Tooltip("The visible bead. Hidden until there is an offer; no collider, no text, no sound.")]
        public GameObject visual;

        [Tooltip("Hands that can accept. Left empty, every MetaHandSource in the scene is used.")]
        public MetaHandSource[] hands;

        [Tooltip("How close the pinch must be to the bead, in metres.")]
        public float reachMetres = 0.14f;

        [Tooltip("Seconds before it can be accepted again, so a hand drifting out of a pinch does not double-fire.")]
        public float cooldownSeconds = 1f;

        [Tooltip("Pulse amplitude, in scale. Small: an invitation, not an alert.")]
        public float pulseAmplitude = 0.06f;

        float _cooldown;
        Vector3 _baseScale = Vector3.one;

        public bool Offered => visual != null && visual.activeSelf;

        void Awake()
        {
            if (visual == null && transform.childCount > 0) visual = transform.GetChild(0).gameObject;
            if (visual != null)
            {
                _baseScale = visual.transform.localScale;
                visual.SetActive(false);
            }
        }

        void Update()
        {
            if (shelf == null || visual == null)
            {
                if (visual != null && visual.activeSelf) visual.SetActive(false);
                return;
            }

            bool offered = shelf.HasPendingSurfaceOffer;
            if (visual != null && visual.activeSelf != offered) visual.SetActive(offered);
            if (!offered) return;

            float t = Time.time * 2.2f;
            visual.transform.localScale = _baseScale * (1f + Mathf.Sin(t) * pulseAmplitude);

            _cooldown = Mathf.Max(0f, _cooldown - Time.deltaTime);
            if (_cooldown > 0f) return;

            if (hands == null || hands.Length == 0)
            {
                hands = FindObjectsByType<MetaHandSource>(FindObjectsSortMode.None);
                if (hands == null || hands.Length == 0) return;
            }

            for (int i = 0; i < hands.Length; i++)
            {
                var hand = hands[i];
                if (hand == null || !hand.IsPinching || !hand.HasPinchPoint) continue;
                if ((hand.PinchPoint - transform.position).sqrMagnitude > reachMetres * reachMetres) continue;

                _cooldown = cooldownSeconds;
                shelf.AcceptSurfaceOffer();
                return;
            }
        }
    }
}
