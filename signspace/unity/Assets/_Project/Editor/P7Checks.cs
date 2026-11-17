using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Signspace.Core;
using Signspace.Gaze;
using Signspace.Hands;
using Signspace.Matching;
using Signspace.Spatial;
using Signspace.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Signspace.Editor
{
    /// <summary>
    /// Headless checks for P7.1–P7.7.
    ///
    /// WHAT THESE PROVE, AND WHAT THEY DO NOT
    /// -------------------------------------
    /// They prove the mix stays under the ceiling, every cue has a clip or a documented silence, the
    /// text budget is what the document says it is, the narrow-profile geometry fits the safe cone, the
    /// scene has no light or shadow to pay for, and each hostile condition has a written answer that
    /// runs. They do **not** prove sustained frame rate, thermal behaviour, cold-boot seconds, crash
    /// freedom over 20 launches, or that a judge understands the onboarding. Those are device rows and
    /// they stay NOT RUN in `phases/p7-polish/` and `docs/00-plan/HUMAN-TASKS.md`.
    /// </summary>
    public static class P7Checks
    {
        const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        const string Sfx = "Assets/_Project/Audio/SFX/";
        const float MixCeiling = 0.5012f;   // -6 dBFS

        static int _failures;
        static int _passes;
        static bool _exitOnFinish = true;
        static int _startPasses, _startFailures;

        static void Require(bool condition, string label)
        {
            if (condition) { _passes++; Debug.Log("  PASS  " + label); }
            else { _failures++; Debug.LogError("  FAIL  " + label); }
        }

        static void Info(string line) => Debug.Log("  ....  " + line);

        static bool OpenScene()
        {
            if (!File.Exists(Path.GetFullPath(ScenePath)))
            {
                Debug.LogError("[P7Checks] " + ScenePath + " is missing; run SIGNSPACE/Build main scene.");
                return false;
            }
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            return true;
        }

        static string DocsRoot => Path.GetFullPath(Application.dataPath + "/../../docs");
        static string PhaseRoot => Path.GetFullPath(Application.dataPath + "/../../phases/p7-polish");

        // ------------------------------------------------------------------ P7.1 sound

        [MenuItem("SIGNSPACE/P7.1 Check sound")]
        public static void CheckSound()
        {
            Reset("P7.1 sound");
            if (!OpenScene()) { Finish("P7.1"); return; }

            var sound = UnityEngine.Object.FindFirstObjectByType<SoundDirector>();
            var band = UnityEngine.Object.FindFirstObjectByType<BandAudio>();
            Require(sound != null, "a SoundDirector is in the scene");
            Require(band != null && band.IsMapped, "the three band sounds are still mapped");
            if (sound == null) { Finish("P7.1"); return; }

            // Every cue the app can raise, with a clip, under the ceiling.
            var cues = (SoundDirector.Cue[])Enum.GetValues(typeof(SoundDirector.Cue));
            foreach (var cue in cues)
            {
                bool has = sound.HasClip(cue);
                Require(has, "cue " + cue + " has a clip");
                if (!has) continue;

                var clip = sound.ClipFor(cue);
                float peak = Peak(clip);
                Require(peak > 0f && peak <= MixCeiling,
                        cue + " peaks at " + Dbfs(peak) + " dBFS (ceiling -6)");
            }

            Require(sound.rotateClip != null, "the rotation texture is bound to the loop source");

            // Shape rules from SOUND-MAP §3: the chime is long and the near-miss is short and different.
            var chime = sound.ClipFor(SoundDirector.Cue.LockChime);
            var near = sound.ClipFor(SoundDirector.Cue.NearMiss);
            if (chime != null)
                Require(chime.length >= 1.2f && chime.length <= 2.0f,
                        "the lock chime is " + chime.length.ToString("0.00") + " s (map: ~1.6)");
            if (near != null)
            {
                Require(near.length <= 0.6f, "the near-miss is " + near.length.ToString("0.00") + " s");
                Require(Peak(near) < Peak(chime),
                        "the near-miss is quieter than the chime (" + Dbfs(Peak(near)) + " vs " +
                        Dbfs(Peak(chime)) + " dBFS)");
                Require(!SamePitchSet(chime, near),
                        "the near-miss is not a transposition of the chime (different intervals)");
            }

            // Short-clip rule: 400 ms, except the chime, its variant and the loop.
            var longAllowed = new HashSet<SoundDirector.Cue>
            {
                SoundDirector.Cue.LockChime, SoundDirector.Cue.ShelfFull,
            };
            foreach (var cue in cues)
            {
                var clip = sound.ClipFor(cue);
                if (clip == null || longAllowed.Contains(cue)) continue;
                Require(clip.length <= 0.45f, cue + " is " + clip.length.ToString("0.00") + " s (<= 450 ms)");
            }

            Require(SoundDirector.DocumentedSilences.Length >= 5,
                    SoundDirector.DocumentedSilences.Length + " silences documented");

            // File budget, from the folder rather than from the inspector.
            var files = Directory.GetFiles(Sfx.Replace("Assets/", Application.dataPath + "/"), "*.wav");
            long total = 0;
            foreach (var file in files) total += new FileInfo(file).Length;
            Require(total < 2 * 1024 * 1024, "SFX folder is " + (total / 1024) + " kB (budget 2048 kB)");
            Require(files.Length >= cues.Length, files.Length + " WAV files for " + cues.Length + " cues");

            var map = Path.Combine(DocsRoot, "04-design/SOUND-MAP.md");
            if (File.Exists(map))
            {
                string text = File.ReadAllText(map);
                Require(text.IndexOf("near_miss", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        text.IndexOf("near-miss", StringComparison.OrdinalIgnoreCase) >= 0,
                        "SOUND-MAP names the near-miss sound");
            }

            Finish("P7.1");
        }

        static float Peak(AudioClip clip)
        {
            if (clip == null) return 0f;
            try
            {
                var data = new float[Mathf.Max(1, clip.samples) * Mathf.Max(1, clip.channels)];
                if (!clip.GetData(data, 0)) return 0f;
                float peak = 0f;
                foreach (var sample in data) peak = Mathf.Max(peak, Mathf.Abs(sample));
                return peak;
            }
            catch (Exception)
            {
                return 0f;
            }
        }

        static string Dbfs(float peak) =>
            peak <= 0f ? "-inf" : (20f * Mathf.Log10(peak)).ToString("0.0");

        /// <summary>True when the two clips are the same intervals, i.e. the near-miss is a transposition.</summary>
        static bool SamePitchSet(AudioClip a, AudioClip b)
        {
            if (a == null || b == null) return false;
            var pa = DominantPitches(a, 3);
            var pb = DominantPitches(b, 3);
            if (pa.Count < 2 || pb.Count < 2) return false;

            float ratioA = pa[1] / pa[0];
            float ratioB = pb[1] / pb[0];
            // A rising fifth (1.5) and a falling third (0.79) are nowhere near each other; a transposed
            // chime would give the same ratio up to the error of a 4096-point DFT.
            return Mathf.Abs(ratioA - ratioB) < 0.08f;
        }

        static List<float> DominantPitches(AudioClip clip, int count)
        {
            var result = new List<float>();
            try
            {
                int n = 4096;
                if (clip.samples < n) return result;
                var data = new float[clip.samples * clip.channels];
                if (!clip.GetData(data, 0)) return result;

                int offset = 0;
                int best = 0;
                float bestEnergy = 0f;
                for (int start = 0; start + n <= data.Length; start += n)
                {
                    float energy = 0f;
                    for (int i = 0; i < n; i++) energy += data[start + i] * data[start + i];
                    if (energy > bestEnergy) { bestEnergy = energy; best = start; }
                }
                offset = best;

                // Goertzel over 200-2000 Hz in 5 Hz steps; enough to tell a fifth from a third.
                var peaks = new List<(float hz, float power)>();
                for (float hz = 180f; hz < 2000f; hz += 5f)
                {
                    float w = 2f * Mathf.PI * hz / clip.frequency;
                    float coeff = 2f * Mathf.Cos(w);
                    float s0 = 0f, s1 = 0f, s2 = 0f;
                    for (int i = 0; i < n; i++)
                    {
                        s0 = data[offset + i] + coeff * s1 - s2;
                        s2 = s1;
                        s1 = s0;
                    }
                    float power = s1 * s1 + s2 * s2 - coeff * s1 * s2;
                    peaks.Add((hz, power));
                }

                peaks.Sort((x, y) => y.power.CompareTo(x.power));
                foreach (var peak in peaks)
                {
                    if (result.Count >= count) break;
                    bool close = result.Any(hz => Mathf.Abs(hz - peak.hz) < 40f);
                    if (!close && peak.power > 0f) result.Add(peak.hz);
                }
            }
            catch (Exception) { }
            return result;
        }

        // ------------------------------------------------------------------ P7.2 text budget

        [MenuItem("SIGNSPACE/P7.2 Check text budget")]
        public static void CheckText()
        {
            Reset("P7.2 text budget");
            if (!OpenScene()) { Finish("P7.2"); return; }

            var texts = UnityEngine.Object.FindObjectsByType<Text>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            int instructionalWords = 0;
            var lines = new List<string>();

            foreach (var text in texts)
            {
                if (text == null || string.IsNullOrWhiteSpace(text.text)) continue;

                string path = PathOf(text.transform);
                if (text.GetComponentInParent<DevOnly>() != null) { lines.Add("dev   " + path); continue; }
                if (IsSurvivor(path)) { lines.Add("keep  " + path + "  \"" + Trim(text.text) + "\""); continue; }
                if (!text.gameObject.activeInHierarchy) { lines.Add("off   " + path + "  \"" + Trim(text.text) + "\""); continue; }

                int words = WordCount(text.text);
                instructionalWords += words;
                lines.Add("WORDS " + path + "  " + words + "  \"" + Trim(text.text) + "\"");
            }

            foreach (var line in lines) Info(line);
            Info("instructional words visible at start: " + instructionalWords);

            Require(instructionalWords <= 3,
                    "<= 3 instructional words in the first five minutes (found " + instructionalWords + ")");

            // The three survivors the task names, each present in the build.
            var credits = UnityEngine.Object.FindFirstObjectByType<CreditsPanel>();
            Require(credits != null, "the credit block exists (survivor 1)");
            var mirrorText = texts.Any(t => t != null &&
                PathOf(t.transform).Contains("HonestMirror") &&
                t.text.IndexOf("replacement", StringComparison.OrdinalIgnoreCase) >= 0);
            Require(mirrorText, "the 'not a replacement' line exists (survivor 2)");
            var nearMiss = UnityEngine.Object.FindFirstObjectByType<NearMissDisplay>();
            Require(nearMiss != null, "the near-miss template exists (survivor 3)");

            // No blocking splash and no wordy instruction hiding in the dev overlay.
            var overlay = UnityEngine.Object.FindFirstObjectByType<DebugOverlay>();
            Require(overlay == null || overlay.GetComponent<DevOnly>() != null,
                    "the debug overlay is dev-only, so its numbers do not count or ship");

            var budget = Path.Combine(PhaseRoot, "text-budget.md");
            Require(File.Exists(budget), "phases/p7-polish/text-budget.md exists");
            if (File.Exists(budget))
            {
                string text = File.ReadAllText(budget);
                Require(text.Contains("instructional"), "the budget document defines what it counts");
                Require(text.Contains("NOT RUN"), "the cold-start-to-interaction row is marked NOT RUN");
            }

            var cue = UnityEngine.Object.FindFirstObjectByType<OnboardingCue>();
            Require(cue != null && cue.referenceHand != null,
                    "the onboarding cue is a spatial pulse on the reference hand, not a label");

            Finish("P7.2");
        }

        static bool IsSurvivor(string path)
        {
            return path.Contains("Credits") || path.Contains("HonestMirror") ||
                   path.Contains("NearMiss") || path.Contains("Fallback") || path.Contains("Explanation");
        }

        static int WordCount(string s)
        {
            return s.Split(new[] { ' ', '\n', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        static string Trim(string s) => s.Length <= 60 ? s.Replace("\n", " ") : s.Substring(0, 57).Replace("\n", " ") + "...";

        static string PathOf(Transform transform)
        {
            var parts = new List<string>();
            for (var t = transform; t != null; t = t.parent) parts.Insert(0, t.name);
            return string.Join("/", parts);
        }

        // ------------------------------------------------------------------ P7.3 FoV

        [MenuItem("SIGNSPACE/P7.3 Check field of view")]
        public static void CheckFov()
        {
            Reset("P7.3 field of view");
            if (!OpenScene()) { Finish("P7.3"); return; }

            var reflow = UnityEngine.Object.FindFirstObjectByType<ShelfReflow>();
            var tray = UnityEngine.Object.FindFirstObjectByType<A11yToggleTray>();
            var hud = UnityEngine.Object.FindFirstObjectByType<A11yHud>();
            Require(reflow != null && reflow.shelf != null, "the shelf reflow is wired to the shelf");
            Require(tray != null, "the accessibility tray is in the scene");
            Require(hud != null, "the accessibility HUD is in the scene");
            if (reflow == null || tray == null) { Finish("P7.3"); return; }

            // The reflow decision itself, at both profiles, through its own code path.
            reflow.overrideHorizontalFovDegrees = 110f;
            reflow.Evaluate();
            Require(!reflow.IsTray && reflow.CurrentColumns == reflow.wideColumns,
                    "at 110° the shelf is a " + reflow.CurrentColumns + "-column grid");
            Require(reflow.AllSlotsInsideSafeCone,
                    "at 110° every slot is inside the " + (reflow.safeConeDegrees * 0.5f) + "° half-cone");

            reflow.overrideHorizontalFovDegrees = 70f;
            reflow.Evaluate();
            Require(reflow.IsTray, "at 70° the shelf reflows to the tray");
            Require(reflow.CurrentColumns == reflow.trayColumns,
                    "at 70° the tray has " + reflow.CurrentColumns + " columns");
            Require(reflow.AllSlotsInsideSafeCone,
                    "at 70° every slot is inside the safe cone (worst " +
                    reflow.MaxSlotAngleDegrees.ToString("0.0") + "°)");

            // The P6 tray and HUD were never FoV-aware. At 70° the tray sat ~40° down and to the right.
            tray.reflow = reflow;
            bool narrow = tray.ApplyFov(70f);
            Require(narrow, "the tray switches to its narrow pose at 70°");
            float narrowAngle = tray.MaxAngleDegrees(70f);
            Require(narrowAngle <= 28f,
                    "narrow tray worst corner is " + narrowAngle.ToString("0.0") +
                    "° off-axis (half-cone 27.5°)");

            tray.ApplyFov(110f);
            float wideAngle = tray.MaxAngleDegrees(110f);
            Info("wide tray worst corner is " + wideAngle.ToString("0.0") +
                 "° — below the eye line by design, reached by looking down, not part of the loop");
            Require(wideAngle <= 45f, "wide tray is within a comfortable look-down angle");

            float hudAngle = Mathf.Atan2(
                Mathf.Sqrt(Mathf.Pow(700f * hud.pixelsToMetres * 0.5f, 2f) +
                           Mathf.Pow(hud.below + 150f * hud.pixelsToMetres, 2f)),
                hud.distance) * Mathf.Rad2Deg;
            Require(hudAngle <= 28f,
                    "the HUD's worst corner is " + hudAngle.ToString("0.0") + "° off-axis at " +
                    hud.distance.ToString("0.00") + " m");

            var doc = Path.Combine(DocsRoot, "02-tech/MRUK-SHELF.md");
            Require(File.Exists(doc), "MRUK-SHELF.md exists");
            if (File.Exists(doc))
            {
                string text = File.ReadAllText(doc);
                Require(text.Contains("70"), "MRUK-SHELF.md records the 70° profile");
                Require(text.Contains("NOT RUN"), "the device FoV pass is marked NOT RUN");
            }

            Finish("P7.3");
        }

        // ------------------------------------------------------------------ P7.4 performance

        [MenuItem("SIGNSPACE/P7.4 Check performance posture")]
        public static void CheckPerformance()
        {
            Reset("P7.4 performance posture");
            if (!OpenScene()) { Finish("P7.4"); return; }

            var perf = UnityEngine.Object.FindFirstObjectByType<PerfSettings>();
            Require(perf != null, "a PerfSettings component is in the scene");
            if (perf != null)
            {
                Require(perf.targetFrameRate == 72, "the frame-rate target is 72");
                Require(perf.disableShadows, "realtime shadows are switched off");
                Require(perf.disableRealtimeReflectionProbes, "realtime reflection probes are off");
                Require(perf.foveation == PerfSettings.Foveation.Medium,
                        "fixed foveated rendering is set to medium");
            }

            var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            Require(lights.Length == 0, "zero realtime lights in the scene (found " + lights.Length + ")");

            int shadowCasters = 0;
            long vertices = 0;
            int renderers = 0;
            foreach (var meshRenderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                renderers++;
                if (meshRenderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off) shadowCasters++;
                var filter = meshRenderer.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null) vertices += filter.sharedMesh.vertexCount;
            }
            Require(shadowCasters == 0, "zero shadow-casting renderers (found " + shadowCasters + ")");
            Info(renderers + " mesh renderers, " + vertices + " vertices in the saved scene");
            Require(vertices < 250000, "scene vertices under the 250k guard (" + vertices + ")");

            // Textures: one hero allowed at 1024, everything else <= 512.
            int over512 = 0, hero = 0, biggest = 0;
            string biggestName = "none";
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null) continue;
                int size = Mathf.Max(texture.width, texture.height);
                if (size > biggest) { biggest = size; biggestName = Path.GetFileName(path); }
                if (size > 1024) over512++;
                else if (size > 512) hero++;
            }
            Info("biggest texture " + biggestName + " at " + biggest + " px");
            Require(over512 == 0, "no texture above 1024 px (" + over512 + " found)");
            Require(hero <= 1, "at most one hero texture above 512 px (" + hero + " found)");

            // The pipeline asset is where MSAA lives.
            var pipeline = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(
                "Assets/_Project/Rendering/SignSpacePipeline.asset");
            Require(pipeline != null, "the URP asset exists");
            if (pipeline != null)
            {
                int msaa = pipeline.msaaSampleCount;
                Info("URP MSAA is " + msaa + "x");
                Require(msaa == 2 || msaa == 4, "MSAA is 2x or 4x (" + msaa + "x)");
            }

            var doc = Path.Combine(DocsRoot, "02-tech/PERFORMANCE.md");
            Require(File.Exists(doc), "PERFORMANCE.md exists");
            if (File.Exists(doc))
            {
                string text = File.ReadAllText(doc);
                Require(text.Contains("NOT RUN"), "the sustained-fps and thermal rows are marked NOT RUN");
            }

            Finish("P7.4");
        }

        // ------------------------------------------------------------------ P7.5 boot

        [MenuItem("SIGNSPACE/P7.5 Check boot readiness")]
        public static void CheckBoot()
        {
            Reset("P7.5 boot readiness");
            if (!OpenScene()) { Finish("P7.5"); return; }

            var boot = UnityEngine.Object.FindFirstObjectByType<BootProbe>();
            Require(boot != null, "a BootProbe is in the scene");
            if (boot != null)
            {
                Require(boot.matcher != null && boot.shelf != null, "the probe sees the matcher and the shelf");
                Require(boot.budgetSeconds <= 15f, "the boot budget is " + boot.budgetSeconds + " s");
                Require(boot.writeFile, "the probe writes rows in a device build");
            }

            Info("splash animation: " + SplashSettings.AnimationName);
            Require(SplashSettings.IsStatic,
                    "the splash is static: no logo animation longer than 1 s");

            // The loop must not wait for a surface: the plinth is a mode, not an error.
            var shelf = UnityEngine.Object.FindFirstObjectByType<ShelfAnchor>();
            Require(shelf != null && shelf.plinthRoot != null, "the plinth fallback exists, so no surface still boots");

            var csv = Path.Combine(PhaseRoot, "boot-times.csv");
            Require(File.Exists(csv), "phases/p7-polish/boot-times.csv exists");
            if (File.Exists(csv))
            {
                string text = File.ReadAllText(csv);
                Require(text.Contains("interactive_ms"), "the CSV has the interactive column");
                Require(text.Contains("NOT RUN"), "the five-launch row is marked NOT RUN");
            }

            Finish("P7.5");
        }

        // ------------------------------------------------------------------ P7.6 hostile conditions

        [MenuItem("SIGNSPACE/P7.6 Check crash posture")]
        public static void CheckCrashPosture()
        {
            Reset("P7.6 crash posture");
            if (!OpenScene()) { Finish("P7.6"); return; }

            var table = HostileGuard.Table;
            Require(table.Length == 5, "five hostile conditions have a written answer (" + table.Length + ")");

            var seen = new HashSet<HostileGuard.Condition>();
            foreach (var verdict in table)
            {
                Require(seen.Add(verdict.condition), verdict.condition + " appears once");
                Require(!string.IsNullOrWhiteSpace(verdict.answer), verdict.condition + " has an answer");
                Require(!string.IsNullOrWhiteSpace(verdict.disabled), verdict.condition + " says what it disables");
            }

            // The inspect path must not throw when everything is missing - that is the no-camera build.
            var guardGo = new GameObject("P7GuardProbe");
            try
            {
                var guard = guardGo.AddComponent<HostileGuard>();
                var found = guard.Inspect();
                Require((found & HostileGuard.Condition.NoCamera) != 0,
                        "a build with no camera reports NoCamera instead of throwing");
                Require(!string.IsNullOrEmpty(guard.Report), "the guard reports a state");
            }
            catch (Exception error)
            {
                Require(false, "HostileGuard.Inspect threw with nothing wired: " + error.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(guardGo);
            }

            var realGuard = UnityEngine.Object.FindFirstObjectByType<HostileGuard>();
            Require(realGuard != null && realGuard.matcher != null && realGuard.shelf != null,
                    "the scene's guard is wired to the matcher and the shelf");

            var doc = Path.Combine(PhaseRoot, "crash-sweep.md");
            Require(File.Exists(doc), "phases/p7-polish/crash-sweep.md exists");
            if (File.Exists(doc))
            {
                string text = File.ReadAllText(doc);
                Require(text.Contains("NOT RUN"), "the 20-launch row is marked NOT RUN");
                foreach (var condition in new[] { "no surface", "no eye tracking", "tracking lost", "room changed" })
                    Require(text.IndexOf(condition, StringComparison.OrdinalIgnoreCase) >= 0,
                            "crash-sweep.md covers: " + condition);
            }

            Finish("P7.6");
        }

        // ------------------------------------------------------------------ P7.7 accessibility regression

        [MenuItem("SIGNSPACE/P7.7 Check accessibility regression")]
        public static void CheckA11yRegression()
        {
            Reset("P7.7 accessibility regression");
            if (!OpenScene()) { Finish("P7.7"); return; }

            // Re-run the P6 suite in this process: polish touched the matcher, the tray and the scene.
            int p6Failures = P6Checks.RunInline();
            Require(p6Failures == 0,
                    "the whole P6 suite still passes after the polish pass (" + p6Failures + " failures)");

            var tray = UnityEngine.Object.FindFirstObjectByType<A11yToggleTray>();
            var settings = UnityEngine.Object.FindFirstObjectByType<A11ySettings>();
            var router = UnityEngine.Object.FindFirstObjectByType<EyesOnlyRouter>();
            var targets = UnityEngine.Object.FindFirstObjectByType<FingerTargetsBuilder>();
            var calibration = UnityEngine.Object.FindFirstObjectByType<Calibration>();

            Require(tray != null && settings != null && router != null && targets != null && calibration != null,
                    "every accessibility component survived the polish pass");
            if (tray == null || settings == null) { Finish("P7.7"); return; }

            int blocks = tray.Build();
            Require(blocks == A11ySettings.All.Length + 1,
                    "the tray still has " + blocks + " blocks (" + A11ySettings.All.Length + " toggles + calibrate)");
            Require(A11ySettings.All.Length == 7, "all seven toggles are present");
            Require(targets.Build() == 5, "all five fingertip targets still build");

            var data = new SaveData();
            Require(data.captionsOn, "captions are on by default after the polish pass");
            Require(!data.eyesOnly && !data.oneHanded, "the opt-in paths still default off");

            // The calibration math is the one thing polish could silently break by touching the matcher.
            var scale = Calibration.ComputeScales(
                new[] { 0f, 0f, 0f, 0f, 0f }, new[] { 45f, 45f, 45f, 45f, 45f },
                new[] { 90f, 90f, 90f, 90f, 90f }, 90f, out float band);
            Require(Mathf.Abs(scale[0] - 2f) < 0.05f, "a half-range hand still scales by 2 (" + scale[0].ToString("0.00") + ")");
            Require(band <= 1.35f, "the band widening is still capped (" + band.ToString("0.00") + ")");

            var matrix = Path.GetFullPath(Application.dataPath + "/../../phases/p6-accessibility/toggle-matrix.md");
            Require(File.Exists(matrix), "the toggle matrix exists");
            if (File.Exists(matrix))
                Require(File.ReadAllText(matrix).Contains("NOT RUN"),
                        "the matrix still marks the device paths NOT RUN, not passed");

            Finish("P7.7");
        }

        // ------------------------------------------------------------------ runner

        [MenuItem("SIGNSPACE/P7 Check all")]
        public static void CheckAll()
        {
            _exitOnFinish = false;
            _passes = 0;
            _failures = 0;

            CheckSound();
            CheckText();
            CheckFov();
            CheckPerformance();
            CheckBoot();
            CheckCrashPosture();
            CheckA11yRegression();

            Debug.Log("[P7Checks] TOTAL " + _passes + " passed, " + _failures + " failed");
            EditorApplication.Exit(_failures == 0 ? 0 : 1);
        }

        static void Reset(string title)
        {
            if (_exitOnFinish) { _passes = 0; _failures = 0; }
            _startPasses = _passes;
            _startFailures = _failures;
            Debug.Log("[P7Checks] " + title);
        }

        static void Finish(string title)
        {
            int p = _passes - _startPasses;
            int f = _failures - _startFailures;
            Debug.Log("[P7Checks] " + title + ": " + p + " passed, " + f + " failed");
            if (_exitOnFinish) EditorApplication.Exit(f == 0 ? 0 : 1);
        }
    }
}
