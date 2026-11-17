using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using Signspace.Hands;
using Signspace.Matching;

namespace Signspace.Core
{
    /// <summary>
    /// Records one stranger's session to a CSV and a console report (P8.2, P8.3).
    ///
    /// The phase's whole claim — "a stranger reaches a first lock in under 75 s" — is a number, and a
    /// number produced with a stopwatch while also holding a clipboard is a number nobody should trust.
    /// This writes the timeline instead: first hand, first grab, first band, first lock, first near-miss,
    /// first accessibility switch, every stall longer than ten seconds, and the session end.
    ///
    /// WHAT COUNTS AS A STALL
    /// ----------------------
    /// The tester's hand has not moved (under 2 cm/s at the palm), no pinch has started, no switch has
    /// flipped and nothing has locked for **10 s**. That is the task's definition ("if they stop for more
    /// than 10 seconds, note it as friction"), made mechanical so the observer can watch the person
    /// instead of the clock.
    ///
    /// WHERE IT WRITES
    /// ---------------
    ///   device: Application.persistentDataPath/playtest-&lt;stamp&gt;.csv
    ///   editor: phases/p8-playtest/playtest-&lt;stamp&gt;.csv
    /// `source` says device/editor. A simulated session from a check writes `fixture`. Nothing in this
    /// file can make a session look like a device run when it was not one.
    /// </summary>
    public class PlaytestProbe : MonoBehaviour
    {
        [Header("Dependencies")]
        public Matcher matcher;
        public ReferenceHand referenceHand;
        public A11ySettings settings;

        [Header("Output")]
        [Tooltip("Empty = per-platform default. Set it to write somewhere else from a harness.")]
        public string pathOverride;
        [Tooltip("device / editor. Written into every row; never guessed.")]
        public string source = "device";
        public bool writeFile = true;

        [Header("Stall detection")]
        [Tooltip("Seconds of no input before the session records a stall. P8.2's number is 10.")]
        public float stallSeconds = 10f;
        [Tooltip("Palm speed under this is 'not moving'. 2 cm/s is below hand-tracker jitter for a held hand.")]
        public float stillSpeed = 0.02f;

        public PlaytestTimeline Timeline { get; private set; } = new PlaytestTimeline();
        public string LogPath { get; private set; }
        public float SessionSeconds => _elapsed;

        float _elapsed;
        bool _running;
        bool _handSeen;
        Vector3 _lastPalm;
        float _stillFor;
        bool _lastPinching;
        bool _lastGrabbed;
        DateTime _startUtc;

        static readonly string[] Fields =
        {
            "timestamp_utc", "unix_ms", "session", "source", "event", "seconds", "detail",
        };

        void Awake()
        {
            if (matcher == null) matcher = FindFirstObjectByType<Matcher>();
            if (referenceHand == null) referenceHand = FindFirstObjectByType<ReferenceHand>();
            if (settings == null) settings = A11ySettings.Instance;

            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            LogPath = ResolvePath(stamp);
            _startUtc = DateTime.UtcNow;
            Timeline.Begin(stamp, Application.isEditor ? "editor" : source, 0f);
            _running = true;
            EnsureHeader();
            Debug.Log("[PlaytestProbe] session " + stamp + " -> " + LogPath);
        }

        string ResolvePath(string stamp)
        {
            if (!string.IsNullOrEmpty(pathOverride)) return pathOverride;
#if UNITY_EDITOR
            string root = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.Combine(root, "phases", "p8-playtest", "playtest-" + stamp + ".csv");
#else
            return Path.Combine(Application.persistentDataPath, "playtest-" + stamp + ".csv");
#endif
        }

        void OnEnable()
        {
            if (matcher != null)
            {
                matcher.OnLocked += OnLocked;
                matcher.OnNearMiss += OnNearMiss;
                matcher.OnBandChanged += OnBandChanged;
            }
            if (settings != null) settings.OnChanged += OnSettingChanged;
        }

        void OnDisable()
        {
            if (matcher != null)
            {
                matcher.OnLocked -= OnLocked;
                matcher.OnNearMiss -= OnNearMiss;
                matcher.OnBandChanged -= OnBandChanged;
            }
            if (settings != null) settings.OnChanged -= OnSettingChanged;
        }

