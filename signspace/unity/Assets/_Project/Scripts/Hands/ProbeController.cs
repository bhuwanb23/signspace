using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>
    /// P1.5: walks the twenty candidate shapes, times a ten-second hold of each, and measures what the
    /// sensor does with it - rather than asking the tester to remember.
    ///
    /// WHY AUTOMATIC AND MANUAL TOGETHER
    /// --------------------------------
    /// TRACKABILITY.md's procedure is manual: hold a shape, judge it High/Medium/Low/Never. A
    /// judgement like that is the right thing to *ask a person*, because "does this feel like it is
    /// tracking" is what a learner will experience. But it is a bad thing to *record alone*, because
    /// P1's own trap list says an unwritten verdict becomes a rumour, and a memory of forty judgements
    /// made in a warm headset is not evidence.
    ///
    /// So each 10-second hold produces both:
    ///   • measured: fraction of frames tracked, fraction of frames locked, dropout count, and the
    ///     fraction of frames where a DIFFERENT shape was the nearest match (the "tracks as the wrong
    ///     shape" failure, which is the only truly dangerous one)
    ///   • judged: the operator's High / Medium / Low / Never, captured immediately with 1-4 pinches
    /// The two are recorded side by side. Where they disagree, the disagreement is the finding: a
    /// shape that measures well but feels bad is a UX problem; a shape that measures badly but feels
    /// fine is a threshold problem.
    ///
    /// FOUR SESSIONS, NOT ONE
    /// ---------------------
    /// condition (daylight / lamplight) and hand (dominant / other) are inspector fields, because the
    /// procedure asks the same twenty shapes four times: two lighting conditions x two hands. Each
    /// session writes its own file. Relaunching between sessions is deliberate - it also clears the
    /// tracker's own adaptation, which is what a judge's first minute would look like.
    /// </summary>
    public class ProbeController : MonoBehaviour
    {
        public enum Condition { Daylight = 0, Lamplight = 1 }
        public enum WhichHand { Dominant = 0, Other = 1 }

        [Header("What it reads")]
        public Matcher matcher;
        public SignSetLoader loader;

        [Tooltip("Shows the target shape. The tester copies this hand.")]
        public ReferenceHand referenceHand;

        [Tooltip("The tracked hand making the shape. Used only for 'is the sensor seeing a hand at all'.")]
        public MetaHandSource trackedHand;

        [Header("The session")]
        [Tooltip("Set before each run. Part of the output file name.")]
        public Condition condition = Condition.Daylight;
        public WhichHand hand = WhichHand.Dominant;

        [Tooltip("Seconds per shape. TRACKABILITY.md specifies 10.")]
        public float holdSeconds = 10f;

        [Tooltip("Quiet time after the last rating pinch before recording and advancing.")]
        public float settleSeconds = 1.5f;

        [Tooltip("Shape order, unless probe-meta.json overrides it.")]
        public string[] shapeOrder =
        {
            "B", "U", "V", "W", "C", "O",
            "A", "M", "N", "T", "R", "S",
            "D", "E", "F", "G", "I", "L", "X", "Y"
        };

        [Header("Output")]
        public bool writeFiles = true;
        public bool showReadout = true;

        [Serializable]
        public struct ProbeCell
        {
            public string shape;
            public string authoringConfidence;
            public int manualScore;          // 0 not rated; 1 High, 2 Medium, 3 Low, 4 Never
            public float trackedFrames;      // fraction of the hold with a tracked hand
            public float lockedFrames;       // fraction of the hold inside the lock band
            public float wrongShapeFrames;   // fraction where another shape was nearest
            public string confusedWith;      // the shape it was most often nearest to, if any
            public int dropouts;             // tracked -> lost transitions
            public float meanDistance;       // degrees to the target
            public float holdSeconds;
        }

        public List<ProbeCell> Cells { get; } = new List<ProbeCell>();
        public int CellIndex { get; private set; }
        public bool Complete => CellIndex >= shapeOrder.Length;

        // per-cell accumulation
        float _frames, _tracked, _locked, _wrong, _distanceSum;
        int _dropouts;
        float _lastDistance;
        bool _wasTracked;
        float _holdStartedAt = -1f;
        readonly Dictionary<string, int> _confusionCounts = new Dictionary<string, int>(8);

        int _manualPicks;
        bool _pinchWasClosed;
        float _lastPinchAt = -999f;
        string _note = "starting";

        GUIStyle _style;

        void Start()
        {
            _style = new GUIStyle { fontSize = 14, normal = { textColor = new Color(1f, 1f, 0.85f) } };
            if (matcher == null) matcher = FindFirstObjectByType<Matcher>();
            if (matcher != null) matcher.OnDistanceUpdated += OnDistance;
            if (loader == null) loader = FindFirstObjectByType<SignSetLoader>();
            if (referenceHand == null) referenceHand = FindFirstObjectByType<ReferenceHand>();
            if (trackedHand == null) trackedHand = FindFirstObjectByType<MetaHandSource>();

            if (loader != null)
            {
                loader.folder = "Probe";      // the twenty, never the shipped six
                loader.autoSelectFirst = false;
            }

            // Wait for the loader, so the first cell is not measured against an empty shape list.
            StartCoroutine(BeginWhenLoaded());
        }

        System.Collections.IEnumerator BeginWhenLoaded()
        {
            float waited = 0f;
            while (loader != null && !loader.IsLoaded && waited < 15f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            LoadCell();
        }

        void LoadCell()
        {
            if (Complete) return;
            ResetAccumulators();

            string id = shapeOrder[CellIndex];
            if (matcher != null) matcher.SetTarget(id);

            var pose = FindPose(id);
            if (pose != null && referenceHand != null) referenceHand.ShowPose(pose.LocalRotations);

            _holdStartedAt = Time.unscaledTime;
            _note = "hold '" + id + "' for " + holdSeconds.ToString("0") + " s";
        }

        HandPose FindPose(string id)
        {
            if (matcher == null || matcher.shapes == null) return null;
            foreach (var p in matcher.shapes)
            {
                if (p != null && string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase)) return p;
            }
            return null;
        }

        void Update()
        {
            if (Complete) return;

            if (Time.unscaledTime - _holdStartedAt < holdSeconds)
            {
                Sample();
                return;
            }

            if (_note.StartsWith("hold")) _note = "RATE '" + shapeOrder[CellIndex] + "': pinch 1=High 2=Med 3=Low 4=Never";
            HandleRating();
        }

        void Sample()
        {
            _frames += 1f;

            bool tracked = trackedHand == null || trackedHand.IsTracked;
            if (tracked) _tracked += 1f;
            if (_wasTracked && !tracked) _dropouts++;
            _wasTracked = tracked;

            if (matcher == null) return;

            if (matcher.IsLocked) _locked += 1f;
            _distanceSum += _lastDistance;

            // "tracks as a different shape": another shape is nearer than the target, or the target
            // is not even in the close band while something else is.
            if (!string.IsNullOrEmpty(matcher.BestOtherId) && matcher.BestOtherDegrees < matcher.closeDeg)
            {
                _wrong += 1f;
                if (_confusionCounts.TryGetValue(matcher.BestOtherId, out int c))
                    _confusionCounts[matcher.BestOtherId] = c + 1;
                else
                    _confusionCounts[matcher.BestOtherId] = 1;
            }
        }

        void HandleRating()
        {
            if (trackedHand == null) return;

            bool closed = trackedHand.IsTracked && trackedHand.IsPinching;
            if (closed && !_pinchWasClosed)
            {
                // rating pinches are counted on release, so a long accidental pinch does not count
            }
            else if (!closed && _pinchWasClosed)
            {
                _manualPicks = Mathf.Clamp(_manualPicks + 1, 1, 4);
                _lastPinchAt = Time.unscaledTime;
            }
            _pinchWasClosed = closed;

            if (_manualPicks > 0 && !closed && Time.unscaledTime - _lastPinchAt >= settleSeconds)
            {
                Record(_manualPicks);
            }
        }

        void Record(int manualScore)
        {
            var cell = new ProbeCell
            {
                shape = shapeOrder[CellIndex],
                authoringConfidence = "",
                manualScore = manualScore,
                trackedFrames = _frames > 0f ? _tracked / _frames : 0f,
                lockedFrames = _frames > 0f ? _locked / _frames : 0f,
                wrongShapeFrames = _frames > 0f ? _wrong / _frames : 0f,
                confusedWith = TopConfusion(),
                dropouts = _dropouts,
                meanDistance = _frames > 0f ? _distanceSum / _frames : 0f,
                holdSeconds = holdSeconds
            };
            Cells.Add(cell);
            AppendLine(cell);

            _note = cell.shape + ": " + ScoreName(manualScore) +
                    ", locked " + (cell.lockedFrames * 100f).ToString("0") + "%" +
                    (string.IsNullOrEmpty(cell.confusedWith) ? "" : ", confused with " + cell.confusedWith);

            CellIndex++;
            if (Complete)
            {
                WriteSummary();
                _note = "all " + Cells.Count + " shapes probed - summary written";
            }
            else
            {
                LoadCell();
            }
        }

        void ResetAccumulators()
        {
            _frames = _tracked = _locked = _wrong = _distanceSum = 0f;
            _dropouts = 0;
            _wasTracked = false;
            _manualPicks = 0;
            _lastPinchAt = -999f;
            _confusionCounts.Clear();
            _lastDistance = 0f;
        }

        string TopConfusion()
        {
            string best = null;
            int bestCount = 0;
            foreach (var pair in _confusionCounts)
            {
                if (pair.Value <= bestCount) continue;
                bestCount = pair.Value;
                best = pair.Key;
            }
            return best;
        }

        public static string ScoreName(int score)
        {
            switch (score)
            {
                case 1: return "High";
                case 2: return "Medium";
                case 3: return "Low";
                case 4: return "Never";
                default: return "unrated";
            }
        }

        // ---- output --------------------------------------------------------------------

        string FileStem => "probe-" + condition.ToString().ToLowerInvariant() + "-" +
                           hand.ToString().ToLowerInvariant();

        void AppendLine(ProbeCell cell)
        {
            if (!writeFiles) return;
            try
            {
                var path = Path.Combine(Application.persistentDataPath, FileStem + ".jsonl");
                var line = string.Format(CultureInfo.InvariantCulture,
                    "{{\"shape\":\"{0}\",\"manual\":{1},\"manualName\":\"{2}\",\"trackedFrames\":{3:0.000}," +
                    "\"lockedFrames\":{4:0.000},\"wrongShapeFrames\":{5:0.000},\"confusedWith\":\"{6}\"," +
                    "\"dropouts\":{7},\"holdSeconds\":{8:0}}}\n",
                    cell.shape, cell.manualScore, ScoreName(cell.manualScore), cell.trackedFrames,
                    cell.lockedFrames, cell.wrongShapeFrames, cell.confusedWith ?? "",
                    cell.dropouts, cell.holdSeconds);
                File.AppendAllText(path, line);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Probe] append failed: " + e.Message);
            }
        }

        public string Report
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append(FileStem).Append("  ").Append(Cells.Count).Append('/')
                  .Append(shapeOrder.Length).Append(" probed\n");
                foreach (var c in Cells)
                {
                    sb.Append("  ").Append(c.shape.PadRight(3))
                      .Append(" ").Append(ScoreName(c.manualScore).PadRight(7))
                      .Append(" lock ").Append((c.lockedFrames * 100f).ToString("0").PadLeft(3)).Append("%")
                      .Append("  wrong ").Append((c.wrongShapeFrames * 100f).ToString("0").PadLeft(3)).Append("%")
                      .Append(c.dropouts > 0 ? "  drops " + c.dropouts : "")
                      .Append(string.IsNullOrEmpty(c.confusedWith) ? "" : "  as " + c.confusedWith)
                      .Append('\n');
                }
                return sb.ToString();
            }
        }

        public void WriteSummary()
        {
            if (!writeFiles) return;
            try
            {
                var path = Path.Combine(Application.persistentDataPath, FileStem + "-summary.txt");
                File.WriteAllText(path,
                    "SIGNSPACE P1.5 probe · " + FileStem + "\n" +
                    "device: " + SystemInfo.deviceModel + " · " + SystemInfo.operatingSystem + "\n" +
                    "unity: " + Application.unityVersion + "\n\n" + Report + "\n" +
                    "lock%   = frames inside the lock band of the target shape\n" +
                    "wrong%  = frames where a DIFFERENT shape was the nearest match (the dangerous failure)\n" +
                    "drops   = tracked->lost transitions during the hold\n" +
                    "manual  = the operator's judgement, captured at the moment of judging\n");
                _note = "summary written to " + path;
            }
            catch (Exception e)
            {
                _note = "summary write failed: " + e.Message;
            }
        }

        void OnDistance(float toTarget, float bestOther) { _lastDistance = toTarget; }

        void OnDestroy()
        {
            if (matcher != null) matcher.OnDistanceUpdated -= OnDistance;
        }

        void OnApplicationQuit() { if (Cells.Count > 0) WriteSummary(); }
        void OnApplicationPause(bool paused) { if (paused && Cells.Count > 0) WriteSummary(); }

        void OnGUI()
        {
            if (!showReadout) return;
            if (!Debug.isDebugBuild && !Application.isEditor) return;

            float remain = Mathf.Max(0f, holdSeconds - (Time.unscaledTime - _holdStartedAt));
            var head = Complete
                ? Report
                : "PROBE " + (CellIndex + 1) + "/" + shapeOrder.Length + " · " + condition + " · " + hand + "\n" +
                  "shape '" + shapeOrder[CellIndex] + "'   " +
                  (remain > 0f ? "hold " + remain.ToString("0.0") + " s" : "rate: 1=High 2=Med 3=Low 4=Never") + "\n" +
                  "tracked " + (_frames > 0 ? (_tracked / _frames * 100f).ToString("0") : "--") + "%" +
                  "  lock " + (_frames > 0 ? (_locked / _frames * 100f).ToString("0") : "--") + "%" +
                  "  wrong " + (_frames > 0 ? (_wrong / _frames * 100f).ToString("0") : "--") + "%" +
                  "  drops " + _dropouts +
                  (string.IsNullOrEmpty(matcher != null ? matcher.BestOtherId : null)
                      ? "" : "  nearest other: " + matcher.BestOtherId) + "\n" + _note;

            GUI.color = new Color(0f, 0f, 0f, 0.62f);
            GUI.DrawTexture(new Rect(12f, 320f, 560f, 96f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(18f, 324f, 548f, 88f), head, _style);
        }
    }
}
