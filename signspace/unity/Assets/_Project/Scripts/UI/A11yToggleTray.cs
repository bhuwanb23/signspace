using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Signspace.Core;
using Signspace.Gaze;
using Signspace.Hands;
using Signspace.Matching;
using Signspace.Spatial;

namespace Signspace.UI
{
    /// <summary>
    /// The accessibility tray (P6.5): seven switch blocks and a calibrate block, floating within reach.
    ///
    /// WHY IT FLOATS
    /// -------------
    /// The task says every switch must be reachable inside the space, not from a system menu. Attaching
    /// the tray to the shelf would tie it to a surface that might be a bed, a couch or a table, and to a
    /// shelf that can be moved. A tray placed at waist height in front of the user at the moment the
    /// scene starts is reachable in every room the app supports, including the plinth fallback, and it
    /// never overlaps the shelf the user is working at.
    ///
    /// It is generated in code for the same reason as the rest of the rig: nobody here can open the
    /// editor to repair a prefab, so the repository holds a recipe that a cold batch run can execute.
    /// </summary>
    public class A11yToggleTray : MonoBehaviour
    {
        public A11ySettings settings;
        public Calibration calibration;

        [Tooltip("Distance in front of the head. 0.42 m is inside arm's reach for nearly every adult.")]
        public float forward = 0.42f;
        [Tooltip("Below eye level, so the user looks down at it rather than through it.")]
        public float down = 0.20f;
        [Tooltip("To the user's right, clear of the reference hand's centre line.")]
        public float right = 0.14f;

        public float spacing = 0.047f;
        public float rowSpacing = 0.058f;
        public int columns = 4;

        [Header("Narrow-field-of-view pose (P7.3)")]
        [Tooltip("At the 70°×66° glasses profile the comfortable pose is 40° off-centre: too far around " +
                 "and too far down to be seen without turning the head. These pull the tray into the " +
                 "55° safe cone. Measured in P7Checks, not guessed.")]
        public float narrowForward = 0.38f;
        public float narrowDown = 0.075f;
        public float narrowRight = 0.04f;
        [Tooltip("Below this horizontal FoV the narrow pose applies. Matches ShelfReflow.narrowFovDegrees.")]
        public float narrowFovDegrees = 85f;

        [Tooltip("Optional. When wired, the tray poses itself for the current field of view.")]
        public ShelfReflow reflow;

        public bool IsNarrowPose { get; private set; }

        /// <summary>Built blocks, including the calibrate block. The check asserts on this.</summary>
        public int BlockCount { get; private set; }

        public string Report { get; private set; } = "not built";

        readonly List<GameObject> _blocks = new List<GameObject>(10);
        readonly List<Material> _plateMaterials = new List<Material>(10);
        readonly List<Text> _labels = new List<Text>(24);

        /// <summary>
        /// High contrast (P6.5). Until P7 this switch reached only the caption panel and the gaze caption:
        /// the HUD lines, this tray and the near-miss copy ignored it, which is the worst of both — a
        /// switch that half works teaches the user it does nothing.
        ///
        /// It makes the plates near-black and the text pure white, which is the largest luminance
        /// separation available without colour. It is a palette swap, not a filter, because a post
        /// effect would also change the passthrough the user is looking through.
        /// </summary>
        public void SetHighContrast(bool on)
        {
            var plate = on ? new Color(0.03f, 0.03f, 0.035f, 1f) : new Color(0.30f, 0.33f, 0.36f, 1f);
            foreach (var material in _plateMaterials)
            {
                if (material == null) continue;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", plate);
                if (material.HasProperty("_Color")) material.SetColor("_Color", plate);
            }

            var text = on ? Color.white : new Color(0.92f, 0.93f, 0.96f, 1f);
            foreach (var label in _labels)
                if (label != null) label.color = text;

            HighContrast = on;
        }

        public bool HighContrast { get; private set; }

        void Start()
        {
            if (settings == null) settings = A11ySettings.Instance;
            if (calibration == null) calibration = FindFirstObjectByType<Calibration>();
            Build();
        }

        /// <summary>Idempotent. Also called by the checks, which have no camera-following Start.</summary>
        public int Build()
        {
            if (_blocks.Count > 0)
            {
                Report = BlockCount + " switch blocks";
                return BlockCount;
            }

            PlaceInFrontOfUser();

            var all = A11ySettings.All;
            int index = 0;
            for (int i = 0; i < all.Length; i++, index++)
                _blocks.Add(MakeToggleBlock(all[i], index));

            // The calibrate block is not a toggle: it starts or skips the 30-second flow, so it must be
            // reachable by the same two inputs as everything else.
            var cal = MakePlate("Switch_Calibrate", index);
            var calLabel = MakeLabel(cal.transform, "Calibrate", "your hand");
            var handle = cal.AddComponent<CalibrationHandle>();
            handle.calibration = calibration;
            handle.title = calLabel.Item1;
            handle.value = calLabel.Item2;
            var action = cal.AddComponent<DwellAction>();
            action.kind = DwellActionKind.StartCalibration;
            action.calibration = calibration;
            _blocks.Add(cal);

            BlockCount = _blocks.Count;

            // Apply the saved palette last, so a scene built with high contrast on does not start in the
            // default colours and only correct itself on the first toggle.
            SetHighContrast(settings != null && settings.Get(A11yToggle.HighContrast));

            Report = BlockCount + " switch blocks (" + all.Length + " toggles + calibrate)";
            Debug.Log("[A11yToggleTray] " + Report);
            return BlockCount;
        }

