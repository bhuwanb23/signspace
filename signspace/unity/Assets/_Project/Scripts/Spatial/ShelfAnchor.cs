using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Signspace.Matching;

namespace Signspace.Spatial
{
    /// <summary>
    /// Places the shelf, and puts it back.
    ///
    /// Three rules from MRUK-SHELF.md, all encoded here:
    ///   1. Never depend on a surface. If MRUK finds nothing, the plinth appears in the same
    ///      relative position — silently. A judge on a sofa must never see a message.
    ///   2. Persist by ANCHOR IDENTITY, never a world position. A world position survives the app
    ///      and not the room; a moved chair then puts the shelf inside the wall.
    ///   3. A surface appearing later is offered, never applied. Auto-migrating teleports the
    ///      user's progress in front of them.
    ///
    /// P1.7 adds the fourth rule, which is the one the gesture above only implied:
    ///
    ///   4. What is saved is the shelf's transform IN THE ANCHOR'S FRAME. Parenting the shelf to the
    ///      anchor transform is not a tidiness choice — it is what makes a reboot survivable. The
    ///      tracking origin is not preserved between sessions, so anything placed at a world position
    ///      comes back offset by however far the origin moved. A child of the anchor inherits the
    ///      runtime's own correction and lands on the same square of real table.
    ///
    /// WHAT IS MEASURED (P1.7 requires the timing and the not-found behaviour)
    ///   ResolveMilliseconds   how long re-finding the anchor took, from query to resolved
    ///   RestoreMilliseconds   from waking up to the shelf being physically back
    ///   WorldDriftMeters      how far the restored world position is from the one recorded last
    ///                         session. This is the tracking-origin shift, i.e. exactly the error a
    ///                         world-space save would have produced. It is a measurement, never an
    ///                         input: nothing is placed from it (see SaveData.lastWorldPos).
    /// </summary>
    public class ShelfAnchor : MonoBehaviour
    {
        public enum Outcome
        {
            NotStarted = 0,
            Restored = 1,          // resolved the saved anchor and put the shelf back
            PlacedFresh = 2,       // no usable save; found a surface and saved it
            SaveTooOld = 3,        // a save exists but predates relative transforms
            AnchorNotFound = 4,    // a save exists and the anchor did not come back
            Plinth = 5,            // no surface at all: the fallback, which is not a failure
            NoFinder = 6,          // misconfigured scene
            DifferentRoom = 7,
            RoomNotFound = 8,
            UnsupportedLayout = 9
        }

        [Header("Dependencies")]
        public MonoBehaviour finderComponent;     // implements ISurfaceFinder
        public SaveSystem save;
        public Transform userHead;
        [Tooltip("Optional. Restoration waits for the room rather than polling for it.")]
        public MrukBoot boot;

        [Header("Shelf")]
        public Transform shelfRoot;
        public GameObject objectPrefab;
        public Material objectMaterial;
        [Tooltip("Twelve slots for distinct unlocked shapes. Repeating a shape returns to its existing slot.")]
        public int slotCount = 12;
        [Tooltip("Slots are laid out in rows of this many. Two rows of six fits the 0.50 x 0.16 plate; " +
                 "one row of twelve does not.")]
        public int slotColumns = 6;
        public float slotSpacing = 0.075f;
        [Tooltip("Row depth. Only has to clear the depth of one object.")]
        public float slotRowSpacing = 0.065f;

        [Header("Slots (P4.2)")]
        [Tooltip("Objects sit under one slot transform each, so a slot's position is defined once and the " +
                 "flight, the resting place and the empty marker cannot disagree.")]
        public Transform slotRoot;
        [Tooltip("Marker for a slot. Optional: one is generated from a quad if it is left empty, because " +
                 "the empty-slot affordance must exist in the build rather than in the wiring.")]
        public GameObject slotMarker;
        [Tooltip("Material for the next, still-empty slot. Dim: it is an invitation, not a hole.")]
        public Material slotEmptyMaterial;
        [Tooltip("Marker scale, in metres. Small squares on the shelf's surface.")]
        public float slotMarkerScale = 0.05f;
        [Tooltip("The one owner of the shelf's breathing. Optional: found if left empty.")]
        public ShelfIdle idle;
        [Tooltip("Clearance above the slot marker, in metres. The shape's mesh bounds determine its lift.")]
        public float slotObjectClearance = 0.001f;
        public float distanceFromUser = 0.45f;
        public float heightOffset = 0.02f;

