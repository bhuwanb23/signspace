using UnityEngine;
using UnityEngine.UI;
using Signspace.Core;

namespace Signspace.Gaze
{
    /// <summary>
    /// The in-world caption for gaze (P6.1 step 3, P6.2 step 4).
    ///
    /// Shows three things and nothing else:
    ///   * the focused finger's label, e.g. "index extended, thumb tucked";
    ///   * the dwell ring's fill;
    ///   * a one-line explanation when the source is not what the user might assume - head gaze
    ///     instead of eyes, or no gaze at all.
    ///
    /// WHY THE EXPLANATION IS A CAPTION AND NOT A LOG
    /// ----------------------------------------------
    /// The task asks for a "visible forced-fallback state and its explanation". A Debug.Log satisfies
    /// nobody wearing a headset. The line appears for a few seconds on the first fallback and can be
    /// brought back by the mode text, so it is not a nag; it is a one-time statement of what the
    /// hardware is doing.
    /// </summary>
    public class GazeCaption : MonoBehaviour
    {
        public GazeHighlighter highlighter;
        public DwellSelector dwell;
        public EyeTrackingProbe probe;
        public A11ySettings settings;

        [Header("Lines")]
        public Text fingerLabel;
        public Text explanation;
        public Text modeLine;

        [Tooltip("The dwell progress ring. Optional; a fill image 0..1.")]
        public Image dwellRing;

        [Tooltip("Seconds the fallback explanation stays up.")]
        public float explanationSeconds = 7f;

        [Tooltip("Dev view: keep the source line visible. Off in release.")]
        public bool alwaysShowMode;

        float _hideExplanationAt = -1f;

        void Awake()
        {
            if (settings == null) settings = A11ySettings.Instance;
            if (alwaysShowMode == false) alwaysShowMode = Signspace.Core.DevOnly.Allowed;
            ClearLines();
        }

        void Start()
        {
            if (highlighter != null)
            {
                highlighter.OnFingerFocused += OnFinger;
                highlighter.OnFocusedNothing += OnNothing;
                highlighter.OnFallbackExplanation += Explain;
            }
            if (probe != null) probe.OnExplanation += Explain;
            if (settings != null) settings.OnChanged += OnChanged;
            ApplyPalette();
        }

        void OnDestroy()
        {
            if (highlighter != null)
            {
                highlighter.OnFingerFocused -= OnFinger;
                highlighter.OnFocusedNothing -= OnNothing;
                highlighter.OnFallbackExplanation -= Explain;
            }
            if (probe != null) probe.OnExplanation -= Explain;
            if (settings != null) settings.OnChanged -= OnChanged;
        }

        void OnFinger(int index, string label) => SetFinger(label);
        void OnNothing() => SetFinger("");

        void SetFinger(string label)
        {
            if (fingerLabel != null) fingerLabel.text = label ?? "";
        }

        void Explain(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            if (explanation != null)
            {
                explanation.text = line;
                explanation.gameObject.SetActive(true);
            }
            _hideExplanationAt = Time.time + explanationSeconds;
            Debug.Log("[GazeCaption] " + line);
        }

        void OnChanged(A11yToggle toggle) => ApplyPalette();

        void Update()
        {
            if (dwellRing != null && dwell != null) dwellRing.fillAmount = dwell.Progress01;

            if (modeLine != null)
            {
                modeLine.text = highlighter != null ? highlighter.ModeLine : "";
                modeLine.gameObject.SetActive(alwaysShowMode);
            }

            if (_hideExplanationAt > 0f && Time.time >= _hideExplanationAt)
            {
                _hideExplanationAt = -1f;
                if (explanation != null) explanation.text = "";
                if (explanation != null) explanation.gameObject.SetActive(false);
            }
        }

        void ClearLines()
        {
            if (fingerLabel != null) fingerLabel.text = "";
            if (explanation != null) explanation.text = "";
            if (modeLine != null) modeLine.text = "";
        }

        void ApplyPalette()
        {
            bool high = settings != null && settings.Data.highContrast;
            var color = high ? Color.white : new Color(1f, 1f, 1f, 0.92f);
            if (fingerLabel != null) fingerLabel.color = color;
            if (explanation != null) explanation.color = high ? Color.white : new Color(1f, 0.92f, 0.72f, 0.95f);
        }
    }
}
