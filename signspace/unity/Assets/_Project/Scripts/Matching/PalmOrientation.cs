using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>Where the palm basis came from. Shown on the overlay during tuning.</summary>
    public enum PalmBasisSource { None = 0, PalmFrame = 1, KnucklePlane = 2 }

    /// <summary>
    /// The palm's facing as a separate error term (P2.6).
    ///
    /// WHY NOT AN ANGLE
    /// Several sign pairs differ mostly in which way the palm faces. Fold that into the finger average
    /// and the app accepts a correct-shape/wrong-facing attempt - which, to a sign learner, is a
    /// different word. So it has to be its own measurement, and it must not be three numbers: any Euler
    /// triple reports a large error at certain wrist orientations for a hand that is exactly right, and
    /// the tuning then chases phantom errors that only appear at particular angles. That is the trap in
    /// this task, and the way out is to never decompose into angles at all.
    ///
    /// So the palm is described by a **basis**, built from the hand itself, and compared as a rotation:
    ///
    ///     forward  the palm's distal direction          (the palm frame's +Y: where the hand points)
    ///     normal   out of the back of the hand          (the palm frame's +Z)
    ///     basis    (x = normal x forward, y = normal, z = forward)
    ///
    /// and the error is the angle of the rotation between two such bases, `2*acos|dot|`. Half a turn is
    /// 180 deg, a quarter turn is 90, and there is no preferred axis and no wrapping anywhere.
    ///
    /// THE KNUCKLE PLANE, AND WHY IT IS USUALLY REJECTED
    /// The task asks for "wrist forward plus index-knuckle plane normal". The plane through the
    /// metacarpal heads is a *positional* thing, and the pose format carries no bone lengths - so the
    /// proxy is the cross product of the index and little metacarpal axes. That proxy is only usable
    /// while those axes are wide enough apart for the cross product to be well conditioned
    /// (amplification 1/sin(theta)) and not rotated by a finger curl. Measured on the shipped shapes:
    ///
    ///     B     axes 11 deg apart, normal 0.0 deg from the palm normal - but 5.2x noise amplification
    ///     U/V/W axes 86 deg apart  - the little metacarpal is curled, normal 88-90 deg out
    ///     C/O   axes 2-3 deg apart - near-parallel, 24-31x amplification
    ///
    /// Under 2 deg/joint noise the knuckle plane moves the reported facing by 5.8 deg (B) to 88 deg (U),
    /// against 0.0 deg for the palm frame, whose inputs are the wrist and palm joints only. So the palm
    /// frame is what runs, the knuckle plane is attempted first and rejected with a recorded reason, and
    /// `BasisSource` reports which one a frame used. Nothing here is a guess: `scripts/palm_sim.py`
    /// regenerates every number in that table.
    /// </summary>
    public static class PalmOrientation
    {
        /// <summary>Below this the metacarpal axes are too parallel for a cross product to mean anything.</summary>
        public const float KnuckleMinSeparationDeg = 25f;
        /// <summary>Above this a metacarpal has been rotated to curl a finger, so its axis is not a knuckle axis.</summary>
        public const float KnuckleMaxSeparationDeg = 45f;
        /// <summary>How far the knuckle normal may disagree with the palm's dorsal axis before it is rejected.</summary>
        public const float KnuckleTrustDeg = 12f;

        static readonly int WristIndex = JointNames.IndexOf("Wrist");
        static readonly int PalmIndex = JointNames.IndexOf("Palm");
        static readonly int IndexMetacarpalIndex = JointNames.IndexOf("IndexMetacarpal");
        static readonly int LittleMetacarpalIndex = JointNames.IndexOf("LittleMetacarpal");

        /// <summary>
        /// Build the palm's basis. Returns false (and an identity basis) when the pose is too short or
        /// the rig cannot supply the joints - the caller must then contribute no orientation error
        /// rather than a phantom one.
        /// </summary>
        public static bool TryBasis(Quaternion[] rotations, out Quaternion basis, out PalmBasisSource source,
                                    out string note)
        {
            basis = Quaternion.identity;
            source = PalmBasisSource.None;
            note = "no pose";

            if (rotations == null || rotations.Length <= PalmIndex) return false;

            // Cumulative rotation of the palm: local rotations are parent-relative (POSE-FORMAT 2), so a
            // wrist turn changes one entry - the Wrist joint - and must be composed in here or the palm
            // basis would be blind to it.
            Quaternion palm = rotations[WristIndex] * rotations[PalmIndex];
            Vector3 forward = palm * Vector3.up;      // +Y is "toward the child" in this rig's convention
            Vector3 palmNormal = palm * Vector3.forward;

            Vector3 normal = palmNormal;
            source = PalmBasisSource.PalmFrame;
            note = "palm frame";

            if (TryKnuckleNormal(rotations, palm, palmNormal, out Vector3 knuckle, out string knuckleNote))
            {
                normal = knuckle;
                source = PalmBasisSource.KnucklePlane;
                note = knuckleNote;
            }
            else
            {
                note = knuckleNote;
            }

            basis = BasisFrom(forward, normal);
            return true;
        }

        static bool TryKnuckleNormal(Quaternion[] rotations, Quaternion palm, Vector3 palmNormal,
                                     out Vector3 normal, out string note)
        {
            normal = Vector3.zero;
            note = "knuckle plane unavailable";
            if (rotations.Length <= LittleMetacarpalIndex) return false;

            Vector3 indexAxis = (palm * rotations[IndexMetacarpalIndex]) * Vector3.up;
            Vector3 littleAxis = (palm * rotations[LittleMetacarpalIndex]) * Vector3.up;

            float separation = Vector3.Angle(indexAxis, littleAxis);
            if (separation < KnuckleMinSeparationDeg)
            {
                note = $"metacarpal axes {separation:0.0} deg apart (too parallel for a cross product)";
                return false;
            }
            if (separation > KnuckleMaxSeparationDeg)
            {
                note = $"metacarpal axes {separation:0.0} deg apart (a metacarpal is curled)";
                return false;
            }

            Vector3 candidate = Vector3.Cross(indexAxis, littleAxis).normalized;
            // The cross product's sign is an artefact of which metacarpal fans toward which side, not a
            // fact about the hand. Without this flip a perfectly good plane reads 180 deg wrong.
            if (Vector3.Dot(candidate, palmNormal) < 0f) candidate = -candidate;

            float disagreement = Vector3.Angle(candidate, palmNormal);
            if (disagreement > KnuckleTrustDeg)
            {
                note = $"knuckle normal {disagreement:0.0} deg off the palm normal";
                return false;
            }

            normal = candidate;
            note = $"knuckle plane, axes {separation:0.0} deg apart";
            return true;
        }

        /// <summary>
        /// Rotation whose +Z is `forward` and whose +Y is the component of `normal` perpendicular to it.
        /// The frame is built explicitly rather than through LookRotation so that the C# and the Python
        /// mirror (`scripts/palm_sim.py`) construct the identical matrix, and a basis disagreement can be
        /// blamed on the data rather than on two different conventions.
        /// </summary>
        static Quaternion BasisFrom(Vector3 forward, Vector3 normal)
        {
            Vector3 z = forward.normalized;
            Vector3 y = (normal - z * Vector3.Dot(normal, z)).normalized;
            if (y.sqrMagnitude < 1e-6f) return Quaternion.identity;
            Vector3 x = Vector3.Cross(y, z);
            return BuildFrame(x, y, z);
        }

        /// <summary>Orthonormal frame -> rotation, computed directly so the C# and the Python mirror agree.</summary>
        static Quaternion BuildFrame(Vector3 x, Vector3 y, Vector3 z)
        {
            var m = new Matrix4x4();
            m.SetColumn(0, new Vector4(x.x, x.y, x.z, 0f));
            m.SetColumn(1, new Vector4(y.x, y.y, y.z, 0f));
            m.SetColumn(2, new Vector4(z.x, z.y, z.z, 0f));
            m.SetColumn(3, new Vector4(0f, 0f, 0f, 1f));
            return m.rotation;
        }

        /// <summary>
        /// `O`: the angle between two palms' facings, in degrees. 0 when either basis cannot be built -
        /// a missing joint must never manufacture an orientation error.
        /// </summary>
        public static float ErrorDegrees(Quaternion[] target, Quaternion[] current)
        {
            if (!TryBasis(target, out var a, out _, out _)) return 0f;
            if (!TryBasis(current, out var b, out _, out _)) return 0f;
            return PoseMath.JointAngle(a, b);
        }

        /// <summary>
        /// Swing (tilt: flexion, extension, deviation) and twist (roll: pronation, supination) of the
        /// palm difference. Diagnostics only - the shipped comparison is the single angle above.
        ///
        /// This is the decomposition that is safe to *report*: swing-twist splits about the palm's own
        /// forward axis, which is a property of the hand, rather than about whatever axis an Euler triple
        /// happens to apply first.
        /// </summary>
        public static void Components(Quaternion[] target, Quaternion[] current,
                                      out float swingDeg, out float twistDeg)
        {
            swingDeg = 0f;
            twistDeg = 0f;
            if (!TryBasis(target, out var a, out _, out _)) return;
            if (!TryBasis(current, out var b, out _, out _)) return;

            Quaternion q = Quaternion.Inverse(a) * b;
            Vector3 axis = q * Vector3.forward;                      // the basis' +Z is the palm's forward
            float twist = 2f * Mathf.Acos(Mathf.Clamp(Mathf.Abs(axis.z), -1f, 1f)) * Mathf.Rad2Deg;
            Quaternion twistQ = Quaternion.AngleAxis(twist, Vector3.forward);
            Quaternion swingQ = q * Quaternion.Inverse(twistQ);

            twistDeg = twist;
            swingDeg = PoseMath.JointAngle(Quaternion.identity, swingQ);
        }
    }
}
