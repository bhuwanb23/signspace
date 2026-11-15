using UnityEngine;
using UnityEngine.UI;
using Signspace.Matching;

namespace Signspace.UI
{
    /// <summary>
    /// The product's signature behaviour, made visible.
    ///
    /// "That's not C. That's O." — the one thing no video-based sign app can say, and the reason this
    /// submission is not another ASL demo (NOVELTY-AND-PRIOR-ART.md N1).
    ///
    /// Two hard constraints, both from P3:
    ///   1. The line is <= 12 words and never scolds. It appears at the emotional low point of an
    ///      attempt; a reprimand there costs more than the feature is worth.
    ///   2. Silence is the default. The matcher already enforces the confidence gate, the cooldown
    ///      and the "ambiguity is not a diagnosis" rule; this component must not add its own noise.
    /// </summary>
    public class NearMissDisplay : MonoBehaviour
    {
        [Header("Dependencies")]
        public Matcher matcher;

        [Header("View")]
        public Text label;
        public CanvasGroup group;
        [Tooltip("Seconds the line stays up.")]
        public float visibleSeconds = 2.5f;
        [Tooltip("Seconds to fade in and out.")]
        public float fadeSeconds = 0.25f;

        [Header("Copy — <= 12 words, no 'incorrect', no 'error', no exclamation mark")]
        [Tooltip("{0} = the target shape, {1} = the shape the user actually made, {2} = degrees of difference")]
        public string template = "That's not {0}. That's {1} — {2}° away.";

        float _timer;
        float _fade;
        bool _visible;

        void OnEnable()
        {
            if (matcher == null) return;
            matcher.OnNearMiss += OnNearMiss;
        }

        void OnDisable()
        {
            if (matcher == null) return;
            matcher.OnNearMiss -= OnNearMiss;
        }

        void OnNearMiss(string madeShapeId, float distanceDeg)
        {
            if (label == null) return;
            string target = matcher.targetShapeId;
            string line = template
                .Replace("{0}", target)
                .Replace("{1}", madeShapeId)
                .Replace("{2}", Mathf.RoundToInt(distanceDeg).ToString());

            label.text = line;
            _timer = visibleSeconds;
            _visible = true;
            if (group != null) group.alpha = 1f;
        }

        void Update()
        {
            if (!_visible) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _fade += Time.deltaTime;
                if (group != null)
                    group.alpha = 1f - Mathf.Clamp01(_fade / Mathf.Max(0.01f, fadeSeconds));
                if (_fade >= fadeSeconds)
                {
                    _visible = false;
                    _fade = 0f;
                    if (group != null) group.alpha = 0f;
                }
            }
        }

        /// <summary>Word count of the current template, for the P3.3 check.</summary>
        public int TemplateWordCount => template.Split(' ').Length;
    }
}
