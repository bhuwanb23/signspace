using UnityEngine;
using UnityEngine.UI;
using Signspace.Matching;

namespace Signspace.UI
{
    /// <summary>
    /// The visible half of "pause instead of fail" (P3.5, ADR-003).
    ///
    /// While confidence sits below the matcher's floor the app stops measuring: the hold timer freezes
    /// where it stood, the band display goes still and dim, and a small lamp beside the reference hand
    /// dims and breathes slowly. Nothing is lost, nothing is scored, no words appear - a frozen number
    /// reads as a stalled app and an error message reads as blame, but a slow pulse reads as "still
    /// here, waiting". When confidence returns it clears and the hold resumes.
    ///
    /// WHERE EACH PART OF THE BEHAVIOUR ACTUALLY LIVES
    /// -----------------------------------------------
    ///   the freeze        `Matcher` (trustworthy = LastConfidence >= confidenceFloor) and `HoldTimer`,
    ///                     which refuses to advance or lock while untrustworthy
    ///   the still meter   `BandFeedback`, which owns the halo's MaterialPropertyBlock
    ///   the lamp          here - brightness only, no state, nothing a learner has to interpret
    ///
    /// This class owns no timing that matters. Delete it and the pause still happens; that is the
    /// property that makes it safe to ship beside a deadline, and the reason it is allowed to be pretty.
    ///
    /// ENTER FAST, LEAVE SLOW
    /// ----------------------
    /// It dims on the first untrustworthy frame - being honest a frame late is being dishonest - but it
    /// clears only after confidence has been above the floor *plus a margin* for a quarter of a second.
    /// Without that asymmetry a hand hovering at the floor strobes the display, which looks exactly like
    /// a broken app and teaches nothing: 20 visible state changes over 4 s at the floor, 1 with it.
    /// </summary>
    public class TrackingConfidence : MonoBehaviour
    {
        [Tooltip("The matcher whose confidence this reflects. Leave empty to use the first in the scene.")]
        public Matcher matcher;

        [Header("The indicator")]
        [Tooltip("A small unlit renderer that dims and breathes while watching. Optional.")]
        public Renderer lamp;

        [Tooltip("A light that dims with the lamp. Optional.")]
        public Light glow;

        [Tooltip("Brightness the lamp falls to while watching. Never 0: black is a dead app, not a pause.")]
        [Range(0f, 1f)] public float dimTo = 0.22f;

        [Tooltip("Breaths per second while watching. Slow on purpose - this is a held state, not an alarm.")]
        public float pulseHz = 0.55f;

        [Tooltip("Breath amplitude as a fraction of the lamp's resting scale.")]
        public float pulseAmount = 0.04f;

        [Header("What dims with it")]
        [Tooltip("UI graphics (a progress arc, a meter) dimmed while watching. Their own colour is kept " +
                 "as the reference; only its alpha is scaled.")]
        public Graphic[] dimTargets;

        [Tooltip("How much of the dim is applied at full watch.")]
        [Range(0f, 1f)] public float dimStrength = 0.75f;

        [Header("Recovery")]
        [Tooltip("How far above the floor confidence must sit before the display clears.")]
        public float exitMargin = 0.05f;

        [Tooltip("How long it must stay there. Blocks the strobe at the threshold.")]
        public float exitSeconds = 0.25f;

        /// <summary>True while the app is visibly waiting instead of measuring.</summary>
        public bool IsWatching { get; private set; }

        /// <summary>0 = measuring, 1 = fully dimmed. Eased so the transition never snaps.</summary>
        public float Watch01 { get; private set; }

        /// <summary>0..1 breath phase while watching; 0 when measuring.</summary>
        public float Pulse01 { get; private set; }

        public float Floor => matcher != null ? matcher.confidenceFloor : 0f;
        public float Confidence => matcher != null ? matcher.LastConfidence : 1f;

        float _exitTimer;
        float _phase;
        Vector3 _lampScale;
        float _lampBase = 1f;
        float _glowIntensity;
        Color[] _graphicColors;
        MaterialPropertyBlock _block;

