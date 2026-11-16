using UnityEngine;
using Signspace.Core;

namespace Signspace.UI
{
    /// <summary>
    /// The controller-free "haptic" (P2.5, step 3).
    ///
    /// Hand tracking has no actuator, so there is nothing to buzz. What is available is the one channel
    /// that the tracked hand already owns - its own appearance - and a pulse there reads as *feedback*
    /// rather than as decoration, because it is localised to the hand that did the thing, and it lasts
    /// less time than the eye needs to call it an effect.
    ///
    /// Three pulses, 90 ms apart, each 60 ms, amplitude falling: close to the rhythm a haptic tick would
    /// have, fast enough to feel involuntary, and short enough that it never competes with the object
    /// arriving.
    /// </summary>
    public class HandPulse : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("The tracked hand's visual. Found automatically if empty.")]
        public Transform target;
        public Renderer[] renderers;

        [Header("Shape of the pulse")]
        public int pulses = 3;
        public float pulseSeconds = 0.06f;
        public float gapSeconds = 0.03f;
        public float scaleAmplitude = 0.045f;
        public float emissionAmplitude = 0.55f;
        [Tooltip("Each pulse is this much quieter than the one before.")]
        public float falloff = 0.55f;

        float _t = -1f;
        int _done;
        Vector3 _baseScale = Vector3.one;

        void Awake()
        {
            if (target == null) target = transform;
            if (renderers == null || renderers.Length == 0)
                renderers = target.GetComponentsInChildren<Renderer>();
            _baseScale = target.localScale;
        }

        /// <summary>Fire the pulse. Safe to call while one is running; the new one takes over.</summary>
        public void Pulse()
        {
            _t = 0f;
            _done = 0;
        }

        void Update()
        {
            if (_t < 0f) return;

            _t += Time.deltaTime;
            float period = pulseSeconds + gapSeconds;
            int index = Mathf.FloorToInt(_t / period);

            if (index >= pulses)
            {
                Stop();
                return;
            }

            float local = _t - index * period;
            if (local > pulseSeconds) return;             // in the gap: no displacement, which is the point

            float k = local / Mathf.Max(0.01f, pulseSeconds);
            float shape = Mathf.Sin(k * Mathf.PI);        // in and out, no snap
            float amp = Mathf.Pow(falloff, index);

            if (target != null)
                target.localScale = _baseScale * (1f + scaleAmplitude * amp * shape);

            LockPayoff.SetAlpha(renderers, 1f, emissionAmplitude * amp * shape);
        }

        void Stop()
        {
            _t = -1f;
            if (target != null) target.localScale = _baseScale;
            LockPayoff.SetAlpha(renderers, 1f, 0f);
        }

        void OnDisable() => Stop();
    }
}
