using System;
using System.Reflection;
using Signspace.Core;
using UnityEngine;

namespace Signspace.UI
{
    /// <summary>
    /// Debug-only passthrough switch (P4.5). It exists to film the comparison, not to ship.
    ///
    /// The rubric asks whether the experience still plays with passthrough off. Arguing that is
    /// cheap; showing it takes three seconds. So one key toggles the passthrough layer's visibility
    /// while everything else - hands, shelf, anchor, pose - stays exactly as it was. That is what
    /// makes the pair honest: the only variable is the room itself.
    ///
    /// It sits behind `DevOnly` (P2.3), so a release build removes it. Hiding the layer is done by
    /// name through reflection, the same way the perf rig looks for the layer: the toggle must not
    /// add a compile dependency on the XR SDK to a UI component.
    /// </summary>
    public class PassthroughToggle : MonoBehaviour
    {
        /// <summary>The seam a headless check can replace. The real sink talks to the XR SDK.</summary>
        public interface ISink
        {
            bool Available { get; }
            void SetHidden(bool hidden);
        }

        [Tooltip("Development builds only: the key that flips passthrough.")]
        public KeyCode hotkey = KeyCode.P;

        [Tooltip("Is passthrough on at launch? The product ships with it on.")]
        public bool startOn = true;

        [Tooltip("Set by the editor check. Null uses the XR SDK layer.")]
        [NonSerialized] public ISink sinkOverride;

        public bool IsOn { get; private set; }
        public bool LastApplySucceeded { get; private set; }

        ISink _sink;
        bool _warned;

        void Awake()
        {
            _sink = sinkOverride ?? new LayerSink();
            IsOn = startOn;
        }

        void Start()
        {
            // Do not force a state the layer already has; on a device with passthrough off at the
            // system level this would hide a layer that was never visible, and log a mystery.
            TrySet(IsOn);
        }

        void Update()
        {
            if (!DevOnly.Allowed) return;
            if (Input.GetKeyDown(hotkey)) Toggle();
        }

        public bool Toggle() => TrySet(!IsOn);

        public bool TrySet(bool on)
        {
            // The check sets `sinkOverride`, which may happen after Awake already chose the real
            // sink. An override always wins, so a headless check never touches the XR SDK.
            if (sinkOverride != null) _sink = sinkOverride;
            else if (_sink == null) _sink = new LayerSink();
            LastApplySucceeded = false;
            IsOn = on;

            if (!_sink.Available)
            {
                if (!_warned)
                {
                    _warned = true;
                    Debug.LogWarning("[PassthroughToggle] no OVRPassthroughLayer in the scene, so the " +
                                     "on/off comparison cannot be filmed here. Add the SDK's Passthrough " +
                                     "building block and run this on the headset.");
                }
                return false;
            }

            _sink.SetHidden(!on);
            LastApplySucceeded = true;
            Debug.Log("[PassthroughToggle] passthrough " + (on ? "on" : "off") +
                      " (layer hidden=" + (!on) + ")");
            return true;
        }

        /// <summary>The real sink: find `OVRPassthroughLayer` by name and flip its `hidden` field.</summary>
        sealed class LayerSink : ISink
        {
            Component _layer;
            FieldInfo _hidden;
            bool _searched;

            public bool Available
            {
                get
                {
                    Search();
                    return _layer != null && _hidden != null;
                }
            }

            public void SetHidden(bool hidden)
            {
                Search();
                if (_layer == null || _hidden == null) return;
                _hidden.SetValue(_layer, hidden);
            }

            void Search()
            {
                if (_searched) return;
                _searched = true;

                Type layerType = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (var t in assembly.GetTypes())
                        {
                            if (t.Name != "OVRPassthroughLayer") continue;
                            layerType = t;
                            break;
                        }
                    }
                    catch (ReflectionTypeLoadException) { }
                    if (layerType != null) break;
                }
                if (layerType == null) return;

                _hidden = layerType.GetField("hidden", BindingFlags.Instance | BindingFlags.Public);
                _layer = UnityEngine.Object.FindFirstObjectByType(layerType) as Component;
            }
        }
    }
}
