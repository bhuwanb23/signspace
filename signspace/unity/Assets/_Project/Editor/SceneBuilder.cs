using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// Builds the Main scene from code.
    ///
    /// WHY
    /// ---
    /// SETUP.md describes the scene as eight manual steps in the editor, each
    /// of which can be done slightly differently by a different person and
    /// none of which leaves a trace in the repository. A scene built by a
    /// script is reproducible: the scene is a build artefact, not a document.
    ///
    /// It also means the headset build is one command, on a machine that has
    /// never opened the editor GUI.
    ///
    /// USAGE
    ///   Unity.exe -batchmode -quit -projectPath unity \
    ///             -executeMethod Signspace.Editor.SceneBuilder.BuildMainScene
    ///
    /// The Meta XR rig is located by searching for the prefab by name, because
    /// its path inside the package changed between SDK versions. If it cannot
    /// be found the script says so plainly and leaves a plain camera behind,
    /// so the rest of the pipeline still runs.
    /// </summary>
    public static class SceneBuilder
    {
        const string ScenesFolder = "Assets/_Project/Scenes";
        const string ScenePath = ScenesFolder + "/Main.unity";

        [MenuItem("SIGNSPACE/Build Main Scene")]
        public static void BuildMainScene()
        {
            Directory.CreateDirectory(ScenesFolder);

            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            // ---- camera rig ------------------------------------------------
            var rig = FindPrefab("OVRCameraRig");
            GameObject rigGo;
            if (rig != null)
            {
                rigGo = (GameObject)PrefabUtility.InstantiatePrefab(rig);
                rigGo.name = "OVRCameraRig";
                Debug.Log("[SceneBuilder] using the Meta XR camera rig");
            }
            else
            {
                rigGo = new GameObject("OVRCameraRig");
                var camGo = new GameObject("CenterEyeAnchor");
                camGo.transform.SetParent(rigGo.transform, false);
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.06f, 0.07f, 0.09f, 1f);
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = 100f;
                Debug.LogWarning("[SceneBuilder] the Meta XR camera rig was not found. " +
                                 "A plain camera was created instead. Check that Meta XR All-in-One is imported.");
            }

            // ---- the three systems that matter -----------------------------
            var matchGo = new GameObject("Matcher");
            var matcher = matchGo.AddComponent<Signspace.Matching.Matcher>();
            matchGo.AddComponent<Signspace.Matching.DebugOverlay>();
            var debugOverlay = matchGo.GetComponent<Signspace.Matching.DebugOverlay>();
            debugOverlay.matcher = matcher;

            var sourceGo = new GameObject("SkeletonPoseSource");
            var source = sourceGo.AddComponent<Signspace.Matching.SkeletonPoseSource>();
            source.matcher = matcher;
            debugOverlay.poseSource = source;

            // ---- the reference hand: the thing you grab and turn ------------
            var refGo = new GameObject("ReferenceHand");
            var reference = refGo.AddComponent<Signspace.Matching.ReferenceHand>();
            var visualGo = new GameObject("HandVisual");
            visualGo.transform.SetParent(refGo.transform, false);
            var visual = visualGo.AddComponent<Signspace.Matching.HandVisual>();
            reference.visual = visual;
            reference.pivot = refGo.transform;
            reference.trackedHand = source;

            var loaderGo = new GameObject("SignSetLoader");
            var loader = loaderGo.AddComponent<Signspace.Matching.SignSetLoader>();
            loader.matcher = matcher;

            var stateGo = new GameObject("AppState");
            stateGo.AddComponent<Signspace.Matching.AppStateMachine>();

            var saveGo = new GameObject("SaveSystem");
            saveGo.AddComponent<Signspace.Matching.SaveSystem>();

            // ---- P1.1: the two tracked hands, and the HandVisual prefab ---------------------
            // This is the step that was missing entirely before P1.1: nothing in the scene had
            // ever bound a tracked skeleton to the matcher, so the matcher was being fed an empty
            // joint array and the reference hand's grab had no pinch source. BuildHandRig() emits
            // Assets/_Project/Prefabs/HandVisual.prefab, instantiates both hands, and assigns the
            // right hand's MetaHandSource to the reference hand.
            HandRigBuilder.BuildHandRig();

            // ---- P1.2: the grab test rig ---------------------------------------------------
            // One floating cube, a PinchGrabber per hand, the trial logger and the no-fallback guard.
            // This is a SPIKE rig, not content: P2 replaces the cube with the first real shape. It
            // lives in the scene builder so that one command still produces a headset build that can
            // answer P1.2's question.
            GrabTestBuilder.BuildGrabTest();

            // ---- scene settings ---------------------------------------------
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.35f, 0.35f, 0.40f);
            RenderSettings.ambientEquatorColor = new Color(0.22f, 0.22f, 0.24f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.10f, 0.11f);

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("[SceneBuilder] wrote " + ScenePath);
        }

        /// <summary>Finds a prefab anywhere in the project or in an imported package.</summary>
        static GameObject FindPrefab(string prefabName)
        {
            string[] guids = AssetDatabase.FindAssets(prefabName + " t:Prefab");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".prefab")) continue;
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;
                if (go.name != prefabName) continue;
                return go;
            }
            return null;
        }
    }
}
