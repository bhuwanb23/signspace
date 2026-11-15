using Signspace.Matching;
using Signspace.Spatial;
using UnityEditor;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// Builds the P1.6 MRUK probe rig: boot the SDK, find a horizontal surface, put one cube on it.
    ///
    /// WHY IT DOES NOT JUST CREATE THE MRUK PREFAB
    /// -------------------------------------------
    /// MRUK needs three things that a `new GameObject` cannot provide: the MRUK prefab itself, its
    /// SceneSettings, and a room data source (Device, Prefab or JSON). Meta's documented way to get
    /// all three is the "Scene" building block. So this builder checks for MRUK, wires up everything
    /// it can wire, and when the prefab is missing it prints the exact steps instead of silently
    /// adding a component that will boot with no settings and look like an SDK failure.
    ///
    /// This matters more than usual because the first reading from this rig is going to be taken in
    /// the Meta XR Simulator, where the data source MUST be a Prefab room - the simulator has no real
    /// room to scan. Telling the operator that at build time is cheaper than debugging it later.
    ///
    /// WHAT IT DISABLES
    /// ----------------
    /// The grab, rotation and probe rigs all listen to the same hands and count the same pinches.
    /// Leaving them enabled here would mean the cube placement was recorded while three other
    /// components were also interpreting the pinch. They are set inactive, not deleted.
    /// </summary>
    public static class MrukProbeBuilder
    {
        [MenuItem("SIGNSPACE/P1.6 Build MRUK probe rig")]
        public static void BuildMenu()
        {
            Build();
        }

        public static void Build()
        {
            // ---- 1. the competing rigs off ------------------------------------------------
            string[] quiet = { "GrabTest", "ProbeRig", "RotationTrial" };
            foreach (var name in quiet)
            {
                var go = GameObject.Find(name);
                if (go == null) continue;
                go.SetActive(false);
                Debug.Log("[MrukProbeBuilder] " + name + " disabled for the surface session");
            }

            // ---- 2. MRUK present? ----------------------------------------------------------
            var mruk = Object.FindFirstObjectByType<Meta.XR.MRUtilityKit.MRUK>();
            if (mruk == null)
            {
                Debug.LogWarning(
                    "[MrukProbeBuilder] No MRUK in the scene. MrukBoot will add a bare component at " +
                    "runtime, which has no SceneSettings and therefore no data source. Before taking " +
                    "the P1.6 readings, add the SDK's 'Scene' building block " +
                    "[Building Blocks > Scene] and set MRUK's data source:\n" +
                    "    headset      -> Device\n" +
                    "    simulator    -> Prefab, and pick a room with a table (e.g. Office_Small)\n" +
                    "A device run also needs the scene/space permission; MRUK prompts for it on first " +
                    "load. The four-condition table is meaningless if every condition loaded the same " +
                    "prefab room, so the simulator readings must be marked as such.");
            }
            else
            {
                Debug.Log("[MrukProbeBuilder] MRUK found: " + mruk.name);
            }

            // ---- 3. rig ---------------------------------------------------------------------
            var root = GameObject.Find("MrukProbeRig");
            if (root == null) root = new GameObject("MrukProbeRig");

            var boot = root.GetComponent<MrukBoot>();
            if (boot == null) boot = root.AddComponent<MrukBoot>();

            var finder = root.GetComponent<MrUkSurfaceFinder>();
            if (finder == null) finder = root.AddComponent<MrUkSurfaceFinder>();
            finder.headOverride = FindHead();
            if (finder.headOverride == null)
            {
                Debug.LogWarning("[MrukProbeBuilder] no head transform found. Camera.main will be " +
                                 "used at runtime, and if that is also missing the finder measures " +
                                 "distances from its own transform at the origin - which would put " +
                                 "every surface out of range and read as a failed condition.");
            }

            var probe = root.GetComponent<SurfaceProbe>();
            if (probe == null) probe = root.AddComponent<SurfaceProbe>();
            probe.boot = boot;
            probe.finder = finder;
            probe.hand = Object.FindFirstObjectByType<MetaHandSource>();
            if (probe.hand == null)
            {
                Debug.LogWarning("[MrukProbeBuilder] no MetaHandSource. The condition cannot be chosen " +
                                 "by pinching and the operator rating cannot be given; every reading " +
                                 "will record conditionSource='default'. Pinch-count condition choice " +
                                 "exists because the condition must be set before the run, not " +
                                 "remembered afterwards.");
            }

            // ---- 4. sanity on the output folder --------------------------------------------
            Debug.Log("[MrukProbeBuilder] writes to " + Application.persistentDataPath +
                      ": mruk-probe.jsonl and mruk-<condition>-summary.txt\n" +
                      "  pull with: adb shell run-as com.signspace.signspace cat " +
                      "/sdcard/Android/data/com.signspace.signspace/files/mruk-probe.jsonl");
        }

        static Transform FindHead()
        {
            var anchors = GameObject.Find("CenterEyeAnchor");
            if (anchors != null) return anchors.transform;
            var cam = Camera.main;
            if (cam != null) return cam.transform;
            var anyCam = Object.FindFirstObjectByType<Camera>();
            return anyCam != null ? anyCam.transform : null;
        }
    }
}
