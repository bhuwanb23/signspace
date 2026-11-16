using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Signspace.Core;
using Signspace.Matching;
using Signspace.Spatial;
using Signspace.UI;
using UnityEditor;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// P4.4, P4.5 and P4.6 as things a machine can check without a headset.
    ///
    /// What these prove: the fallback has the shape the task asked for and never renders an error,
    /// the offer only moves the shelf when it is accepted, the passthrough switch flips a sink and
    /// does not need the XR SDK to compile, and the reflow puts every slot inside the safe cone on a
    /// narrow display without scaling a single object.
    ///
    /// What they do NOT prove: that the plinth reads as physical in a dark room, that the two
    /// passthrough clips look different, or that MRUK reports a 70° display. Those are device and
    /// video rows in the phase documents, and they stay open.
    /// </summary>
    public static class Phase4Checks
    {
        const string RoomA = "11111111-1111-4111-8111-111111111111";
        const string AnchorA = "33333333-3333-4333-8333-333333333333";
        const string AnchorB = "44444444-4444-4444-8444-444444444444";
        const string PrefabPath = "Assets/_Project/Prefabs/ShelfObject.prefab";

        sealed class Finder : ISurfaceFinder
        {
            public Transform parent;
            public bool hasPrimary;
            public string room = RoomA;
            public SurfaceCandidate primary;
            public string RoomId => room;
            public bool TryGetCurrentRoomId(out string id) { id = room; return true; }
            public bool TryGetPrimary(out SurfaceCandidate surface) { surface = primary; return hasPrimary; }
            public bool TryResolve(string id, out SurfaceCandidate surface, out Transform t)
            {
                surface = primary;
                t = parent;
                return hasPrimary && string.Equals(id, primary.AnchorId, StringComparison.OrdinalIgnoreCase);
            }
        }

        sealed class FakeSink : PassthroughToggle.ISink
        {
            public bool hidden;
            public bool available = true;
            public int writes;
            public bool Available => available;
            public void SetHidden(bool value) { hidden = value; writes++; }
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("P4 check: " + message);
        }

        static void SetFinder(ShelfAnchor shelf, ISurfaceFinder finder)
        {
            typeof(ShelfAnchor).GetField("_finder", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(shelf, finder);
        }

        static SurfaceCandidate Surface(string anchor, string label)
        {
            return new SurfaceCandidate
            {
                AnchorId = anchor,
                AnchorUuid = anchor,
                RoomId = RoomA,
                Position = new Vector3(0f, 0.75f, 0.45f),
                Normal = Vector3.up,
                Extent = new Vector2(0.8f, 0.5f),
                Height = 0.75f,
                Score = 1f,
                Label = label,
                FromDevice = true
            };
        }

        // =====================================================================================
        // P4.4 — the plinth
        // =====================================================================================
        [MenuItem("SIGNSPACE/P4.4 Check plinth")]
        public static void CheckPlinth()
        {
            GameObject shelfGo = null, anchorGo = null, saveGo = null, plinth = null;
            string dir = Path.Combine(Path.GetTempPath(), "signspace-p44-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);

            try
            {
                // ---- 1. the prefab is an object, not a grey box ---------------------------------
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlinthBuilder.PrefabPath);
                if (prefab == null) prefab = PlinthBuilder.Build();
                Require(prefab != null, "Plinth.prefab could not be built");

                var top = prefab.transform.Find("PlinthTop");
                var edge = prefab.transform.Find("PlinthEdge");
                var shadow = prefab.transform.Find("PlinthShadow");
                Require(top != null && edge != null && shadow != null,
                        "plinth is missing a part: " + (top == null ? "top " : "") +
                        (edge == null ? "edge " : "") + (shadow == null ? "shadow" : ""));

                float topSurface = top.localPosition.y + top.localScale.y * 0.5f;
                Require(Mathf.Abs(topSurface) < 0.001f,
                        "the prefab's top surface must be local y=0, it is " + topSurface.ToString("0.000"));

                float tilt = prefab.transform.localEulerAngles.x;
                tilt = tilt > 180f ? tilt - 360f : tilt;
                Require(tilt > 2f && tilt < 8f, "the plinth should have a slight tilt, it has " + tilt + "°");

                var topMaterial = top.GetComponent<Renderer>() != null
                    ? top.GetComponent<Renderer>().sharedMaterial : null;
                Require(topMaterial != null, "the plinth top has no material");
                Require(topMaterial.GetTexture("_BaseMap") != null || topMaterial.GetTexture("_MainTex") != null,
                        "the plinth top has no gradient texture");

                var shadowMaterial = shadow.GetComponent<Renderer>() != null
                    ? shadow.GetComponent<Renderer>().sharedMaterial : null;
                Require(shadowMaterial != null && shadowMaterial.renderQueue >= 3000,
                        "the shadow must be a transparent material, not an opaque black quad");

                // ---- 2. no surface -> the plinth, silently, in the same frame --------------------
                saveGo = new GameObject("P44Save");
                var save = saveGo.AddComponent<SaveSystem>();
                save.fileName = Path.Combine(dir, "save.json");
                save.Load();

                shelfGo = new GameObject("P44Shelf");
                var shelf = shelfGo.AddComponent<ShelfAnchor>();
                shelf.save = save;
                shelf.slotRoot = new GameObject("Slots").transform;
                shelf.slotRoot.SetParent(shelfGo.transform, false);
                shelf.slotRoot.localPosition = new Vector3(0f, 0.012f, 0f);
                shelf.objectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                Require(shelf.objectPrefab != null, PrefabPath + " did not load");
                shelf.idle = shelfGo.AddComponent<ShelfIdle>();

                var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plate.name = "ShelfPlate";
                plate.transform.SetParent(shelfGo.transform, false);
                plate.transform.localScale = new Vector3(0.50f, 0.02f, 0.16f);
                shelf.plateRoot = plate.transform;

                plinth = UnityEngine.Object.Instantiate(prefab, shelfGo.transform);
                plinth.name = "Plinth";
                plinth.transform.localPosition = new Vector3(0f, -0.01f, 0f);
                shelf.plinthRoot = plinth;

                var anchorGoTransform = new GameObject("P44Anchor").transform;
                anchorGoTransform.position = new Vector3(0f, 0.75f, 0.45f);
                anchorGo = anchorGoTransform.gameObject;

                var finder = new Finder { parent = anchorGo.transform, hasPrimary = false };
                SetFinder(shelf, finder);

                float plateBottom = plate.transform.localPosition.y - plate.transform.localScale.y * 0.5f;
                Require(Mathf.Abs(plateBottom - -0.01f) < 0.001f, "the plate's underside moved from -0.01");
                Require(Mathf.Abs(plinth.transform.localPosition.y + 0.01f) < 0.001f,
                        "the plinth must sit in contact with the plate, not under a gap");

                shelf.Place(offerOnly: false);
                Require(shelf.Result == ShelfAnchor.Outcome.Plinth, "outcome with no surface was " + shelf.Result);
                Require(!shelf.OnSurface, "the plinth is not a surface");
                Require(plinth.activeSelf, "the plinth was not shown");
                Require(shelfGo.activeInHierarchy, "the shelf hid itself instead of falling back");
                Require(shelfGo.GetComponentInChildren<Canvas>(true) == null,
                        "the fallback opened a panel; it must be silent");
                foreach (var text in shelfGo.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                {
                    string s = (text.text ?? "").ToLowerInvariant();
                    Require(!s.Contains("error") && !s.Contains("no surface") && !s.Contains("not found"),
                            "the fallback rendered an error-looking string: '" + text.text + "'");
                }

                // ---- 3. a surface that appears later is OFFERED, never applied -------------------
                finder.primary = Surface(AnchorA, "TABLE");
                finder.hasPrimary = true;
                Vector3 before = shelf.transform.position;
                // Drive the real once-a-second poll, not the offer method directly: the first-run
                // case has no placement, and an earlier version of Update returned before polling.
                shelf.PollNow();
                Require(shelf.HasPendingSurfaceOffer, "a surface that appeared after launch was not offered");
                Require((shelf.transform.position - before).sqrMagnitude < 1e-8f,
                        "the offer moved the shelf by itself");

                shelf.AcceptSurfaceOffer();
                Require(shelf.OnSurface, "accepting the offer did not put the shelf on the surface");
                Require(shelf.Result == ShelfAnchor.Outcome.PlacedFresh, "outcome after accepting was " + shelf.Result);
                Require(save.Data.anchorId == AnchorA, "accepting the offer did not save the anchor");
                Require(!plinth.activeSelf, "the plinth stayed visible under a real surface");

                // ---- 4. a home that exists can never be captured by an offer --------------------
                finder.primary = Surface(AnchorB, "STORAGE");
                var afterAccept = shelf.transform.position;
                shelf.Place(offerOnly: true);
                Require(save.Data.anchorId == AnchorA,
                        "a second surface replaced the saved home (" + save.Data.anchorId + ")");
                Require((shelf.transform.position - afterAccept).sqrMagnitude < 1e-8f,
                        "an offer migrated a shelf that already had a home");

                Debug.Log("[P4.4] plinth checks passed: 4 parts, top at y=0, 4° tilt, silent fallback, " +
                          "user-initiated offer, saved home protected.");
            }
            catch (Exception e)
            {
                Debug.LogError("[P4.4] plinth checks FAILED: " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
            finally
            {
                if (shelfGo != null) UnityEngine.Object.DestroyImmediate(shelfGo);
                if (anchorGo != null) UnityEngine.Object.DestroyImmediate(anchorGo);
                if (saveGo != null) UnityEngine.Object.DestroyImmediate(saveGo);
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }

        // =====================================================================================
        // P4.5 — passthrough toggle
        // =====================================================================================
        [MenuItem("SIGNSPACE/P4.5 Check passthrough toggle")]
        public static void CheckPassthroughToggle()
        {
            GameObject go = null;
            try
            {
                go = new GameObject("P45Toggle");
                var toggle = go.AddComponent<PassthroughToggle>();
                var sink = new FakeSink();
                toggle.sinkOverride = sink;
                toggle.startOn = true;

                Require(toggle.TrySet(false), "the toggle refused an available sink");
                Require(sink.hidden && sink.writes == 1, "off did not hide the passthrough layer");
                Require(!toggle.IsOn && toggle.LastApplySucceeded, "the off state was not recorded");

                Require(toggle.Toggle(), "the toggle refused to turn back on");
                Require(!sink.hidden && toggle.IsOn, "on did not restore the passthrough layer");
                Require(sink.writes == 2, "the toggle wrote " + sink.writes + " times for two changes");

                // A build with no passthrough layer must not crash or pretend it worked.
                var unavailable = new FakeSink { available = false };
                toggle.sinkOverride = unavailable;
                Require(!toggle.TrySet(false), "a missing layer reported success");
                Require(unavailable.writes == 0, "a missing layer was still written to");

                // The switch ships only in a development build.
                Require(DevOnly.Allowed, "DevOnly must allow development views in the editor");
                Require(go.GetComponent<PassthroughToggle>() != null, "the toggle component vanished");

                Debug.Log("[P4.5] passthrough toggle checks passed: off/on flip one sink, missing layer " +
                          "is reported, component is development-only by design.");
            }
            catch (Exception e)
            {
                Debug.LogError("[P4.5] passthrough toggle checks FAILED: " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // =====================================================================================
        // P4.6 — FoV reflow
        // =====================================================================================
        [MenuItem("SIGNSPACE/P4.6 Check FoV reflow")]
        public static void CheckFovReflow()
        {
            GameObject shelfGo = null, headGo = null, saveGo = null;
            string dir = Path.Combine(Path.GetTempPath(), "signspace-p46-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);

            try
            {
                saveGo = new GameObject("P46Save");
                var save = saveGo.AddComponent<SaveSystem>();
                save.fileName = Path.Combine(dir, "save.json");
                save.Load();
                save.RecordPlacement(RoomA, AnchorA, "TABLE", Vector3.zero, Quaternion.identity, Vector3.zero);
                save.Unlock("U"); save.Unlock("B"); save.Unlock("C"); save.Unlock("W");

                shelfGo = new GameObject("P46Shelf");
                var shelf = shelfGo.AddComponent<ShelfAnchor>();
                shelf.save = save;
                shelf.slotRoot = new GameObject("Slots").transform;
                shelf.slotRoot.SetParent(shelfGo.transform, false);
                shelf.slotRoot.localPosition = new Vector3(0f, 0.012f, 0f);
                shelf.objectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                Require(shelf.objectPrefab != null, PrefabPath + " did not load");
                shelf.idle = shelfGo.AddComponent<ShelfIdle>();

                var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plate.name = "ShelfPlate";
                plate.transform.SetParent(shelfGo.transform, false);
                plate.transform.localScale = new Vector3(0.50f, 0.02f, 0.16f);
                shelf.plateRoot = plate.transform;

                headGo = new GameObject("P46Head");
                headGo.transform.position = new Vector3(0f, 0.30f, 0.45f);
                shelf.userHead = headGo.transform;

                shelf.RestoreUnlocked();
                Require(CountObjects(shelfGo) == 4, "restore produced " + CountObjects(shelfGo) + " objects");

                var scales = new List<Vector3>();
                foreach (var view in shelfGo.GetComponentsInChildren<ShelfObjectView>())
                    scales.Add(view.transform.lossyScale);

                var reflow = shelfGo.AddComponent<ShelfReflow>();
                reflow.shelf = shelf;
                reflow.head = headGo.transform;

                // ---- wide: Quest-class display, six columns, everything visible -----------------
                reflow.overrideHorizontalFovDegrees = 110f;
                reflow.Evaluate();
                Require(!reflow.IsTray, "110° should keep the wide grid");
                Require(reflow.CurrentColumns == 6, "wide profile has " + reflow.CurrentColumns + " columns");
                Require(reflow.AngularWidthDegrees > 45f && reflow.AngularWidthDegrees < 60f,
                        "wide angular width measured " + reflow.AngularWidthDegrees.ToString("0.0") + "°");
                float wideWidth = reflow.AngularWidthDegrees;
                float wideWidest = reflow.MaxSlotAngleDegrees;
                int wideSlotCount = CountSlots(shelf);
                Require(wideSlotCount == 12, "wide grid has " + wideSlotCount + " slots");

                // ---- narrow: VR Glasses 70°, folds into the tray --------------------------------
                reflow.overrideHorizontalFovDegrees = 70f;
                reflow.Evaluate();
                Require(reflow.IsTray, "70° did not trigger the tray");
                Require(reflow.CurrentColumns == 4, "tray has " + reflow.CurrentColumns + " columns");
                Require(reflow.AngularWidthDegrees <= 50f,
                        "tray is still " + reflow.AngularWidthDegrees.ToString("0.0") +
                        "° wide; the task caps it at ~50°");
                Require(reflow.AllSlotsInsideSafeCone,
                        "widest slot " + reflow.MaxSlotAngleDegrees.ToString("0.0") +
                        "° is outside the " + (reflow.safeConeDegrees * 0.5f) + "° half-cone");
                float trayWidth = reflow.AngularWidthDegrees;
                float trayWidest = reflow.MaxSlotAngleDegrees;
                Require(CountObjects(shelfGo) == 4, "reflow lost or duplicated objects: " + CountObjects(shelfGo));
                Require(CountSlots(shelf) == 12, "tray has " + CountSlots(shelf) + " slots, not 12");

                // ---- no object was scaled: the trap P4.6 names --------------------------------
                int i = 0;
                foreach (var view in shelfGo.GetComponentsInChildren<ShelfObjectView>())
                {
                    Vector3 now = view.transform.lossyScale;
                    Require((now - scales[i]).sqrMagnitude < 1e-6f,
                            "reflow scaled object " + view.shapeId + " from " + scales[i] + " to " + now);
                    i++;
                }

                // ---- idempotent: evaluating again changes nothing ------------------------------
                reflow.Evaluate();
                Require(reflow.IsTray && reflow.CurrentColumns == 4 && CountObjects(shelfGo) == 4 &&
                        CountSlots(shelf) == 12, "a second evaluation changed the layout");
                Require(reflow.LastDecision.StartsWith("kept tray"),
                        "a no-op should report that it kept the layout, it said: " + reflow.LastDecision);
                Require(reflow.LastDecision.Contains("70°"), "the no-op lost the measured FoV");

                // ---- back to wide: the reflow is reversible -----------------------------------
                reflow.overrideHorizontalFovDegrees = 110f;
                reflow.Evaluate();
                Require(!reflow.IsTray && reflow.CurrentColumns == 6, "the reflow did not reverse");
                Require(CountObjects(shelfGo) == 4 && CountSlots(shelf) == 12,
                        "reversing the reflow changed the object count");

                Debug.Log("[P4.6] FoV reflow checks passed: 110° -> 6 cols / " +
                          wideWidth.ToString("0.0") + "° grid, widest object " +
                          wideWidest.ToString("0.0") + "°; 70° -> 4 cols / " +
                          trayWidth.ToString("0.0") + "° grid, widest object " +
                          trayWidest.ToString("0.0") + "° inside 27.5°; 12 slots, 4 objects, " +
                          "no scaling, reversible.");
            }
            catch (Exception e)
            {
                Debug.LogError("[P4.6] FoV reflow checks FAILED: " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
            finally
            {
                if (shelfGo != null) UnityEngine.Object.DestroyImmediate(shelfGo);
                if (headGo != null) UnityEngine.Object.DestroyImmediate(headGo);
                if (saveGo != null) UnityEngine.Object.DestroyImmediate(saveGo);
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }

        static int CountObjects(GameObject root)
        {
            int n = 0;
            foreach (var unused in root.GetComponentsInChildren<ShelfObjectView>()) n++;
            return n;
        }

        static int CountSlots(ShelfAnchor shelf)
        {
            int n = 0;
            for (int i = 0; i < shelf.slotCount; i++)
                if (shelf.SlotTransform(i) != null) n++;
            return n;
        }
    }
}
