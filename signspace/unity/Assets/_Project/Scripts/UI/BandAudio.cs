using UnityEngine;
using Signspace.Matching;

namespace Signspace.UI
{
    /// <summary>
    /// The three bands as sound (P2.3, SOUND-MAP.md §2).
    ///
    /// Two rules from the sound map decide this whole file:
    ///
    ///   1. **Band sounds are not spatialised.** They are about *your* hand, and a sound that moves
    ///      when you move cannot be counted. `spatialBlend = 0`, on one pooled source.
    ///   2. **Leaving a band is not a failure.** Warming → close rises; locked → warming is either
    ///      nothing (the documented silence) or the same interval inverted at half volume. There is no
    ///      descending "you lost it" sound anywhere in this component, and there must never be.
    ///
    /// The clips here are **placeholders**, synthesised to the sound map's description (rising two-note
    /// interval 60 ms apart; a single short high tone; the inversion). They are correct in shape and
    /// wrong in timbre - P7 replaces them with the real ones. Swapping them is an inspector change, so
    /// the mapping is real now and the polish is not blocked on it.
    /// </summary>
    public class BandAudio : MonoBehaviour
    {
        [Header("Dependencies")]
        public Matcher matcher;

        [Header("Output")]
        [Tooltip("One source for every band sound. Never a new AudioSource per event (PERFORMANCE.md §5).")]
        public AudioSource source;

        [Header("Clips (P7 replaces; empty = documented silence)")]
        public AudioClip warmingToClose;
        public AudioClip closeToLocked;
        [Tooltip("Optional. Left empty, leaving a band is silent, which SOUND-MAP.md §2 explicitly allows.")]
        public AudioClip lockedToWarming;

        [Header("Levels")]
        [Range(0f, 1f)] public float warmingToCloseVolume = 0.30f;
        [Range(0f, 1f)] public float closeToLockedVolume = 0.34f;
        [Range(0f, 1f)] public float lockedToWarmingVolume = 0.16f;

        Band _previous = Band.Warming;
        bool _warned;

        void Awake()
        {
            if (matcher == null) matcher = FindFirstObjectByType<Matcher>();
            if (source == null) source = GetComponent<AudioSource>();
            if (source == null) source = gameObject.AddComponent<AudioSource>();

            // Body sounds: never spatialised, never looping, never fighting the room.
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        void OnEnable()
        {
            if (matcher == null) return;
            _previous = matcher.CurrentBand;
            matcher.OnBandChanged += OnBandChanged;
        }

        void OnDisable()
        {
            if (matcher != null) matcher.OnBandChanged -= OnBandChanged;
        }

        void OnBandChanged(Band band)
        {
            AudioClip clip = null;
            float volume = 0f;

            if (_previous == Band.Warming && band == Band.Close)
            {
                clip = warmingToClose;
                volume = warmingToCloseVolume;
            }
            else if (_previous == Band.Close && band == Band.Locked)
            {
                clip = closeToLocked;
                volume = closeToLockedVolume;
            }
            else if (band == Band.Warming)
            {
                // Downwards sound only if one was authored; silence is the default and is a decision.
                clip = lockedToWarming;
                volume = lockedToWarmingVolume;
            }

            _previous = band;

            if (clip == null)
            {
                if (band == Band.Warming && !_warned)
                {
                    Debug.Log("[BandAudio] left a band with no clip: documented silence (SOUND-MAP.md §2).");
                    _warned = true;
                }
                return;
            }

            source.PlayOneShot(clip, volume);
        }

        /// <summary>True when every upward band change has a sound. Used by the P2.3 done-when check.</summary>
        public bool IsMapped => warmingToClose != null && closeToLocked != null;
    }
}
