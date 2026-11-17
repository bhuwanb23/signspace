using UnityEngine;
using UnityEngine.UI;
using Signspace.Core;
using Signspace.Gaze;

namespace Signspace.UI
{
    /// <summary>
    /// Builds the small in-world HUD the gaze path needs: the finger label, the fallback explanation,
    /// the source line, the audio captions and the dwell ring (P6.1, P6.2, P6.3, P6.5).
    ///
    /// WHY IT IS BUILT AT RUNTIME
    /// --------------------------
    /// The alternative is four hand-authored world-space canvases in the scene YAML, which nobody here
    /// can repair by hand and which would drift from the font and the sprite the code expects. One
    /// builder, called by the scene builder and by the checks, means the HUD the check measures is the
    /// HUD the build ships.
    ///
    /// It is placed once, in front of the camera, and then left alone. It does not follow the head: a
    /// text panel locked to the gaze direction is the one thing in this product that could make a
    /// seated user queasy, and the information it carries is not urgent enough to justify it.
    /// </summary>
    public class A11yHud : MonoBehaviour
    {
        public GazeCaption gazeCaption;
        public A11yCaptionPanel audioCaptions;
        public DwellSelector dwell;
        public A11ySettings settings;

        [Tooltip("Distance in front of the camera at the moment the scene starts.")]
        public float distance = 0.55f;
        [Tooltip("Metres below eye level.")]
        public float below = 0.06f;

        [Tooltip("Pixel-to-metre scale of the HUD canvas.")]
        public float pixelsToMetres = 0.00042f;

        public Canvas Canvas { get; private set; }
        public Image DwellRing { get; private set; }
        public Text FingerLabel { get; private set; }
        public Text Explanation { get; private set; }
        public Text ModeLine { get; private set; }
        public Text AudioCaption { get; private set; }

        public bool HighContrast { get; private set; }

        /// <summary>
        /// High contrast (P6.5, given an effect in P7): every HUD line becomes pure white at full alpha,
        /// and the fallback explanation stops being amber. The HUD sits over passthrough, so it has no
        /// panel behind it — the fix is maximum luminance on the text and nothing else to read.
        /// </summary>
        public void SetHighContrast(bool on)
        {
            HighContrast = on;
            var strong = on ? Color.white : new Color(0.72f, 0.78f, 0.86f, 0.95f);
            var caption = on ? Color.white : new Color(0.96f, 0.96f, 1f, 0.95f);
            var explanation = on ? Color.white : new Color(1f, 0.92f, 0.72f, 0.95f);

            if (ModeLine != null) ModeLine.color = strong;
            if (AudioCaption != null) AudioCaption.color = caption;
            if (Explanation != null) Explanation.color = explanation;
            if (FingerLabel != null) FingerLabel.color = on ? Color.white : new Color(1f, 1f, 1f, 0.96f);
            if (DwellRing != null) DwellRing.color = on ? Color.white : new Color(1f, 1f, 1f, 0.85f);
        }

        bool _built;

        void Start()
        {
            if (settings == null) settings = A11ySettings.Instance;
            Build();
        }

        /// <summary>Idempotent; the checks call it directly because they never run Start in play mode.</summary>
        public void Build()
        {
            if (_built) return;
            _built = true;

            var cam = Camera.main;
            Vector3 position = cam != null
                ? cam.transform.position + cam.transform.forward * distance - Vector3.up * below
                : new Vector3(0f, 1.25f, 0.6f);

            var canvasGo = new GameObject("A11yHudCanvas", typeof(RectTransform), typeof(Canvas));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.position = position;
            if (cam != null)
                canvasGo.transform.rotation = Quaternion.LookRotation(canvasGo.transform.position - cam.transform.position, Vector3.up);
            canvasGo.transform.localScale = Vector3.one * pixelsToMetres;

            Canvas = canvasGo.GetComponent<Canvas>();
            Canvas.renderMode = RenderMode.WorldSpace;

            var rect = canvasGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(700f, 300f);

            var font = DefaultFont();

            FingerLabel = MakeText(canvasGo.transform, "FingerLabel", font, 30,
                                   new Vector2(0f, -120f), new Vector2(660f, 44f), TextAnchor.MiddleCenter);
            Explanation = MakeText(canvasGo.transform, "Explanation", font, 24,
                                   new Vector2(0f, -168f), new Vector2(660f, 76f), TextAnchor.UpperCenter);
            ModeLine = MakeText(canvasGo.transform, "ModeLine", font, 18,
                                new Vector2(0f, -8f), new Vector2(660f, 30f), TextAnchor.MiddleCenter);
            AudioCaption = MakeText(canvasGo.transform, "AudioCaption", font, 26,
                                    new Vector2(0f, -40f), new Vector2(660f, 44f), TextAnchor.MiddleCenter);

            ModeLine.color = new Color(0.72f, 0.78f, 0.86f, 0.95f);
            AudioCaption.color = new Color(0.96f, 0.96f, 1f, 0.95f);
            Explanation.color = new Color(1f, 0.92f, 0.72f, 0.95f);
            if (settings == null) settings = A11ySettings.Instance;
            SetHighContrast(settings != null && settings.Get(A11yToggle.HighContrast));

            DwellRing = MakeRing(canvasGo.transform, new Vector2(0f, -70f));

            if (gazeCaption != null)
            {
                gazeCaption.fingerLabel = FingerLabel;
                gazeCaption.explanation = Explanation;
                gazeCaption.modeLine = ModeLine;
                gazeCaption.dwellRing = DwellRing;
                if (gazeCaption.highlighter == null) gazeCaption.highlighter = FindFirstObjectByType<GazeHighlighter>();
                if (gazeCaption.probe == null) gazeCaption.probe = FindFirstObjectByType<EyeTrackingProbe>();
                if (gazeCaption.dwell == null) gazeCaption.dwell = dwell;
            }

            if (audioCaptions != null && audioCaptions.caption == null)
                audioCaptions.caption = AudioCaption;

            if (dwell != null && dwell.ring == null) dwell.ring = DwellRing;
        }

        Image MakeRing(Transform parent, Vector2 anchored)
        {
            var go = new GameObject("DwellRing", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(56f, 56f);
            rect.anchoredPosition = anchored;

            var image = go.GetComponent<Image>();
            image.sprite = CircleSprite();
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillClockwise = true;
            image.fillAmount = 0f;
            image.color = new Color(1f, 0.85f, 0.4f, 0.95f);
            image.raycastTarget = false;
            return image;
        }

        static Sprite _circle;

        /// <summary>A ring texture, generated once. No sprite asset to lose in a batch checkout.</summary>
        static Sprite CircleSprite()
        {
            if (_circle != null) return _circle;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "A11yDwellRing";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            var clear = new Color(0f, 0f, 0f, 0f);
            var pixels = new Color[size * size];
            float outer = size * 0.5f - 1f;
            float inner = outer - size * 0.16f;
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                    pixels[y * size + x] = d <= outer && d >= inner ? Color.white : clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            _circle = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _circle;
        }

        static Text MakeText(Transform parent, string name, Font font, int size,
                             Vector2 anchored, Vector2 sizeDelta, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchored;

            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = align;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.color = new Color(1f, 1f, 1f, 0.92f);
            return text;
        }

        static Font DefaultFont()
        {
            Font font = null;
            try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (font == null) { try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            if (font == null)
            {
                var fonts = Resources.FindObjectsOfTypeAll<Font>();
                if (fonts != null && fonts.Length > 0) font = fonts[0];
            }
            return font;
        }
    }
}