        [Header("Restore")]
        public bool restoreOnStart = true;

        [Tooltip("How long to wait for MRUK before giving up on the saved anchor and using the " +
                 "plinth. Must exceed the measured time-to-detection from P1.6, or a slow launch " +
                 "will look like a lost anchor.")]
        public float restoreWaitSeconds = 20f;

        public bool ParentToAnchor = true;

        [Header("Launch log (P4.1 step 5)")]
        [Tooltip("Append one line per launch: which condition was chosen, on what, and why. Read by P8's " +
                 "playtest analysis and by the video edit.")]
        public bool writeLaunchLog = true;
        [Tooltip("File name, written to Application.persistentDataPath.")]
        public string launchLogFile = "shelf-launches.jsonl";

        [Header("Plinth fallback")]
        [Tooltip("Shown when no surface is found. Same screen position as the shelf would occupy.")]
        public GameObject plinthRoot;

        /// <summary>true = on a real surface, false = plinth.</summary>
        public event Action<bool> OnModeChanged;

        public bool OnSurface { get; private set; }
        public string CurrentAnchorId { get; private set; } = "";

        // ---- P1.7 measurement surface, read by the probe and the OnGUI readout ----
        public Outcome Result { get; private set; } = Outcome.NotStarted;
        public string OutcomeDetail { get; private set; } = "not started";
        public float ResolveMilliseconds { get; private set; } = -1f;
        public float RestoreMilliseconds { get; private set; } = -1f;
        public float WorldDriftMeters { get; private set; } = -1f;
        public string SavedAnchorId { get; private set; } = "";
        public string ResolvedAnchorId { get; private set; } = "";

        /// <summary>What the shelf landed on, and its score. Empty when it landed on the plinth.</summary>
        public string ChosenLabel { get; private set; } = "";
        public float ChosenScore { get; private set; } = -1f;
        /// <summary>The line appended for this launch, or "" when logging is off.</summary>
        public string LastLaunchRecord { get; private set; } = "";

        readonly List<GameObject> _spawned = new List<GameObject>();
        ISurfaceFinder _finder;
        bool _pendingSurfaceOffer;
        bool _begun;
        float _nextRoomCheck;
        string _lastObservedRoom = "";
        bool _lastRoomKnown;

        void Awake()
        {
            _finder = finderComponent as ISurfaceFinder;
            if (_finder == null && finderComponent != null)
                Debug.LogError("[ShelfAnchor] finderComponent does not implement ISurfaceFinder");
            if (save == null) save = FindFirstObjectByType<SaveSystem>();
        }

        void Start()
        {
            StartCoroutine(RunBegin());
        }

        /// <summary>
        /// Begin(), then log — in that order and from one place, so a launch record cannot be skipped by
        /// an early `yield break` on one of the six outcome paths.
        /// </summary>
        IEnumerator RunBegin()
        {
            yield return Begin();
            _begun = true;
            _lastRoomKnown = _finder != null && _finder.TryGetCurrentRoomId(out _lastObservedRoom);
            LogLaunch();
        }

        void LogLaunch(string eventType = "launch")
        {
            LastLaunchRecord = BuildLaunchRecord(eventType);
            Debug.Log("[ShelfAnchor] launch: " + LastLaunchRecord);
            if (!writeLaunchLog) return;
            try
            {
                string path = System.IO.Path.Combine(Application.persistentDataPath, launchLogFile);
                System.IO.File.AppendAllText(path, LastLaunchRecord + "\n");
            }
            catch (Exception e)
            {
                // A log that cannot be written must never take the shelf with it.
                Debug.LogWarning("[ShelfAnchor] could not append the launch log: " + e.Message);
            }
        }

