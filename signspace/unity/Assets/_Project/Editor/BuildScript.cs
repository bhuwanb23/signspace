using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Signspace.Matching;

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
        // Consumed, then incremented, on every build: Meta rejects an upload whose version code
        // already exists on a channel, and its error does not name the conflicting build.
        const string VersionCodeFile = "Assets/_Project/version-code.txt";

        // ---- the settings, all in one place -----------------------------
        const string PackageName = "com.signspace.signspace";  // overridden by package-name.txt
        const string ProductName = "SIGNSPACE";
        const string CompanyName = "SIGNSPACE";
        const int MinimumApi = 32;      // Android 12
        const int TargetApi = 34;       // Android 14
        const int BundleVersionCode = 1;
        const string Version = "0.9.0";

        [MenuItem("SIGNSPACE/Apply Player Settings")]
        public static void ApplySettingsOnly()
        {
            ApplySettings();
            Debug.Log("[BuildScript] player settings applied");
        }

        [MenuItem("SIGNSPACE/Build Android APK")]
        public static void BuildAndroid()
        {
            int versionCode = TakeVersionCode();
            ApplySettings(versionCode);

            // A scene value silently overrides a code default, so a threshold that only exists in the
            // C# is a threshold the app may not be running. Refuse to build an app whose tuning
            // disagrees with the one the tuner measured.
            if (!VerifySceneTuning())
            {
                Debug.LogError("[BuildScript] BUILD ABORTED: the scene's serialised matcher tuning does " +
                               "not match Matcher.Tuned*. Run SceneBuilder.BuildMainScene and build again.");
                EditorApplication.Exit(1);
                return;
            }

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
            // The APK's own size, not summary.totalSize: that figure includes every intermediate and
            // said "1256 MB" for a 62 MB APK, which is exactly the kind of number someone quotes.
            long apkBytes = File.Exists(apkPath) ? new FileInfo(apkPath).Length : 0;
            Debug.Log($"[BuildScript] apk size : {apkBytes / (1024 * 1024)} MB");
            Debug.Log($"[BuildScript] build total : {summary.totalSize / (1024 * 1024)} MB (includes intermediates)");
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
        public static void ApplySettings(int? versionCode = null)
        {
            string packageName = ReadPackageName();

            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = Version;
            // Android-specific in Unity 6: PlayerSettings.bundleVersionCode does not exist.
            // null leaves the serialised value alone (ApplySettingsOnly must not consume a code).
            if (versionCode.HasValue)
                PlayerSettings.Android.bundleVersionCode = versionCode.Value;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, packageName);

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;

            // Quest is ARM64 only
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Android, Il2CppCompilerConfiguration.Release);

            // linear colour space is correct for Quest
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // ISDK needs the new input system, and Unity 6 exposes NO public API for it: the value lives
            // in ProjectSettings.asset as `activeInputHandler` and is set by
            // scripts/patch_player_settings.ps1 before the editor launches. Assigning it from here was a
            // regression that did not compile; the build verifies the serialised value instead.
            VerifyActiveInputHandling();

            // texture compression
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            EditorUserBuildSettings.buildAppBundle = false;   // the competition wants an APK

            // Vulkan first, GLES3 as the fallback
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.Vulkan });
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);

            // IL2CPP needs the NDK path; take it from the installed module
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

            AssetDatabase.SaveAssets();
            Debug.Log("[BuildScript] package name: " + packageName +
                      (versionCode.HasValue ? ", version code " + versionCode.Value : ""));
        }

        /// <summary>
        /// Verifies the active input handling by reading the serialized project settings, which is
        /// where the value actually lives in Unity 6.
        ///
        ///   0 = Input Manager (Old)      1 = Input System (New)      2 = Both
        ///
        /// The Meta XR Interaction SDK drives hand and pinch input through the new input system, so 0
        /// means the SDK is installed but its input backends are disabled and nothing responds to a
        /// pinch - a build that succeeds and does nothing on the headset.
        /// </summary>
        static bool VerifyActiveInputHandling()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(),
                "ProjectSettings/ProjectSettings.asset");
            try
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning("[BuildScript] could not read ProjectSettings.asset to verify the " +
                                     "active input handling.");
                    return true;
                }
                var match = System.Text.RegularExpressions.Regex.Match(
                    File.ReadAllText(path), @"(?m)^[ \t]*activeInputHandler:\s*(\d+)");
                if (!match.Success)
                {
                    Debug.LogWarning("[BuildScript] activeInputHandler was not found in " +
                                     "ProjectSettings.asset. It must be 2 (Both) for the Meta XR SDK.");
                    return true;
                }
                int value = int.Parse(match.Groups[1].Value);
                if (value == 2)
                {
                    Debug.Log("[BuildScript] active input handling = Both");
                    return true;
                }
                Debug.LogError("[BuildScript] active input handling is " + value +
                               " but must be 2 (Both) for the Meta XR SDK. Run " +
                               "scripts/patch_player_settings.ps1 and build again.");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BuildScript] could not verify the active input handling: " + e.Message);
                return true;
            }
        }

        /// <summary>
        /// Compares the Matcher component's serialised values in Main.unity against the tuned constants.
        ///
        /// This exists because the defect it catches is invisible everywhere else: the inspector shows
        /// one number, the built app runs another, and no log line mentions either. The scene is opened
        /// only to be read and is closed again before the build.
        /// </summary>
        static bool VerifySceneTuning()
        {
            if (!File.Exists(ScenesPath))
            {
                Debug.LogError("[BuildScript] " + ScenesPath + " does not exist. Build the scene first.");
                return false;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenesPath, OpenSceneMode.Single);
            var matchers = UnityEngine.Object.FindObjectsByType<Matcher>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool ok = matchers.Length == 1;
            if (!ok)
            {
                Debug.LogError("[BuildScript] expected exactly one Matcher in " + ScenesPath +
                               ", found " + matchers.Length + ".");
            }
            else
            {
                var m = matchers[0];
                ok &= Check("lockedDeg", m.lockedDeg, Matcher.TunedLockedDeg);
                ok &= Check("closeDeg", m.closeDeg, Matcher.TunedCloseDeg);
                ok &= Check("bandReleaseDeg", m.bandReleaseDeg, Matcher.TunedBandReleaseDeg);
                ok &= Check("holdSeconds", m.holdSeconds, Matcher.TunedHoldSeconds);
                ok &= Check("orientationSlackDeg", m.orientationSlackDeg, Matcher.TunedOrientationSlackDeg);
                ok &= Check("orientationWeightK", m.orientationWeightK, Matcher.TunedOrientationWeightK);
                ok &= Check("nearMissMaxDeg", m.nearMissMaxDeg, Matcher.TunedNearMissMaxDeg);

                if (m.weights == null)
                {
                    Debug.LogError("[BuildScript] the scene's Matcher has no JointWeights asset; every " +
                                   "distance would use the fallback table.");
                    ok = false;
                }
                else
                {
                    Debug.Log("[BuildScript] scene tuning matches: locked " + m.lockedDeg.ToString("0.#") +
                              " / close " + m.closeDeg.ToString("0.#") +
                              " / slack " + m.orientationSlackDeg.ToString("0.#") +
                              " / weights " + m.weights.Summary());
                }
            }

            EditorSceneManager.CloseScene(scene, true);
            return ok;
        }

        static bool Check(string field, float actual, float expected)
        {
            if (Mathf.Abs(actual - expected) <= 0.0005f) return true;
            Debug.LogError("[BuildScript] scene " + field + " is " + actual.ToString("0.###") +
                           " but the tuned value is " + expected.ToString("0.###") +
                           " - the scene predates the tuning. Rebuild it with SceneBuilder.");
            return false;
        }

        /// <summary>
        /// Reads the bundle version code, then increments the stored value.
        ///
        /// Meta rejects an upload when a build with the same bundle version code already exists on a
        /// release channel, and the error does not name the channel or the conflicting build.
        /// </summary>
        static int TakeVersionCode()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), VersionCodeFile);
            int code = BundleVersionCode;
            try
            {
                if (File.Exists(path))
                {
                    int parsed;
                    if (int.TryParse(File.ReadAllText(path).Trim(), out parsed) && parsed > 0)
                        code = parsed;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BuildScript] could not read the version code file: " + e.Message);
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, (code + 1).ToString());
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BuildScript] could not bump the version code file: " + e.Message);
            }

            return code;
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
