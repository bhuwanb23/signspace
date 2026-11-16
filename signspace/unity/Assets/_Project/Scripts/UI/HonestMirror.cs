using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Signspace.Matching;

namespace Signspace.UI
{
    /// <summary>
    /// The panel that publishes what the sensor cannot see (NOVELTY-AND-PRIOR-ART.md N2).
    ///
    /// Nobody in a field of 650 submissions advertises their platform's limits. This does, with the
    /// reasons, and it is a never-cut item: it is the cheapest trust-building object in the product
    /// and the one thing a Deaf judge is most likely to respect.
    ///
    /// Rules it must obey:
    ///   - numbers come from measurement (TRACKABILITY.md), never from rounding up to look better;
    ///   - every excluded shape is rendered CORRECTLY — an excluded shape shown wrong is worse
    ///     than not showing it;
    ///   - it is reached from inside the space (a shelf object), not a settings menu.
    /// </summary>
    public class HonestMirror : MonoBehaviour
    {
        [Header("Content")]
        [Tooltip("Shapes the harness cannot resolve. Rendered greyed, with a reason each.")]
        public string[] excludedShapes = { "M", "N", "T", "R" };
        public string excludedReason =
            "The cameras cannot see a finger that is behind another finger.";

        [Header("Hands (P2.7)")]
        [Tooltip("Which hand this build reads. Comes from HandSupport, so the panel cannot promise a " +
                 "hand the build does not read. Empty hides the line.")]
        public string handLimitation = HandSupport.OneLineExplanation;

        [Header("Measured numbers — from TRACKABILITY.md, never invented")]
        [Tooltip("Off until P3.6 has run on a device. A panel that prints '0 of 0' is worse than one " +
                 "that says the measurement has not happened yet.")]
        public bool numbersMeasured;
        public int robustVariants;
        public int totalVariants;
        public string measuredOn = "";
        public string device = "Quest 3S";

        [Header("Authorship — mirrors set-meta.json (ADR-007)")]
        public SetMeta meta;
        [Tooltip("Read at Open() so the panel shows the credit line actually shipped in set-meta.json.")]
        public SignSetLoader loader;

        [Header("Shape renders (P3.4)")]
        [Tooltip("Where the excluded shapes' pose files live. Probe/, because they are not shipped.")]
        public string probeFolder = "Probe";
        [Tooltip("One slot per excluded shape, in the same order. Filled from the real pose files.")]
        public RawImage[] thumbnails;

        [Header("View")]
        public GameObject root;
        public Text header;
        public Text body;
        public Text credit;

        readonly List<RenderTexture> _rendered = new List<RenderTexture>();
        Material _thumbMaterial;

        void Start()
        {
            if (root != null) root.SetActive(false);
            Build();
            StartCoroutine(RenderExcludedShapes());
        }

        /// <summary>
        /// Renders each excluded shape from its actual pose file into a texture for the panel.
        ///
        /// WHY RENDER INSTEAD OF DRAWING ICONS
        /// -----------------------------------
        /// The panel's own rule is that an excluded shape shown wrong is worse than not showing it.
        /// An icon is a drawing of an opinion; this is the same HandVisual and the same pose data the
        /// matcher uses, so the picture cannot disagree with the build. It is also why the shapes are
        /// read from Probe/ rather than invented here.
        ///
        /// One camera and one hand are reused for all four, rendered once at start into small
        /// textures and then destroyed. Nothing here runs per frame.
        /// </summary>
        IEnumerator RenderExcludedShapes()
        {
            if (thumbnails == null || thumbnails.Length == 0 || excludedShapes == null ||
                excludedShapes.Length == 0 || loader == null)
                yield break;

            _thumbMaterial = ThumbnailMaterial();

            var camGo = new GameObject("HonestMirrorThumbCamera");
            camGo.transform.SetParent(transform, false);
            camGo.layer = ThumbLayer;
            var cam = camGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.orthographic = true;
            cam.orthographicSize = 0.105f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.08f, 1f);
            cam.cullingMask = 1 << ThumbLayer;
            cam.transform.position = new Vector3(0f, 0.085f, -0.35f);
            cam.transform.LookAt(new Vector3(0f, 0.085f, 0f));

