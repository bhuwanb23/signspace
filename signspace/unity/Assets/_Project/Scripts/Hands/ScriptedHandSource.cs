using System;
using System.Collections.Generic;
using UnityEngine;
using Signspace.Core;

namespace Signspace.Matching
{
    /// <summary>
    /// A hand that comes from a pose file instead of a headset (P10 follow-up).
    ///
    /// WHY THIS EXISTS
    /// ---------------
    /// Every row that needs a headset is a row nobody can run at a desk: the shelf, the lock, the
    /// near-miss and the probe all sit behind "put the device on". Most of that chain is not
    /// device-dependent at all - it is pose in, band out - and the only thing missing was something
    /// to put a pose in. This is that something.
    ///
    /// WHAT IT DOES NOT PROVE
    /// ----------------------
    /// It cannot prove tracking quality, comfort, frame rate or thermal behaviour, and it must never
    /// be used to fill a device row in TRACKABILITY.md. It drives the *same* Matcher the app drives,
    /// with the *same* pose files the app loads, so what it does prove is real: the wiring, the band
    /// logic, the hold, the latch, the near-miss and the probe's logging all run without an SDK.
    ///
    /// WHY IT WRITES localRotation DIRECTLY
    /// ------------------------------------
    /// SkeletonPoseSource reads <c>joints[i].localRotation</c> each frame (see its sampling loop), so a
    /// scripted hand only has to be a canonical-ordered Transform[] whose local rotations are the pose
    /// file's rotations. Parenting and bone length are irrelevant to the matcher and are left to
    /// whatever visual the scene already has. That is also why this component can be verified in
    /// batch mode: no rig, no SDK, no headset.
    ///
    /// SAFETY
    /// ------
    /// Dev-only. It disables itself in a release build rather than deleting itself, so a scene that
    /// accidentally keeps the component still ships a hand-tracking app and not a puppet show.
    /// </summary>
    [DefaultExecutionOrder(90)]   // before SkeletonPoseSource (100) samples the joints
    public class ScriptedHandSource : MonoBehaviour
    {
        [Header("Wiring")]
        [Tooltip("Required. The matcher this hand feeds.")]
        public Matcher matcher;

        [Tooltip("Optional. If set, the canonical Transforms are bound to it so the drawn hand follows " +
                 "the scripted pose. Leave empty in batch runs that tick the matcher directly.")]
        public SkeletonPoseSource poseSource;

        [Tooltip("Tick the matcher from here. Turn off when a SkeletonPoseSource is doing it, or the " +
                 "hand is ticked twice per frame and the hold runs at double speed.")]
        public bool driveMatcherDirectly = true;

        [Header("What to perform")]
        [Tooltip("Shape id from the loaded set. Empty means the matcher's current target.")]
        public string shapeId = "";

        [Tooltip("Blend from a flat hand into the shape. 1 = start already in the pose.")]
        [Range(0f, 1f)] public float approach = 1f;

        [Tooltip("Seconds to reach the pose. 0 with approach 1 means an instant snap.")]
        public float approachSeconds = 0f;

        [Tooltip("Cycle every shape in the set, holding each for secondsPerShape.")]
        public bool cycleShapes = false;

        public float secondsPerShape = 3f;

        [Header("Realism (optional)")]
        [Tooltip("Per-joint angular noise, in degrees, matching scripts/noise_margin_sim.py. 0 = a " +
                 "perfect hand, which no headset will ever give you.")]
        [Range(0f, 6f)] public float noiseDeg = 0f;

        [Tooltip("Deterministic seed: the same run must produce the same numbers or it is not evidence.")]
        public int seed = 7;

        [Header("Reporting")]
        public bool verboseLogging = true;

        /// <summary>Raised when the driver moves to a new shape. Batch harnesses subscribe to this.</summary>
        public event Action<string> OnShapeStarted;

        /// <summary>The pose currently being performed, or null before binding.</summary>
        public HandPose CurrentPose => _pose;

        public Band CurrentBand => matcher != null ? matcher.CurrentBand : Band.Warming;

        public bool IsLocked => matcher != null && matcher.IsLocked;

        Transform[] _joints;
        Quaternion[] _buffer;
        HandPose _pose;
        System.Random _rng;
        float _elapsed;
        float _cycleClock;
        int _cycleIndex;
        bool _bound;
        bool _warnedNoMatcher;

        void Awake()
        {
            if (!DevOnly.Allowed)
            {
                // A release build keeps the component and loses the puppet.
                enabled = false;
                return;
            }

            BuildJoints();
        }

        void Start()
        {
            if (!enabled) return;

            if (matcher == null) matcher = FindFirstObjectByType<Matcher>();
            if (matcher == null)
            {
                Debug.LogError("[ScriptedHandSource] no Matcher in the scene. This driver needs one.");
                enabled = false;
                return;
            }

            Bind();
        }

        void Update()
        {
            if (!_bound) return;
            Step(Time.deltaTime);
        }

        // ---- public surface, used by the editor harness as well as by Update ----------------

