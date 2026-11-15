using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>
    /// Counts P1.2's twenty grab trials, per hand, and writes them where they can be read afterwards.
    ///
    /// WHY THIS EXISTS AT ALL
    /// ----------------------
    /// "20/20 with each hand" is a claim that has to survive being questioned. Held in a headset, it
    /// becomes a memory of a morning. So every attempt is recorded as it happens - hand, distance at
    /// the moment of the pinch, whether it engaged - and the summary is what goes into grab-log.md.
    /// P1's own trap list: an unwritten verdict is a rumour by Day 4.
    ///
    /// THE DEFINITION OF A MISS, STATED ONCE
    /// -------------------------------------
    ///   PINCH         a pinch closing on nothing grabbable within intentRadius. NOT a trial.
    ///   ATTEMPT       a pinch closing with a grabbable within intentRadius. One trial.
    ///   SUCCESS       an attempt that engaged the grab.
    ///   MISS          an attempt that did not engage. Always because the pinch point was farther
    ///                 than grabDistance + radius; the recorded distance says by how much, which is
    ///                 the difference between "the threshold is too tight" and "the tracker drifted".
    ///
    /// Reported per hand, because P1.2 asks for 20/20 with EACH hand, and a combined number would
    /// hide a one-handed problem - which is exactly the failure a user with one dominant hand would
    /// experience and a developer testing with the other would never see.
    /// </summary>
    public class GrabTrialLogger : MonoBehaviour
    {
        [Tooltip("File name inside persistentDataPath.")]
        public string fileName = "grab-trials.jsonl";

        [Tooltip("Write the summary to a second, human-readable file on quit and on pause.")]
        public string summaryFileName = "grab-summary.txt";

        [Tooltip("Also write the summary whenever a hand reaches this many attempts, so the numbers " +
                 "survive the app being killed from the recent list. 20 matches the P1.2 trial size.")]
        public int writeEvery = 20;

        [Tooltip("Draw the running tally in a development build.")]
        public bool showReadout = true;

        [Serializable]
        public struct Trial
        {
            public float time;
            public string hand;
            public string target;
            public float distance;
            public bool engaged;
        }

        readonly List<Trial> _trials = new List<Trial>(64);
        int _leftAttempts, _leftSuccesses, _rightAttempts, _rightSuccesses;
        string _lastWriteNote = "not written yet";
        GUIStyle _style;

        public int Attempts => _leftAttempts + _rightAttempts;
        public int Successes => _leftSuccesses + _rightSuccesses;

        /// <summary>Attempts and successes as "L 18/20 · R 20/20".</summary>
        public string PerHand =>
            "L " + _leftSuccesses + "/" + _leftAttempts + " · R " + _rightSuccesses + "/" + _rightAttempts;

        public string Summary
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append("P1.2 grab trials · ").Append(Attempts).Append(" attempts, ")
                  .Append(Successes).Append(" engaged, ")
                  .Append(Attempts - Successes).Append(" missed\n");
                sb.Append("  left  ").Append(_leftSuccesses).Append('/').Append(_leftAttempts).Append('\n');
                sb.Append("  right ").Append(_rightSuccesses).Append('/').Append(_rightAttempts).Append('\n');

                if (_trials.Count > 0)
                {
                    float worstMiss = 0f;
                    int misses = 0;
                    for (int i = 0; i < _trials.Count; i++)
                    {
                        if (_trials[i].engaged) continue;
                        misses++;
                        worstMiss = Mathf.Max(worstMiss, _trials[i].distance);
                    }
                    if (misses > 0)
                    {
                        sb.Append("  farthest missed attempt: ").Append(worstMiss.ToString("0.000")).Append(" m")
                          .Append(" - if this is under grabDistance+radius the miss was not distance\n");
                    }
                }
                sb.Append("  ").Append(_lastWriteNote);
                return sb.ToString();
            }
        }

        public void Record(MetaHandSource hand, Grabbable target, bool engaged, float distance)
        {
            if (hand == null) return;

            _trials.Add(new Trial
            {
                time = Time.realtimeSinceStartup,
                hand = hand.hand.ToString(),
                target = target != null ? target.name : "none",
                distance = distance,
                engaged = engaged
            });

            if (hand.hand == MetaHandSource.Hand.Left)
            {
                _leftAttempts++; if (engaged) _leftSuccesses++;
            }
            else
            {
                _rightAttempts++; if (engaged) _rightSuccesses++;
            }

            AppendLine(_trials[_trials.Count - 1]);

            if (writeEvery > 0 && Attempts % writeEvery == 0) WriteSummary();
        }

        /// <summary>Clears the tally. The 20-trial protocol is run per session.</summary>
        public void ResetTrials()
        {
            _trials.Clear();
            _leftAttempts = _leftSuccesses = _rightAttempts = _rightSuccesses = 0;
            _lastWriteNote = "tally reset";
        }

        void AppendLine(Trial t)
        {
            try
            {
                var path = Path.Combine(Application.persistentDataPath, fileName);
                var line = "{\"t\":" + t.time.ToString("0.000") +
                           ",\"hand\":\"" + t.hand +
                           "\",\"target\":\"" + t.target +
                           "\",\"distance\":" + t.distance.ToString("0.0000") +
                           ",\"engaged\":" + (t.engaged ? "true" : "false") + "}\n";
                File.AppendAllText(path, line);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GrabTrialLogger] append failed: " + e.Message);
            }
        }

        public void WriteSummary()
        {
            try
            {
                var path = Path.Combine(Application.persistentDataPath, summaryFileName);
                var text = "SIGNSPACE grab trial summary\n" +
                           "device: " + SystemInfo.deviceModel + " · " + SystemInfo.operatingSystem + "\n" +
                           "unity: " + Application.unityVersion + "\n\n" +
                           Summary + "\n\n" +
                           "Definition: a pinch closing with a grabbable within intentRadius is an ATTEMPT; " +
                           "an attempt that engages is a SUCCESS; an attempt that does not is a MISS, and its " +
                           "recorded distance says whether the pinch point was simply too far.\n";
                File.WriteAllText(path, text);
                _lastWriteNote = "summary written to " + path;
            }
            catch (Exception e)
            {
                _lastWriteNote = "summary write failed: " + e.Message;
                Debug.LogWarning("[GrabTrialLogger] " + _lastWriteNote);
            }
        }

        void OnApplicationQuit() => WriteSummary();
        void OnApplicationPause(bool paused) { if (paused) WriteSummary(); }

        void Awake()
        {
            _style = new GUIStyle
            {
                fontSize = 13,
                richText = false,
                normal = { textColor = new Color(1f, 0.95f, 0.75f, 1f) }
            };
        }

        void OnGUI()
        {
            if (!showReadout) return;
            if (!Debug.isDebugBuild && !Application.isEditor) return;
            var rect = new Rect(12f, Screen.height - 74f, 460f, 62f);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(rect, "GRAB  " + PerHand + "\n" + _lastWriteNote, _style);
        }
    }
}
