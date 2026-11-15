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
        public int schema = 1;
        public string roomId = "";
        public string anchorId = "";
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

        string Path => System.IO.Path.Combine(Application.persistentDataPath, fileName);

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
        /// Binds this save to an anchor. If the anchor changed, the unlocked list
        /// is cleared: the shelf belongs to a surface, not to a person.
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
