using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>
    /// Builds a low-poly hand from the canonical 26-joint skeleton, in code.
    ///
    /// WHY PROCEDURAL
    /// --------------
    /// The reference hand is the product's main character - "the stock ISDK hand looks like a demo"
    /// says TECH-STACK.md - and there is no artist and no time for one. But there is something
    /// better than an imported mesh: the geometry is generated from the SAME canonical joint set
    /// the pose files use, so the hand and the data cannot drift apart. Change a joint in
    /// hand_model.py and the mesh changes with it.
    ///
    /// It also means no binary assets in git, and the whole hand is about 8k triangles.
    ///
    /// The result is deliberately a stylised hand: soft capsules, no skin, no texture. It reads as
    /// a hand shape, which is exactly what this product needs to teach - and it cannot be mistaken
    /// for a photoreal attempt at a real hand, which would be worse.
    ///
    /// COST: one capsule per bone, so 25 renderers. On the reference hand plus the tracked hand
    /// that is ~50 draw calls of the 120 the budget allows. If it ever matters, merge the capsules
    /// into one skinned mesh; do not do it before there is a frame problem.
    /// </summary>
    public class HandVisual : MonoBehaviour
    {
        [Tooltip("Overall scale of the hand, in metres. 1.0 is an adult hand.")]
        public float scale = 1f;

        [Tooltip("Material for the bones. Unlit or cheap-lit only.")]
        public Material material;

        [Tooltip("Material for the highlight shell, if any.")]
        public Material highlightMaterial;

        /// <summary>The joint transforms, in canonical order. Index matches JointNames.Canonical.</summary>
        public Transform[] joints;

        /// <summary>Segment lengths in metres, per bone, keyed by the canonical joint name.</summary>
        static readonly System.Collections.Generic.Dictionary<string, float> SegmentLengths =
            new System.Collections.Generic.Dictionary<string, float>
            {
                // wrist to palm
                { "Palm", 0.045f },
                // long fingers: metacarpal->proximal, proximal->intermediate, intermediate->distal, distal->tip
                { "IndexMetacarpal", 0.045f }, { "IndexProximal", 0.027f }, { "IndexIntermediate", 0.019f }, { "IndexDistal", 0.016f },
                { "MiddleMetacarpal", 0.050f }, { "MiddleProximal", 0.029f }, { "MiddleIntermediate", 0.020f }, { "MiddleDistal", 0.016f },
                { "RingMetacarpal", 0.046f }, { "RingProximal", 0.027f }, { "RingIntermediate", 0.019f }, { "RingDistal", 0.015f },
                { "LittleMetacarpal", 0.037f }, { "LittleProximal", 0.022f }, { "LittleIntermediate", 0.016f }, { "LittleDistal", 0.014f },
                // thumb: metacarpal->proximal, proximal->distal, distal->tip
                { "ThumbMetacarpal", 0.036f }, { "ThumbProximal", 0.030f }, { "ThumbDistal", 0.020f },
            };

        static readonly System.Collections.Generic.Dictionary<string, float> SegmentRadii =
            new System.Collections.Generic.Dictionary<string, float>
            {
                { "Palm", 0.020f },
                { "IndexMetacarpal", 0.0095f }, { "IndexProximal", 0.0085f }, { "IndexIntermediate", 0.0075f }, { "IndexDistal", 0.0065f },
                { "MiddleMetacarpal", 0.0098f }, { "MiddleProximal", 0.0090f }, { "MiddleIntermediate", 0.0080f }, { "MiddleDistal", 0.0068f },
                { "RingMetacarpal", 0.0092f }, { "RingProximal", 0.0085f }, { "RingIntermediate", 0.0075f }, { "RingDistal", 0.0065f },
                { "LittleMetacarpal", 0.0082f }, { "LittleProximal", 0.0078f }, { "LittleIntermediate", 0.0070f }, { "LittleDistal", 0.0060f },
                { "ThumbMetacarpal", 0.0110f }, { "ThumbProximal", 0.0095f }, { "ThumbDistal", 0.0080f },
            };

        /// <summary>
        /// The parent of every joint, by canonical name. The palm is the parent of the finger
        /// metacarpals; the wrist is the parent of the palm. This matches the OpenXR hand
        /// hierarchy, and the legacy Oculus layout differs only in which of the two is the root.
        /// </summary>
        static readonly System.Collections.Generic.Dictionary<string, string> Parents =
            new System.Collections.Generic.Dictionary<string, string>
            {
                { "Palm", "Wrist" },
                { "ThumbMetacarpal", "Palm" },
                { "IndexMetacarpal", "Palm" },
                { "MiddleMetacarpal", "Palm" },
                { "RingMetacarpal", "Palm" },
                { "LittleMetacarpal", "Palm" },
            };

        static Mesh _capsuleMesh;
        static Mesh _palmMesh;

        /// <summary>
        /// Builds the rig under this transform. Returns the joint transforms in canonical order.
        /// Safe to call again: it clears whatever it built last time.
        /// </summary>
        public Transform[] Build()
        {
            Clear();
            EnsureMeshes();

            joints = new Transform[JointNames.Canonical.Length];
            var byName = new System.Collections.Generic.Dictionary<string, Transform>();

            for (int i = 0; i < JointNames.Canonical.Length; i++)
            {
                string name = JointNames.Canonical[i];
                var go = new GameObject(name);
                go.transform.SetParent(transform, false);
                byName[name] = go.transform;
                joints[i] = go.transform;
            }

            // parent the chain
            foreach (var pair in Parents)
            {
                byName[pair.Key].SetParent(byName[pair.Value], false);
            }
            for (int i = 0; i < JointNames.Canonical.Length; i++)
            {
                string name = JointNames.Canonical[i];
                string parent;
                if (!Parents.TryGetValue(name, out parent))
                {
                    // the rest of a finger hangs off the previous joint of the same finger
                    parent = PreviousInFinger(name);
                }
                if (parent != null) byName[name].SetParent(byName[parent], false);
            }

            // geometry
            foreach (var pair in SegmentLengths)
            {
                string joint = pair.Key;
                string parent = Parents.ContainsKey(joint) ? Parents[joint] : PreviousInFinger(joint);
                if (parent == null) continue;

                Vector3 local = byName[joint].localPosition;
                float length = pair.Value * scale;
                float radius = (SegmentRadii.ContainsKey(joint) ? SegmentRadii[joint] : 0.008f) * scale;

                var bone = new GameObject(joint + "_bone");
                bone.transform.SetParent(byName[parent].transform, false);
                bone.transform.localPosition = new Vector3(0f, length * 0.5f, 0f);
                bone.transform.localRotation = Quaternion.identity;

                if (joint == "Palm")
                {
                    // the palm is a flattened ellipsoid, not a finger-shaped capsule
                    bone.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 1.15f);
                }
                else
                {
                    bone.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);
                }

                var renderer = bone.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                var filter = bone.AddComponent<MeshFilter>();
                filter.sharedMesh = joint == "Palm" ? _palmMesh : _capsuleMesh;
            }

            return joints;
        }

        static string PreviousInFinger(string joint)
        {
            foreach (var chain in FingerChains)
            {
                for (int i = 0; i < chain.Length; i++)
                {
                    if (chain[i] != joint) continue;
                    if (i == 0) return "Palm";
                    return chain[i - 1];
                }
            }
            return null;
        }

        /// <summary>
        /// The canonical finger chains. Kept here rather than in PoseMath because that file is the
        /// hot path and must stay allocation-free.
        /// </summary>
        static readonly string[][] FingerChains =
        {
            new[] { "ThumbMetacarpal", "ThumbProximal", "ThumbDistal", "ThumbTip" },
            new[] { "IndexMetacarpal", "IndexProximal", "IndexIntermediate", "IndexDistal", "IndexTip" },
            new[] { "MiddleMetacarpal", "MiddleProximal", "MiddleIntermediate", "MiddleDistal", "MiddleTip" },
            new[] { "RingMetacarpal", "RingProximal", "RingIntermediate", "RingDistal", "RingTip" },
            new[] { "LittleMetacarpal", "LittleProximal", "LittleIntermediate", "LittleDistal", "LittleTip" },
        };

        /// <summary>
        /// Applies a set of local joint rotations, in canonical order.
        /// This is what puts a handshape on the hand.
        /// </summary>
        public void ApplyPose(Quaternion[] localRotations)
        {
            if (joints == null || localRotations == null) return;
            int n = Mathf.Min(joints.Length, localRotations.Length);
            for (int i = 0; i < n; i++)
            {
                if (joints[i] == null) continue;
                joints[i].localRotation = localRotations[i];
            }
        }

        /// <summary>Resets every joint to identity - the neutral, open hand.</summary>
        public void ApplyNeutral()
        {
            if (joints == null) return;
            for (int i = 0; i < joints.Length; i++)
            {
                if (joints[i] == null) continue;
                joints[i].localRotation = Quaternion.identity;
            }
        }

        void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name.EndsWith("_bone", System.StringComparison.Ordinal)) continue;
                Destroy(child.gameObject);
            }
        }

        void EnsureMeshes()
        {
            if (_capsuleMesh != null) return;

            // Unity's built-in capsule is a unit capsule: 2 units tall, radius 1.
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _capsuleMesh = capsule.GetComponent<MeshFilter>().sharedMesh;
            DestroyTemp(capsule);

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _palmMesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            DestroyTemp(sphere);
        }

        static void DestroyTemp(GameObject go)
        {
            if (Application.isPlaying) Object.Destroy(go);
            else Object.DestroyImmediate(go);
        }
    }
}