        string BuildLaunchRecord(string eventType)
        {
            string finderNote = "no finder";
            var mrukFinder = _finder as MrUkSurfaceFinder;
            if (mrukFinder != null) finderNote = mrukFinder.LastRejectionSummary;

            return "{"
                + "\"event\":\"" + Escape(eventType) + "\","
                + "\"utc\":\"" + DateTime.UtcNow.ToString("o") + "\","
                + "\"app\":\"" + Escape(Application.version) + "\","
                + "\"outcome\":\"" + Result + "\","
                + "\"onSurface\":" + (OnSurface ? "true" : "false") + ","
                + "\"label\":\"" + Escape(ChosenLabel) + "\","
                + "\"score\":" + ChosenScore.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ","
                + "\"anchor\":\"" + Escape(CurrentAnchorId) + "\","
                + "\"savedAnchor\":\"" + Escape(SavedAnchorId) + "\","
                + "\"homeAnchor\":\"" + Escape(save != null ? save.Data.anchorId : "") + "\","
                + "\"room\":\"" + Escape(_finder != null ? _finder.RoomId : "") + "\","
                + "\"savedRoom\":\"" + Escape(save != null ? save.Data.roomId : "") + "\","
                + "\"schema\":" + (save != null ? save.Data.schema : 0) + ","
                + "\"writesBlocked\":" + (save != null && save.WritesBlocked ? "true" : "false") + ","
                + "\"saveStatus\":\"" + Escape(save != null ? save.LastSaveStatus : "no save") + "\","
                + "\"layout\":\"" + Escape(save != null ? save.Data.layoutVariant : "") + "\","
                + "\"unlockOrder\":\"" + Escape(save != null ? string.Join(",", save.Data.unlocked) : "") + "\","
                + "\"resolveMs\":" + ResolveMilliseconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ","
                + "\"restoreMs\":" + RestoreMilliseconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ","
                + "\"driftM\":" + WorldDriftMeters.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ","
                + "\"detail\":\"" + Escape(OutcomeDetail) + "\","
                + "\"finder\":\"" + Escape(finderNote) + "\""
                + "}";
        }

