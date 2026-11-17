using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Signspace.Core;
using Signspace.Hands;
using Signspace.Matching;
using Signspace.Spatial;

namespace Signspace.UI
{
    /// <summary>
    /// Every state change in SOUND-MAP.md §2, played from three pooled sources (P7.1).
    ///
    /// THE RULES THIS FILE IMPLEMENTS
    /// ------------------------------
    ///   * **No music bed.** There is no field for one.
    ///   * **Body sounds are not spatialised, room sounds are.** Band changes live in `BandAudio`
    ///     (they are about your hand); this component owns everything that happens *to* something.
    ///   * **The near-miss is a question, not a verdict.** It is two notes a *falling third* apart,
    ///     quieter than the chime, and it is deliberately not a transposition of the chime.
    ///   * **Silence is a state, not a gap.** Pausing on tracking loss, leaving the band, opening a
    ///     reading panel and idling all have no sound, and `DocumentedSilences` names them so the check
    ///     can tell a decision from an omission.
    ///   * **One source per kind, pooled.** A new `AudioSource` per event is a per-frame allocation
    ///     waiting to happen (`PERFORMANCE.md` §5).
    ///
    /// Placeholders: the clips are synthesised by `scripts/make_sfx.py` to the map's *shape*, not its
    /// timbre. Swapping in recorded wood is a reference swap and no code change.
    /// </summary>
    public class SoundDirector : MonoBehaviour
    {
        public enum Cue
        {
            HandsFound,
            ReferenceHandAppears,
            Grab,
            Release,
            LockChime,
            ObjectSettles,
            NearMiss,
            SurfaceFound,
            ShelfFull,
            ToggleFlip,
            CalibrationStart,
            CalibrationDone,
        }

        [System.Serializable]
        public struct Slot
        {
            public Cue cue;
            public AudioClip clip;
            [Tooltip("True = it comes from a place in the room; false = it is about you.")]
            public bool spatial;
            [Range(0f, 1f)] public float volume;
        }

        [Header("Dependencies")]
        public Matcher matcher;
        public ShelfAnchor shelf;
        public ReferenceHand referenceHand;
        public Calibration calibration;
        public A11ySettings settings;
        public Transform listener;

        [Header("Clips — one row per cue, filled by the scene builder")]
        public Slot[] cues;

        [Tooltip("The granular rotation texture, driven by how fast the reference hand is turned.")]
        public AudioClip rotateClip;

        [Header("Behaviour")]
        [Tooltip("Flight time to the shelf slot, so the settle lands when the object does.")]
        public float settleDelaySeconds = 0.66f;
        [Tooltip("Rotation below this many degrees per second is not 'turning' and stays silent.")]
        public float rotateFloorDegreesPerSecond = 12f;
        [Range(0f, 1f)] public float rotateVolume = 0.55f;

        /// <summary>
        /// States with no sound, on purpose. `SOUND-MAP.md` §5. The check reads this list, so a missing
        /// clip on a real cue cannot hide behind "we meant to".
        /// </summary>
        public static readonly string[] DocumentedSilences =
        {
            "paused: tracking lost",        // absence of judgement; a sound here would say something is wrong
            "un-paused",
            "left the locked band",         // not a failure; it just stops sounding like progress
            "credits opened",               // reading is silent
            "the honest mirror opened",     // reading is silent
            "idling",
        };

        AudioSource _body;
        AudioSource _room;
        AudioSource _rotate;
        Quaternion _lastHandRotation;
        bool _hasLastRotation;
        bool _handsFoundPlayed;
        bool _handAppeared;
        bool _wasGrabbed;
        Coroutine _settle;

        readonly Dictionary<Cue, Slot> _byCue = new Dictionary<Cue, Slot>();

        void Awake()
        {
            if (matcher == null) matcher = FindFirstObjectByType<Matcher>();
            if (shelf == null) shelf = FindFirstObjectByType<ShelfAnchor>();
            if (referenceHand == null) referenceHand = FindFirstObjectByType<ReferenceHand>();
            if (calibration == null) calibration = FindFirstObjectByType<Calibration>();
            if (settings == null) settings = A11ySettings.Instance;

            _byCue.Clear();
            if (cues != null)
            {
                for (int i = 0; i < cues.Length; i++)
                {
                    if (!_byCue.ContainsKey(cues[i].cue)) _byCue.Add(cues[i].cue, cues[i]);
                }
            }

            _body = MakeSource("SFX_Body", 0f);
            _room = MakeSource("SFX_Room", 1f);
            _rotate = MakeSource("SFX_Rotate", 1f);
            _rotate.loop = true;
            _rotate.volume = 0f;
            _rotate.clip = rotateClip;
            if (listener == null && Camera.main != null) listener = Camera.main.transform;
            _room.minDistance = 0.3f;
            _room.maxDistance = 5f;
            _rotate.minDistance = 0.3f;
            _rotate.maxDistance = 3f;
        }

        AudioSource MakeSource(string name, float spatialBlend)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = spatialBlend;
            source.dopplerLevel = 0f;
            return source;
        }

        void OnEnable()
        {
            if (matcher != null)
            {
                matcher.OnLocked += OnLocked;
                matcher.OnUnlocked += OnUnlocked;
                matcher.OnNearMiss += OnNearMiss;
                matcher.OnDistanceUpdated += OnDistanceUpdated;
            }
            if (shelf != null) shelf.OnModeChanged += OnModeChanged;
            if (calibration != null)
            {
                calibration.OnStarted += OnCalibrationStart;
                calibration.OnFinished += OnCalibrationDone;
            }
            if (settings != null) settings.OnChanged += OnSettingChanged;
        }

