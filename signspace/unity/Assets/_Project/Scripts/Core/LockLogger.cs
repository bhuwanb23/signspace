using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using Signspace.Matching;

namespace Signspace.Core
{
    /// <summary>
    /// Every lock and unlock, with a timestamp, appended to a CSV (P2.4, step 5).
    ///
    /// The point is not telemetry - there is no network in this build. The point is that "20/20
    /// intentional locks, 0/20 false locks" is a claim, and a claim about a device needs rows:
    /// when it happened, how much in-band time it took, at what distance, with how much confidence,
    /// and whether tracking had dropped out during the hold. Those five columns are enough to tell a
    /// genuine lock from a lucky one after the headset has been taken off.
    ///
    /// Where it writes:
    ///   node:   phases/p2-core-loop/lock-log.csv  (relative to the project root, one level above Assets)
    ///   device: Application.persistentDataPath/lock-log.csv, pulled with
    ///           adb pull /sdcard/Android/data/com.signspace.signspace/files/lock-log.csv
    ///
    /// The same columns are used by scripts/hold_sim.py, so a simulated row and a device row can sit in
    /// one file and be told apart by `source`. A simulation is never allowed to look like a measurement.
    /// </summary>
    public class LockLogger : MonoBehaviour
    {
        [Header("Dependencies")]
        public Matcher matcher;

        [Header("Output")]
        [Tooltip("Empty = the default for this platform. Set it to write somewhere else (a test harness).")]
        public string pathOverride;

        [Header("What to record")]
        [Tooltip("Log the release of a lock as well as its arrival, so a re-lock cannot be mistaken for a duplicate.")]
        public bool logUnlocks = true;

        [Tooltip("Marked in the `source` column. Anything that is not a real device run must not say device.")]
        public string source = "device";

        string _path;
        int _framesSinceLastLock;
        float _dropoutSinceLastLock;
        int _lockCount;

        public string LogPath => _path;
        public int LockCount => _lockCount;

        static readonly string[] Fields =
        {
            "timestamp_utc", "unix_ms", "session", "source", "event", "shape",
            "held_s", "distance_deg", "confidence", "dropout_s", "frames", "note",
        };

        void Awake()
        {
            _path = string.IsNullOrEmpty(pathOverride) ? DefaultPath() : pathOverride;
            EnsureHeader();
        }

        static string DefaultPath()
        {
#if UNITY_EDITOR
            // Assets/../phases/... : the log lives with the phase documents, not with the build.
            string root = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.Combine(root, "phases", "p2-core-loop", "lock-log.csv");
#else
            return Path.Combine(Application.persistentDataPath, "lock-log.csv");
#endif
        }

        void OnEnable()
        {
            if (matcher == null) return;
            matcher.OnLocked += OnLocked;
            if (logUnlocks) matcher.OnUnlocked += OnUnlocked;
            matcher.OnDistanceUpdated += OnDistance;
            matcher.OnPausedChanged += OnPausedChanged;
        }

        void OnDisable()
        {
            if (matcher == null) return;
            matcher.OnLocked -= OnLocked;
            if (logUnlocks) matcher.OnUnlocked -= OnUnlocked;
            matcher.OnDistanceUpdated -= OnDistance;
            matcher.OnPausedChanged -= OnPausedChanged;
        }

        void OnDistance(float distance, float bestOther) => _framesSinceLastLock++;

        bool _paused;

        void OnPausedChanged(bool paused) => _paused = paused;

        void Update()
        {
            // A dropout during a hold is the thing this log exists to prove did not cost the user a lock.
            if (_paused) _dropoutSinceLastLock += Time.unscaledDeltaTime;
        }

        void OnLocked(string shapeId)
        {
            _lockCount++;
            Write("lock", shapeId, matcher.HoldProgress01 * matcher.holdSeconds,
                  _lockCount + " of the session");
        }

        void OnUnlocked(string shapeId) => Write("unlock", shapeId, 0f, "left the band");

        void Write(string evt, string shapeId, float held, string note)
        {
            var now = DateTime.UtcNow;
            var sb = new StringBuilder(160);
            sb.Append(now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(((DateTimeOffset)now).ToUnixTimeMilliseconds()).Append(',');
            sb.Append(SessionStamp).Append(',');
            sb.Append(source).Append(',');
            sb.Append(evt).Append(',');
            sb.Append(Csv(shapeId)).Append(',');
            sb.Append(held.ToString("0.000", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(matcher.DistanceDegrees.ToString("0.00", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(matcher.LastConfidence.ToString("0.00", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(_dropoutSinceLastLock.ToString("0.000", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(_framesSinceLastLock).Append(',');
            sb.Append(Csv(note));

            AppendLine(sb.ToString());
            _framesSinceLastLock = 0;
            _dropoutSinceLastLock = 0f;
        }

        string _sessionStamp;

        string SessionStamp => _sessionStamp ??= DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");

        static string Csv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.IndexOfAny(new[] { ',', '"', '\n' }) >= 0
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }

        void EnsureHeader()
        {
            try
            {
                if (File.Exists(_path) && new FileInfo(_path).Length > 0) return;
                var dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(_path, string.Join(",", Fields) + "\n");
                Debug.Log("[LockLogger] started " + _path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LockLogger] cannot write " + _path + ": " + e.Message);
                enabled = false;
            }
        }

        void AppendLine(string line)
        {
            try
            {
                File.AppendAllText(_path, line + "\n");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LockLogger] append failed: " + e.Message);
                enabled = false;
            }
        }
    }
}