        void Update()
        {
            if (!_running) return;
            _elapsed += Time.unscaledDeltaTime;

            if (referenceHand == null || matcher == null) return;

            // Hands found: the first frame the matcher is willing to judge, not the first frame the
            // process is alive.
            if (!_handSeen && !matcher.IsPaused && matcher.LastConfidence > 0.05f)
            {
                _handSeen = true;
                Record(PlaytestTimeline.HandSeen);
            }

            bool grabbed = referenceHand.IsGrabbed;
            if (grabbed && !_lastGrabbed) Record(PlaytestTimeline.FirstGrab);
            if (grabbed != _lastGrabbed) ResetStill();
            _lastGrabbed = grabbed;

            Vector3 palm = referenceHand.transform.position;
            float speed = Time.unscaledDeltaTime > 0f
                ? Vector3.Distance(palm, _lastPalm) / Time.unscaledDeltaTime
                : 0f;
            _lastPalm = palm;

            bool pinching = referenceHand.trackedSource != null && referenceHand.trackedSource.IsTracked
                            && referenceHand.trackedSource.IsPinching;
            if (pinching && !_lastPinching) ResetStill();
            _lastPinching = pinching;

            if (speed > stillSpeed || grabbed || pinching)
            {
                _stillFor = 0f;
                return;
            }

            _stillFor += Time.unscaledDeltaTime;
            if (_stillFor >= stallSeconds)
            {
                Record(PlaytestTimeline.Stall, "still " + stallSeconds.ToString("0") + " s");
                _stillFor = 0f;   // one row per stall, not one per frame
            }
        }

        void ResetStill() => _stillFor = 0f;

        void OnBandChanged(Band band)
        {
            if (band == Band.Close) Record(PlaytestTimeline.BandClose);
        }

        void OnLocked(string shapeId) => Record(PlaytestTimeline.FirstLock, shapeId);
        void OnNearMiss(string shapeId, float degrees) =>
            Record(PlaytestTimeline.FirstNearMiss, shapeId + " " + degrees.ToString("0.0") + "°");

        void OnSettingChanged(A11yToggle toggle) => Record(PlaytestTimeline.FirstSwitch, toggle.ToString());

        void Record(string name, string detail = "")
        {
            if (Timeline.Mark(_elapsed, name, detail))
            {
                Debug.Log("[Playtest] " + _elapsed.ToString("0.0") + "s " + name +
                          (string.IsNullOrEmpty(detail) ? "" : " — " + detail));
                FlushOne(_elapsed, name, detail);
            }
        }

        void OnApplicationQuit() => EndSession();

        void OnApplicationPause(bool paused)
        {
            if (paused) EndSession();
        }

        public void EndSession()
        {
            if (!_running) return;
            Record(PlaytestTimeline.SessionEnd, Timeline.Summary());
            _running = false;
            Debug.Log(Timeline.Report());
        }

        // ------------------------------------------------------------------ output

        void EnsureHeader()
        {
            if (!writeFile) return;
            try
            {
                string dir = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                if (!File.Exists(LogPath) || new FileInfo(LogPath).Length == 0)
                    File.WriteAllText(LogPath, PlaytestTimeline.Header + "\n");
            }
            catch (Exception error)
            {
                // A probe that throws is worse than no probe: it becomes the failure it was measuring.
                Debug.LogWarning("[PlaytestProbe] cannot write " + LogPath + ": " + error.Message);
                writeFile = false;
            }
        }

        void FlushOne(float seconds, string name, string detail)
        {
            if (!writeFile) return;
            try
            {
                var at = _startUtc.AddSeconds(seconds);
                File.AppendAllText(LogPath, string.Join(",",
                    at.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                    ((DateTimeOffset)at).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
                    Timeline.Session,
                    Timeline.Source,
                    name,
                    seconds.ToString("0.000", CultureInfo.InvariantCulture),
                    Escape(detail)) + "\n");
            }
            catch (Exception error)
            {
                Debug.LogWarning("[PlaytestProbe] append failed: " + error.Message);
                writeFile = false;
            }
        }

        static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.IndexOfAny(new[] { ',', '"', '\n' }) >= 0
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }
    }
}
