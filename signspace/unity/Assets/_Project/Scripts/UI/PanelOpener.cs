using UnityEngine;
using Signspace.Matching;

namespace Signspace.UI
{
    /// <summary>
    /// A physical object in the room that opens the honest mirror (P3.4 step 4).
    ///
    /// Not a settings menu, not a wrist button, not a system panel: the panel about what the hardware
    /// cannot see is opened by touching something that is there. P4 re-homes this object onto the shelf;
    /// until then it stands on its own plinth so the panel is reachable and testable.
    ///
    /// Pinch, not gaze: the product's only other input is a pinch (the grab), and a panel opened by
    /// looking at it would fire while someone is simply looking around.
    /// </summary>
    public class PanelOpener : MonoBehaviour
    {
        [Tooltip("The panel this object opens.")]
        public HonestMirror panel;

        [Tooltip("Hands that can open it. Left empty, every MetaHandSource in the scene is used.")]
        public MetaHandSource[] hands;

        [Tooltip("How close the pinch must be to the object's centre, in metres. Palm-sized, so it " +
                 "reads as touching rather than pointing.")]
        public float reachMetres = 0.12f;

        [Tooltip("Seconds before it can be opened again - a hand leaving a pinch drifts across the " +
                 "boundary and would otherwise toggle twice.")]
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
