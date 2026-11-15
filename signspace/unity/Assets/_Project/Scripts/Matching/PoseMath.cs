using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>All the arithmetic the matcher needs. Pure functions, no state.</summary>
    public static class PoseMath
    {
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
        /// Weighted mean joint angle, plus an independent palm-orientation term.
        /// Returns the combined distance in DEGREES — never a 0..1 score.
        /// A percentage invites optimising a number; degrees describe a shape.
        /// </summary>
        public static float Distance(
            Quaternion[] target, Quaternion[] current, float[] weights,
            int palmIndex, float palmSlackDeg, float palmWeight)
        {
            if (target == null || current == null || weights == null) return float.PositiveInfinity;
            int n = Mathf.Min(Mathf.Min(target.Length, current.Length), weights.Length);

            float sum = 0f, wsum = 0f;
            int lastFinger = n;
            for (int i = 0; i < n; i++)
            {
                if (i == palmIndex) { lastFinger = i; continue; } // palm handled separately
                sum += weights[i] * JointAngle(target[i], current[i]);
                wsum += weights[i];
            }
            if (wsum <= 0f) return float.PositiveInfinity;

            float d = sum / wsum;

            if (palmIndex >= 0 && palmIndex < n && palmWeight > 0f)
            {
                float palm = Mathf.Max(0f, JointAngle(target[palmIndex], current[palmIndex]) - palmSlackDeg);
                d = (d * wsum + palmWeight * palm) / (wsum + palmWeight);
            }
            return d;
        }

        /// <summary>Linear band 0..2 from a distance. 0 = warming, 1 = close, 2 = locked.</summary>
        public static int Band(float distanceDeg, float lockedDeg, float closeDeg)
        {
            if (distanceDeg <= lockedDeg) return 2;
            if (distanceDeg <= closeDeg) return 1;
            return 0;
        }
    }
}
