using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Signspace.Perf
{
    /// <summary>
    /// P1.8: the baseline frame budget, measured on the weaker device.
    ///
    /// This component exists because the phase has two traps and both of them are about honesty:
    ///
    ///   trap 1 - measuring in the editor or the simulator. Neither has the XR compositor, the
    ///            dynamic clock levels or a battery, so a number from either is not a baseline for
    ///            Quest 3S. Nothing here can prevent that, but the summary REFUSES to certify a run
    ///            that did not happen on a headset (see `deviceModel` / `graphicsDeviceName` and the
    ///            validation block).
    ///   trap 2 - measuring for 60 seconds and calling it thermal-stable. A Quest 3S is cool for the
    ///            first minutes and throttles later, so a short run cannot see the thing the run is
    ///            for. `requiredSeconds` is enforced: a run shorter than 10 minutes is written down as
    ///            INVALID rather than as a result.
    ///
    /// WHAT "fps" MEANS HERE
    /// The frame interval is what the user experiences, so it is measured from `unscaledDeltaTime`
    /// per frame; the per-frame CPU/GPU split comes from `FrameTimingManager`, which reports zeros
    /// unless **Player Settings > Frame Timing Stats** is enabled - so the summary states which of
    /// the two it actually got instead of printing `0.00 ms GPU` as if it were good news.
    ///
    /// "1% low" is defined before the data arrives, because there are several definitions: sort the
    /// frame intervals, take the worst 1% of frames, and report `1000 / mean(those)`. It is the
    /// number that decides whether the app feels bad, so it is reported next to the average, never
    /// instead of it.
    /// </summary>
    public class PerfProbe : MonoBehaviour
    {
        [Header("Run length")]
        [Tooltip("Seconds to sample. PERFORMANCE.md §2 specifies 15 min; P1.8's gate needs 10.")]
        public float durationSeconds = 900f;

        [Tooltip("Gate G1's minimum. Shorter runs are recorded as INVALID, not as results.")]
        public float requiredSeconds = 600f;

        [Tooltip("Excluded from the headline numbers: the first seconds include scene load and MRUK.")]
        public float warmupSeconds = 20f;

        [Header("Budget")]
        [Tooltip("72 Hz -> 13.9 ms. Every frame above this missed the deadline once.")]
        public float budgetFrameMs = 13.9f;

        [Tooltip("Read off the OVR Metrics overlay's FPS line before the run. Reported so a 90 Hz run " +
                 "cannot be compared against a 72 Hz budget by accident.")]
        public float assumedRefreshHz = 72f;

        [Tooltip("A frame this many times the budget is logged individually as a hitch.")]
        public float hitchMultiplier = 2f;

        [Header("Sampling")]
        public float windowSeconds = 10f;

        [Header("Output")]
        public bool writeFiles = true;
        public string jsonlFileName = "perf-baseline.jsonl";
        public string summaryFileName = "perf-baseline-summary.txt";

        // ---- live state, read by the OnGUI readout ----
        public bool Running { get; private set; }
        public bool Finished { get; private set; }
        public float Elapsed { get; private set; }
        public float LiveFps { get; private set; }
        public int ThermalStatus { get; private set; } = -1;
        public string Summary { get; private set; } = "";

        const int MaxFrames = 200000;      // ~37 min at 90 Hz; beyond this we stop recording frames

        float[] _frameMs;
        int _frameCount;
        int _firstSteadyFrame = -1;   // index of the first frame after warmup, set exactly once
        readonly List<string> _windows = new List<string>();
        readonly List<float> _hitchTimes = new List<float>();
        readonly List<float> _hitchMs = new List<float>();
        readonly List<string> _thermalTimeline = new List<string>();

        float _startAt;
        float _lastWindowAt;
        float _windowFrames;
        float _windowSumMs;
        float _windowMaxMs;
        float _liveAccum;
        int _liveFrames;
        bool _warnedNoFrameTiming;
        int _lastThermal = -1;
        float _pausedFor;
        bool _paused;
        float _pauseBegan;

        AndroidThermal _thermal;

        void Awake()
        {
            _frameMs = new float[MaxFrames];
            _thermal = new AndroidThermal();
            Application.targetFrameRate = -1;      // let the XR runtime own the cadence
        }

        void Start()
        {
            _startAt = Time.realtimeSinceStartup;
            _lastWindowAt = _startAt;
            Running = true;

            Debug.Log("[PerfProbe] device=" + SystemInfo.deviceModel +
                      " gfx=" + SystemInfo.graphicsDeviceName +
                      " refresh(assumed)=" + assumedRefreshHz + " Hz" +
                      " budget=" + budgetFrameMs + " ms, duration=" + durationSeconds + " s");
        }

        void OnApplicationPause(bool paused)
        {
            _paused = paused;
            if (paused) _pauseBegan = Time.realtimeSinceStartup;
            else
            {
                // Time spent with the headset off is not a frame budget measurement. It is excluded
                // from the run length too, otherwise a run left on a table could "pass" its duration.
                _pausedFor += Time.realtimeSinceStartup - _pauseBegan;
                Debug.Log("[PerfProbe] paused for " + (Time.realtimeSinceStartup - _pauseBegan).ToString("0.0") +
                          " s, excluded from the run");
            }
        }

        void Update()
        {
            if (!Running) return;
            if (_paused) return;

            float now = Time.realtimeSinceStartup;
            Elapsed = now - _startAt - _pausedFor;

            // ---- per-frame sampling -------------------------------------------------------
            float ms = Time.unscaledDeltaTime * 1000f;
            if (_firstSteadyFrame < 0 && Elapsed >= warmupSeconds) _firstSteadyFrame = _frameCount;
            if (_frameCount < MaxFrames) _frameMs[_frameCount++] = ms;

            _windowFrames++;
            _windowSumMs += ms;
            if (ms > _windowMaxMs) _windowMaxMs = ms;

            _liveAccum += ms;
            _liveFrames++;
            if (_liveFrames >= 30)
            {
                LiveFps = _liveAccum > 0f ? 1000f * _liveFrames / _liveAccum : 0f;
                _liveAccum = 0f;
                _liveFrames = 0;
            }

            if (ms > budgetFrameMs * hitchMultiplier && _hitchTimes.Count < 500)
            {
                _hitchTimes.Add(Elapsed);
                _hitchMs.Add(ms);
            }

            // ---- thermal, polled rather than assumed -------------------------------------
            int thermal = _thermal.Read();
            ThermalStatus = thermal;
            if (thermal >= 0 && thermal != _lastThermal)
            {
                _lastThermal = thermal;
                _thermalTimeline.Add(Elapsed.ToString("0.0", CultureInfo.InvariantCulture) + "s:" + thermal);
                Debug.Log("[PerfProbe] thermal status -> " + thermal + " (" + AndroidThermal.Describe(thermal) +
                          ") at " + Elapsed.ToString("0") + " s");
            }

            // ---- window flush ------------------------------------------------------------
            if (now - _lastWindowAt >= windowSeconds)
            {
                FlushWindow(now);
                _lastWindowAt = now;
            }

            if (Elapsed >= durationSeconds) Finish();
        }

        void FlushWindow(float now)
        {
            float seconds = now - _lastWindowAt;
            if (_windowFrames < 1 || seconds <= 0f) { _windowFrames = 0; _windowSumMs = 0; _windowMaxMs = 0; return; }

            float fps = _windowFrames / seconds;
            float avgMs = _windowSumMs / _windowFrames;
            bool warmup = (Elapsed - seconds) < warmupSeconds;

            // per-frame timings for this window, when the player setting allows it
            float cpuMs = 0f, gpuMs = 0f;
            int timingCount = 0;
            var timings = new FrameTiming[1];
            uint got = FrameTimingManager.GetLatestTimings(1, timings);
            if (got > 0)
            {
                cpuMs = (float)timings[0].cpuFrameTime;
                gpuMs = (float)timings[0].gpuFrameTime;
                timingCount = 1;
            }
            else if (!_warnedNoFrameTiming)
            {
                _warnedNoFrameTiming = true;
                Debug.LogWarning("[PerfProbe] FrameTimingManager returned nothing. CPU/GPU split will be " +
                                 "blank. Enable Project Settings > Player > Frame Timing Stats for the " +
                                 "profiler-sourced numbers; the fps and 1% low in this file do not " +
                                 "depend on it.");
            }

            string row =
                "{\"t\":" + Elapsed.ToString("0.0", CultureInfo.InvariantCulture) +
                ",\"windowS\":" + seconds.ToString("0.0", CultureInfo.InvariantCulture) +
                ",\"fps\":" + fps.ToString("0.0", CultureInfo.InvariantCulture) +
                ",\"avgFrameMs\":" + avgMs.ToString("0.00", CultureInfo.InvariantCulture) +
                ",\"maxFrameMs\":" + _windowMaxMs.ToString("0.0", CultureInfo.InvariantCulture) +
                ",\"cpuMs\":" + cpuMs.ToString("0.00", CultureInfo.InvariantCulture) +
                ",\"gpuMs\":" + gpuMs.ToString("0.00", CultureInfo.InvariantCulture) +
                ",\"timingSamples\":" + timingCount +
                ",\"thermal\":" + ThermalStatus +
                ",\"onBudget\":" + (avgMs <= budgetFrameMs ? "true" : "false") +
                ",\"warmup\":" + (warmup ? "true" : "false") +
                "}";

            _windows.Add(row);

            _windowFrames = 0;
            _windowSumMs = 0f;
            _windowMaxMs = 0f;
        }

        void Finish()
        {
            Running = false;
            Finished = true;
            FlushWindow(Time.realtimeSinceStartup);

            // ---- headline statistics -----------------------------------------------------
            // The warmup boundary is the exact frame index recorded when the clock crossed it, not
            // an estimate from an assumed frame rate - an assumption there would silently move the
            // boundary on a run that was not hitting 72.
            int firstIndex = _firstSteadyFrame >= 0 ? _firstSteadyFrame : 0;

            int n = Mathf.Max(0, _frameCount - firstIndex);
            if (n <= 0)
            {
                Summary = "no frames recorded";
                Debug.LogWarning("[PerfProbe] " + Summary);
                WriteFiles();
                return;
            }

            var sorted = new float[n];
            Array.Copy(_frameMs, firstIndex, sorted, 0, n);
            Array.Sort(sorted);

            double sum = 0.0;
            for (int i = 0; i < n; i++) sum += sorted[i];
            float meanMs = (float)(sum / n);

            int worstCount = Mathf.Max(1, Mathf.RoundToInt(n * 0.01f));
            double worstSum = 0.0;
            for (int i = n - worstCount; i < n; i++) worstSum += sorted[i];
            float worst1PctMeanMs = (float)(worstSum / worstCount);

            float p50 = sorted[Mathf.Clamp(Mathf.RoundToInt(n * 0.50f), 0, n - 1)];
            float p95 = sorted[Mathf.Clamp(Mathf.RoundToInt(n * 0.95f), 0, n - 1)];
            float p99 = sorted[Mathf.Clamp(Mathf.RoundToInt(n * 0.99f), 0, n - 1)];

            int overBudget = 0;
            for (int i = 0; i < n; i++) if (sorted[i] > budgetFrameMs) overBudget++;

            float avgFps = meanMs > 0f ? 1000f / meanMs : 0f;
            float onePercentLowFps = worst1PctMeanMs > 0f ? 1000f / worst1PctMeanMs : 0f;
            float runSeconds = Elapsed;

            // ---- thermal onset: first status that means throttling -----------------------
            float onset = -1f;
            int peak = -1;
            foreach (var entry in _thermalTimeline)
            {
                int colon = entry.IndexOf(':');
                if (colon <= 0) continue;
                int status;
                if (!int.TryParse(entry.Substring(colon + 1), out status)) continue;
                if (status > peak) peak = status;
                if (status >= AndroidThermal.Moderate && onset < 0f)
                {
                    float at;
                    if (float.TryParse(entry.Substring(0, colon), NumberStyles.Float,
                                       CultureInfo.InvariantCulture, out at)) onset = at;
                }
            }

            string verdict;
            if (runSeconds < requiredSeconds)
                verdict = "INVALID — ran " + runSeconds.ToString("0") + " s of the required " +
                          requiredSeconds.ToString("0") + " s. A short run is not a thermal measurement " +
                          "(trap 2); this file is evidence of a method, not a baseline.";
            else if (avgFps < assumedRefreshHz * 0.98f)
                verdict = "BELOW BUDGET — " + avgFps.ToString("0.0") + " fps against " +
                          assumedRefreshHz.ToString("0") + " Hz. The top three profiler costs are " +
                          "required in perf-baseline.md before moving on.";
            else if (onePercentLowFps < assumedRefreshHz * 0.90f)
                verdict = "BUDGET MET ON AVERAGE, NOT IN THE TAIL — 1% low " +
                          onePercentLowFps.ToString("0.0") + " fps. Hitching, not throughput.";
            else
                verdict = "PASS — " + avgFps.ToString("0.0") + " fps average, 1% low " +
                          onePercentLowFps.ToString("0.0") + " fps over " + runSeconds.ToString("0") + " s";

            var sb = new StringBuilder();
            sb.AppendLine("SIGNSPACE P1.8 — frame budget baseline");
            sb.AppendLine("when          : " + DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            sb.AppendLine("verdict       : " + verdict);
            sb.AppendLine();
            sb.AppendLine("DEVICE (this is what makes the run admissable)");
            sb.AppendLine("  model       : " + SystemInfo.deviceModel);
            sb.AppendLine("  gpu         : " + SystemInfo.graphicsDeviceName);
            sb.AppendLine("  cpu         : " + SystemInfo.processorType + " x" + SystemInfo.processorCount);
            sb.AppendLine("  memory      : " + SystemInfo.systemMemorySize + " MB");
            sb.AppendLine("  os          : " + SystemInfo.operatingSystem);
            sb.AppendLine("  graphicsApi : " + SystemInfo.graphicsDeviceType);
            sb.AppendLine("  app version : " + Application.version + "  (" + Application.unityVersion + ")");
            sb.AppendLine("  build       : " + (Application.isEditor ? "EDITOR — NOT ADMISSABLE" : "player"));
            sb.AppendLine();
            sb.AppendLine("RUN");
            sb.AppendLine("  duration    : " + runSeconds.ToString("0.0") + " s   (warmup excluded: " +
                          warmupSeconds.ToString("0") + " s)");
            sb.AppendLine("  refresh     : assumed " + assumedRefreshHz.ToString("0") + " Hz from the overlay");
            sb.AppendLine("  budget      : " + budgetFrameMs.ToString("0.0") + " ms/frame");
            sb.AppendLine("  frames used : " + n);
            sb.AppendLine();
            sb.AppendLine("FRAME RATE");
            sb.AppendLine("  average     : " + avgFps.ToString("0.00") + " fps   (" + meanMs.ToString("0.00") + " ms)");
            sb.AppendLine("  1% low      : " + onePercentLowFps.ToString("0.00") + " fps   (" +
                          worst1PctMeanMs.ToString("0.00") + " ms mean of the worst " + worstCount + " frames)");
            sb.AppendLine("  p50 / p95   : " + p50.ToString("0.00") + " / " + p95.ToString("0.00") + " ms");
            sb.AppendLine("  p99 / max   : " + p99.ToString("0.00") + " / " + sorted[n - 1].ToString("0.00") + " ms");
            sb.AppendLine("  over budget : " + overBudget + " frames (" +
                          (100f * overBudget / n).ToString("0.00") + "%) exceeded " +
                          budgetFrameMs.ToString("0.0") + " ms");
            sb.AppendLine("  hitches     : " + _hitchTimes.Count + " frames over " +
                          (budgetFrameMs * hitchMultiplier).ToString("0.0") + " ms" +
                          (_hitchTimes.Count > 0
                              ? "  first at " + _hitchTimes[0].ToString("0.0") + " s, worst " +
                                MaxOf(_hitchMs).ToString("0.0") + " ms"
                              : ""));
            sb.AppendLine();
            sb.AppendLine("THERMAL");
            sb.AppendLine("  source      : Android PowerManager thermal status (0 none … 6 shutdown)");
            sb.AppendLine("  timeline    : " + (_thermalTimeline.Count > 0
                              ? string.Join("  ", _thermalTimeline.ToArray())
                              : "no transitions reported (status stayed " + _lastThermal + ")"));
            sb.AppendLine("  throttling  : " + (onset >= 0f
                              ? "starts at " + onset.ToString("0") + " s (status ≥ " + AndroidThermal.Moderate + ")"
                              : "NONE in " + runSeconds.ToString("0") + " s"));
            sb.AppendLine("  peak status : " + peak);
            sb.AppendLine();
            sb.AppendLine("MARGIN AGAINST THE BUDGET");
            sb.AppendLine("  headroom    : " + (assumedRefreshHz - avgFps).ToString("0.0") + " fps" +
                          "  (" + (budgetFrameMs - meanMs).ToString("0.00") + " ms of the " +
                          budgetFrameMs.ToString("0.0") + " ms frame)");
            sb.AppendLine("  judge floor : 60 fps — the number to beat is 72");
            sb.AppendLine();
            sb.AppendLine("NOTE the CPU/GPU split comes from FrameTimingManager and is blank unless");
            sb.AppendLine("Player Settings > Frame Timing Stats is enabled. The GPU level (0-4) is read");
            sb.AppendLine("off the OVR Metrics overlay and written into the phase doc by hand.");

            Summary = sb.ToString();
            Debug.Log("[PerfProbe] " + verdict);
            WriteFiles();
        }

        static float MaxOf(List<float> values)
        {
            float max = 0f;
            for (int i = 0; i < values.Count; i++) if (values[i] > max) max = values[i];
            return max;
        }

        void WriteFiles()
        {
            if (!writeFiles) return;
            try
            {
                var dir = Application.persistentDataPath;
                File.WriteAllText(Path.Combine(dir, jsonlFileName), string.Join("\n", _windows.ToArray()) + "\n");
                File.WriteAllText(Path.Combine(dir, summaryFileName), Summary);
                Debug.Log("[PerfProbe] wrote " + Path.Combine(dir, summaryFileName));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[PerfProbe] could not write: " + e.Message);
            }
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            style.normal.textColor = Color.white;

            string text = "P1.8 FRAME BUDGET\n";
            text += "run      : " + Elapsed.ToString("0") + " / " + durationSeconds.ToString("0") + " s" +
                    (Elapsed < requiredSeconds ? "   (needs " + requiredSeconds.ToString("0") + ")" : "   ✓ length") + "\n";
            text += "fps      : " + LiveFps.ToString("0.0") + " / " + assumedRefreshHz.ToString("0") + " Hz\n";
            text += "budget   : " + budgetFrameMs.ToString("0.0") + " ms  (" +
                    (LiveFps >= assumedRefreshHz * 0.98f ? "on budget" : "OVER") + ")\n";
            text += "thermal  : " + (ThermalStatus < 0 ? "unavailable" : ThermalStatus + " " +
                    AndroidThermal.Describe(ThermalStatus)) + "\n";
            text += "hitches  : " + _hitchTimes.Count + "\n";
            text += Finished ? "FINISHED — summary written\n" : "keep both hands up and one object anchored\n";
            text += Application.isEditor ? "!! EDITOR — this number is not a baseline" : "player build";

            GUI.Box(new Rect(12f, 12f, 460f, 210f), GUIContent.none);
            GUI.Label(new Rect(24f, 22f, 440f, 180f), text, style);
        }
    }

    /// <summary>
    /// Android thermal status, read from `PowerManager.getCurrentThermalStatus()` (API 29+).
    ///
    /// Why not an OVR API: the metric that matters is "the system has started cutting clocks", and
    /// Android's thermal service is the thing that signals it. Meta's own overlay shows temperatures
    /// and clock levels rather than a single throttle state, so reading the platform's status gives a
    /// timestamp that can be compared with the frame-rate trace directly.
    ///
    /// If the call is unavailable (older OS, or a stripped service) this returns -1 and the probe
    /// says "unavailable" instead of inventing a zero ("0 = none" would be a lie that looks like a
    /// pass). The documented fallback is to poll `adb shell dumpsys thermalservice` during the run.
    /// </summary>
    public class AndroidThermal
    {
        public const int None = 0;
        public const int Light = 1;
        public const int Moderate = 2;      // Android: "throttling has started"

        AndroidJavaObject _powerManager;
        AndroidJavaObject _activity;
        bool _failed;

        public AndroidThermal()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                _activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                _powerManager = _activity.Call<AndroidJavaObject>("getSystemService", "power");
            }
            catch (Exception e)
            {
                _failed = true;
                Debug.LogWarning("[PerfProbe] no PowerManager: " + e.Message);
            }
#endif
        }

        public int Read()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_failed || _powerManager == null) return -1;
            try { return _powerManager.Call<int>("getCurrentThermalStatus"); }
            catch { _failed = true; return -1; }
#else
            return -1;
#endif
        }

        public static string Describe(int status)
        {
            switch (status)
            {
                case -1: return "unavailable";
                case 0: return "none";
                case 1: return "light";
                case 2: return "moderate (throttling)";
                case 3: return "severe";
                case 4: return "critical";
                case 5: return "emergency";
                case 6: return "shutdown";
                default: return "unknown";
            }
        }
    }
}
