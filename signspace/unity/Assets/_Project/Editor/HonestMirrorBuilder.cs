using Signspace.Matching;
using Signspace.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Signspace.Editor
{
    /// <summary>
    /// Builds Assets/_Project/Prefabs/HonestMirror.prefab and plants it in the scene, reachable from a
    /// physical object (P3.4).
    ///
    /// WHY GENERATED AND NOT HAND-WRITTEN
    /// ----------------------------------
    /// The panel is a world-space canvas, three text blocks and four world-space slots that each receive
    /// a posed hand at runtime. Hand-authoring that as YAML is how an afternoon disappears into a broken
    /// asset - and nobody here can open the editor to fix it. The repository holds the recipe; the prefab
    /// is what running it produces.
    ///
    /// THE STRUCTURE, AND WHY IT IS NOT ALL ON ONE CANVAS
    /// -------------------------------------------------
    ///   HonestMirror            (component; stays ACTIVE so the load coroutine runs while hidden)
    ///     Content               (hidden until you touch the plinth - the panel is something you find)
    ///       Canvas              world-space, scaled 0.0007 so 1100x760 rect units = ~0.77x0.53 m
    ///         CountLine, Reasons, Credit
    ///       Specimens           four world-space slots, scale 1
    ///     HonestMirrorPlinth    the object you pinch to open it (separate scene object)
    ///
    /// The hands are metres-scale objects built by HandVisual. Parenting them under the canvas would
    /// multiply them by 0.0007 and they would vanish, so the slots live beside the canvas in metres.
    /// </summary>
    public static class HonestMirrorBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/HonestMirror.prefab";
        const string MaterialsFolder = BuildMaterials.Folder;

        static readonly Color PanelColor = new Color(0.08f, 0.09f, 0.11f, 0.94f);
        static readonly Color TextColor = new Color(0.90f, 0.91f, 0.93f, 1f);
        static readonly Color CreditColor = new Color(0.62f, 0.64f, 0.68f, 1f);
        static readonly Color PlinthColor = new Color(0.30f, 0.31f, 0.34f, 1f);
        static readonly Color ExcludedColor = new Color(0.42f, 0.44f, 0.48f, 1f);

        /// <summary>Panel size in rect units and the canvas scale that turns it into metres.</summary>
        static readonly Vector2 PanelSize = new Vector2(1100f, 760f);
        const float PixelsToMetres = 0.0007f;

        [MenuItem("SIGNSPACE/P3.4 Build honest mirror")]
        public static void BuildHonestMirror()
        {
            BuildMaterials.EnsureFolder("Assets/_Project/Prefabs");

            var excludedMaterial = BuildMaterials.Get(MaterialsFolder + "/HandExcluded.mat", ExcludedColor);

            // Idempotent: clear any previous instance before planting a new one.
            foreach (var old in Object.FindObjectsByType<HonestMirror>(FindObjectsSortMode.None))
                Object.DestroyImmediate(old.gameObject);
            foreach (var old in Object.FindObjectsByType<PanelOpener>(FindObjectsSortMode.None))
                Object.DestroyImmediate(old.gameObject);

            var prefab = BuildPanelPrefab(excludedMaterial);
            GameObject instance;
            if (prefab != null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = new Vector3(0f, 1.30f, 1.00f);
                instance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);   // faces a user at the origin
            }
            else
            {
                instance = new GameObject("HonestMirror");
                instance.AddComponent<HonestMirror>();
            }
            instance.name = "HonestMirror";

            var panel = instance.GetComponent<HonestMirror>();
            var plinth = BuildPlinth(panel);

            Debug.Log("[HonestMirrorBuilder] " + PrefabPath + " built; plinth at " + plinth.transform.position +
                      ". Pinch the plinth to open the panel.");
        }

        public static GameObject BuildPanelPrefab(Material excludedMaterial)
        {
            var root = new GameObject("HonestMirror");
            var content = new GameObject("Content");
            content.transform.SetParent(root.transform, false);

            // ---- the canvas: count line, reasons, credit --------------------------------
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(Image));
            canvasGo.transform.SetParent(content.transform, false);
            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = PanelSize;
            canvasRect.localScale = Vector3.one * PixelsToMetres;
            canvasRect.localPosition = Vector3.zero;

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var background = canvasGo.GetComponent<Image>();
            background.color = PanelColor;
            background.raycastTarget = false;

            var font = DefaultFont();

            var header = MakeText(canvasGo.transform, "CountLine", font, 38, TextColor,
                                  new Vector2(0f, -34f), new Vector2(1010f, 120f));
            var body = MakeText(canvasGo.transform, "Reasons", font, 24, TextColor,
                                new Vector2(0f, -158f), new Vector2(1010f, 360f));
            var credit = MakeText(canvasGo.transform, "Credit", font, 18, CreditColor,
                                  new Vector2(0f, -534f), new Vector2(1010f, 90f));

            // ---- the specimens: four world-space slots, in metres -----------------------
            var specimens = new GameObject("Specimens");
            specimens.transform.SetParent(content.transform, false);
            specimens.transform.localPosition = new Vector3(0f, -0.34f, 0.02f);

            var slots = new Transform[4];
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = new GameObject("HandSlot" + i);
                slot.transform.SetParent(specimens.transform, false);
                slot.transform.localPosition = new Vector3(-0.27f + i * 0.18f, 0f, 0f);
                slot.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // palm toward the reader
                slots[i] = slot.transform;
            }

            var panel = root.AddComponent<HonestMirror>();
            panel.content = content;
            panel.header = header;
            panel.body = body;
            panel.credit = credit;
            panel.handSlots = slots;
            panel.excludedMaterial = excludedMaterial;

            GameObject saved = null;
            try
            {
                saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[HonestMirrorBuilder] wrote " + PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
            return saved;
        }

        /// <summary>
        /// The object you pinch to open the panel: a small plinth with a bright bead on top, standing where
        /// a hand naturally falls. P4 re-homes this onto the shelf; until then it stands alone so the panel
        /// is reachable and testable.
        /// </summary>
        static GameObject BuildPlinth(HonestMirror panel)
        {
            var plinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plinth.name = "HonestMirrorPlinth";
            plinth.transform.position = new Vector3(0f, 0.62f, 0.90f);
            plinth.transform.localScale = new Vector3(0.16f, 0.06f, 0.16f);
            var plinthRenderer = plinth.GetComponent<Renderer>();
            if (plinthRenderer != null)
                plinthRenderer.sharedMaterial = BuildMaterials.Get(MaterialsFolder + "/Plinth.mat", PlinthColor);

            var target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.name = "HonestMirrorTarget";
            target.transform.SetParent(plinth.transform, false);
            target.transform.localScale = new Vector3(0.45f, 1.15f, 0.45f);   // cancels the plinth flattening
            var targetRenderer = target.GetComponent<Renderer>();
            if (targetRenderer != null)
                targetRenderer.sharedMaterial = BuildMaterials.Get(MaterialsFolder + "/PlinthTarget.mat", TextColor);

            var opener = target.AddComponent<PanelOpener>();
            opener.panel = panel;
            return plinth;
        }

        // ------------------------------------------------------------------ helpers

        static Text MakeText(Transform parent, string name, Font font, int size, Color color,
                             Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;

            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// The built-in font's name has moved across Unity versions (Arial.ttf before 2022, then
        /// LegacyRuntime.ttf). Asking for the wrong one logs and returns null, which would ship an
        /// invisible panel - so every route is tried and the failure is loud.
        /// </summary>
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
            if (font == null)
                Debug.LogError("[HonestMirrorBuilder] no font available; the panel's text will not render. " +
                               "Assign a font to its Text components before shipping.");
            return font;
        }

    }
}
