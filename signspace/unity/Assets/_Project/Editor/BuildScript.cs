using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
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
        const string Version = "0.9.0";

        /// <summary>
        /// The bundle version code lives in a file so every upload to a release
        /// channel automatically gets a HIGHER one.
        ///
        /// Not paranoia: Meta rejects an upload when a build with the same bundle
        /// version code already exists on one of your release channels, and the
        /// error names neither the channel nor the conflicting build. Losing an
        /// hour to that on Day 4 is the whole point of automating the build.
        /// </summary>
        const string VersionCodeFile = "Assets/_Project/version-code.txt";

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

            var info = new System.IO.FileInfo(apkPath);
            Debug.Log($"[BuildScript] result   : {summary.result}");
            Debug.Log($"[BuildScript] apk size : {info.Length / (1024 * 1024)} MB   ({apkPath})");
            Debug.Log($"[BuildScript] duration : {summary.totalTime}");
            Debug.Log($"[BuildScript] version  : {Version} (bundle version code {versionCode})");

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
        /// Sets the active input handling, by reflection.
        ///
        /// The property is called `activeInputHandling` (not `activeInputHandler` --
        /// the old name was removed), and its enum type lives outside the UnityEditor
        /// namespace, so naming it directly does not compile. Reflection is used for
        /// the same reason PerfRigBuilder uses it for `enableFrameTimingStats`: an
        /// API that moves between editor versions should cost a log line, not a build.
        /// </summary>
        /// <summary>
        /// Verifies the active input handling by reading the serialized project
        /// settings, which is where the value actually lives in Unity 6.
        ///
        ///   0 = Input Manager (Old)      1 = Input System (New)      2 = Both
        ///
        /// The Meta XR Interaction SDK drives hand and pinch input through the new
        /// input system, so 0 means the SDK is installed but its input backends
        /// are disabled and nothing responds to a pinch.
        /// </summary>
        static void VerifyActiveInputHandling()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(),
                "ProjectSettings/ProjectSettings.asset");
            try
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning("[BuildScript] could not read ProjectSettings.asset to verify the " +
                                     "active input handling.");
                    return;
                }
                var match = System.Text.RegularExpressions.Regex.Match(
                    File.ReadAllText(path), @"(?m)^[ \t]*activeInputHandler:\s*(\d+)");
                if (!match.Success)
                {
                    Debug.LogWarning("[BuildScript] activeInputHandler was not found in " +
                                     "ProjectSettings.asset. It must be 2 (Both) for the Meta XR SDK.");
                    return;
                }
                int value = int.Parse(match.Groups[1].Value);
                if (value == 2)
                {
                    Debug.Log("[BuildScript] active input handling = Both");
                }
                else
                {
                    Debug.LogError("[BuildScript] active input handling is " + value +
                                   " but must be 2 (Both) for the Meta XR SDK. Run " +
                                   "scripts/patch_player_settings.ps1 and build again.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BuildScript] could not verify the active input handling: " + e.Message);
            }
        }

        /// <summary>
        /// Reads the bundle version code, then increments the stored value.
        ///
        /// Meta rejects an upload when a build with the same bundle version code
        /// already exists on one of your release channels, and the error does not
        /// name the channel or the conflicting build. Losing an hour to that on
        /// Day 4 is the whole point of automating the build.
        /// </summary>
        static int TakeVersionCode()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), VersionCodeFile);
            int code = 1;
            try
            {
                if (File.Exists(path))
                {
                    string text = File.ReadAllText(path).Trim();
                    int parsed;
                    if (int.TryParse(text, out parsed) && parsed > 0) code = parsed;
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
        /// Applies the Android Player settings. Idempotent.
        /// </summary>
        /// <param name="versionCode">Bundle version code to apply, or null to leave it alone.</param>
        public static void ApplySettings(int? versionCode = null)
        {
            string packageName = ReadPackageName();

            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = Version;
            if (versionCode.HasValue) PlayerSettings.Android.bundleVersionCode = versionCode.Value;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, packageName);

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;

            // Quest is ARM64 only
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);

            // linear colour space is correct for Quest
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // ISDK needs the new input system. Unity 6 exposes no public API for
            // the setting any more, so it is patched in the serialized project
            // settings (scripts/patch_player_settings.ps1) and VERIFIED here by
            // reading the same file back. An earlier version of this code
            // reflected over PlayerSettings looking for a property that Unity 6
            // does not have, and warned on every build about a setting that was
            // in fact correct.
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
            Debug.Log("[BuildScript] package name: " + packageName);
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
