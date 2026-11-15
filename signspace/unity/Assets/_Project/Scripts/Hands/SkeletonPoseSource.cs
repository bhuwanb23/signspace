using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>
    /// Feeds the Matcher from a tracked hand skeleton.
    ///
    /// Deliberately SDK-agnostic: it reads localRotation from an ordered array of joint
    /// Transforms. Either assign those Transforms in the inspector, or call AutoBind()
    /// once at startup with the transform list the SDK gives you.
    ///
    /// Why not reference the SDK type directly: a v208 upgrade that renames an enum
    /// member should cost us zero build time, and this file should still compile.
    /// </summary>
    public class SkeletonPoseSource : MonoBehaviour
    {
        [Tooltip("Joint transforms in the SAME order as the pose JSON's jointNames. Wrist first.")]
        public Transform[] joints;
        public Matcher matcher;
        [Range(0f, 1f)] public float confidenceOverride = -1f; // <0 means read from the SDK

        Quaternion[] _buffer;

        public int JointCount => joints?.Length ?? 0;

        void Awake()
        {
            if (joints == null || joints.Length == 0)
                Debug.LogError("[SkeletonPoseSource] no joints assigned — nothing will ever match");
            _buffer = new Quaternion[JointCount];
        }

        /// <summary>Bind from whatever ordered list of bone transforms the SDK exposes.</summary>
        public void AutoBind(System.Collections.Generic.IList<Transform> bones)
        {
            joints = new Transform[bones.Count];
            for (int i = 0; i < bones.Count; i++) joints[i] = bones[i];
            _buffer = new Quaternion[JointCount];
        }

        void Update()
        {
            if (matcher == null || joints == null || joints.Length == 0) return;

            for (int i = 0; i < joints.Length; i++)
            {
                if (joints[i] == null) return;              // skeleton not ready yet
                _buffer[i] = joints[i].localRotation;       // LOCAL, relative to parent
            }

            float conf = confidenceOverride >= 0f ? confidenceOverride : ReadConfidence();
            matcher.Tick(_buffer, conf, Time.deltaTime);
        }

        /// <summary>
        /// Replace this with the SDK's per-hand confidence value.
        /// VERIFY ON DEVICE: the exact API name differs between SDK versions.
        /// Returning 1f keeps the app running; it disables the pause behaviour.
        /// </summary>
        float ReadConfidence() => 1f;
    }
}
