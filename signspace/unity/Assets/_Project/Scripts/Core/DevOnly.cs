using UnityEngine;

namespace Signspace.Core
{
    /// <summary>
    /// Removes a development-only view from a release build (P2.3, step 5).
    ///
    /// The overlay and the confidence readout are the reason this project can be debugged with a
    /// headset on, and they contain words like "LOST" and "no matcher". Those words are correct and
    /// useful in a development build and are exactly the sort of thing that must never reach a judge:
    /// a panel that says LOST, in a product whose entire design commitment is that there is no failure
    /// state, would be the most expensive string in the build.
    ///
    /// So the string stays and the *view* goes. `Debug.isDebugBuild` is false in a release build, which
    /// means this needs no build defines and no per-scene bookkeeping: attach it to the view and it
    /// stops existing when the build stops being a development build.
    /// </summary>
    public class DevOnly : MonoBehaviour
    {
        [Tooltip("Where to log once when this removes itself. Empty = silent.")]
        public string logTag = "DevOnly";

        void Awake()
        {
            if (Debug.isDebugBuild || Application.isEditor) return;

            if (!string.IsNullOrEmpty(logTag))
                Debug.Log($"[{logTag}] development-only view removed from a release build: {name}");

            gameObject.SetActive(false);
        }

        /// <summary>True when development views are allowed to be visible in this build.</summary>
        public static bool Allowed => Debug.isDebugBuild || Application.isEditor;
    }
}
