using UnityEngine;
using UnityEngine.UI;
using Signspace.Core;
using Signspace.Gaze;
using Signspace.Hands;
using Signspace.Matching;

namespace Signspace.UI
{
    /// <summary>
    /// One physical toggle block on the accessibility tray (P6.5).
    ///
    /// Same pattern as the near-miss switch: a small object you pinch. It is a block rather than a
    /// button on a panel because a panel needs a controller to point at, and the whole point of these
    /// switches is that they must be reachable by the input the user already has.
    ///
    /// Each block carries a <see cref="DwellAction.TryDispatch">DwellAction</see> as well, so the same
    /// block is reachable by gaze when eyes-only is on. That is deliberate: an accessibility switch
    /// that itself needs a pinch is a joke at the expense of the user.
    /// </summary>
    [RequireComponent(typeof(Grabbable))]
    public class A11yToggleBlock : MonoBehaviour
    {
        public A11yToggle toggle = A11yToggle.Captions;
        public A11ySettings settings;

        [Tooltip("Title line, e.g. 'Captions'.")]
        public Text title;
        [Tooltip("State line, e.g. 'on'. Also shows the speed multiplier.")]
        public Text value;

        public Color onColor = new Color(0.95f, 0.89f, 0.76f, 1f);
        public Color offColor = new Color(0.30f, 0.33f, 0.36f, 1f);

        Grabbable _grabbable;
        Renderer _renderer;

        void Awake()
        {
            if (settings == null) settings = A11ySettings.Instance;
            _grabbable = GetComponent<Grabbable>();
            _renderer = GetComponentInChildren<Renderer>();
            if (_grabbable != null) _grabbable.OnGrabbed += OnGrabbed;
            if (title != null) title.text = A11ySettings.Title(toggle);
            Apply();
        }

        void Start()
        {
            if (settings == null) settings = A11ySettings.Instance;
            Apply();
        }

        void OnDestroy()
        {
            if (_grabbable != null) _grabbable.OnGrabbed -= OnGrabbed;
            if (settings != null) settings.OnChanged -= OnChanged;
        }

        void OnEnable()
        {
            if (settings != null) settings.OnChanged += OnChanged;
        }

        void OnDisable()
        {
            if (settings != null) settings.OnChanged -= OnChanged;
        }

        void OnGrabbed(MetaHandSource hand) => Flip();

        void OnChanged(A11yToggle changed) => Apply();

        /// <summary>Also called by the block's DwellAction, so both inputs take the same path.</summary>
        public void Flip()
        {
            if (settings == null) settings = A11ySettings.Instance;
            if (settings == null)
            {
                Debug.LogWarning("[A11yToggleBlock] no A11ySettings in the scene; '" + toggle + "' does nothing.");
                return;
            }
            settings.Toggle(toggle);
            Apply();
        }

        public void Apply()
        {
            bool on = settings != null && settings.Get(toggle);
            if (value != null) value.text = settings != null ? settings.ValueLabel(toggle) : "?";

            if (_renderer == null) return;
            var material = _renderer.material;
            var tint = on ? onColor : offColor;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
        }
    }
}
