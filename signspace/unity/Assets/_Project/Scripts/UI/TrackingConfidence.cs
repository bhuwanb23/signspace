using UnityEngine;
using UnityEngine.UI;
using Signspace.Matching;

namespace Signspace.UI
{
    /// <summary>
    /// The P3.5 pause, made visible: when tracking is too poor to judge, the feedback dims and
    /// breathes instead of showing a frozen number or an error string.
    ///
    /// ADR-003 says a dropout pauses the exercise, never fails it. The matcher already freezes the
    /// hold and the band; this is the part the user can see. There are no words: a dimmed, slowly
    /// breathing panel reads as "still watching", while any sentence would read as a verdict about
    /// the person's hand.
    /// </summary>
    public class TrackingConfidence : MonoBehaviour
    {
        public Matcher matcher;

        [Tooltip("Everything that should dim while paused. Usually the feedback canvas.")]
        public CanvasGroup group;

        [Tooltip("Optional dot that keeps breathing while paused, so dimming never reads as 'off'.")]
        public Graphic pulse;

        [Range(0f, 1f)] public float pausedAlpha = 0.45f;
        public float fadeSeconds = 0.18f;
        public float breathHz = 0.5f;

        float _alpha = 1f;
        float _clock;

        void Awake()
        {
            if (matcher == null) matcher = FindFirstObjectByType<Matcher>();
            if (group != null) _alpha = group.alpha;
        }

        void Update()
        {
            if (matcher == null) return;

            float wanted = matcher.IsPaused ? pausedAlpha : 1f;
            _alpha = Mathf.MoveTowards(_alpha, wanted, Time.deltaTime / Mathf.Max(0.01f, fadeSeconds));
            if (group != null) group.alpha = _alpha;
            if (pulse == null) return;

            _clock += Time.deltaTime;
            bool breathing = matcher.IsPaused;
            var c = pulse.color;
            c.a = breathing ? 0.35f + 0.35f * (0.5f + 0.5f * Mathf.Sin(_clock * Mathf.PI * 2f * breathHz)) : 0f;
            pulse.color = c;
        }

        public bool IsPaused => matcher != null && matcher.IsPaused;
    }
}
