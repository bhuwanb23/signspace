using System;
using System.Collections.Generic;
using System.IO;
using Signspace.Matching;
using UnityEditor;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// Builds the two tracked hands and the HandVisual prefab from code - the P1.1 artefact.
    ///
    /// WHY A PREFAB IF THE SCENE IS GENERATED
    /// -------------------------------------
    /// P1.1's deliverable names `Assets/_Project/Prefabs/HandVisual.prefab`. A .prefab is a YAML
    /// file with a GUID per object; hand-writing one is how you spend an afternoon on a broken
    /// asset. So the prefab is EMITTED here, by the same code path that builds the scene: the
    /// repository holds the recipe, the prefab is an artefact of running it. That keeps the
    /// project's rule - "the scene is a build artefact, not a document" - and still gives P1.1 the
    /// file it asks for.
    ///
    /// WHAT IS BUILT
    /// -------------
    ///   HandVisual.prefab      HandVisual + 20 capsules, joints serialised, no materials baked in
    ///   TrackedHand_Left       prefab instance + SkeletonPoseSource + MetaHandSource
    ///   TrackedHand_Right      the same, and this one also drives the reference hand's grab
    ///   HandConfidenceReadout  on the rig root, so the numbers are always in view in a dev build
    ///
    /// Two materials, not one: the reference hand and your own hand must be distinguishable at a
    /// glance, because the whole exercise is "make yours look like this one". Neither is red -
    /// UX-STATES.md says red is reserved for a failure state that does not exist in this product.
    /// </summary>
    public static class HandRigBuilder
    {
        public const string PrefabFolder = "Assets/_Project/Prefabs";
        public const string MaterialFolder = "Assets/_Project/Materials";
        public const string HandPrefabPath = PrefabFolder + "/HandVisual.prefab";
        public const string ReferenceMaterialPath = MaterialFolder + "/HandReference.mat";
        public const string TrackedMaterialPath = MaterialFolder + "/HandTracked.mat";

        static readonly Color ReferenceColor = new Color(0.92f, 0.90f, 0.86f, 1f);  // warm neutral: the model
        static readonly Color TrackedColor = new Color(0.70f, 0.84f, 0.92f, 1f);    // cool pale: your hand

        [MenuItem("SIGNSPACE/P1.1 Build hand rig")]
        public static void BuildHandRig()
        {
            var prefab = BuildHandPrefab();
            if (prefab == null)
            {
                Debug.LogError("[HandRigBuilder] the hand prefab could not be built; the rig was not created.");
                return;
            }

            // Find or make a root, so running this twice does not stack two rigs in the scene.
            var root = GameObject.Find("HandRig");
            if (root == null) root = new GameObject("HandRig");

            foreach (var existing in root.GetComponentsInChildren<MetaHandSource>(true))
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            var right = CreateTrackedHand(root.transform, prefab, MetaHandSource.Hand.Right);
            var left = CreateTrackedHand(root.transform, prefab, MetaHandSource.Hand.Left);

            // The reference hand's grab has been dead since the project had a ReferenceHand: this
            // assigns the pinch provider, which is what IHandStateProvider was always waiting for.
            var reference = UnityEngine.Object.FindFirstObjectByType<ReferenceHand>();
            if (reference != null)
            {
                reference.handStateProvider = right.source;
                reference.trackedSource = right.source;      // P1.3 needs wrist POSITION, not just rotation
                Debug.Log("[HandRigBuilder] ReferenceHand.handStateProvider + trackedSource <- " + right.source.name);

                // P1.3's comparison rig: six cells (3 variants x 2 distances), rated with the left hand.
                var trial = root.GetComponent<RotationTrialController>();
                if (trial == null) trial = root.AddComponent<RotationTrialController>();
                trial.referenceHand = reference;
                trial.ratingHand = left.source;
                EnsureFollowFeelsLive(reference);

                // P1.4: the pose loader, pointed at the reference hand so the loaded shape appears on
                // the object the user holds rather than on a second, invisible hand somewhere else.
                var provider = root.GetComponent<PoseProvider>();
                if (provider == null) provider = root.AddComponent<PoseProvider>();
                provider.referenceHand = reference;
                provider.visual = reference != null ? reference.visual : null;
                provider.poseIds = new[] { "B", "U" };   // P1.4 names two poses; the set ships six
                Debug.Log("[HandRigBuilder] PoseProvider wired to the reference hand (poses B, U)");
            }
            else
            {
                Debug.LogWarning("[HandRigBuilder] no ReferenceHand in the scene. Grab will still be dead " +
                                 "on it until the scene builder runs.");
            }

            // Without this the pose sources have no matcher and SkeletonPoseSource.Update returns
            // on its first line - the hands would draw and nothing would ever be matched.
            var matcher = UnityEngine.Object.FindFirstObjectByType<Matcher>();
            if (matcher == null)
            {
                Debug.LogError("[HandRigBuilder] no Matcher in the scene: the hands will draw but never " +
                               "match. Run SIGNSPACE/Build Main Scene first, or add a Matcher.");
            }
            else
            {
                foreach (var h in new[] { left, right })
                {
                    var ps = h.source.poseSource;
                    if (ps != null) ps.matcher = matcher;
                }
            }

            var readout = root.GetComponent<HandConfidenceReadout>();
            if (readout == null) readout = root.AddComponent<HandConfidenceReadout>();
            readout.hands = new[] { left.source, right.source };

            Debug.Log("[HandRigBuilder] hand rig built: 2 tracked hands, readout attached.");
        }

        /// <summary>
        /// Emits Assets/_Project/Prefabs/HandVisual.prefab and returns the asset. Rebuilds the joint
        /// hierarchy each time, so the prefab always matches HandVisual's current geometry.
        /// </summary>
        public static GameObject BuildHandPrefab()
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);

            var material = EnsureMaterial(ReferenceMaterialPath, ReferenceColor);

            var go = new GameObject("HandVisual");
            try
            {
                var visual = go.AddComponent<HandVisual>();
                visual.material = material;
                visual.Build();                     // 26 joints, 20 capsules, canonical order

                var prefab = PrefabUtility.SaveAsPrefabAsset(go, HandPrefabPath);
                Debug.Log("[HandRigBuilder] wrote " + HandPrefabPath +
                          " (" + visual.CountCost().renderers + " renderers, " +
                          visual.CountCost().triangles + " triangles at full visibility)");
                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// The reference hand is placed in front of the camera once, at Start. In a batch-built scene
        /// the camera may not exist yet, and a hand stuck at the world origin is not testable. This
        /// re-places it in the editor so the P1.3 rig is usable from the moment the editor opens.
        /// </summary>
        static void EnsureFollowFeelsLive(ReferenceHand reference)
        {
            if (reference == null) return;
            reference.SetDistance(reference.distance);
        }

        struct HandWiring
        {
            public MetaHandSource source;
        }

        static HandWiring CreateTrackedHand(Transform parent, GameObject prefab, MetaHandSource.Hand hand)
        {
            var root = new GameObject("TrackedHand_" + hand);
            root.transform.SetParent(parent, false);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            instance.name = "HandVisual_" + hand;
            var visual = instance.GetComponent<HandVisual>();
            visual.material = EnsureMaterial(TrackedMaterialPath, TrackedColor);

            var poseGo = new GameObject("PoseSource_" + hand);
            poseGo.transform.SetParent(root.transform, false);
            var poseSource = poseGo.AddComponent<SkeletonPoseSource>();

            var source = root.AddComponent<MetaHandSource>();
            source.hand = hand;
            source.visual = visual;
            source.poseSource = poseSource;

            poseSource.confidenceProvider = source;      // IConfidenceProvider
            poseSource.handStateProvider = source;       // IHandStateProvider

            return new HandWiring { source = source };
        }

        /// <summary>
        /// A material, or an existing one refreshed to the given colour. The shader is chosen by
        /// trying the pipeline the project is actually on rather than assuming one: TECH-STACK.md
        /// says URP, but the manifest does not list it, so a hard URP reference would break the
        /// build the moment the assumption is wrong.
        /// </summary>
        public static Material EnsureMaterial(string path, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.color = color;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var shader = FindUnlitShader();
            var material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            material.color = color;
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            Debug.Log("[HandRigBuilder] material " + path + " using shader '" + shader.name + "'");
            return material;
        }

        static Shader FindUnlitShader()
        {
            // Ordered by preference on Quest: URP unlit is the cheapest thing that also behaves
            // correctly with foveated rendering; the rest are fallbacks so a missing package is a
            // warning rather than a null-material hand.
            string[] candidates =
            {
                "Universal Render Pipeline/Unlit",
                "Universal Render Pipeline/Simple Lit",
                "Unlit/Color",
                "Sprites/Default",
                "Standard"
            };
            foreach (var name in candidates)
            {
                var shader = Shader.Find(name);
                if (shader != null) return shader;
            }
            Debug.LogError("[HandRigBuilder] no usable shader found. The hands will be magenta.");
            return null;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
