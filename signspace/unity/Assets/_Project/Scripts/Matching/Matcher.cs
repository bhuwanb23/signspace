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
    /// </summary>
    public class Matcher : MonoBehaviour
    {
        [Header("Shape set")]
        [Tooltip("Shapes available this session. Order does not matter.")]
        public List<HandPose> shapes = new List<HandPose>();
        public string targetShapeId;

        [Header("Per-joint weights (index = joint index from the SDK)")]
        [Range(0f, 2f)] public float proximalWeight = 0.40f;   // wrist-adjacent
        [Range(0f, 2f)] public float midWeight = 0.70f;        // first phalanx
        [Range(0f, 2f)] public float distalWeight = 1.00f;     // tip — most informative
        [Range(0f, 2f)] public float thumbWeight = 0.80f;
        public int[] proximalJoints = new int[0];
        public int[] midJoints = new int[0];
        public int[] distalJoints = new int[0];
        public int[] thumbJoints = new int[0];
        public int palmJointIndex = 0;

        [Header("Bands (degrees of weighted mean error)")]
        public float lockedDeg = 12f;
        public float closeDeg = 24f;
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
        [Range(0f, 20f)] public float palmSlackDeg = 15f;
        [Range(0f, 4f)] public float palmWeight = 1.5f;

        [Header("Near-miss (the product's signature behaviour)")]
        public bool nearMissEnabled = true;
        [Tooltip("Fire only when the wrong shape is at least this close, in degrees.")]
        public float nearMissMaxDeg = 18f;
        [Tooltip("Minimum seconds between two near-miss messages.")]
        public float nearMissCooldownSeconds = 20f;
        [Tooltip("Confidence required for the preceding window before we are allowed to name a shape.")]
        public float nearMissConfidenceWindowSeconds = 0.4f;

        // ---- events ----
        public event Action<Band> OnBandChanged;
        public event Action<string> OnLocked;                  // shape id
        public event Action<string, float> OnNearMiss;         // shape id you actually made, its distance
        public event Action<bool> OnPausedChanged;             // true when tracking is too weak to judge
        public event Action<float, float> OnDistanceUpdated;   // current distance, best-other distance

        // ---- state ----
        float[] _weights;
        float _holdTimer, _outOfBandTimer, _hysteresisTimer, _nearMissCooldown, _goodConfidenceTimer;
        Band _band = Band.Warming, _pendingBand = Band.Warming;
        bool _locked, _paused;
        float _lastConfidence = 1f;
        HandPose _target;

        public Band CurrentBand => _band;
        public bool IsPaused => _paused;
        public bool IsLocked => _locked;
        public float HoldProgress01 => Mathf.Clamp01(_holdTimer / Mathf.Max(0.0001f, holdSeconds));

        void Awake() => RebuildWeights();

        public void SetTarget(string shapeId)
        {
            targetShapeId = shapeId;
            _target = shapes.Find(s => s.Id == shapeId);
            _holdTimer = 0f; _outOfBandTimer = 0f; _locked = false;
            if (_target == null) Debug.LogError($"[Matcher] target '{shapeId}' not found in the shape set");
        }

        void RebuildWeights()
        {
            if (_target == null) _target = shapes.Find(s => s.Id == targetShapeId);
            int n = _target?.JointCount ?? 0;
            _weights = new float[n];
            for (int i = 0; i < n; i++) _weights[i] = midWeight;
            foreach (int i in proximalJoints) if (i >= 0 && i < n) _weights[i] = proximalWeight;
            foreach (int i in distalJoints)   if (i >= 0 && i < n) _weights[i] = distalWeight;
            foreach (int i in thumbJoints)    if (i >= 0 && i < n) _weights[i] = thumbWeight;
        }

        /// <summary>Call once per frame from the adapter.</summary>
        public void Tick(Quaternion[] localRotations, float confidence, float dt)
        {
            if (_target == null) return;
            if (_weights == null || _weights.Length != _target.JointCount) RebuildWeights();

            _lastConfidence = confidence;
            bool trustworthy = confidence >= confidenceFloor;
            SetPaused(!trustworthy);

            float d = PoseMath.Distance(_target.LocalRotations, localRotations, _weights,
                                        palmJointIndex, palmSlackDeg, palmWeight);

            // best OTHER shape — this is what makes the near-miss possible
            float bestOther = float.PositiveInfinity;
            string bestOtherId = null;
            for (int i = 0; i < shapes.Count; i++)
            {
                var s = shapes[i];
                if (s.Id == _target.Id || s.JointCount != _target.JointCount) continue;
                float od = PoseMath.Distance(s.LocalRotations, localRotations, _weights,
                                             palmJointIndex, palmSlackDeg, palmWeight);
                if (od < bestOther) { bestOther = od; bestOtherId = s.Id; }
            }
            OnDistanceUpdated?.Invoke(d, bestOther);

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

            if (nearMissEnabled && !_locked && bestOtherId != null
                && _band != Band.Locked
                && bestOther <= nearMissMaxDeg
                && bestOther < d
                && _goodConfidenceTimer >= nearMissConfidenceWindowSeconds
                && _nearMissCooldown <= 0f)
            {
                _nearMissCooldown = nearMissCooldownSeconds;
                OnNearMiss?.Invoke(bestOtherId, bestOther);
            }
        }

        void SetPaused(bool paused)
        {
            if (paused == _paused) return;
            _paused = paused;
            OnPausedChanged?.Invoke(_paused);
        }
    }
}
