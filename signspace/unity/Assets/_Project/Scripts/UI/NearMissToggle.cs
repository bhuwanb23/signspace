using UnityEngine;
using Signspace.Matching;

namespace Signspace.UI
{
    /// <summary>
    /// The near-miss off switch (G3 criterion 3): a small block beside the honest-mirror handle.
    /// Pinching it turns the naming behaviour off or on for the session, and the block dims when it
    /// is off so the state is visible without a menu.
    ///
    /// WHY A PHYSICAL OBJECT
    /// ---------------------
    /// The same reason the mirror is reached by picking something up: a user who decides the
    /// naming is wrong must be able to stop it in one gesture, from inside the space, without
    /// finding a settings screen they have never seen. The switch is deliberately not part of the
    /// mirror panel - the panel is about what the sensor cannot see, this is about what the product
    /// is allowed to say.
    ///
    /// Turning it back on does NOT refill the session's cap. Otherwise the cap is decorative: a user
    /// could toggle the switch to farm hints whenever they felt stuck.
    /// </summary>
    [RequireComponent(typeof(Grabbable))]
    public class NearMissToggle : MonoBehaviour
    {
        public Matcher matcher;

        [Tooltip("Colour when the naming is ON. The block is dimmed when it is OFF.")]
        public Color onColor = new Color(0.95f, 0.89f, 0.76f, 1f);
        public Color offColor = new Color(0.30f, 0.33f, 0.36f, 1f);

        Grabbable _grabbable;
        Renderer _renderer;

        void Awake()
        {
            _grabbable = GetComponent<Grabbable>();
            _grabbable.OnGrabbed += OnGrabbed;
            _renderer = GetComponentInChildren<Renderer>();
            Apply();
        }

        void OnDestroy()
        {
            if (_grabbable != null) _grabbable.OnGrabbed -= OnGrabbed;
        }

        void OnGrabbed(MetaHandSource hand)
        {
            Toggle();
        }

        public void Toggle()
        {
            if (matcher == null)
            {
                Debug.LogWarning("[NearMissToggle] no matcher assigned - the switch does nothing.");
                return;
            }

            matcher.NearMissEnabled = !matcher.NearMissEnabled;
            Debug.Log("[NearMissToggle] near-miss naming " +
                      (matcher.NearMissEnabled ? "ON" : "OFF") +
                      " (" + matcher.NearMissesThisSession + " this session)");
            Apply();
        }

        void Apply()
        {
            if (_renderer == null) return;
            var tint = matcher == null || matcher.NearMissEnabled ? onColor : offColor;
            var material = _renderer.material;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
        }
    }
}