        void OnDisable()
        {
            if (matcher != null)
            {
                matcher.OnLocked -= OnLocked;
                matcher.OnUnlocked -= OnUnlocked;
                matcher.OnNearMiss -= OnNearMiss;
                matcher.OnDistanceUpdated -= OnDistanceUpdated;
            }
            if (shelf != null) shelf.OnModeChanged -= OnModeChanged;
            if (calibration != null)
            {
                calibration.OnStarted -= OnCalibrationStart;
                calibration.OnFinished -= OnCalibrationDone;
            }
            if (settings != null) settings.OnChanged -= OnSettingChanged;
            StopRotate();
        }

        // ------------------------------------------------------------------ cue plumbing

        public bool HasClip(Cue cue) => _byCue.TryGetValue(cue, out var slot) && slot.clip != null;

        public AudioClip ClipFor(Cue cue) => _byCue.TryGetValue(cue, out var slot) ? slot.clip : null;

        public void Play(Cue cue, Vector3? position = null)
        {
            if (!_byCue.TryGetValue(cue, out var slot) || slot.clip == null) return;

            var source = slot.spatial ? _room : _body;
            if (slot.spatial && position.HasValue) source.transform.position = position.Value;
            source.PlayOneShot(slot.clip, slot.volume <= 0f ? 1f : slot.volume);
        }

        // ------------------------------------------------------------------ events

        void OnDistanceUpdated(float current, float other)
        {
            if (_handsFoundPlayed) return;
            _handsFoundPlayed = true;
            Play(Cue.HandsFound);   // "I see you", once, and never again this session
        }

        void OnLocked(string shapeId)
        {
            Vector3 at = ShelfPosition(shapeId, out bool onShelf);
            if (!onShelf) Play(Cue.ShelfFull, at);
            else Play(Cue.LockChime, at);

            if (_settle != null) StopCoroutine(_settle);
            _settle = StartCoroutine(SettleAfterFlight(at));
        }

        IEnumerator SettleAfterFlight(Vector3 at)
        {
            yield return new WaitForSeconds(settleDelaySeconds);
            Play(Cue.ObjectSettles, at);
            _settle = null;
        }

        void OnUnlocked(string shapeId)
        {
            Play(Cue.Release, HandPosition());
        }

        void OnNearMiss(string shapeId, float distance)
        {
            // Body sound: a comment on what you did, not a thing in the room.
            Play(Cue.NearMiss);
        }

        void OnModeChanged(bool onSurface)
        {
            if (onSurface && shelf != null) Play(Cue.SurfaceFound, shelf.transform.position);
        }

        void OnCalibrationStart() => Play(Cue.CalibrationStart);

        void OnCalibrationDone() => Play(Cue.CalibrationDone);

        void OnSettingChanged(A11yToggle toggle) => Play(Cue.ToggleFlip);

        // ------------------------------------------------------------------ per-frame hand sound

        void Update()
        {
            if (referenceHand == null) return;

            if (!_handAppeared && referenceHand.visual != null &&
                referenceHand.visual.gameObject.activeInHierarchy)
            {
                _handAppeared = true;
                Play(Cue.ReferenceHandAppears, HandPosition());
            }

            bool grabbed = referenceHand.IsGrabbed;
            if (grabbed != _wasGrabbed)
            {
                _wasGrabbed = grabbed;
                Play(grabbed ? Cue.Grab : Cue.Release, HandPosition());
                if (!grabbed) StopRotate();
            }

            if (grabbed) RotateSound();
            else StopRotate();
        }

        void RotateSound()
        {
            var rotation = referenceHand.transform.rotation;
            if (!_hasLastRotation)
            {
                _lastHandRotation = rotation;
                _hasLastRotation = true;
                return;
            }

            float degrees = Quaternion.Angle(_lastHandRotation, rotation);
            _lastHandRotation = rotation;

            float speed = degrees / Mathf.Max(0.0001f, Time.deltaTime);
            float loudness = Mathf.InverseLerp(rotateFloorDegreesPerSecond, 120f, speed);
            _rotate.volume = Mathf.MoveTowards(_rotate.volume, loudness * rotateVolume, Time.deltaTime * 2f);
            _rotate.transform.position = HandPosition();

            if (!_rotate.isPlaying && _rotate.clip != null) _rotate.Play();
        }

        void StopRotate()
        {
            _hasLastRotation = false;
            if (_rotate == null) return;
            _rotate.volume = Mathf.MoveTowards(_rotate.volume, 0f, Time.deltaTime * 6f);
            if (_rotate.volume <= 0.001f && _rotate.isPlaying) _rotate.Stop();
        }

        Vector3 HandPosition() =>
            referenceHand != null ? referenceHand.transform.position : transform.position;

        /// <summary>Where the reward comes from: the object's own slot, and whether it had one.</summary>
        public Vector3 ShelfPosition(string shapeId, out bool onShelf)
        {
            if (shelf != null)
            {
                int index = shelf.SlotIndexFor(shapeId);
                if (index >= 0)
                {
                    onShelf = true;
                    return shelf.SlotTransform(index).position;
                }
                onShelf = false;
                return shelf.transform.position;
            }
            onShelf = false;
            return transform.position;
        }

    }
}
