using System;
using System.Collections.Generic;
using UnityEngine;

namespace Signspace.Matching
{
    public enum Band { Warming = 0, Close = 1, Locked = 2 }

    /// <summary>
    /// The matcher. Engine-agnostic on purpose: it consumes Quaternion[] each frame and
    /// knows nothing about Meta's SDK. That keeps it unit-testable and means an SDK
    /// upgrade cannot break the product's core logic (see MATCH-ALGORITHM.md).
    ///
    /// Owns: distance, bands with hysteresis, the 0.7 s hold, confidence pausing,
    /// lock latching, and near-miss selection.
    ///
    /// The thresholds below are not guesses. They come from
    /// scripts/tune_match.py, which measures the shipped metric against the
    /// shipped shapes with a simulated tracking-noise model:
    ///
    ///     lockedDeg      12   above the noise (p95 = 8.5 deg at 2 deg/joint),
    ///                         below the nearest near-miss pair (26.0 deg)
    ///     closeDeg       20   a wrong shape at 26 deg reads as "warming", which
    ///                         is the honest thing to show
    ///     nearMissDeg    26   the nearest intentional pair, so a deliberate
    ///                         wrong shape can always be named
    ///
    /// Re-run `python3 scripts/tune_match.py` after changing the shapes.
    /// </summary>
    public class Matcher : MonoBehaviour
    {
        [Header("Shape set")]
        [Tooltip("Shapes available this session. Order does not matter.")]
        public List<HandPose> shapes = new List<HandPose>();
        public string targetShapeId;

        [Header("Bands (degrees)")]
        [Tooltip("Locked: this close to the target shape.")]
        public float lockedDeg = 12f;
        [Tooltip("Close: recognisably near the target shape.")]
        public float closeDeg = 20f;
        [Tooltip("Band changes only after this long in the new state, so the meter never strobes.")]
        public float bandHysteresisSeconds = 0.15f;

        [Header("Hold")]
        [Tooltip("How long the shape must stay inside the locked band before it counts.")]
        public float holdSeconds = 0.7f;
        [Tooltip("Tracking below this confidence pauses the timer instead of resetting it.")]
        public float confidenceFloor = 0.7f;
        [Tooltip("A dip shorter than this is ignored entirely.")]
        public float dropoutGraceSeconds = 0.2f;
        [Tooltip("Leaving the locked band for longer than this resets progress.")]
        public float resetAfterSeconds = 0.5f;

        [Header("Palm orientation")]
        [Tooltip("Wrist and palm error is ignored up to this angle, so a naturally "
               + "different resting posture is forgiven. Beyond it, the hand is held wrong.")]
        public float orientationSlackDeg = 20f;

        [Header("Near-miss (the product's signature behaviour)")]
        public bool nearMissEnabled = true;
        [Tooltip("Fire only when the wrong shape is at least this close, in degrees.")]
        public float nearMissMaxDeg = 26f;
        [Tooltip("Minimum seconds between two near-miss messages.")]
        public float nearMissCooldownSeconds = 20f;
        [Tooltip("Confidence required for the preceding window before we are allowed to name a shape.")]
        public float nearMissConfidenceWindowSeconds = 0.4f;
        [Tooltip("How much closer the wrong shape must be than the target before we name it.")]
        public float nearMissMarginDeg = 2f;

        // ---- events ----
        public event Action<Band> OnBandChanged;
        public event Action<string> OnLocked;                  // shape id
        public event Action<string, float> OnNearMiss;         // shape id you actually made, its distance
        public event Action<bool> OnPausedChanged;             // true when tracking is too weak to judge
        public event Action<float, float> OnDistanceUpdated;   // current distance, best-other distance
        public event Action<float[]> OnFingerAnglesUpdated;    // per-finger angles, for the overlay

        // ---- state ----
        float[] _fingerAngles = new float[PoseMath.FingerChainEnd.Length];
        float _holdTimer, _outOfBandTimer, _hysteresisTimer, _nearMissCooldown, _goodConfidenceTimer;
        Band _band = Band.Warming, _pendingBand = Band.Warming;
        bool _locked, _paused;
        float _lastConfidence = 1f;
        HandPose _target;

        public Band CurrentBand => _band;
        public bool IsPaused => _paused;
        public bool IsLocked => _locked;
        public float HoldProgress01 => Mathf.Clamp01(_holdTimer / Mathf.Max(0.0001f, holdSeconds));
        public float[] FingerAngles => _fingerAngles;
        public float LastConfidence => _lastConfidence;

        void Awake()
        {
            // nothing to allocate: every buffer is fixed size and reused
        }

        public void SetTarget(string shapeId)
        {
            targetShapeId = shapeId;
            _target = shapes.Find(s => s.Id == shapeId);
            _holdTimer = 0f; _outOfBandTimer = 0f; _locked = false;
            if (_target == null) Debug.LogError($"[Matcher] target '{shapeId}' not found in the shape set");
        }

