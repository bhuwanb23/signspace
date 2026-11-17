using System;
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
    /// P8's scene wiring, in one place and in one command.
    ///
    /// The phase's claim is a number: a stranger reaches a first lock in under
    /// 75 seconds. The instrumentation that produces that number (PlaytestProbe
    /// and PlaytestTimeline) already exists and compiles, but it was never wired
    /// into a scene, so it could never run. This builder closes that gap.
    ///
    /// Everything it adds is optional at runtime (null dependencies degrade to
    /// "this feature is off", never to an exception), so a scene built without
    /// it still runs.
    ///
    /// USAGE
    ///   Unity.exe -batchmode -quit -projectPath unity -executeMethod Signspace.Editor.PlaytestBuilder.BuildPlaytest
    /// </summary>
    public static class PlaytestBuilder
    {
        [MenuItem("SIGNSPACE/P8 Build playtest rig")]
        public static void BuildPlaytest()
        {
            const string ScenePath = "Assets/_Project/Scenes/Main.unity";
            if (!System.IO.File.Exists(System.IO.Path.GetFullPath(ScenePath)))
            {
                Debug.LogError("[PlaytestBuilder] " + ScenePath + " is missing; run SIGNSPACE/Build main scene first.");
                return;
            }
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

            var matcher = UnityEngine.Object.FindFirstObjectByType<Matcher>();
            var reference = UnityEngine.Object.FindFirstObjectByType<ReferenceHand>();
            var settings = A11ySettings.Instance;
            if (settings == null) settings = UnityEngine.Object.FindFirstObjectByType<A11ySettings>();

            if (matcher == null)
            {
                Debug.LogError("[PlaytestBuilder] no matcher in the scene; build the main scene first.");
                return;
            }

            var probeGo = FindOrCreate("PlaytestProbe");
            var probe = FindOrAdd<PlaytestProbe>(probeGo);
            probe.matcher = matcher;
            probe.referenceHand = reference;
            probe.settings = settings;
            probe.pathOverride = "phases/p8-playtest";
            probe.source = Application.isEditor ? "editor" : "device";
            probe.writeFile = true;
            probe.stallSeconds = 10f;
            probe.stillSpeed = 0.02f;

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Debug.Log("[PlaytestBuilder] PlaytestProbe wired (path=" + probe.LogPath +
                      ", source=" + probe.source + ", stall=" + probe.stallSeconds + "s).");
            Debug.Log("[PlaytestBuilder] P8.2: run the scene, do nothing, record the CSV. " +
                      "P8.3: python scripts/playtest_report.py --gate");
        }

        static GameObject FindOrCreate(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null) return existing;
            return new GameObject(name);
        }

        static T FindOrAdd<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            if (existing != null) return existing;
            return go.AddComponent<T>();
        }
    }
}
