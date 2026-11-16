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

        [Tooltip("The flicker test's window: band changes inside this many seconds are counted. "
               + "P2.3's done-when is 'no flicker over a 30 s test', so the overlay counts it.")]
        public float bandWindowSeconds = 30f;

        Text _text;
        float _nextRefresh;
        readonly System.Collections.Generic.List<float> _bandChanges = new System.Collections.Generic.List<float>();

        /// <summary>
        /// Counts displayed band changes. Park a hand on an edge, watch this for 30 s: a number above
        /// ~1 means the meter strobes, whatever the offline simulation said about that edge.
        /// </summary>
        void OnBand(Band band)
        {
            _bandChanges.Add(Time.time);
            PruneBandChanges();
        }

        void PruneBandChanges()
        {
            float cutoff = Time.time - bandWindowSeconds;
            while (_bandChanges.Count > 0 && _bandChanges[0] < cutoff) _bandChanges.RemoveAt(0);
        }

        void Awake()
        {
            // Development view only. It is the panel that knows what "LOST" and "no matcher" mean,
            // and a release build must not be able to show either (P2.3 step 5).
            if (!Signspace.Core.DevOnly.Allowed) { gameObject.SetActive(false); return; }

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
                matcher.OnBandChanged += OnBand;
            }
        }

        void OnDisable()
        {
            if (matcher != null)
            {
                matcher.OnDistanceUpdated -= OnDistance;
                matcher.OnFingerAnglesUpdated -= OnFingers;
                matcher.OnBandChanged -= OnBand;
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

            var sb = new System.Text.StringBuilder(320);
            sb.Append("target ").Append(matcher.targetShapeId).Append('\n');
            sb.Append("dist   ").Append(_distance.ToString("0.0")).Append(" deg\n");
            sb.Append("flat   ").Append(matcher.FlatDegrees.ToString("0.0"))
              .Append(" deg   (flat mean - ").Append(DistanceGapNote()).Append(")\n");
            sb.Append("other  ").Append(_bestOther.ToString("0.0")).Append(" deg\n");
            PruneBandChanges();
            sb.Append("band   ").Append(matcher.CurrentBand)
              .Append("   hold ").Append(matcher.HoldPhase)
              .Append(' ').Append(matcher.HoldSecondsHeld.ToString("0.00"))
              .Append('/').Append(matcher.holdSeconds.ToString("0.00")).Append('s')
              .Append("  ").Append(matcher.HoldProgress01.ToString("0.00"))
              .Append("   changes/").Append(bandWindowSeconds.ToString("0")).Append("s ")
              .Append(_bandChanges.Count).Append('\n');
            sb.Append("conf   ").Append(matcher.LastConfidence.ToString("0.00"))
              .Append(matcher.IsPaused ? "   PAUSED" : "").Append('\n');

            sb.Append("fingers ");
            for (int i = 0; i < _fingers.Length && i < PoseMath.FingerNames.Length; i++)
            {
                sb.Append(PoseMath.FingerNames[i][0]).Append(' ')
                  .Append(_fingers[i].ToString("0.0")).Append("  ");
            }
            sb.Append('\n');

            // The palm term can be what is holding the lock, and then the worst joints are innocent.
            // It is additive now (P2.6), so the comparison is "is the palm term bigger than the finger
            // term", not "is it equal to the total".
            // P2.7: which hand is actually playing, and what happened to the other one. On a
            // left-handed tester this line is the difference between "broken" and "not in this build".
            if (poseSource != null)
                sb.Append("hand   ").Append(poseSource.DrivesMatcher ? "feeding the matcher" : "not feeding it")
                  .Append("   ").Append(Signspace.Matching.HandSupport.Report()).Append('\n');

            sb.Append("palm   O ").Append(matcher.OrientationDegrees.ToString("0.0"))
              .Append("  +").Append(matcher.OrientationPenaltyDegrees.ToString("0.0"))
              .Append("  k ").Append(matcher.orientationWeightK.ToString("0.00"))
              .Append(" slack ").Append(matcher.orientationSlackDeg.ToString("0"))
              .Append("  ").Append(matcher.PalmBasis).Append('\n');
            sb.Append("       swing ").Append(matcher.PalmSwingDegrees.ToString("0.0"))
              .Append("  twist ").Append(matcher.PalmTwistDegrees.ToString("0.0"))
              .Append("   ").Append(matcher.BasisNote).Append('\n');

            if (matcher.OrientationPenaltyDegrees > 0f &&
                matcher.OrientationPenaltyDegrees > Mathf.Max(0f, _distance - matcher.OrientationPenaltyDegrees))
                sb.Append("gate   the palm is the bigger term - the fingers are not the problem\n");

            // P2.2: the three joints actually holding the lock back. Without names the number is a
            // verdict; with names it is a diagnosis the wearer can act on in the same second.
            sb.Append("worst  ").Append(matcher.DescribeWorstJoints()).Append('\n');
            if (matcher.weights != null)
                sb.Append("weights ").Append(matcher.weights.Summary()).Append('\n');

            if (poseSource != null)
            {
                sb.Append("joints ").Append(poseSource.JointsFullyMapped ? "OK" : "PARTIAL").Append('\n');
            }
            sb.Append("lockedDeg ").Append(matcher.lockedDeg.ToString("0.0"))
              .Append("  closeDeg ").Append(matcher.closeDeg.ToString("0.0"))
              .Append("  release ").Append(matcher.bandReleaseDeg.ToString("0.0"))
              .Append("  nearMiss ").Append(matcher.nearMissMaxDeg.ToString("0.0"));

            _text.text = sb.ToString();
        }

        /// <summary>
        /// A one-word reading of D against the flat mean.
        ///
        /// When the flat mean is far below D, exactly one finger is wrong: that is a shape problem the
        /// user can fix, and it is the case a plain average would have hidden. When the two are close,
        /// the whole hand is uniformly off, which usually means aim, not articulation.
        /// </summary>
        string DistanceGapNote()
        {
            float d = matcher.DistanceDegrees;
            float flat = matcher.FlatDegrees;
            if (float.IsInfinity(d) || float.IsInfinity(flat)) return "-";
            float gap = d - flat;
            if (gap > 8f) return "one finger";
            if (gap > 3f) return "one finger, mild";
            return "whole hand";
        }

        void SetVisible(bool visible)
        {
            if (canvas != null && canvas.gameObject.activeSelf != visible)
                canvas.gameObject.SetActive(visible);
        }
    }
}
