using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Signspace.Matching;
using Signspace.UI;

namespace Signspace.Editor
{
    /// <summary>
    /// Builds the P3 feedback views from code, so "the near-miss is implemented" cannot mean
    /// "the near-miss exists in a script nobody instantiated".
    ///
    /// The canvas is Screen Space - Camera rather than World Space: a head-locked panel is the
    /// correct choice for a line that must be read in the two seconds after an attempt, and it
    /// avoids the world-space orientation trap (a canvas facing the wrong way renders nothing and
    /// reports no error). The mirror is still opened by a physical object in the room.
    /// </summary>
    public static class FeedbackUiBuilder
    {
        public static HonestMirror Build(Transform rigRoot, Camera cam, Matcher matcher,
                                         SignSetLoader loader)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("FeedbackCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1.1f;
            canvas.sortingOrder = 20;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var canvasGroup = canvasGo.AddComponent<CanvasGroup>();

            // ---- near-miss line -------------------------------------------------------------
            var nearMissGo = new GameObject("NearMiss", typeof(RectTransform));
            nearMissGo.transform.SetParent(canvasGo.transform, false);
            var nmRect = (RectTransform)nearMissGo.transform;
            nmRect.anchorMin = new Vector2(0.5f, 0f);
            nmRect.anchorMax = new Vector2(0.5f, 0f);
            nmRect.pivot = new Vector2(0.5f, 0f);
            nmRect.anchoredPosition = new Vector2(0f, 150f);
            nmRect.sizeDelta = new Vector2(1100f, 160f);

            var nearMissText = nearMissGo.AddComponent<Text>();
            nearMissText.font = font;
            nearMissText.fontSize = 38;
            nearMissText.alignment = TextAnchor.MiddleCenter;
            nearMissText.horizontalOverflow = HorizontalWrapMode.Wrap;
            nearMissText.verticalOverflow = VerticalWrapMode.Overflow;
            nearMissText.color = new Color(0.97f, 0.95f, 0.90f, 1f);
            var nmShadow = nearMissGo.AddComponent<Shadow>();
            nmShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            nmShadow.effectDistance = new Vector2(2f, -2f);

            var nmGroup = nearMissGo.AddComponent<CanvasGroup>();
            nmGroup.alpha = 0f;
            nmGroup.interactable = false;
            nmGroup.blocksRaycasts = false;

            var nearMiss = nearMissGo.AddComponent<NearMissDisplay>();
            nearMiss.matcher = matcher;
            nearMiss.label = nearMissText;
            nearMiss.group = nmGroup;

            // ---- honest mirror --------------------------------------------------------------
            var mirrorGo = new GameObject("HonestMirror");
            var mirror = mirrorGo.AddComponent<HonestMirror>();
            mirror.loader = loader;

            var panelGo = new GameObject("HonestMirrorPanel", typeof(RectTransform));
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelRect = (RectTransform)panelGo.transform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1050f, 800f);
            panelRect.anchoredPosition = Vector2.zero;

            var bg = panelGo.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.06f, 0.08f, 0.94f);

            var header = MakeText(panelGo.transform, "Header", font, 44, TextAnchor.UpperCenter,
                                  new Vector2(0.5f, 1f), new Vector2(-60f, -50f), new Vector2(930f, 90f));
            var body = MakeText(panelGo.transform, "Body", font, 30, TextAnchor.UpperLeft,
                                new Vector2(0.5f, 1f), new Vector2(-60f, -170f), new Vector2(930f, 300f));
            var credit = MakeText(panelGo.transform, "Credit", font, 22, TextAnchor.LowerCenter,
                                  new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(930f, 70f));
            credit.color = new Color(0.72f, 0.76f, 0.80f, 1f);