        void PlaceInFrontOfUser()
        {
            float horizontalFov = reflow != null && reflow.HorizontalFovDegrees > 0f
                ? reflow.HorizontalFovDegrees
                : 110f;
            ApplyFov(horizontalFov);

            float useForward = IsNarrowPose ? narrowForward : forward;
            float useDown = IsNarrowPose ? narrowDown : down;
            float useRight = IsNarrowPose ? narrowRight : right;

            var cam = Camera.main;
            if (cam == null)
            {
                // A batch build has no runtime camera pose worth trusting; a fixed comfortable point
                // keeps the object addressable and the check honest.
                transform.position = new Vector3(0.30f, 1.05f, 0.70f);
                transform.rotation = Quaternion.identity;
                return;
            }

            var t = cam.transform;
            transform.position = t.position + t.forward * useForward - Vector3.up * useDown + t.right * useRight;

            // Face the user, then tilt back so the plate is a tray rather than a wall.
            Vector3 toUser = transform.position - t.position;
            transform.rotation = Quaternion.LookRotation(toUser.normalized, Vector3.up) *
                                 Quaternion.Euler(52f, 0f, 0f);
        }

        /// <summary>Chooses the wide or narrow pose. Pure, so the check can exercise both profiles.</summary>
        public bool ApplyFov(float horizontalFovDegrees)
        {
            IsNarrowPose = horizontalFovDegrees > 0f && horizontalFovDegrees < narrowFovDegrees;
            return IsNarrowPose;
        }

        /// <summary>
        /// The worst-case angular offset of any part of the tray from the forward axis, in degrees.
        /// The block grid is 4 x 2 at most, plus half a plate either side; the pose numbers are the ones
        /// `PlaceInFrontOfUser` uses, so this measures the thing that ships.
        /// </summary>
        public float MaxAngleDegrees(float horizontalFovDegrees)
        {
            bool narrow = horizontalFovDegrees > 0f && horizontalFovDegrees < narrowFovDegrees;
            float z = narrow ? narrowForward : forward;
            float y = (narrow ? narrowDown : down) + rowSpacing;
            float x = (narrow ? narrowRight : right) + (columns - 1) * 0.5f * spacing + 0.018f;
            return Mathf.Atan2(Mathf.Sqrt(x * x + y * y), z) * Mathf.Rad2Deg;
        }

        GameObject MakePlate(string name, int index)
        {
            int col = index % Mathf.Max(1, columns);
            int row = index / Mathf.Max(1, columns);

            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = name;
            plate.transform.SetParent(transform, false);
            // Centred on the tray: 4 columns -> -1.5 .. +1.5 spacings.
            float x = (col - (columns - 1) * 0.5f) * spacing;
            float z = -row * rowSpacing + 0.03f;
            plate.transform.localPosition = new Vector3(x, 0f, z);
            plate.transform.localScale = new Vector3(0.036f, 0.010f, 0.030f);

            var renderer = plate.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            if (shader != null)
            {
                var mat = new Material(shader);
                mat.name = "A11yBlock_" + name;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.30f, 0.33f, 0.36f, 1f));
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(0.30f, 0.33f, 0.36f, 1f));
                if (renderer != null) renderer.sharedMaterial = mat;
                _plateMaterials.Add(mat);
            }
            else
            {
                // A null shader would throw and take the whole tray down at Start. The block still works
                // (the collider and the label are what the user interacts with), so it is a warning.
                Debug.LogWarning("[A11yToggleTray] no usable shader for the switch blocks; the plates " +
                                 "use the primitive's default material.");
            }

            var grab = plate.AddComponent<Grabbable>();
            grab.radius = 0.045f;
            return plate;
        }

        GameObject MakeToggleBlock(A11yToggle toggle, int index)
        {
            var plate = MakePlate("Switch_" + toggle, index);

            var label = MakeLabel(plate.transform, A11ySettings.Title(toggle), "off");
            var block = plate.AddComponent<A11yToggleBlock>();
            block.toggle = toggle;
            block.settings = settings;
            block.title = label.Item1;
            block.value = label.Item2;

            var action = plate.AddComponent<DwellAction>();
            action.kind = DwellActionKind.ToggleA11y;
            action.toggle = toggle;
            return plate;
        }

        (Text, Text) MakeLabel(Transform parent, string titleText, string valueText)
        {
            var canvasGo = new GameObject("Label", typeof(RectTransform), typeof(Canvas));
            canvasGo.transform.SetParent(parent, false);
            // A plaque on the user-facing side of the plate. Facing the user, not lying on the top
            // face: text on the top face is read upside down by someone looking down at the tray.
            canvasGo.transform.localPosition = new Vector3(0f, 0.006f, -0.016f);
            canvasGo.transform.localRotation = Quaternion.identity;
            canvasGo.transform.localScale = Vector3.one * 0.00042f;

            var rect = canvasGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150f, 90f);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var font = DefaultFont();
            var title = MakeText(canvasGo.transform, "Title", font, 26, new Vector2(0f, -4f), new Vector2(150f, 40f));
            var value = MakeText(canvasGo.transform, "Value", font, 22, new Vector2(0f, -44f), new Vector2(150f, 40f));
            title.text = titleText;
            value.text = valueText;
            title.color = new Color(0.92f, 0.93f, 0.96f, 1f);
            value.color = new Color(0.78f, 0.82f, 0.88f, 1f);
            _labels.Add(title);
            _labels.Add(value);
            return (title, value);
        }

        static Text MakeText(Transform parent, string name, Font font, int size,
                             Vector2 anchored, Vector2 sizeDelta)
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
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
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
