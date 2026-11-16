using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Signspace.Editor
{
    /// <summary>
    /// Creates the Universal Render Pipeline assets and points the project at them.
    ///
    /// WHY THIS IS A SCRIPT AND NOT A CLICK
    /// ----------------------------------
    /// A render pipeline that is not assigned is the single most common way a Unity
    /// project renders magenta on a device and nobody notices until the headset is
    /// on someone's face. Doing it here means the assignment is in the repository,
    /// it is applied the same way on every machine, and the build cannot be made
    /// with the pipeline half-configured.
    ///
    /// Meta's project setup guidance says the URP step is required when the full
    /// All-in-One SDK is installed, which is what we install.
    ///
    /// USAGE
    ///   Unity.exe -batchmode -quit -projectPath unity \
    ///             -executeMethod Signspace.Editor.SetupRenderPipeline.Run
    /// </summary>
    public static class SetupRenderPipeline
    {
        const string Folder = "Assets/_Project/Rendering";

        [MenuItem("SIGNSPACE/Set up the render pipeline")]
        public static void Run()
        {
            Directory.CreateDirectory(Folder);

            var rendererData = LoadOrCreate<UniversalRendererData>(
                Folder + "/SignSpaceRenderer.asset");
            var pipeline = UniversalRenderPipelineAsset.Create(rendererData);
            pipeline.name = "SignSpacePipeline";

            AssetDatabase.DeleteAsset(Folder + "/SignSpacePipeline.asset");
            AssetDatabase.CreateAsset(pipeline, Folder + "/SignSpacePipeline.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // the default pipeline, and the current quality level
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;

            // every quality level, so switching quality in a build cannot drop the pipeline
            string[] levels = QualitySettings.names;
            for (int i = 0; i < levels.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(0, false);

            EditorUtility.SetDirty(GraphicsSettings.GetGraphicsSettings());
            AssetDatabase.SaveAssets();

            Debug.Log("[SetupRenderPipeline] assigned " + pipeline.name + " to the default pipeline and to " +
                      levels.Length + " quality level(s)");

            // prove it, so nobody has to trust a log line
            var check = GraphicsSettings.defaultRenderPipeline;
            if (check == null)
            {
                Debug.LogError("[SetupRenderPipeline] FAILED: the default render pipeline is still null.");
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("[SetupRenderPipeline] verified: GraphicsSettings.defaultRenderPipeline = " + check.name);
            EditorApplication.Exit(0);
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            AssetDatabase.SaveAssets();
            return created;
        }
    }
}
