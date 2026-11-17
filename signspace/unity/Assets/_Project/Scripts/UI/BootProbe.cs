using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using Signspace.Matching;
using Signspace.Spatial;

namespace Signspace.UI
{
    /// <summary>
    /// Measures cold boot and every background cycle, and writes the rows `boot-times.csv` needs (P7.5).
    ///
    /// WHAT IS MEASURED, EXACTLY
    /// -------------------------
    ///   * `startup_ms` — from process start to the first frame this component sees. This is as close to
    ///     "cold boot" as a scene can observe; the splash and engine init happen before it.
    ///   * `hands_ms` — to the first frame the matcher reports a tracked hand.
    ///   * `interactive_ms` — to the first frame where **both** a hand and the shelf exist. That is the
    ///     task's "playable before the shelf resolves" line: if the shelf never resolves, the plinth
    ///     fallback counts, because the loop is playable on the plinth.
    ///   * `resume_ms` — from each `OnApplicationPause(false)` to the first frame afterwards.
    ///
    /// The file is written to `Application.persistentDataPath/boot-times.csv`, one row per launch, so a
    /// five-launch measurement is five app starts and one `adb pull`. It is deliberately NOT in the
    /// StreamingAssets folder: a build must not be the thing that measures itself into its own APK.
    ///
    /// The device rows are `HUMAN-TASKS` H24. This class produces them; it cannot produce the numbers.
    /// </summary>
    public class BootProbe : MonoBehaviour
    {
        public Matcher matcher;
        public ShelfAnchor shelf;

        [Tooltip("Off in the editor: an editor 'boot' is not a cold boot and must not enter the CSV.")]
        public bool writeFile = true;

        [Tooltip("A boot slower than this is logged as a warning so it cannot pass unnoticed.")]
        public float budgetSeconds = 15f;

        public float StartupMs { get; private set; } = -1f;
        public float HandsMs { get; private set; } = -1f;
        public float InteractiveMs { get; private set; } = -1f;
        public int ResumeCount { get; private set; }
        public string LastResumeMs { get; private set; } = "-";

        float _handsAt = -1f, _interactiveAt = -1f;
        float _pausedAt = -1f;
        bool _reported;
        readonly StringBuilder _resumes = new StringBuilder();

        void Awake()
        {
            StartupMs = Time.realtimeSinceStartup * 1000f;
            if (matcher == null) matcher = FindFirstObjectByType<Matcher>();
            if (shelf == null) shelf = FindFirstObjectByType<ShelfAnchor>();
        }

        void Update()
        {
            float now = Time.realtimeSinceStartup * 1000f;

            if (_handsAt < 0f && matcher != null && (!matcher.IsPaused || matcher.LastConfidence > 0.05f))
                _handsAt = now;

            // The shelf counts as ready the moment it has a mode at all: a surface, or the plinth.
            if (_interactiveAt < 0f && _handsAt > 0f && shelf != null && shelf.Result != ShelfAnchor.Outcome.NotStarted)
            {
                _interactiveAt = now;
                Report();
            }
        }

        void Report()
        {
            if (_reported) return;
            _reported = true;

            HandsMs = _handsAt - StartupMs;
            InteractiveMs = _interactiveAt - StartupMs;

            Debug.Log("[BootProbe] startup " + StartupMs.ToString("0") + " ms, hands " +
                      HandsMs.ToString("0") + " ms, interactive " + InteractiveMs.ToString("0") + " ms");
            if (InteractiveMs > budgetSeconds * 1000f)
                Debug.LogWarning("[BootProbe] interactive frame took " + (InteractiveMs / 1000f).ToString("0.0") +
                                 " s, over the " + budgetSeconds + " s budget (P7.5).");

            if (writeFile) WriteRow();
        }

        void WriteRow()
        {
            // In the editor the launch time is the editor's, so a row here would poison the table.
            if (Application.isEditor) return;

            try
            {
                string path = Path.Combine(Application.persistentDataPath, "boot-times.csv");
                bool header = !File.Exists(path);
                using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(stream))
                {
                    if (header)
                        writer.WriteLine("utc,startup_ms,hands_ms,interactive_ms,resumes,resume_ms");
                    writer.WriteLine(string.Join(",",
                        DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                        StartupMs.ToString("0"),
                        HandsMs.ToString("0"),
                        InteractiveMs.ToString("0"),
                        ResumeCount.ToString(CultureInfo.InvariantCulture),
                        string.IsNullOrEmpty(_resumes.ToString()) ? "-" : _resumes.ToString()));
                }
                Debug.Log("[BootProbe] row written to " + path);
            }
            catch (Exception error)
            {
                // A probe that throws is worse than no probe: the boot it was measuring now fails.
                Debug.LogWarning("[BootProbe] could not write the CSV: " + error.Message);
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                _pausedAt = Time.realtimeSinceStartup * 1000f;
                return;
            }

            if (_pausedAt < 0f) return;
            ResumeCount++;
            float elapsed = Time.realtimeSinceStartup * 1000f - _pausedAt;
            _pausedAt = -1f;
            LastResumeMs = elapsed.ToString("0");
            if (_resumes.Length > 0) _resumes.Append(";");
            _resumes.Append(LastResumeMs);
            Debug.Log("[BootProbe] resumed in " + LastResumeMs + " ms (cycle " + ResumeCount + ")");
        }
    }
}