        static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "/").Replace("\"", "'").Replace("\n", " ").Replace("\r", " ");
        }

        IEnumerator Begin()
        {
            float startedAt = Time.realtimeSinceStartup;

            if (_finder == null)
            {
                Result = Outcome.NoFinder;
                OutcomeDetail = "no ISurfaceFinder: the scene has no MrUkSurfaceFinder assigned, so " +
                                "nothing can be placed on furniture. Using the plinth.";
                UsePlinth();
                RestoreMilliseconds = (Time.realtimeSinceStartup - startedAt) * 1000f;
                yield break;
            }

            SavedAnchorId = save != null ? save.Data.anchorId : "";

            yield return WaitForRoomOrTimeout(startedAt);

            if (save == null || (!save.HasPlacement && string.IsNullOrEmpty(save.Data.anchorId) && !save.WritesBlocked))
            {
                Place(offerOnly: false);
                if (Result == Outcome.NotStarted) Result = OnSurface ? Outcome.PlacedFresh : Outcome.Plinth;
                RestoreMilliseconds = (Time.realtimeSinceStartup - startedAt) * 1000f;
                yield break;
            }

            if (save.WritesBlocked || !save.HasPlacement || !restoreOnStart)
            {
                Result = Outcome.SaveTooOld;
                OutcomeDetail = "placement unavailable or save read-only; original file is preserved";
                UsePlinth();
            }
            else
                ResolveForCurrentRoom();
            RestoreMilliseconds = (Time.realtimeSinceStartup - startedAt) * 1000f;
        }

        // A single saved home placement, plus a temporary plinth outside that room. Progress remains
        // global: unlocks made on the plinth are saved, but never replace the home's room or anchor.
        void ResolveForCurrentRoom()
        {
            string room;
            if (_finder == null || !_finder.TryGetCurrentRoomId(out room))
            {
                Result = Outcome.RoomNotFound;
                OutcomeDetail = "current room UUID unavailable; saved placement preserved";
                UsePlinth();
                return;
            }
            if (!string.Equals(room, save.Data.roomId, StringComparison.OrdinalIgnoreCase))
            {
                Result = Outcome.DifferentRoom;
                OutcomeDetail = "different room; saved home placement preserved";
                UsePlinth();
                return;
            }
            if (save.Data.layoutVariant != SaveSystem.DefaultLayout)
            {
                Result = Outcome.UnsupportedLayout;
                OutcomeDetail = "unknown layout variant; saved data preserved";
                UsePlinth();
                return;
            }
            slotColumns = 6;
            float t0 = Time.realtimeSinceStartup;
            SurfaceCandidate surface;
            Transform anchorTransform;
            bool resolved = _finder.TryResolve(save.Data.anchorId, out surface, out anchorTransform);
            ResolveMilliseconds = (Time.realtimeSinceStartup - t0) * 1000f;
            if (!ParentToAnchor || !resolved || anchorTransform == null ||
                !string.Equals(surface.RoomId, room, StringComparison.OrdinalIgnoreCase))
            {
                bool alreadyFallback = Result == Outcome.AnchorNotFound && !OnSurface;
                Result = Outcome.AnchorNotFound;
                OutcomeDetail = "saved anchor unavailable in current room; saved placement preserved";
                if (!alreadyFallback) UsePlinth();
                return;
            }
            Restore(surface, anchorTransform);
        }

        void Update()
        {
            if (!_begun || _finder == null || save == null || !save.HasPlacement || save.WritesBlocked ||
                !restoreOnStart || Time.unscaledTime < _nextRoomCheck) return;
            _nextRoomCheck = Time.unscaledTime + 1f;
            string room;
            bool known = _finder.TryGetCurrentRoomId(out room);
            bool changed = known != _lastRoomKnown ||
                (known && !string.Equals(room, _lastObservedRoom, StringComparison.OrdinalIgnoreCase));
            // Retry an unresolved anchor in the same room as MRUK finishes localising.
            if (!changed && !(known && Result == Outcome.AnchorNotFound)) return;
            _lastRoomKnown = known;
            _lastObservedRoom = room;
            var previous = Result;
            ResolveForCurrentRoom();
            // A transition is an additional JSONL event; retries are not relaunch measurements.
            if (changed || previous != Result) LogLaunch("room-transition");
        }

        /// <summary>
        /// Wait for MRUK's room, not for a stopwatch. `boot` publishes readiness (P1.6), so this
        /// costs one subscription instead of a poll, and it degrades to the timeout when no boot
        /// component is wired.
        /// </summary>
        IEnumerator WaitForRoomOrTimeout(float startedAt)
        {
            if (boot == null) yield break;
            if (boot.Current == MrukBoot.State.Ready) yield break;

            bool ready = false;
            Action onReady = () => ready = true;
            boot.OnRoomReady += onReady;
            while (!ready && boot.Current != MrukBoot.State.Ready
                   && Time.realtimeSinceStartup - startedAt < restoreWaitSeconds)
            {
                yield return null;
            }
            boot.OnRoomReady -= onReady;
        }

        string DescribeWhyNotFound()
        {
            if (boot == null) return "no MrukBoot to explain it";
            if (boot.Current == MrukBoot.State.NoRoom) return "no room loaded: " + boot.Status;
            if (boot.Current == MrukBoot.State.Failed) return "MRUK failed: " + boot.Status;
            if (boot.Current == MrukBoot.State.Waiting) return "MRUK was still loading after " +
                                                               restoreWaitSeconds.ToString("0") + " s";
            return "the room loaded but no anchor carries that id - a rescan replaces anchor ids, " +
                   "so a rescanned room legitimately loses the shelf";
        }

        void Restore(SurfaceCandidate surface, Transform anchorTransform)
        {
            float t0 = Time.realtimeSinceStartup;

            Vector3 before = Vector3.zero;
            bool hadDiagnostic = save.Data.lastWorldPos.sqrMagnitude > 0.000001f;
            if (hadDiagnostic) before = save.Data.lastWorldPos;

            if (ParentToAnchor && anchorTransform != null)
                transform.SetParent(anchorTransform, false);
            else
                transform.SetParent(null, true);

            transform.localPosition = save.Data.localPos;
            transform.localRotation = save.Data.localRot;

            OnSurface = true;
            CurrentAnchorId = surface.AnchorId;
            ResolvedAnchorId = surface.AnchorId;
            ChosenLabel = surface.Label;
            ChosenScore = surface.Score;

            if (hadDiagnostic) WorldDriftMeters = Vector3.Distance(transform.position, before);
            else WorldDriftMeters = -1f;

            if (save != null) save.NoteRestored();
            SetMode(true);
            RestoreUnlocked();

            Result = Outcome.Restored;
            OutcomeDetail = "restored onto " + surface.Label + " (" + Shorten(surface.AnchorId) + ") in " +
                            surface.RoomId;

            // The restore itself is timed separately: parenting plus two local assignments is cheap,
            // but it is measured rather than assumed because it happens during launch.
            Debug.Log("[ShelfAnchor] restore step took " +
                      ((Time.realtimeSinceStartup - t0) * 1000f).ToString("0.00") + " ms");
        }

        /// <summary>
        /// Resolve placement. Called on start, on room change, and from the debug overlay.
        /// </summary>
        public void Place(bool offerOnly)
        {
            if (save != null && (save.WritesBlocked || !string.IsNullOrEmpty(save.Data.anchorId)))
            {
                // Never silently migrate a saved shelf. Explicit reset is a separate operation.
                if (offerOnly) _pendingSurfaceOffer = true;
                return;
            }
            SurfaceCandidate surface = default;
            bool found = _finder != null && _finder.TryGetPrimary(out surface);

            if (!found)
            {
                UsePlinth();
                return;
            }

            // A surface we have never seen, while already placed: offer, do not move.
            if (offerOnly && OnSurface && CurrentAnchorId != surface.AnchorId)
            {
                _pendingSurfaceOffer = true;
                return;
            }

            // Parent BEFORE positioning, so the transform written to disk is anchor-relative and the
            // same square of furniture comes back in the next session.
            Transform anchorTransform = null;
            SurfaceCandidate resolved;
            if (_finder.TryResolve(surface.AnchorId, out resolved, out anchorTransform) == false)
                anchorTransform = null;

            Guid roomGuid, anchorGuid;
            if (!ParentToAnchor || anchorTransform == null ||
                !Guid.TryParse(surface.RoomId, out roomGuid) || roomGuid == Guid.Empty ||
                !Guid.TryParse(surface.AnchorId, out anchorGuid) || anchorGuid == Guid.Empty)
            {
                Result = Outcome.RoomNotFound;
                OutcomeDetail = "stable room/anchor UUID unavailable; using temporary plinth";
                UsePlinth();
                return;
            }

            if (ParentToAnchor && anchorTransform != null)
                transform.SetParent(anchorTransform, false);
            else
                transform.SetParent(null, true);

            transform.position = surface.Position + surface.Normal * heightOffset;
            FaceUser();

            CurrentAnchorId = surface.AnchorId;
            ResolvedAnchorId = surface.AnchorId;
            ChosenLabel = surface.Label;
            ChosenScore = surface.Score;
            OnSurface = true;

            if (save != null && ParentToAnchor && anchorTransform != null)
            {
                save.RecordPlacement(surface.RoomId, surface.AnchorId, surface.Label,
                                     transform.localPosition, transform.localRotation, transform.position);
            }

            SetMode(true);
            RestoreUnlocked();

            if (Result == Outcome.NotStarted || Result == Outcome.SaveTooOld)
            {
                Result = Outcome.PlacedFresh;
                OutcomeDetail = "placed fresh on " + surface.Label + " (" + Shorten(surface.AnchorId) +
                                "), saved in anchor-local space";
            }

            Debug.Log("[ShelfAnchor] new anchor '" + Shorten(surface.AnchorId) +
                      "' — starting a fresh shelf in this room");
        }

        void FaceUser()
        {
            if (userHead == null) return;
            var flat = userHead.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
        }

        void UsePlinth()
        {
            // Deliberately the same relative placement as a real surface would get, so the
            // experience is identical in shape — only the ground it sits on is virtual.
            transform.SetParent(null, true);
            if (userHead != null)
            {
                var forward = userHead.forward; forward.y = 0f; forward.Normalize();
                transform.position = userHead.position + forward * distanceFromUser;
                transform.position += Vector3.down * 0.25f;
                FaceUser();
            }
            CurrentAnchorId = "";
            ResolvedAnchorId = "";
            WorldDriftMeters = -1f;
            ChosenLabel = "plinth";
            ChosenScore = -1f;
            if (Result == Outcome.NotStarted) Result = Outcome.Plinth;
            SetMode(false);
            RestoreUnlocked();
        }

        void SetMode(bool onSurface)
        {
            OnSurface = onSurface;
            if (plinthRoot != null) plinthRoot.SetActive(!onSurface);
            if (shelfRoot != null) shelfRoot.gameObject.SetActive(true);
            OnModeChanged?.Invoke(onSurface);
        }

        /// <summary>Re-spawn every object for the shapes already unlocked in this room.</summary>
        public void RestoreUnlocked()
        {
            if (idle == null) idle = FindFirstObjectByType<ShelfIdle>();
            foreach (var go in _spawned)
            {
                if (go == null) continue;
                if (idle != null) idle.Unregister(go.transform);
                Destroy(go);
            }
            _spawned.Clear();
            if (save == null || objectPrefab == null) return;
            for (int i = 0; i < save.Data.unlocked.Count && i < slotCount; i++)
            {
                var go = SpawnAt(i, save.Data.unlocked[i]);
                RegisterIdle(go);       // restored objects are already at rest: no settle to wait for
            }
            RefreshSlots();
        }

        /// <summary>Called by the lock payoff. Places one object with a settle animation.</summary>
        public GameObject AddObject(string shapeId, int slot)
        {
            var go = SpawnAt(slot, shapeId);
            if (go != null) StartCoroutine(Settle(go));
            return go;
        }

        GameObject SpawnAt(int slot, string shapeId)
        {
            if (objectPrefab == null || slot < 0 || slot >= slotCount) return null;

            Transform slotTransform = SlotTransform(slot);
            ClearSlot(slot, null);
            var go = Instantiate(objectPrefab, slotTransform != null ? slotTransform : transform);
            go.name = "ShelfObject_" + shapeId + "_" + slot;

            var view = go.GetComponent<ShelfObjectView>();
            if (view != null)
            {
                if (objectMaterial != null) view.objectMaterial = objectMaterial;
                view.SetShape(shapeId);
            }
            if (slotTransform != null) go.transform.localPosition = Vector3.up * ObjectLift(shapeId);

            // No breathing yet: anything that arrives with a settle registers when it has stopped
            // moving (Settle), because the idle and the settle would otherwise write the same scale.
            _spawned.Add(go);
            RefreshSlots();
            return go;
        }

        /// <summary>
        /// The transform for a slot, created once and reused. Slot 0 is the leftmost of the front row and
        /// the grid is centred on the shelf, so a two-object shelf is not stuck against one edge.
        /// </summary>
        public Transform SlotTransform(int slot)
        {
            if (slot < 0 || slot >= slotCount) return null;
            Transform parent = slotRoot != null ? slotRoot : transform;
            string name = "Slot_" + slot;
            Transform existing = parent.Find(name);
            if (existing != null) return existing;

            int columns = slotColumns > 0 ? slotColumns : slotCount;
            int rows = (slotCount + columns - 1) / columns;
            int column = slot % columns;
            int row = slot / columns;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(
                (column - (columns - 1) * 0.5f) * slotSpacing,
                0f,
                (row - (rows - 1) * 0.5f) * slotRowSpacing);
            return go.transform;
        }

        /// <summary>
        /// Show the filled slots and ONE empty one - the hook at the end of the session. From the first
        /// launch that means a single dimmed slot and nothing else, which is the point: the shelf is a
        /// record with room in it, not a trophy cabinet that happens to be empty.
        /// </summary>
        public void RefreshSlots()
        {
            int unlocked = save != null && save.Data != null ? save.Data.unlocked.Count : 0;

            for (int i = 0; i < slotCount; i++)
            {
                Transform slot = SlotTransform(i);
                if (slot == null) continue;

                bool isNextEmpty = i == unlocked;
                bool visible = i < unlocked || isNextEmpty;

                Transform marker = slot.Find("Marker");
                if (marker == null && visible)
                {
                    marker = MakeMarker(slot);
                    if (marker == null) continue;
                }
                if (marker == null) continue;

                marker.gameObject.SetActive(visible);
                if (!visible) continue;

                var renderer = marker.GetComponent<Renderer>();
                if (renderer == null) continue;

                // Tint RGB rather than relying on alpha (the material can be opaque). Refresh both
                // states so a marker brightens when its slot becomes occupied.
                if (slotEmptyMaterial != null) renderer.sharedMaterial = slotEmptyMaterial;
                Tint(renderer, isNextEmpty
                    ? new Color(0.30f, 0.32f, 0.35f, 1f)
                    : new Color(0.58f, 0.60f, 0.64f, 1f));
            }
        }

        Transform MakeMarker(Transform slot)
        {
            GameObject go = slotMarker != null ? Instantiate(slotMarker, slot) : MakeQuadMarker(slot);
            if (go == null) return null;
            go.name = "Marker";
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one * slotMarkerScale;
            return go.transform;
        }

        static MaterialPropertyBlock _tintBlock;

        static void Tint(Renderer renderer, Color colour)
        {
            if (renderer == null || renderer.sharedMaterial == null) return;
            if (_tintBlock == null) _tintBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_tintBlock);
            if (renderer.sharedMaterial.HasProperty("_BaseColor")) _tintBlock.SetColor("_BaseColor", colour);
            if (renderer.sharedMaterial.HasProperty("_Color")) _tintBlock.SetColor("_Color", colour);
            renderer.SetPropertyBlock(_tintBlock);
        }

        static GameObject MakeQuadMarker(Transform slot)
        {
            // A generated marker rather than a required prefab: the affordance has to exist in a scene
            // nobody wired.
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) Destroy(collider); else DestroyImmediate(collider);
            }
            go.transform.SetParent(slot, false);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // flat on the shelf
            return go;
        }

        /// <summary>
        /// One object per slot. Meeting a shape you already have does not put a second copy on the shelf -
        /// it goes back to its own slot, and the copy that was there steps aside. A shelf that grows two
        /// of everything the user repeats is a scoreboard, not a shelf.
        /// </summary>
        void ClearSlot(int slot, GameObject except)
        {
            Transform target = SlotTransform(slot);
            if (target == null) return;

            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                var other = _spawned[i];
                if (other == null)
                {
                    _spawned.RemoveAt(i);
                    continue;
                }
                if (other == except || other.transform.parent != target) continue;
                _spawned.RemoveAt(i);
                if (idle != null) idle.Unregister(other.transform);
                Destroy(other);
            }
        }

        /// <summary>Hand an object to the idle, once it has stopped moving. One owner, one baseline.</summary>
        void RegisterIdle(GameObject go)
        {
            if (go == null) return;
            if (idle == null) idle = FindFirstObjectByType<ShelfIdle>();
            if (idle == null) return;                 // no idle in the scene is a valid configuration
            idle.Register(go.transform);
            idle.Rebaseline(go.transform);            // whatever it was scaled to on arrival is the rest scale
        }

        /// <summary>
        /// Take ownership of an object that has already flown here (the lock payoff builds it, so it can
        /// be seen leaving the hand). From this call on it is a shelf object: one slot, one silhouette,
        /// registered with the one idle.
        /// </summary>
        public GameObject Adopt(GameObject go, string shapeId)
        {
            if (go == null) return null;

            int index = SlotIndexFor(shapeId);
            Transform slot = SlotTransform(index);
            if (slot == null) return go;              // no free slot: leave it where it landed

            var view = go.GetComponent<ShelfObjectView>();
            if (view == null)
            {
                // The payoff's prototype is not a shelf object (a plain cube from the P2 rig). Replace it
                // with the shelf's own, so what stays in the room is one kind of thing, not two.
                Destroy(go);
                var replacement = SpawnAt(index, shapeId);
                if (replacement != null) StartCoroutine(Settle(replacement));
                return replacement;
            }

            ClearSlot(index, go);
            go.transform.SetParent(slot, false);
            go.transform.localPosition = Vector3.up * ObjectLift(shapeId);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            if (objectMaterial != null) view.objectMaterial = objectMaterial;
            view.SetShape(shapeId);

            if (!_spawned.Contains(go)) _spawned.Add(go);
            RegisterIdle(go);
            RefreshSlots();
            return go;
        }

        /// <summary>Lift the mesh so its bottom rests just above its marker.</summary>
        public float ObjectLift(string shapeId)
        {
            var mesh = ShelfSilhouettes.For(shapeId);
            return Mathf.Max(0f, -mesh.bounds.min.y) + slotObjectClearance;
        }

        /// <summary>Which slot a shape belongs in, or -1 when there is no free slot.</summary>
        public int SlotIndexFor(string shapeId)
        {
            int index = -1;
            if (save != null && save.Data != null) index = save.Data.unlocked.IndexOf(shapeId);
            if (index < 0) index = save != null && save.Data != null ? save.Data.unlocked.Count : 0;
            return index >= slotCount ? -1 : index;
        }

        /// <summary>
        /// The slot an object for this shape should fly to. An already-unlocked shape goes back to its own
        /// slot; a new one takes the next empty slot.
        /// </summary>
        public bool TryGetSlotFor(string shapeId, out Transform slot)
        {
            slot = SlotTransform(SlotIndexFor(shapeId));
            return slot != null;
        }

        IEnumerator Settle(GameObject go)
        {
            var t = go.transform;
            var target = t.localScale;
            t.localScale = target * 0.01f;
            float k = 0f;
            while (k < 1f)
            {
                if (t == null) yield break;
                k += Time.deltaTime / 0.35f;
                t.localScale = Vector3.Lerp(target * 0.01f, target, Mathf.SmoothStep(0f, 1f, k));
                yield return null;
            }

            if (t != null)
            {
                t.localScale = target;
                RegisterIdle(go);
            }
        }

        /// <summary>True when a better surface has appeared and the user has not taken it.</summary>
        public bool HasPendingSurfaceOffer => _pendingSurfaceOffer;

        public void AcceptSurfaceOffer()
        {
            if (!_pendingSurfaceOffer) return;
            _pendingSurfaceOffer = false;
            Place(offerOnly: false);
        }

        static string Shorten(string id)
        {
            if (string.IsNullOrEmpty(id)) return "(none)";
            return id.Length <= 12 ? id : id.Substring(0, 8) + "…";
        }
    }
}
