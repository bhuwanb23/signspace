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
    ///     lockedDeg      12   above the noise (p95 = 6.7 deg at 2 deg/joint),
    ///                         below the nearest near-miss pair (26.0 deg)
    ///     closeDeg       24   measured in P2.3, not rounded to 25: a wrong shape
    ///                         at 26 deg reads as "warming", which is the honest display
    ///     bandReleaseDeg  2   hysteresis, 150 ms - without it the band chatters on noise
    ///     nearMiss      band-based since P3.1: exactly one OTHER shape inside the lock band,
    ///                   and the target outside it. Not a 26 deg window - see NearMiss.cs
    ///     holdSeconds   0.7   P2.4: with a 200 ms freeze grace, so one dropped frame
    ///                         does not restart the hold
    ///
    /// The palm term (P2.6) is additive, not a floor: D_total = D_fingers + k*max(0, O - slack)
    /// with k = 1.0 and slack = 15 deg. See phases/p2-core-loop/palm-tuning.md.
    ///
    /// Re-run `python3 scripts/tune_match.py` after changing the shapes, `band_sim.py` after
    /// changing the bands, and `palm_sim.py` after changing the palm term.
    /// </summary>
    public class Matcher : MonoBehaviour
    {
        [Header("Shape set")]
        [Tooltip("Shapes available this session. Order does not matter.")]
        public List<HandPose> shapes = new List<HandPose>();
        public string targetShapeId;

        [Header("Weights")]
        [Tooltip("Per-joint weights, as an asset so they can be tuned without recompiling. If this is " +
                 "empty the documented defaults are used and a warning is logged - a missing asset " +
                 "must not turn into a divide-by-zero.")]
        public JointWeights weights;

        [Header("Bands (degrees)")]
        [Tooltip("Locked: this close to the target shape.")]
        public float lockedDeg = 12f;
        [Tooltip("Close: recognisably near the target shape. Measured in band_sim.py: 24 leaves 2.0 deg "
               + "to the nearest accidental pair (U-V, 26.0), and a hand making that pair is told "
               + "'close' on 0.0% of frames.")]
        public float closeDeg = 24f;
        [Tooltip("Degrees of hysteresis on the way OUT of a band. Entering uses the plain edge. "
               + "2 deg removes the last of the edge flicker (band_sim.py: 4 changes/30 s -> 0).")]
        public float bandReleaseDeg = 2f;
        [Tooltip("Band changes only after this long in the new state, so the meter never strobes.")]
        public float bandHysteresisSeconds = 0.15f;

        [Header("Hold")]
        [Tooltip("How long the shape must stay inside the locked band before it counts.")]
        public float holdSeconds = 0.7f;
        [Tooltip("Tracking below this confidence pauses the timer instead of resetting it.")]
        public float confidenceFloor = 0.7f;
        [Tooltip("A tracking dip shorter than this is FROZEN, not reset. Measured in hold_sim.py.")]
        public float dropoutGraceSeconds = 0.2f;
        [Tooltip("Beyond the grace window, progress bleeds at this many seconds per second rather than " +
                 "being dumped. 0.5 keeps a nearly-finished attempt alive through a 1.5 s dropout " +
                 "while still clearing stale progress in a long one (hold_sim.py).")]
        public float stallDecayPerSecond = 0.5f;
        [Tooltip("Outside the locked band (or a lock held) for longer than this, progress is forfeited " +
                 "and the lock releases.")]
        public float resetAfterSeconds = 0.5f;

        [Header("Palm orientation (P2.6)")]
        [Tooltip("Palm facing error ignored up to this angle, so a naturally different resting wrist is " +
                 "not punished. Measured: wrist jitter for a correctly-facing hand has sd 1.4 deg and " +
                 "p99 6.4 deg (palm_sim.py), so 15 deg is ~10 sigma of noise.")]
        public float orientationSlackDeg = 15f;
        [Tooltip("k: how much a degree of palm error costs, beyond the slack. 1.0 = a degree of facing " +
                 "error costs exactly a degree of match, which is the smallest weight that stops a 30 deg " +
                 "mis-face from locking (palm_sim.py section 4).")]
        public float orientationWeightK = 1f;
        [Tooltip("Clamp on the orientation penalty. 0 = uncapped. See palm-tuning.md section 5 for why " +
                 "the shipped value is uncapped.")]
        public float orientationPenaltyCapDeg = 0f;

        [Header("Near-miss (the product's signature behaviour)")]
        public bool nearMissEnabled = true;
        [Tooltip("Minimum seconds between two near-miss messages. It has to stay rare to stay valuable.")]
        public float nearMissCooldownSeconds = 20f;
        [Tooltip("The stable window (P3.2): BOTH conditions must hold for this long before the app may " +
                 "name a shape - the confidence above the floor, AND the same shape being the one it " +
                 "would name. 400 ms is the task's figure and it is the difference between 'the rule is " +
                 "satisfied right now' and 'the hand is still doing that'.")]
        public float nearMissStableSeconds = 0.4f;
        [Tooltip("How much closer the wrong shape must be than the target before we name it. The rule " +
                 "itself is band-based (NearMiss.cs): exactly one other shape inside the lock band, " +
                 "the target outside it. This margin only rejects a near-tie, which the shipped set " +
                 "cannot produce - every pair is at least 26 deg apart.")]
        public float nearMissMarginDeg = 2f;

        // ---- events ----
        public event Action<Band> OnBandChanged;
        public event Action<string> OnLocked;                  // shape id
        public event Action<string> OnUnlocked;                // shape id — the latch released
        public event Action<string, float> OnNearMiss;         // shape id you actually made, its distance
        public event Action<bool> OnPausedChanged;             // true when tracking is too weak to judge
        public event Action<float, float> OnDistanceUpdated;   // current distance, best-other distance
        public event Action<float[]> OnFingerAnglesUpdated;    // per-finger angles, for the overlay

        // ---- state ----
        float[] _fingerAngles = new float[PoseMath.FingerChainEnd.Length];
        float[] _jointAngles = new float[JointNames.Canonical.Length];   // P2.2: per-joint, for the overlay
        readonly HoldTimer _hold = new HoldTimer();
        float _hysteresisTimer, _nearMissCooldown, _goodConfidenceTimer;
        string _nearMissCandidateId;
        float _nearMissCandidateSeconds;
        string[] _shapeIds;
        float[] _shapeDegrees;
        Band _band = Band.Warming, _pendingBand = Band.Warming;
        bool _locked, _paused;
        float _lastConfidence = 1f;
        HandPose _target;

        public Band CurrentBand => _band;

        /// <summary>What the hold is doing right now: Idle, Holding, Coasting, Stalled, Locked.
        /// Named Phase rather than State so it cannot be confused with the type in expressions.</summary>
        public HoldState HoldPhase { get; private set; }

        /// <summary>Seconds of in-band time banked towards the lock. The overlay's progress bar.</summary>
        public float HoldSecondsHeld { get; private set; }
        public bool IsPaused => _paused;
        public bool IsLocked => _locked;
        public float HoldProgress01 => _hold.Progress01;
        public float[] FingerAngles => _fingerAngles;
        public float LastConfidence => _lastConfidence;

        /// <summary>
        /// Which non-target shape is currently closest, and how close. Null until the first tick.
        ///
        /// P1.5 needs this: "does it track as a different shape" is the worst failure in
        /// TRACKABILITY.md, and the near-miss event cannot answer it because it is rate-limited to
        /// one per 20 seconds. Exposing the live nearest neighbour makes the probe able to say
        /// "confused with U for 4.2 of 10 seconds" instead of "a near-miss fired once".
        /// </summary>
        /// <summary>
        /// The live distance in DEGREES: the worst finger's weighted mean (P2.2). This is the number
        /// the bands use and the only one the user should ever be shown, if any.
        /// </summary>
        public float DistanceDegrees { get; private set; } = float.PositiveInfinity;

        /// <summary>
        /// The flat weighted mean across every weighted joint. Reporting only - it is the trap number,
        /// shown beside D so that "one finger very wrong" cannot be misread as "the shape is close".
        /// </summary>
        public float FlatDegrees { get; private set; } = float.PositiveInfinity;

        /// <summary>Canonical indices of the three worst-contributing joints this frame, worst first.</summary>
        public readonly int[] WorstJointIndices = { -1, -1, -1 };

        /// <summary>Their angles in degrees, parallel to <see cref="WorstJointIndices"/>.</summary>
        public readonly float[] WorstJointDegrees = { 0f, 0f, 0f };

        /// <summary>Their weights, so the overlay can show the contribution rather than just the angle.</summary>
        public readonly float[] WorstJointWeights = { 0f, 0f, 0f };

        /// <summary>`O`: this frame's palm-facing error, in degrees, independent of the finger angles.</summary>
        public float OrientationDegrees { get; private set; }

        /// <summary>What the palm term added to the distance this frame: `k * max(0, O - slack)`.</summary>
        public float OrientationPenaltyDegrees { get; private set; }

        /// <summary>How long the current candidate shape has held. The second half of the stable window.</summary>
        public float NearMissCandidateSeconds => _nearMissCandidateSeconds;

        /// <summary>Near-misses delivered this session. The rate cap keeps it low; this proves it.</summary>
        public int NearMissesThisSession { get; private set; }

        /// <summary>
        /// Whether the app may name shapes at all. Persistent: this is the switch P6.5 surfaces as the
        /// sixth accessibility toggle, so it is stored with the save data and not just on the component.
        /// </summary>
        public void SetNearMissEnabled(bool enabled)
        {
            nearMissEnabled = enabled;
            if (save != null)
            {
                save.Data.nearMissEnabled = enabled;
                save.Save();
            }
        }

        /// <summary>
        /// The off switch as a property. Assigning goes through <see cref="SetNearMissEnabled"/> so a
        /// toggle written by the in-space switch (P3.2/P6.5) is persisted, not just held in memory.
        /// </summary>
        public bool NearMissEnabled
        {
            get => nearMissEnabled;
            set => SetNearMissEnabled(value);
        }

        /// <summary>Optional. When assigned, the off switch survives a relaunch (P3.2, used by P6.5).</summary>
        public SaveSystem save;

        /// <summary>
        /// This frame's near-miss verdict, including the reason when it stays silent. Exposed so the
        /// overlay and the tests can tell "nothing to say" from "we chose not to speak".
        /// </summary>
        public NearMissDecision NearMissState { get; private set; }

        /// <summary>Palm tilt and roll, for the overlay. Diagnostics only - never part of the distance.</summary>
        public float PalmSwingDegrees { get; private set; }
        public float PalmTwistDegrees { get; private set; }

        /// <summary>Which basis the palm comparison used this frame.</summary>
        public PalmBasisSource PalmBasis { get; private set; }

        /// <summary>Why the knuckle plane was or was not used. Overlay only.</summary>
        public string BasisNote { get; private set; } = "not evaluated";

        public string BestOtherId { get; private set; }

        /// <summary>Distance in degrees to <see cref="BestOtherId"/>.</summary>
        public float BestOtherDegrees { get; private set; } = float.PositiveInfinity;

        void Awake()
        {
            // nothing to allocate: every buffer is fixed size and reused
            if (weights == null)
            {
                weights = JointWeights.Default();
                Debug.LogWarning("[Matcher] no JointWeights asset assigned; using the documented " +
                                 "defaults. Assign Assets/_Project/Matching/Weights.asset so the " +
                                 "weights can be tuned without a recompile (P2.2).");
            }
            SyncHoldTimer();

            string problem = weights.Validate();
            if (!string.IsNullOrEmpty(problem))
                Debug.LogError("[Matcher] JointWeights are unusable (" + problem + "); every distance " +
                               "will be wrong, and probably zero.");
        }

        void Start()
        {
            // P3.2: the near-miss off switch is a setting, not a session whim. Read in Start, not Awake,
            // because SaveSystem loads its file in Awake and the order between two components is not
            // guaranteed - reading first would silently restore the default over the user's choice.
            // P6.5 surfaces this switch in-space as the sixth accessibility toggle.
            if (save != null && save.Data != null) nearMissEnabled = save.Data.nearMissEnabled;
        }

        /// <summary>Push the inspector values onto the state machine. Called in Awake and on validate.</summary>
        public void SyncHoldTimer()
        {
            _hold.holdSeconds = holdSeconds;
            _hold.dropoutGraceSeconds = dropoutGraceSeconds;
            _hold.stallDecayPerSecond = stallDecayPerSecond;
            _hold.outOfBandForgetSeconds = resetAfterSeconds;
        }

        void OnValidate() => SyncHoldTimer();

        public void SetTarget(string shapeId)
        {
            targetShapeId = shapeId;
            _target = shapes.Find(s => s.Id == shapeId);
            _hold.Reset();
            _locked = false;
            if (_target == null) Debug.LogError($"[Matcher] target '{shapeId}' not found in the shape set");
        }

        /// <summary>Call once per frame from the adapter.</summary>
        public void Tick(Quaternion[] localRotations, float confidence, float dt)
        {
            if (_target == null) return;

            _lastConfidence = confidence;
            bool trustworthy = confidence >= confidenceFloor;
            SetPaused(!trustworthy);

            float d = PoseMath.Distance(weights, _target.LocalRotations, localRotations,
                                        _fingerAngles, _jointAngles,
                                        out float wristAngle, out float flatDegrees);
            DistanceDegrees = d;
            FlatDegrees = flatDegrees;
            UpdateWorstJoints();

            // Palm orientation is a gate, not a score. Rotating the wrist the wrong
            // way has to break the match even when every finger is perfect, but the
            // wrist error is not comparable with a finger error, so it is applied
            // as a slack on its own term rather than averaged into the distance.
            // Palm facing is now its own term (P2.6): a basis comparison, additive, not a max gate.
            //
            // The old rule was `d = max(d, wristError - slack)`, which made orientation a *floor* - so a
            // hand with perfect fingers and a 40 deg wrong facing read the same distance as one with
            // perfect fingers and 12 deg of wrong facing once both were inside the floor. Additive is the
            // behaviour the task wants: the facing error is added to the shape error, weighted, so the
            // two contribute independently and neither can hide the other.
            float orientation = PalmOrientation.ErrorDegrees(_target.LocalRotations, localRotations);
            PalmOrientation.TryBasis(localRotations, out _, out PalmBasisSource basisSource, out string basisNote);
            BasisNote = basisNote;
            PalmOrientation.Components(_target.LocalRotations, localRotations,
                                       out float palmSwing, out float palmTwist);
            PalmSwingDegrees = palmSwing;
            PalmTwistDegrees = palmTwist;
            PalmBasis = basisSource;

            float orientationPenalty = orientationWeightK * Mathf.Max(0f, orientation - orientationSlackDeg);
            if (orientationPenaltyCapDeg > 0f) orientationPenalty = Mathf.Min(orientationPenalty, orientationPenaltyCapDeg);

            OrientationDegrees = orientation;
            OrientationPenaltyDegrees = orientationPenalty;
            d += orientationPenalty;

            // Distance to EVERY shape, not "the target and then a guess at the nearest". 12 shapes x 26
            // joints is nothing, and it is what makes the near-miss able to say which shape the hand
            // actually made (P3.1).
            if (_shapeIds == null || _shapeIds.Length != shapes.Count)
            {
                _shapeIds = new string[shapes.Count];
                _shapeDegrees = new float[shapes.Count];
            }
            for (int i = 0; i < shapes.Count; i++)
            {
                var s = shapes[i];
                _shapeIds[i] = s.Id;
                _shapeDegrees[i] = (s.Id == _target.Id || s.JointCount != _target.JointCount)
                    ? float.PositiveInfinity
                    : PoseMath.Distance(weights, s.LocalRotations, localRotations, null, null, out _, out _);
            }

            float bestOther = float.PositiveInfinity;
            string bestOtherId = null;
            for (int i = 0; i < shapes.Count; i++)
            {
                if (_shapeIds[i] == _target.Id) continue;
                if (_shapeDegrees[i] < bestOther) { bestOther = _shapeDegrees[i]; bestOtherId = _shapeIds[i]; }
            }
            BestOtherId = bestOtherId;
            BestOtherDegrees = bestOther;
            OnDistanceUpdated?.Invoke(d, bestOther);
            OnFingerAnglesUpdated?.Invoke(_fingerAngles);

            // ---- the hold (P2.4) ----------------------------------------------------------
            // Ticked BEFORE the trustworthy check below, because a tracking dropout has to reach the
            // hold timer: freezing progress through a dip is the whole point, and a timer that is only
            // ticked on good frames would be reset by omission rather than by decision.
            var hold = _hold.Tick(d, trustworthy, lockedDeg, lockedDeg + bandReleaseDeg, dt);
            HoldPhase = hold.State;
            HoldSecondsHeld = hold.HeldSeconds;

            if (hold.LockedNow)
            {
                _locked = true;
                OnLocked?.Invoke(_target.Id);
            }
            else if (hold.UnlockedNow)
            {
                _locked = false;
                OnUnlocked?.Invoke(_target.Id);
            }

            // ---- bands, with hysteresis ----
            // While tracking is untrustworthy the band is FROZEN. A dropout is not a verdict (ADR-003:
            // pause rather than penalise), and letting a lost frame move the display is the cheapest
            // way to produce a strobe that no time constant can hide: the distance is garbage, so the
            // band that follows it is garbage too. The hand coming back re-enters on its own merits.
            if (!trustworthy)
            {
                _pendingBand = _band;
                _hysteresisTimer = 0f;
                return;
            }

            Band wanted = (Band)PoseMath.Band(d, lockedDeg, closeDeg, bandReleaseDeg, (int)_band);
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

            // ---- near-miss ----
            _goodConfidenceTimer = trustworthy ? _goodConfidenceTimer + dt : 0f;
            _nearMissCooldown = Mathf.Max(0f, _nearMissCooldown - dt);

            // The rule lives in NearMiss.cs; this decides only whether the app is allowed to speak yet.
            var nearMiss = NearMiss.Evaluate(d, _target.Id, _shapeIds, _shapeDegrees, shapes.Count,
                                             lockedDeg, nearMissMarginDeg);
            NearMissState = nearMiss;

            // Stability of the DECISION, not just of the tracking (P3.2's trap). A hand sweeping through
            // the set satisfies the rule for a frame or two on every shape it passes; requiring the same
            // shape to hold for the stable window is what stops that from becoming a stream of names.
            if (nearMiss.Fires && nearMiss.ShapeId == _nearMissCandidateId)
            {
                _nearMissCandidateSeconds += dt;
            }
            else
            {
                _nearMissCandidateId = nearMiss.Fires ? nearMiss.ShapeId : null;
                _nearMissCandidateSeconds = nearMiss.Fires ? dt : 0f;
            }

            if (nearMissEnabled && !_locked
                && _band != Band.Locked
                && nearMiss.Fires
                && _goodConfidenceTimer >= nearMissStableSeconds
                && _nearMissCandidateSeconds >= nearMissStableSeconds
                && _nearMissCooldown <= 0f)
            {
                _nearMissCooldown = nearMissCooldownSeconds;
                NearMissesThisSession++;
                OnNearMiss?.Invoke(nearMiss.ShapeId, nearMiss.Degrees);
            }
        }

        /// <summary>
        /// P2.2: the three joints contributing most to the distance this frame, worst first.
        ///
        /// Contribution, not raw angle. A 20 deg error on a 0.4-weight metacarpal is a smaller part of
        /// what the user is being judged on than a 12 deg error on a 1.0-weight distal joint, and the
        /// overlay has to name the thing that is actually holding the lock back. Ties are broken by
        /// canonical index so the display does not flicker between two equal joints.
        /// </summary>
        void UpdateWorstJoints()
        {
            for (int i = 0; i < WorstJointIndices.Length; i++)
            {
                WorstJointIndices[i] = -1;
                WorstJointDegrees[i] = 0f;
                WorstJointWeights[i] = 0f;
            }
            if (weights == null) return;

            for (int j = 0; j < _jointAngles.Length; j++)
            {
                float w = weights.Weight(j);
                if (w <= 0f) continue;
                float contribution = w * _jointAngles[j];

                for (int slot = 0; slot < WorstJointIndices.Length; slot++)
                {
                    float slotContribution = WorstJointIndices[slot] < 0
                        ? -1f
                        : WorstJointWeights[slot] * WorstJointDegrees[slot];
                    if (contribution <= slotContribution) continue;

                    // shift the rest down one slot
                    for (int k = WorstJointIndices.Length - 1; k > slot; k--)
                    {
                        WorstJointIndices[k] = WorstJointIndices[k - 1];
                        WorstJointDegrees[k] = WorstJointDegrees[k - 1];
                        WorstJointWeights[k] = WorstJointWeights[k - 1];
                    }
                    WorstJointIndices[slot] = j;
                    WorstJointDegrees[slot] = _jointAngles[j];
                    WorstJointWeights[slot] = w;
                    break;
                }
            }
        }

        /// <summary>The worst joints as "Name 12.4° (w 1.00)", for the overlay and the log.</summary>
        public string DescribeWorstJoints()
        {
            var sb = new System.Text.StringBuilder(64);
            for (int i = 0; i < WorstJointIndices.Length; i++)
            {
                int index = WorstJointIndices[i];
                if (index < 0) continue;
                if (sb.Length > 0) sb.Append("  ");
                sb.Append(JointNames.Canonical[index]).Append(' ')
                  .Append(WorstJointDegrees[i].ToString("0.0")).Append("\u00b0");
                sb.Append(" (w ").Append(WorstJointWeights[i].ToString("0.00")).Append(')');
            }
            return sb.ToString();
        }

        void SetPaused(bool paused)
        {
            if (paused == _paused) return;
            _paused = paused;
            OnPausedChanged?.Invoke(_paused);
        }
    }
}
