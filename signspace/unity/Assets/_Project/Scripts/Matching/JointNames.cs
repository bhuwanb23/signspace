using System;
using System.Collections.Generic;

namespace Signspace.Matching
{
    /// <summary>
    /// Maps the hand joint names a device reports onto the canonical joint names
    /// the pose files use.
    ///
    /// WHY BY NAME AND NOT BY INDEX
    /// ---------------------------
    /// The two hand layouts Meta ships disagree about the order:
    ///
    ///   legacy Oculus : WristRoot(0) Palm(1) ThumbBase(2) ... PinkyTip(25)
    ///   OpenXR        : PALM(0) WRIST(1) THUMB_METACARPAL(2) ... LITTLE_TIP(25)
    ///
    /// Everything from index 2 up is in the same order; only the wrist and the
    /// palm swap, and "Pinky" is called "Little". Aligning by NAME means one
    /// pose file works on both, and it means the pose files stay readable and
    /// diffable, which is what POSE-FORMAT.md promised in the first place.
    ///
    /// The alias table mirrors scripts/hand_model.py. Keep them in step: the
    /// Python side authors the poses, this side reads them.
    /// </summary>
    public static class JointNames
    {
        /// <summary>Canonical joint names, in the order the pose files store them.</summary>
        public static readonly string[] Canonical =
        {
            "Wrist",
            "Palm",
            "ThumbMetacarpal", "ThumbProximal", "ThumbDistal", "ThumbTip",
            "IndexMetacarpal", "IndexProximal", "IndexIntermediate", "IndexDistal", "IndexTip",
            "MiddleMetacarpal", "MiddleProximal", "MiddleIntermediate", "MiddleDistal", "MiddleTip",
            "RingMetacarpal", "RingProximal", "RingIntermediate", "RingDistal", "RingTip",
            "LittleMetacarpal", "LittleProximal", "LittleIntermediate", "LittleDistal", "LittleTip"
        };

        static readonly Dictionary<string, string> Aliases = BuildAliases();

        static Dictionary<string, string> BuildAliases()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            void Add(string canonical, params string[] spellings)
            {
                map[canonical.ToLowerInvariant()] = canonical;
                foreach (var s in spellings) map[s.ToLowerInvariant()] = canonical;
            }

            Add("Wrist", "WristRoot");
            Add("Palm");
            Add("ThumbMetacarpal", "ThumbBase", "Thumb1");
            Add("ThumbProximal", "Thumb2");
            Add("ThumbDistal", "Thumb3");
            Add("ThumbTip", "Thumb4");
            Add("IndexMetacarpal", "IndexBase", "Index1");
            Add("IndexProximal", "Index2");
            Add("IndexIntermediate", "Index3");
            Add("IndexDistal", "Index4");
            Add("IndexTip", "Index5");
            Add("MiddleMetacarpal", "MiddleBase", "Middle1");
            Add("MiddleProximal", "Middle2");
            Add("MiddleIntermediate", "Middle3");
            Add("MiddleDistal", "Middle4");
            Add("MiddleTip", "Middle5");
            Add("RingMetacarpal", "RingBase", "Ring1");
            Add("RingProximal", "Ring2");
            Add("RingIntermediate", "Ring3");
            Add("RingDistal", "Ring4");
            Add("RingTip", "Ring5");
            Add("LittleMetacarpal", "PinkyBase", "Pinky1", "Little1");
            Add("LittleProximal", "PinkyProximal", "Pinky2", "Little2");
            Add("LittleIntermediate", "PinkyIntermediate", "Pinky3", "Little3");
            Add("LittleDistal", "PinkyDistal", "Pinky4", "Little4");
            Add("LittleTip", "PinkyTip", "Pinky5", "Little5");

            return map;
        }

        /// <summary>
        /// Turns a device joint name into a canonical joint name, or null.
        ///
        /// Handles the Hand_ prefix, the _01/_02 bone suffixes, and any
        /// separators, so 'Hand_IndexProximal', 'index proximal' and
        /// 'Index_Proximal' all land on the same joint.
        /// </summary>
        public static string Canonicalize(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;

            string cleaned = raw.Trim().ToLowerInvariant();
            foreach (var prefix in new[] { "hand_", "hand ", "finger_", "joint_" })
            {
                if (cleaned.StartsWith(prefix, StringComparison.Ordinal))
                {
                    cleaned = cleaned.Substring(prefix.Length);
                }
            }
            while (cleaned.Length > 2 && char.IsDigit(cleaned[cleaned.Length - 1]) &&
                   (cleaned[cleaned.Length - 2] == '_' || cleaned[cleaned.Length - 2] == '-' ||
                    cleaned[cleaned.Length - 2] == ' '))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 2);
            }
            cleaned = cleaned.Replace("_", "").Replace("-", "").Replace(" ", "");

            string canonical;
            return Aliases.TryGetValue(cleaned, out canonical) ? canonical : null;
        }

        /// <summary>Index of a canonical joint name, or -1.</summary>
        public static int IndexOf(string canonical)
        {
            for (int i = 0; i < Canonical.Length; i++)
            {
                if (Canonical[i] == canonical) return i;
            }
            return -1;
        }
    }
}
