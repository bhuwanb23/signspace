using System;
using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>
    /// All the arithmetic the matcher needs. Pure functions, no state.
    ///
    /// THE METRIC, AND WHY IT IS THIS SHAPE
    /// ------------------------------------
    /// v1 measured a weighted mean of the per-joint angle differences across the
    /// whole hand. That is the obvious metric and it does not work.
    ///
    /// U and V differ in exactly one joint -- how far the index is abducted.
    /// Spread across 25 other joints, a 26 degree spread contributed about
    /// 3 degrees to the score. With a near-miss threshold of 18 degrees, the
    /// U/V near-miss could never fire. The near-miss is the product's entire
    /// identity, so this was not a tuning problem, it was a broken instrument.
    ///
    /// v2 measures each FINGER separately, as the accumulated rotation of that
    /// finger's chain against the target's, and combines the five fingers with
    /// a max: a handshape is wrong if any finger is wrong. Spread and curl both
    /// then register at full magnitude, because a finger's whole configuration
    /// is one number instead of a share of an average.
    ///
    /// Measured offline against the six shipped shapes (scripts/tune_match.py):
    ///     nearest intended near-miss pair   26.0 deg  (U-V, index spread)
    ///     nearest accidental pair           100.2 deg
    ///     tracking noise, p50 / p95           5.5 / 8.5 deg  at 2 deg per joint
    ///     lock threshold                     12 deg  -- above the noise, below the pair
    ///
    /// The terminal (tip) joint is excluded from the chain: it carries no shape
    /// information for these six letters and it carries tracking noise.
    ///
    /// See phases/p2-core-loop/matcher-tuning.md for the full matrices.
    ///
    /// ENGINE-AGNOSTIC ON PURPOSE: this file references no Meta type, so an SDK
    /// upgrade cannot break the product's core logic. See ARCHITECTURE.md.
    /// </summary>
    public static class PoseMath
    {
        // ------------------------------------------------------------------
        // Canonical joint indices. These MUST match scripts/hand_model.py.
        // The canonical order is the OpenXR XR_EXT_hand_tracking layout, which
        // is what ISDK v207's OpenXR hand skeleton reports.
        //
        //   0 Wrist        1 Palm
        //   2 ThumbMetacarpal 3 ThumbProximal 4 ThumbDistal 5 ThumbTip
        //   6 IndexMetacarpal 7 IndexProximal 8 IndexIntermediate 9 IndexDistal 10 IndexTip
        //  11 Middle...       16 Ring...      21 Little...
        // ------------------------------------------------------------------
        public const int WristIndex = 0;
        public const int PalmIndex = 1;

        /// <summary>
        /// The joint that carries each finger's accumulated chain rotation.
        ///
        /// A joint's stored local rotation already contains every ancestor's
        /// rotation, so the LAST joint of a chain holds the whole chain:
        ///
        ///     stored[distal] = d_base * d_prox * d_inter * d_distal  = chain rotation
        ///
        /// No parent map is needed, which means no assumption about how the rig
        /// parents the fingers, and it costs one quaternion comparison per
        /// finger instead of one per joint.
        ///
        /// The terminal (tip) joint is deliberately not used: it carries no shape
        /// information for these six letters and it carries tracking noise.
        /// </summary>
        public static readonly int[] FingerChainEnd =
        {
            4,   // thumb  : ThumbDistal
            9,   // index  : IndexDistal
            14,  // middle : MiddleDistal
            19,  // ring   : RingDistal
            24   // little : LittleDistal
        };

        /// <summary>Human-readable names, for the debug overlay.</summary>
        public static readonly string[] FingerNames = { "thumb", "index", "middle", "ring", "little" };

        /// <summary>
        /// Angle in degrees between two local joint rotations, 0..180.
        /// 2*acos(|w|) handles the q == -q ambiguity of quaternions.
        /// </summary>
        public static float JointAngle(Quaternion target, Quaternion current)
        {
            Quaternion delta = Quaternion.Inverse(target) * current;
            float w = Mathf.Clamp(Mathf.Abs(delta.w), -1f, 1f);
            return 2f * Mathf.Acos(w) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Angle in degrees between two finger chains.
        ///
        /// The chain's accumulated rotation is carried by its last joint, so
        /// this is one quaternion comparison. See FingerChainEnd for why.
        /// </summary>
        public static float ChainAngle(Quaternion[] target, Quaternion[] current, int chainEnd)
        {
            if (target == null || current == null) return float.PositiveInfinity;
            if (chainEnd >= target.Length || chainEnd >= current.Length) return float.PositiveInfinity;
            return JointAngle(target[chainEnd], current[chainEnd]);
        }

        /// <summary>
        /// The distance between two hand poses, in DEGREES -- never a percentage.
        /// A percentage invites optimising a number; degrees describe a shape.
        ///
        /// The score is the worst finger, plus a separate wrist term that
        /// carries palm orientation: rotating the wrist the wrong way has to
        /// break the match even if every finger is perfect.
        /// </summary>
        /// <param name="fingerAngles">Per-finger chain angles, for the debug overlay.
        /// Pass a reused buffer: this runs every frame and must not allocate.</param>
        /// <param name="wristAngle">Wrist angle, the palm-orientation term.</param>
        public static float Distance(Quaternion[] target, Quaternion[] current,
                                     float[] fingerAngles, out float wristAngle)
        {
            wristAngle = 0f;

            if (target == null || current == null) return float.PositiveInfinity;
            if (target.Length != current.Length) return float.PositiveInfinity;

            float worst = 0f;
            for (int i = 0; i < FingerChainEnd.Length; i++)
            {
                float a = ChainAngle(target, current, FingerChainEnd[i]);
                if (fingerAngles != null && i < fingerAngles.Length) fingerAngles[i] = a;
                if (a > worst) worst = a;
            }

            if (WristIndex < target.Length && WristIndex < current.Length)
                wristAngle = JointAngle(target[WristIndex], current[WristIndex]);

            return worst;
        }

        /// <summary>Convenience overload that drops the diagnostics.</summary>
        public static float Distance(Quaternion[] target, Quaternion[] current)
        {
            return Distance(target, current, null, out _);
        }

        /// <summary>
        /// Linear band 0..2 from a distance. 0 = warming, 1 = close, 2 = locked.
        /// </summary>
        public static int Band(float distanceDeg, float lockedDeg, float closeDeg)
        {
            if (distanceDeg <= lockedDeg) return 2;
            if (distanceDeg <= closeDeg) return 1;
            return 0;
        }
    }
}
