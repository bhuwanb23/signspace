using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// Dumps the hand rig's rest-pose local rotations to JSON.
    ///
    /// WHY
    /// ---
    /// The pose files are authored assuming the rig's rest pose has IDENTITY
    /// local rotations -- a straight-finger rig with aligned bones, which is
    /// how a hand rig is normally built. If the shipped rig turns out to have
    /// non-identity rest rotations, every stored value is offset by a constant
    /// and nothing will match.
    ///
    /// Rather than guess, run this once, then regenerate the poses with:
    ///     python3 scripts/make_pose.py --neutral unity/rest-pose.json
    ///
    /// The runtime never sees any of this: the rest pose is folded into the
    /// pose files at authoring time.
    ///
    /// USAGE
    ///   Unity.exe -batchmode -quit -projectPath unity \
    ///             -executeMethod Signspace.Editor.RestPoseDump.Dump
    /// </summary>
    public static class RestPoseDump
    {
        const string OutputPath = "rest-pose.json";

        [MenuItem("SIGNSPACE/Dump hand rig rest pose")]
        public static void Dump()
        {
            var joints = FindHandJoints();
            if (joints == null || joints.Count == 0)
            {
                Debug.LogError("[RestPoseDump] no hand rig joints were found. " +
                               "Open a scene with the Meta XR rig, or check that Meta XR All-in-One is imported.");
                EditorApplication.Exit(1);
                return;
            }

            var sb = new StringBuilder();
            sb.Append("{\n");
            var lines = new List<string>();
            foreach (var kvp in joints)
            {
                Quaternion q = kvp.Value.localRotation;
                lines.Add($"  \"{kvp.Key}\": [{q.x:F6}, {q.y:F6}, {q.z:F6}, {q.w:F6}]");
            }
            sb.Append(string.Join(",\n", lines));
            sb.Append("\n}\n");

            File.WriteAllText(OutputPath, sb.ToString());
            Debug.Log($"[RestPoseDump] wrote {OutputPath} with {joints.Count} joints");
            foreach (var l in lines) Debug.Log("  " + l);
        }

        /// <summary>
        /// Finds the hand rig's bone transforms. Looks for transforms whose name
        /// canonicalises to one of the canonical joint names, anywhere in the
        /// loaded scene or in the package assets.
        /// </summary>
        static Dictionary<string, Transform> FindHandJoints()
        {
            var found = new Dictionary<string, Transform>();

            // prefer whatever is in the open scene
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                string canonical = Signspace.Matching.JointNames.Canonicalize(t.name);
                if (canonical == null) continue;
                if (t.parent == null) continue;
                if (!found.ContainsKey(canonical)) found[canonical] = t;
            }
            if (found.Count >= Signspace.Matching.JointNames.Canonical.Length) return found;

            // otherwise look through the package assets
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("XR") && !path.Contains("Hand")) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
                {
                    string canonical = Signspace.Matching.JointNames.Canonicalize(t.name);
                    if (canonical == null) continue;
                    if (!found.ContainsKey(canonical)) found[canonical] = t;
                }
                if (found.Count >= Signspace.Matching.JointNames.Canonical.Length) break;
            }
            return found;
        }
    }
}
