using System;
using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>
    /// Feeds the Matcher from a tracked hand skeleton.
    ///
    /// Deliberately SDK-agnostic: it reads localRotation from an ordered array of joint
    /// Transforms and knows nothing about Meta's SDK. The joints are matched to the
    /// canonical order BY NAME, so it does not matter whether the device reports the
    /// legacy Oculus hand layout or the OpenXR one.
    ///
    /// Why not reference the SDK type directly: a v208 upgrade that renames an enum
    /// member should cost us zero build time, and this file should still compile.
    /// </summary>
    public class SkeletonPoseSource : MonoBehaviour
    {
        [Tooltip("Joint transforms of ONE hand, in any order. Their names are matched to the " +
                 "canonical joint set, so the order in the list does not matter.")]
        public Transform[] joints;
        public Matcher matcher;

        [Range(0f, 1f)] public float confidenceOverride = -1f; // <0 means read from the SDK

        [Tooltip("Optional provider for the SDK's per-hand tracking confidence. " +
                 "Returning 1f keeps the app running but disables the pause behaviour.")]
        public MonoBehaviour confidenceProvider;

        [Tooltip("Optional provider for the SDK's pinch state, for the grab-and-rotate mechanic. " +
                 "Implement IHandStateProvider on a small component and assign it here.")]
        public MonoBehaviour handStateProvider;

        /// <summary>Filled after the joint mapping is resolved. Shown in the debug overlay.</summary>
        public string JointReport { get; private set; } = "not resolved";

        /// <summary>True when every canonical joint was found on the device.</summary>
        public bool JointsFullyMapped { get; private set; }

        /// <summary>
        /// The tracked hand's wrist rotation, in the canonical frame.
        /// The reference hand follows this, which is how you turn it with your own wrist.
        /// </summary>
        public Quaternion WristRotation =>
            (_buffer != null && _buffer.Length > PoseMath.WristIndex) ? _buffer[PoseMath.WristIndex] : Quaternion.identity;

        /// <summary>
        /// Optional: supplies the SDK's pinch state, for the grab-and-rotate mechanic.
        /// Implement this on a small component and assign it to handStateProvider.
        /// </summary>
        public interface IHandStateProvider
        {
            bool IsPinching { get; }
            float PinchStrength { get; }
        }

        Quaternion[] _buffer;
        int[] _deviceIndexForCanonical;   // device joint index per canonical joint, -1 if absent
        bool _mapped;

        public int JointCount => joints?.Length ?? 0;

        void Awake()
        {
            _buffer = new Quaternion[JointNames.Canonical.Length];
            _deviceIndexForCanonical = new int[JointNames.Canonical.Length];
            for (int i = 0; i < _deviceIndexForCanonical.Length; i++) _deviceIndexForCanonical[i] = -1;
        }

        void Start()
        {
            ResolveJointMapping();
        }

        /// <summary>Bind from whatever ordered list of bone transforms the SDK exposes.</summary>
        public void AutoBind(System.Collections.Generic.IList<Transform> bones)
        {
            joints = new Transform[bones.Count];
            for (int i = 0; i < bones.Count; i++) joints[i] = bones[i];
            ResolveJointMapping();
        }

        /// <summary>
        /// Binds an array that is ALREADY in canonical order.
        ///
        /// Needed by the tracked hand: the SDK's bone transforms are named for the SDK
        /// ("b_l_index1", or an OVRBone id), not for us, so name matching would map nothing.
        /// MetaHandSource does the id-to-canonical mapping once, then hands the ordered array
        /// here. Name matching stays available for rigs that do use canonical names.
        ///
        /// A null entry is a joint the device did not report. It is recorded as missing and the
        /// pose is zeroed for that joint rather than guessed, because a guessed joint is a shape
        /// that matches by accident.
        /// </summary>
        public void BindCanonical(Transform[] alreadyCanonical, string sourceName)
        {
            joints = alreadyCanonical;
            _mapped = true;

            int matched = 0;
            var report = new System.Text.StringBuilder();
            for (int c = 0; c < _deviceIndexForCanonical.Length; c++)
            {
                bool present = alreadyCanonical != null && c < alreadyCanonical.Length && alreadyCanonical[c] != null;
                _deviceIndexForCanonical[c] = present ? c : -1;
                if (present) matched++;
                else
                {
                    _buffer[c] = Quaternion.identity;
                    report.AppendLine("  MISSING: " + JointNames.Canonical[c]);
                }
            }

            JointsFullyMapped = matched == JointNames.Canonical.Length;
            JointReport = "bound in canonical order from " + (sourceName ?? "unknown") +
                          ": joints " + matched + "/" + JointNames.Canonical.Length + " mapped\n" + report;
            Debug.Log("[SkeletonPoseSource] " + JointReport);
            if (!JointsFullyMapped)
            {
                Debug.LogError("[SkeletonPoseSource] " + (JointNames.Canonical.Length - matched) +
                               " canonical joints were not reported by the device. See the missing list " +
                               "above; the shapes that depend on them will never match until it is fixed.");
            }
        }

        /// <summary>
        /// Works out which device joint is which canonical joint, by name.
        ///
        /// This replaces the old "paste the joint indices into the Matcher by hand"
        /// step. If a name is not recognised it is reported loudly rather than
        /// silently mis-mapped, because a mis-mapped joint is a shape that never
        /// matches and no error appears anywhere.
        /// </summary>
        public void ResolveJointMapping()
        {
            if (joints == null || joints.Length == 0)
            {
                Debug.LogError("[SkeletonPoseSource] no joints assigned - nothing will ever match");
                JointReport = "no joints assigned";
                return;
            }

            for (int i = 0; i < _deviceIndexForCanonical.Length; i++) _deviceIndexForCanonical[i] = -1;

            var report = new System.Text.StringBuilder();
            int matched = 0;
            var used = new bool[joints.Length];

            for (int c = 0; c < JointNames.Canonical.Length; c++)
            {
                string canonical = JointNames.Canonical[c];
                for (int d = 0; d < joints.Length; d++)
                {
                    if (used[d] || joints[d] == null) continue;
                    string name = joints[d].name;
                    if (JointNames.Canonicalize(name) == canonical)
                    {
                        _deviceIndexForCanonical[c] = d;
                        used[d] = true;
                        matched++;
                        break;
                    }
                }
                if (_deviceIndexForCanonical[c] < 0)
                {
                    report.AppendLine("  MISSING: " + canonical);
                }
            }

            JointsFullyMapped = matched == JointNames.Canonical.Length;

            report.Insert(0, "joints " + matched + "/" + JointNames.Canonical.Length + " mapped");
            for (int d = 0; d < joints.Length; d++)
            {
                if (!used[d] && joints[d] != null)
                {
                    report.AppendLine("  UNRECOGNISED: '" + joints[d].name + "'");
                }
            }
            JointReport = report.ToString();

            Debug.Log("[SkeletonPoseSource] " + JointReport);
            if (!JointsFullyMapped)
            {
                Debug.LogError("[SkeletonPoseSource] the canonical joint set was not fully mapped. " +
                               "Shapes will not match until this is fixed. See JointNames.cs for the alias table.");
            }
            _mapped = true;
        }

        void Update()
        {
            if (matcher == null || joints == null || joints.Length == 0) return;
            if (!_mapped) ResolveJointMapping();

            // fill the canonical-order buffer from the device joints
            for (int c = 0; c < _deviceIndexForCanonical.Length; c++)
            {
                int d = _deviceIndexForCanonical[c];
                if (d < 0) { _buffer[c] = Quaternion.identity; continue; }

                var t = joints[d];
                if (t == null) { _buffer[c] = Quaternion.identity; continue; }
                _buffer[c] = t.localRotation;       // LOCAL, relative to parent
            }

            float conf = confidenceOverride >= 0f ? confidenceOverride : ReadConfidence();
            matcher.Tick(_buffer, conf, Time.deltaTime);
        }

        /// <summary>
        /// The SDK's per-hand tracking confidence.
        ///
        /// VERIFY ON DEVICE: the exact API name differs between SDK versions and
        /// between the legacy and OpenXR hand paths. Until it is wired up this
        /// returns 1f, which keeps the app running and disables the pause
        /// behaviour (the matcher will never say "paused").
        ///
        /// To wire it up without touching this file, assign a component that
        /// implements IConfidenceProvider to confidenceProvider.
        /// </summary>
        public interface IConfidenceProvider
        {
            float Confidence { get; }
        }

        float ReadConfidence()
        {
            var provider = confidenceProvider as IConfidenceProvider;
            if (provider != null) return Mathf.Clamp01(provider.Confidence);
            return 1f;
        }
    }
}
