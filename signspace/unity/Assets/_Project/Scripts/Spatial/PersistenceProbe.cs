using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using Signspace.Matching;

namespace Signspace.Spatial
{
    /// <summary>
    /// P1.7 probe: does the shelf come back?
    ///
    /// This rig does not place anything itself. It watches `ShelfAnchor` do its job, records what
    /// happened on THIS launch, and appends one row per launch to a file - because the question P1.7
    /// asks ("cube restores after a kill/relaunch, and after a reboot") is a question about a
    /// SEQUENCE. A single session cannot answer it, so the log is the artefact and the readings are
    /// rows 1, 2 and 3.
    ///
    /// THE THREE ROWS THAT CLOSE THE TASK
    ///   launch 1  outcome PlacedFresh    - nothing saved yet, so the shelf lands and writes the anchor
    ///   launch 2  outcome Restored       - killed from recents, relaunched: same physical square
    ///   launch 3  outcome Restored       - after a headset reboot: same physical square
    ///
    /// If row 2 or 3 says `AnchorNotFound`, that is not a crash and not a silent plinth: the outcome,
    /// the reason, and the fact that the save was kept are all in the row.
    ///
    /// WHY THE OPERATOR STILL RATES IT
    /// The automatic numbers prove the *code* resolved the right anchor. Only a person can say the
    /// cube is on the same square of real table, because "restored 3 cm to the left" is a pass in
    /// data and a failure in the product. Rating is 1-3, pinched in the window after the shelf
    /// appears: 3 = same spot, 2 = close but visibly off, 1 = wrong place or absent.
    /// </summary>
    public class PersistenceProbe : MonoBehaviour
    {
        [Header("Scene")]
        public ShelfAnchor shelf;
        public SaveSystem save;
        public MrukBoot boot;
        [Tooltip("Optional. Used for the operator rating only.")]
        public MetaHandSource hand;

        [Header("Behaviour")]
        [Tooltip("Clears the save at startup so the next launch is a clean 'first run'. Flip this on, " +
                 "launch once, flip it off, and the sequence starts from a known state.")]
        public bool clearSaveOnStart = false;

        [Tooltip("Seconds allowed to pinch the operator rating after the shelf settles.")]
        public float ratingWindow = 15f;

        [Header("Output")]
        public bool writeFiles = true;
        public string jsonlFileName = "persistence-probe.jsonl";

        public int LaunchIndex { get; private set; }
        public int OperatorRating { get; private set; }
        public float SecondsSinceSave { get; private set; } = -1f;
        public string RatingNote { get; private set; } = "";

        int _rowsAtStart = -1;   // read once: OnGUI must never touch the file system per frame
        float _startAt;
        float _settledAt = -1f;
        int _pinches;
        bool _pinchWas, _logged;

        void Awake()
        {
            _startAt = Time.realtimeSinceStartup;
            _rowsAtStart = CountExistingRows();
            if (clearSaveOnStart && save != null)
            {
                save.ClearPlacement();
                Debug.Log("[PersistenceProbe] save cleared: this launch is a clean first run");
            }
        }

        void Update()
        {
            if (_logged) return;

            // wait for ShelfAnchor to reach a conclusion
            if (shelf == null) return;
            if (shelf.Result == ShelfAnchor.Outcome.NotStarted) return;
            if (shelf.RestoreMilliseconds < 0f) return;

            if (_settledAt < 0f)
            {
                _settledAt = Time.realtimeSinceStartup;
                SecondsSinceSave = SecondsSince(SaveSystemStamp());
                Debug.Log("[PersistenceProbe] launch " + LaunchIndex + " outcome " + shelf.Result +
                          " · resolve " + shelf.ResolveMilliseconds.ToString("0.0") + " ms" +
                          " · total " + shelf.RestoreMilliseconds.ToString("0.0") + " ms");
            }

            // ---- operator rating ---------------------------------------------------------
            bool pinching = hand != null && hand.IsTracked && hand.IsPinching;
            if (pinching && !_pinchWas && OperatorRating < 3) OperatorRating++;
            _pinchWas = pinching;

            if (Time.realtimeSinceStartup - _settledAt >= ratingWindow)
            {
                RatingNote = OperatorRating == 0 ? "not rated" :
                             OperatorRating == 3 ? "same physical spot" :
                             OperatorRating == 2 ? "close but visibly off" :
                                                   "wrong place or absent";
                Log();
                _logged = true;
            }
        }

