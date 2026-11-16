using System;
using System.IO;
using System.Reflection;
using Signspace.Matching;
using Signspace.Spatial;
using UnityEditor;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// P4.3's relaunch claim, tested across PROCESSES rather than in one editor session.
    ///
    /// WHY THIS EXISTS
    /// ---------------
    /// The editor check (`PersistenceP43Checks`) proves the save round-trips inside one process. That is
    /// not the claim P4.3 makes: "kill the app, relaunch, is the shelf still there?" depends on the file
    /// surviving with no in-memory help. So this probe is invoked as two methods and driven from the
    /// shell:
    ///
    ///   Unity -batchmode -quit -executeMethod Signspace.Editor.PersistenceRelaunch.Init
    ///   Unity -batchmode -quit -executeMethod Signspace.Editor.PersistenceRelaunch.Verify   # x5
    ///
    /// Each `Verify` is a fresh Unity process: new domain, new statics, nothing carried over except
    /// `save.json`. It loads the save, restores through the real `ShelfAnchor.ResolveForCurrentRoom`,
    /// and checks that the objects come back in unlock order.
    ///
    /// WHAT IT STILL DOES NOT PROVE
    /// ----------------------------
    /// It is not an Android launch, an MRUK room resolution, a physical anchor, or an OS reboot. The
    /// finder is fake and the UUIDs are fixed. Those rows stay in `persistence-matrix.md` as NOT RUN.
    /// </summary>
    public static class PersistenceRelaunch
    {
        const string RoomA = "11111111-1111-4111-8111-111111111111";
        const string AnchorA = "33333333-3333-4333-8333-333333333333";
        const string PrefabPath = "Assets/_Project/Prefabs/ShelfObject.prefab";
        static readonly Vector3 SavedLocalPos = new Vector3(0.12f, -0.03f, 0.21f);
        static readonly Quaternion SavedLocalRot = Quaternion.Euler(0f, 37f, 0f);

        static string Dir => Path.Combine(Path.GetTempPath(), "signspace-p43-relaunch");
        static string SavePath => Path.Combine(Dir, "save.json");
        // Room transitions get their own file so the five-relaunch loop is not disturbed by the
        // extra shape unlocked away from home.
        static string RoomPath => Path.Combine(Dir, "room.json");
        const string RoomB = "22222222-2222-4222-8222-222222222222";

        sealed class FakeFinder : ISurfaceFinder
        {
            public Transform parent;
            public string RoomId => RoomA;
            public bool TryGetCurrentRoomId(out string id) { id = RoomA; return true; }
            public bool TryGetPrimary(out SurfaceCandidate surface) { surface = default; return false; }
            public bool TryResolve(string id, out SurfaceCandidate surface, out Transform t)
            {
                surface = new SurfaceCandidate { AnchorId = AnchorA, RoomId = RoomA, Label = "TABLE" };
                t = parent;
                return id == AnchorA;
            }
        }

        /// <summary>A finder whose only variable is which room the headset reports.</summary>
        sealed class RoomFinder : ISurfaceFinder
        {
            public Transform parent;
            public string room = RoomA;
            public bool roomKnown = true;
            public string RoomId => roomKnown ? room : "";
            public bool TryGetCurrentRoomId(out string id) { id = RoomId; return roomKnown; }
            public bool TryGetPrimary(out SurfaceCandidate surface) { surface = default; return false; }
            public bool TryResolve(string id, out SurfaceCandidate surface, out Transform t)
            {
                surface = new SurfaceCandidate { AnchorId = AnchorA, RoomId = room, Label = "TABLE" };
                t = parent;
                return roomKnown && id == AnchorA;
            }
        }

        [MenuItem("SIGNSPACE/P4.3 Relaunch probe - init")]
        public static void Init()
        {
            try
            {
                if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
                Directory.CreateDirectory(Dir);

                var go = new GameObject("P43RelaunchInit");
                try
                {
                    var save = go.AddComponent<SaveSystem>();
                    save.fileName = SavePath;
                    save.Load();
                    save.RecordPlacement(RoomA, AnchorA, "TABLE", SavedLocalPos, SavedLocalRot, Vector3.zero);
                    save.Unlock("U");
                    save.Unlock("B");
                    save.Unlock("C");

                    Require(save.HasPlacement, "placement was not recorded");
                    Require(save.Data.unlocked.Count == 3, "expected three unlocks");
                    Debug.Log("[P43Relaunch] init ok: schema " + save.Data.schema + ", unlock order " +
                              string.Join(",", save.Data.unlocked) + ", file " + SavePath);
                }
                finally { UnityEngine.Object.DestroyImmediate(go); }
            }
            catch (Exception e)
            {
                Debug.LogError("[P43Relaunch] init FAILED: " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        [MenuItem("SIGNSPACE/P4.3 Relaunch probe - verify")]
        public static void Verify()
        {
            GameObject saveGo = null, shelfGo = null, anchorGo = null;
            try
            {
                Require(File.Exists(SavePath), "no save file - run Init first");

                saveGo = new GameObject("P43RelaunchSave");
                var save = saveGo.AddComponent<SaveSystem>();
                save.fileName = SavePath;
                save.Load();

                // ---- the file survived the process boundary intact -------------------------------
                Require(save.Data.schema == SaveSystem.CurrentSchema, "schema is " + save.Data.schema);
                Require(save.HasPlacement, "placement is not trusted after reload");
                Require(save.Data.roomId == RoomA && save.Data.anchorId == AnchorA, "home ids changed");
                Require(save.Data.layoutVariant == SaveSystem.DefaultLayout, "layout variant changed");
                Require(string.Join(",", save.Data.unlocked) == "U,B,C", "unlock order changed: " +
                        string.Join(",", save.Data.unlocked));
                Require(save.Data.unlockRecords.Count == 3, "missing unlock timestamps");
                foreach (var record in save.Data.unlockRecords)
                    Require(!string.IsNullOrEmpty(record.unlockedUtc), "empty timestamp for " + record.shapeId);
                int restoresBefore = save.Data.restoreCount;

                // ---- the real restore path, with a fake room but real code ----------------------
                anchorGo = new GameObject("P43RelaunchAnchor");
                anchorGo.transform.position = new Vector3(1f, 0f, 2f);
                anchorGo.transform.rotation = Quaternion.Euler(0f, 15f, 0f);

                shelfGo = new GameObject("P43RelaunchShelf");
                var shelf = shelfGo.AddComponent<ShelfAnchor>();
                shelf.save = save;
                shelf.slotRoot = new GameObject("Slots").transform;
                shelf.slotRoot.SetParent(shelfGo.transform, false);
                shelf.objectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                Require(shelf.objectPrefab != null, PrefabPath + " did not load");
                shelf.idle = shelfGo.AddComponent<ShelfIdle>();

                var finder = new FakeFinder { parent = anchorGo.transform };
                typeof(ShelfAnchor).GetField("_finder", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(shelf, finder);
                typeof(ShelfAnchor).GetMethod("ResolveForCurrentRoom", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(shelf, null);

                Require(shelf.Result == ShelfAnchor.Outcome.Restored, "outcome was " + shelf.Result);
                Require(Vector3.Distance(shelf.transform.localPosition, SavedLocalPos) < 0.0001f,
                        "local position is " + shelf.transform.localPosition + ", expected " + SavedLocalPos);
                Require(Quaternion.Angle(shelf.transform.localRotation, SavedLocalRot) < 0.01f,
                        "local rotation is " + shelf.transform.localRotation.eulerAngles);

                // ---- objects, in the order they were learned ----------------------------------
                for (int i = 0; i < 3; i++)
                {
                    var slot = shelf.SlotTransform(i);
                    var view = slot != null ? slot.GetComponentInChildren<ShelfObjectView>() : null;
                    Require(view != null, "slot " + i + " has no object after restore");
                    Require(view.shapeId == save.Data.unlocked[i],
                            "slot " + i + " holds " + view.shapeId + ", expected " + save.Data.unlocked[i]);
                }

                // A second restore must not duplicate objects - that is what a relaunch after a
                // mid-session room change does.
                shelf.RestoreUnlocked();
                int objects = 0;
                foreach (var view in shelfGo.GetComponentsInChildren<ShelfObjectView>()) objects++;
                Require(objects == 3, "RestoreUnlocked duplicated objects: " + objects);

                Require(save.Data.restoreCount == restoresBefore + 1,
                        "restoreCount did not advance: " + save.Data.restoreCount + " from " + restoresBefore);

                Debug.Log("[P43Relaunch] verify ok: restore " + save.Data.restoreCount +
                          ", order " + string.Join(",", save.Data.unlocked) +
                          ", objects 3, localPos " + shelf.transform.localPosition.ToString("F3"));
            }
            catch (Exception e)
            {
                Debug.LogError("[P43Relaunch] verify FAILED: " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
            finally
            {
                if (shelfGo != null) UnityEngine.Object.DestroyImmediate(shelfGo);
                if (anchorGo != null) UnityEngine.Object.DestroyImmediate(anchorGo);
                if (saveGo != null) UnityEngine.Object.DestroyImmediate(saveGo);
            }
        }

        /// <summary>
        /// B1/B2 in one process: launch in room B, where the saved home does not live. The shelf must
        /// stand on the temporary plinth with the same progress, and the file on disk must still point
        /// at home. Then unlock a shape there, which is the "progress is global" half of the policy.
        /// </summary>
        [MenuItem("SIGNSPACE/P4.3 Relaunch probe - leave room")]
        public static void LeaveRoom()
        {
            GameObject saveGo = null, reloadGo = null, shelfGo = null, anchorGo = null;
            try
            {
                if (!File.Exists(RoomPath)) SeedRoomSave();

                saveGo = new GameObject("P43RoomSave");
                var save = saveGo.AddComponent<SaveSystem>();
                save.fileName = RoomPath;
                save.Load();
                Require(save.Data.roomId == RoomA && save.Data.unlocked.Count == 3, "room save seed is wrong");
                int restoresBefore = save.Data.restoreCount;

                shelfGo = BuildShelf(save, RoomB, true, out var shelf, out anchorGo);
                Resolve(shelf);

                Require(shelf.Result == ShelfAnchor.Outcome.DifferentRoom, "outcome in B was " + shelf.Result);
                Require(!shelf.OnSurface, "a different room must not be treated as the home surface");
                Require(shelf.CurrentAnchorId == "", "a different room must not claim an anchor");
                Require(CountViews(shelfGo) == 3, "global progress lost in B: " + CountViews(shelfGo) + " objects");
                Require(save.Data.roomId == RoomA && save.Data.anchorId == AnchorA, "home ids changed in B");
                Require(save.Data.restoreCount == restoresBefore, "the plinth must not count as a restore");

                // B2: progress made away from home is still progress, and it is saved.
                save.Unlock("W");
                save.Save();
                Require(File.Exists(RoomPath), "room save disappeared");

                reloadGo = new GameObject("P43RoomReload");
                var reload = reloadGo.AddComponent<SaveSystem>();
                reload.fileName = RoomPath;
                reload.Load();
                Require(reload.Data.roomId == RoomA && reload.Data.anchorId == AnchorA,
                        "the file no longer points home after leaving the room");
                Require(string.Join(",", reload.Data.unlocked) == "U,B,C,W",
                        "away-from-home unlock was not persisted: " + string.Join(",", reload.Data.unlocked));
                Require(reload.Data.unlockRecords.Count == 4 &&
                        !string.IsNullOrEmpty(reload.Data.unlockRecords[3].unlockedUtc),
                        "away-from-home unlock has no timestamp");

                Debug.Log("[P43Relaunch] leave ok: DifferentRoom, plinth, 3 objects, home " +
                          RoomA.Substring(0, 8) + " kept, W unlocked and saved");
            }
            catch (Exception e)
            {
                Debug.LogError("[P43Relaunch] leave FAILED: " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
            finally
            {
                if (shelfGo != null) UnityEngine.Object.DestroyImmediate(shelfGo);
                if (anchorGo != null) UnityEngine.Object.DestroyImmediate(anchorGo);
                if (saveGo != null) UnityEngine.Object.DestroyImmediate(saveGo);
                if (reloadGo != null) UnityEngine.Object.DestroyImmediate(reloadGo);
            }
        }

        /// <summary>
        /// B4 in a fresh process: launch back in room A. The original anchor and transform must come
        /// back, with the shape unlocked in B in its own slot.
        /// </summary>
        [MenuItem("SIGNSPACE/P4.3 Relaunch probe - return room")]
        public static void ReturnRoom()
        {
            GameObject saveGo = null, shelfGo = null, anchorGo = null;
            try
            {
                Require(File.Exists(RoomPath), "no room save - run Leave room first");
                saveGo = new GameObject("P43RoomSave");
                var save = saveGo.AddComponent<SaveSystem>();
                save.fileName = RoomPath;
                save.Load();
                Require(string.Join(",", save.Data.unlocked) == "U,B,C,W",
                        "expected four shapes from the B run, found " + string.Join(",", save.Data.unlocked));

                shelfGo = BuildShelf(save, RoomA, true, out var shelf, out anchorGo);
                Resolve(shelf);

                Require(shelf.Result == ShelfAnchor.Outcome.Restored, "outcome back home was " + shelf.Result);
                Require(shelf.OnSurface && shelf.transform.parent == anchorGo.transform, "not parented to the home anchor");
                Require(Vector3.Distance(shelf.transform.localPosition, SavedLocalPos) < 0.0001f,
                        "home local position moved: " + shelf.transform.localPosition);
                Require(Quaternion.Angle(shelf.transform.localRotation, SavedLocalRot) < 0.01f,
                        "home local rotation moved");

                var order = string.Join(",", save.Data.unlocked);
                for (int i = 0; i < 4; i++)
                {
                    var slot = shelf.SlotTransform(i);
                    var view = slot != null ? slot.GetComponentInChildren<ShelfObjectView>() : null;
                    Require(view != null, "slot " + i + " is empty after returning home");
                    Require(view.shapeId == save.Data.unlocked[i],
                            "slot " + i + " holds " + view.shapeId + ", expected " + save.Data.unlocked[i]);
                }
                Require(CountViews(shelfGo) == 4, "returning home duplicated objects: " + CountViews(shelfGo));

                Debug.Log("[P43Relaunch] return ok: Restored home anchor, order " + order +
                          ", objects 4, localPos " + shelf.transform.localPosition.ToString("F3"));
            }
            catch (Exception e)
            {
                Debug.LogError("[P43Relaunch] return FAILED: " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
            finally
            {
                if (shelfGo != null) UnityEngine.Object.DestroyImmediate(shelfGo);
                if (anchorGo != null) UnityEngine.Object.DestroyImmediate(anchorGo);
                if (saveGo != null) UnityEngine.Object.DestroyImmediate(saveGo);
            }
        }

        static void SeedRoomSave()
        {
            Directory.CreateDirectory(Dir);
            var go = new GameObject("P43RoomSeed");
            try
            {
                var save = go.AddComponent<SaveSystem>();
                save.fileName = RoomPath;
                save.Load();
                save.RecordPlacement(RoomA, AnchorA, "TABLE", SavedLocalPos, SavedLocalRot, Vector3.zero);
                save.Unlock("U"); save.Unlock("B"); save.Unlock("C");
                save.Save();
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        static GameObject BuildShelf(SaveSystem save, string room, bool roomKnown,
                                     out ShelfAnchor shelf, out GameObject anchorGo)
        {
            anchorGo = new GameObject("P43RoomAnchor");
            anchorGo.transform.position = new Vector3(1f, 0f, 2f);
            anchorGo.transform.rotation = Quaternion.Euler(0f, 15f, 0f);

            var shelfGo = new GameObject("P43RoomShelf");
            shelf = shelfGo.AddComponent<ShelfAnchor>();
            shelf.save = save;
            shelf.slotRoot = new GameObject("Slots").transform;
            shelf.slotRoot.SetParent(shelfGo.transform, false);
            shelf.objectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Require(shelf.objectPrefab != null, PrefabPath + " did not load");
            shelf.idle = shelfGo.AddComponent<ShelfIdle>();

            var finder = new RoomFinder { parent = anchorGo.transform, room = room, roomKnown = roomKnown };
            typeof(ShelfAnchor).GetField("_finder", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(shelf, finder);
            return shelfGo;
        }

        static void Resolve(ShelfAnchor shelf)
        {
            typeof(ShelfAnchor).GetMethod("ResolveForCurrentRoom", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(shelf, null);
        }

        static int CountViews(GameObject root)
        {
            int n = 0;
            foreach (var unused in root.GetComponentsInChildren<ShelfObjectView>()) n++;
            return n;
        }

        [MenuItem("SIGNSPACE/P4.3 Relaunch probe - clean")]
        public static void Clean()
        {
            if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
            Debug.Log("[P43Relaunch] cleaned " + Dir);
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("P4.3 relaunch: " + message);
        }
    }
}
