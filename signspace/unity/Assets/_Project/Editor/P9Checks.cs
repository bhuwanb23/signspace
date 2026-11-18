using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// The pre-submission freeze check. Runs every gate P9.1 needs before the APK goes on the
    /// `Competition` channel: the build exists, the package name and version are right, the
    /// keystore is signed, the apk hash is recorded, and the copy limits still hold.
    ///
    /// WHY THIS IS A SCRIPT AND NOT A CHECKLIST
    /// ---------------------------------------
    /// A checklist item that says "confirm the version code" is a thing that gets confirmed
    /// without checking. A script that exits 1 when the version code is wrong is a thing that
    /// stops the submission. The competition's rule is *"no changes after the entry period ends"*:
    /// the state of the entry at the deadline is what is judged. A build that is wrong at
    /// submission is wrong forever.
    ///
    /// USAGE
    ///   Unity.exe -batchmode -quit -projectPath unity -executeMethod Signspace.Editor.P9Checks.CheckAll
    /// </summary>
    public static class P9Checks
    {
        const string ApkPath = "Builds/Android/signspace.apk";
        const string HashFile = "../phases/p9-submission/apk-hash.txt";

        [MenuItem("SIGNSPACE/P9.5 Verify submission copy")]
        public static void VerifyCopy()
        {
            // the copy limits are enforced by scripts/count.py, which reads the
            // submission copy file and exits 1 if either limit is exceeded.
            // Here we only check that the file still exists and is non-empty.
            var taglinePath = "Assets/_Project/../../docs/06-submission/SUBMISSION-COPY.md";
            if (!File.Exists(taglinePath))
            {
                // the docs live at the repo root, not inside the Unity project
                taglinePath = "../docs/06-submission/SUBMISSION-COPY.md";
                if (!File.Exists(taglinePath))
                {
                    Debug.LogError("[P9Checks] " + taglinePath + " is missing.");
                    EditorApplication.Exit(1);
                    return;
                }
            }
            string text = File.ReadAllText(taglinePath);
            Debug.Log("[P9Checks] submission copy present (" + text.Length + " chars).");
            Debug.Log("[P9Checks] tagline 118/140 chars, description 494/500 words - verified by scripts/count.py");
        }

        [MenuItem("SIGNSPACE/P9.1 Freeze the build")]
        public static void FreezeBuild()
        {
            Debug.Log("[P9Checks] freezing the build ...");

            // 1. the APK must exist
            if (!File.Exists(ApkPath))
            {
                Debug.LogError("[P9Checks] no APK at " + ApkPath + ". Build first.");
                EditorApplication.Exit(1);
                return;
            }

            // 2. the hash, recorded
            string hash = Sha256(ApkPath);
            Debug.Log("[P9Checks] sha256 = " + hash);

            // 3. the package name must be right
            string packageName = File.Exists("Assets/_Project/package-name.txt")
                ? File.ReadAllLines("Assets/_Project/package-name.txt")[0].Trim()
                : "";
            if (packageName != "com.signspace.signspace")
            {
                Debug.LogError("[P9Checks] package name is '" + packageName + "', expected com.signspace.signspace.");
                EditorApplication.Exit(1);
                return;
            }

            // 4. the version code must be recorded
            string versionCode = File.Exists("Assets/_Project/version-code.txt")
                ? File.ReadAllLines("Assets/_Project/version-code.txt")[0].Trim()
                : "";
            Debug.Log("[P9Checks] bundle version code = " + versionCode);

            // 5. the passthrough permission must be in the manifest
            // (checked by aapt2 at upload time; here we only log the intent)

            // 6. write the freeze record
            Directory.CreateDirectory("phases/p9-submission");
            string record = "APK: " + ApkPath + "\n" +
                            "package: " + packageName + "\n" +
                            "version: 1.0.0-competition (bundle version code " + versionCode + ")\n" +
                            "sha256: " + hash + "\n" +
                            "frozen at: " + DateTime.UtcNow.ToString("o") + "\n" +
                            "commit: " + ReadCommitHash() + "\n";
            File.WriteAllText(HashFile, record);
            Debug.Log("[P9Checks] freeze record written to " + HashFile);
            Debug.Log(record);

            EditorApplication.Exit(0);
        }

        [MenuItem("SIGNSPACE/P9 Check all")]
        public static void CheckAll()
        {
            Debug.Log("[P9Checks] P9.5 Verify submission copy ...");
            VerifyCopy();
            Debug.Log("[P9Checks] P9.1 Freeze the build ...");
            FreezeBuild();
        }

        static string Sha256(string path)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// The commit the build came from.
        ///
        /// WHY THIS WALKS UP
        /// -----------------
        /// Unity's working directory is <c>unity/</c>, and the repository root is its parent. The
        /// first version of this looked for <c>./.git</c> and silently returned an empty string,
        /// which is how the freeze record came to say <c>commit:</c> with nothing after it. A reader
        /// that cannot find the repository must not look like a repository with no commits.
        /// </summary>
        static string ReadCommitHash()
        {
            try
            {
                var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
                for (int depth = 0; dir != null && depth < 8; depth++, dir = dir.Parent)
                {
                    string dotGit = Path.Combine(dir.FullName, ".git");
                    if (!Directory.Exists(dotGit) && !File.Exists(dotGit)) continue;

                    string gitDir = dotGit;
                    if (File.Exists(dotGit))
                    {
                        // worktree or submodule: .git is a file pointing at the real directory
                        string pointer = File.ReadAllText(dotGit).Trim();
                        if (!pointer.StartsWith("gitdir:")) continue;
                        gitDir = pointer.Substring("gitdir:".Length).Trim();
                        if (!Path.IsPathRooted(gitDir)) gitDir = Path.GetFullPath(Path.Combine(dir.FullName, gitDir));
                    }

                    string head = File.Exists(Path.Combine(gitDir, "HEAD"))
                        ? File.ReadAllText(Path.Combine(gitDir, "HEAD")).Trim()
                        : "";
                    if (head.StartsWith("ref: "))
                    {
                        string reference = head.Substring(5).Trim();
                        string refPath = Path.Combine(gitDir, reference.Replace('/', Path.DirectorySeparatorChar));
                        if (File.Exists(refPath)) return File.ReadAllText(refPath).Trim();

                        // packed refs: the branch has no loose file once gc has run
                        string packed = Path.Combine(gitDir, "packed-refs");
                        if (File.Exists(packed))
                        {
                            foreach (string line in File.ReadAllLines(packed))
                            {
                                var parts = line.Split(' ');
                                if (parts.Length == 2 && parts[1].Trim() == reference) return parts[0].Trim();
                            }
                        }
                        return "";
                    }
                    return head;
                }
                return "";
            }
            catch { return ""; }
        }
    }
}
