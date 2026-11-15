using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>
    /// Persistence for the shelf.
    ///
    /// One file, written on lock and on quit. Keyed to the MRUK ANCHOR, never to
    /// a world position: a world position survives the app but not the room. If
    /// the room cannot be re-resolved, the plinth state is used silently.
    ///
    /// See ARCHITECTURE.md section 4.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>
        /// 2 since P1.7. Version 1 had an anchor id and no relative transform, which is not enough to
        /// put anything back where it was - so a version 1 file is READ, never silently trusted.
        /// </summary>
        public int schema = 2;

        public string roomId = "";
        public string anchorId = "";          // the surface's identity: the system anchor UUID
        public string anchorLabel = "";       // TABLE / COUCH / BED / STORAGE, for the log and UI

        // The thing P1.7 is actually about. These are in the ANCHOR's local space, so they mean the
        // same physical place in every session, whatever the tracking origin happens to be.
        public Vector3 localPos = Vector3.zero;
        public Quaternion localRot = Quaternion.identity;

        /// <summary>
        /// Measurement only. Never read to place anything.
        ///
        /// It is written so that the log can answer a question the saved-anchor design otherwise
        /// hides: how far did the tracking origin move between sessions? If a world position had been
        /// used, that number is the error the user would have seen. Recording it turns "anchors are
        /// better than world positions" from an assertion into a measurement.
        /// </summary>
        public Vector3 lastWorldPos = Vector3.zero;

        public string savedUtc = "";
        public int restoreCount = 0;

        public List<string> unlocked = new List<string>();
        public bool calibrated = false;
        public string lastShapeId = "";
    }

    public class SaveSystem : MonoBehaviour
    {
        [Tooltip("File name inside Application.persistentDataPath.")]
        public string fileName = "signspace-save.json";

        public SaveData Data { get; private set; } = new SaveData();

        public event Action OnUnlocked;

        public string Path => System.IO.Path.Combine(Application.persistentDataPath, fileName);

        /// <summary>True when the file holds enough to restore a physical placement.</summary>
        public bool HasPlacement =>
            Data != null && Data.schema >= 2 && !string.IsNullOrEmpty(Data.anchorId);

        /// <summary>Why there is no placement, for the log. Empty when there is one.</summary>
        public string PlacementUnavailableReason
        {
            get
            {
                if (Data == null) return "no save data";
                if (string.IsNullOrEmpty(Data.anchorId)) return "the save holds no anchor (nothing placed yet)";
                if (Data.schema < 2)
                    return "save schema " + Data.schema + " predates relative transforms; it stores an " +
                           "anchor id but no offset, so the shelf is re-placed from the surface instead";
                return "";
            }
        }

        void Awake()
        {
            Load();
        }

        void OnApplicationQuit() => Save();

        void OnApplicationPause(bool paused) { if (paused) Save(); }

        public void Load()
        {
            try
            {
                if (!File.Exists(Path)) { Data = new SaveData(); return; }
                var json = File.ReadAllText(Path);
                var loaded = JsonUtility.FromJson<SaveData>(json);
                Data = loaded ?? new SaveData();
                if (Data.unlocked == null) Data.unlocked = new List<string>();
            }
            catch (Exception e)
            {
                // A corrupt save must never stop the app from starting. The
                // worst outcome is an empty shelf, which is a normal outcome.
                Debug.LogWarning("[SaveSystem] could not read the save file: " + e.Message);
                Data = new SaveData();
            }
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(Path, JsonUtility.ToJson(Data, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveSystem] could not write the save file: " + e.Message);
            }
        }

        /// <summary>Records a mastered shape. Idempotent.</summary>
        public void Unlock(string shapeId)
        {
            if (string.IsNullOrEmpty(shapeId)) return;
            if (Data.unlocked.Contains(shapeId)) return;
            Data.unlocked.Add(shapeId);
            Save();
            OnUnlocked?.Invoke();
        }

        public bool IsUnlocked(string shapeId) => Data.unlocked.Contains(shapeId);

        /// <summary>
        /// Records where the shelf sits, in the anchor's own frame.
        ///
        /// `localPos`/`localRot` are the shelf transform expressed relative to the anchor transform,
        /// which is the only form that survives a reboot: the tracking origin is not preserved across
        /// sessions, so a world position would come back in the wrong place (P1.7 trap).
        /// </summary>
        public void RecordPlacement(string roomId, string anchorId, string anchorLabel,
                                    Vector3 localPos, Quaternion localRot, Vector3 worldPosForMeasurement)
        {
            if (string.IsNullOrEmpty(anchorId))
            {
                Debug.LogWarning("[SaveSystem] refusing to record a placement with no anchor id");
                return;
            }

            if (Data.anchorId != anchorId)
            {
                // The shelf belongs to a surface, not to a person: progress does not travel.
                Data.unlocked.Clear();
                Data.restoreCount = 0;
            }

            Data.schema = 2;
            Data.roomId = roomId ?? "";
            Data.anchorId = anchorId;
            Data.anchorLabel = anchorLabel ?? "";
            Data.localPos = localPos;
            Data.localRot = localRot;
            Data.lastWorldPos = worldPosForMeasurement;
            Data.savedUtc = DateTime.UtcNow.ToString("o");
            Save();
        }

        /// <summary>Counts a successful restore. Saved immediately: the process may be killed next.</summary>
        public void NoteRestored()
        {
            Data.restoreCount++;
            Save();
        }

        /// <summary>Forgets the placement but keeps progress. Used by the P1.7 rig's reset.</summary>
        public void ClearPlacement()
        {
            Data.anchorId = "";
            Data.roomId = "";
            Data.anchorLabel = "";
            Data.localPos = Vector3.zero;
            Data.localRot = Quaternion.identity;
            Data.lastWorldPos = Vector3.zero;
            Data.savedUtc = "";
            Data.restoreCount = 0;
            Save();
        }

        /// <summary>
        /// Binds this save to an anchor without a transform. Kept for callers that only care which
        /// surface owns the progress.
        /// </summary>
        public void BindToAnchor(string roomId, string anchorId)
        {
            if (Data.anchorId == anchorId) return;
            Data.roomId = roomId ?? "";
            Data.anchorId = anchorId ?? "";
            Data.unlocked.Clear();
            Save();
        }

        public bool MatchesAnchor(string anchorId) => Data.anchorId == anchorId;
    }
}
