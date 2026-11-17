using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Signspace.Core
{
    /// <summary>
    /// The timeline of one uncoached session (P8.2, P8.3), as a pure data structure.
    ///
    /// WHY THIS IS PURE
    /// ----------------
    /// A playtest's numbers are the only evidence in this project that cannot be produced by a script:
    /// a stranger's first grab, first lock, first near-miss, and the moments they stopped moving. If the
    /// code that records them lives inside a MonoBehaviour, the only way to check the arithmetic is to
    /// put on a headset — which is exactly what the phase is trying to avoid needing twice. So the
    /// timeline is a class with no Unity types: the probe feeds it timestamps, `P8Checks` feeds it a
    /// synthetic session, and the CSV it emits is the same file either way.
    ///
    /// WHAT IT REFUSES TO DO
    /// ---------------------
    /// It never invents a row. A session with no lock has `first_lock=-` and the report says so; the
    /// 75 s target is a predicate over a recorded number, not a value the class fills in.
    /// </summary>
    public class PlaytestTimeline
    {
        public const float TargetFirstLockSeconds = 75f;

        public const string SessionStart = "session_start";
        public const string HandSeen = "hand_seen";
        public const string FirstGrab = "first_grab";
        public const string BandClose = "band_close";
        public const string FirstLock = "first_lock";
        public const string FirstNearMiss = "first_near_miss";
        public const string FirstSwitch = "first_switch";
        public const string Stall = "stall";
        public const string SessionEnd = "session_end";

        public struct Entry
        {
            public float seconds;      // from session start
            public string name;
            public string detail;
        }

        readonly List<Entry> _entries = new List<Entry>();
        readonly HashSet<string> _once = new HashSet<string>();

        public string Session { get; private set; } = "unstarted";
        public string Source { get; private set; } = "unset";
        public IReadOnlyList<Entry> Entries => _entries;
        public int StallCount { get; private set; }

        /// <summary>Starts a session. `source` must be "device", "editor" or "fixture" — never a claim by omission.</summary>
        public void Begin(string session, string source, float atSeconds)
        {
            Session = string.IsNullOrEmpty(session) ? "unknown" : session;
            Source = string.IsNullOrEmpty(source) ? "unset" : source;
            _entries.Clear();
            _once.Clear();
            StallCount = 0;
            Add(atSeconds, SessionStart, source);
        }

        /// <summary>
        /// Records an event. Events that are conceptually once-per-session are recorded once: a second
        /// lock must not overwrite the first-lock time, or the phase's headline number becomes the last
        /// lock instead of the first.
        /// </summary>
        public bool Mark(float atSeconds, string name, string detail = "")
        {
            if (IsOnce(name) && !_once.Add(name)) return false;

            Add(atSeconds, name, detail);
            if (name == Stall) StallCount++;
            return true;
        }

        public static bool IsOnce(string name)
        {
            switch (name)
            {
                case HandSeen:
                case FirstGrab:
                case BandClose:
                case FirstLock:
                case FirstNearMiss:
                case FirstSwitch:
                case SessionEnd:
                    return true;
                default:
                    return false;
            }
        }

        void Add(float seconds, string name, string detail)
        {
            _entries.Add(new Entry
            {
                seconds = Math.Max(0f, seconds),
                name = name,
                detail = detail ?? "",
            });
        }

        public bool Has(string name) => _once.Contains(name);

        /// <summary>The first occurrence in seconds, or -1 when it never happened.</summary>
        public float First(string name)
        {
            for (int i = 0; i < _entries.Count; i++)
                if (_entries[i].name == name) return _entries[i].seconds;
            return -1f;
        }

        public int Count(string name)
        {
            int n = 0;
            for (int i = 0; i < _entries.Count; i++)
                if (_entries[i].name == name) n++;
            return n;
        }

        public bool HasFirstLock => First(FirstLock) >= 0f;

        /// <summary>G8's criterion, as a predicate over a recorded time. False when nothing was recorded.</summary>
        public bool MeetsFirstLockTarget =>
            HasFirstLock && First(FirstLock) <= TargetFirstLockSeconds;

        /// <summary>One line for the log and the report: the numbers, or "-" where there are none.</summary>
        public string Summary()
        {
            return "first_grab=" + Seconds(this.First(FirstGrab)) +
                   " first_lock=" + Seconds(this.First(FirstLock)) +
                   " first_near_miss=" + Seconds(this.First(FirstNearMiss)) +
                   " stalls=" + StallCount +
                   " locks=" + Count(FirstLock) +
                   " source=" + Source;
        }

        static string Seconds(float value) =>
            value < 0f ? "-" : value.ToString("0.0", CultureInfo.InvariantCulture) + "s";

        public static string Header =>
            "timestamp_utc,unix_ms,session,source,event,seconds,detail";

        /// <summary>CSV rows for every entry, with the event's own UTC time reconstructed from its offset.</summary>
        public IEnumerable<string> CsvRows(DateTime sessionStartUtc)
        {
            foreach (var entry in _entries)
            {
                var at = sessionStartUtc.AddSeconds(entry.seconds);
                yield return string.Join(",",
                    at.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                    ((DateTimeOffset)at).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
                    Escape(Session),
                    Escape(Source),
                    Escape(entry.name),
                    entry.seconds.ToString("0.000", CultureInfo.InvariantCulture),
                    Escape(entry.detail));
            }
        }

        static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.IndexOfAny(new[] { ',', '"', '\n' }) >= 0
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }

        /// <summary>A multi-line report for the console, so a session can be read without opening Excel.</summary>
        public string Report()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Playtest] session " + Session + " (" + Source + ")");
            foreach (var entry in _entries)
                sb.AppendLine("  " + entry.seconds.ToString("0.0", CultureInfo.InvariantCulture) + "s  " +
                              entry.name + (string.IsNullOrEmpty(entry.detail) ? "" : " — " + entry.detail));
            sb.AppendLine("[Playtest] SUMMARY " + Summary() +
                          " target<" + TargetFirstLockSeconds + "s=" +
                          (MeetsFirstLockTarget ? "MET" : (HasFirstLock ? "MISSED" : "NO LOCK")));
            return sb.ToString();
        }
    }
}
