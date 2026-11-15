using Signspace.Matching;
using UnityEditor;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// Builds the P1.5 probe rig: the twenty candidate poses loaded as the matcher's shape set, the
    /// reference hand showing each one, and the controller that times the holds and records the results.
    ///
    /// WHY THIS IS A SEPARATE RIG FROM THE GRAB TEST
    /// ---------------------------------------------
    /// The grab test wants a floating cube and nothing else competing for a pinch. The probe wants no
    /// cube at all and a shape on the reference hand. Wiring both into one scene would mean the probe's
    /// pinches (which are how the operator rates a shape) landing next to a grabbable object. Keeping
    /// them separate rigs keeps each measurement honest, and each is one menu item away.
    ///
    /// WHAT IT CHANGES ABOUT THE SCENE
    /// -------------------------------
    /// The sign set loader is pointed at StreamingAssets/Probe instead of /Signs, so the matcher holds
    /// twenty references rather than the six that ship. That is a probe-build setting and must NOT
    /// reach a submission build - ProbeController sets it at runtime as well, so a scene that is built
    /// by mistake still cannot ship the probe set as content.
    /// </summary>
    public static class ProbeBuilder
    {
        [MenuItem("SIGNSPACE/P1.5 Build probe rig")]
        public static void BuildProbeMenu()
        {
            BuildProbe();
        }

        public static void BuildProbe()
        {
            var root = GameObject.Find("ProbeRig");
            if (root == null) root = new GameObject("ProbeRig");

            // The grab cube would compete with the rating pinch. Disable, do not delete: the grab rig
            // is still wanted for P1.2 and re-running the scene builder restores it.
            var grabTest = GameObject.Find("GrabTest");
            if (grabTest != null)
            {
                grabTest.SetActive(false);
                Debug.Log("[ProbeBuilder] GrabTest disabled for the probe session");
            }

            var loader = Object.FindFirstObjectByType<SignSetLoader>();
            if (loader != null)
            {
                loader.folder = "Probe";
                loader.autoSelectFirst = false;
                Debug.Log("[ProbeBuilder] SignSetLoader folder <- Probe (20 candidates)");
            }
            else
            {
                Debug.LogError("[ProbeBuilder] no SignSetLoader in the scene. Run SIGNSPACE/Build Main Scene first.");
            }

            var matcher = Object.FindFirstObjectByType<Matcher>();
            var reference = Object.FindFirstObjectByType<ReferenceHand>();

            var controller = root.GetComponent<ProbeController>();
            if (controller == null) controller = root.AddComponent<ProbeController>();
            controller.matcher = matcher;
            controller.loader = loader;
            controller.referenceHand = reference;
            controller.trackedHand = Object.FindFirstObjectByType<MetaHandSource>();

            // The reference hand should not be grabbable during a probe: the tester needs both hands
            // free to make the shape, and a pinch near the model would drag it around.
            if (reference != null) reference.handStateProvider = null;

            VerifyPoseFolder();
        }

        static void VerifyPoseFolder()
        {
            const string folder = "Assets/StreamingAssets/Probe";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogError("[ProbeBuilder] " + folder + " does not exist. Run " +
                               "'python3 scripts/make_probe_poses.py' first - the probe has no shapes " +
                               "to compare against without it.");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:TextAsset", new[] { folder });
            Debug.Log("[ProbeBuilder] probe folder holds " + guids.Length + " files (20 poses + index + meta expected)");
        }
    }
}