        string SaveSystemStamp()
        {
            return save != null ? save.Data.savedUtc : "";
        }

        float SecondsSince(string isoUtc)
        {
            if (string.IsNullOrEmpty(isoUtc)) return -1f;
            DateTime when;
            if (!DateTime.TryParse(isoUtc, CultureInfo.InvariantCulture,
                                   DateTimeStyles.RoundtripKind, out when)) return -1f;
            return (float)(DateTime.UtcNow - when.ToUniversalTime()).TotalSeconds;
        }

        void Log()
        {
            LaunchIndex = _rowsAtStart + 1;

            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"utc\":\"").Append(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)).Append("\",");
            sb.Append("\"launchIndex\":").Append(LaunchIndex).Append(",");
            sb.Append("\"outcome\":\"").Append(shelf != null ? shelf.Result.ToString() : "none").Append("\",");
            sb.Append("\"detail\":\"").Append(Esc(shelf != null ? shelf.OutcomeDetail : "")).Append("\",");
            sb.Append("\"savedAnchorId\":\"").Append(Esc(shelf != null ? shelf.SavedAnchorId : "")).Append("\",");
            sb.Append("\"resolvedAnchorId\":\"").Append(Esc(shelf != null ? shelf.ResolvedAnchorId : "")).Append("\",");
            sb.Append("\"saveSchema\":").Append(save != null ? save.Data.schema : 0).Append(",");
            sb.Append("\"restoreCount\":").Append(save != null ? save.Data.restoreCount : 0).Append(",");
            sb.Append("\"resolveMs\":").Append(F(shelf != null ? shelf.ResolveMilliseconds : -1f)).Append(",");
            sb.Append("\"restoreMs\":").Append(F(shelf != null ? shelf.RestoreMilliseconds : -1f)).Append(",");
            sb.Append("\"worldDriftM\":").Append(F(shelf != null ? shelf.WorldDriftMeters : -1f)).Append(",");
            sb.Append("\"secondsSinceSave\":").Append(F(SecondsSinceSave)).Append(",");
            sb.Append("\"onSurface\":").Append(shelf != null && shelf.OnSurface ? "true" : "false").Append(",");
            sb.Append("\"scenePermission\":").Append(boot != null && boot.ScenePermissionGranted ? "true" : "false").Append(",");
            sb.Append("\"secondsToRoom\":").Append(F(boot != null ? boot.DetectionSeconds : -1f)).Append(",");
            sb.Append("\"operatorRating\":").Append(OperatorRating).Append(",");
            sb.Append("\"ratingNote\":\"").Append(Esc(RatingNote)).Append("\",");
            sb.Append("\"sessionSeconds\":").Append(F(Time.realtimeSinceStartup - _startAt));
            sb.Append("}");

            var row = sb.ToString();

            if (!writeFiles) { Debug.Log("[PersistenceProbe] " + row); return; }

            try
            {
                var path = Path.Combine(Application.persistentDataPath, jsonlFileName);
                File.AppendAllText(path, row + "\n");

                var summary = Path.Combine(Application.persistentDataPath, "persistence-summary.txt");
                File.WriteAllText(summary, BuildSummary());
                Debug.Log("[PersistenceProbe] appended launch " + LaunchIndex + " to " + path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[PersistenceProbe] could not write: " + e.Message);
            }
        }

        int CountExistingRows()
        {
            try
            {
                var path = Path.Combine(Application.persistentDataPath, jsonlFileName);
                if (!File.Exists(path)) return 0;
                int n = 0;
                foreach (var line in File.ReadAllLines(path))
                    if (!string.IsNullOrWhiteSpace(line)) n++;
                return n;
            }
            catch { return 0; }
        }

