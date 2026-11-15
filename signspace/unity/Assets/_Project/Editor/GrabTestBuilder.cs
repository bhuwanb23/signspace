using Signspace.Matching;
using UnityEditor;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// Builds the P1.2 rig: one floating cube, one grabber per hand, a trial counter, and a guard that
    /// proves no ray or controller path exists.
    ///
    /// WHY A SINGLE CUBE AND NOT A PLAYGROUND
    /// -------------------------------------
    /// P1.2 asks one question - can a pinch pick up a floating object, twenty times, each hand. A
    /// scene with six objects answers a different question (which object did I mean?) and would let a
    /// marginal grab pass by accident of geometry. One cube at a measured distance is the controlled
    /// version.
    ///
    /// THE DISTANCE IS MEASURED, NOT GUESSED
    /// ------------------------------------
    /// The cube is placed along the camera's forward axis at `distance` (default 0.45 m, the same
    /// arm's-length figure P1.3 will test rotation at), so the number in grab-log.md means something.
    /// If Camera.main is absent - which is what happens in a -batchmode build - the cube goes to a
    /// fixed seated-eye position instead of silently landing at the world origin.
    /// </summary>
    public static class GrabTestBuilder
    {
        public const string CubeMaterialPath = "Assets/_Project/Materials/GrabCube.mat";
        static readonly Color CubeColor = new Color(0.98f, 0.84f, 0.52f, 1f);

        [MenuItem("SIGNSPACE/P1.2 Build grab test")]
        public static void BuildGrabTestMenu()
        {
            BuildGrabTest();
        }

        /// <summary>
        /// Adds the test cube, grabbers, logger and guard to the open scene. Safe to run twice: the
        /// previous rig is removed first.
        /// </summary>
        public static void BuildGrabTest()
        {
            var old = GameObject.Find("GrabTest");
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject("GrabTest");

            // ---- the cube ---------------------------------------------------------------
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "GrabCube";
            cube.transform.SetParent(root.transform, false);
            cube.transform.localScale = Vector3.one * 0.1f;      // 10 cm: a hand-sized object
            cube.transform.position = PlaceInFront(0.45f);

            var material = HandRigBuilder.EnsureMaterial(CubeMaterialPath, CubeColor);
            var renderer = cube.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;

            var grabbable = cube.AddComponent<Grabbable>();
            grabbable.radius = 0.087f;          // circumscribed radius of a 0.1 m cube
            grabbable.grabDistance = 0.12f;     // generous: P1.2 tests grab, not the threshold

            // ---- the logger and the guard ----------------------------------------------
            var logger = root.AddComponent<GrabTrialLogger>();
            var guard = root.AddComponent<NoFallbackGuard>();
            guard.Scan();

            // ---- one grabber per hand ---------------------------------------------------
            int wired = 0;
            foreach (var source in Object.FindObjectsByType<MetaHandSource>(FindObjectsSortMode.None))
            {
                var grabber = source.GetComponent<PinchGrabber>();
                if (grabber == null) grabber = source.gameObject.AddComponent<PinchGrabber>();
                grabber.source = source;
                grabber.logger = logger;
                wired++;
            }

            if (wired == 0)
            {
                Debug.LogError("[GrabTestBuilder] no MetaHandSource in the scene, so no grabber could be " +
                               "created. Run SIGNSPACE/P1.1 Build hand rig first.");
            }
            else
            {
                Debug.Log("[GrabTestBuilder] grab test built: cube at " +
                          cube.transform.position.ToString("0.00") + " m, " + wired + " grabbers, " +
                          "cube GrabDistance " + grabbable.grabDistance + " m + radius " + grabbable.radius + " m.");
            }
        }

        static Vector3 PlaceInFront(float distance)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                // Slightly below eye height, where a hand naturally pinches.
                return cam.transform.position + cam.transform.forward * distance - Vector3.up * 0.10f;
            }

            // -batchmode has no main camera. A seated eye is ~1.2 m; the cube goes 45 cm in front of it.
            return new Vector3(0f, 1.1f, distance);
        }
    }
}
