using System.IO;
using Signspace.Matching;
using Signspace.Spatial;
using UnityEditor;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// Builds the P1.7 rig: a shelf with one cube on it, saved against the MRUK anchor and restored
    /// on the next launch.
    ///
    /// WHY THE CUBE IS A CHILD OF THE SHELF, NOT A SPAWNED OBJECT
    /// The task's acceptance test is "the cube is still there". If the cube only appeared after a
    /// shape was unlocked, the test would need a lesson in sign language first. So the test cube is
    /// part of the shelf and is placed on the very first launch, which means the relaunch comparison
    /// is about persistence and nothing else.
    ///
    /// REUSING THE P1.6 BOOT
    /// Restoration must wait for the room rather than poll for it, and `MrukBoot` already owns that
    /// state machine. If the P1.6 rig exists this builder reuses its boot and finder instead of
    /// creating a second MRUK consumer - two of them would race, and the loser would log "no room".
    /// </summary>
    public static class PersistenceBuilder
    {
        [MenuItem("SIGNSPACE/P1.7 Build persistence rig")]
        public static void BuildMenu() { Build(); }

        [MenuItem("SIGNSPACE/P1.7 Clear save (clean first run)")]
        public static void ClearSaveMenu()
        {
            var path = Path.Combine(Application.persistentDataPath, "signspace-save.json");
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log("[PersistenceBuilder] deleted " + path);
            }
            else
            {
                Debug.Log("[PersistenceBuilder] nothing to delete at " + path);
            }
            Debug.Log("[PersistenceBuilder] on device, the same effect is `clearSaveOnStart = true` " +
                      "on PersistenceProbe for one launch, or:\n" +
                      "  adb shell rm -f /sdcard/Android/data/com.signspace.signspace/files/signspace-save.json");
        }

        public static void Build(bool includeProbe = true)
        {
            // ---- 1. only this rig is live ---------------------------------------------------
            string[] quiet = { "GrabTest", "ProbeRig", "RotationTrial" };
            foreach (var name in quiet)
            {
                var go = GameObject.Find(name);
                if (go == null) continue;
                go.SetActive(false);
                Debug.Log("[PersistenceBuilder] " + name + " disabled for the persistence session");
            }

            // ---- 2. reuse P1.6's boot and finder, or make them ------------------------------
            MrUkSurfaceFinder finder = Object.FindFirstObjectByType<MrUkSurfaceFinder>();
            MrukBoot boot = Object.FindFirstObjectByType<MrukBoot>();
            if (finder == null || boot == null)
            {
                var spatial = GameObject.Find("MrukProbeRig");
                if (spatial == null) spatial = new GameObject("MrukProbeRig");
                if (boot == null) boot = spatial.AddComponent<MrukBoot>();
                if (finder == null) finder = spatial.AddComponent<MrUkSurfaceFinder>();
                Debug.Log("[PersistenceBuilder] created boot + finder on '" + spatial.name +
                          "' (the P1.6 rig was not in this scene)");
            }

            var head = FindHead();
            if (finder.headOverride == null) finder.headOverride = head;

            // ---- 3. save -------------------------------------------------------------------
            SaveSystem save = Object.FindFirstObjectByType<SaveSystem>();
            if (save == null)
            {
                var go = new GameObject("SaveSystem");
                save = go.AddComponent<SaveSystem>();
                Debug.Log("[PersistenceBuilder] added a SaveSystem");
            }

            // ---- 4. the shelf ---------------------------------------------------------------
            var shelfGo = GameObject.Find("Shelf");
            if (shelfGo == null) shelfGo = new GameObject("Shelf");

            var plate = FindOrMake(shelfGo.transform, "ShelfPlate", PrimitiveType.Cube);
            plate.transform.localScale = new Vector3(0.50f, 0.02f, 0.16f);
            plate.transform.localPosition = Vector3.zero;
            Tint(plate, new Color(0.16f, 0.17f, 0.20f));

            if (includeProbe)
            {
                // The acceptance object. 10 cm, same as every other rig in this phase.
                var cube = FindOrMake(shelfGo.transform, "TestCube", PrimitiveType.Cube);
                cube.transform.localScale = Vector3.one * 0.10f;
                cube.transform.localPosition = new Vector3(-0.18f, 0.06f, 0f);
                Tint(cube, new Color(0.15f, 0.9f, 1f));

                // a nose so a rotation error is visible, not just a translation error
                var nose = FindOrMake(cube.transform, "Nose", PrimitiveType.Sphere);
                nose.transform.localScale = Vector3.one * 0.35f;
                nose.transform.localPosition = new Vector3(0f, 0f, 0.75f);
                var noseCollider = nose.GetComponent<Collider>();
                if (noseCollider != null) Object.DestroyImmediate(noseCollider);

            }

            // ---- 4a. the plinth, as a designed object (P4.4) --------------------------------
            // A cube with a tint was enough to prove the fallback path in P1.7. It is not enough to
            // hand a judge: the plinth has to look like something the room could contain. The
            // prefab owns its gradient, its lip and its shadow, and its local y=0 is its top.
            var oldPlinth = shelfGo.transform.Find("Plinth");
            if (oldPlinth != null) Object.DestroyImmediate(oldPlinth.gameObject);

            var plinthPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlinthBuilder.PrefabPath);
            if (plinthPrefab == null) plinthPrefab = PlinthBuilder.Build();
            GameObject plinth = null;
            if (plinthPrefab != null)
            {
                plinth = (GameObject)PrefabUtility.InstantiatePrefab(plinthPrefab, shelfGo.transform);
                plinth.name = "Plinth";
                plinth.transform.localRotation = Quaternion.Euler(4f, 0f, 0f);
                // The plate's underside is at -0.01; the prefab's top is at 0. Contact, no gap.
                plinth.transform.localPosition = new Vector3(0f, -0.01f, 0f);
                plinth.SetActive(false);   // ShelfAnchor turns it on when there is no surface
            }
            else
            {
                Debug.LogWarning("[PersistenceBuilder] could not build the plinth prefab; the fallback " +
                                 "will be the bare shelf with no platform.");
            }

            var anchor = shelfGo.GetComponent<ShelfAnchor>();
            if (anchor == null) anchor = shelfGo.AddComponent<ShelfAnchor>();
            anchor.finderComponent = finder;
            anchor.save = save;
            anchor.boot = boot;
            anchor.userHead = head;
            anchor.shelfRoot = shelfGo.transform;
            anchor.plinthRoot = plinth;
            anchor.plateRoot = plate.transform;

            // ---- 4b. the shelf's objects and its breathing (P4.2) ---------------------------
            // One owner for the idle: twelve objects each with their own Update is the trap the task
            // names, and the switch that turns it off has to exist before P7 needs it.
            anchor.objectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/ShelfObject.prefab");
            if (anchor.objectPrefab == null)
                Debug.LogWarning("[PersistenceBuilder] ShelfObject.prefab missing: locked shapes will " +
                                 "have nothing to put on the shelf. Run SIGNSPACE/P4.2 or re-import.");

            anchor.objectMaterial = BuildMaterials.Get(
                BuildMaterials.Folder + "/ShelfObject.mat", Color.white);

            Transform slotRoot = shelfGo.transform.Find("Slots");
            if (slotRoot == null)
            {
                var slots = new GameObject("Slots");
                slots.transform.SetParent(shelfGo.transform, false);
                slots.transform.localPosition = new Vector3(0f, 0.012f, 0f);   // just above the plate
                slotRoot = slots.transform;
            }
            anchor.slotRoot = slotRoot;
            anchor.slotEmptyMaterial = BuildMaterials.Get(
                BuildMaterials.Folder + "/SlotEmpty.mat", new Color(0.30f, 0.32f, 0.35f, 1f));

            var idle = shelfGo.GetComponent<ShelfIdle>();
            if (idle == null) idle = shelfGo.AddComponent<ShelfIdle>();
            anchor.idle = idle;
            anchor.RefreshSlots();

            // ---- 4c. the quiet surface offer (P4.4 step 3) ---------------------------------
            // A bead on the plate's right edge, hidden until a surface appears after launch. It is
            // the only thing that can accept the offer, so nothing ever migrates on its own.
            var offerGo = shelfGo.transform.Find("SurfaceOffer");
            Transform offerRoot = offerGo;
            if (offerRoot == null)
            {
                var beadRoot = new GameObject("SurfaceOffer");
                beadRoot.transform.SetParent(shelfGo.transform, false);
                beadRoot.transform.localPosition = new Vector3(0.15f, 0.035f, 0.02f);

                var bead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bead.name = "Bead";
                bead.transform.SetParent(beadRoot.transform, false);
                bead.transform.localScale = Vector3.one * 0.018f;
                var beadCollider = bead.GetComponent<Collider>();
                if (beadCollider != null) Object.DestroyImmediate(beadCollider);
                var beadRenderer = bead.GetComponent<Renderer>();
                if (beadRenderer != null)
                    beadRenderer.sharedMaterial = BuildMaterials.Get(
                        BuildMaterials.Folder + "/SurfaceOffer.mat", new Color(0.55f, 0.85f, 0.95f));

                var offer = beadRoot.AddComponent<SurfaceOffer>();
                offer.shelf = anchor;
                offer.visual = bead;
                offerRoot = beadRoot.transform;
            }
            else
            {
                var offer = offerRoot.GetComponent<SurfaceOffer>();
                if (offer != null) offer.shelf = anchor;
            }

            // ---- 4d. measured FoV reflow (P4.6) --------------------------------------------
            var reflow = shelfGo.GetComponent<ShelfReflow>();
            if (reflow == null) reflow = shelfGo.AddComponent<ShelfReflow>();
            reflow.shelf = anchor;
            reflow.head = head;
            reflow.showSafeCone = false;      // P4.6 turns this on for the video, then removes it

            if (includeProbe)
            {
                // ---- 5. the probe ---------------------------------------------------------------
                var probeGo = GameObject.Find("PersistenceProbe");
                if (probeGo == null) probeGo = new GameObject("PersistenceProbe");
                var probe = probeGo.GetComponent<PersistenceProbe>();
                if (probe == null) probe = probeGo.AddComponent<PersistenceProbe>();
                probe.shelf = anchor;
                probe.save = save;
                probe.boot = boot;
                probe.hand = Object.FindFirstObjectByType<MetaHandSource>();

                // ---- 6. what the operator has to do ---------------------------------------------
                Debug.Log(
                    "[PersistenceBuilder] P1.7 rig ready. The three rows that close the task:\n" +
                    "  launch 1  nothing saved  -> outcome PlacedFresh, cube lands on the table\n" +
                    "  launch 2  kill from recents, relaunch -> outcome Restored, same physical square\n" +
                    "  launch 3  reboot the headset, relaunch -> outcome Restored again\n" +
                    "Pull the log:\n" +
                    "  adb shell run-as com.signspace.signspace cat /sdcard/Android/data/com.signspace.signspace/files/persistence-probe.jsonl");            }

        }

        static GameObject FindOrMake(Transform parent, string name, PrimitiveType type)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.gameObject;
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            return go;
        }

        static void Tint(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            // Shader.Find returns a "fake null" Unity object, so `??` would not fall back correctly
            var shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return;   // untextured is survivable; a red console is not worth it
            var material = new Material(shader);
            material.color = color;
            renderer.sharedMaterial = material;
        }

        static Transform FindHead()
        {
            var center = GameObject.Find("CenterEyeAnchor");
            if (center != null) return center.transform;
            var cam = Camera.main;
            if (cam != null) return cam.transform;
            var any = Object.FindFirstObjectByType<Camera>();
            return any != null ? any.transform : null;
        }
    }
}
