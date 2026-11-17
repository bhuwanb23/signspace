using System;
using System.Reflection;
using UnityEngine;

namespace Signspace.UI
{
    /// <summary>
    /// The parts of P7.4 that are code rather than measurement: frame-rate target, no realtime shadows,
    /// no realtime reflection probes, and fixed foveated rendering at medium.
    ///
    /// WHY THIS IS A COMPONENT AND NOT A BUILD SETTING ONLY
    /// ----------------------------------------------------
    /// The URP asset is where MSAA lives and the build script sets that. But foveation is an `OVRManager`
    /// property, `OVRManager` may not exist in a scene, and the value can be reset by the SDK between
    /// scene loads. Applying it every time the scene starts, on the object that runs the scene, means the
    /// setting cannot be silently absent in a build the way a forgotten inspector tick can.
    ///
    /// The SDK is reached by reflection, like `EyeTrackingProbe` does, because these assembly definitions
    /// do not reference the Oculus assemblies. When the type is not present (editor, simulator, a build
    /// without the XR plugin) this logs one line and changes nothing — it never pretends to have applied
    /// foveation it did not apply.
    ///
    /// It does **not** claim a frame rate. 72 fps sustained is a device measurement (P7.4, `HUMAN-TASKS`
    /// H23); this component only asks for 72 and removes the two most expensive things in the scene.
    /// </summary>
    public class PerfSettings : MonoBehaviour
    {
        public enum Foveation
        {
            Off = 0,
            Low = 1,
            MediumLow = 2,
            Medium = 3,
            MediumHigh = 4,
            High = 5,
            HighTop = 6,
        }

        [Header("Target")]
        [Tooltip("Quest 3S is a 72 Hz panel at the 72 fps target. Asking for more wastes the budget.")]
        public int targetFrameRate = 72;

        [Header("What is switched off, and why")]
        [Tooltip("The build has no realtime light, so every shadow it computes is a shadow of nothing.")]
        public bool disableShadows = true;
        [Tooltip("No reflective surfaces are shipped; probes would only cost a cubemap per frame.")]
        public bool disableRealtimeReflectionProbes = true;

        [Header("Foveation")]
        public Foveation foveation = Foveation.Medium;
        [Tooltip("Dynamic foveation lowers the peripheral resolution further when the GPU is late.")]
        public bool dynamicFoveation = true;

        public string Report { get; private set; } = "not applied";

        void Awake()
        {
            Apply();
        }

        public void Apply()
        {
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0;

            if (disableShadows) QualitySettings.shadows = ShadowQuality.Disable;
            if (disableRealtimeReflectionProbes) QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.antiAliasing = 0;   // MSAA is owned by the URP asset; 0 here means "as configured"

            string foveationResult = ApplyFoveation();
            Report = "target " + targetFrameRate + " fps, shadows " +
                     (disableShadows ? "off" : "on") + ", foveation " + foveationResult;
            Debug.Log("[PerfSettings] " + Report);
        }

        string ApplyFoveation()
        {
            var managerType = FindType("OVRManager");
            if (managerType == null) return "unavailable (no OVRManager in this runtime)";

            object instance = null;
            var instanceProperty = managerType.GetProperty("instance",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (instanceProperty != null) instance = instanceProperty.GetValue(null);
            if (instance == null) instance = FindFirstObjectByType(managerType) as UnityEngine.Object;
            if (instance == null) return "unavailable (OVRManager not instantiated)";

            bool any = false;
            var levelField = managerType.GetProperty("foveatedRenderingLevel",
                BindingFlags.Public | BindingFlags.Instance);
            if (levelField != null && levelField.CanWrite)
            {
                var levelType = levelField.PropertyType;
                if (levelType.IsEnum)
                {
                    int wanted = Mathf.Clamp((int)foveation, 0, Enum.GetValues(levelType).Length - 1);
                    levelField.SetValue(instance, Enum.ToObject(levelType, wanted));
                    any = true;
                }
            }

            var dynamicField = managerType.GetProperty("useDynamicFoveatedRendering",
                BindingFlags.Public | BindingFlags.Instance);
            if (dynamicField != null && dynamicField.CanWrite && dynamicField.PropertyType == typeof(bool))
            {
                dynamicField.SetValue(instance, dynamicFoveation);
                any = true;
            }

            if (!any) return "OVRManager found but exposes no foveation property (SDK version?)";
            return foveation + (dynamicFoveation ? " + dynamic" : "");
        }

        static Type FindType(string name)
        {
            var direct = Type.GetType(name + ", Oculus.VR") ?? Type.GetType(name);
            if (direct != null) return direct;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var found = assembly.GetType(name, false, false);
                if (found != null) return found;
            }
            return null;
        }
    }
}
