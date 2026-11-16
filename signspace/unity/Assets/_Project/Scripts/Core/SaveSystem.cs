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
    public class ShapeUnlock
    {
        public string shapeId = "";
        // Empty for a migrated legacy unlock: do not invent a historic timestamp.
        public string unlockedUtc = "";
    }

    [Serializable]
    public class SaveData
    {
        /// <summary>
        /// Schema 3 adds per-shape UTC records, a layout variant and an explicit placement-valid flag.
        /// Schema 1/2 saves migrate without inventing historic unlock timestamps.
        /// </summary>
        public int schema = 3;

        public string roomId = "";
        public string anchorId = "";          // the surface's identity: the system anchor UUID
        public bool placementValid = false;  // true only when anchor-relative coordinates were recorded
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

        // `unlocked` remains the authoritative order for existing callers and legacy migration.
        public List<string> unlocked = new List<string>();
        public List<ShapeUnlock> unlockRecords = new List<ShapeUnlock>();
        public string layoutVariant = "grid-6x2-v1";
        public bool calibrated = false;
        public string lastShapeId = "";

        /// <summary>
        /// P3.2: the near-miss off switch. Stored here so the choice survives a relaunch, and read by
        /// Matcher on Awake. P6.5 surfaces it in-space as the sixth accessibility toggle - the switch
        /// exists now, with no UI of its own, so that "turn the diagnosis off" is never blocked on a
        /// phase that has not run yet.
        /// </summary>
        public bool nearMissEnabled = true;
    }

    public class SaveSystem : MonoBehaviour
    {
        [Tooltip("File name inside Application.persistentDataPath.")]
        public string fileName = "signspace-save.json";

        public SaveData Data { get; private set; } = new SaveData();

        public const int CurrentSchema = 3;
        public const string DefaultLayout = "grid-6x2-v1";
        public bool WritesBlocked { get; private set; }
        public string LoadStatus { get; private set; } = "not loaded";
        public bool LastSaveSucceeded { get; private set; }
        public string LastSaveStatus { get; private set; } = "not attempted";
        [Serializable] class SchemaHeader { public int schema; }

        public event Action OnUnlocked;

        public string Path => System.IO.Path.Combine(Application.persistentDataPath, fileName);

        /// <summary>True when the file holds enough to restore a physical placement.</summary>
        public bool HasPlacement =>
            Data != null && Data.placementValid && !string.IsNullOrEmpty(Data.anchorId);

        /// <summary>Why there is no placement, for the log. Empty when there is one.</summary>
        public string PlacementUnavailableReason
        {
            get
            {
                if (Data == null) return "no save data";
                if (string.IsNullOrEmpty(Data.anchorId)) return "the save holds no anchor (nothing placed yet)";
                if (!Data.placementValid)
                    return "saved placement has no trusted anchor-relative transform; using plinth";
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
            WritesBlocked = false;
            try
            {
                string backup = Path + ".bak";
                if (!File.Exists(Path) && File.Exists(backup))
                {
                    // A previous fallback rename may have been interrupted. Recover the last committed file.
                    File.Copy(backup, Path);
                }
                if (!File.Exists(Path)) { Data = new SaveData(); LoadStatus = "new"; return; }
                string json = File.ReadAllText(Path);
                var header = JsonUtility.FromJson<SchemaHeader>(json);
                if (header == null || header.schema < 1 || header.schema > CurrentSchema)
                    throw new InvalidDataException("unknown or missing schema; file preserved");
                var loaded = JsonUtility.FromJson<SaveData>(json);
                if (loaded == null) throw new InvalidDataException("empty save");
                Data = loaded;
                if (Data.unlocked == null) Data.unlocked = new List<string>();
                if (Data.unlockRecords == null) Data.unlockRecords = new List<ShapeUnlock>();
                // Preserve list order and unknown legacy dates, rather than pretending to know them.
                foreach (string id in Data.unlocked)
                    if (!string.IsNullOrEmpty(id) && !Data.unlockRecords.Exists(r => r != null && r.shapeId == id))
                        Data.unlockRecords.Add(new ShapeUnlock { shapeId = id, unlockedUtc = "" });
                if (string.IsNullOrEmpty(Data.layoutVariant)) Data.layoutVariant = DefaultLayout;
                int oldSchema = header.schema;
                Data.schema = CurrentSchema;
                LoadStatus = oldSchema < CurrentSchema ? "migrated schema " + oldSchema : "loaded";
                // A migration is in memory until the next normal save; schema 1 remains ineligible
                // for placement until a new anchor-local placement is explicitly recorded.
                if (oldSchema < CurrentSchema) Data.placementValid = oldSchema >= 2 && !string.IsNullOrEmpty(Data.anchorId);
            }
            catch (Exception e)
            {
                // Never overwrite a corrupt, future or unreadable file with an empty save on quit.
                WritesBlocked = true;
                LoadStatus = "read-only: " + e.Message;
                Data = new SaveData();
                Debug.LogWarning("[SaveSystem] " + LoadStatus);
            }
        }

        public void Save()
        {
            LastSaveSucceeded = false;
            if (WritesBlocked) { LastSaveStatus = "blocked: original file preserved"; return; }
            string tmp = Path + ".tmp", backup = Path + ".bak";
            try
            {
                File.WriteAllText(tmp, JsonUtility.ToJson(Data, true));
                if (!File.Exists(Path))
                {
                    File.Move(tmp, Path);
                    LastSaveSucceeded = true;
                    LastSaveStatus = "written";
                    return;
                }
                // Atomic replacement where supported. The old file is retained as a recovery backup.
                try { File.Replace(tmp, Path, backup); }
                catch (PlatformNotSupportedException)
                {
                    if (File.Exists(backup)) File.Delete(backup);
                    File.Move(Path, backup);
                    File.Move(tmp, Path);
                }
                LastSaveSucceeded = true;
                LastSaveStatus = "written";
            }
            catch (Exception e)
            {
                LastSaveStatus = "write failed: " + e.Message;
                Debug.LogWarning("[SaveSystem] could not write the save file: " + e.Message);
            }
        }

        /// <summary>Records a mastered shape. Idempotent.</summary>
        public void Unlock(string shapeId)
        {
            if (string.IsNullOrEmpty(shapeId)) return;
            if (Data.unlocked.Contains(shapeId)) return;
            Data.unlocked.Add(shapeId);
            Data.unlockRecords.Add(new ShapeUnlock { shapeId = shapeId, unlockedUtc = DateTime.UtcNow.ToString("o") });
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
            Guid roomGuid, anchorGuid;
            if (!Guid.TryParse(roomId, out roomGuid) || roomGuid == Guid.Empty ||
                !Guid.TryParse(anchorId, out anchorGuid) || anchorGuid == Guid.Empty)
            {
                Debug.LogWarning("[SaveSystem] placement lacks a stable room/anchor UUID; not saved");
                return;
            }
            if (!string.IsNullOrEmpty(Data.anchorId) &&
                (!string.Equals(Data.anchorId, anchorId, StringComparison.OrdinalIgnoreCase) ||
                 !string.Equals(Data.roomId, roomId, StringComparison.OrdinalIgnoreCase)))
            {
                Debug.LogWarning("[SaveSystem] refusing to overwrite an existing shelf placement");
                return;
            }
            Data.schema = CurrentSchema;
            Data.placementValid = true;
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
            Data.placementValid = false;
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
        /// Legacy identity-only call is deliberately ignored: an identity alone cannot restore a
        /// shelf safely without an anchor-relative transform.
        /// </summary>
        public void BindToAnchor(string roomId, string anchorId)
        {
            // Identity without an anchor-local transform is not a usable placement. Do not mutate
            // an existing shelf or pretend that the default transform was actually measured.
            Debug.LogWarning("[SaveSystem] BindToAnchor requires a measured local transform; ignored");
        }

        public bool MatchesAnchor(string anchorId) => Data.anchorId == anchorId;
    }
}
