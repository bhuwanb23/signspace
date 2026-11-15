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

        public static void Build()
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

            var plinth = FindOrMake(shelfGo.transform, "Plinth", PrimitiveType.Cube);
            plinth.transform.localScale = new Vector3(0.46f, 0.02f, 0.14f);
            plinth.transform.localPosition = new Vector3(0f, -0.03f, 0f);
            Tint(plinth, new Color(0.28f, 0.26f, 0.24f));
            plinth.SetActive(false);   // ShelfAnchor turns it on when there is no surface

            var anchor = shelfGo.GetComponent<ShelfAnchor>();
            if (anchor == null) anchor = shelfGo.AddComponent<ShelfAnchor>();
            anchor.finderComponent = finder;
            anchor.save = save;
            anchor.boot = boot;
            anchor.userHead = head;
            anchor.shelfRoot = shelfGo.transform;
            anchor.plinthRoot = plinth;

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
                "  adb shell run-as com.signspace.signspace cat /sdcard/Android/data/com.signspace.signspace/files/persistence-probe.jsonl");
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
