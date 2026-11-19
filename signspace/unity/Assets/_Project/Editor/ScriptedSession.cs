using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Signspace.Matching;

namespace Signspace.Editor
{
    /// <summary>
    /// Runs the lock / near-miss flow with no headset attached, and wires the scripted hand into the
    /// real scene for Play Mode.
    ///
    /// WHY BATCH MODE MATTERS
    /// ---------------------
    /// A Play Mode driver that only a person can run is a demo, not evidence. The same driver, stepped
    /// with a fixed delta, produces a repeatable result that a reviewer can re-run with one command -
    /// and repeatability is the difference between "it works" and "it worked once".
    ///
    /// WHAT THIS DOES NOT REPLACE
    /// --------------------------
    /// H34-H40 (device, performance, strangers) stay unrun. This harness answers a narrower question:
    /// are the matcher, the bands, the hold, the latch and the near-miss wiring correct? It cannot
    /// answer whether a Quest tracks well enough for any of it to fire on a real hand.
    ///
    /// USAGE
    ///   Unity.exe -batchmode -quit -projectPath unity -executeMethod Signspace.Editor.ScriptedSession.RunBatch
    ///   SIGNSPACE/Dev/Add scripted hand driver   (menu, for Play Mode)
    /// </summary>
    public static class ScriptedSession
    {
        const float Dt = 1f / 72f;                 // 72 Hz, the budget's frame time
        const string SignsFolder = "Assets/StreamingAssets/Signs";

        // ---- Play Mode ---------------------------------------------------------------------

        [MenuItem("SIGNSPACE/Dev/Add scripted hand driver")]
        public static void AddDriverToScene()
        {
            var matcher = UnityEngine.Object.FindFirstObjectByType<Matcher>();
            if (matcher == null)
            {
                EditorUtility.DisplayDialog("Scripted hand driver",
                    "No Matcher in the open scene. Open Main.unity first.", "OK");
                return;
            }

            var existing = UnityEngine.Object.FindFirstObjectByType<ScriptedHandSource>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorUtility.DisplayDialog("Scripted hand driver",
                    "A driver already exists on '" + existing.name + "'.", "OK");
                return;
            }

            var go = new GameObject("ScriptedHandDriver");
            Undo.RegisterCreatedObjectUndo(go, "Add scripted hand driver");

            var driver = go.AddComponent<ScriptedHandSource>();
            driver.matcher = matcher;

            // If the scene has a pose source that feeds this matcher, let the driver feed its joints
            // so the drawn hand moves too. The driver then stops ticking the matcher itself.
            var sources = UnityEngine.Object.FindObjectsByType<SkeletonPoseSource>(FindObjectsSortMode.None);
            foreach (var source in sources)
            {
                if (source.matcher == matcher && source.driveMatcher)
                {
                    driver.poseSource = source;
                    driver.driveMatcherDirectly = false;
                    break;
                }
            }

            Selection.activeGameObject = go;
            Debug.Log("[ScriptedSession] driver added. Press Play; set 'shapeId' or tick 'cycleShapes'. " +
                      "This is dev-only and disables itself in a release build.");
        }

        // ---- batch mode --------------------------------------------------------------------

