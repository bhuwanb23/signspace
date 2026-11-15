using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Signspace.Matching
{
    /// <summary>
    /// Loads handshape poses out of StreamingAssets and puts them on the reference hand, measuring
    /// what that costs. This is P1.4's deliverable: proof that the product's content is data.
    ///
    /// WHY THIS IS NOT PART OF SignSetLoader
    /// -------------------------------------
    /// SignSetLoader's job is to hand the matcher a list of shapes to compare against. This one's job
    /// is to put ONE pose on ONE visible hand and say how long it took. They read the same files
    /// through the same platform-correct path (SignSetLoader.ToUrl is reused, not reimplemented), but
    /// they answer different questions and P1.4 has to be answerable without loading a whole set.
    ///
    /// WHY THE TIMING IS SPLIT INTO THREE NUMBERS
    /// ------------------------------------------
    /// "It loads fast" is not a measurement. Read (I/O out of the apk), parse (JSON + validation +
    /// name mapping) and apply (26 local rotations onto 26 transforms) fail for completely different
    /// reasons: read is the only one a judge's device could make slow, parse is the one that grows if
    /// the schema gets heavier, and apply is the one that runs every frame a shape is on screen. If a
    /// number is surprising later, the split says where to look.
    ///
    /// The apply measurement is a WARM LOOP of 100 applies, because a single apply on a cold monobehaviour
    /// reads as 0.0 ms and tells you nothing.
    /// </summary>
    public class PoseProvider : MonoBehaviour
    {
        [Header("Where it goes")]
        [Tooltip("Preferred target: the reference hand, so the pose appears on the object the user " +
                 "actually holds. Falls back to a bare HandVisual if this is empty.")]
        public ReferenceHand referenceHand;

        [Tooltip("Used only when referenceHand is empty.")]
        public HandVisual visual;

        [Header("What it loads")]
        [Tooltip("Folder inside StreamingAssets.")]
        public string folder = "Signs";

        [Tooltip("Pose ids to load, in order. P1.4 asks for two; the shipped set has six.")]
        public string[] poseIds = { "B", "U" };

        [Tooltip("Advance to the next pose automatically, for eyeballing animation-frame to frame.")]
        public bool autoCycle = false;
        public float cycleSeconds = 3f;

        [Header("Reporting")]
        public string reportFileName = "pose-load-report.txt";
        public bool showReadout = true;

        public event Action<HandPose> OnPoseApplied;

        public HandPose Current { get; private set; }
        public int Index { get; private set; }

        public float LastReadMs { get; private set; }
        public float LastParseMs { get; private set; }
        public float LastApplyMs { get; private set; }
        /// <summary>Mean cost of one apply over 100 warm iterations, in microseconds.</summary>
        public float WarmApplyMicros { get; private set; }

        public string LastError { get; private set; }
        public bool IsLoaded { get; private set; }

        readonly List<HandPose> _poses = new List<HandPose>();
        float _nextCycle;
        GUIStyle _style;

        void Start()
        {
            if (referenceHand == null) referenceHand = FindFirstObjectByType<ReferenceHand>();
            if (visual == null && referenceHand != null) visual = referenceHand.visual;
            _style = new GUIStyle { fontSize = 13, normal = { textColor = new Color(0.8f, 1f, 0.85f) } };
            StartCoroutine(LoadAll());
        }

        void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.RightArrow)) NextPose();
