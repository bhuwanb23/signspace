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
        /// The JOINTED chains, in canonical indices, tips excluded (P2.2).
        ///
        /// FingerChainEnd above gives the chain's accumulated rotation in ONE quaternion, which was
        /// metric v2. v3 needs the joints individually because the weights are per joint, so the
        /// membership has to be explicit here. These arrays must match hand_model.FINGERS - the same
        /// list, used by the Python tuner and by the C# matcher, so a difference would show up as a
        /// metric that disagrees with its own tuning report.
        /// </summary>
        public static readonly int[][] FingerChains =
        {
            new[] {  2,  3,  4 },                       // thumb  : CMC, proximal, distal (no intermediate)
            new[] {  6,  7,  8,  9 },                   // index  : metacarpal, proximal, intermediate, distal
            new[] { 11, 12, 13, 14 },                   // middle
            new[] { 16, 17, 18, 19 },                   // ring
            new[] { 21, 22, 23, 24 }                    // little
        };

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
        /// The weighted mean angle WITHIN one finger (P2.2).
        ///
        /// The denominator is the sum of the weights of the joints actually included, never the count
        /// of them. That is the task's second trap, and it is not a pedantic one: dividing by the joint
        /// count while weighting by role would silently scale every distance by (total weight / count),
        /// so changing a weight in the asset would move every distance uniformly and the thresholds
        /// would look wrong for no visible reason. Dividing by the total weight makes the number a
        /// genuine mean, in degrees, that means the same thing whatever weights are in play.
        ///
        /// `worstIndex` receives the joint (canonical index) with the largest weighted contribution -
        /// which is what the overlay needs to name, because "your thumb is 9 deg off" is actionable
        /// and "you are 14 deg off" is not.
        /// </summary>
        public static float WeightedFingerMean(Quaternion[] target, Quaternion[] current, int[] chain,
                                               JointWeights weights, float[] jointAngles,
                                               out int worstIndex)
        {
            worstIndex = -1;
            if (target == null || current == null) return float.PositiveInfinity;
            if (weights == null) return float.PositiveInfinity;

            float sum = 0f, total = 0f, worstContribution = -1f;
            for (int i = 0; i < chain.Length; i++)
            {
                int j = chain[i];
                if (j >= target.Length || j >= current.Length) continue;
                float w = weights.Weight(j);
                float angle = JointAngle(target[j], current[j]);
                if (jointAngles != null && j < jointAngles.Length) jointAngles[j] = angle;

                if (w <= 0f) continue;
                sum += w * angle;
                total += w;
                if (w * angle > worstContribution) { worstContribution = w * angle; worstIndex = j; }
            }
            return total > 0f ? sum / total : 0f;
        }

        /// <summary>
        /// P2.2's distance, in degrees: the worst finger's weighted mean, plus the flat weighted mean
        /// for reporting, plus the palm term.
        ///
        /// WHY THE ACROSS-FINGER COMBINE IS A MAX AND NOT A MEAN, measured rather than argued
        /// (scripts/tune_match.py, six shipped shapes, 2 deg/joint noise):
        ///
        ///     metric                       nearest pair   noise p95   pair/noise
        ///     v2  max of chain angles            26.0         8.5        3.06
        ///     v3  max of weighted finger means   26.0         6.7        3.89   <- this one
        ///     v3  flat weighted mean (the trap)   5.3         3.7        1.43
        ///
        /// The flat mean collapses U-V - which differ only in index abduction - from 26 deg to 5.3 deg,
        /// because the difference is spread across a whole hand. That is the task's first trap
        /// ("averaging all 26 joints equally - the little finger then cancels out the thumb") measured
        /// on our own content. Weighting *within* a finger and taking the max *across* fingers keeps
        /// the separability and improves the noise floor by half, because the within-finger mean
        /// averages noise while the across-finger max preserves a single wrong finger.
        ///
        /// `flatDegrees` is returned for the overlay and the log only. It is the number to show
        /// beside D so a suspiciously small flat value can be recognised as "one finger is very
        /// wrong" rather than "the shape is close".
        /// </summary>
        public static float Distance(JointWeights weights, Quaternion[] target, Quaternion[] current,
                                     float[] fingerAngles, float[] jointAngles,
                                     out float wristAngle, out float flatDegrees)
        {
            wristAngle = 0f;
            flatDegrees = float.PositiveInfinity;

            if (target == null || current == null) return float.PositiveInfinity;
            if (target.Length != current.Length) return float.PositiveInfinity;

            float worstFinger = 0f;
            float flatSum = 0f, flatTotal = 0f;

            // Every joint angle first, including Wrist and Palm. The flat mean is over "every weighted
            // joint in the hand", and the wrist/palm carry 0.4 each - computing only the finger chains
            // and then averaging across all weighted joints would have mixed 26 fresh angles with two
            // stale ones, which is the kind of bug that shows up as a distance that never reaches zero.
            if (jointAngles != null)
            {
                for (int j = 0; j < target.Length && j < jointAngles.Length; j++)
                    jointAngles[j] = JointAngle(target[j], current[j]);
            }

            for (int f = 0; f < FingerChains.Length; f++)
            {
                int ignoredWorst;
                float mean = WeightedFingerMean(target, current, FingerChains[f], weights,
                                                jointAngles, out ignoredWorst);
                if (fingerAngles != null && f < fingerAngles.Length) fingerAngles[f] = mean;
                if (mean > worstFinger) worstFinger = mean;
            }

            // the flat mean, over every weighted joint in the hand (reporting only - see above)
            if (jointAngles != null && weights != null)
            {
                for (int j = 0; j < target.Length; j++)
                {
                    float w = weights.Weight(j);
                    if (w <= 0f) continue;
                    flatSum += w * jointAngles[j];
                    flatTotal += w;
                }
                if (flatTotal > 0f) flatDegrees = flatSum / flatTotal;
            }

            if (WristIndex < target.Length && WristIndex < current.Length)
                wristAngle = JointAngle(target[WristIndex], current[WristIndex]);

            return worstFinger;
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

        /// <summary>
        /// The band test with a **degree-level** release offset, on top of the time hysteresis.
        ///
        /// The edges are not symmetric: entering a band uses the plain threshold, leaving it needs the
        /// hand to be `releaseDeg` past the edge. Without this, a hand parked on an edge crosses it on
        /// half its frames and the display strobes (P2.3 measured 1083 changes per 30 s with no
        /// damping at all, and still 4 on the worst of 300 trials with the 150 ms timer alone).
        ///
        /// Only the exit is widened. Widening the entry too would mean the lock band were bigger than
        /// the number written on it, and the number is what the overlay and the tuning table report.
        /// </summary>
        public static int Band(float distanceDeg, float lockedDeg, float closeDeg,
                               float releaseDeg, int currentBand)
        {
            float lockEdge = lockedDeg + (currentBand == 2 ? releaseDeg : 0f);
            float closeEdge = closeDeg + (currentBand >= 1 ? releaseDeg : 0f);
            if (distanceDeg <= lockEdge) return 2;
            if (distanceDeg <= closeEdge) return 1;
            return 0;
        }
    }
}
