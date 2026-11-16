using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>
    /// The left-hand mirror, derived rather than guessed (P2.7).
    ///
    /// A reflection in the plane x = 0 is M = diag(-1, 1, 1). A rotation R becomes M R M, which is
    /// still a rotation - that is the whole trick, because it means a left pose is a right pose
    /// conjugated by a reflection and not a new kind of object. Conjugating a unit quaternion by
    /// that reflection gives
    ///
    ///     (x, y, z, w)  ->  (x, -y, -z, w)        bones:  (dx, dy, dz) -> (-dx, dy, dz)
    ///
    /// which is the same rotation as (-x, y, z, -w), because q and -q are the same rotation. Written
    /// here as "negate x and w" because that is the form the P2.7 brief used and the two agree; the
    /// version that negates x ONLY is a rotation, not a reflection, and it is not this function.
    ///
    /// WHY THE LITERAL READING IS DANGEROUS, measured: negating x and keeping w puts bone
    /// directions up to 89.6 deg from the true mirror and moves the drawn thumb tip by up to
    /// 44 mm - while every finger still curls the way it should, so it does not look broken. A
    /// mirror that is wrong by a reflection looks like a hand. Only the derived form is exact.
    ///
    /// This is the utility, not the policy: `HandSupport` decides whether it runs, and today it does
    /// not (`LeftHandSupported` is false). It is here because the enable path is otherwise a piece of
    /// folklore someone has to re-derive under deadline.
    /// </summary>
    public static class HandMirror
    {
        /// <summary>Mirror one parent-relative rotation across the hand's sagittal plane.</summary>
        public static Quaternion Mirror(Quaternion q)
        {
            return new Quaternion(-q.x, q.y, q.z, -q.w);
        }

        /// <summary>Mirror every joint of a pose, in place.</summary>
        public static void MirrorInPlace(Quaternion[] rotations)
        {
            if (rotations == null) return;
            for (int i = 0; i < rotations.Length; i++)
                rotations[i] = Mirror(rotations[i]);
        }

        /// <summary>Mirror a pose into another array. Sizes are matched, not trusted.</summary>
        public static void MirrorInto(Quaternion[] source, Quaternion[] destination)
        {
            if (source == null || destination == null) return;
            int n = Mathf.Min(source.Length, destination.Length);
            for (int i = 0; i < n; i++) destination[i] = Mirror(source[i]);
        }

        /// <summary>
        /// True when a rotation is its own mirror - i.e. a joint whose axis lies in the sagittal
        /// plane, such as pure flexion. Used by the overlay to show how much of a pose the mirror
        /// actually touches; a handshape made only of these would be handedness-blind.
        /// </summary>
        public static bool IsMirrorInvariant(Quaternion q)
        {
            return Mathf.Abs(q.y) < 1e-4f && Mathf.Abs(q.z) < 1e-4f;
        }
    }
}