            for (int i = 0; i < thumbnails.Length && i < excludedShapes.Length; i++)
            {
                var slot = thumbnails[i];
                if (slot == null) continue;

                HandPose pose = null;
                yield return loader.LoadPose(probeFolder, excludedShapes[i], p => pose = p);
                if (pose == null)
                {
                    // Leave the slot empty rather than draw a shape we could not load. An empty
                    // rectangle is honest; a guessed handshape is not.
                    slot.color = new Color(1f, 1f, 1f, 0.12f);
                    continue;
                }

                var handGo = new GameObject("Thumb_" + excludedShapes[i]);
                handGo.transform.SetParent(camGo.transform, false);
                handGo.transform.localPosition = Vector3.zero;
                handGo.transform.localRotation = Quaternion.identity;
                handGo.layer = ThumbLayer;

                var visual = handGo.AddComponent<HandVisual>();
                visual.material = _thumbMaterial;
                visual.scale = 1f;
                visual.Build();
                visual.ApplyPose(pose.LocalRotations);

                // HandVisual builds its bones as new GameObjects, which default to layer 0. The
                // camera only sees ThumbLayer, so the whole tree has to be moved onto it or the
                // render comes back empty.
                foreach (var t in handGo.GetComponentsInChildren<Transform>(true))
                    t.gameObject.layer = ThumbLayer;

                var rt = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
                rt.Create();
                _rendered.Add(rt);

                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = null;

                slot.texture = rt;
                slot.color = Color.white;

                Destroy(handGo);
            }

            Destroy(camGo);
        }

        const int ThumbLayer = 31;

        static Material ThumbnailMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogWarning("[HonestMirror] no unlit shader found; shape renders will be magenta.");
                return null;
            }

            var material = new Material(shader);
            var tint = new Color(0.93f, 0.91f, 0.86f, 1f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
            return material;
        }

        void OnDestroy()
        {
            for (int i = 0; i < _rendered.Count; i++)
            {
                if (_rendered[i] == null) continue;
                _rendered[i].Release();
                Destroy(_rendered[i]);
            }
            _rendered.Clear();
            if (_thumbMaterial != null) Destroy(_thumbMaterial);
        }

        public void Open()
        {
            Build();
            if (root != null) root.SetActive(true);
        }

        public void Close()
        {
            if (root != null) root.SetActive(false);
        }

        public void Toggle()
        {
            if (root == null) return;
            if (root.activeSelf) Close(); else Open();
        }

        void Build()
        {
            SetMeta shipped = loader != null ? loader.Meta : meta;

            if (header != null)
            {
                header.text = numbersMeasured
                    ? $"{robustVariants} of {totalVariants} handshape variants are robustly trackable today"
                    : "Trackability review pending — no device measurement yet";
            }

            if (body != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine(excludedReason);
                sb.AppendLine();
                sb.Append("Not taught here: ");
                sb.AppendLine(string.Join(", ", excludedShapes));
                if (!string.IsNullOrEmpty(handLimitation))
                {
                    sb.AppendLine();
                    sb.AppendLine(handLimitation);
                }
                if (!string.IsNullOrEmpty(measuredOn))
                    sb.Append($"Measured on {device}, {measuredOn}.");
                body.text = sb.ToString();
            }

            if (credit != null)
            {
                // The authorship line is data, never copy (ADR-007). Whatever ships, ships truthfully.
                credit.text = shipped != null && !string.IsNullOrEmpty(shipped.creditLine)
                    ? shipped.creditLine
                    : "Handshapes authored against published ASL references. Deaf signer review in progress.";
            }
        }
    }
}
