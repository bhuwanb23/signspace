using System;
using UnityEngine;
using Signspace.Gaze;
using Signspace.Hands;
using Signspace.Matching;
using Signspace.Spatial;

namespace Signspace.Core
{
    /// <summary>The stored accessibility switches. Order is the order of the blocks on the tray.</summary>
    public enum A11yToggle
    {
        Captions = 0,
        HighContrast = 1,
        Speed = 2,
        OneHanded = 3,
        EyesOnly = 4,
        NearMiss = 5,
        ReducedMotion = 6,
    }

    /// <summary>
    /// One owner for every accessibility switch (P6.5), and the one place that applies them.
    ///
    /// WHY NOT A SETTINGS MENU
    /// -----------------------
    /// The task's trap is real: a settings panel is a menu a user has to find, and on a headset that
    /// usually means a controller they may not be using. Every switch here is also a physical block
    /// on the tray (<see cref="Signspace.UI.A11yToggleBlock"/>), so the whole set is reachable with a
    /// pinch from inside the space.
    ///
    /// WHY IT OWNS THE APPLY STEP
    /// --------------------------
    /// Six components could each read `SaveData` in their own Start, and then the order between them
    /// would silently decide whether the user's choice or the inspector value won. This class reads
    /// once, in Start, after SaveSystem has loaded, and pushes the values out. When a switch changes it
    /// pushes again. Nothing else writes these fields except `Matcher.NearMissEnabled`, which the
    /// near-miss block drives through <see cref="Set"/> so there is still one owner.
    ///
    /// WHAT "SPEED" MEANS
    /// ------------------
    /// It multiplies the hold requirement and the dwell time together, because they are the same
    /// quantity to the user: how long a shape must be held still. 1.0 is the tuned default; 1.5 is the
    /// relaxed value the toggle selects. It never changes the match thresholds - a slower pace must
    /// not make the gesture easier, only give more time to make it.
    /// </summary>
    public class A11ySettings : MonoBehaviour
    {
        public SaveSystem save;
        public Matcher matcher;
        public ShelfIdle shelfIdle;
        public DwellSelector dwell;
        public LockPayoff payoff;
        public ReferenceHand referenceHand;

        [Header("High contrast targets (P7: the switch used to persist and do nothing)")]
        public UI.A11yHud hud;
        public UI.A11yToggleTray tray;
        public UI.NearMissDisplay nearMiss;

        /// <summary>Selected by the speed block. One value, not a slider: two states are testable.</summary>
        public float relaxedSpeedScale = 1.5f;

        /// <summary>
        /// P6.3 step 3: eyes-only lengthens the hold beyond the pace scale, because a dwell is easier to
        /// start by accident than a pinch is. 1.35 on top of 0.7 s is 0.95 s of shape held to lock.
        /// </summary>
        public float eyesOnlyHoldScale = 1.35f;

        static A11ySettings _instance;

        /// <summary>
        /// The scene's owner. Resolved lazily because the editor checks run outside play mode, where
        /// Awake never runs - without this every switch would read as "no settings owner" in exactly
        /// the code path that is supposed to prove the switches work.
        /// </summary>
        public static A11ySettings Instance
        {
            get
            {
                if (_instance == null) _instance = FindFirstObjectByType<A11ySettings>();
                return _instance;
            }
            private set { _instance = value; }
        }

        /// <summary>True when the scene has an owner. Prefab previews may not.</summary>
        public static bool HasInstance => Instance != null;

        public event Action<A11yToggle> OnChanged;

        readonly SaveData _fallback = new SaveData();

        public SaveData Data => save != null && save.Data != null ? save.Data : _fallback;

        float _baseHold = -1f;
        float _baseDwell = -1f;
        float _baseIdleSpin = -1f;

        /// <summary>
        /// Read by anything that has to skip motion. Static so a dozen small components can ask
        /// without each holding a reference; false when no owner exists, which is the safe default
        /// (animations run as tuned) and the one the editor checks rely on.
        /// </summary>
        public static bool ReducedMotion => Instance != null && Instance.Data.reducedMotion;

        /// <summary>Hold/dwell time multiplier. 1 when no owner is in the scene.</summary>
        public static float PaceScale
        {
            get
            {
                if (Instance == null) return 1f;
                return Mathf.Clamp(Instance.Data.speedScale, 0.5f, 3f);
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[A11ySettings] a second owner in the scene; keeping the first and " +
                                 "disabling this one so the switches cannot disagree.");
                enabled = false;
                return;
            }
            Instance = this;
        }

