using UnityEngine;
using UnityEngine.UI;

namespace Signspace.Matching
{
    /// <summary>
    /// The debug overlay: a world-space panel inside the ~55 degree cone that
    /// shows the live match numbers.
    ///
    /// SETUP.md says to build this BEFORE anything visual: you cannot tune what
    /// you cannot see. Every number the matcher uses is on this panel, so a
    /// threshold can be changed in the inspector and judged on the device in
    /// one frame.
    ///
    /// It reads only from the Matcher's public state, so it can never disagree
    /// with the matcher.
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        public Matcher matcher;
        public SkeletonPoseSource poseSource;

        [Tooltip("Canvas in world space, placed inside the comfortable cone.")]
        public Canvas canvas;

        [Tooltip("Distance from the camera, in metres. Keep it inside the 2 ft airplane-seat shell.")]
        public float distance = 0.45f;

        [Tooltip("Hide the overlay in a release build.")]
        public bool hideInRelease = true;

        [Tooltip("How often the text refreshes. 12 Hz is plenty and costs nothing.")]
        public float refreshHz = 12f;

        Text _text;
        float _nextRefresh;

        void Awake()
        {
            if (canvas == null) canvas = GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                var cam = Camera.main;
                if (cam != null)
                {
                    canvas.transform.position = cam.transform.position + cam.transform.forward * distance;
                    canvas.transform.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
                }
            }
            _text = GetComponentInChildren<Text>();
        }

        void OnEnable()
        {
            if (matcher != null)
            {
                matcher.OnDistanceUpdated += OnDistance;
                matcher.OnFingerAnglesUpdated += OnFingers;
            }
        }

        void OnDisable()
        {
            if (matcher != null)
            {
                matcher.OnDistanceUpdated -= OnDistance;
                matcher.OnFingerAnglesUpdated -= OnFingers;
            }
        }

        float _distance, _bestOther;
        float[] _fingers = new float[5];

        void OnDistance(float distance, float bestOther)
        {
            _distance = distance;
            _bestOther = bestOther;
        }

        void OnFingers(float[] fingers)
        {
            if (fingers != null) _fingers = fingers;
        }

        void Update()
        {
            if (hideInRelease && !Debug.isDebugBuild) { SetVisible(false); return; }
            SetVisible(true);

            if (_text == null) return;
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 1f / Mathf.Max(1f, refreshHz);

            if (matcher == null) { _text.text = "no matcher"; return; }

            var sb = new System.Text.StringBuilder(220);
            sb.Append("target ").Append(matcher.targetShapeId).Append('\n');
            sb.Append("dist   ").Append(_distance.ToString("0.0")).Append(" deg\n");
            sb.Append("other  ").Append(_bestOther.ToString("0.0")).Append(" deg\n");
            sb.Append("band   ").Append(matcher.CurrentBand)
              .Append("   hold ").Append(matcher.HoldProgress01.ToString("0.00")).Append('\n');
            sb.Append("conf   ").Append(matcher.LastConfidence.ToString("0.00"))
              .Append(matcher.IsPaused ? "   PAUSED" : "").Append('\n');

            sb.Append("fingers ");
            for (int i = 0; i < _fingers.Length && i < PoseMath.FingerNames.Length; i++)
            {
                sb.Append(PoseMath.FingerNames[i][0]).Append(' ')
                  .Append(_fingers[i].ToString("0.0")).Append("  ");
            }
            sb.Append('\n');

            if (poseSource != null)
            {
                sb.Append("joints ").Append(poseSource.JointsFullyMapped ? "OK" : "PARTIAL").Append('\n');
            }
            sb.Append("lockedDeg ").Append(matcher.lockedDeg.ToString("0.0"))
              .Append("  closeDeg ").Append(matcher.closeDeg.ToString("0.0"))
              .Append("  nearMiss ").Append(matcher.nearMissMaxDeg.ToString("0.0"));

            _text.text = sb.ToString();
        }

        void SetVisible(bool visible)
        {
            if (canvas != null && canvas.gameObject.activeSelf != visible)
                canvas.gameObject.SetActive(visible);
        }
    }
}
