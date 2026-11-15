using System;
using System.Reflection;
using Signspace.Perf;
using Signspace.Spatial;
using UnityEditor;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// Builds the P1.8 rig: both hands tracked, one anchored object, passthrough on, and a 15-minute
    /// frame sampler.
    ///
    /// WHY THIS BUILDER ENABLES THINGS INSTEAD OF ADDING THEM
    /// The run must measure the product's *steady* state, not four trial rigs writing jsonl. So the
    /// rigs that log per trial are switched off and the pieces that belong in the real loop - hands,
    /// the shelf from P1.7, passthrough - are switched on. What is left running is what a judge would
    /// see.
    ///
    /// The one thing it cannot do by itself is passthrough: an `OVRPassthroughLayer` needs the
    /// camera configured as the passthrough layer and the "Enable Passthrough" permission ticked.
    /// Rather than half-configure it, this builder says so.
    /// </summary>
    public static class PerfRigBuilder
    {
        [MenuItem("SIGNSPACE/P1.8 Build perf rig")]
        public static void BuildMenu() { Build(); }

        [MenuItem("SIGNSPACE/P1.8 Enable Frame Timing Stats")]
        public static void EnableFrameTimingStats()
        {
            // FrameTimingManager returns zeros without this player setting, and a summary full of
            // 0.00 ms GPU numbers reads like a perfect frame. The property has moved around between
            // Unity versions, so it is reached reflectively: if it is not there, say so instead of
            // failing to compile on a version we cannot test against.
            var prop = typeof(PlayerSettings).GetProperty("enableFrameTimingStats",
                BindingFlags.Public | BindingFlags.Static);
            if (prop == null)
            {
                Debug.LogError("[PerfRigBuilder] PlayerSettings.enableFrameTimingStats not found in this " +
                               "Unity version. Enable it by hand: Project Settings > Player > Other " +
                               "Settings > Frame Timing Stats. Without it, CPU/GPU times are blank.");
                return;
            }
            prop.SetValue(null, true, null);
            AssetDatabase.SaveAssets();
            Debug.Log("[PerfRigBuilder] Frame Timing Stats = true (was " + prop.GetValue(null, null) + ")");
        }

        public static void Build()
        {
            // ---- 1. trial rigs off: this run measures the product, not the harness ----------
            string[] quiet = { "GrabTest", "ProbeRig", "RotationTrial", "PersistenceProbe" };
            foreach (var name in quiet)
            {
                var go = GameObject.Find(name);
                if (go == null) continue;
                go.SetActive(false);
                Debug.Log("[PerfRigBuilder] " + name + " disabled: trial loggers cost frames");
            }

            // the P1.6 surface probe does one placement and then idles, but its OnGUI is a full
            // repaint every frame - keep the boot + finder, drop the probe
            var surfaceProbe = UnityEngine.Object.FindFirstObjectByType<SurfaceProbe>();
            if (surfaceProbe != null)
            {
                surfaceProbe.enabled = false;
                Debug.Log("[PerfRigBuilder] SurfaceProbe component disabled (its OnGUI repaints per frame)");
            }

            // ---- 2. passthrough ------------------------------------------------------------
            if (!SceneHasPassthroughLayer())
            {
                Debug.LogWarning(
                    "[PerfRigBuilder] NO OVRPassthroughLayer in the scene, so the run is not measuring " +
                    "the product's real loop. Add the SDK's 'Passthrough' building block " +
                    "[Building Blocks > Passthrough], and on OVRManager set Passthrough Support = " +
                    "Required with 'Enable Passthrough' ticked under Permission Requests On Startup. " +
                    "Passthrough is compositor work, not app GPU time, which is exactly why the baseline " +
                    "must include it.");
            }
            else
            {
                Debug.Log("[PerfRigBuilder] passthrough layer present");
            }

            // ---- 3. the anchored object ----------------------------------------------------
            var shelf = UnityEngine.Object.FindFirstObjectByType<ShelfAnchor>();
            if (shelf == null)
            {
                Debug.LogWarning("[PerfRigBuilder] no ShelfAnchor in the scene. P1.8 asks for 'one " +
                                 "anchored object'; run SIGNSPACE/P1.7 Build persistence rig first so " +
                                 "the measurement includes the shelf, the surface query and the cube.");
            }

            // ---- 4. the sampler ------------------------------------------------------------
            var go2 = GameObject.Find("PerfProbe");
            if (go2 == null) go2 = new GameObject("PerfProbe");
            var probe = go2.GetComponent<PerfProbe>();
            if (probe == null) probe = go2.AddComponent<PerfProbe>();
            probe.durationSeconds = 900f;
            probe.requiredSeconds = 600f;

            // ---- 5. the one player setting that changes what the file can say ----------------
            var prop = typeof(PlayerSettings).GetProperty("enableFrameTimingStats",
                BindingFlags.Public | BindingFlags.Static);
            if (prop != null && prop.PropertyType == typeof(bool) && !(bool)prop.GetValue(null, null))
            {
                Debug.LogWarning("[PerfRigBuilder] Frame Timing Stats is OFF, so cpuMs/gpuMs will be " +
                                 "blank. Run SIGNSPACE/P1.8 Enable Frame Timing Stats, or accept fps-only " +
                                 "numbers (the phase's required numbers are fps, 1% low and thermal window, " +
                                 "so this is a convenience, not a blocker).");
            }

            Debug.Log(
                "[PerfRigBuilder] P1.8 rig ready. Protocol:\n" +
                " 1. adb shell am start omms://app               # OVR Metrics Tool\n" +
                " 2. adb shell am broadcast -n com.oculus.ovrmonitormetricsservice/.SettingsBroadcastReceiver " +
                "-a com.oculus.ovrmonitormetricsservice.ENABLE_OVERLAY\n" +
                " 3. read the overlay's FPS line and the GPU level, put them in perf-baseline.md\n" +
                " 4. install the APK on the QUEST 3S, hands in view, cube anchored, passthrough on, sit still\n" +
                " 5. let it run 15 minutes without touching the headset (touch nothing: the sampler needs " +
                "an untouched device, and a controller picked up mid-run is a hitch)\n" +
                " 6. pull:\n" +
                "    adb shell run-as com.signspace.signspace cat " +
                "/sdcard/Android/data/com.signspace.signspace/files/perf-baseline-summary.txt\n" +
                "    adb shell run-as com.signspace.signspace cat " +
                "/sdcard/Android/data/com.signspace.signspace/files/perf-baseline.jsonl\n" +
                " 7. thermal ground truth, if the app's status is unavailable:\n" +
                "    adb shell dumpsys thermalservice | head -40   (poll it every minute)\n" +
                "NOT the editor, NOT the simulator: neither has the compositor, dynamic clocks or a battery.");
        }

        /// <summary>
        /// Looks for OVRPassthroughLayer by NAME across the loaded assemblies rather than by type,
        /// because its namespace differs between Meta SDK versions and this file must not depend on
        /// getting that right without a compiler to check it.
        /// </summary>
        static bool SceneHasPassthroughLayer()
        {
            Type layerType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch { continue; }
                foreach (var t in types)
                {
                    if (t.Name == "OVRPassthroughLayer") { layerType = t; break; }
                }
                if (layerType != null) break;
            }
            if (layerType == null) return false;

            var found = UnityEngine.Object.FindObjectsByType(layerType, FindObjectsSortMode.None);
            return found != null && found.Length > 0;
        }
    }
}