using UnityEngine;
using UnityEngine.UI;
using Signspace.Core;
using Signspace.Matching;

namespace Signspace.UI
{
    /// <summary>
    /// Captions for the product's non-speech sounds (P6.5: captions default ON).
    ///
    /// What there is to caption: the band tones and the lock chime. A Deaf user who cannot hear the
    /// chime has to be told the same thing the chime says - that the shape landed - in the same moment,
    /// without looking for a setting. So this listens to the matcher's own events, not to the audio
    /// clips: if a clip is missing or muted the caption still appears, and the caption can never
    /// describe a sound that did not happen.
    ///
    /// It writes one short line. Not a scrolling transcript: there is no speech in the product, and
    /// pretending there is would be the kind of overstatement P6.7 exists to prevent.
    /// </summary>
    public class A11yCaptionPanel : MonoBehaviour
    {
        public Matcher matcher;
        public Text caption;
        public A11ySettings settings;

        [Tooltip("How long a caption stays after the sound. Long enough to read twice.")]
        public float visibleSeconds = 2.5f;

        public Color normalColor = new Color(1f, 1f, 1f, 0.92f);
        public Color highContrastColor = new Color(1f, 1f, 1f, 1f);
        public Color highContrastBackground = new Color(0f, 0f, 0f, 0.86f);

        public string LastCaption { get; private set; } = "";

        float _hideAt = -1f;
        Image _background;

        void Awake()
        {
            if (settings == null) settings = A11ySettings.Instance;
            _background = GetComponentInChildren<Image>();
            Clear();
        }

        void Start()
        {
            if (matcher == null) matcher = FindFirstObjectByType<Matcher>();
            if (matcher != null)
            {
                matcher.OnLocked += OnLocked;
                matcher.OnUnlocked += OnUnlocked;
                matcher.OnBandChanged += OnBand;
            }
            if (settings != null) settings.OnChanged += OnChanged;
            ApplyPalette();
        }

        void OnDestroy()
        {
            if (matcher != null)
            {
                matcher.OnLocked -= OnLocked;
                matcher.OnUnlocked -= OnUnlocked;
                matcher.OnBandChanged -= OnBand;
            }
            if (settings != null) settings.OnChanged -= OnChanged;
        }

        void OnLocked(string shapeId) => Say("locked: " + shapeId);
        void OnUnlocked(string shapeId) => Say("released: " + shapeId);

        void OnBand(Band band)
        {
            if (band == Band.Close) Say("close");
            else if (band == Band.Locked) { /* the lock chime is captioned by OnLocked */ }
            else if (band == Band.Warming) { /* silence: there is no warming sound */ }
        }

        void OnChanged(A11yToggle toggle)
        {
            if (toggle == A11yToggle.HighContrast) ApplyPalette();
            if (toggle == A11yToggle.Captions && !settings.Data.captionsOn) Clear();
        }

        void Say(string line)
        {
            if (settings != null && !settings.Data.captionsOn) return;
            LastCaption = line;
            if (caption != null) caption.text = line;
            if (caption != null) caption.gameObject.SetActive(true);
            _hideAt = Time.time + visibleSeconds;
        }

        void Update()
        {
            if (_hideAt > 0f && Time.time >= _hideAt)
            {
                _hideAt = -1f;
                if (caption != null) caption.text = "";
            }
        }

        void Clear()
        {
            LastCaption = "";
            if (caption != null) caption.text = "";
            _hideAt = -1f;
        }

        void ApplyPalette()
        {
            bool high = settings != null && settings.Data.highContrast;
            if (caption != null) caption.color = high ? highContrastColor : normalColor;
            if (_background != null) _background.color = high ? highContrastBackground : new Color(0f, 0f, 0f, 0.35f);
        }
    }
}
