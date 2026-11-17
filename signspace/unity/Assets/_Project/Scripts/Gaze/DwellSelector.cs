using System;
using UnityEngine;
using UnityEngine.UI;

namespace Signspace.Gaze
{
    /// <summary>
    /// The eyes-only path: hold your gaze on a target and it confirms (P6.1 step 4, P6.3).
    ///
    /// Why this exists at all: Best Accessibility Forward's own definition names "eyes-only navigation
    /// for limited mobility". It is also the strongest single accessibility shot in the submission
    /// video. So it is a complete path, not a crippled one — picking a shape, inspecting a finger,
    /// confirming a lock and opening the honest mirror must all be reachable with the hands still.
    ///
    /// TUNING
    /// ------
    /// P6.1 says start at 700 ms. That is the base dwell; head gaze is noisier than eye gaze, so the
    /// fallback multiplies it by 1.5 (1.05 s). Both numbers are fields, and the "relaxed pace" toggle
    /// multiplies them through <see cref="Signspace.Core.A11ySettings"/>.
    ///
    /// FIRING ONCE PER ACQUISITION
    /// ---------------------------
    /// The first version fired, reset the timer and fired again every 1.2 s for as long as the gaze
    /// stayed put. On a toggle block that reads as a block that flickers on and off while you look at
    /// it. It now fires once per target acquisition and re-arms when the target changes — look away
    /// and back, and it fires again. That is the "re-trigger works" the doc means, without the
    /// machine-gun behaviour.
    /// </summary>
    public class DwellSelector : MonoBehaviour
    {
        public GazeHighlighter highlighter;

        [Tooltip("Seconds of steady gaze before a confirm. P6.1: start at 0.7 s.")]
        public float dwellSeconds = 0.7f;

        [Tooltip("Ring image. Optional — the path must work without any UI.")]
        public Image ring;

        [Tooltip("Head-gaze targets need a longer dwell because head gaze is noisier.")]
        public float headFallbackMultiplier = 1.5f;

        public event Action<GameObject> OnDwellComplete;

        GameObject _target;
        float _timer;
        bool _armed = true;

        public float Progress01 => Mathf.Clamp01(_timer / EffectiveDwell);

        /// <summary>True once the current target has fired and is waiting for the gaze to leave.</summary>
        public bool WaitingForRearm => !_armed;

        public GameObject CurrentTarget => _target;

        /// <summary>Effective time, after the head-gaze multiplier and the pace toggle.</summary>
        public float EffectiveDwell =>
            dwellSeconds *
            (highlighter != null && highlighter.IsHeadFallback ? headFallbackMultiplier : 1f) *
            Signspace.Core.A11ySettings.PaceScale;

        void OnEnable()
        {
            if (highlighter != null)
            {
                highlighter.OnTargetFocused += SetTarget;
                highlighter.OnFocusedNothing += NotifyNothingFocused;
            }
        }

        void OnDisable()
        {
            if (highlighter != null)
            {
                highlighter.OnTargetFocused -= SetTarget;
                highlighter.OnFocusedNothing -= NotifyNothingFocused;
            }
        }

        /// <summary>Test seam: focus a target without a raycast. The checks drive the timer this way.</summary>
        public void BeginFocus(GameObject go) => SetTarget(go);

        void SetTarget(GameObject go)
        {
            if (go == _target) return;      // same target: keep accumulating, do not reset
            _target = go;
            _timer = 0f;
            _armed = true;                  // a new target is a new decision
        }

        /// <summary>Called when the gaze leaves a target, so a return can fire again.</summary>
        public void NotifyNothingFocused()
        {
            if (_target == null) return;
            _target = null;
            _timer = 0f;
            _armed = true;
        }

        /// <summary>Drop the current dwell without firing. Used when a mode change invalidates it.</summary>
        public void Cancel()
        {
            _target = null;
            _timer = 0f;
            _armed = true;
            UpdateRing();
        }

        void Update() => Tick(Time.deltaTime);

        /// <summary>Test seam: advance the dwell by hand. Update is this call with Time.deltaTime.</summary>
        public void Tick(float dt)
        {
            if (_target == null)
            {
                _timer = 0f;
                _armed = true;
                UpdateRing();
                return;
            }

            if (!_armed)
            {
                UpdateRing();
                return;
            }

            _timer += dt;
            UpdateRing();

            if (_timer >= EffectiveDwell)
            {
                var done = _target;
                _timer = 0f;
                _armed = false;
                OnDwellComplete?.Invoke(done);
            }
        }

        void UpdateRing()
        {
            if (ring != null) ring.fillAmount = Progress01;
        }
    }
}
