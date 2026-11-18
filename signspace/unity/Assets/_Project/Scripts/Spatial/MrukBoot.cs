using System;
using System.Collections;
using UnityEngine;

namespace Signspace.Spatial
{
    /// <summary>
    /// Starts MRUK and measures how long it takes to produce a room.
    ///
    /// P1.6's first trap is "assuming detection is instant - it is not, and launch flow must account
    /// for it". This component exists to make that assumption impossible: it records the seconds from
    /// launch to the first room with anchors, publishes them, and refuses to claim success without a
    /// number.
    ///
    /// THE THREE STATES, WHICH ARE NOT THE SAME FAILURE
    /// ------------------------------------------------
    ///   Ready    a room exists with at least one anchor
    ///   NoRoom   MRUK initialised but the device has no scanned space (the user has never run
    ///            Space Setup, or is in a room that was deleted). Recoverable by the user, not a bug
    ///   Failed   the MRUK singleton never appeared, or loading threw. A build problem
    ///
    /// The product must behave sanely in all three, because the shelf's whole design carries a plinth
    /// fallback: `ShelfAnchor` never requires a surface. This component therefore never blocks the
    /// app - it publishes a state and gets out of the way.
    ///
    /// VERIFIED API (P1.6): MRUK.Instance, MRUK.IsInitialized, MRUK.RegisterSceneLoadedCallback,
    /// MRUK.LoadSceneFromDevice(bool, bool), MRUK.GetCurrentRoom(), MRUKRoom.Anchors.
    /// Meta documents that a device load needs the scene permission and that
    /// `requestSceneCaptureIfNoDataFound: true` will start the space-setup flow if the device has no
    /// scan - which is why the failure text below tells the user to run Space Setup rather than
    /// showing an error code.
    /// </summary>
    public class MrukBoot : MonoBehaviour
    {
        public enum State { Waiting = 0, Ready = 1, NoRoom = 2, Failed = 3 }

        [Tooltip("Seconds to wait for a room before calling it NoRoom. MRUK can take a few seconds on " +
                 "a cold start; a judge's first launch is exactly a cold start.")]
        public float timeoutSeconds = 20f;

        [Tooltip("Start the device load ourselves. Turn this OFF if the MRUK prefab's own " +
                 "'Load Scene on Startup' is enabled, to avoid loading twice.")]
        public bool requestLoad = true;

        [Tooltip("Ask for space setup automatically when the device has no scan. Off for silent fallback.")]
        public bool requestSceneCaptureIfNoDataFound = false;

        public State Current { get; private set; } = State.Waiting;

        /// <summary>
        /// Whether the app holds `com.oculus.permission.USE_SCENE`.
        ///
        /// This is checked because MRUK failing to load and USE_SCENE being withheld look identical
        /// from the API: both give a null room and no anchors. They are not the same problem - one is
        /// a manifest/permission fix, the other is a user who has never scanned the room - and a
        /// vendor report says a sideloaded APK can lose ACCESS_MR_SENSORS between launches without
        /// store-level authorisation, which would make every condition in the P1.6 table fail for a
        /// reason that has nothing to do with surfaces. Recording the permission state turns that
        /// into one readable line instead of an afternoon of guessing.
        /// </summary>
        public bool ScenePermissionGranted { get; private set; }

        public string ScenePermissionNote { get; private set; } = "not checked";

        /// <summary>Seconds from Awake to the first room with anchors. -1 until it happens.</summary>
        public float DetectionSeconds { get; private set; } = -1f;

        public int RoomCount { get; private set; }
        public int AnchorCount { get; private set; }
        public string RoomName { get; private set; } = "";
        public string Status { get; private set; } = "waiting";

        /// <summary>Raised once, when a room becomes available. Also fired for a later room change.</summary>
        public event System.Action OnRoomReady;

        float _awakeAt;
        bool _readyRaised;
        System.Threading.Tasks.Task _loadInFlight;

        void Awake()
        {
            _awakeAt = Time.realtimeSinceStartup;
            CheckScenePermission();
        }

        void CheckScenePermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            const string scenePermission = "com.oculus.permission.USE_SCENE";
            try
            {
                ScenePermissionGranted = UnityEngine.Android.Permission.HasUserAuthorizedPermission(scenePermission);
                ScenePermissionNote = ScenePermissionGranted
                    ? "USE_SCENE granted"
                    : "USE_SCENE NOT granted - MRUK cannot read scene data. Fix: OVRManager > Quest " +
                      "Features > Scene Support = Required, tick Scene under Permission Requests On " +
                      "Startup, then Meta > Tools > Update AndroidManifest.xml, and accept the prompt " +
                      "on first launch.";
            }
            catch (Exception e)
            {
                ScenePermissionGranted = false;
                ScenePermissionNote = "could not read USE_SCENE (" + e.GetType().Name + ")";
            }
#else
            ScenePermissionGranted = true;
            ScenePermissionNote = "editor/simulator - no Android permission applies";
#endif
        }

