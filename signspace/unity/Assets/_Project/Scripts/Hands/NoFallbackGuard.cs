using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>
    /// Proves P1.2's third criterion - "no ray or controller fallback active" - instead of asserting it.
    ///
    /// WHY A GUARD AND NOT A NOTE
    /// --------------------------
    /// A ray interactor left enabled in a scene is invisible until a tester's pinch fails and the
    /// object moves anyway. That is the worst kind of bug for this phase: it makes a FAILING grab look
    /// like a passing one, and the 20/20 number in grab-log.md would be measuring the wrong thing.
    /// So the scene is scanned at start, by type name, and any ray, pointer or controller interactor is
    /// reported as an error with its GameObject named.
    ///
    /// WHY BY NAME
    /// -----------
    /// Referencing ISDK types directly would put the SDK back in the compile path this project has
    /// deliberately kept it out of (see MetaHandSource's header). Type names are stable enough for a
    /// guard whose only job is to shout.
    ///
    /// IT IS NOT FOOLPROOF, AND SAYS SO. It catches components whose type name contains a known
    /// token. A hand-written ray with no such name would slip through - which is why grab-log.md
    /// records that the scene contains no such component by construction: nothing in
    /// GrabTestBuilder creates one, and nothing else is in the scene.
    /// </summary>
    public class NoFallbackGuard : MonoBehaviour
    {
        [Tooltip("Scan on Start and log a single report.")]
        public bool scanOnStart = true;

        [Tooltip("Also re-scan periodically, in case something is added at runtime.")]
        public bool scanContinuously = false;

        public float scanEverySeconds = 5f;

        [Tooltip("Type-name fragments that indicate a ray, pointer or controller input path.")]
        public string[] forbiddenFragments =
        {
            "RayInteractor", "RaycastInteractor", "PointableCanvasModule", "TrackedDeviceGraphicRaycaster",
            "OVRRay", "HandRay", "ControllerInteractor", "ControllerPointer", "LinePointer"
        };

        /// <summary>What the last scan found. Empty means clean.</summary>
        public string LastReport { get; private set; } = "not scanned";

        public int ForbiddenCount { get; private set; }

        float _next;

        void Start()
        {
            if (scanOnStart) Scan();
        }

        void Update()
        {
            if (!scanContinuously) return;
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + Mathf.Max(1f, scanEverySeconds);
            Scan();
        }

        public void Scan()
        {
            var found = new List<string>();
            var behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();

            foreach (var mb in behaviours)
            {
                if (mb == null) continue;
                var go = mb.gameObject;
                if (go == null || !go.scene.IsValid()) continue;          // skip prefab assets
                string typeName = mb.GetType().Name;
                foreach (var fragment in forbiddenFragments)
                {
                    if (typeName.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    found.Add(typeName + " on '" + go.name + "'");
                    break;
                }
            }

            ForbiddenCount = found.Count;
            if (found.Count == 0)
            {
                LastReport = "clean: no ray, pointer or controller interactor in the scene";
                Debug.Log("[NoFallbackGuard] " + LastReport + " (scanned " + behaviours.Length + " components)");
                return;
            }

            var sb = new StringBuilder();
            sb.Append("FALLBACK PRESENT - P1.2 cannot be trusted until this is removed:\n");
            foreach (var f in found) sb.Append("  ").Append(f).Append('\n');
            LastReport = sb.ToString();
            Debug.LogError("[NoFallbackGuard] " + LastReport);
        }

        void OnGUI()
        {
            if (ForbiddenCount == 0) return;
            if (!Debug.isDebugBuild && !Application.isEditor) return;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(12f, Screen.height * 0.5f, 520f, 26f + 18f * Mathf.Min(4, ForbiddenCount)),
                            Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(18f, Screen.height * 0.5f + 4f, 500f, 100f), LastReport);
        }
    }
}