        /// <summary>
        /// Binds the pose set and the transforms. Safe to call twice; the second call is a no-op.
        /// </summary>
        public void Bind()
        {
            if (_bound) return;
            if (matcher == null)
            {
                if (!_warnedNoMatcher)
                {
                    _warnedNoMatcher = true;
                    Debug.LogError("[ScriptedHandSource] cannot bind without a Matcher.");
                }
                return;
            }

            if (_joints == null) BuildJoints();
            _rng = new System.Random(seed);

            if (poseSource != null)
            {
                poseSource.BindCanonical(_joints, "ScriptedHandSource");
            }

            _bound = true;

            string wanted = string.IsNullOrEmpty(shapeId) ? matcher.targetShapeId : shapeId;
            SetShape(wanted);

            if (verboseLogging)
            {
                Debug.Log("[ScriptedHandSource] bound " + _joints.Length + " canonical joints; " +
                          "shapes available: " + string.Join(", ", ShapeIds()) +
                          "; performing '" + (_pose != null ? _pose.Id : "none") + "'" +
                          (noiseDeg > 0f ? " with " + noiseDeg.ToString("0.0") + " deg/joint noise" : ""));
            }
        }

        /// <summary>The shape ids the matcher actually loaded. Never a hardcoded list.</summary>
        public List<string> ShapeIds()
        {
            var ids = new List<string>();
            if (matcher == null) return ids;
            foreach (var shape in matcher.shapes)
            {
                if (shape != null) ids.Add(shape.Id);
            }
            return ids;
        }

        /// <summary>Switch to a shape by id. Also moves the matcher's target, which is what a user's shelf click does.</summary>
        public void SetShape(string id)
        {
            if (matcher == null) return;
            var found = matcher.shapes.Find(s => s != null && s.Id == id);
            if (found == null)
            {
                Debug.LogError("[ScriptedHandSource] shape '" + id + "' is not in the matcher's set (" +
                               string.Join(", ", ShapeIds()) + ").");
                return;
            }

            _pose = found;
            matcher.SetTarget(found.Id);
            _elapsed = 0f;
            _cycleClock = 0f;

            if (verboseLogging) Debug.Log("[ScriptedHandSource] performing '" + found.Id + "'");
            OnShapeStarted?.Invoke(found.Id);
        }

        /// <summary>
        /// Advances the scripted hand by <paramref name="dt"/> and (optionally) ticks the matcher.
        /// Batch harnesses call this with a fixed step so the results do not depend on frame pacing.
        /// </summary>
        public Band Step(float dt)
        {
            if (!_bound || _pose == null || matcher == null) return Band.Warming;

            _elapsed += dt;

            if (cycleShapes)
            {
                _cycleClock += dt;
                if (_cycleClock >= secondsPerShape)
                {
                    _cycleClock = 0f;
                    var ids = ShapeIds();
                    if (ids.Count > 0)
                    {
                        _cycleIndex = (_cycleIndex + 1) % ids.Count;
                        SetShape(ids[_cycleIndex]);
                    }
                }
            }

            float alpha = approach;
            if (approachSeconds > 0f)
            {
                float t = Mathf.Clamp01(_elapsed / approachSeconds);
                alpha *= t * t * (3f - 2f * t);          // smoothstep: no velocity jump at either end
            }

            Apply(alpha);

            if (driveMatcherDirectly)
            {
                matcher.Tick(_buffer, 1f, dt);
            }
            return matcher.CurrentBand;
        }

        // ---- internals --------------------------------------------------------------------

        void BuildJoints()
        {
            int count = JointNames.Canonical.Length;
            _joints = new Transform[count];
            _buffer = new Quaternion[count];

            var root = new GameObject("ScriptedHand").transform;
            root.SetParent(transform, false);
            root.name = "ScriptedHandJoints";

            for (int i = 0; i < count; i++)
            {
                // Flat, not a bone chain: the matcher reads localRotation only, and a chain would make
                // each joint's local rotation depend on its parent's, which the pose files do not assume.
                var go = new GameObject(i.ToString("00") + "_" + JointNames.Canonical[i]);
                go.transform.SetParent(root, false);
                go.transform.localRotation = Quaternion.identity;
                _joints[i] = go.transform;
                _buffer[i] = Quaternion.identity;
            }
        }

        void Apply(float alpha)
        {
            var target = _pose.LocalRotations;
            int count = Mathf.Min(_joints.Length, target != null ? target.Length : 0);

            for (int i = 0; i < _joints.Length; i++)
            {
                Quaternion q = i < count ? Quaternion.Slerp(Quaternion.identity, target[i], alpha) : Quaternion.identity;

                // The wrist is left unrotated on purpose: the matcher compares the hand's pose around
                // the wrist, and rotating the root would simulate a turned hand, which is a different
                // test (P1.3's wrist-rotation trial, not this one).
                if (noiseDeg > 0f && JointNames.Canonical[i] != "Wrist")
                {
                    q = q * RandomRotation(noiseDeg);
                }

                _joints[i].localRotation = q;
                _buffer[i] = q;
            }
        }

        Quaternion RandomRotation(float sigmaDeg)
        {
            double x = NextGaussian(), y = NextGaussian(), z = NextGaussian();
            var axis = new Vector3((float)x, (float)y, (float)z);
            if (axis.sqrMagnitude < 1e-8f) axis = Vector3.right;
            return Quaternion.AngleAxis((float)NextGaussian() * sigmaDeg, axis.normalized);
        }

        double NextGaussian()
        {
            // Box-Muller. System.Random is used rather than UnityEngine.Random so the seed is
            // independent of anything else in the scene that draws random numbers.
            double u1 = 1.0 - _rng.NextDouble();
            double u2 = _rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }
    }
}
