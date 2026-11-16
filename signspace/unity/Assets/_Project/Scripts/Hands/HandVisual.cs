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
    /// COST: one capsule per bone. Counted, not guessed, in P1.1: SegmentLengths has 20 entries
    /// (palm + 4 per long finger + 3 thumb), so **20 renderers per hand** and 40 for the reference
    /// hand plus the tracked hand - not the 25 this comment claimed before it was counted. That is
    /// a third of the 120 draw-call budget in TECH-STACK.md section 4.
    ///
    /// TWO MODES, ONE MESH
    /// -------------------
    /// The reference hand is driven by ROTATIONS ONLY (ApplyPose) because the pose files store
    /// rotations against an identity rest pose. The tracked hand is driven by WORLD POSITIONS
    /// (FitToSkeleton) because a real hand's bone lengths are not the 20 constants below and
    /// squeezing them in would look wrong. Both modes move the same 20 capsules, so the thing you
    /// turn with your wrist is drawn by exactly the same code as the thing you are trying to match. If it ever matters, merge the capsules
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

        /// <summary>
        /// The capsule transform for each canonical joint, or null for joints that draw nothing
        /// (the wrist and the five tips). Parallel to <see cref="joints"/>. Filled by Build().
        /// </summary>
        public Transform[] bones;

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

        /// <summary>
        /// Where each joint sits relative to its parent, in metres at scale 1.
        ///
        /// WHY THIS EXISTS
        /// ---------------
        /// Without it every joint was created at its parent's origin, so all 26 joints occupied one
        /// point: the "hand" was a bundle of capsules radiating from the palm's centre. The metric is
        /// rotations-only, so it never noticed, and a compile never notices. But the honest mirror
        /// draws the excluded shapes with this rig - a specimen with no hand geometry is not a
        /// specimen. The four knuckle offsets are what make it a hand: index to little span 45 mm,
        /// index and middle proximal joints 17 mm apart.
        /// </summary>
        static readonly System.Collections.Generic.Dictionary<string, Vector3> RestOffsets =
            new System.Collections.Generic.Dictionary<string, Vector3>
            {
                // wrist -> palm
                { "Palm", new Vector3(0f, 0.040f, 0f) },
                // knuckle line across the palm: lateral, then forward, at the palm's far edge
                { "IndexMetacarpal",  new Vector3(-0.024f, 0.055f, 0f) },
                { "MiddleMetacarpal", new Vector3(-0.007f, 0.060f, 0f) },
                { "RingMetacarpal",   new Vector3(0.008f, 0.056f, 0f) },
                { "LittleMetacarpal", new Vector3(0.021f, 0.050f, 0f) },
                // thumb sits off the palm's radial side, slightly in front of it
                { "ThumbMetacarpal",  new Vector3(-0.032f, 0.012f, 0.012f) },
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
            bones = new Transform[JointNames.Canonical.Length];
            ParentIndices = new int[JointNames.Canonical.Length];
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

            // rest positions: knuckles across the palm, every other joint at its parent bone's tip
            for (int i = 0; i < JointNames.Canonical.Length; i++)
            {
                string name = JointNames.Canonical[i];
                Vector3 offset;
                if (RestOffsets.TryGetValue(name, out offset))
                {
                    byName[name].localPosition = offset * scale;
                }
                else
                {
                    string parent = Parents.ContainsKey(name) ? Parents[name] : PreviousInFinger(name);
                    float parentLength = parent != null && SegmentLengths.ContainsKey(parent)
                        ? SegmentLengths[parent] : 0f;
                    byName[name].localPosition = new Vector3(0f, parentLength * scale, 0f);
                }
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
                // The capsule starts at this joint and extends along +Y. For the palm it spans
                // wrist -> palm base, so its centre is half the offset, not half a bone past it.
                bone.transform.localPosition = joint == "Palm"
                    ? new Vector3(0f, local.y * 0.5f, local.z * 0.5f)
                    : local + new Vector3(0f, length * 0.5f, 0f);
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

                int jointIndex = JointNames.IndexOf(joint);
                if (jointIndex >= 0) bones[jointIndex] = bone.transform;
            }

            for (int i = 0; i < JointNames.Canonical.Length; i++)
            {
                string parentName = Parents.ContainsKey(JointNames.Canonical[i])
                    ? Parents[JointNames.Canonical[i]]
                    : PreviousInFinger(JointNames.Canonical[i]);
                ParentIndices[i] = parentName == null ? -1 : JointNames.IndexOf(parentName);
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

        /// <summary>Index of each joint's parent in canonical order, or -1. Filled by Build().</summary>
        public int[] ParentIndices;

        /// <summary>
        /// Drives the visual from a real, tracked skeleton. This is the tracked-hand mode: the
        /// capsules are stretched and aimed to span each real bone, so the drawn hand is the
        /// user's hand at the user's hand size instead of the 20 constants above.
        ///
        /// WHAT IT DOES, PER BONE
        ///   1. take the bone's real start and end: the parent joint's world position and its own
        ///   2. length = that distance, so a child's hand is not drawn as an adult's
        ///   3. rotate the capsule so its +Y axis runs along the bone
        ///   4. the palm gets a real basis - flattened along the palm normal, computed from the
        ///      index and little metacarpals - so the palm faces the right way instead of
        ///      presenting whichever roll FromToRotation happened to pick
        ///
        /// WHY NOT localRotation LIKE ApplyPose
        /// ------------------------------------
        /// A pose file has no positions: it was authored against a rest pose with identity local
        /// rotations, so rotations are all it can carry. A tracked skeleton has real positions and
        /// a real, different rest pose. Feeding rotations alone would replay the SDK's rest pose
        /// onto our geometry, which is the difference between "a hand" and "a hand-shaped thing
        /// wearing someone else's skeleton".
        ///
        /// The guard clamps absurd lengths rather than trusting the tracker: a lost joint can
        /// report a position a metre away and a 2 m finger looks like broken hardware.
        /// </summary>
        public void FitToSkeleton(Transform[] sourceJoints)
        {
            if (bones == null || sourceJoints == null || ParentIndices == null) return;
            int n = Mathf.Min(JointNames.Canonical.Length, Mathf.Min(bones.Length, sourceJoints.Length));

            for (int i = 0; i < n; i++)
            {
                var bone = bones[i];
                if (bone == null) continue;
                int p = ParentIndices[i];
                if (p < 0 || p >= n) continue;
                var src = sourceJoints[i];
                var srcParent = sourceJoints[p];
                if (src == null || srcParent == null) continue;

                Vector3 a = srcParent.position;
                Vector3 d = src.position - a;
                float length = d.magnitude;
                if (length < 0.002f) continue;                      // collapsed: not a bone
                length = Mathf.Min(length, 0.12f);                  // nothing on a hand is longer
                Vector3 axis = d / d.magnitude;

                bool isPalm = JointNames.Canonical[i] == "Palm";
                Vector3 normal = isPalm ? PalmNormal(sourceJoints) : axis;
                bone.rotation = isPalm
                    ? Quaternion.LookRotation(normal, axis)
                    : Quaternion.FromToRotation(Vector3.up, axis);
                bone.position = a + d.normalized * (length * 0.5f);

                float radius = (SegmentRadii.ContainsKey(JointNames.Canonical[i])
                    ? SegmentRadii[JointNames.Canonical[i]] : 0.008f) * scale;
                bone.localScale = isPalm
                    ? new Vector3(radius * 2f, length * 0.5f, radius * 1.15f)
                    : new Vector3(radius * 2f, length * 0.5f, radius * 2f);
            }
        }

        /// <summary>
        /// The palm's facing direction: across the knuckles, from the index side to the little side.
        /// Falls back to the bone axis when the two metacarpals are not both tracked, which keeps
        /// the palm visible rather than hiding it.
        /// </summary>
        static Vector3 PalmNormal(Transform[] src)
        {
            int wrist = JointNames.IndexOf("Wrist");
            int index = JointNames.IndexOf("IndexMetacarpal");
            int little = JointNames.IndexOf("LittleMetacarpal");
            if (wrist < 0 || index < 0 || little < 0) return Vector3.forward;
            if (wrist >= src.Length || index >= src.Length || little >= src.Length) return Vector3.forward;
            if (src[wrist] == null || src[index] == null || src[little] == null) return Vector3.forward;

            Vector3 toIndex = src[index].position - src[wrist].position;
            Vector3 toLittle = src[little].position - src[wrist].position;
            Vector3 n = Vector3.Cross(toIndex, toLittle);
            return n.sqrMagnitude < 1e-8f ? Vector3.forward : n.normalized;
        }

        /// <summary>
        /// Draw cost of this hand, counted from the meshes actually attached. Used by the P1.1
        /// measurement and by the debug readout - a number from the scene, not from this file's
        /// comments (which were wrong once already).
        /// </summary>
        public HandCost CountCost()
        {
            var cost = new HandCost();
            if (bones == null) return cost;
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == null) continue;
                var filter = bones[i].GetComponent<MeshFilter>();
                var renderer = bones[i].GetComponent<MeshRenderer>();
                if (filter == null || filter.sharedMesh == null || renderer == null) continue;
                cost.renderers++;
                cost.triangles += filter.sharedMesh.triangles.Length / 3;
                if (renderer.isVisible) cost.visibleRenderers++;
            }
            return cost;
        }

        public struct HandCost
        {
            public int renderers;
            public int visibleRenderers;
            public int triangles;

            public override string ToString() =>
                renderers + " renderers (" + visibleRenderers + " visible), " + triangles + " triangles";
        }

        /// <summary>Shows or hides the whole hand without touching the joint data.</summary>
        public void SetVisible(bool visible)
        {
            if (bones == null) return;
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == null) continue;
                var renderer = bones[i].GetComponent<MeshRenderer>();
                if (renderer != null && renderer.enabled != visible) renderer.enabled = visible;
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