            // Excluded shapes, rendered from their real probe pose files at start (P3.4). Empty
            // slots stay dim rather than showing a guessed hand. G3 asks for correct renders, so
            // the panel must not be text-only.
            var slots = new RawImage[mirror.excludedShapes.Length];
            const float spacing = 190f;
            float startX = -(slots.Length - 1) * spacing * 0.5f;
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = MakeShapeSlot(panelGo.transform, mirror.excludedShapes[i], font,
                                         new Vector2(startX + i * spacing, 168f));
            }
            mirror.thumbnails = slots;

            mirror.root = panelGo;
            mirror.header = header;
            mirror.body = body;
            mirror.credit = credit;
            panelGo.SetActive(false);

            // ---- pause indicator ------------------------------------------------------------
            var dotGo = new GameObject("ConfidenceDot", typeof(RectTransform));
            dotGo.transform.SetParent(canvasGo.transform, false);
            var dotRect = (RectTransform)dotGo.transform;
            dotRect.anchorMin = dotRect.anchorMax = new Vector2(0.5f, 1f);
            dotRect.pivot = new Vector2(0.5f, 1f);
            dotRect.anchoredPosition = new Vector2(0f, -40f);
            dotRect.sizeDelta = new Vector2(16f, 16f);
            var dot = dotGo.AddComponent<Image>();
            dot.color = new Color(0.85f, 0.92f, 1f, 0f);

            var confidence = canvasGo.AddComponent<TrackingConfidence>();
            confidence.matcher = matcher;
            confidence.group = canvasGroup;
            confidence.pulse = dot;

            // ---- the physical handle --------------------------------------------------------
            // P3.4: the mirror is reached from inside the space. This is provisional until P4's
            // shelf exists; the binding is one event, so replacing the shelf does not touch the panel.
            var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = "HonestMirrorHandle";
            handle.transform.SetParent(rigRoot, false);
            handle.transform.localPosition = new Vector3(0.42f, 0.95f, 0.55f);
            handle.transform.localScale = Vector3.one * 0.12f;
            var handleRenderer = handle.GetComponent<Renderer>();
            if (handleRenderer != null)
                handleRenderer.material.color = new Color(0.95f, 0.89f, 0.76f);

            var grabbable = handle.AddComponent<Grabbable>();
            grabbable.radius = 0.06f;
            grabbable.grabDistance = 0.16f;
            var binding = handle.AddComponent<HonestMirrorHandle>();
            binding.mirror = mirror;

            // ---- the near-miss off switch (G3) ----------------------------------------------
            // A second block, smaller and below the mirror handle: one gesture silences the naming
            // for the session, and the block dims so the state is visible. Not a settings screen.
            var toggleGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            toggleGo.name = "NearMissToggle";
            toggleGo.transform.SetParent(rigRoot, false);
            toggleGo.transform.localPosition = new Vector3(0.42f, 0.78f, 0.55f);
            toggleGo.transform.localScale = Vector3.one * 0.075f;
            var toggleGrab = toggleGo.AddComponent<Grabbable>();
            toggleGrab.radius = 0.04f;
            toggleGrab.grabDistance = 0.14f;
            var toggle = toggleGo.AddComponent<NearMissToggle>();
            toggle.matcher = matcher;

            Debug.Log("[SceneBuilder] P3 feedback wired: near-miss line, honest mirror " +
                      "(panel + handle), pause indicator, near-miss off switch");

            return mirror;
        }

        static RawImage MakeShapeSlot(Transform parent, string shape, Font font, Vector2 position)
        {
            var go = new GameObject("Shape_" + shape, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(150f, 150f);

            var image = go.AddComponent<RawImage>();
            image.color = new Color(1f, 1f, 1f, 0.12f);
            image.raycastTarget = false;

            var label = MakeText(go.transform, "Label", font, 30, TextAnchor.UpperCenter,
                                 new Vector2(0.5f, 0f), new Vector2(0f, -40f), new Vector2(150f, 40f));
            label.text = shape;
            label.color = new Color(0.80f, 0.84f, 0.88f, 1f);
            return image;
        }

        static Text MakeText(Transform parent, string name, Font font, int size, TextAnchor anchor,
                             Vector2 anchorPoint, Vector2 position, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = anchorPoint;
            rect.pivot = anchorPoint;
            rect.anchoredPosition = position;
            rect.sizeDelta = sizeDelta;

            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = new Color(0.96f, 0.96f, 0.94f, 1f);
            return text;
        }
    }
}
