using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// Builds the Main scene from code.
    ///
    /// WHY
    /// ---
    /// SETUP.md describes the scene as eight manual steps in the editor, each
    /// of which can be done slightly differently by a different person and
    /// none of which leaves a trace in the repository. A scene built by a
    /// script is reproducible: the scene is a build artefact, not a document.
    ///
    /// It also means the headset build is one command, on a machine that has
    /// never opened the editor GUI.
    ///
    /// USAGE
    ///   Unity.exe -batchmode -quit -projectPath unity \
    ///             -executeMethod Signspace.Editor.SceneBuilder.BuildMainScene
    ///
    /// The Meta XR rig is located by searching for the prefab by name, because
    /// its path inside the package changed between SDK versions. If it cannot
    /// be found the script says so plainly and leaves a plain camera behind,
    /// so the rest of the pipeline still runs.
    /// </summary>
    public static class SceneBuilder
    {
        const string ScenesFolder = "Assets/_Project/Scenes";
        const string ScenePath = ScenesFolder + "/Main.unity";

        [MenuItem("SIGNSPACE/Build Main Scene")]
        public static void BuildMainScene()
        {
            Directory.CreateDirectory(ScenesFolder);

            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            // ---- camera rig ------------------------------------------------
            var rig = FindPrefab("OVRCameraRig");
            GameObject rigGo;
            if (rig != null)
            {
                rigGo = (GameObject)PrefabUtility.InstantiatePrefab(rig);
                rigGo.name = "OVRCameraRig";
                Debug.Log("[SceneBuilder] using the Meta XR camera rig");
            }
            else
            {
                rigGo = new GameObject("OVRCameraRig");
                var camGo = new GameObject("CenterEyeAnchor");
                camGo.transform.SetParent(rigGo.transform, false);
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.06f, 0.07f, 0.09f, 1f);
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = 100f;
                Debug.LogWarning("[SceneBuilder] the Meta XR camera rig was not found. " +
                                 "A plain camera was created instead. Check that Meta XR All-in-One is imported.");
            }

            // ---- the three systems that matter -----------------------------
            var matchGo = new GameObject("Matcher");
            var matcher = matchGo.AddComponent<Signspace.Matching.Matcher>();

            // Write the tuned values explicitly rather than trusting a field initialiser: the scene is
            // serialised, and a stale serialised value wins over any default forever. BuildScript then
            // checks the serialised scene against these same constants.
            matcher.lockedDeg = Signspace.Matching.Matcher.TunedLockedDeg;
            matcher.closeDeg = Signspace.Matching.Matcher.TunedCloseDeg;
            matcher.bandReleaseDeg = Signspace.Matching.Matcher.TunedReleaseDeg;
            matcher.orientationSlackDeg = Signspace.Matching.Matcher.TunedSlackDeg;
            matcher.holdSeconds = Signspace.Matching.Matcher.TunedHoldSeconds;
            Debug.Log("[SceneBuilder] matcher tuning <- locked " + matcher.lockedDeg.ToString("0.#") +
                      " / close " + matcher.closeDeg.ToString("0.#") +
                      " / release " + matcher.bandReleaseDeg.ToString("0.#") +
                      " / slack " + matcher.orientationSlackDeg.ToString("0.#") +
                      " / hold " + matcher.holdSeconds.ToString("0.00") + " s");

            // P2.2: the weights live in an asset. Assign it here so a scene built from code has the
            // tunable table from the first frame rather than the runtime defaults.
            var jointWeights = AssetDatabase.LoadAssetAtPath<Signspace.Matching.JointWeights>(
                "Assets/_Project/Matching/Weights.asset");
            if (jointWeights != null)
            {
                matcher.weights = jointWeights;
                string weightsProblem = jointWeights.Validate();
                if (!string.IsNullOrEmpty(weightsProblem))
                    Debug.LogError("[SceneBuilder] JointWeights unusable: " + weightsProblem);
                else
                    Debug.Log("[SceneBuilder] matcher weights <- Matching/Weights.asset (" +
                              jointWeights.Summary() + ")");
            }
            else
            {
                Debug.LogWarning("[SceneBuilder] Matching/Weights.asset not found; the matcher will use " +
                                 "its runtime defaults. Run SIGNSPACE/P2.2 Write weights asset from defaults.");
            }
            // The overlay lives on its OWN child object, not on the matcher's object: DevOnly removes
            // the object it is attached to from a release build, and attaching it to the Matcher would
            // delete the matcher from the submission build - a spectacularly quiet way to ship nothing.
            var devViewsGo = new GameObject("DevViews");
            devViewsGo.transform.SetParent(matchGo.transform, false);
            devViewsGo.AddComponent<Signspace.Core.DevOnly>();
            var debugOverlay = devViewsGo.AddComponent<Signspace.Matching.DebugOverlay>();
            debugOverlay.matcher = matcher;

            var sourceGo = new GameObject("SkeletonPoseSource");
            var source = sourceGo.AddComponent<Signspace.Matching.SkeletonPoseSource>();
            source.matcher = matcher;
            debugOverlay.poseSource = source;

            // ---- P2.3: the three bands as colour, motion and sound -------------------------
            // Feedback hangs off the matcher, not the reference hand, because the band is a fact about
            // the learner's hand and must survive the reference visual being dissolved by LockPayoff.
            var bandFeedback = matchGo.AddComponent<Signspace.UI.BandFeedback>();
            bandFeedback.matcher = matcher;

            // ---- P3.5: the visible pause ----------------------------------------------------
            // A small lamp beside the reference hand. While confidence is below the matcher's floor it
            // dims and breathes slowly; nothing is said, nothing is scored, and the hold timer is frozen
            // by the matcher itself. TrackingConfidence owns no timing that matters - delete it and the
            // pause still happens, which is exactly the property that makes it safe to ship.
            var lampGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lampGo.name = "TrackingLamp";
            lampGo.transform.position = new Vector3(0.20f, 1.42f, 0.62f);
            lampGo.transform.localScale = Vector3.one * 0.028f;
            var lampRenderer = lampGo.GetComponent<Renderer>();
            if (lampRenderer != null)
                lampRenderer.sharedMaterial = BuildMaterials.Get(
                    BuildMaterials.Folder + "/TrackingLamp.mat", new Color(0.55f, 0.68f, 0.78f, 1f));
            var tracking = lampGo.AddComponent<Signspace.UI.TrackingConfidence>();
            tracking.matcher = matcher;
            tracking.lamp = lampRenderer;

            var bandAudio = matchGo.AddComponent<Signspace.UI.BandAudio>();
            bandAudio.matcher = matcher;
            bandAudio.warmingToClose = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/_Project/Audio/SFX/band_close.wav");
            bandAudio.closeToLocked = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/_Project/Audio/SFX/band_lock_in.wav");
            bandAudio.lockedToWarming = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/_Project/Audio/SFX/band_release.wav");
            if (bandAudio.IsMapped)
                Debug.Log("[SceneBuilder] band sounds mapped (placeholders; P7 replaces the clips)");
            else
                Debug.LogWarning("[SceneBuilder] band clips missing from Audio/SFX; band changes will " +
                                 "be silent, which SOUND-MAP.md allows but is not what was built.");

            // ---- the reference hand: the thing you grab and turn ------------
            var refGo = new GameObject("ReferenceHand");
            var reference = refGo.AddComponent<Signspace.Matching.ReferenceHand>();
            var visualGo = new GameObject("HandVisual");
            visualGo.transform.SetParent(refGo.transform, false);
            var visual = visualGo.AddComponent<Signspace.Matching.HandVisual>();
            reference.visual = visual;
            reference.pivot = refGo.transform;
            reference.trackedHand = source;

            var loaderGo = new GameObject("SignSetLoader");
            var loader = loaderGo.AddComponent<Signspace.Matching.SignSetLoader>();
            loader.matcher = matcher;

            var stateGo = new GameObject("AppState");
            stateGo.AddComponent<Signspace.Matching.AppStateMachine>();

            var saveGo = new GameObject("SaveSystem");
            saveGo.AddComponent<Signspace.Matching.SaveSystem>();

            // Build the production shelf before wiring the payoff. The P1.7 debug cube and probe are
            // deliberately excluded from the main scene.
            PersistenceBuilder.Build(includeProbe: false);

            // ---- P2.5: the lock payoff ------------------------------------------------------
            // Instantiated from the committed prefab so the timing lives in one place (the prefab) and
            // the scene only wires what it alone can know: the matcher, the tracked hand, the shelf.
            // In a cold batch run the asset database may not have imported a committed prefab yet, and
            // LoadAssetAtPath then returns null - which would write a scene with no payoff and only a
            // warning to show for it. Refresh first, and regenerate the prefab if it still will not load.
            AssetDatabase.Refresh();
            var payoffPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/LockPayoff.prefab");
            if (payoffPrefab == null)
            {
                Debug.Log("[SceneBuilder] LockPayoff.prefab did not load; regenerating it.");
                LockPayoffBuilder.Build();
                AssetDatabase.Refresh();
                payoffPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/LockPayoff.prefab");
            }

            Signspace.Core.LockPayoff payoff = null;
            if (payoffPrefab != null)
            {
                var payoffGo = (GameObject)PrefabUtility.InstantiatePrefab(payoffPrefab);
                payoff = payoffGo.GetComponent<Signspace.Core.LockPayoff>();
                payoff.matcher = matcher;
                payoff.save = saveGo.GetComponent<Signspace.Matching.SaveSystem>();
                payoff.shelf = UnityEngine.Object.FindFirstObjectByType<Signspace.Spatial.ShelfAnchor>();
                payoff.referenceHand = refGo.transform;
                payoff.referenceHandVisual = visualGo;
                // The prefab's stand-in anchor is not the visible reference hand in this scene.
                payoff.flyFrom = refGo.transform;

                // the controller-free "haptic": a pulse in the hand that did the thing
                var pulse = visualGo.AddComponent<Signspace.UI.HandPulse>();
                pulse.target = visualGo.transform;
                payoff.handPulse = pulse;

                Debug.Log("[SceneBuilder] lock payoff wired, " + payoff.TotalSeconds.ToString("0.00") +
                          " s of a " + payoff.budgetSeconds.ToString("0.00") + " s budget");
            }
            else
            {
                Debug.LogWarning("[SceneBuilder] Prefabs/LockPayoff.prefab missing; run " +
                                 "SIGNSPACE/P2.5 Build lock payoff prefab. No payoff will play.");
            }


            // ---- P2.4: the lock log ---------------------------------------------------------
            // Every lock and unlock with a timestamp, so "20/20 intentional, 0/20 false" can be
            // checked after the headset comes off instead of remembered.
            var lockLogGo = new GameObject("LockLogger");
            var lockLogger = lockLogGo.AddComponent<Signspace.Core.LockLogger>();
            lockLogger.matcher = matcher;
            Debug.Log("[SceneBuilder] lock log -> " + lockLogger.LogPath);

            // ---- P1.1: the two tracked hands, and the HandVisual prefab ---------------------
            // This is the step that was missing entirely before P1.1: nothing in the scene had
            // ever bound a tracked skeleton to the matcher, so the matcher was being fed an empty
            // joint array and the reference hand's grab had no pinch source. BuildHandRig() emits
            // Assets/_Project/Prefabs/HandVisual.prefab, instantiates both hands, and assigns the
            // right hand's MetaHandSource to the reference hand.
            HandRigBuilder.BuildHandRig();

            // ---- P3.4: the honest mirror -------------------------------------------
            // A panel about what the cameras cannot see, opened by pinching a small plinth in front of
            // the user. Runs after HandRigBuilder so the plinth can find the hands in the scene; P4
            // re-homes the plinth onto the shelf.
            HonestMirrorBuilder.BuildHonestMirror();

            // ---- P5.5: the credits, on the shelf --------------------------------------
            // Credit is content, not small print: a bead on the shelf opens it, and the panel shows
            // itself once in the first minute of the first launch. Runs after the shelf exists so the
            // object can follow it, and after the honest mirror so the two beads do not collide.
            CreditsBuilder.BuildCredits();

            // ---- P6: the accessibility layer -----------------------------------------------
            // Wired last: it needs the camera, the matcher, the save system, the reference hand, the
            // shelf's idle motion and the payoff, all of which exist by this point. It adds the gaze
            // rig, the finger targets, the caption HUD and the toggle tray; every reference is assigned
            // here so the saved scene is the whole wiring.
            A11yBuilder.BuildAccessibility();

            // ---- P7: what polish needs in the scene ----------------------------------------
            // Sound, the performance switches, the boot probe, the hostile-condition table and the one
            // spatial onboarding cue. Runs last because it looks up components every earlier builder
            // created (matcher, shelf, reference hand, calibration, gaze, HUD, reflow, tray).
            PolishBuilder.BuildPolish();

            // ---- P4.5: the passthrough comparison switch ----------------------------------
            // One key, and only in a development build: the pair of clips has to be the same scene
            // with one variable changed, not two staged shots. DevOnly removes it from a release
            // build, so a judge never finds a debug key in the product.
            var toggleGo = GameObject.Find("PassthroughToggle");
            if (toggleGo == null) toggleGo = new GameObject("PassthroughToggle");
            if (toggleGo.GetComponent<Signspace.UI.PassthroughToggle>() == null)
                toggleGo.AddComponent<Signspace.UI.PassthroughToggle>();
            if (toggleGo.GetComponent<Signspace.Core.DevOnly>() == null)
                toggleGo.AddComponent<Signspace.Core.DevOnly>();

            // ---- P1.2: the grab test rig ---------------------------------------------------
            // One floating cube, a PinchGrabber per hand, the trial logger and the no-fallback guard.
            // This is a SPIKE rig, not content: P2 replaces the cube with the first real shape. It
            // lives in the scene builder so that one command still produces a headset build that can
            // answer P1.2's question.
            GrabTestBuilder.BuildGrabTest();

            // ---- P7.4: nothing in the finished scene casts a shadow -------------------------
            // Run last, after every builder that creates a renderer, because a builder that runs after
            // this one would reintroduce a caster. There is no light in the scene, so this is culling
            // cost removed, not an image change.
            PolishBuilder.DisableShadowCasting();

            // ---- scene settings ---------------------------------------------
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.35f, 0.35f, 0.40f);
            RenderSettings.ambientEquatorColor = new Color(0.22f, 0.22f, 0.24f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.10f, 0.11f);

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("[SceneBuilder] wrote " + ScenePath);
        }

        /// <summary>Finds a prefab anywhere in the project or in an imported package.</summary>
        static GameObject FindPrefab(string prefabName)
        {
            string[] guids = AssetDatabase.FindAssets(prefabName + " t:Prefab");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".prefab")) continue;
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;
                if (go.name != prefabName) continue;
                return go;
            }
            return null;
        }
    }
}
