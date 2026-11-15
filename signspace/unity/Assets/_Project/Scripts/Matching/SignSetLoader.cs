using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>
    /// Loads every handshape in StreamingAssets/Signs into the Matcher, plus set-meta.json.
    /// One bad file must never take down the app: it is reported and skipped.
    /// </summary>
    public class SignSetLoader : MonoBehaviour
    {
        public Matcher matcher;
        [Tooltip("Folder inside StreamingAssets")]
        public string folder = "Signs";
        [Tooltip("Load the first shape as the target on start")]
        public bool autoSelectFirst = true;

        public SetMeta Meta { get; private set; }
        public List<string> LoadErrors { get; } = new List<string>();

        void Awake()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, folder);
            if (!Directory.Exists(dir))
            {
                Debug.LogError($"[SignSetLoader] folder not found: {dir}");
                return;
            }

            var metaPath = Path.Combine(dir, "set-meta.json");
            if (File.Exists(metaPath)) Meta = JsonUtility.FromJson<SetMeta>(File.ReadAllText(metaPath));

            var poses = new List<HandPose>();
            foreach (var file in Directory.GetFiles(dir, "*.json"))
            {
                if (Path.GetFileName(file) == "set-meta.json") continue;
                if (Path.GetFileName(file).StartsWith("EXAMPLE")) continue;

                var pose = HandPose.FromJson(File.ReadAllText(file), out string err);
                if (pose == null) { LoadErrors.Add($"{Path.GetFileName(file)}: {err}"); continue; }
                poses.Add(pose);
            }

            foreach (var e in LoadErrors) Debug.LogError("[SignSetLoader] " + e);

            if (matcher != null)
            {
                matcher.shapes = poses;
                if (autoSelectFirst && poses.Count > 0) matcher.SetTarget(poses[0].Id);
            }

            Debug.Log($"[SignSetLoader] {poses.Count} shapes loaded, {LoadErrors.Count} failed. " +
                      $"Authorship: {Meta?.authorship ?? "unknown"}");
        }
    }
}
