using UnityEngine;
using Signspace.Matching;

namespace Signspace.Hands
{
    /// <summary>
    /// The 30-second hand calibration (P6.4), and the thing it is really for: comparing the user to
    /// their own hand instead of to a canonical one.
    ///
    /// THE INVERSION
    /// -------------
    /// Every system in this category compares the user to a model hand and fails if the user's range
    /// is smaller - arthritis, an injury, a missing finger, a splint. We invert it: each finger's
    /// measured range becomes the scale that finger is judged on, and the *target* pose is measured on
    /// the same scale. A user who can curl 40 degrees gets the same 0-degree "you made it" reading as
    /// one who can curl 80.
    ///
    /// WHY THE MEASUREMENT IS THE MATCHER'S OWN ANGLES
    /// -----------------------------------------------
    /// The alternative was a second angle extractor inside this class, and then there would be two
    /// definitions of "how curled is your index finger" that could disagree. `Matcher.FingerAngles` is
    /// already the weighted mean the match uses, so measuring it means the calibration cannot be
    /// measuring a quantity the matcher does not use.
    ///
    /// WHAT IT DOES NOT DO
    /// -------------------
    /// It does not diagnose, score, or advise. It measures a range, and it says so. There is no
    /// clinical claim anywhere in this file or in the doc, by design (P6.4 trap).
    /// </summary>
    public class Calibration : MonoBehaviour
    {
        public enum Step
        {
            Idle = 0,
            Open = 1,
            Fist = 2,
            Thumb = 3,
            Done = 4,
        }

        public Matcher matcher;
        public SaveSystem save;

        [Tooltip("Optional: shown the neutral hand while the flow asks for an open one.")]
        public ReferenceHand referenceHand;

        [Header("Timing — 22 s of guidance, under the 30 s the task allows")]
        public float openSeconds = 6f;
        public float fistSeconds = 6f;
        public float thumbSeconds = 10f;

        [Tooltip("Used only if the shape set has not loaded; the model's own maximum is preferred.")]
        public float fallbackCanonicalDegrees = 90f;

        public Step Current { get; private set; } = Step.Idle;
        public bool IsRunning => Current != Step.Idle && Current != Step.Done;
        public bool HasResult { get; private set; }

        /// <summary>0..1 through the whole flow. Drives the caption's progress bar.</summary>
        public float Progress01
        {
            get
            {
                float total = openSeconds + fistSeconds + thumbSeconds;
                if (total <= 0f) return 0f;
                float before = Current == Step.Fist ? openSeconds
                             : Current == Step.Thumb ? openSeconds + fistSeconds
                             : Current == Step.Done ? total
                             : 0f;
                return Mathf.Clamp01((before + _stepTimer) / total);
            }
        }

        public float StepSecondsLeft => Mathf.Max(0f, _stepLength - _stepTimer);

        public string Prompt
        {
            get
            {
                switch (Current)
                {
                    case Step.Open: return "Open your hand wide and hold it.";
                    case Step.Fist: return "Make a fist, as tight as is comfortable.";
                    case Step.Thumb: return "Touch your thumb to each fingertip, in turn.";
                    case Step.Done: return "Calibrated. Your range is now the reference.";
                    default: return HasResult ? "Calibrated" : "Not calibrated — the default path is on.";
                }
            }
        }

        /// <summary>Per-finger multipliers applied to the matcher, 1 = no change.</summary>
        public float[] Scales { get; private set; } = new float[5];
        public float[] Canonical { get; private set; } = new float[5];
        public float[] MeasuredMin { get; private set; } = new float[5];
        public float[] MeasuredMax { get; private set; } = new float[5];

        /// <summary>How much the lock band was widened. 1 = not at all.</summary>
        public float BandScale { get; private set; } = 1f;

        public string Report =>
            "calibration: " + (HasResult ? "valid" : "none") +
            (HasResult
                ? " range " + RangeSummary() + " band x" + BandScale.ToString("0.00")
                : "");

        float _stepTimer;
        float _stepLength;
        bool _measuring;

        void Start()
        {
            if (matcher == null) matcher = FindFirstObjectByType<Matcher>();
            if (save == null) save = FindFirstObjectByType<SaveSystem>();
            Restore();
        }

        /// <summary>Re-applies a calibration stored in the save file. Safe to call at any time.</summary>
        public void Restore()
        {
            if (matcher == null) matcher = FindFirstObjectByType<Matcher>();
            if (save == null) save = FindFirstObjectByType<SaveSystem>();
            if (matcher == null || save == null || save.Data == null) return;

            var record = save.Data.calibration;
            if (record == null || !record.HasUsableRange)
            {
                // A save that says calibrated but holds no usable range is a bug, not a calibration.
                // Matcher.bandScale stays 1 and the default path runs - never a half-applied profile.
                matcher.SetFingerNormalisation(null, 1f);
                HasResult = false;
                return;
            }

            Canonical = (float[])record.canonical.Clone();
            MeasuredMin = (float[])record.min.Clone();
            MeasuredMax = (float[])record.max.Clone();
            ComputeScales();
            matcher.SetFingerNormalisation(Scales, BandScale);
            HasResult = true;
            Current = Step.Done;
            Debug.Log("[Calibration] restored: " + Report);
        }

        public void Begin()
        {
            if (matcher == null) matcher = FindFirstObjectByType<Matcher>();
            if (matcher == null)
            {
                Debug.LogWarning("[Calibration] no matcher; cannot measure.");
                return;
            }

            // Measure the user's raw range, not their range through a previous calibration, or the
            // second run would compound the first.
            matcher.SetFingerNormalisation(null, 1f);
            HasResult = false;

            for (int i = 0; i < 5; i++)
            {
                MeasuredMin[i] = 999f;
                MeasuredMax[i] = -999f;
            }

            _measuring = true;
            Enter(Step.Open);
            if (referenceHand != null) referenceHand.ShowNeutral();
            Debug.Log("[Calibration] started — " + Prompt);
        }

