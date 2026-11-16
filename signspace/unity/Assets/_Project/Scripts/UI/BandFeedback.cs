using UnityEngine;
using Signspace.Matching;

namespace Signspace.UI
{
    /// <summary>
    /// The three bands as something you can see, with no words anywhere (P2.3).
    ///
    /// ADR-003 forbids a failure state, and `UX-STATES.md` forbids a red X. So the matcher's distance
    /// cannot be reported as a verdict or a number the learner has to interpret - it is reported as a
    /// **colour** and a **kind of motion**, which is what a person reads fastest and what survives
    /// being seen out of the corner of the eye while their hands are busy being the thing they are
    /// concentrating on.
    ///
    /// The three states are deliberately different in kind, not just in hue:
    ///
    ///   warming  amber,  breathing slowly     - alive, waiting, no information about direction yet
    ///   close    teal,   pulsing faster, ring contracting as D falls
    ///   locked   gold,   completely still     - the app stops fidgeting because you have arrived
    ///
    /// Stillness is the reward. A meter that keeps moving after success makes success feel like
    /// continued effort, and the one thing a learner needs to feel at the lock is *done*.
    /// </summary>
    public class BandFeedback : MonoBehaviour
    {
        [Header("Dependencies")]
        public Matcher matcher;

        [Header("View")]
        [Tooltip("The halo around the reference hand. Colour and pulse live here.")]
        public Renderer halo;
        [Tooltip("Optional: a ring that contracts towards the halo as D falls. Motion, not text.")]
        public Transform ring;
        [Tooltip("Optional: a light so the colour also lands on the room.")]
        public Light glow;
        [Tooltip("Light intensity when locked; the other bands scale down from it.")]
        public float glowIntensityAtLock = 0.9f;

        [Header("Warming — amber, slow breath")]
        public Color warmingColor = new Color(0.78f, 0.46f, 0.16f, 1f);
        public float warmingBreathHz = 0.25f;
        public float warmingPulseScale = 0.020f;

        [Header("Close — teal, faster pulse, contracting ring")]
        public Color closeColor = new Color(0.16f, 0.65f, 0.63f, 1f);
        public float closePulseHz = 0.80f;
        public float closePulseScale = 0.055f;
        [Tooltip("Ring radius (relative to its resting scale) at the close edge and at the lock edge.")]
        public float ringOuterScale = 1.00f;
        public float ringInnerScale = 0.42f;

        [Header("Locked — gold, and still")]
        public Color lockedColor = new Color(0.95f, 0.89f, 0.76f, 1f);
        [Tooltip("Emissive multiplier at rest, per band. Locked is the brightest and completely steady.")]
        public float warmingGlow = 0.35f;
        public float closeGlow = 0.65f;
        public float lockedGlow = 1.10f;

        [Header("Transition")]
        [Tooltip("Seconds to blend between band colours. Below ~0.08 s a band change reads as a cut.")]
        public float colorBlendSeconds = 0.14f;

        // P3.5. How far the display dims while the matcher is paused - tracking too weak to judge. It dims
        // and stops breathing; it never goes out, because a dark meter is indistinguishable from a dead app.
        // The dim lives HERE rather than in TrackingConfidence because this component owns the property
        // block that writes the halo's colour every frame: anything else setting the material colour is
        // overwritten on the next Update, which looks implemented and does nothing.
        [Tooltip("How far the halo dims while the app is paused waiting for the tracker.")]
        [Range(0f, 1f)] public float pauseDim = 0.6f;
        [Tooltip("The single snap on lock entry, in seconds. Not a loop: the lock itself is still.")]
        public float lockSnapSeconds = 0.12f;
        public float lockSnapScale = 0.35f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        MaterialPropertyBlock _block;
        Color _current;
        Band _shownBand;
        float _clock;
        float _lockSnapTimer = -1f;
        float _lastDistance = float.PositiveInfinity;

        void Awake()
        {
            _block = new MaterialPropertyBlock();
            _current = warmingColor;
            if (halo == null) halo = GetComponentInChildren<Renderer>();
            if (matcher == null) matcher = FindFirstObjectByType<Matcher>();
        }

