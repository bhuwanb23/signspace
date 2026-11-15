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
    /// The two hand layouts Meta ships disagree about almost everything:
    ///
    ///   OpenXR (default since v71) : PALM(0) WRIST(1) THUMB_METACARPAL(2)
    ///                                ... LITTLE_TIP(25)  -- 26 joints, no forearm
    ///   legacy OVR                 : WristRoot(0) ForearmStub(1) Thumb0(2) ...
    ///                                PinkyTip(23)  -- 24 joints, no palm
    ///
    /// The wrist/palm order is not simply swapped, either: the legacy layout has
    /// no palm at all, has no metacarpal for the index, middle or ring finger, and
    /// numbers those fingers one joint lower. Aligning by NAME means one pose file
    /// works on both, and it means the pose files stay readable and diffable,
    /// which is what POSE-FORMAT.md promised in the first place.
    ///
    /// THREE LAYOUTS, NOT TWO (corrected 15 Nov 2026 from Meta's own table)
    /// ------------------------------------------------------------------
    /// Since SDK v71 the DEFAULT skeleton is the OpenXR one (26 joints, palm,
    /// z-forward/y-up), and the legacy OVR skeleton (24 joints, no palm, forearm
    /// stub, x-forward) is the opt-in. OVRSkeleton.BoneId carries BOTH sets, so a
    /// device can legally report "XRHand_IndexProximal" or "Hand_Index1" for what
    /// this table must treat as the same joint:
    ///
    ///   OpenXR : palm, thumb metacarpal/proximal/distal/tip, and for every other
    ///            finger metacarpal + proximal + intermediate + distal + tip
    ///   OVR    : NO palm, NO index/middle/ring metacarpal, a forearm stub, and a
    ///            thumb TRAPEZIUM (Thumb0) in front of the metacarpal
    ///
    /// The consequence is that the same finger is numbered one joint apart in the
    /// two layouts: OVR Hand_Index1 is the PROXIMAL phalange, while OpenXR
    /// XRHand_IndexMetacarpal is the metacarpal. An earlier version of this file
    /// mapped Index1 -> IndexMetacarpal, which would have rotated every finger one
    /// joint off its real axis - a systematic distortion that looks like bad
    /// tracking and is not.
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
            // Thumb: OVR's Thumb0 is the trapezium (no canonical home) and Thumb1-3 are
            // metacarpal/proximal/distal, so the numbers start one higher than the fingers.
            Add("ThumbMetacarpal", "ThumbBase", "Thumb1");
            Add("ThumbProximal", "Thumb2");
            Add("ThumbDistal", "Thumb3");
            Add("ThumbTip", "Thumb4");
            // Index/Middle/Ring: OVR has NO metacarpal, so Hand_Index1 is the proximal
            // phalange. Mapping it to IndexMetacarpal (as this table used to) shifts every
            // finger by one joint.
            Add("IndexMetacarpal", "IndexBase");
            Add("IndexProximal", "Index1");
            Add("IndexIntermediate", "Index2");
            Add("IndexDistal", "Index3");
            Add("IndexTip", "Index4", "Index5");
            Add("MiddleMetacarpal", "MiddleBase");
            Add("MiddleProximal", "Middle1");
            Add("MiddleIntermediate", "Middle2");
            Add("MiddleDistal", "Middle3");
            Add("MiddleTip", "Middle4", "Middle5");
            Add("RingMetacarpal", "RingBase");
            Add("RingProximal", "Ring1");
            Add("RingIntermediate", "Ring2");
            Add("RingDistal", "Ring3");
            Add("RingTip", "Ring4", "Ring5");
            // The pinky is the one finger OVR DOES give a metacarpal (Pinky0), so its numbering
            // matches the other fingers rather than the thumb.
            Add("LittleMetacarpal", "PinkyBase", "Pinky0");
            Add("LittleProximal", "PinkyProximal", "Pinky1");
            Add("LittleIntermediate", "PinkyIntermediate", "Pinky2");
            Add("LittleDistal", "PinkyDistal", "Pinky3");
            Add("LittleTip", "PinkyTip", "Pinky4", "Pinky5");

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
            foreach (var prefix in new[] { "xrhand_", "xrhand ", "hand_", "hand ", "finger_", "joint_" })
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
