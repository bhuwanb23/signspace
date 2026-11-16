using Signspace.Matching;
using UnityEditor;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// Writes and assigns the per-joint weight asset (P2.2).
    ///
    /// WHY A BUILDER WHEN THE ASSET IS ALREADY COMMITTED
    /// The committed `Weights.asset` is a Unity YAML file that was written by hand, because this
    /// repository is edited without a Unity editor in the loop. Hand-written YAML is reproducible but
    /// not trustworthy on its own: if the class gains a field or the script GUID changes, the asset
    /// quietly stops deserialising and every weight becomes zero. So there are two paths to the same
    /// object - the committed file, and this menu item that regenerates it from the defaults in
    /// `JointWeights.DefaultWeightFor()` - and the second one is the way to check the first.
    ///
    /// It also means retuning is a supported operation rather than a text edit: change weights in the
    /// inspector, no recompile, then run `scripts/tune_match.py` (which reads this same asset) to see
    /// what the change did to separability before wearing the headset.
    /// </summary>
    public static class WeightsBuilder
    {
        const string AssetFolder = "Assets/_Project/Matching";
        const string AssetPath = AssetFolder + "/Weights.asset";

        [MenuItem("SIGNSPACE/P2.2 Write weights asset from defaults")]
        public static void WriteAsset()
        {
            if (!AssetDatabase.IsValidFolder(AssetFolder))
                AssetDatabase.CreateFolder("Assets/_Project", "Matching");

            var defaults = JointWeights.Default();
            var existing = AssetDatabase.LoadAssetAtPath<JointWeights>(AssetPath);

            if (existing == null)
            {
                AssetDatabase.CreateAsset(defaults, AssetPath);
                Debug.Log("[WeightsBuilder] created " + AssetPath);
            }
            else
            {
                // Overwrite in place so the GUID (and therefore every reference to it) survives.
                existing.version = defaults.version;
                existing.joints = defaults.joints;
                EditorUtility.SetDirty(existing);
                Debug.Log("[WeightsBuilder] rewrote " + AssetPath + " from the documented defaults");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("SIGNSPACE/P2.2 Assign weights to matcher")]
        public static void AssignToMatcher()
        {
            var matcher = Object.FindFirstObjectByType<Matcher>();
            if (matcher == null)
            {
                Debug.LogError("[WeightsBuilder] no Matcher in the scene. Run SIGNSPACE/Build Main Scene first.");
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<JointWeights>(AssetPath);
            if (asset == null)
            {
                Debug.LogWarning("[WeightsBuilder] no " + AssetPath + "; using the runtime defaults. " +
                                 "Run SIGNSPACE/P2.2 Write weights asset from defaults to create it.");
                matcher.weights = null;
                return;
            }

            matcher.weights = asset;
            EditorUtility.SetDirty(matcher);
            string problem = asset.Validate();
            Debug.Log("[WeightsBuilder] matcher.weights <- " + AssetPath +
                      (string.IsNullOrEmpty(problem) ? "  (" + asset.Summary() + ")"
                                                     : "  PROBLEM: " + problem));
        }

        [MenuItem("SIGNSPACE/P2.2 Report weights")]
        public static void Report()
        {
            var asset = AssetDatabase.LoadAssetAtPath<JointWeights>(AssetPath);
            if (asset == null) { Debug.LogError("[WeightsBuilder] no asset at " + AssetPath); return; }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Per-joint weights (" + AssetPath + ", version " + asset.version + ")");
            for (int i = 0; i < JointNames.Canonical.Length; i++)
            {
                sb.Append("  ").Append(JointNames.Canonical[i].PadRight(22))
                  .Append(asset.Weight(i).ToString("0.00")).AppendLine();
            }
            sb.AppendLine("  " + asset.Summary());
            Debug.Log("[WeightsBuilder]\n" + sb);
        }
    }
}
