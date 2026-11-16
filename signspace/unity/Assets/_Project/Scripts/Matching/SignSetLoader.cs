using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Signspace.Matching
{
    /// <summary>
    /// Loads every handshape in StreamingAssets/Signs into the Matcher, plus set-meta.json.
    /// One bad file must never take down the app: it is reported and skipped.
    ///
    /// WHY THIS IS A COROUTINE AND WHY IT USES UnityWebRequest
    /// --------------------------------------------------------
    /// On Android, StreamingAssets live INSIDE the apk, which is a zip archive.
    /// File.ReadAllText and Directory.GetFiles against that path always fail:
    /// the loader would report "folder not found", load zero shapes, and the
    /// app would sit there matching nothing -- the exact silent failure the
    /// docs call the worst available outcome.
    ///
    /// UnityWebRequest reads StreamingAssets correctly on every platform:
    /// file:// on desktop, jar:file:// on Android. One code path, no branches.
    ///
    /// The file list comes from index.json, because a zip archive cannot be
    /// listed at runtime. scripts/make_pose.py writes it alongside the shapes.
    /// </summary>
    public class SignSetLoader : MonoBehaviour
    {
        public Matcher matcher;
        [Tooltip("Folder inside StreamingAssets")]
        public string folder = "Signs";
        [Tooltip("Load the first shape as the target on start")]
        public bool autoSelectFirst = true;

        /// <summary>Raised once loading finishes, successfully or not.</summary>
        public event Action OnLoaded;

        public SetMeta Meta { get; private set; }
        public List<string> LoadErrors { get; } = new List<string>();
        public bool IsLoaded { get; private set; }

        /// <summary>Shape ids in the order the index lists them.</summary>
        public List<string> ShapeIds { get; } = new List<string>();

        void Awake()
        {
            StartCoroutine(LoadAll());
        }

        IEnumerator LoadAll()
        {
            string dir = folder;

            string indexJson = null;
            yield return ReadText(Combine(dir, "index.json"), result => indexJson = result);
            if (string.IsNullOrEmpty(indexJson))
            {
                Debug.LogError($"[SignSetLoader] could not read index.json in '{dir}'. " +
                               "The sign set will be empty and nothing will ever match.");
                IsLoaded = true;
                OnLoaded?.Invoke();
                yield break;
            }

            List<string> ids = ParseIndex(indexJson);
            if (ids == null || ids.Count == 0)
            {
                Debug.LogError("[SignSetLoader] index.json listed no shapes.");
                IsLoaded = true;
                OnLoaded?.Invoke();
                yield break;
            }

            var poses = new List<HandPose>();
            foreach (string id in ids)
            {
                string json = null;
                yield return ReadText(Combine(dir, id + ".json"), result => json = result);
                if (string.IsNullOrEmpty(json))
                {
                    LoadErrors.Add($"{id}.json: could not be read");
                    continue;
                }
                var pose = HandPose.FromJson(json, out string err);
                if (pose == null)
                {
                    LoadErrors.Add($"{id}.json: {err}");
                    continue;
                }
                poses.Add(pose);
                ShapeIds.Add(id);
            }

            string metaJson = null;
            yield return ReadText(Combine(dir, "set-meta.json"), result => metaJson = result);
            if (!string.IsNullOrEmpty(metaJson))
            {
                try { Meta = JsonUtility.FromJson<SetMeta>(metaJson); }
                catch (Exception e) { Debug.LogError("[SignSetLoader] set-meta.json: " + e.Message); }
            }

            foreach (var e in LoadErrors) Debug.LogError("[SignSetLoader] " + e);

            if (matcher != null)
            {
                matcher.shapes = poses;
                if (autoSelectFirst && poses.Count > 0) matcher.SetTarget(poses[0].Id);
            }

            Debug.Log($"[SignSetLoader] {poses.Count} shapes loaded, {LoadErrors.Count} failed. " +
                      $"Authorship: {Meta?.authorship ?? "unknown"}");

            IsLoaded = true;
            OnLoaded?.Invoke();
        }

        /// <summary>Reads a file out of StreamingAssets, on any platform.</summary>
        IEnumerator ReadText(string relativePath, Action<string> done)
        {
            string url = ToUrl(relativePath);
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    done(request.downloadHandler.text);
                }
                else
                {
                    done(null);
                }
            }
        }

        /// <summary>
        /// Loads one pose by id from any folder under StreamingAssets. The honest mirror needs the
        /// excluded shapes, which live in Probe/ precisely because they are NOT part of the shipped
        /// set, so this is a public entry point instead of a second loader component that would
        /// duplicate the Android-safe read path.
        /// </summary>
        public IEnumerator LoadPose(string dir, string id, Action<HandPose> done)
        {
            string json = null;
            yield return ReadText(Combine(dir, id + ".json"), result => json = result);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[SignSetLoader] no pose at " + Combine(dir, id + ".json"));
                done(null);
                yield break;
            }

            var pose = HandPose.FromJson(json, out string error);
            if (pose == null)
                Debug.LogWarning("[SignSetLoader] " + id + ": " + error);
            done(pose);
        }

        /// <summary>
        /// Turns a StreamingAssets-relative path into a URL UnityWebRequest accepts.
        /// Desktop: C:/.../StreamingAssets/Signs/B.json -> file:///C:/...
        /// Android: jar:file:///data/app/.../assets/Signs/B.json (already a URL).
        /// </summary>
        public static string ToUrl(string relativePath)
        {
            string root = Application.streamingAssetsPath;
            string combined = root.EndsWith("/") ? root + relativePath : root + "/" + relativePath;

            if (combined.StartsWith("jar:", StringComparison.OrdinalIgnoreCase) ||
                combined.StartsWith("http:", StringComparison.OrdinalIgnoreCase) ||
                combined.StartsWith("https:", StringComparison.OrdinalIgnoreCase))
            {
                return combined;
            }
            return "file://" + combined.Replace("\\", "/");
        }

        static string Combine(string dir, string file)
        {
            return string.IsNullOrEmpty(dir) ? file : dir + "/" + file;
        }

        /// <summary>
        /// index.json is a tiny wrapper so we do not have to parse JSON arrays by
        /// hand: Unity's JsonUtility cannot deserialise a bare string[].
        /// </summary>
        [Serializable]
        public class SignIndex
        {
            public string[] shapes;
            public string version;
        }

        static List<string> ParseIndex(string json)
        {
            try
            {
                var index = JsonUtility.FromJson<SignIndex>(json);
                if (index?.shapes == null) return null;
                return new List<string>(index.shapes);
            }
            catch (Exception e)
            {
                Debug.LogError("[SignSetLoader] index.json: " + e.Message);
                return null;
            }
        }
    }
}
