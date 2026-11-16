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
    /// P1.6 probe: find a horizontal surface and put a cube on it, four ways.
    ///
    /// WHY THIS IS A SEPARATE SCENE FROM THE PRODUCT
    /// The probe answers one question - "can MRUK find a surface here, and how long does it take" -
    /// and it must answer it in four conditions that the product would never tolerate being wrong in:
    /// clean desk, lap, bed, cluttered table. Mixing the probe into the app would make every failure
    /// ambiguous between SDK behaviour and product logic.
    ///
    /// THE MEASUREMENT THAT MATTERS
    /// Time-to-detection is measured from LAUNCH, not from when the probe started asking. MRUK is not
    /// instant (P1.6 trap a), and the number the launch flow needs is "how long after the user opens
    /// the app does the shelf know where it is". Two numbers are recorded because they answer
    /// different questions:
    ///   secondsToRoom   a room with anchors exists
    ///   secondsToCube   a cube is actually sitting on furniture and visible
    /// The second is always larger, and it is the one a loading state has to cover.
    ///
    /// ONE LAUNCH PER CONDITION, DELIBERATELY
    /// The condition is chosen at startup by pinching 1-4 times in the first few seconds. It cannot
    /// be changed afterwards, because changing it mid-session would mean the second reading was not
    /// a cold start - exactly the number the phase is required to produce.
    ///
    /// NEVER REQUIRES A SURFACE (P1.6 trap b)
    /// A missing surface is a recorded data point with a reason, not an exception and not a block.
    /// `failReason` in the log is the whole point of the four-condition table: "no horizontal anchor"
    /// and "horizontal anchor 1.4 m away" are different sentences with different consequences.
    /// </summary>
    public class SurfaceProbe : MonoBehaviour
    {
        public enum Condition { CleanDesk = 1, Lap = 2, Bed = 3, ClutteredTable = 4 }

        [Header("Scene")]
        public MrUkSurfaceFinder finder;
        public MrukBoot boot;
        [Tooltip("Optional. Used only to read the condition pinches and the operator rating.")]
        public MetaHandSource hand;

        [Header("Condition selection")]
        [Tooltip("Seconds after startup during which pinch counts select the condition. Must be " +
                 "shorter than a comfortable wait and long enough to pinch deliberately.")]
        public float selectionWindow = 6f;

        [Tooltip("Used when no hand is tracked or nobody pinched. Recorded as conditionSource=default " +
                 "so the reading is never mistaken for a chosen one.")]
        public Condition defaultCondition = Condition.CleanDesk;

        [Header("Cube")]
        [Tooltip("10 cm, the same test object P1.2 and P1.3 used.")]
        public float cubeSize = 0.10f;

        [Tooltip("Metres above the surface along its normal. Flush is wrong: a cube embedded in the " +
                 "tabletop z-fights, and z-fighting looks exactly like a failed placement.")]
        public float surfaceOffset = 0.02f;

        [Header("Rating")]
        [Tooltip("Seconds allowed to pinch the operator rating after the cube appears.")]
        public float ratingWindow = 12f;

        [Header("Output")]
        public bool writeFiles = true;
        public string jsonlFileName = "mruk-probe.jsonl";

        public Condition Active { get; private set; }
        public string ConditionSource { get; private set; } = "pending";
        public bool CubePlaced { get; private set; }
        public float SecondsToCube { get; private set; } = -1f;
        public string FailReason { get; private set; } = "";
        public int OperatorRating { get; private set; }
        public List<SurfaceCandidate> Candidates { get; private set; } = new List<SurfaceCandidate>();

        GameObject _cube;
        float _startAt;
        int _pinches;
        bool _pinchWas;
        bool _conditionLocked, _placed, _rated;
        float _querySeconds = -1f;
        string _rejects = "";
        string _lastRow = "";

        static readonly string[] ConditionNames =
        {
            "clean desk", "lap", "bed", "cluttered table"
        };

        void Awake()
        {
            _startAt = Time.realtimeSinceStartup;
        }

        void Start()
        {
            Active = defaultCondition;
            if (hand == null)
            {
                ConditionSource = "default (no hand source configured)";
                _conditionLocked = true;
            }
        }

        void Update()
        {
            float elapsed = Time.realtimeSinceStartup - _startAt;

            // ---- 1. choose the condition ------------------------------------------------------
            if (!_conditionLocked)
            {
                bool pinching = hand != null && hand.IsTracked && hand.IsPinching;
                if (pinching && !_pinchWas) _pinches++;
                _pinchWas = pinching;

                if (elapsed >= selectionWindow)
                {
                    if (_pinches >= 1 && _pinches <= 4)
                    {
                        Active = (Condition)_pinches;
                        ConditionSource = "selected by " + _pinches + " pinch(es)";
                    }
                    else
                    {
                        Active = defaultCondition;
                        ConditionSource = _pinches == 0
                            ? "default (no pinch in window)"
                            : "default (" + _pinches + " pinches, out of range)";
                    }
                    _conditionLocked = true;
                    _pinchWas = false;   // a pinch held across the lock must not count as a rating
                    Debug.Log("[SurfaceProbe] condition = " + Active + " via " + ConditionSource);
                }
                return;   // nothing else happens until the condition is known
            }

            // ---- 2. place the cube once a room is available ------------------------------------
            if (!_placed)
            {
                bool roomSettled = boot == null || boot.Current != MrukBoot.State.Waiting;
                if (!roomSettled) return;

                if (elapsed < selectionWindow) return;
                Place();
                _placed = true;
                return;
            }

            // ---- 3. operator rating ----------------------------------------------------------
            if (!_rated && !CubePlaced) _rated = true;   // nothing on screen to grade
            if (!_rated)
            {
                bool pinching = hand != null && hand.IsTracked && hand.IsPinching;
                if (pinching && !_pinchWas && OperatorRating < 3) OperatorRating++;
                _pinchWas = pinching;

                if (elapsed - SecondsToCube >= ratingWindow && OperatorRating > 0)
                {
                    _rated = true;
                    WriteReport();
                    Debug.Log("[SurfaceProbe] rating " + OperatorRating + " for " + Active);
                }
                else if (elapsed - SecondsToCube >= ratingWindow * 2f)
                {
                    _rated = true;
                    WriteReport();
                }
            }
        }

        void Place()
        {
            float queryStart = Time.realtimeSinceStartup;
            Candidates = finder != null ? finder.GetAll() : new List<SurfaceCandidate>();
            _querySeconds = Time.realtimeSinceStartup - queryStart;
            _rejects = finder != null ? finder.LastRejectionSummary : "no finder";

            // `default` is not cosmetic: the short-circuit below leaves `best` unassigned when finder
            // is null, and the compiler cannot infer that `!ok` implies it was written.
            SurfaceCandidate best = default;
            bool ok = finder != null && finder.TryGetPrimary(out best);
            if (!ok)
            {
                CubePlaced = false;
                SecondsToCube = -1f;
                FailReason = ExplainNoSurface();
                Debug.LogWarning("[SurfaceProbe] no surface for " + Active + ": " + FailReason);
                WriteReport();
                return;
            }

            // offset along the surface normal, not along world up: on a tilted bed those differ,
            // and using world up would sink one corner of the cube into the mattress
            Vector3 spawn = best.Position + best.Normal * surfaceOffset;

            _cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _cube.name = "P1.6 test cube";
            _cube.transform.localScale = Vector3.one * cubeSize;
            _cube.transform.position = spawn;
            _cube.transform.rotation = Quaternion.LookRotation(
                Vector3.ProjectOnPlane(best.Normal, Vector3.up).normalized == Vector3.zero
                    ? Vector3.forward
                    : Vector3.ProjectOnPlane(best.Normal, Vector3.up).normalized,
                best.Normal);

            var renderer = _cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                // bright and unlit-looking: a dark cube on a dark table reads as "not placed"
                renderer.material.color = new Color(0.15f, 0.9f, 1f);
            }

            // a small nose on the cube so a tilt of the surface is visible at a glance
            var nose = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nose.name = "orientation nose";
            nose.transform.SetParent(_cube.transform, false);
            nose.transform.localScale = Vector3.one * 0.35f;
            nose.transform.localPosition = new Vector3(0f, 0f, 0.75f);
            Destroy(nose.GetComponent<Collider>());

            CubePlaced = true;
            SecondsToCube = Time.realtimeSinceStartup - _startAt;

            Debug.Log("[SurfaceProbe] cube on '" + best.Label + "' at " + best.Height.ToString("0.00") +
                      " m, " + SecondsToCube.ToString("0.00") + " s after launch (" + Active + ")");
            WriteReport();
        }

        /// <summary>
        /// Turns "no surface" into a sentence someone can act on. This is the difference between a
        /// log that says the feature failed and a log that says why, and the phase requires the
        /// failure conditions to be documented.
        /// </summary>
        string ExplainNoSurface()
        {
            if (boot == null) return "no MrukBoot in scene, finder may not be initialised";
            if (!boot.ScenePermissionGranted) return "USE_SCENE not granted, so MRUK cannot read the room: " + boot.ScenePermissionNote;
            if (boot.Current == MrukBoot.State.NoRoom)
                return "no room: the device has no space setup for this room";
            if (boot.Current == MrukBoot.State.Failed)
                return "MRUK failed to load: " + boot.Status;
            if (boot.AnchorCount == 0) return "room has zero anchors";

            var room = Meta.XR.MRUtilityKit.MRUK.Instance != null
                ? Meta.XR.MRUtilityKit.MRUK.Instance.GetCurrentRoom()
                : null;
            if (room == null) return "room disappeared between boot and query";

            int withGeometry = 0, total = 0;
            foreach (var a in room.Anchors)
            {
                if (a == null) continue;
                total++;
                if (a.VolumeBounds.HasValue || a.PlaneRect.HasValue) withGeometry++;
            }
            return "room '" + boot.RoomName + "' has " + total + " anchors (" + withGeometry +
                   " with geometry) but none passed the filter: " + _rejects;
        }

        void WriteReport()
        {
            if (!writeFiles) return;

            string conditionSlug = Slug(ConditionNames[(int)Active - 1]);
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"utc\":\"").Append(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)).Append("\",");
            sb.Append("\"condition\":\"").Append(ConditionNames[(int)Active - 1]).Append("\",");
            sb.Append("\"conditionSource\":\"").Append(Esc(ConditionSource)).Append("\",");
            sb.Append("\"bootState\":\"").Append(boot != null ? boot.Current.ToString() : "none").Append("\",");
            sb.Append("\"bootStatus\":\"").Append(Esc(boot != null ? boot.Status : "")).Append("\",");
            sb.Append("\"scenePermission\":").Append(boot != null && boot.ScenePermissionGranted ? "true" : "false").Append(",");
            sb.Append("\"scenePermissionNote\":\"").Append(Esc(boot != null ? boot.ScenePermissionNote : "")).Append("\",");
            sb.Append("\"roomName\":\"").Append(Esc(boot != null ? boot.RoomName : "")).Append("\",");
            sb.Append("\"roomCount\":").Append(boot != null ? boot.RoomCount : 0).Append(",");
            sb.Append("\"anchorCount\":").Append(boot != null ? boot.AnchorCount : 0).Append(",");
            sb.Append("\"secondsToRoom\":").Append(F(boot != null ? boot.DetectionSeconds : -1f)).Append(",");
            sb.Append("\"queryMs\":").Append(F(_querySeconds * 1000f)).Append(",");
            sb.Append("\"rejects\":\"").Append(Esc(_rejects)).Append("\",");
            sb.Append("\"cubePlaced\":").Append(CubePlaced ? "true" : "false").Append(",");
            sb.Append("\"secondsToCube\":").Append(F(SecondsToCube)).Append(",");
            sb.Append("\"surfaceOffset\":").Append(F(surfaceOffset)).Append(",");
            sb.Append("\"failReason\":\"").Append(Esc(FailReason)).Append("\",");
            sb.Append("\"operatorRating\":").Append(OperatorRating).Append(",");
            sb.Append("\"candidates\":[");
            for (int i = 0; i < Candidates.Count; i++)
            {
                var c = Candidates[i];
                if (i > 0) sb.Append(",");
                sb.Append("{\"label\":\"").Append(Esc(c.Label)).Append("\"");
                sb.Append(",\"height\":").Append(F(c.Height));
                sb.Append(",\"extent\":[").Append(F(c.Extent.x)).Append(",").Append(F(c.Extent.y)).Append("]");
                sb.Append(",\"score\":").Append(F(c.Score));
                sb.Append(",\"fromDevice\":").Append(c.FromDevice ? "true" : "false");
                sb.Append(",\"anchorId\":\"").Append(Esc(c.AnchorId)).Append("\"}");
            }
            sb.Append("]}");

            _lastRow = sb.ToString();
            try
            {
                var path = Path.Combine(Application.persistentDataPath, jsonlFileName);
                File.AppendAllText(path, _lastRow + "\n");

                var summary = Path.Combine(Application.persistentDataPath,
                    "mruk-" + conditionSlug + "-summary.txt");
                File.WriteAllText(summary, BuildSummary());
                Debug.Log("[SurfaceProbe] wrote " + path + " and " + summary);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SurfaceProbe] could not write: " + e.Message);
            }
        }

        string BuildSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("SIGNSPACE P1.6 - MRUK surface probe");
            sb.AppendLine("condition : " + ConditionNames[(int)Active - 1] + "  (" + ConditionSource + ")");
            sb.AppendLine("when      : " + DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            sb.AppendLine("boot      : " + (boot != null ? boot.Current.ToString() : "none"));
            sb.AppendLine("           " + (boot != null ? boot.Status : ""));
            sb.AppendLine("permission: " + (boot != null ? (boot.ScenePermissionGranted ? "ok  " : "MISSING  ") + boot.ScenePermissionNote : "-"));
            sb.AppendLine();
            sb.AppendLine("seconds to room   : " + (boot != null ? F(boot.DetectionSeconds) : "-1"));
            sb.AppendLine("seconds to cube   : " + F(SecondsToCube) + (CubePlaced ? "" : "   (NOT PLACED)"));
            sb.AppendLine("query time        : " + F(_querySeconds * 1000f) + " ms");
            sb.AppendLine("rooms             : " + (boot != null ? boot.RoomCount : 0));
            sb.AppendLine("anchors in room   : " + (boot != null ? boot.AnchorCount : 0));
            sb.AppendLine("filter            : " + _rejects);
            if (!CubePlaced) sb.AppendLine("FAILURE           : " + FailReason);
            sb.AppendLine();
            sb.AppendLine("candidates, best first:");
            if (Candidates.Count == 0) sb.AppendLine("  (none)");
            for (int i = 0; i < Candidates.Count; i++)
            {
                var c = Candidates[i];
                sb.AppendLine("  " + (i + 1).ToString().PadLeft(2) + ". " + c.Label.PadRight(9)
                    + " height " + c.Height.ToString("0.00", CultureInfo.InvariantCulture).PadLeft(5) + " m"
                    + "  extent " + c.Extent.x.ToString("0.00", CultureInfo.InvariantCulture) + " x "
                    + c.Extent.y.ToString("0.00", CultureInfo.InvariantCulture) + " m"
                    + "  score " + c.Score.ToString("0.00", CultureInfo.InvariantCulture)
                    + (c.FromDevice ? "  device" : "  prefab/json")
                    + "  " + c.AnchorId);
            }
            sb.AppendLine();
            sb.AppendLine("operator rating   : " + OperatorRating + "   (0 = not given, 1 wrong place, " +
                          "2 approximately, 3 exactly on the surface)");
            return sb.ToString();
        }

        static string F(float v) { return v.ToString("0.###", CultureInfo.InvariantCulture); }

        static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ");
        }

        static string Slug(string s)
        {
            var chars = new char[s.Length];
            for (int i = 0; i < s.Length; i++)
                chars[i] = char.IsLetterOrDigit(s[i]) ? char.ToLowerInvariant(s[i]) : '-';
            return new string(chars);
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.normal.textColor = Color.white;

            var box = new GUIStyle(GUI.skin.box);
            box.alignment = TextAnchor.UpperLeft;
            box.padding = new RectOffset(10, 10, 10, 10);

            string text = "P1.6 MRUK SURFACE PROBE\n";
            text += "condition : " + (_conditionLocked
                ? ConditionNames[(int)Active - 1] + "  (" + ConditionSource + ")"
                : "choosing... pinch 1=desk 2=lap 3=bed 4=cluttered table  "
                  + Mathf.Max(0f, selectionWindow - (Time.realtimeSinceStartup - _startAt)).ToString("0.0") + " s") + "\n";
            text += "boot      : " + (boot != null ? boot.Current.ToString() : "none")
                 + (boot != null && !boot.ScenePermissionGranted ? "   [USE_SCENE MISSING]" : "") + "\n";
            if (boot != null) text += "            " + boot.Status + "\n";
            text += "to room   : " + (boot != null && boot.DetectionSeconds >= 0
                ? boot.DetectionSeconds.ToString("0.00") + " s" : "-") + "\n";
            text += "to cube   : " + (CubePlaced ? SecondsToCube.ToString("0.00") + " s" : "-") + "\n";
            text += "filter    : " + _rejects + "\n";
            if (!string.IsNullOrEmpty(FailReason)) text += "FAILED    : " + FailReason + "\n";
            text += "candidates: " + Candidates.Count + "\n";
            for (int i = 0; i < Candidates.Count && i < 4; i++)
            {
                var c = Candidates[i];
                text += "  " + c.Label + " " + c.Height.ToString("0.00") + " m  " +
                        c.Extent.x.ToString("0.00") + "x" + c.Extent.y.ToString("0.00") + "\n";
            }
            text += "rating    : " + OperatorRating + (OperatorRating == 0
                ? "   pinch 1-3 after the cube appears" : "");

            GUI.Box(new Rect(12f, 12f, 430f, 30f + 22f * (5 + Mathf.Min(4, Candidates.Count))),
                    GUIContent.none);
            GUI.Label(new Rect(24f, 22f, 410f, 400f), text, style);
        }
    }
}