        /// <summary>Bail out. The default path is untouched, which is the task's "skippable" criterion.</summary>
        public void Skip()
        {
            if (!_measuring)
            {
                Current = Step.Idle;
                return;
            }

            _measuring = false;
            Current = Step.Idle;
            if (matcher != null) matcher.SetFingerNormalisation(null, 1f);
            if (save != null && save.Data != null)
            {
                save.Data.calibrated = false;
                save.Data.calibration = new CalibrationRecord();
            }
            Debug.Log("[Calibration] skipped; the default path is unchanged.");
        }

        public void Rerun() => Begin();

        void Update()
        {
            if (!_measuring || matcher == null) return;

            MeasureFrame();

            _stepTimer += Time.deltaTime;
            if (_stepTimer < _stepLength) return;

            switch (Current)
            {
                case Step.Open: Enter(Step.Fist); break;
                case Step.Fist: Enter(Step.Thumb); break;
                case Step.Thumb: Finish(); break;
            }
        }

        void Enter(Step step)
        {
            Current = step;
            _stepTimer = 0f;
            _stepLength = step == Step.Open ? openSeconds : step == Step.Fist ? fistSeconds : thumbSeconds;
        }

        void MeasureFrame()
        {
            var angles = matcher.FingerAngles;
            if (angles == null) return;
            int n = Mathf.Min(5, angles.Length);
            for (int i = 0; i < n; i++)
            {
                float a = angles[i];
                if (a < MeasuredMin[i]) MeasuredMin[i] = a;
                if (a > MeasuredMax[i]) MeasuredMax[i] = a;
            }
        }

        void Finish()
        {
            _measuring = false;
            Current = Step.Done;

            Canonical = CanonicalFromShapeSet();
            ComputeScales();

            HasResult = true;
            matcher.SetFingerNormalisation(Scales, BandScale);

            if (save != null && save.Data != null)
            {
                save.Data.calibration = new CalibrationRecord
                {
                    valid = true,
                    min = (float[])MeasuredMin.Clone(),
                    max = (float[])MeasuredMax.Clone(),
                    canonical = (float[])Canonical.Clone(),
                    capturedUtc = System.DateTime.UtcNow.ToString("o"),
                };
                save.Data.calibrated = true;
                save.Save();
            }

            Debug.Log("[Calibration] " + Report);
        }

        void ComputeScales()
        {
            Scales = ComputeScales(MeasuredMin, MeasuredMax, Canonical, fallbackCanonicalDegrees,
                                   out float band);
            BandScale = band;
        }

        /// <summary>
        /// The calibration math, as a pure function so the check exercises the same code the flow runs.
        ///
        /// A finger whose measured span is half the model's gets a scale of 2: its rotation is doubled
        /// before comparison, so the half it can make reads as the whole the model asks for. The cap
        /// keeps a nearly-immobile finger from being declared a match on noise, and the floor keeps a
        /// hand that moves farther than the model from being judged more harshly than the model.
        /// </summary>
        public static float[] ComputeScales(float[] min, float[] max, float[] canonical,
                                            float fallbackCanonical, out float bandScale)
        {
            var scales = new float[5];
            float sum = 0f;

            for (int i = 0; i < 5; i++)
            {
                float lo = min != null && i < min.Length ? min[i] : 0f;
                float hi = max != null && i < max.Length ? max[i] : 0f;
                float range = Mathf.Max(4f, hi - lo);
                float target = Mathf.Max(1f, canonical != null && i < canonical.Length && canonical[i] > 0.5f
                                                 ? canonical[i] : fallbackCanonical);

                scales[i] = Mathf.Clamp(target / range, 1f, 2.5f);
                sum += scales[i];
            }

            // Widening the band compensates for the noise that the stretch amplifies. Square root because
            // the band already carries a margin: doubling it would lock on a nearby shape.
            float mean = sum / 5f;
            bandScale = Mathf.Clamp(Mathf.Sqrt(mean), 1f, 1.35f);
            return scales;
        }

        float[] CanonicalFromShapeSet()
        {
            var max = new float[5];
            for (int i = 0; i < 5; i++) max[i] = 0f;

            if (matcher != null && matcher.shapes != null && matcher.shapes.Count > 0)
            {
                int length = 0;
                for (int i = 0; i < matcher.shapes.Count; i++)
                    if (matcher.shapes[i] != null) length = Mathf.Max(length, matcher.shapes[i].JointCount);

                if (length > 0)
                {
                    var neutral = new Quaternion[length];
                    for (int j = 0; j < length; j++) neutral[j] = Quaternion.identity;

                    for (int s = 0; s < matcher.shapes.Count; s++)
                    {
                        var pose = matcher.shapes[s];
                        if (pose == null || pose.JointCount != length) continue;
                        for (int f = 0; f < 5; f++)
                        {
                            float a = PoseMath.ChainAngle(pose.LocalRotations, neutral,
                                                          PoseMath.FingerChainEnd[f]);
                            if (!float.IsInfinity(a) && a > max[f]) max[f] = a;
                        }
                    }
                }
            }

            for (int f = 0; f < 5; f++)
                if (max[f] < 1f) max[f] = fallbackCanonicalDegrees;

            return max;
        }

        string RangeSummary()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 5; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(PoseMath.FingerNames[i]).Append(' ')
                  .Append((MeasuredMax[i] - MeasuredMin[i]).ToString("0"));
            }
            sb.Append(" deg");
            return sb.ToString();
        }
    }
}
