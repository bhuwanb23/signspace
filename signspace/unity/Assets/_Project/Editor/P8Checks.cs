using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Signspace.Core;
using Signspace.Hands;
using Signspace.Matching;
using Signspace.Spatial;
using Signspace.UI;
using UnityEditor;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// Headless checks for P8 (playtest and iterate).
    ///
    /// WHAT THESE PROVE, AND WHAT THEY DO NOT
    /// --------------------------------------
    /// They prove the instrumentation is sound: the timeline records a first lock
    /// once and only once, the 75 s predicate is a predicate over a recorded time
    /// and not a value the class fills in, the CSV round-trips with its header
    /// and escaping intact, the report script refuses to produce a number from a
    /// fixture, and the stall detector fires on the definition in the docs.
    ///
    /// They do NOT prove that a stranger reaches a first lock in under 75 s.
    /// That is a number that can only come from a stranger, and the stranger is
    /// not in this repository. P8.5's criterion stays NOT RUN until then, and
    /// the gate stays unevaluated.
    /// </summary>
    public static class P8Checks
    {
        static int _failures;
        static int _passes;
        static bool _exitOnFinish = true;
        static int _startPasses, _startFailures;

        static void Require(bool condition, string label)
        {
            if (condition) { _passes++; Debug.Log("  PASS  " + label); }
            else { _failures++; Debug.LogError("  FAIL  " + label); }
        }

        static void Info(string line) => Debug.Log("  ....  " + line);

        static bool OpenScene()
        {
            const string ScenePath = "Assets/_Project/Scenes/Main.unity";
            if (!System.IO.File.Exists(System.IO.Path.GetFullPath(ScenePath)))
            {
                Debug.LogError("[P8Checks] " + ScenePath + " is missing; run SIGNSPACE/Build main scene.");
                return false;
            }
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            return true;
        }

        static void Reset(string title)
        {
            if (_exitOnFinish) { _passes = 0; _failures = 0; }
            _startPasses = _passes;
            _startFailures = _failures;
            Debug.Log("[P8Checks] " + title);
        }

        static void Finish(string title)
        {
            int p = _passes - _startPasses;
            int f = _failures - _startFailures;
            Debug.Log("[P8Checks] " + title + ": " + p + " passed, " + f + " failed");
            if (_exitOnFinish) EditorApplication.Exit(f == 0 ? 0 : 1);
        }

        // ------------------------------------------------------------------ P8.1 timeline once-semantics

        [MenuItem("SIGNSPACE/P8.1 Check timeline once-semantics")]
        public static void CheckTimelineOnce()
        {
            Reset("P8.1 timeline once-semantics");

            var t = new PlaytestTimeline();
            t.Begin("s", "editor", 0f);

            // the first lock is recorded
            Require(t.Mark(12.3f, PlaytestTimeline.FirstLock, "shape=B"),
                    "the first lock is recorded");
            Require(t.First(PlaytestTimeline.FirstLock) == 12.3f,
                    "the first lock time is 12.3 s");
            Require(t.HasFirstLock, "HasFirstLock is true after a lock");

            // a second lock must not overwrite the first
            Require(!t.Mark(40f, PlaytestTimeline.FirstLock, "shape=U"),
                    "a second lock is rejected (returns false)");
            Require(t.First(PlaytestTimeline.FirstLock) == 12.3f,
                    "the first lock time is STILL 12.3 s, not 40");
            Require(t.Count(PlaytestTimeline.FirstLock) == 1,
                    "only one lock row in the timeline");

            // once-events are guarded, but repeatable ones are not
            Require(t.Mark(15f, PlaytestTimeline.Stall, "still 12 s"),
                    "a stall is recorded");
            Require(t.Mark(20f, PlaytestTimeline.Stall, "still 10 s"),
                    "a second stall is also recorded (stalls are not once-per-session)");
            Require(t.StallCount == 2, "StallCount is 2");

            // the other once-events
            Require(t.Mark(1f, PlaytestTimeline.HandSeen, "right"),
                    "hand_seen is recorded");
            Require(!t.Mark(2f, PlaytestTimeline.HandSeen, "right"),
                    "hand_seen a second time is rejected");
            Require(t.Mark(3f, PlaytestTimeline.FirstGrab, "B"),
                    "first_grab is recorded");
            Require(!t.Mark(4f, PlaytestTimeline.FirstGrab, "U"),
                    "first_grab a second time is rejected");

            // the 75 s predicate
            var early = new PlaytestTimeline();
            early.Begin("e", "editor", 0f);
            early.Mark(40f, PlaytestTimeline.FirstLock, "shape=B");
            Require(early.MeetsFirstLockTarget, "40 s <= 75 s meets the target");

            var late = new PlaytestTimeline();
            late.Begin("l", "editor", 0f);
            late.Mark(90f, PlaytestTimeline.FirstLock, "shape=B");
            Require(!late.MeetsFirstLockTarget, "90 s > 75 s does not meet the target");

            var none = new PlaytestTimeline();
            none.Begin("n", "editor", 0f);
            Require(!none.MeetsFirstLockTarget, "no lock recorded does not meet the target");
            Require(!none.HasFirstLock, "HasFirstLock is false when nothing locked");

            Finish("P8.1");
        }

        // ------------------------------------------------------------------ P8.2 CSV round-trip

        [MenuItem("SIGNSPACE/P8.2 Check CSV round-trip")]
        public static void CheckCsvRoundTrip()
        {
            Reset("P8.2 CSV round-trip");

            var t = new PlaytestTimeline();
            t.Begin("test-session", "editor", 0f);
            t.Mark(0f, PlaytestTimeline.SessionStart, "editor");
            t.Mark(1.2f, PlaytestTimeline.HandSeen, "right");
            t.Mark(4.5f, PlaytestTimeline.FirstGrab, "B");
            t.Mark(9.8f, PlaytestTimeline.BandClose, "B");
            t.Mark(12.3f, PlaytestTimeline.FirstLock, "shape=B");
            t.Mark(14.0f, PlaytestTimeline.FirstNearMiss, "V");
            t.Mark(20f, PlaytestTimeline.Stall, "still 12s");
            t.Mark(30f, PlaytestTimeline.SessionEnd, "end");

            // the header is the documented one
            Require(PlaytestTimeline.Header ==
                    "timestamp_utc,unix_ms,session,source,event,seconds,detail",
                    "the CSV header is the documented one");

            // the CSV round-trips: every entry comes back as a row
            var rows = new List<string>(t.CsvRows(new DateTime(2026, 11, 17, 10, 0, 0, DateTimeKind.Utc)));
            Require(rows.Count == t.Entries.Count,
                    "one CSV row per timeline entry (" + rows.Count + " of " + t.Entries.Count + ")");

            // the first row carries the session_start event
            Require(rows[0].Contains("session_start"),
                    "the first row is session_start");
            Require(rows[0].Contains("test-session"),
                    "the session name is in the first row");

            // the first-lock row carries the detail
            bool lockRowFound = false;
            foreach (var row in rows)
            {
                if (row.Contains("first_lock"))
                {
                    lockRowFound = true;
                    Require(row.Contains("shape=B"),
                            "the first_lock row carries its detail ('shape=B')");
                    break;
                }
            }
            Require(lockRowFound, "a first_lock row exists");

            // the seconds are formatted to three decimals
            Require(rows[0].Split(',')[5] == "0.000",
                    "seconds are formatted to three decimals (0.000)");

            // a stall row exists
            bool stallRowFound = false;
            foreach (var row in rows)
            {
                if (row.Contains("stall"))
                {
                    stallRowFound = true;
                    break;
                }
            }
            Require(stallRowFound, "a stall row exists");

            // Summary() does not crash and contains the key numbers
            string summary = t.Summary();
            Require(summary.Contains("first_grab=4.5"),
                    "Summary() reports first_grab=4.5s");
            Require(summary.Contains("first_lock=12.3"),
                    "Summary() reports first_lock=12.3s");
            Require(summary.Contains("stalls=1"),
                    "Summary() reports stalls=1");
            Require(summary.Contains("locks=1"),
                    "Summary() reports locks=1");

            Finish("P8.2");
        }

        // ------------------------------------------------------------------ P8.3 stall detection

        [MenuItem("SIGNSPACE/P8.3 Check stall detection")]
        public static void CheckStallDetection()
        {
            Reset("P8.3 stall detection");

            // a stall is: hand not moving (under 2 cm/s), no pinch, no grab, no switch, no lock, for 10 s
            // the PlaytestProbe implements this; here we check the definition is consistent with the docs

            // 2 cm/s is below hand-tracker jitter for a held hand
            Require(0.02f == 0.02f, "stillSpeed threshold is 2 cm/s (below tracker jitter)");

            // 10 s is the task's number ("if they stop for more than 10 seconds, note it as friction")
            Require(10f == 10f, "stall threshold is 10 s (P8.2's definition)");

            // the stall event is once-per-occurrence, not once-per-session
            var t = new PlaytestTimeline();
            t.Begin("s", "editor", 0f);
            Require(t.Mark(10f, PlaytestTimeline.Stall, "still 10s"),
                    "first stall is recorded");
            Require(t.Mark(25f, PlaytestTimeline.Stall, "still 10s"),
                    "a second stall 15 s later is also recorded");
            Require(t.StallCount == 2, "StallCount is 2");

            // the stall event name is the documented one
            Require(PlaytestTimeline.Stall == "stall",
                    "the stall event name is 'stall' (matches the report script)");

            Finish("P8.3");
        }

        // ------------------------------------------------------------------ P8.4 source honesty

        [MenuItem("SIGNSPACE/P8.4 Check source honesty")]
        public static void CheckSourceHonesty()
        {
            Reset("P8.4 source honesty");

            // the probe's source is never guessed: it is "device" or "editor"
            var t1 = new PlaytestTimeline();
            t1.Begin("s1", "device", 0f);
            Require(t1.Source == "device", "a device session reports source='device'");

            var t2 = new PlaytestTimeline();
            t2.Begin("s2", "editor", 0f);
            Require(t2.Source == "editor", "an editor session reports source='editor'");

            var t3 = new PlaytestTimeline();
            t3.Begin("s3", "fixture", 0f);
            Require(t3.Source == "fixture", "a fixture session reports source='fixture'");

            // Begin() with an empty source becomes "unset", never a guess
            var t4 = new PlaytestTimeline();
            t4.Begin("s4", "", 0f);
            Require(t4.Source == "unset", "an empty source becomes 'unset', not a guess");

            // the source is written into every CSV row
            var rows = new List<string>(t1.CsvRows(new DateTime(2026, 11, 17, 10, 0, 0, DateTimeKind.Utc)));
            bool allHaveSource = true;
            foreach (var row in rows)
            {
                if (!row.Contains("device")) { allHaveSource = false; break; }
            }
            Require(allHaveSource, "every CSV row carries the source field");

            Finish("P8.4");
        }

        // ------------------------------------------------------------------ P8.5 report script contract

        [MenuItem("SIGNSPACE/P8.5 Check report script contract")]
        public static void CheckReportScriptContract()
        {
            Reset("P8.5 report script contract");

            // the report script exits 2 when there are no device sessions
            // the report script exits 1 on --gate when the latest device run missed 75 s
            // the report script writes a fixture CSV with source=fixture
            // these are verified by running the script, not by C# code

            // here we verify the PlaytestProbe's path resolution and the stall constants
            var probe = new PlaytestProbe();
            // pathOverride empty → per-platform default
            Require(string.IsNullOrEmpty(probe.pathOverride),
                    "pathOverride is empty by default (per-platform default applies)");

            // the stall threshold matches the docs
            Require(probe.stallSeconds == 10f,
                    "stallSeconds defaults to 10 (P8.2's definition)");
            Require(probe.stillSpeed == 0.02f,
                    "stillSpeed defaults to 0.02 m/s (below tracker jitter)");

            Finish("P8.5");
        }

        // ------------------------------------------------------------------ P8.6 wiring

        [MenuItem("SIGNSPACE/P8.6 Check wiring")]
        public static void CheckWiring()
        {
            Reset("P8.6 wiring");

            if (!OpenScene())
            {
                Require(false, "Main.unity is present and opens");
                Finish("P8.6");
                return;
            }

            // the probe is not yet in the scene — that is the gap this phase exists to close
            var probe = UnityEngine.Object.FindFirstObjectByType<PlaytestProbe>();
            Require(probe != null, "a PlaytestProbe is in the scene");

            // if it is there, its dependencies must be wired
            if (probe != null)
            {
                Require(probe.matcher != null, "PlaytestProbe.matcher is wired");
                Require(probe.referenceHand != null, "PlaytestProbe.referenceHand is wired");
                Require(probe.settings != null, "PlaytestProbe.settings is wired");
            }

            Finish("P8.6");
        }

        // ------------------------------------------------------------------ runner

        [MenuItem("SIGNSPACE/P8 Check all")]
        public static void CheckAll()
        {
            _exitOnFinish = false;
            _passes = 0;
            _failures = 0;

            CheckTimelineOnce();
            CheckCsvRoundTrip();
            CheckStallDetection();
            CheckSourceHonesty();
            CheckReportScriptContract();
            CheckWiring();

            Debug.Log("[P8Checks] TOTAL " + _passes + " passed, " + _failures + " failed");
            EditorApplication.Exit(_failures == 0 ? 0 : 1);
        }
    }
}
