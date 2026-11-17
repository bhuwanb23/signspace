using Signspace.Spatial;
using Signspace.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Signspace.Editor
{
    /// <summary>
    /// Builds `Assets/_Project/Prefabs/Credits.prefab` and plants it on the shelf (P5.5).
    ///
    /// The credit has to be visible in the first minute and reachable afterwards, without becoming a
    /// settings menu. So the prefab is two things on one root: a small bead you pinch, and a world-space
    /// panel that opens itself once on first launch. The root follows the shelf, which means the credit
    /// is where the user's progress is, on a real surface or on the plinth.
    ///
    /// Generated rather than hand-authored for the same reason as the honest mirror: nobody here can
    /// open the editor to repair a broken YAML asset, so the repository holds the recipe.
    /// </summary>
    public static class CreditsBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Credits.prefab";
        const string MaterialsFolder = BuildMaterials.Folder;

        static readonly Color PanelColor = new Color(0.08f, 0.09f, 0.11f, 0.94f);
        static readonly Color TextColor = new Color(0.90f, 0.91f, 0.93f, 1f);
        static readonly Color BeadColor = new Color(0.80f, 0.84f, 0.90f, 1f);

        static readonly Vector2 PanelSize = new Vector2(900f, 420f);
        const float PixelsToMetres = 0.0006f;

        [MenuItem("SIGNSPACE/P5.5 Build credits")]
        public static void BuildCredits()
        {
            BuildMaterials.EnsureFolder("Assets/_Project/Prefabs");

            foreach (var old in Object.FindObjectsByType<CreditsPanel>(FindObjectsSortMode.None))
                Object.DestroyImmediate(old.gameObject);

            var prefab = BuildPrefab();
            if (prefab == null)
            {
                Debug.LogError("[CreditsBuilder] the prefab did not save; no credits object in the scene.");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "Credits";

            var panel = instance.GetComponent<CreditsPanel>();
            var shelf = Object.FindFirstObjectByType<ShelfAnchor>();
            if (shelf != null)
            {
                panel.follow = shelf.transform;
                instance.transform.position = shelf.transform.TransformPoint(panel.followOffset);
            }
            else
            {
                // No shelf in the scene: the panel is still reachable where it stands. It is not on the
                // shelf, and the check says so rather than pretending.
                instance.transform.position = new Vector3(0.26f, 0.62f, 0.90f);
                Debug.LogWarning("[CreditsBuilder] no ShelfAnchor in the scene; credits planted at a fixed " +
                                 "position instead of on the shelf.");
            }

            Debug.Log("[CreditsBuilder] " + PrefabPath + " built; panel " +
                      (shelf != null ? "follows the shelf" : "has no shelf") +
                      ", auto-show at " + panel.showOnFirstLaunchAfterSeconds + " s.");
        }

        public static GameObject BuildPrefab()
        {
            var root = new GameObject("Credits");
            var panel = root.AddComponent<CreditsPanel>();
            panel.followOffset = Vector3.zero;
            panel.metaFile = "Signs/set-meta.json";

            // ---- the panel ---------------------------------------------------------------
            var content = new GameObject("Content");
            content.transform.SetParent(root.transform, false);
            content.transform.localPosition = new Vector3(0.20f, 0.18f, 0.02f);

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
            var header = MakeText(canvasGo.transform, "Header", font, 30, TextColor,
                                  new Vector2(0f, -26f), new Vector2(830f, 50f));
            var body = MakeText(canvasGo.transform, "Body", font, 20, TextColor,
                                new Vector2(0f, -90f), new Vector2(830f, 280f));
            header.text = "The hand in this build";
            body.text = "Reading the authorship record...";

            content.SetActive(false);   // closed at rest; the first-launch timer opens it

            // ---- the bead you pinch ------------------------------------------------------
            var bead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bead.name = "CreditsBead";
            bead.transform.SetParent(root.transform, false);
            bead.transform.localPosition = new Vector3(0.20f, 0.035f, 0.03f);
            bead.transform.localScale = Vector3.one * 0.035f;
            var beadRenderer = bead.GetComponent<Renderer>();
            if (beadRenderer != null)
                beadRenderer.sharedMaterial = BuildMaterials.Get(MaterialsFolder + "/CreditsBead.mat", BeadColor);

            var handle = bead.AddComponent<CreditsHandle>();
            handle.panel = panel;

            panel.content = content;
            panel.header = header;
            panel.body = body;

            GameObject saved = null;
            try
            {
                saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[CreditsBuilder] wrote " + PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
            return saved;
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
                Debug.LogError("[CreditsBuilder] no font available; the panel's text will not render.");
            return font;
        }
    }
}
