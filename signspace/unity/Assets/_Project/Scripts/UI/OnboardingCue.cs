using UnityEngine;
using Signspace.Matching;

namespace Signspace.UI
{
    /// <summary>
    /// The one spatial cue the onboarding needs (P7.2): a soft pulse around the reference hand that says
    /// "this is the thing you pick up" without a word, and stops the moment it has been picked up.
    ///
    /// Why a pulse and not a label: the target is five fingers, a moving hand and a rotating wrist. A
    /// sentence cannot survive being read while all three happen, and a label would also be text in the
    /// first five minutes, which `FIRST-FIVE-MINUTES.md` rations to three words. A slow 1.2 s breath
    /// around the object is the smallest thing that reads as "look here" and disappears when understood.
    ///
    /// It is NOT a click target: it has no collider, so it cannot steal a gaze target or a pinch.
    /// </summary>
    public class OnboardingCue : MonoBehaviour
    {
        public ReferenceHand referenceHand;

        [Tooltip("Colour of the pulse. Warm, low-saturation: it is an invitation, not a warning.")]
        public Color colour = new Color(1f, 0.86f, 0.62f, 1f);

        [Tooltip("Resting radius in metres, around the palm.")]
        public float radius = 0.055f;

        [Tooltip("Depth of the breath, as a fraction of the radius.")]
        [Range(0f, 0.5f)] public float breath = 0.16f;

        [Tooltip("Seconds per breath. Slow enough to read as alive, not blinking.")]
        public float periodSeconds = 1.2f;

        [Tooltip("Gone for good after a grab, a lock, or this many seconds, whichever comes first.")]
        public float giveUpSeconds = 45f;

        public bool Finished { get; private set; }

        Transform _pulse;
        Material _material;
        float _age;

        void Start()
        {
            if (referenceHand == null) referenceHand = FindFirstObjectByType<ReferenceHand>();
            Build();
        }

        void Build()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "OnboardingPulse";
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);   // never a target

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader != null)
            {
                _material = new Material(shader);
                if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", colour);
                if (_material.HasProperty("_Color")) _material.SetColor("_Color", colour);
                var renderer = go.GetComponent<Renderer>();
                if (renderer != null) renderer.sharedMaterial = _material;
            }

            _pulse = go.transform;
            _pulse.SetParent(transform, false);
            _pulse.localScale = Vector3.one * radius;
        }

        void Update()
        {
            if (Finished) return;

            _age += Time.deltaTime;

            bool grabbed = referenceHand != null && referenceHand.IsGrabbed;
            if (grabbed || _age > giveUpSeconds)
            {
                Finish();
                return;
            }

            if (_pulse == null) return;
            if (referenceHand != null) _pulse.position = referenceHand.transform.position;

            float phase = Mathf.Sin(_age / Mathf.Max(0.01f, periodSeconds) * Mathf.PI * 2f);
            _pulse.localScale = Vector3.one * radius * (1f + breath * phase);

            if (_material != null && _material.HasProperty("_BaseColor"))
            {
                var c = colour;
                c.a = 0.35f + 0.25f * (phase * 0.5f + 0.5f);
                _material.SetColor("_BaseColor", c);
            }
        }

        void Finish()
        {
            Finished = true;
            if (_pulse != null) Destroy(_pulse.gameObject);
        }
    }
}
