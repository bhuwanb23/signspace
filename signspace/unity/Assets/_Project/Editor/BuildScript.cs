using System;
using System.IO;
using System.Security.Cryptography;
using Signspace.Matching;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// Applies every Player setting the build needs, then builds the APK.
    ///
    /// WHY THIS EXISTS
    /// ---------------
    /// toolchain.md lists the settings as a checklist a human ticks. That is
    /// exactly the kind of thing that gets half-ticked on Day 11. Here the
    /// settings are code: they are reviewed in the diff, they are applied
    /// identically every time, and the same command produces the submission
    /// build on Day 4 that produced the test build on Day 1.
    ///
    /// The values come from phases/p0-foundation/toolchain.md section 2 and
    /// docs/02-tech/TECH-STACK.md section 1.
    ///
    /// USAGE
    ///   Unity.exe -batchmode -quit -projectPath unity \
    ///             -executeMethod Signspace.Editor.BuildScript.BuildAndroid
    ///
    ///   Unity.exe -batchmode -quit -projectPath unity \
    ///             -executeMethod Signspace.Editor.BuildScript.ApplySettingsOnly
    ///
    /// The package name is read from a small text file so that the single
    /// irreversible decision lives in ONE place and is not duplicated here:
    ///   unity/package-name.txt
    /// </summary>
    public static class BuildScript
    {
        const string ProjectRoot = "Assets/_Project";
        const string ScenesPath = ProjectRoot + "/Scenes/Main.unity";
        const string OutputFolder = "Builds/Android";
        const string PackageNameFile = "Assets/_Project/package-name.txt";

        // ---- the settings, all in one place -----------------------------
        const string PackageName = "com.signspace.signspace";  // overridden by package-name.txt
        const string ProductName = "SIGNSPACE";
        const string CompanyName = "SIGNSPACE";
        const int MinimumApi = 32;      // Android 12
        const int TargetApi = 34;       // Android 14
        // 13 is the next code after the v12 APK built on 16 Nov; installing over it must not be a downgrade.
        // 14 is the P7 polish build: new audio, the 70° tray pose, and the perf switches.
        const int BundleVersionCode = 14;
        const string Version = "1.0.0-competition";

        [MenuItem("SIGNSPACE/Apply Player Settings")]
        public static void ApplySettingsOnly()
        {
            ApplySettings();
            Debug.Log("[BuildScript] player settings applied");
        }

        [MenuItem("SIGNSPACE/Build Android APK")]
        public static void BuildAndroid()
        {
            ApplySettings();
            VerifySceneTuning();

            string outDir = Path.Combine(Directory.GetCurrentDirectory(), OutputFolder);
            Directory.CreateDirectory(outDir);
            string apkPath = Path.Combine(outDir, "signspace.apk");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenesPath },
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            Debug.Log("[BuildScript] building " + apkPath);
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log($"[BuildScript] result   : {summary.result}");
            Debug.Log($"[BuildScript] size     : {summary.totalSize / (1024 * 1024)} MB");
            Debug.Log($"[BuildScript] duration : {summary.totalTime}");

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("[BuildScript] BUILD FAILED");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("[BuildScript] sha256   : " + Sha256(apkPath));
            Debug.Log("[BuildScript] apk      : " + apkPath);
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// Applies the Android Player settings. Idempotent.
        /// </summary>
        public static void ApplySettings()
        {
            string packageName = ReadPackageName();

            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = Version;
            // Unity exposes the version code on the platform group, not on PlayerSettings itself.
            PlayerSettings.Android.bundleVersionCode = BundleVersionCode;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, packageName);

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;

            // Quest is ARM64 only
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Android, Il2CppCompilerConfiguration.Release);

            // linear colour space is correct for Quest
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // ISDK needs the new input system. Unity 6 exposes no public setter for this, so it is
            // read back from the serialised project and the build refuses to run if it drifted.
            // (A patch of ProjectSettings.asset here would race the asset database.)
            AssertActiveInputBoth();

            // texture compression
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            EditorUserBuildSettings.buildAppBundle = false;   // the competition wants an APK

            // Vulkan first, GLES3 as the fallback
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.Vulkan });
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);

            // IL2CPP needs the NDK path; take it from the installed module
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

            // ---- P7.5: no animated splash -------------------------------------------------
            // A judge launches once, cold. Unity's logo cannot be removed on this licence, but an
            // animated one costs a second and a half of the first impression; static costs none.
            Debug.Log("[BuildScript] " + SplashSettings.MakeStatic());

            // ---- P7.4: MSAA in the pipeline asset, which is where URP reads it -----------------
            // 4x MSAA is the default and costs bandwidth the 3S cannot spare on a passthrough scene
            // with no specular highlights to benefit from it. 2x is the documented choice; the 4x-vs-2x
            // comparison stays a device row (P7.4, HUMAN-TASKS) because it needs the GPU.
            var pipeline = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(
                "Assets/_Project/Rendering/SignSpacePipeline.asset");
            if (pipeline != null)
            {
                pipeline.msaaSampleCount = 2;
                EditorUtility.SetDirty(pipeline);
                Debug.Log("[BuildScript] MSAA set to 2x in the URP asset");
            }
            else
            {
                Debug.LogWarning("[BuildScript] URP asset missing; MSAA left as configured");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[BuildScript] package name: " + packageName);
        }

        /// <summary>
        /// Refuses to build a scene whose serialised matcher values disagree with the tuned constants.
        ///
        /// WHY THIS EXISTS
        /// ---------------
        /// Unity's serialised scene value beats the code default, so a scene generated before a tuning
        /// change keeps the old bands and nothing reports it. This project shipped exactly that once
        /// (close 20 / slack 20 in the scene, 24 / 15 in the code). The check is cheap and the failure it
        /// prevents is invisible until a learner is told the wrong thing.
        /// </summary>
        static void VerifySceneTuning()
        {
            if (!File.Exists(ScenesPath))
                throw new BuildFailedException("[BuildScript] scene missing: " + ScenesPath);

            EditorSceneManager.OpenScene(ScenesPath, OpenSceneMode.Single);
            var matcher = UnityEngine.Object.FindFirstObjectByType<Matcher>();
            if (matcher == null)
                throw new BuildFailedException("[BuildScript] no Matcher in " + ScenesPath);

            var problems = new System.Text.StringBuilder();
            Check(problems, "lockedDeg", matcher.lockedDeg, Matcher.TunedLockedDeg);
            Check(problems, "closeDeg", matcher.closeDeg, Matcher.TunedCloseDeg);
            Check(problems, "bandReleaseDeg", matcher.bandReleaseDeg, Matcher.TunedReleaseDeg);
            Check(problems, "orientationSlackDeg", matcher.orientationSlackDeg, Matcher.TunedSlackDeg);
            Check(problems, "holdSeconds", matcher.holdSeconds, Matcher.TunedHoldSeconds);

            if (problems.Length > 0)
                throw new BuildFailedException("[BuildScript] scene tuning does not match the code:" + problems);

            Debug.Log("[BuildScript] scene tuning matches: locked " + matcher.lockedDeg.ToString("0.#") +
                      " / close " + matcher.closeDeg.ToString("0.#") +
                      " / release " + matcher.bandReleaseDeg.ToString("0.#") +
                      " / slack " + matcher.orientationSlackDeg.ToString("0.#") +
                      " / hold " + matcher.holdSeconds.ToString("0.00") + " s");
        }

        static void Check(System.Text.StringBuilder problems, string field, float actual, float tuned)
        {
            if (Mathf.Abs(actual - tuned) <= 0.0001f) return;
            problems.Append("\n  " + field + " is " + actual + " in the scene, tuned value is " + tuned);
        }

        /// <summary>
        /// Fails the build when Player Settings → Active Input Handling is not "Both". The Meta
        /// Interaction SDK only raises hand events under the new input system, so a project set to
        /// "Input Manager (Old)" compiles, builds, installs - and then shows no hands at all. That is
        /// precisely the class of failure a build script exists to make impossible.
        /// </summary>
        static void AssertActiveInputBoth()
        {
            const string settingsPath = "ProjectSettings/ProjectSettings.asset";
            if (!File.Exists(settingsPath))
                throw new BuildFailedException("[BuildScript] cannot verify input handling: " + settingsPath + " is missing");

            foreach (string line in File.ReadAllLines(settingsPath))
            {
                if (!line.TrimStart().StartsWith("activeInputHandler:")) continue;

                string value = line.Substring(line.IndexOf(':') + 1).Trim();
                if (value == "2") return;                  // 2 = Both

                throw new BuildFailedException("[BuildScript] activeInputHandler is " + value +
                    "; the hands need 2 (Both). Set Player Settings > Active Input Handling > Both.");
            }

            throw new BuildFailedException("[BuildScript] activeInputHandler not found in " + settingsPath);
        }

        /// <summary>
        /// Reads the package name from a text file, so the one irreversible
        /// decision is recorded in exactly one place and can be changed before
        /// the app is created on the Meta dashboard.
        /// </summary>
        static string ReadPackageName()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), PackageNameFile);
            try
            {
                if (File.Exists(path))
                {
                    string line = File.ReadAllText(path).Trim();
                    if (!string.IsNullOrEmpty(line) && !line.StartsWith("#"))
                    {
                        return line.Split('\n')[0].Trim();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BuildScript] could not read the package name file: " + e.Message);
            }
            return PackageName;
        }

        public static string Sha256(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