        public static void RunBatch()
        {
            var poses = LoadPoses();
            if (poses.Count == 0)
            {
                Debug.LogError("[ScriptedSession] no poses loaded from " + SignsFolder +
                               ". Nothing to perform; refusing to report a result.");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("[ScriptedSession] loaded shapes: " + string.Join(", ", poses.ConvertAll(p => p.Id)));

            var results = new List<(string name, bool passed, string detail)>();

            results.Add(Scenario(
                "correct shape locks (B)",
                poses, noiseDeg: 0f,
                perform: "B", target: "B", seconds: 4f,
                expectLocked: true, expectedLock: "B"));

            results.Add(Scenario(
                "wrong shape never locks (target B, hand O)",
                poses, noiseDeg: 0f,
                perform: "O", target: "B", seconds: 4f,
                expectLocked: false, expectedLock: null));

            results.Add(Scenario(
                "near-miss names the shape actually made (target B, hand V)",
                poses, noiseDeg: 0f,
                perform: "V", target: "B", seconds: 6f,
                expectLocked: false, expectedLock: null,
                expectedNearMiss: "V"));

            results.Add(Scenario(
                "2 deg/joint noise still locks (U)",
                poses, noiseDeg: 2f,
                perform: "U", target: "U", seconds: 8f,
                expectLocked: true, expectedLock: "U"));

            results.Add(Scenario(
                "4 deg/joint noise is the honest edge (B)",
                poses, noiseDeg: 4f,
                perform: "B", target: "B", seconds: 8f,
                expectLocked: null, expectedLock: null));   // reported, not asserted: see MATCH-ALGORITHM.md 11

            int failed = 0;
            var summary = new StringBuilder();
            summary.AppendLine();
            summary.AppendLine("SCRIPTED SESSION (no headset)");
            foreach (var r in results)
            {
                summary.AppendLine("  " + (r.passed ? "PASS" : "FAIL") + "  " + r.name + "  " + r.detail);
                if (!r.passed) failed++;
            }
            summary.AppendLine();
            summary.AppendLine(failed == 0
                ? "All scripted flows behaved. This is not device evidence: H34-H40 remain unrun."
                : failed + " scripted flow(s) failed. This is a real failure and predates any headset discussion.");
            Debug.Log(summary.ToString());

            EditorApplication.Exit(failed == 0 ? 0 : 1);
        }

        /// <summary>
        /// One scenario: a fresh matcher, a scripted hand, a fixed step, and the events the app listens to.
        /// </summary>
        static (string, bool, string) Scenario(
            string name, List<HandPose> poses, float noiseDeg,
            string perform, string target, float seconds,
            bool? expectLocked, string expectedLock, string expectedNearMiss = null)
        {
            var root = new GameObject("ScriptedScenario");
            try
            {
                var matcher = root.AddComponent<Matcher>();
                matcher.shapes = new List<HandPose>(poses);
                matcher.nearMissEnabled = true;
                matcher.nearMissCooldownSeconds = 0f;      // each scenario is its own session
                matcher.weights = AssetDatabase.LoadAssetAtPath<JointWeights>(
                    "Assets/_Project/Matching/Weights.asset");

                var driver = root.AddComponent<ScriptedHandSource>();
                driver.matcher = matcher;
                driver.driveMatcherDirectly = true;
                driver.verboseLogging = false;              // the summary is the output, not the frame log
                driver.noiseDeg = noiseDeg;
                driver.approachSeconds = 0f;

                string locked = null, nearMiss = null;
                int bandChanges = 0;
                Band lastBand = Band.Warming;

                matcher.OnLocked += id => locked = id;
                matcher.OnNearMiss += (id, deg) => nearMiss = id;
                matcher.OnBandChanged += band =>
                {
                    if (band != lastBand) { bandChanges++; lastBand = band; }
                };

                // Start() never runs outside Play Mode, and this harness runs in batch. Binding
                // explicitly is the difference between measuring the matcher and measuring nothing.
                driver.Bind();

                matcher.SetTarget(target);
                driver.SetShape(perform);
                matcher.SetTarget(target);                  // performing and target may differ (near-miss rows)

                int steps = Mathf.CeilToInt(seconds / Dt);
                float lockedAt = -1f, nearMissAt = -1f;
                for (int i = 0; i < steps; i++)
                {
                    driver.Step(Dt);
                    float t = (i + 1) * Dt;
                    if (locked != null && lockedAt < 0f) lockedAt = t;
                    if (nearMiss != null && nearMissAt < 0f) nearMissAt = t;
                }

                var detail = new StringBuilder();
                detail.Append("locked=").Append(locked ?? "no");
                if (lockedAt >= 0f) detail.Append(" at ").Append(lockedAt.ToString("0.00")).Append("s");
                detail.Append(", nearMiss=").Append(nearMiss ?? "no");
                if (nearMissAt >= 0f) detail.Append(" at ").Append(nearMissAt.ToString("0.00")).Append("s");
                detail.Append(", band changes=").Append(bandChanges);

                bool passed = true;
                if (expectLocked == true && locked != expectedLock) passed = false;
                if (expectLocked == false && locked != null) passed = false;
                if (expectLocked == null) passed = true;                        // reported only
                if (expectedNearMiss != null && nearMiss != expectedNearMiss) passed = false;

                return (name, passed, detail.ToString());
            }
            catch (Exception e)
            {
                return (name, false, "threw: " + e.GetType().Name + " " + e.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        static List<HandPose> LoadPoses()
        {
            var poses = new List<HandPose>();
            string dir = Path.Combine(Application.dataPath, "StreamingAssets/Signs");
            if (!Directory.Exists(dir))
            {
                Debug.LogError("[ScriptedSession] missing folder " + dir);
                return poses;
            }

            // The runtime list is index.json (SignSetLoader reads the same file). Loading every
            // *.json would pull in EXAMPLE-B.json, a template the app never ships - and a duplicate
            // id would make "which B did I lock?" unanswerable.
            string indexPath = Path.Combine(dir, "index.json");
            if (!File.Exists(indexPath))
            {
                Debug.LogError("[ScriptedSession] missing " + indexPath);
                return poses;
            }

            var ids = ParseShapeIds(File.ReadAllText(indexPath));
            foreach (string id in ids)
            {
                string file = Path.Combine(dir, id + ".json");
                if (!File.Exists(file))
                {
                    Debug.LogError("[ScriptedSession] index lists '" + id + "' but " + file + " is missing");
                    continue;
                }

                string error;
                var pose = HandPose.FromJson(File.ReadAllText(file), out error);
                if (pose == null)
                {
                    Debug.LogError("[ScriptedSession] " + id + ".json: " + error);
                    continue;
                }
                poses.Add(pose);
            }

            return poses;
        }

        /// <summary>index.json is <c>{ "version": 1, "shapes": ["B", ...] }</c>; read the list, not the files.</summary>
        static List<string> ParseShapeIds(string json)
        {
            var ids = new List<string>();
            int start = json.IndexOf("\"shapes\"", StringComparison.Ordinal);
            int open = start < 0 ? -1 : json.IndexOf('[', start);
            int close = open < 0 ? -1 : json.IndexOf(']', open);
            if (close < 0)
            {
                Debug.LogError("[ScriptedSession] index.json has no shapes array; refusing to guess.");
                return ids;
            }

            foreach (string raw in json.Substring(open + 1, close - open - 1).Split(','))
            {
                string id = raw.Trim().Trim('"');
                if (id.Length > 0) ids.Add(id);
            }
            return ids;
        }
    }
}
