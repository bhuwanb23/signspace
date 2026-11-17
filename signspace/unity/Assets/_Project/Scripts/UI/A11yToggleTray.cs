using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Signspace.Core;
using Signspace.Gaze;
using Signspace.Hands;
using Signspace.Matching;

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

        /// <summary>Built blocks, including the calibrate block. The check asserts on this.</summary>
        public int BlockCount { get; private set; }

        public string Report { get; private set; } = "not built";

        readonly List<GameObject> _blocks = new List<GameObject>(10);

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
            Report = BlockCount + " switch blocks (" + all.Length + " toggles + calibrate)";
            Debug.Log("[A11yToggleTray] " + Report);
            return BlockCount;
        }

        void PlaceInFrontOfUser()
        {
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
            transform.position = t.position + t.forward * forward - Vector3.up * down + t.right * right;

            // Face the user, then tilt back so the plate is a tray rather than a wall.
            Vector3 toUser = transform.position - t.position;
            transform.rotation = Quaternion.LookRotation(toUser.normalized, Vector3.up) *
                                 Quaternion.Euler(52f, 0f, 0f);
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
            var mat = new Material(shader);
            mat.name = "A11yBlock_" + name;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.30f, 0.33f, 0.36f, 1f));
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(0.30f, 0.33f, 0.36f, 1f));
            if (renderer != null) renderer.sharedMaterial = mat;

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
