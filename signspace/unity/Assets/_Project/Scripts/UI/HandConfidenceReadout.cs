using System.Text;
using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>
    /// The P1.1 confidence readout: what the tracker thinks of each hand, on screen, in a dev build.
    ///
    /// WHY A READOUT AND NOT A LOG
    /// ---------------------------
    /// "Does it track?" is not answerable from a log while wearing a headset and waving. The whole
    /// point of P1 is to judge tracking with your eyes on your own hands, so the number has to be
    /// in the same view. It is also how the two P1.1 unknowns get answered on day one: the mapped
    /// joint count tells you instantly whether the device gave us the 26-joint OpenXR layout or the
    /// 20-joint legacy one, without pulling a file off the headset.
    ///
    /// WHY IMGUI AND NOT A CANVAS
    /// --------------------------
    /// No prefab, no canvas, no material, no extra draw call when off, and it cannot be broken by a
    /// scene rebuild. It is drawn only when the flag below allows it, and the flag defaults to
    /// "development builds only".
    ///
    /// COST WHEN OFF: one boolean test per frame.
    /// </summary>
    public class HandConfidenceReadout : MonoBehaviour
    {
        [Tooltip("The two tracked hands. Leave empty to find every MetaHandSource in the scene.")]
        public MetaHandSource[] hands;

        [Tooltip("Draw in release builds too. Off by default: a shipped app must not show debug text.")]
        public bool showInRelease = false;

        [Tooltip("Corner. The overlay is placed away from the debug overlay so they do not overlap.")]
        public Vector2 origin = new Vector2(12f, 12f);

        [Tooltip("Refresh rate of the text. 8 Hz is unreadable-but-fine and costs nothing.")]
        public float refreshHz = 8f;

        [Tooltip("Hands from the tracker count as visible: the fade-in of the readout itself.")]
        public float scale = 1f;

        readonly StringBuilder _sb = new StringBuilder(512);
        float _next;
        string _text = "";
        GUIStyle _style;

        bool Allowed => showInRelease || Debug.isDebugBuild || Application.isEditor;

        void Awake()
        {
            if (hands == null || hands.Length == 0)
            {
#if UNITY_2023_1_OR_NEWER
                hands = FindObjectsByType<MetaHandSource>(FindObjectsSortMode.None);
#else
                hands = FindObjectsOfType<MetaHandSource>();
#endif
            }
            _style = new GUIStyle
            {
                fontSize = Mathf.RoundToInt(14 * scale),
                richText = false,
                normal = { textColor = new Color(0.85f, 0.95f, 1f, 1f) },
                padding = new RectOffset(6, 6, 4, 4)
            };
        }

        void Update()
        {
            if (!Allowed) return;
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 1f / Mathf.Max(0.1f, refreshHz);

            _sb.Length = 0;
            _sb.Append("HANDS  ").Append(Mathf.RoundToInt(1f / Mathf.Max(0.0001f, Time.smoothDeltaTime))).Append(" fps\n");

            if (hands != null)
            {
                foreach (var h in hands)
                {
                    if (h == null) continue;
                    _sb.Append(h.hand).Append(": ")
                       .Append(h.IsTracked ? "tracked" : "LOST")
                       .Append("  conf ").Append(h.Confidence.ToString("0.00"))
                       .Append("  joints ").Append(h.MappedJoints).Append("/26")
                       .Append("  bones ").Append(h.DeviceBoneCount)
                       .Append("  pinch ").Append(h.PinchStrength.ToString("0.00"))
                       .Append(h.IsPinching ? " CLOSED" : "")
                       .Append('\n');
                }
            }

            // The draw cost is measured from the meshes attached, not from a comment.
            if (hands != null && hands.Length > 0 && hands[0] != null && hands[0].visual != null)
            {
                HandVisual.HandCost one = hands[0].visual.CountCost();
                int n = hands.Length;
                _sb.Append("cost/hand: ").Append(one.ToString())
                   .Append("  ·  both: ").Append(one.renderers * n).Append(" renderers, ")
                   .Append(one.triangles * n).Append(" tris");
            }

            _text = _sb.ToString();
        }

        void OnGUI()
        {
            if (!Allowed || string.IsNullOrEmpty(_text)) return;
            var size = _style.CalcSize(new GUIContent(_text));
            var rect = new Rect(origin.x, origin.y, size.x + 8f, size.y + 8f);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(rect, _text, _style);
        }
    }
}