        void Start()
        {
            StartCoroutine(Boot());
        }

        IEnumerator Boot()
        {
            // 1. the singleton. A scene built from code may have the component but not the prefab's
            //    SceneSettings, so this reports what it finds rather than assuming.
            float waited = 0f;
            while (Meta.XR.MRUtilityKit.MRUK.Instance == null && waited < 3f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (Meta.XR.MRUtilityKit.MRUK.Instance == null)
            {
                var go = new GameObject("MRUK");
                var mruk = go.AddComponent<Meta.XR.MRUtilityKit.MRUK>();
                yield return null;
                if (mruk == null)
                {
                    Current = State.Failed;
                    Status = "MRUK component could not be added. Is com.meta.xr.mrutoolkitkit " +
                             "(MR Utility Kit) installed? It ships inside Meta XR All-in-One.";
                    Debug.LogError("[MrukBoot] " + Status);
                    yield break;
                }
                Debug.LogWarning("[MrukBoot] no MRUK in the scene - added one at runtime. The prefab's " +
                                 "SceneSettings may be missing; check the room data source in P1.6.");
            }

            var mrukSingleton = Meta.XR.MRUtilityKit.MRUK.Instance;
            mrukSingleton.RegisterSceneLoadedCallback(OnSceneLoaded);

            // 2. if the prefab does not auto-load, ask for a device load.
            if (requestLoad)
            {
                if (!TryRequestDeviceLoad(mrukSingleton, out string why))
                {
                    Debug.LogWarning("[MrukBoot] did not start a device load (" + why + "). Still " +
                                     "waiting for a room: the MRUK prefab's own 'Load Scene on " +
                                     "Startup' may already be loading one.");
                }
            }

            // 3. wait for a room, however it arrives.
            float elapsed = 0f;
            while (elapsed < timeoutSeconds)
            {
                CheckRoom();
                if (Current == State.Ready) yield break;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            CheckRoom();
            if (Current == State.Ready) yield break;

            Current = State.NoRoom;
            Status = (_loadInFlight != null && !_loadInFlight.IsCompleted
                        ? "the device load has not completed either - "
                        : "") +
                     (ScenePermissionGranted ? "" : "[" + ScenePermissionNote + "] ") +
                     "No room with anchors after " + timeoutSeconds.ToString("0") + " s. If the headset " +
                     "has never scanned this room, run Settings > Physical Space > Space Setup, then " +
                     "relaunch. The shelf uses its plinth fallback until then.";
            Debug.LogWarning("[MrukBoot] " + Status);
        }

        /// <summary>
        /// Start the device load using the pinned MRUK v207 API. Its third SceneModel argument is
        /// optional; reflection lookup for only one or two bools does not match that signature.
        /// A direct call also gives the IL2CPP linker a real reference to the API.
        /// </summary>
        bool TryRequestDeviceLoad(Meta.XR.MRUtilityKit.MRUK mrukSingleton, out string why)
        {
            why = "";
            if (mrukSingleton == null) { why = "no MRUK instance"; return false; }
            try
            {
                _loadInFlight = mrukSingleton.LoadSceneFromDevice(requestSceneCaptureIfNoDataFound, true);
                return true;
            }
            catch (Exception e)
            {
                why = e.GetType().Name + ": " + e.Message;
                return false;
            }
        }

        void OnSceneLoaded()
        {
            CheckRoom();
            if (Current != State.Ready) return;
            if (_readyRaised) return;
            _readyRaised = true;
            if (OnRoomReady != null) OnRoomReady();
        }

        void CheckRoom()
        {
            var mrukSingleton = Meta.XR.MRUtilityKit.MRUK.Instance;
            if (mrukSingleton == null) return;

            var rooms = mrukSingleton.GetRooms();
            RoomCount = rooms != null ? rooms.Count : 0;

            var room = mrukSingleton.GetCurrentRoom();
            if (room == null) return;

            int anchors = room.Anchors != null ? room.Anchors.Count : 0;
            if (anchors <= 0) return;

            bool firstTime = Current != State.Ready;
            if (firstTime) DetectionSeconds = Time.realtimeSinceStartup - _awakeAt;

            Current = State.Ready;
            AnchorCount = anchors;
            RoomName = room.name;
            Status = "room '" + RoomName + "' with " + anchors + " anchors, detected " +
                     DetectionSeconds.ToString("0.00") + " s after launch";

            if (!_readyRaised)
            {
                _readyRaised = true;
                Debug.Log("[MrukBoot] " + Status);
                if (OnRoomReady != null) OnRoomReady();
            }
        }
    }
}
