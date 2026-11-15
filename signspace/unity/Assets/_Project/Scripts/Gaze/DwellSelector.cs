using System;
using UnityEngine;
using UnityEngine.UI;

namespace Signspace.Gaze
{
    /// <summary>
    /// The eyes-only path: hold your gaze on a target and it confirms.
    ///
    /// Why this exists at all: Best Accessibility Forward's own definition names "eyes-only navigation
    /// for limited mobility". It is also the strongest single accessibility shot in the submission
    /// video. So it is a complete path, not a crippled one — picking a shape, inspecting a finger,
    /// confirming a lock and opening the honest mirror must all be reachable with the hands still.
    ///
    /// Tuning from GAZE-INPUT.md: 1.2 s dwell, 2.5 deg radius, and re-trigger must work.
    /// </summary>
    public class DwellSelector : MonoBehaviour
    {
        public GazeHighlighter highlighter;
        public float dwellSeconds = 1.2f;
        [Tooltip("Ring image. Optional — the path must work without any UI.")]
        public Image ring;
        [Tooltip("Head-gaze targets need a longer dwell because head gaze is noisier.")]
        public float headFallbackMultiplier = 1.5f;

        public event Action<GameObject> OnDwellComplete;

        GameObject _target;
        float _timer;

        public float Progress01 => Mathf.Clamp01(_timer / EffectiveDwell);

        float EffectiveDwell =>
            dwellSeconds * (highlighter != null && highlighter.IsHeadFallback ? headFallbackMultiplier : 1f);

        void OnEnable()
        {
            if (highlighter != null) highlighter.OnTargetFocused += SetTarget;
        }

        void OnDisable()
        {
            if (highlighter != null) highlighter.OnTargetFocused -= SetTarget;
        }

        void SetTarget(GameObject go)
        {
            if (go == _target) return;      // same target: keep accumulating, do not reset
            _target = go;
            _timer = 0f;
        }

        void Update()
        {
            if (_target == null)
            {
                _timer = 0f;
                UpdateRing();
                return;
            }

            _timer += Time.deltaTime;
            UpdateRing();

            if (_timer >= EffectiveDwell)
            {
                var done = _target;
                // Re-trigger: reset rather than disable, or the path becomes one-shot and unusable.
                _timer = 0f;
                OnDwellComplete?.Invoke(done);
            }
        }

        void UpdateRing()
        {
            if (ring != null) ring.fillAmount = Progress01;
        }
    }
}
