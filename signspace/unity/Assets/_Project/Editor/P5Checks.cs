using System;
using System.IO;
using System.Reflection;
using Signspace.Matching;
using Signspace.Spatial;
using Signspace.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// P5.5 as things a machine can check without a headset.
    ///
    /// What these prove: the credits object exists, carries a pinch bead, reads its wording from
    /// `set-meta.json` rather than from a string in a prefab, always carries the fixed sentence, rides
    /// the shelf, and opens itself inside the first minute.
    ///
    /// What they do NOT prove: that the signer approved the wording, or that the panel reads well in a
    /// headset. Those stay open in the task file.
    /// </summary>
    public static class P5Checks
    {
        const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        const string MetaPath = "Assets/StreamingAssets/Signs/set-meta.json";

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("P5 check: " + message);
        }

        [MenuItem("SIGNSPACE/P5.7 Check blind-spot copy")]
        public static void CheckBlindSpotCopy()
        {
            try
            {
                string json = File.ReadAllText(Path.Combine(Application.dataPath, "StreamingAssets/trackability.json"));
                var data = Trackability.Parse(json);
                Require(data != null, "trackability.json does not parse");

                string forward = Trackability.ForwardLine(data);
                Require(!string.IsNullOrEmpty(forward), "there is no forward-looking line");
                Require(!System.Text.RegularExpressions.Regex.IsMatch(forward, @"\b20\d\d\b"),
                        "the forward-looking line promises a date; a date in an honesty panel is a claim");
                Require(forward.Contains("depth"),
                        "the forward line does not say what would change the exclusion");

                // One sentence per excluded shape, no apology, and the doc must carry the same sentence.
                var ids = Trackability.ExcludedIds(data);
                Require(ids.Count > 0, "no excluded shapes in the file");
                // Batchmode's working directory is unity/, not the repository root: resolve the doc
                // from Application.dataPath so the check runs the same way from the menu and headless.
                string docPath = Path.GetFullPath(Path.Combine(Application.dataPath,
                    "../../docs/03-content/TRACKABILITY.md"));
                Require(File.Exists(docPath), "TRACKABILITY.md not found at " + docPath);
                string doc = File.ReadAllText(docPath);
                foreach (var id in ids)
                {
                    string reason = Trackability.ReasonFor(data, id);
                    Require(!string.IsNullOrEmpty(reason), "no reason for " + id);
                    Require(!reason.Contains("sorry") && !reason.Contains("unfortunately"),
                            "the reason for " + id + " apologises");
                    Require(doc.Contains(reason), "TRACKABILITY.md does not carry the shipped reason for " + id);
                }
                Require(doc.Contains(forward), "TRACKABILITY.md does not carry the forward-looking line");

                Debug.Log("[P5.7] blind-spot copy checks passed: " + ids.Count +
                          " excluded shapes, one factual sentence each, forward line with no date, " +
                          "doc and panel agree. The signer's rewrite is still owed.");
            }
            catch (Exception e)
            {
                Debug.LogError("[P5.7] blind-spot copy checks FAILED: " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        /// <summary>Drives the panel's real text composition, the same path the runtime uses.</summary>
        static void Compose(CreditsPanel panel, SetMeta meta)
        {
            var prop = typeof(CreditsPanel).GetProperty("Meta",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var build = typeof(CreditsPanel).GetMethod("Build",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Require(prop != null && build != null, "CreditsPanel no longer exposes the path this check drives");

            prop.SetValue(panel, meta, null);
            build.Invoke(panel, null);
        }

        [MenuItem("SIGNSPACE/P5.5 Check credits")]
        public static void CheckCredits()
        {
            try
            {
                AssetDatabase.Refresh();

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreditsBuilder.PrefabPath);
                Require(prefab != null, CreditsBuilder.PrefabPath + " is missing");

                var panel = prefab.GetComponent<CreditsPanel>();
                Require(panel != null, "the prefab has no CreditsPanel");
                Require(panel.content != null, "the panel has no content object");
                Require(!panel.content.activeSelf, "the panel is open at rest; it must open itself once, not ship open");
                Require(panel.header != null && panel.body != null, "the panel is missing its text blocks");
                Require(panel.showOnFirstLaunchAfterSeconds <= 60f,
                        "the first-launch showing is outside the first minute");
                Require(!string.IsNullOrEmpty(panel.metaFile) && panel.metaFile.Contains("set-meta"),
                        "the panel does not read its wording from set-meta.json");

                var handle = prefab.GetComponentInChildren<CreditsHandle>(true);
                Require(handle != null, "the prefab has no pinch bead (CreditsHandle)");
                Require(handle.panel == panel, "the bead is not wired to the panel");
                Require(handle.transform.parent == prefab.transform,
                        "the bead is inside the panel content, so it would vanish when the panel closes");

                // The wording is data. A prefab that hard-codes a credit line is exactly the failure
                // ADR-007 exists to prevent: the product would keep claiming a name after it changed.
                var metaFile = new FileInfo(MetaPath);
                Require(metaFile.Exists, MetaPath + " is missing");
                var meta = JsonUtility.FromJson<SetMeta>(File.ReadAllText(MetaPath));
                Require(meta != null, MetaPath + " does not parse");
                Require(!string.IsNullOrEmpty(meta.creditLine), "set-meta.json has no creditLine");
                Require(meta.creditLine.Contains("not a replacement"),
                        "the credit line is missing the fixed practice sentence");
                Require(!string.IsNullOrEmpty(CreditsPanel.FixedLine),
                        "the panel has no fixed practice sentence of its own");

                // State honesty: a name may only appear alongside a signer-verified state.
                if (!string.IsNullOrEmpty(meta.signerName))
                    Require(meta.authorship == "signer-verified",
                            "a signer name is present while authorship is '" + meta.authorship + "'");

                // ---- the text the panel actually composes ----------------------------------
                // Reading the file is not the same as proving the panel renders it. The first version of
                // this check only did the former, so a panel that ignored `Meta` would still have passed.
                var probe = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                try
                {
                    var live = probe.GetComponent<CreditsPanel>();
                    Require(live != null && live.body != null, "the instantiated panel has no body text");

                    // A: a signed build names the signer, the city and the date.
                    Compose(live, new SetMeta
                    {
                        authorship = "signer-verified",
                        signerName = "Test Signer",
                        signerCity = "Test City",
                        verifiedOn = "2026-11-20",
                        creditLine = "Handshapes verified by Test Signer. " + CreditsPanel.FixedLine
                    });
                    Require(live.body.text.Contains("Test Signer"), "the panel does not show the signer's name");
                    Require(live.body.text.Contains("Test City"), "the panel does not show the signer's city");
                    Require(live.body.text.Contains("2026-11-20"), "the panel does not show the capture date");
                    Require(live.body.text.Contains(CreditsPanel.FixedLine), "the panel dropped the fixed sentence");

                    // B: no signer names itself as unsigned instead of leaving a blank.
                    Compose(live, meta);
                    Require(live.body.text.Contains("No signer has reviewed"),
                            "the panel does not state the unsigned state plainly");
                    Require(live.body.text.Contains(CreditsPanel.FixedLine), "the unsigned panel lost the fixed sentence");

                    // C: a credit line that omits the fixed sentence still gets it.
                    Compose(live, new SetMeta { creditLine = "Handshapes authored from references." });
                    Require(live.body.text.Contains(CreditsPanel.FixedLine),
                            "the panel does not append the fixed sentence when the data omits it");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(probe);
                }

                // The scene instance rides the shelf.
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var instance = UnityEngine.Object.FindFirstObjectByType<CreditsPanel>();
                Require(instance != null, "no CreditsPanel in " + ScenePath);
                Require(instance.follow != null, "the scene credits object does not follow the shelf");
                Require(instance.follow.GetComponent<ShelfAnchor>() != null,
                        "the object it follows is not the shelf");

                Debug.Log("[P5.5] credits checks passed: prefab + bead on the shelf, wording read from " +
                          "set-meta.json (state '" + meta.authorship + "', signer '" +
                          (string.IsNullOrEmpty(meta.signerName) ? "(none)" : meta.signerName) + "'), " +
                          "fixed sentence present, auto-show at " + instance.showOnFirstLaunchAfterSeconds +
                          " s. Signer approval of the wording is still owed.");
            }
            catch (Exception e)
            {
                Debug.LogError("[P5.5] credits checks FAILED: " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }
    }
}