#endif
            if (!autoCycle || _poses.Count == 0) return;
            if (Time.unscaledTime < _nextCycle) return;
            _nextCycle = Time.unscaledTime + Mathf.Max(0.25f, cycleSeconds);
            NextPose();
        }

        IEnumerator LoadAll()
        {
            _poses.Clear();
            foreach (string id in poseIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                yield return LoadOne(id);
            }

            IsLoaded = true;
            if (_poses.Count > 0)
            {
                Index = 0;
                Apply(_poses[0]);
                WriteReport();
                UnityEngine.Debug.Log("[PoseProvider] " + Report);
            }
            else
            {
                UnityEngine.Debug.LogError("[PoseProvider] no pose loaded. " + (LastError ?? ""));
            }
        }

        IEnumerator LoadOne(string id)
        {
            string relative = string.IsNullOrEmpty(folder) ? id + ".json" : folder + "/" + id + ".json";
            string url = SignSetLoader.ToUrl(relative);

            var readWatch = Stopwatch.StartNew();
            string json = null;
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success) json = request.downloadHandler.text;
                else LastError = id + ": read failed (" + request.error + ") at " + url;
            }
            readWatch.Stop();
            if (string.IsNullOrEmpty(json))
            {
                UnityEngine.Debug.LogError("[PoseProvider] " + LastError);
                yield break;
            }

            var parseWatch = Stopwatch.StartNew();
            var pose = HandPose.FromJson(json, out string error);
            parseWatch.Stop();
            if (pose == null)
            {
                LastError = id + ": " + error;
                UnityEngine.Debug.LogError("[PoseProvider] " + LastError);
                yield break;
            }

            _poses.Add(pose);
            LastReadMs = (float)readWatch.Elapsed.TotalMilliseconds;
            LastParseMs = (float)parseWatch.Elapsed.TotalMilliseconds;

            UnityEngine.Debug.Log("[PoseProvider] loaded '" + id + "' in " +
                                  LastReadMs.ToString("0.00") + " ms read + " +
                                  LastParseMs.ToString("0.00") + " ms parse, " +
                                  pose.FileJointCount + " joints in the file, " +
                                  pose.JointCount + " applied");
        }

        /// <summary>Applies a pose and measures it, including a warm loop so the number means something.</summary>
        void Apply(HandPose pose)
        {
            if (pose == null) return;

            var watch = Stopwatch.StartNew();
            ApplyOnce(pose);
            watch.Stop();
            LastApplyMs = (float)watch.Elapsed.TotalMilliseconds;

            const int warmIterations = 100;
            var warm = Stopwatch.StartNew();
            for (int i = 0; i < warmIterations; i++) ApplyOnce(pose);
            warm.Stop();
            WarmApplyMicros = (float)(warm.Elapsed.TotalMilliseconds * 1000.0 / warmIterations);

            Current = pose;
            if (OnPoseApplied != null) OnPoseApplied(pose);
        }

        void ApplyOnce(HandPose pose)
        {
            // The reference hand first: it owns the pivot, the distance and the grab state, and
            // applying straight to the visual while a P1.3 grab is live would fight the rotation.
            if (referenceHand != null) referenceHand.ShowPose(pose.LocalRotations);
            else if (visual != null) visual.ApplyPose(pose.LocalRotations);
        }

        public void NextPose()
        {
            if (_poses.Count == 0) return;
            Index = (Index + 1) % _poses.Count;
            Apply(_poses[Index]);
        }

        public void Show(string id)
        {
            int found = _poses.FindIndex(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
            if (found < 0) return;
            Index = found;
            Apply(_poses[Index]);
        }

        public string Report
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append("P1.4 pose load report\n");
                sb.Append("  poses in memory: ").Append(_poses.Count).Append('\n');
                if (Current != null)
                {
                    sb.Append("  current: ").Append(Current.Id)
                      .Append(" (").Append(Current.Family).Append(", ").Append(Current.Source).Append(")\n")
                      .Append("  joints: ").Append(Current.FileJointCount).Append(" in file, ")
                      .Append(Current.JointCount).Append(" applied\n");
                }
                sb.Append("  last read:  ").Append(LastReadMs.ToString("0.00")).Append(" ms\n");
                sb.Append("  last parse: ").Append(LastParseMs.ToString("0.00")).Append(" ms\n");
                sb.Append("  last apply: ").Append(LastApplyMs.ToString("0.000")).Append(" ms\n");
                sb.Append("  warm apply: ").Append(WarmApplyMicros.ToString("0.0")).Append(" us per apply (100 iterations)");
                if (!string.IsNullOrEmpty(LastError)) sb.Append("\n  last error: ").Append(LastError);
                return sb.ToString();
            }
        }

        public void WriteReport()
        {
            try
            {
                var path = Path.Combine(Application.persistentDataPath, reportFileName);
                File.WriteAllText(path,
                    Report + "\n\ndevice: " + SystemInfo.deviceModel + " · " + SystemInfo.operatingSystem +
                    "\nunity: " + Application.unityVersion +
                    "\n\nread  = I/O out of the apk (the only number a device can make slow)\n" +
                    "parse = JSON + validation + name-to-canonical mapping\n" +
                    "apply = 26 local rotations onto 26 transforms\n");
                UnityEngine.Debug.Log("[PoseProvider] report written to " + path);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[PoseProvider] report write failed: " + e.Message);
            }
        }

        void OnGUI()
        {
            if (!showReadout) return;
            if (!UnityEngine.Debug.isDebugBuild && !Application.isEditor) return;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            var rect = new Rect(12f, 200f, 470f, 96f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, rect.height - 8f), Report, _style);
        }
    }
}
