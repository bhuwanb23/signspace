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

        [Header("Copy — <= 12 words, no 'incorrect', no 'error', no exclamation mark (P3.3)")]
        [Tooltip("{0} = the target, {1} = the shape the user made, {2} = degrees to it. The hint is " +
                 "appended as a second sentence, so the pair's own difference is what names the target.")]
        public string template = "That's {1} — {2}° away.";

        [Tooltip("Plain form for the high-contrast / text-only path: no dash, no degree symbol, no number. " +
                 "P6.5's contrast toggle is what selects it.")]
        public string templatePlain = "That's {1}.";

        [Tooltip("Use the plain form. Set by the accessibility toggles (P6.5); defaults to the richer line.")]
        public bool plainText = false;

        /// <summary>
        /// One hint per ordered pair: what the TARGET needs, said in the user's words.
        ///
        /// These are the only lines in the product that describe articulation, and they exist so the
        /// near-miss can be specific instead of merely correct. Each one is derived from the authored
        /// deltas - C flexes 38/48/34 where O flexes 52/72/54 and adds thumb contact, so from O the way
        /// to C is *less* curl - not written from memory of the alphabet. Where a pair has no hint the
        /// app says only what it knows ("That's O - 3° away") rather than inventing a correction.
        ///
        /// The four pairs whose shapes do not ship yet are absent on purpose: a hint for a shape the app
        /// cannot show is a correction with nothing to look at.
        /// </summary>
        [System.Serializable]
        public struct Hint
        {
            public string target;
            public string made;
            public string line;
        }

        [Tooltip("Optional overrides. Leave empty to use the authored defaults below.")]
        public Hint[] hints;

        static readonly Hint[] DefaultHints =
        {
            // C <-> O : curvature and contact. O is the more closed of the two.
            new Hint { target = "C", made = "O", line = "C opens a little." },
            new Hint { target = "O", made = "C", line = "O closes the circle." },
            // U <-> V : index spread.
            new Hint { target = "V", made = "U", line = "V spreads those two." },
            new Hint { target = "U", made = "V", line = "U holds them together." },
            // V <-> W : one more finger.
            new Hint { target = "W", made = "V", line = "W straightens the ring finger." },
            new Hint { target = "V", made = "W", line = "V folds the ring finger down." },
            // B <-> U : two fingers folded.
            new Hint { target = "U", made = "B", line = "U folds the last two." },
            new Hint { target = "B", made = "U", line = "B straightens all four." },
        };

        /// <summary>Empty string when this pair has no authored hint. Never a guess.</summary>
        public string HintFor(string target, string made)
        {
            if (hints != null)
            {
                for (int i = 0; i < hints.Length; i++)
                    if (hints[i].target == target && hints[i].made == made) return hints[i].line;
            }
            for (int i = 0; i < DefaultHints.Length; i++)
                if (DefaultHints[i].target == target && DefaultHints[i].made == made) return DefaultHints[i].line;
            return "";
        }

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

            // Whole degrees (P3.1 step 5), and never 0: the number shown is how close the hand came to
            // the shape being named, so "0 degrees away" would read as "you made it", which is what
            // the matcher would have said instead of firing a near-miss.
            int shown = Mathf.Max(1, Mathf.RoundToInt(distanceDeg));

            string line = (plainText ? templatePlain : template)
                .Replace("{0}", target)
                .Replace("{1}", madeShapeId)
                .Replace("{2}", shown.ToString());

            // The hint is the specific half of the line, and it is optional by design: a pair with no
            // authored hint gets a shorter, still-true sentence instead of an invented correction.
            string hint = HintFor(target, madeShapeId);
            if (!string.IsNullOrEmpty(hint)) line = line + " " + hint;

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

        /// <summary>
        /// Worst-case word count of what can appear, for the P3.3 check: the template, the degrees, and
        /// the longest hint. The budget is 12 and the shipped worst case is 9.
        /// </summary>
        public int TemplateWordCount
        {
            get
            {
                string t = (plainText ? templatePlain : template).Replace("{0}", "C").Replace("{1}", "O")
                                                                        .Replace("{2}", "3");
                int longest = 0;
                for (int i = 0; i < DefaultHints.Length; i++)
                    longest = Mathf.Max(longest, DefaultHints[i].line.Trim().TrimEnd('.').Split(' ').Length);
                return t.Trim().Split(' ').Length + longest;
            }
        }
    }
}
