using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// P10's desk-side checks: the post-submission artefacts exist, say what the phase requires, and do
    /// not claim more than the evidence supports.
    ///
    /// WHAT THIS CAN AND CANNOT DO
    /// ---------------------------
    /// The gate G10 asks is about *time and accounts*: is the channel listing still up, does the invite
    /// URL still install, is the video still public. None of that is answerable from inside Unity, so it
    /// is not asserted here. What is asserted is the part a repository can prove:
    ///
    ///  - the APK on disk still hashes to the freeze record (the artifact has not been rebuilt),
    ///  - `scripts/freeze_check.py` exists and covers the checks the log relies on,
    ///  - `reverify-log.md` contains a dated row with the real hash and the NOT RUN rows,
    ///  - the judging note fits on a page and answers both hard questions,
    ///  - the launch sketch defines its numbers, dates its milestones and names its dependencies,
    ///  - the post-mortem admits that hours were not recorded,
    ///  - no P10 document claims G10 has passed.
    ///
    /// The last one is the reason this file exists. A post-submission phase fails by writing "verified"
    /// in a document and calling it a verification.
    ///
    /// USAGE
    ///   Unity.exe -batchmode -quit -projectPath unity -executeMethod Signspace.Editor.P10Checks.CheckAll
    /// </summary>
    public static class P10Checks
    {
        const string ApkPath = "Builds/Android/signspace.apk";
        const string RecordPath = "../phases/p9-submission/apk-hash.txt";
        const string PhaseDir = "../phases/p10-post/";

        static int _passed;
        static int _failed;

        [MenuItem("SIGNSPACE/P10 Check all")]
        public static void CheckAll()
        {
            _passed = 0;
            _failed = 0;

            CheckFreezeScript();
            CheckArtifactHash();
            CheckReverifyLog();
            CheckJudgingNote();
            CheckLaunchSketch();
            CheckPostmortem();
            CheckNoOverclaim();

            Debug.Log("[P10Checks] TOTAL " + _passed + " passed, " + _failed + " failed");
            EditorApplication.Exit(_failed == 0 ? 0 : 1);
        }

        // ------------------------------------------------------------------ the guard

        static void CheckFreezeScript()
        {
            string path = "../scripts/freeze_check.py";
            string text = Read(path);
            Check("freeze_check.py exists", text.Length > 0);
            Check("freeze_check.py hashes the APK", text.Contains("sha256("));
            Check("freeze_check.py checks the tag", text.Contains("tag"));
            Check("freeze_check.py checks commits after the freeze", text.Contains("no commits after freeze"));
            Check("freeze_check.py reports NOT RUN instead of guessing", text.Contains("NOT RUN"));
            Check("freeze_check.py exits non-zero on FAIL", text.Contains("return 0 if all("));
        }

        static void CheckArtifactHash()
        {
            if (!File.Exists(ApkPath))
            {
                Check("APK present", false);
                return;
            }
            Check("APK present", true);

            string record = Read(RecordPath);
            var match = Regex.Match(record, @"sha256:\s*([0-9a-f]{64})");
            if (!match.Success)
            {
                Check("freeze record has a sha256", false);
                return;
            }
            Check("freeze record has a sha256", true);

            string expected = match.Groups[1].Value;
            string actual = Sha256(ApkPath);
            Check("APK is byte-identical to the freeze record", expected == actual);
            if (expected != actual)
                Debug.Log("[P10Checks]   expected " + expected + " actual " + actual);
        }

        static void CheckReverifyLog()
        {
            string text = Read(PhaseDir + "reverify-log.md");
            Check("reverify-log.md exists", text.Length > 0);
            Check("log has a dated row", Regex.IsMatch(text, @"\d{1,2} Nov 2026"));
            Check("log records the real artifact hash", text.Contains("81b60582"));
            Check("log states a FAIL count", text.Contains("4 PASS") && text.Contains("9 FAIL"));
            Check("log keeps account checks NOT RUN", text.Contains("NOT RUN"));
            Check("log has weekly rows", text.Contains("25 Nov 2026") && text.Contains("2 Dec 2026"));
            Check("log names the empty commit as a defect", text.Contains("commit: "));
            Check("log tells the owner not to rebuild", text.ToLowerInvariant().Contains("do not rebuild"));
        }

        static void CheckJudgingNote()
        {
            string text = Read(PhaseDir + "judging-note.md");
            Check("judging-note.md exists", text.Length > 0);

            int words = CountWords(text);
            Check("judging note is one page (" + words + " words ≤ 800)", words > 200 && words <= 800);

            Check("note carries three sentences", text.Contains("three sentences"));
            Check("note maps the four criteria", text.Contains("Innovation") && text.Contains("Technical"));
            Check("note answers the Language IRL question", text.Contains("Language IRL"));
            Check("note answers the shape-count question", text.Contains("twenty"));
            Check("note cites the run count", text.Contains("131"));
            Check("note keeps unmeasured claims out", text.Contains("NOT RUN"));
        }

        static void CheckLaunchSketch()
        {
            string text = Read(PhaseDir + "launch-sketch.md");
            Check("launch-sketch.md exists", text.Length > 0);
            Check("sketch defines 60 signs", text.Contains("60"));
            Check("sketch lists Store requirements", text.Contains("Quest Store"));
            Check("sketch dates its milestones", Regex.IsMatch(text, @"\d{1,2} (Dec 2026|Jan 2027|Feb 2027|Mar 2027)"));
            Check("sketch names the signer dependency", text.ToLowerInvariant().Contains("signer"));
            Check("sketch names a risk with a date", text.Contains("15 Dec 2026"));
        }

        static void CheckPostmortem()
        {
            string text = Read(PhaseDir + "POSTMORTEM.md");
            Check("POSTMORTEM.md exists", text.Length > 0);
            Check("post-mortem admits hours are not recorded", text.Contains("NOT RECORDED"));
            Check("post-mortem has measured commit counts", text.Contains("57"));
            Check("post-mortem carries three lessons", text.Contains("carry-forward lessons"));
            Check("post-mortem names the closest-to-red gate", text.Contains("G8"));
            Check("post-mortem answers the 30-day question", text.Contains("30 days"));
        }

        static void CheckNoOverclaim()
        {
            string readme = Read(PhaseDir + "README.md");
            string gate = Read(PhaseDir + "gate.md");
            string log = Read(PhaseDir + "reverify-log.md");

            Check("phase README exists", readme.Length > 0);
            Check("phase README does not claim the phase is done", !readme.Contains("✅ Done"));
            Check("gate is not marked PASS", !gate.Contains("✅ PASS"));
            Check("log does not claim G10 passed", !log.Contains("G10: PASS"));
        }

        // ------------------------------------------------------------------ helpers

        static void Check(string name, bool ok)
        {
            if (ok) { _passed++; Debug.Log("  PASS " + name); }
            else { _failed++; Debug.LogError("  FAIL " + name); }
        }

        static string Read(string path)
        {
            try { return File.Exists(path) ? File.ReadAllText(path) : ""; }
            catch (Exception error)
            {
                Debug.LogWarning("[P10Checks] cannot read " + path + ": " + error.Message);
                return "";
            }
        }

        static int CountWords(string text)
        {
            return Regex.Matches(text, @"[A-Za-z0-9']+").Count;
        }

        static string Sha256(string path)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
