using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>
    /// Runs P1.3 as a controlled comparison: 3 rotation variants × 2 distances, rated in the headset,
    /// with the lag and drift measured while the user is actually turning the hand.
    ///
    /// WHY A CONTROLLER AND NOT A CHECKLIST
    /// ------------------------------------
    /// P1.3's verdict is the most consequential page in the project: if no variant feels right, the
    /// concept is reopened. A verdict produced by a person trying six combinations from memory in one
    /// sitting is worth very little - the order alone biases the result, and nobody remembers whether
    /// arm's-length was a 3 or a 4 forty minutes later.
    ///
    /// So the six cells are visited in a fixed order, each cell's score is captured at the moment of
    /// judging rather than afterwards, and the lag/drift numbers are recorded automatically rather
    /// than estimated.
    ///
    /// THE SIX CELLS
    ///   A · Direct    @ 0.40 m / 0.60 m
    ///   B · Damped    @ 0.40 m / 0.60 m
    ///   C · HoldStill @ 0.40 m / 0.60 m
    ///
    /// HOW TO JUDGE WITHOUT A MENU
    /// ---------------------------
    /// The LEFT hand does everything, so the right hand can stay on the work:
    ///   • pinch once per point you want to give - 1 to 5 pinches = a score of 1..5
    ///   • stop pinching for 2 seconds and the score is recorded and the next cell loads
    ///   • hold a pinch for over 1.2 s to throw the pending score away (a mis-pinch is not a verdict)
    ///
    /// In the editor or the simulator the same operations are on keys 1/2/3 (variant), 4 (distance)
    /// and Return (record), so the rig can be exercised without a hand.
    /// </summary>
    public class RotationTrialController : MonoBehaviour
    {
        [Header("What it drives")]
        public ReferenceHand referenceHand;

        [Tooltip("The hand used for RATING. The other hand keeps hold of the object.")]
        public MetaHandSource ratingHand;

        [Header("The two distances P1.3 specifies")]
        public float armLengthDistance = 0.40f;
        public float lapDistance = 0.60f;

        [Header("Rating gesture")]
        [Tooltip("A pinch shorter than this counts as one point. Longer throws the score away.")]
        public float pointMaxSeconds = 1.2f;

        [Tooltip("Quiet time after the last pinch before the score is recorded and the next cell loads.")]
        public float settleSeconds = 2.0f;

        [Header("Output")]
        public string resultsFileName = "rotation-trials.jsonl";
        public string summaryFileName = "rotation-summary.txt";
        public bool showReadout = true;

        [Serializable]
        public struct CellResult
        {
            public string variant;
            public float distance;
            public int score;                 // 0 = not yet rated
            public float lagMean;
            public float lagP95;
            public float lagMax;
            public float driftMax;
            public float sampledSeconds;
        }

        static readonly string[] VariantNames = { "A · Direct", "B · Damped", "C · HoldStill" };
        static readonly ReferenceHand.RotationMode[] VariantModes =
        {
            ReferenceHand.RotationMode.Direct,
            ReferenceHand.RotationMode.Damped,
            ReferenceHand.RotationMode.HoldStill
        };

        public CellResult[] Results { get; } = new CellResult[6];

        /// <summary>Index of the cell being tested, 0..5. Order is variant-major so each variant is
        /// judged at both distances back to back.</summary>
        public int CellIndex { get; private set; }

        public int PendingScore { get; private set; }
        public bool Complete => CellIndex >= Results.Length;

        // lag sampling
        readonly List<float> _lagSamples = new List<float>(2048);
        float _driftMax;
        float _sampledSeconds;

        bool _pinchWasClosed;
        float _pinchClosedAt;
        float _lastPinchAt = -999f;
        string _note = "ready";

        void Start()
        {
            for (int i = 0; i < Results.Length; i++) Results[i] = new CellResult { score = 0 };
            if (referenceHand == null) referenceHand = FindFirstObjectByType<ReferenceHand>();
            LoadCell();
        }

        void Update()
        {
            HandleKeyboard();
            HandleRatingGesture();
            SampleWhileTurning();
        }

        // ---- the cell under test -------------------------------------------------------

        void LoadCell()
        {
            if (Complete || referenceHand == null) return;

            int variant = CellIndex / 2;
            int distanceIndex = CellIndex % 2;

            referenceHand.SetMode(VariantModes[variant]);
            referenceHand.SetDistance(distanceIndex == 0 ? armLengthDistance : lapDistance);

            _lagSamples.Clear();
            _driftMax = 0f;
            _sampledSeconds = 0f;
            PendingScore = 0;

            Results[CellIndex] = new CellResult
            {
                variant = VariantNames[variant],
                distance = referenceHand.distance,
                score = 0
            };

            _note = "cell " + (CellIndex + 1) + "/6 loaded";
        }

        void RecordAndAdvance(int score)
        {
            if (Complete) return;

            var cell = Results[CellIndex];
            cell.score = score;
            cell.lagMean = Mean(_lagSamples);
            cell.lagP95 = Percentile(_lagSamples, 0.95f);
            cell.lagMax = Max(_lagSamples);
            cell.driftMax = _driftMax;
            cell.sampledSeconds = _sampledSeconds;
            Results[CellIndex] = cell;

            AppendResult(cell, CellIndex);
            _note = cell.variant + " @ " + cell.distance.ToString("0.00") + " m scored " + score;

            CellIndex++;
            if (Complete)
            {
                WriteSummary();
                _note = "all six cells rated - summary written";
            }
            else
            {
                LoadCell();
            }
        }

        // ---- gesture ------------------------------------------------------------------

        void HandleRatingGesture()
        {
            if (ratingHand == null || Complete) return;

            bool closed = ratingHand.IsTracked && ratingHand.IsPinching;

            if (closed && !_pinchWasClosed)
            {
                _pinchClosedAt = Time.unscaledTime;
            }
            else if (!closed && _pinchWasClosed)
            {
                float held = Time.unscaledTime - _pinchClosedAt;
                if (held > pointMaxSeconds)
                {
                    PendingScore = 0;
                    _note = "score discarded (pinch held " + held.ToString("0.0") + " s)";
                }
                else
                {
                    PendingScore = Mathf.Clamp(PendingScore + 1, 1, 5);
                }
                _lastPinchAt = Time.unscaledTime;
            }

            _pinchWasClosed = closed;

            if (PendingScore > 0 && !closed && Time.unscaledTime - _lastPinchAt >= settleSeconds)
            {
                RecordAndAdvance(PendingScore);
            }
        }

        void HandleKeyboard()
        {
            // Simulator/editor convenience only. On device these keys do not exist, and nothing in the
            // protocol depends on them.
            //
            // Guarded by the legacy-input define because Unity 6 projects can be configured for the
            // new Input System alone, where any UnityEngine.Input call throws at runtime. A trial
            // rig that crashes on startup because a debug key was compiled in would be a spectacular
            // way to lose an afternoon.
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectVariant(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SelectVariant(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SelectVariant(2);
            if (Input.GetKeyDown(KeyCode.Alpha4) && !Complete)
            {
                referenceHand.SetDistance(Mathf.Approximately(referenceHand.distance, armLengthDistance)
                    ? lapDistance : armLengthDistance);
                _note = "distance " + referenceHand.distance.ToString("0.00") + " m (keyboard)";
            }
            if (Input.GetKeyDown(KeyCode.Return) && PendingScore > 0) RecordAndAdvance(PendingScore);
#endif
        }

        void SelectVariant(int variant)
        {
            if (referenceHand == null) return;
            referenceHand.SetMode(VariantModes[variant]);
            _note = VariantNames[variant] + " (keyboard; not recorded as a cell)";
        }

        // ---- measurement ---------------------------------------------------------------

        void SampleWhileTurning()
        {
            if (referenceHand == null || !referenceHand.IsGrabbed) return;
            _lagSamples.Add(referenceHand.LagDegrees);
            _driftMax = Mathf.Max(_driftMax, referenceHand.PositionDrift);
            _sampledSeconds += Time.unscaledDeltaTime;
        }

        static float Mean(List<float> v)
        {
            if (v.Count == 0) return 0f;
            double sum = 0; for (int i = 0; i < v.Count; i++) sum += v[i];
            return (float)(sum / v.Count);
        }

        static float Max(List<float> v)
        {
            float m = 0f; for (int i = 0; i < v.Count; i++) m = Mathf.Max(m, v[i]); return m;
        }

        static float Percentile(List<float> v, float p)
        {
            if (v.Count == 0) return 0f;
            var copy = new List<float>(v);
            copy.Sort();
            int idx = Mathf.Clamp(Mathf.RoundToInt(p * (copy.Count - 1)), 0, copy.Count - 1);
            return copy[idx];
        }

        // ---- output --------------------------------------------------------------------

        void AppendResult(CellResult cell, int index)
        {
            try
            {
                var path = Path.Combine(Application.persistentDataPath, resultsFileName);
                var line = string.Format(CultureInfo.InvariantCulture,
                    "{{\"cell\":{0},\"variant\":\"{1}\",\"distance\":{2:0.00},\"score\":{3}," +
                    "\"lagMean\":{4:0.00},\"lagP95\":{5:0.00},\"lagMax\":{6:0.00},\"driftMax\":{7:0.0000}," +
                    "\"sampledSeconds\":{8:0.0}}}\n",
                    index, cell.variant, cell.distance, cell.score,
                    cell.lagMean, cell.lagP95, cell.lagMax, cell.driftMax, cell.sampledSeconds);
                File.AppendAllText(path, line);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[RotationTrial] append failed: " + e.Message);
            }
        }

        public string Summary
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append("P1.3 rotation trials · ").Append(CellIndex).Append("/6 cells rated\n");
                for (int i = 0; i < Results.Length; i++)
                {
                    var c = Results[i];
                    if (c.score <= 0) continue;
                    sb.Append("  ").Append(c.variant).Append(" @ ").Append(c.distance.ToString("0.00"))
                      .Append(" m  score ").Append(c.score)
                      .Append("  lag mean ").Append(c.lagMean.ToString("0.0"))
                      .Append("° p95 ").Append(c.lagP95.ToString("0.0"))
                      .Append("°  drift ").Append(c.driftMax.ToString("0.0000")).Append(" m\n");
                }
                return sb.ToString();
            }
        }

        public void WriteSummary()
        {
            try
            {
                var path = Path.Combine(Application.persistentDataPath, summaryFileName);
                File.WriteAllText(path,
                    "SIGNSPACE P1.3 rotation trial summary\n" +
                    "device: " + SystemInfo.deviceModel + " · " + SystemInfo.operatingSystem + "\n" +
                    "unity: " + Application.unityVersion + "\n\n" + Summary + "\n" +
                    "Score is the user's 1-5 judgement, captured in the headset at the moment of judging.\n" +
                    "lag = angle between the object and the wrist-derived target while turning.\n" +
                    "drift = how far the object moved from where it was grabbed.\n");
                _note = "summary written to " + path;
            }
            catch (Exception e)
            {
                _note = "summary write failed: " + e.Message;
            }
        }

        void OnApplicationQuit() { if (CellIndex > 0) WriteSummary(); }
        void OnApplicationPause(bool paused) { if (paused && CellIndex > 0) WriteSummary(); }

        void OnGUI()
        {
            if (!showReadout) return;
            if (!Debug.isDebugBuild && !Application.isEditor) return;

            var text = Complete
                ? Summary + "\n" + _note
                : (CellIndex < Results.Length
                    ? "ROTATION TEST  cell " + (CellIndex + 1) + "/6\n" + Results[CellIndex].variant +
                      " @ " + Results[CellIndex].distance.ToString("0.00") + " m\n" +
                      "score: " + PendingScore + " / 5   (pinch 1-5 times, pause 2 s to record)\n" +
                      "lag " + referenceHand.LagDegrees.ToString("0.0") + "°  drift " +
                      referenceHand.PositionDrift.ToString("0.000") + " m\n" + _note
                    : Summary);

            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            var rect = new Rect(12f, 96f, 520f, 92f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, rect.height - 8f), text);
        }
    }
}