        void Update()
        {
            if (matcher == null) return;

            Band band = matcher.CurrentBand;
            float dt = Time.deltaTime;
            bool paused = matcher.IsPaused;

            // A breathing meter is a measuring meter. While the app is waiting for the tracker, motion
            // stops dead: only the colour is left, dimmed. That combination reads as "still here, not
            // judging" and it cannot be mistaken for a measurement in progress.
            if (!paused) _clock += dt;

            // A band change is a transition, not a cut. Cutting colour on a threshold is what makes a
            // meter feel like a machine deciding about you.
            Color wanted = band == Band.Locked ? lockedColor
                         : band == Band.Close ? closeColor
                         : warmingColor;
            _current = Color.Lerp(_current, wanted, Mathf.Clamp01(dt / Mathf.Max(0.01f, colorBlendSeconds)));

            if (band != _shownBand)
            {
                if (band == Band.Locked) _lockSnapTimer = 0f;
                _shownBand = band;
            }

            float scale = 1f;
            float glowRatio = 1f;               // relative to glowIntensityAtLock

            switch (band)
            {
                case Band.Warming:
                    scale += Mathf.Sin(_clock * Mathf.PI * 2f * warmingBreathHz) * warmingPulseScale;
                    glowRatio = warmingGlow / lockedGlow;
                    break;

                case Band.Close:
                {
                    // Pulse rate and amplitude both feed off how close we are: the meter leans in as
                    // the hand arrives, so motion carries the gradient and colour only carries state.
                    float span = Mathf.Max(1f, matcher.closeDeg - matcher.lockedDeg);
                    float closeness = 1f - Mathf.Clamp01((matcher.DistanceDegrees - matcher.lockedDeg) / span);
                    float pulse = Mathf.Sin(_clock * Mathf.PI * 2f * closePulseHz);
                    scale += pulse * closePulseScale * Mathf.Lerp(0.4f, 1f, closeness);
                    glowRatio = Mathf.Lerp(closeGlow * 0.7f, closeGlow, closeness) / lockedGlow;

                    if (ring != null && !paused)
                    {
                        var s = ring.localScale;
                        float r = Mathf.Lerp(ringOuterScale, ringInnerScale, closeness);
                        ring.localScale = new Vector3(r, r, s.z);
                    }
                    break;
                }

                case Band.Locked:
                    // One snap on arrival, then nothing. This is the whole of the "you are here" signal
                    // in 3D: a single event, then stillness.
                    if (_lockSnapTimer >= 0f)
                    {
                        _lockSnapTimer += dt;
                        float k = Mathf.Clamp01(_lockSnapTimer / Mathf.Max(0.01f, lockSnapSeconds));
                        scale += lockSnapScale * (1f - k);
                        if (k >= 1f) _lockSnapTimer = -1f;
                    }
                    glowRatio = 1f;
                    break;
            }

            _lastDistance = matcher.DistanceDegrees;

            // No motion at all while paused, whatever the band was doing a frame ago.
            if (paused) scale = 1f;

            Apply(scale, glowRatio, paused);
        }

        void Apply(float scale, float glowRatio, bool paused)
        {
            // Dim by multiplying the colour, not by setting alpha: an unlit material is opaque, and alpha
            // on an opaque surface changes nothing on screen - a dim that does not dim.
            Color shown = paused
                ? new Color(_current.r * (1f - pauseDim), _current.g * (1f - pauseDim),
                            _current.b * (1f - pauseDim), _current.a)
                : _current;

            if (halo != null)
            {
                halo.transform.localScale = Vector3.one * scale;
                halo.GetPropertyBlock(_block);
                if (halo.sharedMaterial != null && halo.sharedMaterial.HasProperty(BaseColorId))
                    _block.SetColor(BaseColorId, shown);
                if (halo.sharedMaterial != null && halo.sharedMaterial.HasProperty(ColorId))
                    _block.SetColor(ColorId, shown);
                if (halo.sharedMaterial != null && halo.sharedMaterial.HasProperty(EmissionId))
                    _block.SetColor(EmissionId, shown * glowRatio);
                halo.SetPropertyBlock(_block);
            }

            if (glow != null)
            {
                glow.color = shown;
                glow.intensity = glowIntensityAtLock * Mathf.Clamp(glowRatio, 0.1f, 1.2f);
                glow.enabled = true;
            }
        }

        /// <summary>The band colour, for anything else that needs to agree with it (shelf, ring, hand).</summary>
        public Color ColorFor(Band band) =>
            band == Band.Locked ? lockedColor : band == Band.Close ? closeColor : warmingColor;

        /// <summary>Name of the motion, for the debug overlay only. Never shown to a learner.</summary>
        public string MotionName =>
            matcher == null ? "-" : matcher.CurrentBand == Band.Locked ? "still"
                          : matcher.CurrentBand == Band.Close ? "pulse + ring contracting"
                          : "slow breath";
    }
}
