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
            NoFinder = 6           // misconfigured scene
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
        public int slotCount = 6;
        public float slotSpacing = 0.075f;
        public float distanceFromUser = 0.45f;
        public float heightOffset = 0.02f;

        [Header("Restore")]
        public bool restoreOnStart = true;

        [Tooltip("How long to wait for MRUK before giving up on the saved anchor and using the " +
                 "plinth. Must exceed the measured time-to-detection from P1.6, or a slow launch " +
                 "will look like a lost anchor.")]
        public float restoreWaitSeconds = 20f;

        public bool ParentToAnchor = true;

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

        readonly List<GameObject> _spawned = new List<GameObject>();
        ISurfaceFinder _finder;
        bool _pendingSurfaceOffer;

        void Awake()
        {
            _finder = finderComponent as ISurfaceFinder;
            if (_finder == null && finderComponent != null)
                Debug.LogError("[ShelfAnchor] finderComponent does not implement ISurfaceFinder");
            if (save == null) save = FindFirstObjectByType<SaveSystem>();
        }

        void Start()
        {
            StartCoroutine(Begin());
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
                yield break;
            }

            SavedAnchorId = save != null ? save.Data.anchorId : "";

            // ---- no usable save: normal first-run placement --------------------------------
            if (save == null || !save.HasPlacement)
            {
                Result = save != null && !string.IsNullOrEmpty(save.Data.anchorId)
                    ? Outcome.SaveTooOld
                    : Outcome.NotStarted;
                OutcomeDetail = save != null ? save.PlacementUnavailableReason : "no SaveSystem";

                // restoreOnStart=false means "do not try to re-find anything", not "place nothing":
                // the shelf still has to appear, so it is placed from the live surface and saved.
                yield return WaitForRoomOrTimeout(startedAt);
                Place(offerOnly: false);
                if (Result == Outcome.NotStarted) Result = OnSurface ? Outcome.PlacedFresh : Outcome.Plinth;
                RestoreMilliseconds = (Time.realtimeSinceStartup - startedAt) * 1000f;
                yield break;
            }

            if (!restoreOnStart)
            {
                yield return WaitForRoomOrTimeout(startedAt);
                Place(offerOnly: false);
                Result = OnSurface ? Outcome.PlacedFresh : Outcome.Plinth;
                OutcomeDetail = "restoreOnStart is off, so the saved anchor was ignored and the shelf " +
                                "was placed from the live surface";
                RestoreMilliseconds = (Time.realtimeSinceStartup - startedAt) * 1000f;
                yield break;
            }

            // ---- wait for the room, then resolve ------------------------------------------
            yield return WaitForRoomOrTimeout(startedAt);

            float t0 = Time.realtimeSinceStartup;
            SurfaceCandidate surface;
            Transform anchorTransform;
            bool ok = _finder.TryResolve(SavedAnchorId, out surface, out anchorTransform);
            ResolveMilliseconds = (Time.realtimeSinceStartup - t0) * 1000f;

            if (!ok)
            {
                // THE DOCUMENTED NOT-FOUND BEHAVIOUR.
                // The save is left INTACT on purpose. A shelf that forgets its anchor because the
                // headset launched in another room would lose a week of progress to a walk down the
                // corridor; instead the plinth is shown now and the saved anchor is retried on the
                // next launch, or the moment a room loads.
                Result = Outcome.AnchorNotFound;
                OutcomeDetail = "saved anchor '" + Shorten(SavedAnchorId) + "' did not come back (" +
                                DescribeWhyNotFound() + "). The save is kept, the plinth is shown, and " +
                                "the anchor will be retried on the next launch.";
                UsePlinth();
                RestoreMilliseconds = (Time.realtimeSinceStartup - startedAt) * 1000f;
                Debug.LogWarning("[ShelfAnchor] " + OutcomeDetail);
                yield break;
            }

            Restore(surface, anchorTransform);
            RestoreMilliseconds = (Time.realtimeSinceStartup - startedAt) * 1000f;

            Debug.Log("[ShelfAnchor] restored onto '" + surface.Label + "' in " +
                      ResolveMilliseconds.ToString("0.0") + " ms (resolve), " +
                      RestoreMilliseconds.ToString("0.0") + " ms (total), drift " +
                      WorldDriftMeters.ToString("0.000") + " m");
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
            // `default`: the short-circuit leaves `surface` unassigned when _finder is null, which the
            // compiler correctly refuses to let us read after the `found` check.
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

            if (ParentToAnchor && anchorTransform != null)
                transform.SetParent(anchorTransform, false);
            else
                transform.SetParent(null, true);

            transform.position = surface.Position + surface.Normal * heightOffset;
            FaceUser();

            CurrentAnchorId = surface.AnchorId;
            ResolvedAnchorId = surface.AnchorId;
            OnSurface = true;

            if (save != null)
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
            foreach (var go in _spawned) if (go != null) Destroy(go);
            _spawned.Clear();
            if (save == null || objectPrefab == null) return;
            for (int i = 0; i < save.Data.unlocked.Count; i++)
                SpawnAt(i, save.Data.unlocked[i]);
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
            if (objectPrefab == null) return null;
            var go = Instantiate(objectPrefab, transform);
            float x = (slot - (slotCount - 1) * 0.5f) * slotSpacing;
            go.transform.localPosition = new Vector3(x, 0f, 0f);
            go.name = $"ShelfObject_{shapeId}_{slot}";
            _spawned.Add(go);
            return go;
        }

        IEnumerator Settle(GameObject go)
        {
            var t = go.transform;
            var target = t.localScale;
            t.localScale = target * 0.01f;
            float k = 0f;
            while (k < 1f)
            {
                k += Time.deltaTime / 0.35f;
                t.localScale = Vector3.Lerp(target * 0.01f, target, Mathf.SmoothStep(0f, 1f, k));
                yield return null;
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
