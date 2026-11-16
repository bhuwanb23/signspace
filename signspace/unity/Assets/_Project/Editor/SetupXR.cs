using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// Configures the Android build target for XR: the OpenXR loader and Meta's
    /// feature set.
    ///
    /// WHY THIS IS A SCRIPT
    /// -------------------
    /// Both of these are normally two clicks in the XR Plug-in Management window.
    /// That window does not exist in batch mode, so a headless build would
    /// silently ship an APK with NO XR loader enabled - a build that installs,
    /// launches, and shows nothing but a black screen. That is the worst possible
    /// failure: it looks like a working build until someone puts the headset on.
    ///
    /// Meta's own `MetaXRFeatureEnabler` does the second half automatically, but
    /// only when it is NOT in batch mode (it shows a dialog otherwise), so a
    /// headless build misses it as well.
    ///
    /// USAGE
    ///   Unity.exe -batchmode -quit -projectPath unity \
    ///             -executeMethod Signspace.Editor.SetupXR.Run
    /// </summary>
    public static class SetupXR
    {
        const string OpenXRLoaderType = "UnityEngine.XR.OpenXR.OpenXRLoader";

        /// <summary>
        /// Meta's feature set id. `Meta.XR.MetaXRFeatureSet` is internal to the
        /// Core SDK, so the id is written out here. It is a public const in that
        /// class, so this string cannot drift silently - if Meta changes it, the
        /// feature set lookup below returns null and this script fails loudly.
        /// </summary>
        const string MetaXRFeatureSetId = "com.meta.openxr.featureset.metaxr";

        [MenuItem("SIGNSPACE/Configure XR for Android")]
        public static void Run()
        {
            var group = BuildTargetGroup.Android;

            // ---- 1. the XR settings asset ----------------------------------------
            // The XR Plug-in Management window normally creates this. In batch mode
            // there is no window, so it is created here.
            var perBuildTarget = FindOrCreateSettingsAsset();
            if (perBuildTarget == null)
            {
                Debug.LogError("[SetupXR] could not create the XR settings asset.");
                EditorApplication.Exit(1);
                return;
            }

            if (!perBuildTarget.HasManagerSettingsForBuildTarget(group))
            {
                perBuildTarget.CreateDefaultManagerSettingsForBuildTarget(group);
                Debug.Log("[SetupXR] created the XR manager settings for Android");
            }

            var manager = perBuildTarget.ManagerSettingsForBuildTarget(group);
            if (manager == null)
            {
                Debug.LogError("[SetupXR] the XR manager settings for Android are still null.");
                EditorApplication.Exit(1);
                return;
            }

            // ---- 2. the OpenXR loader --------------------------------------------
            bool already = XRPackageMetadataStore.IsLoaderAssigned(OpenXRLoaderType, group);
            bool assigned = XRPackageMetadataStore.AssignLoader(manager, OpenXRLoaderType, group);
            Debug.Log("[SetupXR] OpenXR loader for Android: assigned=" + assigned + " (was already " +
                      already + ")");

            if (!XRPackageMetadataStore.IsLoaderAssigned(OpenXRLoaderType, group))
            {
                Debug.LogError("[SetupXR] the OpenXR loader is still not assigned for Android.");
                EditorApplication.Exit(1);
                return;
            }

            // ---- 2. the Meta XR feature set --------------------------------------
            var featureSet = OpenXRFeatureSetManager.GetFeatureSetWithId(group, MetaXRFeatureSetId);
            if (featureSet == null)
            {
                Debug.LogError("[SetupXR] the Meta XR feature set was not found for Android. The OpenXR " +
                               "loader is assigned but Meta's feature set is missing, so hand tracking and " +
                               "passthrough will not be available. Re-import com.meta.xr.sdk.core and re-run.");
                EditorApplication.Exit(1);
                return;
            }

            if (!featureSet.isEnabled)
            {
                featureSet.isEnabled = true;
                OpenXRFeatureSetManager.SetFeaturesFromEnabledFeatureSets(group);
                Debug.Log("[SetupXR] Meta XR feature set enabled for Android");
            }
            else
            {
                Debug.Log("[SetupXR] Meta XR feature set was already enabled for Android");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // ---- 3. the duplicate native library -------------------------------
            // The Meta XR Core SDK ships its own libopenxr_loader.so, and the
            // Unity OpenXR Plugin ships another one with the same filename.
            // Android's packager refuses to merge two native libraries with the
            // same path, so the build dies in Gradle with:
            //
            //   2 files found with path 'lib/arm64-v8a/libopenxr_loader.so'
            //
            // Meta's own OVRGradleGeneration normally removes the Unity copy when
            // the Meta XR feature set is enabled. In batch mode that hook does not
            // fire (no [Meta] messages appear in the build log), so the exclusion
            // is done here instead: keep Meta's copy, drop everyone else's.
            DisableDuplicateOpenXRLoaders();

            // ---- 4. prove it ------------------------------------------------------
            // In XR Management 4.x the loader list lives in the
            // XRGeneralSettingsPerBuildTarget asset, not in ProjectSettings. So the
            // proof is the loader list itself.
            var loaders = manager.activeLoaders;
            var names = string.Join(", ", loaders.Select(l => l == null ? "null" : l.GetType().FullName));

            Debug.Log("[SetupXR] Android XR loaders : " + names);
            Debug.Log("[SetupXR] Meta XR feature set: " + featureSet.isEnabled);

            bool hasOpenXR = loaders.Any(l => l is UnityEngine.XR.OpenXR.OpenXRLoader);
            if (!hasOpenXR)
            {
                Debug.LogError("[SetupXR] no OpenXR loader in the Android loader list. A build made now " +
                               "would install and show a black screen.");
                EditorApplication.Exit(1);
                return;
            }
            if (!featureSet.isEnabled)
            {
                Debug.LogError("[SetupXR] the Meta XR feature set is not enabled, so hand tracking and " +
                               "passthrough would not be available on the headset.");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("[SetupXR] done. Android has the OpenXR loader and Meta's feature set enabled.");
            LogInputHandlingMembers();
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// Prints how the active input handling is exposed in this editor.
        ///
        /// It has moved and changed shape between versions - it was
        /// `activeInputHandler` as a public property, and in Unity 6 the
        /// documented member is `activeInputHandling`, but the exact accessibility
        /// is not something to guess at. This prints the truth so the next person
        /// does not lose an hour to it.
        /// </summary>
        static void LogInputHandlingMembers()
        {
            var type = typeof(PlayerSettings);
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                                       BindingFlags.Static | BindingFlags.Instance;
            foreach (var member in type.GetMembers(flags))
            {
                if (member.Name.IndexOf("nput", StringComparison.OrdinalIgnoreCase) < 0) continue;
                Debug.Log("[SetupXR] PlayerSettings." + member.MemberType + " " + member.Name);
            }
        }

        /// <summary>
        /// Removes the duplicate OpenXR loader from every package except Meta's.
        ///
        /// Meta's OVRGradleGeneration does exactly this, but only when the
        /// MetaXRFeature is enabled, and its hook does not run in batch mode. The
        /// symptom without it is a Gradle failure that looks nothing like an XR
        /// problem, which is why it is worth doing explicitly and logging.
        /// </summary>
        static void DisableDuplicateOpenXRLoaders()
        {
            int disabled = 0, kept = 0;
            foreach (var importer in PluginImporter.GetAllImporters())
            {
                string assetPath = importer.assetPath;
                if (assetPath == null) continue;
                if (!assetPath.Contains("libopenxr_loader.so") && !assetPath.Contains("openxr_loader.aar"))
                    continue;

                var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath);
                bool isMeta = package != null && package.name.StartsWith("com.meta.xr", StringComparison.OrdinalIgnoreCase);

                if (isMeta)
                {
                    kept++;
                    Debug.Log("[SetupXR] keeping Meta's OpenXR loader: " + assetPath);
                }
                else
                {
                    importer.SetIncludeInBuildDelegate(path => false);
                    disabled++;
                    Debug.Log("[SetupXR] disabled the duplicate OpenXR loader from " +
                              (package != null ? package.name : "an unknown package") + ": " + assetPath);
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[SetupXR] OpenXR loader copies: kept " + kept + " (Meta), disabled " + disabled);
        }

        /// <summary>
        /// Finds the XRGeneralSettingsPerBuildTarget asset, or creates it.
        ///
        /// The XR Plug-in Management window normally makes this asset. Its own
        /// factory method is internal to the package, so this does the same job
        /// with public API: find the asset by type, or make one and save it.
        /// </summary>
        static XRGeneralSettingsPerBuildTarget FindOrCreateSettingsAsset()
        {
            string[] guids = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var found = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(path);
                if (found != null) return found;
            }

            const string path2 = "Assets/XRGeneralSettingsPerBuildTarget.asset";
            var created = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(created, path2);
            AssetDatabase.SaveAssets();
            Debug.Log("[SetupXR] created " + path2);
            return created;
        }
    }
}