        void OnEnable()
        {
            // A scene opened in the editor has no Awake: claim the slot here too, so the lazy lookup
            // and the play-mode owner cannot disagree.
            if (_instance == null || _instance == this) _instance = this;
        }

        void Start()
        {
            // The save file is loaded in SaveSystem.Awake; Start is after every Awake in the scene,
            // so this reads the user's values and never the field defaults.
            CaptureSceneBases();
            ApplyAll();
        }

        /// <summary>
        /// Records the tuned values before any multiplier is applied. Public because the checks build
        /// the scene outside play mode: without this they would measure a value that had already been
        /// multiplied, and a second apply would compound it.
        /// </summary>
        public void CaptureSceneBases()
        {
            if (matcher != null) _baseHold = matcher.holdSeconds;
            if (dwell != null) _baseDwell = dwell.dwellSeconds;
            if (referenceHand != null) _baseIdleSpin = referenceHand.idleDegreesPerSecond;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---- reading -------------------------------------------------------------------------------

        public bool Get(A11yToggle toggle)
        {
            var d = Data;
            switch (toggle)
            {
                case A11yToggle.Captions: return d.captionsOn;
                case A11yToggle.HighContrast: return d.highContrast;
                case A11yToggle.Speed: return d.speedScale > 1.01f;
                case A11yToggle.OneHanded: return d.oneHanded;
                case A11yToggle.EyesOnly: return d.eyesOnly;
                case A11yToggle.NearMiss: return d.nearMissEnabled;
                case A11yToggle.ReducedMotion: return d.reducedMotion;
                default: return false;
            }
        }

        public static string Title(A11yToggle toggle)
        {
            switch (toggle)
            {
                case A11yToggle.Captions: return "Captions";
                case A11yToggle.HighContrast: return "High contrast";
                case A11yToggle.Speed: return "Relaxed pace";
                case A11yToggle.OneHanded: return "One hand";
                case A11yToggle.EyesOnly: return "Eyes only";
                case A11yToggle.NearMiss: return "Near-miss naming";
                case A11yToggle.ReducedMotion: return "Reduced motion";
                default: return toggle.ToString();
            }
        }

        /// <summary>The value line a block shows. Short enough to fit a 6 cm plate.</summary>
        public string ValueLabel(A11yToggle toggle)
        {
            switch (toggle)
            {
                case A11yToggle.Speed: return Get(toggle) ? "1.5x" : "1x";
                case A11yToggle.NearMiss: return Get(toggle) ? "on" : "off";
                default: return Get(toggle) ? "on" : "off";
            }
        }

        /// <summary>Every switch, in tray order. The builder and the checks both use this.</summary>
        public static readonly A11yToggle[] All =
        {
            A11yToggle.Captions,
            A11yToggle.HighContrast,
            A11yToggle.Speed,
            A11yToggle.OneHanded,
            A11yToggle.EyesOnly,
            A11yToggle.NearMiss,
            A11yToggle.ReducedMotion,
        };

        // ---- writing -------------------------------------------------------------------------------

        public void Toggle(A11yToggle toggle) => Set(toggle, !Get(toggle));

        public void Set(A11yToggle toggle, bool on)
        {
            var d = Data;
            switch (toggle)
            {
                case A11yToggle.Captions: d.captionsOn = on; break;
                case A11yToggle.HighContrast: d.highContrast = on; break;
                case A11yToggle.Speed: d.speedScale = on ? relaxing(d) : 1f; break;
                case A11yToggle.OneHanded: d.oneHanded = on; break;
                case A11yToggle.EyesOnly: d.eyesOnly = on; break;
                case A11yToggle.NearMiss: d.nearMissEnabled = on; break;
                case A11yToggle.ReducedMotion: d.reducedMotion = on; break;
            }

            ApplyAll();
            if (save != null) save.Save();

            Debug.Log("[A11ySettings] " + Title(toggle) + " = " + ValueLabel(toggle));
            OnChanged?.Invoke(toggle);
        }

        /// <summary>
        /// Returns every switch to its default and clears the calibration. Used by the reset block and
        /// by the checks; a user who has made the space unusable needs one gesture back to known good.
        /// </summary>
        public void ResetDefaults()
        {
            var d = Data;
            d.captionsOn = true;
            d.highContrast = false;
            d.speedScale = 1f;
            d.oneHanded = false;
            d.eyesOnly = false;
            d.nearMissEnabled = true;
            d.reducedMotion = false;
            d.calibration = new CalibrationRecord();
            d.calibrated = false;

            ApplyAll();
            if (save != null) save.Save();
            OnChanged?.Invoke(A11yToggle.Captions);
        }

        /// <summary>
        /// Cross-checks the file against what the scene can honour, and fixes the drift. Called by the
        /// P6 checks, not by Update: recomputing every frame would hide a bug instead of reporting it.
        /// </summary>
        public int Repair()
        {
            int fixedCount = 0;
            var d = Data;

            if (d.calibration == null)
            {
                d.calibration = new CalibrationRecord();
                fixedCount++;
            }

            if (d.calibrated && !d.calibration.HasUsableRange)
            {
                // A save from a build that wrote the flag without a usable record: the honest state is
                // "not calibrated", because the default path is the one that always works.
                d.calibrated = false;
                fixedCount++;
            }

            if (d.speedScale <= 0f)
            {
                d.speedScale = 1f;
                fixedCount++;
            }

            if (fixedCount > 0)
            {
                ApplyAll();
                if (save != null) save.Save();
            }
            return fixedCount;
        }

        public void ApplyAll()
        {
            Resolve();

            // Late-resolved references have no captured base yet; record it now, before any multiplier,
            // or the first apply would compound onto an already-scaled value.
            if (matcher != null && _baseHold < 0f) _baseHold = matcher.holdSeconds;
            if (dwell != null && _baseDwell < 0f) _baseDwell = dwell.dwellSeconds;
            if (referenceHand != null && _baseIdleSpin < 0f) _baseIdleSpin = referenceHand.idleDegreesPerSecond;

            var d = Data;

            if (matcher != null)
            {
                matcher.NearMissEnabled = d.nearMissEnabled;
                if (_baseHold > 0f)
                {
                    float scale = d.speedScale * (d.eyesOnly ? Mathf.Max(1f, eyesOnlyHoldScale) : 1f);
                    matcher.holdSeconds = _baseHold * scale;
                    matcher.SyncHoldTimer();
                }
            }

            if (dwell != null && _baseDwell > 0f) dwell.dwellSeconds = _baseDwell * d.speedScale;

            // Reduced motion: the shelf stops breathing, the reference hand stops drifting, and the
            // payoff runs as a cross-fade. All three are the same decision, so they are set together.
            if (shelfIdle != null) shelfIdle.breathing = !d.reducedMotion;

            if (referenceHand != null && _baseIdleSpin >= 0f)
                referenceHand.idleDegreesPerSecond = d.reducedMotion ? 0f : _baseIdleSpin;

            if (referenceHand != null) referenceHand.oneHanded = d.oneHanded;

            if (payoff != null) payoff.reducedMotion = d.reducedMotion;

            // High contrast: the HUD goes white, the tray goes black-on-white, and the near-miss drops
            // its dash and degree number. The plain form is not a preference: a low-vision user reading
            // "That's O — 3° away" needs the symbol and the number to be legible, and the plain line is
            // the one that still reads at arm's length.
            if (hud != null) hud.SetHighContrast(d.highContrast);
            if (tray != null) tray.SetHighContrast(d.highContrast);
            if (nearMiss != null) nearMiss.plainText = d.highContrast;
        }

        float relaxing(SaveData d)
        {
            return Mathf.Max(1.01f, relaxedSpeedScale);
        }

        /// <summary>
        /// Fills in any reference the scene builder forgot. A switch that silently does nothing because
        /// one field was left empty is the failure this class exists to prevent, so it looks the target
        /// up rather than skipping it. Runs once per owner; the fields are then populated.
        /// </summary>
        void Resolve()
        {
            if (matcher == null) matcher = FindFirstObjectByType<Matcher>();
            if (dwell == null) dwell = FindFirstObjectByType<DwellSelector>();
            if (shelfIdle == null) shelfIdle = FindFirstObjectByType<ShelfIdle>();
            if (referenceHand == null) referenceHand = FindFirstObjectByType<ReferenceHand>();
            if (payoff == null) payoff = FindFirstObjectByType<LockPayoff>();
            if (save == null) save = FindFirstObjectByType<SaveSystem>();
            if (hud == null) hud = FindFirstObjectByType<UI.A11yHud>();
            if (tray == null) tray = FindFirstObjectByType<UI.A11yToggleTray>();
            if (nearMiss == null) nearMiss = FindFirstObjectByType<UI.NearMissDisplay>();
        }
    }
}
