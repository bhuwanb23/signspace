using UnityEditor;
using UnityEngine;
using Signspace.Core;
using Signspace.UI;

namespace Signspace.Editor
{
    /// <summary>
    /// Builds `Assets/_Project/Prefabs/LockPayoff.prefab` from code (P2.5).
    ///
    /// The prefab is committed, because an artefact a reviewer can open is worth more than a builder
    /// they have to trust. But it was authored as YAML outside Unity (this repository is edited without
    /// an editor in the loop), so this menu item exists as the **regeneration path**: if the committed
    /// file ever opens with a missing script or a missing mesh, running this writes a correct one with
    /// the same structure and the same default timings.
    ///
    /// It deliberately builds *slots*, not a finished scene: the matcher, the tracked hand's visual and
    /// the chime are wired by SceneBuilder at scene-build time, because those objects do not exist until
    /// the hand rig has been created.
    /// </summary>
    public static class LockPayoffBuilder
    {
        const string Folder = "Assets/_Project/Prefabs";
        const string Path = Folder + "/LockPayoff.prefab";

        [MenuItem("SIGNSPACE/P2.5 Build lock payoff prefab")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");

            var root = new GameObject("LockPayoff");
            var payoff = root.AddComponent<LockPayoff>();

            // where the reference hand sits, so the payoff knows where the object is born
            var handSlot = new GameObject("ReferenceHandSlot");
            handSlot.transform.SetParent(root.transform, false);
            handSlot.transform.localPosition = new Vector3(0f, 0.32f, 0.42f);
            payoff.flyFrom = handSlot.transform;

            // the temporary anchor - the shelf proper arrives in P4
            var anchor = new GameObject("RestAnchor");
            anchor.transform.SetParent(root.transform, false);
            anchor.transform.localPosition = new Vector3(0.26f, 0.18f, 0.46f);
            payoff.restAnchor = anchor.transform;

            // the object the hand turns into. A slab, not a cube: the shelf holds things that look
            // like they were made, and a 16 cm cube is a box.
            var proto = GameObject.CreatePrimitive(PrimitiveType.Cube);
            proto.name = "ObjectPrototype";
            proto.transform.SetParent(root.transform, false);
            proto.transform.localPosition = new Vector3(0.26f, 0.18f, 0.46f);
            proto.transform.localScale = new Vector3(0.16f, 0.11f, 0.03f);
            payoff.objectPrototype = proto;
            proto.SetActive(false);

            payoff.dissolveSeconds = 0.40f;
            payoff.objectFormStart = 0.30f;
            payoff.objectFormSeconds = 0.22f;
            payoff.arcSeconds = 0.45f;
            payoff.settleSeconds = 0.12f;
            payoff.handReturnSeconds = 0.12f;
            payoff.budgetSeconds = 1.2f;
            payoff.fullFidelityRepeats = 3;
            payoff.hurryScale = 0.55f;
            payoff.maxObjectsOnAnchor = 3;
            payoff.arcHeight = 0.12f;
            payoff.lockChime = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/_Project/Audio/SFX/lock_chime.wav");

            var source = root.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;          // the reward comes from the shelf, not from your head
            source.minDistance = 0.3f;
            source.maxDistance = 5f;
            payoff.chime = source;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Path);
            Object.DestroyImmediate(root);
            Debug.Log("[LockPayoff] wrote " + Path + " (" +
                      (prefab != null ? "ok" : "FAILED") + "), total " + payoff.TotalSeconds.ToString("0.00") + " s");
            AssetDatabase.Refresh();
        }

        [MenuItem("SIGNSPACE/P2.5 Check payoff timing")]
        public static void CheckTiming()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
            var payoff = prefab != null ? prefab.GetComponent<LockPayoff>() : null;
            if (payoff == null)
            {
                Debug.LogError("[LockPayoff] no prefab or no LockPayoff component at " + Path);
                return;
            }
            float total = payoff.TotalSeconds;
            string verdict = total <= payoff.budgetSeconds ? "inside budget" : "OVER BUDGET";
            Debug.Log($"[LockPayoff] {total:0.00} s of a {payoff.budgetSeconds:0.00} s budget ({verdict}); " +
                      $"after {payoff.fullFidelityRepeats} locks it runs at x{payoff.hurryScale:0.00} = " +
                      $"{total * payoff.hurryScale:0.00} s");
        }
    }
}
