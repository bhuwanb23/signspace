using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>
    /// The glue between Meta's hand tracking and everything in this project that wants a hand.
    ///
    /// WHY THIS FILE EXISTS
    /// --------------------
    /// P1.1 found the project had no bridge at all: SkeletonPoseSource.AutoBind() had zero
    /// callers, and no component implemented IHandStateProvider, so ReferenceHand.IsPinching()
    /// always returned false and grab could never fire. This is that bridge.
    ///
    /// WHY REFLECTION INSTEAD OF A DIRECT OVRSkeleton REFERENCE
    /// ------------------------------------------------------
    /// SkeletonPoseSource's own header states the rule: "a v208 upgrade that renames an enum
    /// member should cost us zero build time, and this file should still compile". A direct
    /// `using Oculus.Interaction.Input;` breaks that rule twice over - the type moved between
    /// namespaces across SDK versions, and the project would stop compiling if the SDK were
    /// absent. So the SDK is touched through cached reflection, and the rest of the project stays
    /// a pure C# program that happens to run inside Unity.
    ///
    /// The cost is a reflection lookup, ONCE, at bind time. PropertyInfo reads are cached and the
    /// per-frame path is array copies - the same work a direct reference would do.
    ///
    /// The honest downside: a renamed SDK member now fails at RUNTIME with a clear message instead
    /// of a compile error. That trade is why the failure is logged loudly and the report is dumped
    /// to a file the owner can pull off the headset.
    ///
    /// THE 20-BONE TRAP
    /// ----------------
    /// P1.1's own trap list says: "re-skinning 26 joints to a 20-bone rig and silently mangling the
    /// fingers". The legacy Oculus hand has no palm joint and 4 bones per finger; the OpenXR hand
    /// has a palm and 5. Rather than assume, this component REPORTS what the device actually
    /// reports, synthesises a palm only when one is genuinely absent, and logs every joint it could
    /// not place. Nothing is silently mangled: an unplaced joint is a reported missing joint.
    /// </summary>
    [DefaultExecutionOrder(100)]   // after the SDK's skeleton update, before the matcher reads
    public class MetaHandSource : MonoBehaviour,
        SkeletonPoseSource.IConfidenceProvider,
        SkeletonPoseSource.IHandStateProvider
    {
        public enum Hand
        {
            Left = 0,
            Right = 1
        }

        [Header("What to drive")]
        [Tooltip("The hand that is drawn. Uses FitToSkeleton, so the drawn hand takes the real bone lengths.")]
        public HandVisual visual;

        [Tooltip("Optional. Receives the canonical-order joint array for matching. One per hand.")]
        public SkeletonPoseSource poseSource;

        [Tooltip("Which physical hand this component follows.")]
        public Hand hand = Hand.Right;

        [Header("Pinch (the grab input)")]
        [Tooltip("Thumb-tip to index-tip distance, in metres, at which the hand counts as pinching. " +
                 "Geometric, not OVRHand.GetFingerIsPinching, so it works on any SDK and in the simulator.")]
        public float pinchDistance = 0.025f;

        [Tooltip("Distance at which a pinch is released. Above pinchDistance so a shaky boundary does " +
                 "not chatter the grab on and off.")]
        public float releaseDistance = 0.035f;

        [Tooltip("Distance treated as fully open, for PinchStrength. Not used for the pinching test.")]
        public float openDistance = 0.07f;

        [Header("Reporting")]
        [Tooltip("Write the joint report to persistentDataPath so it can be pulled off the headset with adb.")]
        public bool writeReportToFile = true;

        [Tooltip("File name inside persistentDataPath.")]
        public string reportFileName = "hand-joint-report.txt";

        [Header("Debug")]
        [Tooltip("Log every bind and every tracking-state change. Off in release.")]
        public bool verboseLogging = true;

        /// <summary>True when the SDK says this hand's skeleton data is valid this frame.</summary>
        public bool IsTracked { get; private set; }

        /// <summary>
        /// True when this hand is allowed to drive the matcher (P2.7). A tracked left hand reports
        /// false in this build: it is still tracked, still drawn, and still usable by P1.3's rating
        /// trials, it just does not play the game.
        /// </summary>
        public bool FeedsMatcher { get; private set; }

        /// <summary>0..1. From the SDK's confidence enum when available, else 1 while tracked, else 0.</summary>
        public float Confidence { get; private set; }

        /// <summary>True while thumb tip and index tip are pinched together.</summary>
        public bool IsPinching { get; private set; }

        /// <summary>0..1, how closed the pinch is. Drives nothing yet; the grab uses the boolean.</summary>
        public float PinchStrength { get; private set; }

        /// <summary>
        /// The midpoint of thumb tip and index tip: the point a pinch actually closes around, and
        /// therefore the point a grab must be measured from. Using the wrist instead would make
        /// every grab target effectively 10 cm nearer than it looks.
        /// </summary>
        public Vector3 PinchPoint { get; private set; }

        /// <summary>False when either tip joint is missing, so callers do not test a stale point.</summary>
        public bool HasPinchPoint { get; private set; }

        /// <summary>
        /// The wrist's WORLD position. Used by P1.3's hold-still variant, which needs to know whether
        /// the hand is stationary - a question nothing else in the project asks.
        /// </summary>
        public Vector3 WristPosition
        {
            get
            {
                int i = JointNames.IndexOf("Wrist");
                if (_canonical == null || i < 0 || i >= _canonical.Length) return transform.position;
                var wrist = _canonical[i];
                return wrist != null ? wrist.position : transform.position;
            }
        }

        /// <summary>
        /// The wrist's WORLD rotation. This is the hand's orientation, and it is what the grab
        /// follows and what P1.3 turns the reference hand with. Reading it from this component's own
        /// transform would be wrong: that transform is the rig root and never rotates.
        /// </summary>
        public Quaternion WristRotation
        {
            get
            {
                int i = JointNames.IndexOf("Wrist");
                if (_canonical == null || i < 0 || i >= _canonical.Length) return Quaternion.identity;
                var wrist = _canonical[i];
                return wrist != null ? wrist.rotation : Quaternion.identity;
            }
        }

        /// <summary>How many of the 26 canonical joints the device actually reported.</summary>
        public int MappedJoints { get; private set; }

        /// <summary>
        /// Which skeleton the device is reporting: "OpenXR (26-joint)" or "OVR legacy (24-joint)".
        /// Read from the bone-id prefixes, because the two layouts are not interchangeable: see
        /// JointNames and POSE-FORMAT.md section 1.1.
        /// </summary>
        public string DetectedLayout { get; private set; } = "unknown";

        /// <summary>The last report, for the debug readout.</summary>
        public string Report { get; private set; } = "not bound";

        /// <summary>The device's own bone names, in the order the SDK lists them. This is what P1.1's
        /// "joint count and names documented" deliverable is built from.</summary>
        public string DeviceBoneList { get; private set; } = "not read";

        public int DeviceBoneCount => _deviceBones?.Count ?? 0;

        // ---- reflection, resolved once ------------------------------------------------
        MonoBehaviour _skeleton;                 // OVRSkeleton, or whatever the SDK calls it
        PropertyInfo _pIsInitialized;
        PropertyInfo _pIsDataValid;
        PropertyInfo _pBones;
        PropertyInfo _pBoneTransform;
        PropertyInfo _pBoneId;
        MonoBehaviour _hand;                     // OVRHand, optional; confidence only
        PropertyInfo _pConfidence;

        List<object> _deviceBones;
        Transform[] _canonical;                  // 26, nulls where the device had nothing
        bool _bound;
        bool _bindFailedLogged;

        static readonly string[] SkeletonTypeNames = { "OVRSkeleton" };
        static readonly string[] HandTypeNames = { "OVRHand" };

        void Awake()
        {
            _canonical = new Transform[JointNames.Canonical.Length];

            // The tracked hand builds its own capsules from the same code the reference hand uses,
            // so there is exactly one hand mesh in the project and it cannot drift.
            if (visual != null && (visual.joints == null || visual.joints.Length == 0)) visual.Build();

            // Start hidden: a hand drawn at the origin before the tracker reports would look like a
            // tracking failure on a headset that is merely still initialising.
            if (visual != null) visual.SetVisible(false);
        }

        void OnEnable()
        {
            _bound = false;
            _skeleton = null;
            _hand = null;
            TryDiscover();
        }

        void LateUpdate()
        {
            if (_skeleton == null)
            {
                if (!_bindFailedLogged)
                {
                    _bindFailedLogged = true;
                    Debug.LogWarning("[MetaHandSource:" + hand + "] no OVRSkeleton found in the scene. " +
                                     "Nothing will track. In the editor or Meta XR Simulator this is expected " +
                                     "until the rig is present; on device it means the rig is missing.");
                }
                Tracked(false);
                return;
            }

            if (!ReadBool(_pIsInitialized, _skeleton) || !ReadBool(_pIsDataValid, _skeleton))
            {
                Tracked(false);
                return;
            }

            if (!_bound && !Bind())
            {
                Tracked(false);
                return;
            }

            RefreshConfidence();
            RefreshPinch();
            if (visual != null) visual.FitToSkeleton(_canonical);
            Tracked(true);
        }

        // ---- discovery ----------------------------------------------------------------

        /// <summary>
        /// Finds the SDK components. Searched on this object, then children, then the whole scene,
        /// because the rig's hierarchy differs between the legacy OVR rig and the v207 builders.
        /// </summary>
        void TryDiscover()
        {
            foreach (var name in SkeletonTypeNames)
            {
                var type = ResolveType(name);
                if (type == null) continue;

                var candidates = FindAll(type);
                MonoBehaviour chosen = null;

                foreach (var c in candidates)
                {
                    var declared = SkeletonHandedness(c);
                    if (declared == null || declared == hand) { chosen = c; break; }
                }

                if (chosen != null)
                {
                    _skeleton = chosen;
                    _pIsInitialized = Prop(chosen.GetType(), "IsInitialized");
                    _pIsDataValid = Prop(chosen.GetType(), "IsDataValid");
                    _pBones = Prop(chosen.GetType(), "Bones");
                    if (verboseLogging)
                        Debug.Log("[MetaHandSource:" + hand + "] using " + chosen.GetType().Name +
                                  " on '" + chosen.gameObject.name + "'");
                    break;
                }
            }

            foreach (var name in HandTypeNames)
            {
                var type = ResolveType(name);
                if (type == null) continue;
                foreach (var c in FindAll(type))
                {
                    var declared = SkeletonHandedness(c);

                    // Only accept an OVRHand that STATES which hand it is. If it does not say, it
                    // could be the other hand, and reading the other hand's confidence would pause
                    // a working hand for no reason. Falling through to "tracked and unqualified"
                    // (confidence 1) is the safer of the two failures.
                    if (declared != hand) continue;
                    _hand = c;
                    _pConfidence = Prop(c.GetType(), "HandConfidence");
                    if (verboseLogging)
                        Debug.Log("[MetaHandSource:" + hand + "] confidence from OVRHand on '" +
                                  c.gameObject.name + "'");
                    break;
                }
                if (_hand != null) break;
            }
        }

        /// <summary>Which hand an SDK component claims to be, or null if it does not say.</summary>
        static Hand? SkeletonHandedness(MonoBehaviour c)
        {
            var type = c.GetType();

            // OVRSkeleton.GetSkeletonType() -> SkeletonType.HandLeft / HandRight
            var method = type.GetMethod("GetSkeletonType", BindingFlags.Public | BindingFlags.Instance);
            if (method != null && method.GetParameters().Length == 0)
            {
                try
                {
                    var value = method.Invoke(c, null);
                    var asHand = NameToHand(value == null ? null : value.ToString());
                    if (asHand != null) return asHand;
                }
                catch { /* a throwing SDK accessor is not a reason to stop */ }
            }

            // OVRHand.HandType / OVRSkeleton's serialized fields, both spelled the same way
            foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (p.Name != "HandType" && p.Name != "SkeletonType") continue;
                try
                {
                    var asHand = NameToHand(p.GetValue(c, null)?.ToString());
                    if (asHand != null) return asHand;
                }
                catch { }
            }
            return null;
        }

        static Hand? NameToHand(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            if (s.EndsWith("Left", StringComparison.OrdinalIgnoreCase)) return Hand.Left;
            if (s.EndsWith("Right", StringComparison.OrdinalIgnoreCase)) return Hand.Right;
            return null;
        }

        static List<MonoBehaviour> FindAll(Type type)
        {
            var found = new List<MonoBehaviour>();
            var all = Resources.FindObjectsOfTypeAll(type);
            foreach (var o in all)
            {
                var mb = o as MonoBehaviour;
                if (mb == null || mb.gameObject.scene.IsValid() == false) continue;   // skip prefab assets
                found.Add(mb);
            }
            return found;
        }

        static Type ResolveType(string simpleName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types; }
                catch { continue; }

                foreach (var t in types)
                {
                    if (t != null && t.Name == simpleName) return t;
                }
            }
            return null;
        }

        static PropertyInfo Prop(Type t, string name) =>
            t?.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);

        static bool ReadBool(PropertyInfo p, object target)
        {
            if (p == null || target == null) return false;
            try { return p.GetValue(target, null) is bool b && b; }
            catch { return false; }
        }

        // ---- binding ------------------------------------------------------------------

        /// <summary>
        /// Reads the SDK's bone list once and builds the canonical-order array. Returns false if
        /// the list is not readable yet, so LateUpdate simply tries again next frame.
        /// </summary>
        bool Bind()
        {
            if (_pBones == null || _skeleton == null) return false;

            IList bones = null;
            try { bones = _pBones.GetValue(_skeleton, null) as IList; }
            catch (Exception e) { Debug.LogWarning("[MetaHandSource] reading Bones failed: " + e.Message); }
            if (bones == null || bones.Count == 0) return false;

            var first = bones[0];
            if (first == null) return false;
            if (_pBoneTransform == null) _pBoneTransform = Prop(first.GetType(), "Transform");
            if (_pBoneId == null) _pBoneId = Prop(first.GetType(), "Id");
            if (_pBoneTransform == null || _pBoneId == null)
            {
                Debug.LogError("[MetaHandSource] a bone is missing Transform or Id. Cannot bind.");
                return false;
            }

            _deviceBones = new List<object>(bones.Count);
            for (int i = 0; i < bones.Count; i++) _deviceBones.Add(bones[i]);

            var report = new System.Text.StringBuilder();
            var bonesByName = new System.Text.StringBuilder();
            int xrIds = 0, legacyIds = 0;
            var used = new bool[JointNames.Canonical.Length];
            int placed = 0;

            bonesByName.Append("device bone count = ").Append(bones.Count).Append('\n');

            for (int i = 0; i < _deviceBones.Count; i++)
            {
                var bone = _deviceBones[i];
                if (bone == null) continue;

                string id = _pBoneId.GetValue(bone, null)?.ToString() ?? "?";
                if (id.StartsWith("XRHand_", StringComparison.Ordinal)) xrIds++;
                else if (id.StartsWith("Hand_", StringComparison.Ordinal)) legacyIds++;
                var tr = _pBoneTransform.GetValue(bone, null) as Transform;
                bonesByName.Append("  [").Append(i.ToString("00")).Append("] ").Append(id);
                if (tr != null) bonesByName.Append("  -> '").Append(tr.name).Append('\'');

                string canonical = JointNames.Canonicalize(id);
                if (canonical == null && tr != null) canonical = JointNames.Canonicalize(tr.name);

                int index = canonical == null ? -1 : JointNames.IndexOf(canonical);
                if (index >= 0 && !used[index] && tr != null)
                {
                    _canonical[index] = tr;
                    used[index] = true;
                    placed++;
                    bonesByName.Append("   => ").Append(canonical);
                }
                else if (index >= 0 && used[index])
                {
                    bonesByName.Append("   duplicate of ").Append(canonical).Append(" - ignored");
                }
                else
                {
                    bonesByName.Append("   (not a pose joint)");
                }
                bonesByName.Append('\n');
            }

            // A palm is the one joint worth synthesising: the legacy layout has none, and without it
            // the palm capsule has nothing to follow. It sits midway between the wrist and the middle
            // metacarpal, which is where the OpenXR layout puts it.
            if (_canonical[JointNames.IndexOf("Palm")] == null)
            {
                var wrist = _canonical[JointNames.IndexOf("Wrist")];
                var middle = _canonical[JointNames.IndexOf("MiddleMetacarpal")];
                if (wrist != null && middle != null)
                {
                    var palmGo = new GameObject("Palm_synthesised");
                    palmGo.transform.SetParent(transform, false);
                    palmGo.transform.position = Vector3.Lerp(wrist.position, middle.position, 0.5f);
                    palmGo.transform.rotation = wrist.rotation;
                    _canonical[JointNames.IndexOf("Palm")] = palmGo.transform;
                    used[JointNames.IndexOf("Palm")] = true;
                    placed++;
                    report.AppendLine("SYNTHESISED: Palm (the device reported none - legacy 20-bone layout)");
                }
            }

            for (int c = 0; c < JointNames.Canonical.Length; c++)
            {
                if (!used[c]) report.AppendLine("MISSING: " + JointNames.Canonical[c]);
            }

            DetectedLayout = xrIds > legacyIds
                ? "OpenXR (26-joint)"
                : (legacyIds > 0 ? "OVR legacy (24-joint)" : "unrecognised");

            if (legacyIds > 0 && xrIds == 0 && verboseLogging)
            {
                // This is worth a warning rather than a note. The pose files were authored against
                // the OpenXR joint set and its z-forward/y-up alignment; the legacy skeleton has a
                // different alignment AND no metacarpal for index/middle/ring, which is where the
                // spread in every one of the six shipped shapes comes from. It will look like bad
                // tracking, and it will not be.
                Debug.LogWarning("[MetaHandSource:" + hand + "] the device is reporting the LEGACY OVR " +
                                 "skeleton (24 joints, no palm, no index/middle/ring metacarpal). The pose " +
                                 "files assume the OpenXR 26-joint set and its axis alignment. Fix by setting " +
                                 "OVRManager > Hand Skeleton Version to OpenXR (the default since v71) rather " +
                                 "than by re-tuning thresholds.");
            }

            MappedJoints = placed;
            DeviceBoneList = bonesByName.ToString();
            Report = "hand " + hand + " · layout " + DetectedLayout + " · bound " + placed + "/" +
                     JointNames.Canonical.Length + " canonical joints from " + _deviceBones.Count +
                     " device bones\n" + report;

            if (poseSource != null)
            {
                poseSource.BindCanonical(_canonical, "MetaHandSource(" + hand + ")");

                // P2.7: the policy is applied here, at the only moment both the hand and its pose
                // source are known. A hand that cannot be matched still binds and still reports - it
                // just does not feed the game, so P1.3's rating trials keep working and the right hand
                // is never fighting the left for the matcher.
                FeedsMatcher = HandSupport.FeedsMatcher(hand);
                poseSource.driveMatcher = FeedsMatcher;
                poseSource.mirrorInput = HandSupport.MirrorsInput(hand);

                if (!FeedsMatcher)
                {
                    Debug.Log("[MetaHandSource:" + hand + "] tracking, but not feeding the matcher: " +
                              HandSupport.Why + ". The user-facing line is in the honest mirror: \"" +
                              HandSupport.OneLineExplanation + "\"");
                }
            }

            if (verboseLogging) Debug.Log("[MetaHandSource:" + hand + "] " + Report);
            if (writeReportToFile) WriteReport();

            _bound = true;
            return true;
        }

        void WriteReport()
        {
            try
            {
                var path = Path.Combine(Application.persistentDataPath, reportFileName);
                var text = "SIGNSPACE hand joint report\n" +
                           "hand: " + hand + "\n" +
                           "device: " + SystemInfo.deviceModel + " · " + SystemInfo.operatingSystem + "\n" +
                           "unity: " + Application.unityVersion + "\n\n" +
                           "--- what the device reported ---\n" + DeviceBoneList + "\n" +
                           "--- what we made of it ---\n" + Report;
                File.WriteAllText(path, text);
                Debug.Log("[MetaHandSource:" + hand + "] joint report written to " + path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MetaHandSource] could not write the joint report: " + e.Message);
            }
        }

        // ---- per-frame signals --------------------------------------------------------

        void Tracked(bool tracked)
        {
            if (IsTracked == tracked) return;
            IsTracked = tracked;

            // Hide the renderers rather than the GameObject: the GameObject may also carry the
            // pose source, and deactivating it would take the matcher down with the hand.
            if (visual != null) visual.SetVisible(tracked);

            if (!tracked)
            {
                IsPinching = false;
                PinchStrength = 0f;
                Confidence = 0f;
                HasPinchPoint = false;
            }
        }

        void RefreshConfidence()
        {
            if (_pConfidence != null && _hand != null)
            {
                try
                {
                    var value = _pConfidence.GetValue(_hand, null)?.ToString();
                    switch (value)
                    {
                        case "High": Confidence = 1f; return;
                        case "Medium": Confidence = 0.7f; return;
                        case "Low": Confidence = 0.4f; return;
                        case "None": Confidence = 0f; return;
                    }
                }
                catch { }
            }
            Confidence = 1f;   // tracked and the SDK did not qualify it
        }

        void RefreshPinch()
        {
            var thumb = _canonical[JointNames.IndexOf("ThumbTip")];
            var index = _canonical[JointNames.IndexOf("IndexTip")];
            if (thumb == null || index == null)
            {
                IsPinching = false;
                PinchStrength = 0f;
                HasPinchPoint = false;
                return;
            }

            PinchPoint = (thumb.position + index.position) * 0.5f;
            HasPinchPoint = true;
            float d = Vector3.Distance(thumb.position, index.position);
            PinchStrength = Mathf.Clamp01(1f - (d - pinchDistance) / Mathf.Max(0.001f, openDistance - pinchDistance));

            // Hysteresis: close at pinchDistance, open only above releaseDistance. A single
            // threshold makes a grab flicker at the boundary, which feels like the app is broken.
            if (IsPinching) { if (d > releaseDistance) IsPinching = false; }
            else if (d < pinchDistance) { IsPinching = true; }
        }
    }
}