        /// <summary>Call once per frame from the adapter.</summary>
        public void Tick(Quaternion[] localRotations, float confidence, float dt)
        {
            if (_target == null) return;

            _lastConfidence = confidence;
            bool trustworthy = confidence >= confidenceFloor;
            SetPaused(!trustworthy);

            float d = PoseMath.Distance(_target.LocalRotations, localRotations,
                                        _fingerAngles, out float wristAngle);

            // Palm orientation is a gate, not a score. Rotating the wrist the wrong
            // way has to break the match even when every finger is perfect, but the
            // wrist error is not comparable with a finger error, so it is applied
            // as a slack on its own term rather than averaged into the distance.
            float orientation = Mathf.Max(wristAngle, PalmAngle(localRotations));
            float orientationPenalty = Mathf.Max(0f, orientation - orientationSlackDeg);
            if (orientationPenalty > 0f) d = Mathf.Max(d, orientationPenalty);

            // best OTHER shape -- this is what makes the near-miss possible
            float bestOther = float.PositiveInfinity;
            float secondBestOther = float.PositiveInfinity;
            string bestOtherId = null;

            for (int i = 0; i < shapes.Count; i++)
            {
                var s = shapes[i];
                if (s.Id == _target.Id || s.JointCount != _target.JointCount) continue;
                float od = PoseMath.Distance(s.LocalRotations, localRotations);
                if (od < bestOther)
                {
                    secondBestOther = bestOther;
                    bestOther = od;
                    bestOtherId = s.Id;
                }
                else if (od < secondBestOther)
                {
                    secondBestOther = od;
                }
            }
            OnDistanceUpdated?.Invoke(d, bestOther);
            OnFingerAnglesUpdated?.Invoke(_fingerAngles);

            // ---- bands, with hysteresis ----
            Band wanted = (Band)PoseMath.Band(d, lockedDeg, closeDeg);
            if (wanted != _band)
            {
                if (wanted != _pendingBand) { _pendingBand = wanted; _hysteresisTimer = 0f; }
                _hysteresisTimer += dt;
                if (_hysteresisTimer >= bandHysteresisSeconds)
                {
                    _band = wanted;
                    OnBandChanged?.Invoke(_band);
                }
            }
            else { _pendingBand = _band; _hysteresisTimer = 0f; }

            // ---- hold, with grace ----
            if (!trustworthy)
            {
                // pause: neither progress nor reset
                _outOfBandTimer = 0f;
                return;
            }

            if (_band == Band.Locked)
            {
                _outOfBandTimer = 0f;
                _holdTimer += dt;
                if (!_locked && _holdTimer >= holdSeconds)
                {
                    _locked = true;
                    OnLocked?.Invoke(_target.Id);
                }
            }
            else
            {
                _outOfBandTimer += dt;
                if (_outOfBandTimer >= resetAfterSeconds) _holdTimer = 0f;
            }

            // ---- near-miss ----
            _goodConfidenceTimer = trustworthy ? _goodConfidenceTimer + dt : 0f;
            _nearMissCooldown = Mathf.Max(0f, _nearMissCooldown - dt);

            // Ambiguity is not a diagnosis. Two conditions, both required:
            //   1. the winner must be strictly closer than the runner-up, so a
            //      hand sitting between two shapes never gets a verdict;
            //   2. the winner must beat the TARGET by a margin, so being
            //      marginally closer to the wrong shape is not a diagnosis.
            // Note this is NOT "how many shapes fall inside the window": with
            // U and V exactly 26 deg apart, a perfect U is inside the window
            // twice, and counting qualifiers would silence a correct call.
            bool unambiguous = bestOther < secondBestOther;
            bool clearlyCloser = bestOther < d - nearMissMarginDeg;

            if (nearMissEnabled && !_locked && bestOtherId != null
                && _band != Band.Locked
                && bestOther <= nearMissMaxDeg
                && bestOther < d
                && unambiguous
                && clearlyCloser
                && _goodConfidenceTimer >= nearMissConfidenceWindowSeconds
                && _nearMissCooldown <= 0f)
            {
                _nearMissCooldown = nearMissCooldownSeconds;
                OnNearMiss?.Invoke(bestOtherId, bestOther);
            }
        }

        float PalmAngle(Quaternion[] current)
        {
            if (current == null || current.Length <= PoseMath.PalmIndex) return 0f;
            if (_target.LocalRotations.Length <= PoseMath.PalmIndex) return 0f;
            return PoseMath.JointAngle(_target.LocalRotations[PoseMath.PalmIndex],
                                       current[PoseMath.PalmIndex]);
        }

        void SetPaused(bool paused)
        {
            if (paused == _paused) return;
            _paused = paused;
            OnPausedChanged?.Invoke(_paused);
        }
    }
}