        string BuildSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("SIGNSPACE P1.7 - persistence probe");
            sb.AppendLine("when        : " + DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            sb.AppendLine("launch      : #" + LaunchIndex);
            sb.AppendLine();
            sb.AppendLine("outcome     : " + (shelf != null ? shelf.Result.ToString() : "no shelf"));
            sb.AppendLine("              " + (shelf != null ? shelf.OutcomeDetail : ""));
            sb.AppendLine("saved id    : " + (shelf != null ? shelf.SavedAnchorId : ""));
            sb.AppendLine("resolved id : " + (shelf != null ? shelf.ResolvedAnchorId : ""));
            sb.AppendLine("resolve     : " + (shelf != null ? F(shelf.ResolveMilliseconds) + " ms" : "-"));
            sb.AppendLine("restore tot : " + (shelf != null ? F(shelf.RestoreMilliseconds) + " ms" : "-"));
            sb.AppendLine("world drift : " + (shelf != null ? F(shelf.WorldDriftMeters) + " m  (the origin shift, " +
                          "i.e. the error a world-position save would have shown)" : "-"));
            sb.AppendLine("since save  : " + F(SecondsSinceSave) + " s");
            sb.AppendLine("restores    : " + (save != null ? save.Data.restoreCount : 0) + " this anchor");
            sb.AppendLine("save file   : " + (save != null ? save.Path : "-"));
            sb.AppendLine();
            sb.AppendLine("rating      : " + OperatorRating + " (" + (string.IsNullOrEmpty(RatingNote) ? "not rated" : RatingNote) + ")");
            sb.AppendLine("              3 = same physical spot, 2 = close but visibly off, 1 = wrong place");
            return sb.ToString();
        }

        static string F(float v) { return v.ToString("0.###", CultureInfo.InvariantCulture); }

        static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ");
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            style.normal.textColor = Color.white;

            string text = "P1.7 PERSISTENCE\n";
            text += "launch   : #" + (LaunchIndex > 0 ? LaunchIndex : _rowsAtStart + 1) + "\n";
            text += "outcome  : " + (shelf != null ? shelf.Result.ToString() : "-") + "\n";
            if (shelf != null && !string.IsNullOrEmpty(shelf.OutcomeDetail))
                text += "           " + shelf.OutcomeDetail + "\n";
            text += "saved    : " + (shelf != null ? Short(shelf.SavedAnchorId) : "-") + "\n";
            text += "resolved : " + (shelf != null ? Short(shelf.ResolvedAnchorId) : "-") + "\n";
            text += "resolve  : " + (shelf != null && shelf.ResolveMilliseconds >= 0
                ? shelf.ResolveMilliseconds.ToString("0.0") + " ms" : "-") + "\n";
            text += "restore  : " + (shelf != null && shelf.RestoreMilliseconds >= 0
                ? shelf.RestoreMilliseconds.ToString("0.0") + " ms" : "-") + "\n";
            text += "drift    : " + (shelf != null && shelf.WorldDriftMeters >= 0
                ? shelf.WorldDriftMeters.ToString("0.000") + " m" : "-") + "\n";
            if (_settledAt >= 0f && !_logged)
                text += "RATE     : pinch 3 = same spot · 2 = close · 1 = wrong   (" +
                        Mathf.Max(0f, ratingWindow - (Time.realtimeSinceStartup - _settledAt)).ToString("0") + " s)\n";
            text += "rating   : " + OperatorRating + (string.IsNullOrEmpty(RatingNote) ? "" : "  " + RatingNote);

            GUI.Box(new Rect(12f, 12f, 470f, 300f), GUIContent.none);
            GUI.Label(new Rect(24f, 22f, 450f, 280f), text, style);
        }

        static string Short(string id)
        {
            if (string.IsNullOrEmpty(id)) return "(none)";
            return id.Length <= 12 ? id : id.Substring(0, 8) + "…";
        }
    }
}