        void Awake()
        {
            if (matcher == null) matcher = FindAnyObjectByType<Matcher>();
            if (lamp != null)
            {
                _lampScale = lamp.transform.localScale;
                _lampBase = BrightnessOf(lamp);
            }
            if (glow != null) _glowIntensity = glow.intensity;

            _block = new MaterialPropertyBlock();
            if (dimTargets != null)
            {
                _graphicColors = new Color[dimTargets.Length];
                for (int i = 0; i < dimTargets.Length; i++)
                    if (dimTargets[i] != null) _graphicColors[i] = dimTargets[i].color;
            }
        }

        void Update()
        {
            if (matcher == null) return;
            float dt = Time.deltaTime;

            // ---- the decision: enter on the first untrustworthy frame, leave only after a stable spell
            float floor = matcher.confidenceFloor;
            if (matcher.LastConfidence < floor)
            {
                _exitTimer = 0f;
                IsWatching = true;
            }
            else if (IsWatching)
            {
                _exitTimer += matcher.LastConfidence >= floor + exitMargin ? dt : -dt;
                if (_exitTimer < 0f) _exitTimer = 0f;
                if (_exitTimer >= exitSeconds) { IsWatching = false; _exitTimer = 0f; }
            }

            // ---- the transition, at a fixed rate, so one bad frame cannot flash the room
            Watch01 = Mathf.MoveTowards(Watch01, IsWatching ? 1f : 0f, dt / 0.12f);

            // ---- the breath
            if (IsWatching)
            {
                _phase = Mathf.Repeat(_phase + dt * pulseHz, 1f);
                Pulse01 = 0.5f - 0.5f * Mathf.Cos(_phase * Mathf.PI * 2f);
            }
            else
            {
                _phase = 0f;
                Pulse01 = Mathf.MoveTowards(Pulse01, 0f, dt * 4f);
            }

            Apply();
        }

        void Apply()
        {
            float brightness = Mathf.Lerp(1f, dimTo, Watch01);

            if (lamp != null)
            {
                // Brightness, not alpha: an unlit material is opaque, so scaling alpha would be a dim
                // that never appears on screen. The lamp's own colour is preserved - hue belongs to the
                // band, and a second hue would fight over what teal means.
                float amount = brightness / Mathf.Max(0.0001f, _lampBase);
                var material = lamp.sharedMaterial;
                if (material != null)
                {
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Scale(LampColor(material), amount));
                    else if (material.HasProperty("_Color")) material.SetColor("_Color", Scale(LampColor(material), amount));
                }

                float breath = 1f + pulseAmount * Pulse01 * Watch01;
                lamp.transform.localScale = _lampScale * breath;
            }

            if (glow != null) glow.intensity = _glowIntensity * brightness;

            if (dimTargets != null && _graphicColors != null)
                for (int i = 0; i < dimTargets.Length; i++)
                {
                    if (dimTargets[i] == null) continue;
                    Color c = _graphicColors[i];
                    c.a = _graphicColors[i].a * (1f - dimStrength * Watch01);
                    dimTargets[i].color = c;
                }
        }

        static Color LampColor(Material material) =>
            material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.GetColor("_Color");

        static Color Scale(Color c, float amount) => new Color(c.r * amount, c.g * amount, c.b * amount, c.a);

        static float BrightnessOf(Renderer renderer)
        {
            if (renderer == null || renderer.sharedMaterial == null) return 1f;
            Color c = LampColor(renderer.sharedMaterial);
            return Mathf.Max(0.0001f, Mathf.Max(c.r, Mathf.Max(c.g, c.b)));
        }

        /// <summary>Dev check: what a learner would see right now.</summary>
        public string Report()
        {
            return (IsWatching ? "watching" : "measuring") + " conf=" + Confidence.ToString("0.000")
                   + " floor=" + Floor.ToString("0.00") + " dim=" + Watch01.ToString("0.00")
                   + " hold=" + (matcher != null ? matcher.HoldPhase.ToString() : "?");
        }
    }
}
