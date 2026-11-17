using System;
using Signspace.Core;
using Signspace.Gaze;
using Signspace.Hands;
using Signspace.Matching;
using Signspace.Spatial;
using Signspace.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Signspace.Editor
{
    /// <summary>
    /// P7's scene wiring, in one place and in one command.
    ///
    /// Everything here is a reference or a clip assignment: sound, the performance switches, the boot
    /// probe, the hostile-condition table and the one spatial onboarding cue. Nothing here is a new
    /// mechanic — P7 is the phase where scope moves down, not up — and every object it adds is
    /// optional at runtime (`null` dependencies degrade to "this feature is off", never to an exception).
    ///
    /// USAGE
    ///   Unity.exe -batchmode -quit -projectPath unity -executeMethod Signspace.Editor.PolishBuilder.BuildPolish
    /// </summary>
    public static class PolishBuilder
    {
        const string Sfx = "Assets/_Project/Audio/SFX/";

        [MenuItem("SIGNSPACE/P7 Build polish")]
        public static void BuildPolish()
        {
            // The WAVs are new on disk in a cold run; without this the clips load as null and the build
            // would silently ship no sound at all.
            AssetDatabase.Refresh();

            var matcher = UnityEngine.Object.FindFirstObjectByType<Matcher>();
            var shelf = UnityEngine.Object.FindFirstObjectByType<ShelfAnchor>();
            var reference = UnityEngine.Object.FindFirstObjectByType<ReferenceHand>();
            var calibration = UnityEngine.Object.FindFirstObjectByType<Calibration>();
            var settings = UnityEngine.Object.FindFirstObjectByType<A11ySettings>();
            var gaze = UnityEngine.Object.FindFirstObjectByType<GazeHighlighter>();
            var hud = UnityEngine.Object.FindFirstObjectByType<A11yHud>();
            var reflow = UnityEngine.Object.FindFirstObjectByType<ShelfReflow>();

            if (matcher == null)
            {
                Debug.LogError("[PolishBuilder] no matcher in the scene; build the main scene first.");
                return;
            }

            var soundGo = FindOrCreate("Polish_Sound");
            var sound = FindOrAdd<SoundDirector>(soundGo);
            sound.matcher = matcher;
            sound.shelf = shelf;
            sound.referenceHand = reference;
            sound.calibration = calibration;
            sound.settings = settings;
            sound.rotateClip = Clip("rotate_texture");
            sound.cues = new[]
            {
                Slot(SoundDirector.Cue.HandsFound, "boot_hands", false, 0.80f),
                Slot(SoundDirector.Cue.ReferenceHandAppears, "hand_appear", true, 0.80f),
                Slot(SoundDirector.Cue.Grab, "grab_click", true, 0.80f),
                Slot(SoundDirector.Cue.Release, "release_click", true, 0.70f),
                Slot(SoundDirector.Cue.LockChime, "lock_chime", true, 1.00f),
                Slot(SoundDirector.Cue.ObjectSettles, "object_settle", true, 0.80f),
                Slot(SoundDirector.Cue.NearMiss, "near_miss", false, 0.90f),
                Slot(SoundDirector.Cue.SurfaceFound, "surface_found", true, 0.80f),
                Slot(SoundDirector.Cue.ShelfFull, "shelf_full", true, 0.90f),
                Slot(SoundDirector.Cue.ToggleFlip, "toggle_click", false, 0.60f),
                Slot(SoundDirector.Cue.CalibrationStart, "calibrate_start", false, 0.70f),
                Slot(SoundDirector.Cue.CalibrationDone, "calibrate_done", false, 0.85f),
            };
            int mapped = 0;
            foreach (var cue in sound.cues) if (cue.clip != null) mapped++;
            Debug.Log("[PolishBuilder] sounds mapped: " + mapped + "/" + sound.cues.Length +
                      (mapped == sound.cues.Length ? "" : " — MISSING CLIPS, see Audio/SFX"));

            var perfGo = FindOrCreate("Polish_Performance");
            var perf = FindOrAdd<PerfSettings>(perfGo);
            perf.targetFrameRate = 72;
            perf.disableShadows = true;
            perf.disableRealtimeReflectionProbes = true;
            perf.foveation = PerfSettings.Foveation.Medium;

            var bootGo = FindOrCreate("Polish_Boot");
            var boot = FindOrAdd<BootProbe>(bootGo);
            boot.matcher = matcher;
            boot.shelf = shelf;
            boot.budgetSeconds = 15f;

            var guardGo = FindOrCreate("Polish_HostileGuard");
            var guard = FindOrAdd<HostileGuard>(guardGo);
            guard.eye = Camera.main;
            guard.matcher = matcher;
            guard.shelf = shelf;
            guard.gaze = gaze;
            guard.hud = hud;

            var cueGo = FindOrCreate("Polish_OnboardingCue");
            var onboarding = FindOrAdd<OnboardingCue>(cueGo);
            onboarding.referenceHand = reference;

            // ---- the near-miss line (P2.6 copy, never placed in the scene until P7) ----------------
            // The signature behaviour had a component, a copy template and eight authored hints, and no
            // object in the scene: it could not appear. It is built here as an empty label, so it adds
            // no words to the first five minutes until a near-miss actually fires.
            var nearMiss = BuildNearMiss(matcher, shelf, reference);
            if (settings != null)
            {
                settings.nearMiss = nearMiss;
                settings.hud = UnityEngine.Object.FindFirstObjectByType<A11yHud>();
                settings.tray = UnityEngine.Object.FindFirstObjectByType<A11yToggleTray>();
            }

            // ---- P7.4: every renderer stops casting shadows -------------------------------------
            // QualitySettings turns the shadow pass off at runtime, but a renderer left on
            // ShadowCastingMode.On is a per-frame culling cost for a pass that does not run, and the
            // audit reads the serialised value. There is no light in the scene, so nothing is lost.
            int casters = 0;
            foreach (var meshRenderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (meshRenderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off) continue;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                casters++;
            }
            Debug.Log("[PolishBuilder] shadow casting switched off on " + casters + " renderers");

            SetPipelineMsaa(2);

            // The tray has to know the field of view to pose itself inside the safe cone at 70°.
            var tray = UnityEngine.Object.FindFirstObjectByType<A11yToggleTray>();
            if (tray != null) tray.reflow = reflow;

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[PolishBuilder] sound " + mapped + "/" + sound.cues.Length + ", perf 72 fps + foveation " +
                      perf.foveation + ", boot probe " + boot.budgetSeconds + " s budget, " +
                      HostileGuard.Table.Length + " hostile conditions documented, onboarding cue on " +
                      (reference != null ? reference.name : "NOTHING"));
        }

        static NearMissDisplay BuildNearMiss(Matcher matcher, ShelfAnchor shelf, ReferenceHand reference)
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<NearMissDisplay>();
            if (existing != null)
            {
                existing.matcher = matcher;
                return existing;
            }

            var go = new GameObject("NearMiss");
            var canvasGo = new GameObject("NearMissCanvas", typeof(RectTransform), typeof(Canvas),
                                          typeof(CanvasGroup));
            canvasGo.transform.SetParent(go.transform, false);
            canvasGo.transform.localScale = Vector3.one * 0.0006f;

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = canvasGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(620f, 120f);

            var textGo = new GameObject("Line", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(canvasGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(620f, 120f);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 46;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = "";   // silence until a near-miss actually fires

            var group = canvasGo.GetComponent<CanvasGroup>();
            group.alpha = 0f;

            var display = go.AddComponent<NearMissDisplay>();
            display.matcher = matcher;
            display.label = text;
            display.group = group;

            // Above the shelf and facing the user, so the line appears where the object would have
            // landed rather than in the middle of the room.
            go.transform.position = shelf != null
                ? shelf.transform.position + Vector3.up * 0.32f
                : (reference != null ? reference.transform.position + Vector3.up * 0.25f
                                     : new Vector3(0f, 1.5f, 0.5f));
            go.transform.rotation = Quaternion.identity;

            return display;
        }

        static void SetPipelineMsaa(int samples)
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(
                "Assets/_Project/Rendering/SignSpacePipeline.asset");
            if (pipeline == null)
            {
                Debug.LogWarning("[PolishBuilder] URP asset missing; MSAA unchanged");
                return;
            }
            pipeline.msaaSampleCount = samples;
            EditorUtility.SetDirty(pipeline);
            Debug.Log("[PolishBuilder] MSAA " + samples + "x");
        }

        static SoundDirector.Slot Slot(SoundDirector.Cue cue, string file, bool spatial, float volume)
        {
            return new SoundDirector.Slot
            {
                cue = cue,
                clip = Clip(file),
                spatial = spatial,
                volume = volume,
            };
        }

        static AudioClip Clip(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Sfx + name + ".wav");
            if (clip == null) Debug.LogWarning("[PolishBuilder] missing clip " + Sfx + name + ".wav");
            return clip;
        }

        static GameObject FindOrCreate(string name)
        {
            var go = GameObject.Find(name);
            if (go == null) go = new GameObject(name);
            return go;
        }

        static T FindOrAdd<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }
    }
}
