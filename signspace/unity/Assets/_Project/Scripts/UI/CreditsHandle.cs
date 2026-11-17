using UnityEngine;
using Signspace.Matching;

namespace Signspace.UI
{
    /// <summary>
    /// The bead that opens the credits panel (P5.5).
    ///
    /// Same input as everything else in the product — a pinch, never a gaze — and the same reach as the
    /// honest-mirror plinth, so the two objects in the space behave identically. It exists so the credit
    /// is a thing you can touch on the shelf rather than a line buried in a menu.
    /// </summary>
    public class CreditsHandle : MonoBehaviour
    {
        [Tooltip("The panel this bead opens.")]
        public CreditsPanel panel;

        [Tooltip("Hands that can open it. Left empty, every MetaHandSource in the scene is used.")]
        public MetaHandSource[] hands;

        [Tooltip("How close the pinch must be to the bead, in metres.")]
        public float reachMetres = 0.12f;

        [Tooltip("Seconds before it can be toggled again.")]
        public float cooldownSeconds = 1f;

        float _cooldown;

        void Update()
        {
            _cooldown = Mathf.Max(0f, _cooldown - Time.deltaTime);
            if (panel == null || _cooldown > 0f) return;

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
                panel.Toggle();
                return;
            }
        }
    }
}
