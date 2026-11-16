using System;
using System.IO;
using System.Reflection;
using Signspace.Matching;
using Signspace.Spatial;
using UnityEditor;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>Editor-only integration checks against the actual save and room decision code.</summary>
    public static class PersistenceP43Checks
    {
        const string RoomA = "11111111-1111-4111-8111-111111111111";
        const string RoomB = "22222222-2222-4222-8222-222222222222";
        const string Anchor = "33333333-3333-4333-8333-333333333333";
        sealed class FakeFinder : ISurfaceFinder
        {
            public string room = RoomA;
            public bool roomKnown = true, anchorKnown = true;
            public Transform parent;
            public string RoomId => roomKnown ? room : "";
            public bool TryGetCurrentRoomId(out string id) { id = RoomId; return roomKnown; }
            public bool TryGetPrimary(out SurfaceCandidate surface) { surface = default; return false; }
            public bool TryResolve(string id, out SurfaceCandidate surface, out Transform t)
            {
                surface = new SurfaceCandidate { AnchorId = Anchor, RoomId = room };
                t = anchorKnown ? parent : null;
                return anchorKnown && id == Anchor;
            }
        }
        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("P4.3 check: " + message);
        }

        [MenuItem("SIGNSPACE/P4.3 Check persistence")]
        public static void Run()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "signspace-p43-" + Guid.NewGuid());
            Directory.CreateDirectory(dir);
            GameObject saveGo = null, shelfGo = null, anchorGo = null;
            try
            {
                saveGo = new GameObject("P43SaveCheck");
                var save = saveGo.AddComponent<SaveSystem>();
                save.fileName = System.IO.Path.Combine(dir, "save.json");
                save.Load();
                save.RecordPlacement(RoomA, Anchor, "TABLE", new Vector3(.1f, .2f, .3f), Quaternion.identity, Vector3.zero);
                save.Unlock("U"); save.Unlock("B"); save.Unlock("U");
                string stamp = save.Data.unlockRecords[0].unlockedUtc;
                Require(!string.IsNullOrEmpty(stamp) && save.Data.unlocked.Count == 2, "unlock timestamps/idempotence");
                save.Load();
                Require(save.Data.schema == 3 && save.HasPlacement && save.Data.unlocked[0] == "U" &&
                        save.Data.unlocked[1] == "B" && save.Data.unlockRecords[0].unlockedUtc == stamp &&
                        save.Data.layoutVariant == SaveSystem.DefaultLayout, "round trip/order/layout");
                save.RecordPlacement(RoomB, "44444444-4444-4444-8444-444444444444", "TABLE", Vector3.zero, Quaternion.identity, Vector3.zero);
                Require(save.Data.roomId == RoomA && save.Data.anchorId == Anchor, "replacement must be rejected");

                anchorGo = new GameObject("P43AnchorCheck");
                shelfGo = new GameObject("P43ShelfCheck");
                var shelf = shelfGo.AddComponent<ShelfAnchor>();
                shelf.save = save;
                var finder = new FakeFinder { parent = anchorGo.transform };
                typeof(ShelfAnchor).GetField("_finder", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(shelf, finder);
                var resolve = typeof(ShelfAnchor).GetMethod("ResolveForCurrentRoom", BindingFlags.Instance | BindingFlags.NonPublic);
                resolve.Invoke(shelf, null);
                Require(shelf.Result == ShelfAnchor.Outcome.Restored && shelf.transform.parent == anchorGo.transform &&
                        shelf.transform.localPosition == new Vector3(.1f, .2f, .3f), "home restore in anchor frame");
                finder.room = RoomB;
                resolve.Invoke(shelf, null);
                Require(shelf.Result == ShelfAnchor.Outcome.DifferentRoom && save.Data.anchorId == Anchor &&
                        save.Data.unlocked.Count == 2, "different room preserves home");
                finder.roomKnown = false;
                resolve.Invoke(shelf, null);
                Require(shelf.Result == ShelfAnchor.Outcome.RoomNotFound && save.Data.roomId == RoomA, "unknown room preserves home");
                finder.roomKnown = true; finder.room = RoomA; finder.anchorKnown = false;
                resolve.Invoke(shelf, null);
                Require(shelf.Result == ShelfAnchor.Outcome.AnchorNotFound && save.Data.anchorId == Anchor, "missing anchor preserves home");
                finder.anchorKnown = true;
                resolve.Invoke(shelf, null);
                Require(shelf.Result == ShelfAnchor.Outcome.Restored, "return home");

                // A version-2 save had no per-shape timestamp: preserve order and mark it unknown.
                File.WriteAllText(save.Path, "{\"schema\":2,\"roomId\":\"" + RoomA + "\",\"anchorId\":\"" + Anchor + "\",\"unlocked\":[\"V\",\"C\"]}");
                save.Load();
                Require(save.Data.schema == 3 && save.HasPlacement && save.Data.unlocked[0] == "V" &&
                        save.Data.unlockRecords.Count == 2 && save.Data.unlockRecords[0].unlockedUtc == "", "schema 2 migration");
                File.WriteAllText(save.Path, "{\"schema\":1,\"roomId\":\"" + RoomA + "\",\"anchorId\":\"" + Anchor + "\",\"unlocked\":[\"U\"]}");
                save.Load(); save.Save(); save.Load();
                Require(!save.HasPlacement && save.Data.anchorId == Anchor && save.Data.unlocked[0] == "U",
                        "schema 1 placement remains untrusted after migration");
                string future = "{\"schema\":999,\"keep\":\"untouched\"}";
                File.WriteAllText(save.Path, future);
                save.Load(); save.Unlock("O"); save.Save();
                Require(save.WritesBlocked && File.ReadAllText(save.Path) == future, "future file must not be overwritten");
                File.WriteAllText(save.Path, "not-json");
                save.Load(); save.Save();
                Require(save.WritesBlocked && File.ReadAllText(save.Path) == "not-json", "corrupt file must not be overwritten");
                Debug.Log("[P4.3] editor checks passed. Headset relaunch/reboot/room matrix is still pending.");
            }
            finally
            {
                if (shelfGo != null) UnityEngine.Object.DestroyImmediate(shelfGo);
                if (anchorGo != null) UnityEngine.Object.DestroyImmediate(anchorGo);
                if (saveGo != null) UnityEngine.Object.DestroyImmediate(saveGo);
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }
    }
}
