using System;
using System.Collections.Generic;
using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>One joint's share of the distance. Named, never indexed — see the class comment.</summary>
    [Serializable]
    public class JointWeightEntry
    {
        public string name;
        public float weight;
    }

    /// <summary>
    /// The per-joint weights, as an asset so they can be tuned WITHOUT RECOMPILING.
    ///
    /// P2.2 asks for exactly this, and there is a second reason: the weights are a *product judgement*
    /// (which joints a human reads as "the shape"), not a constant. A judgement that lives in code is
    /// changed by whoever is willing to edit code; a judgement that lives in an asset is changed by
    /// whoever is wearing the headset.
    ///
    /// NAMED ENTRIES, NOT A FLOAT[26]. P1.4's lesson applies here more than anywhere: the pose format
    /// was trusted to be in canonical order and nothing enforced it. `weight[13]` is meaningless to a
    /// person tuning a matcher at 11 p.m.; `MiddleIntermediate = 1.0` is not. Entries are matched to
    /// the canonical set BY NAME at load, so reordering the asset in the inspector cannot silently
    /// move a weight onto the wrong joint.
    ///
    /// THE DEFAULT TABLE, and why each number is what it is:
    ///
    ///   Wrist, Palm                      0.40  posture, not shape. Palm orientation is already a
    ///                                           separate gate in Matcher (orientationSlackDeg); giving
    ///                                           it a full weight here would count it twice
    ///   *Metacarpal (CMC)                0.40  the base of the finger barely moves for these six
    ///                                           letters, and it is noisy on-device
    ///   *Proximal                        0.70  carries some shape, mostly carries reach
    ///   *Intermediate                    1.00  where curl actually happens: the highest-signal joint
    ///   *Distal                          1.00  the fingertip's direction; what the eye reads
    ///   *Tip                             0.00  excluded. Carries no shape information for these six
    ///                                           letters and does carry tracking noise — the P1.5/v2
    ///                                           finding, kept rather than re-litigated
    ///
    /// Those weights are not guesses either: measured offline against the six shipped shapes, the
    /// metric built from them separates the nearest intended pair (U-V, 26.0 deg) from tracking noise
    /// (p95 6.7 deg at 2 deg/joint) at a score of 3.89, against 3.06 for the previous max-of-chains
    /// metric and 1.43 for the flat weighted mean (which is the trap this task names: it collapses
    /// U-V from 26.0 to 5.3 deg). Same noise model, same shapes, measured in
    /// scripts/tune_match.py --v3-only. See phases/p2-core-loop/matcher-tuning.md section 8.
    /// </summary>
    [CreateAssetMenu(fileName = "Weights", menuName = "Signspace/Joint Weights", order = 10)]
    public class JointWeights : ScriptableObject
    {
        [Tooltip("Bumped only when the meaning of the numbers changes, so a stale asset is visible.")]
        public int version = 1;

        [Tooltip("One entry per canonical joint, matched BY NAME. Order does not matter.")]
        public JointWeightEntry[] joints = new JointWeightEntry[0];

        // resolved once, keyed by canonical index; never serialised
        [NonSerialized] float[] _byIndex;
        [NonSerialized] bool _built;

        public const float WristWeight = 0.40f;
        public const float MetacarpalWeight = 0.40f;
        public const float ProximalWeight = 0.70f;
        public const float IntermediateWeight = 1.00f;
        public const float DistalWeight = 1.00f;
        public const float TipWeight = 0.00f;

        /// <summary>The weight for a canonical joint index. Missing entries read as 0, loudly.</summary>
        public float Weight(int canonicalIndex)
        {
            EnsureBuilt();
            if (canonicalIndex < 0 || canonicalIndex >= _byIndex.Length) return 0f;
            return _byIndex[canonicalIndex];
        }

        /// <summary>Total of the weights actually in play. The denominator of the mean — see
        /// PoseMath.WeightedFingerMean for why dividing by the joint COUNT is the trap.</summary>
        public float WeightOf(int[] canonicalIndices)
        {
            float sum = 0f;
            for (int i = 0; i < canonicalIndices.Length; i++) sum += Weight(canonicalIndices[i]);
            return sum;
        }

        void EnsureBuilt()
        {
            if (_built) return;
            _built = true;
            _byIndex = new float[JointNames.Canonical.Length];

            if (joints == null || joints.Length == 0)
            {
                Debug.LogError("[JointWeights] asset has no entries; every weight is 0 and every " +
                               "distance would be NaN. Falling back to the documented defaults.");
                ApplyDefaultsTo(_byIndex);
                return;
            }

            var seen = new bool[JointNames.Canonical.Length];
            for (int i = 0; i < joints.Length; i++)
            {
                var e = joints[i];
                if (e == null || string.IsNullOrEmpty(e.name)) continue;
                string canonical = JointNames.Canonicalize(e.name);
                int index = canonical == null ? -1 : JointNames.IndexOf(canonical);
                if (index < 0)
                {
                    Debug.LogWarning("[JointWeights] '" + e.name + "' is not a canonical joint; ignored");
                    continue;
                }
                if (seen[index])
                {
                    Debug.LogWarning("[JointWeights] '" + canonical + "' appears twice; the last wins");
                }
                _byIndex[index] = Mathf.Max(0f, e.weight);
                seen[index] = true;
            }

            for (int i = 0; i < seen.Length; i++)
            {
                if (!seen[i] && _byIndex[i] == 0f)
                    Debug.LogWarning("[JointWeights] no entry for '" + JointNames.Canonical[i] +
                                     "'; it contributes nothing to the distance");
            }
        }

        static void ApplyDefaultsTo(float[] target)
        {
            for (int i = 0; i < target.Length; i++)
                target[i] = DefaultWeightFor(JointNames.Canonical[i]);
        }

        /// <summary>The documented default for one joint, by name. The single place the numbers live.</summary>
        public static float DefaultWeightFor(string jointName)
        {
            if (jointName == "Wrist" || jointName == "Palm") return WristWeight;
            if (jointName.EndsWith("Metacarpal", StringComparison.Ordinal)) return MetacarpalWeight;
            if (jointName.EndsWith("Tip", StringComparison.Ordinal)) return TipWeight;
            if (jointName.EndsWith("Distal", StringComparison.Ordinal)) return DistalWeight;
            if (jointName.EndsWith("Intermediate", StringComparison.Ordinal)) return IntermediateWeight;
            if (jointName.EndsWith("Proximal", StringComparison.Ordinal)) return ProximalWeight;
            return ProximalWeight;
        }

        /// <summary>
        /// An in-memory asset with the default table. Used when nothing is assigned in the inspector,
        /// so a scene that forgot to wire the asset still matches instead of dividing by zero.
        /// </summary>
        public static JointWeights Default()
        {
            var asset = CreateInstance<JointWeights>();
            asset.name = "Weights (runtime default)";
            asset.version = 1;
            var entries = new JointWeightEntry[JointNames.Canonical.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i] = new JointWeightEntry
                {
                    name = JointNames.Canonical[i],
                    weight = DefaultWeightFor(JointNames.Canonical[i])
                };
            }
            asset.joints = entries;
            asset._built = false;
            return asset;
        }

        /// <summary>Empty when the asset is usable; otherwise the reason, for the overlay and the log.</summary>
        public string Validate()
        {
            if (joints == null || joints.Length == 0) return "no entries";
            int positives = 0;
            var names = new List<string>();
            for (int i = 0; i < joints.Length; i++)
            {
                if (joints[i] == null || string.IsNullOrEmpty(joints[i].name)) continue;
                if (joints[i].weight > 0f) positives++;
                names.Add(joints[i].name);
            }
            if (positives == 0) return "every weight is zero";
            if (positives < 5) return "fewer than five weighted joints; a finger count that small " +
                                     "cannot tell five fingers apart";
            return "";
        }

        /// <summary>One-line summary for the overlay: how many joints carry weight, and the total.</summary>
        public string Summary()
        {
            EnsureBuilt();
            float total = 0f;
            int positives = 0;
            for (int i = 0; i < _byIndex.Length; i++)
            {
                total += _byIndex[i];
                if (_byIndex[i] > 0f) positives++;
            }
            return positives + " joints, total weight " + total.ToString("0.00");
        }
    }
}
