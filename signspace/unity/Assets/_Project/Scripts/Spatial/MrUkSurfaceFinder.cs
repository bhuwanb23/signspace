using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Signspace.Spatial
{
    /// <summary>A horizontal surface the shelf could live on.</summary>
    public struct SurfaceCandidate
    {
        public string AnchorId;     // the identity of the surface, used for persistence
        public string AnchorUuid;   // the system anchor's UUID: stable across sessions (see P1.7)
        public string RoomId;
        public Vector3 Position;    // a point on the top surface
        public Vector3 Normal;      // surface up
        public Vector2 Extent;      // metres, horizontal
        public float Height;        // metres above the room's floor, if the floor is known
        public float Score;         // higher is better
        public string Label;        // MRUK's semantic label, for the log
        public bool FromDevice;     // true when a system anchor backs it (device), false for prefab/JSON rooms
    }

    /// <summary>
    /// The shelf must never depend on MRUK directly. This interface is the seam: the product logic
    /// works against it, and the MRUK implementation is the only file that touches the SDK.
    ///
    /// Same reasoning as Matcher/PoseMath: an SDK upgrade should cost us one file, not the product.
    /// </summary>
    public interface ISurfaceFinder
    {
        bool TryGetPrimary(out SurfaceCandidate surface);

        /// <summary>
        /// Find a surface again by the identity that was written to disk. This is the whole of P1.7:
        /// restoring a shelf means resolving an ANCHOR, not a position, so the same logical surface
        /// comes back at whatever place the room happens to be in this session.
        ///
        /// `anchorTransform` is returned because the shelf must be parented to it: a child of the
        /// anchor follows every tracking-space correction the runtime makes, while an object placed
        /// at a world position does not.
        /// </summary>
        bool TryResolve(string anchorId, out SurfaceCandidate surface, out Transform anchorTransform);

        string RoomId { get; }
        bool TryGetCurrentRoomId(out string roomId);
    }

    /// <summary>
    /// MRUK-backed finder.
    ///
    /// VERIFIED API (P1.6, 15 Nov 2026) — every member used here was checked against Meta's MRUK
    /// reference before being trusted, because the first draft of this file was written with four
    /// unverified guesses:
    ///
    ///   MRUK.Instance · GetCurrentRoom() · MRUKRoom.Anchors · MRUKRoom.GetFloorAnchor()
    ///   MRUKAnchor.Label (SceneLabels) · HasAnyLabel(SceneLabels) · PlaneRect (Rect?) ·
    ///   VolumeBounds (Bounds?) · GetAnchorCenter() · GetClosestSurfacePosition(pos, out, out)
    ///   HasValidHandle

    /// THREE FIXES, two of which would each have produced a false "MRUK does not work" result.

    ///   1. VOLUMES WERE IGNORED. The draft only accepted anchors with a PlaneRect, so every TABLE
    ///      and COUCH - which MRUK reports as volumes with VolumeBounds - was skipped. The v207
    ///      anchor reference says it outright: "Example of anchors with a volume are a table or a
    ///      couch." On a table it found nothing and the phase would have read as a platform failure.
    ///
    ///   2. PREFAB ROOMS WERE REJECTED. The draft required `anchor.HasValidHandle`. The same
    ///      reference: "An anchor will have a valid handle if it was loaded from device... Anchors
    ///      loaded from JSON or Prefabs do not have a valid handle." So requiring a handle makes the
    ///      Meta XR Simulator's sample rooms - the only way to test any of this before the headset
    ///      arrives - report zero surfaces. The handle is now informational (`FromDevice`), not a
    ///      filter.
    ///
    ///   3. GEOMETRY ASSUMED WORLD-ALIGNED AXES. Reading `bounds.center + up * size.y / 2` is only
    ///      right when the anchor's local +Y is up, which nothing promises. The surface point now
    ///      comes from `GetClosestSurfacePosition`, which returns a point and its normal regardless
    ///      of how the anchor is rotated.
    ///
    /// </summary>
    public class MrUkSurfaceFinder : MonoBehaviour, ISurfaceFinder
    {
        [Tooltip("Ignore surfaces nearer than this. A shelf under the user's chin is not reachable " +
                 "with a hand without looking straight down, and it is not what \"in front of you\" means.")]
        public float minDistance = 0.30f;

        [Tooltip("Ignore surfaces further than this from the user.")]
        public float maxDistance = 1.0f;

        // P4.1. "Within 60 degrees of forward" is measured on the YAW, not on the 3D direction: a
        // waist-high surface 0.4 m away is ~40 degrees BELOW the eye line, so a 3D cone would reject the
        // most natural shelf in the room for being too low. The question is whether it is in front of
        // you, and that question is horizontal.
        [Tooltip("Surfaces more than this angle off the user's forward direction (yaw) are ignored.")]
        public float forwardConeDegrees = 60f;

        [Tooltip("Preferred surface height, in metres above the room's floor.")]
        public float preferredHeight = 0.75f;

        [Tooltip("Minimum free area for a 0.45 m shelf.")]
        public Vector2 minExtent = new Vector2(0.45f, 0.20f);

        [Tooltip("Free area that scores full marks. Bigger is better up to this, and no better after: " +
                 "a 3 m dining table is not a better shelf than a 0.6 m side table, it is just closer " +
                 "to the wall.")]
        public Vector2 comfortableExtent = new Vector2(0.75f, 0.40f);

        [Tooltip("Height tolerance when scoring, in metres.")]
        public float heightTolerance = 0.35f;

        [Tooltip("Labels a shelf may sit on. TABLE and STORAGE are the intended ones; COUCH and BED " +
                 "are accepted because a lap is a legitimate place to hold a shelf.")]
        public Meta.XR.MRUtilityKit.MRUKAnchor.SceneLabels acceptedLabels =
            Meta.XR.MRUtilityKit.MRUKAnchor.SceneLabels.TABLE |
            Meta.XR.MRUtilityKit.MRUKAnchor.SceneLabels.STORAGE |
            Meta.XR.MRUtilityKit.MRUKAnchor.SceneLabels.COUCH |
            Meta.XR.MRUtilityKit.MRUKAnchor.SceneLabels.BED |
            Meta.XR.MRUtilityKit.MRUKAnchor.SceneLabels.OTHER;

        [Tooltip("The floor is NOT a shelf. Off by default and it should stay off: a shelf at floor " +
                 "level is unusable, so accepting the floor would turn every failure into a bad pass.")]
        public bool acceptFloor = false;

        [Tooltip("Optional eye transform. Falls back to Camera.main, and then to this transform, " +
                 "because a batch-built scene has no tagged MainCamera.")]
        public Transform headOverride;

        public string RoomId { get; private set; } = "";

        /// <summary>
        /// Why the last query rejected things it rejected, in plain language. This exists because
        /// "MRUK found nothing" is not a diagnosis: three anchors too small and one anchor too low is
        /// a completely different problem from no room at all, and only one of them is MRUK's fault.
        /// </summary>
        public string LastRejectionSummary { get; private set; } = "not queried";

        public int LastAnchorCount { get; private set; }

        Transform Head
        {
            get
            {
                if (headOverride != null) return headOverride;
                if (Camera.main != null) return Camera.main.transform;
                return transform;
            }
        }

        public bool TryGetPrimary(out SurfaceCandidate best)
        {
            var all = new List<SurfaceCandidate>(16);
            bool found = Collect(all, out best);
            return found;
        }

        /// <summary>Every acceptable surface, best first. The P1.6 probe logs this whole list.</summary>
        public List<SurfaceCandidate> GetAll()
        {
            var all = new List<SurfaceCandidate>(16);
            SurfaceCandidate ignored;
            Collect(all, out ignored);
            all.Sort((a, b) => b.Score.CompareTo(a.Score));
            return all;
        }

        /// <summary>Resolve the stable surface UUID in the current room only.</summary>
        public bool TryResolve(string anchorId, out SurfaceCandidate surface, out Transform anchorTransform)
        {
            surface = default;
            anchorTransform = null;
            if (string.IsNullOrEmpty(anchorId)) return false;

            var mrukSingleton = Meta.XR.MRUtilityKit.MRUK.Instance;
            if (mrukSingleton == null) return false;

            // Only the *current* room may restore. Other loaded rooms must not teleport the shelf.
            var room = mrukSingleton.GetCurrentRoom();
            if (room == null || room.Anchors == null) return false;
            var anchors = room.Anchors;

            foreach (var anchor in anchors)
            {
                if (anchor == null) continue;

                string uuid = AnchorUuidOf(anchor);
                bool uuidMatch = !string.IsNullOrEmpty(uuid) &&
                    string.Equals(uuid, anchorId, StringComparison.OrdinalIgnoreCase);
                if (!uuidMatch) continue;

                Vector3 pos, up;
                Vector2 extent;
                if (!TryGeometry(anchor, Head.position, out pos, out up, out extent)) continue;

                string roomId = "";
                try { if (anchor.Room != null) roomId = ResolveRoomId(anchor.Room); } catch { }

                surface = new SurfaceCandidate
                {
                    AnchorId = uuid,
                    AnchorUuid = uuid,
                    RoomId = roomId,
                    Position = pos,
                    Normal = up,
                    Extent = extent,
                    Height = HeightAboveFloor(anchor, pos),
                    Score = 100f,     // a uuid match beats a name match, always
                    Label = anchor.Label.ToString(),
                    FromDevice = anchor.HasValidHandle
                };
                anchorTransform = anchor.transform;
                return true;
            }
            return false;
        }

        /// <summary>
        /// The system anchor's UUID. Meta's own reference: "The unique identifier of this anchor. UUIDs
        /// persist across sessions." That sentence is the entire justification for saving this string
        /// instead of a world position.
        /// </summary>
        static string AnchorUuidOf(Meta.XR.MRUtilityKit.MRUKAnchor anchor)
        {
            Guid uuid = anchor.Anchor.Uuid;
            return uuid == Guid.Empty ? "" : uuid.ToString("D");
        }

        /// <summary>Surface point, normal and horizontal extent for a plane or a volume anchor.</summary>
        /// <summary>
        /// The surface point, its up, and its footprint - with the point chosen as the one NEAREST THE
        /// USER, not the one nearest the anchor's centre.
        ///
        /// That distinction is the whole of P4.1's second correction. The old query asked the SDK for the
        /// point closest to a spot 0.8 m above the anchor's centre, which for a big table is directly
        /// under the middle of it: a 1.4 m dining table whose near edge is 0.4 m from the user then failed
        /// a 1.0 m reach limit, and had it passed, the shelf would have been placed where nobody can
        /// reach. Asking for the point nearest the user makes `reach` mean what it says, puts the shelf at
        /// the edge of the surface (which §3.2 already claimed), and lets the SDK do the rect math in the
        /// anchor's own frame - so an anchor rotated 90 degrees is handled by the SDK, not by an
        /// axis-aligned box of our own invention.
        /// </summary>
        static bool TryGeometry(Meta.XR.MRUtilityKit.MRUKAnchor anchor, Vector3 towardPoint,
                                out Vector3 pos, out Vector3 up, out Vector2 extent)
        {
            pos = Vector3.zero; up = Vector3.zero; extent = Vector2.zero;
            if (!anchor.PlaneRect.HasValue && !anchor.VolumeBounds.HasValue) return false;

            // The surface point comes from the SDK rather than from our own math on purpose: an
            // anchor's local axes are not promised to be world-aligned, so `bounds.center +
            // up * size.y / 2` is only correct when local +Y is up. The query is lifted to the
            // anchor's own height because GetClosestSurfacePosition projects along the surface normal;
            // asking from the user's feet would give the same answer only by luck.
            Vector3 query = new Vector3(towardPoint.x, anchor.GetAnchorCenter().y + 0.8f, towardPoint.z);
            anchor.GetClosestSurfacePosition(query, out pos, out up);

            if (anchor.VolumeBounds.HasValue)
            {
                var b = anchor.VolumeBounds.Value;
                extent = new Vector2(Mathf.Max(b.size.x, b.size.z), Mathf.Min(b.size.x, b.size.z));
            }
            else
            {
                var r = anchor.PlaneRect.Value;
                extent = new Vector2(Mathf.Max(r.width, r.height), Mathf.Min(r.width, r.height));
            }
            return true;
        }

        /// <summary>Height of a surface above its room's floor, or above y=0 if there is no floor.</summary>
        static float HeightAboveFloor(Meta.XR.MRUtilityKit.MRUKAnchor anchor, Vector3 surfacePoint)
        {
            try
            {
                var room = anchor.Room;
                var floor = room != null ? room.GetFloorAnchor() : null;
                if (floor != null) return surfacePoint.y - floor.transform.position.y;
            }
            catch { }
            return surfacePoint.y;
        }

        bool Collect(List<SurfaceCandidate> into, out SurfaceCandidate best)
        {
            best = default;
            var rejects = new StringBuilder();
            int horizontal = 0, small = 0, near = 0, far = 0, behind = 0, lowOrHigh = 0, wrongLabel = 0, usable = 0;

            var room = Meta.XR.MRUtilityKit.MRUK.Instance != null
                ? Meta.XR.MRUtilityKit.MRUK.Instance.GetCurrentRoom()
                : null;

            if (room == null)
            {
                LastAnchorCount = 0;
                LastRejectionSummary = "no room loaded yet (MRUK.GetCurrentRoom() returned null)";
                return false;
            }

            RoomId = ResolveRoomId(room);
            var head = Head;
            var anchors = room.Anchors;
            LastAnchorCount = anchors != null ? anchors.Count : 0;

            if (anchors == null || anchors.Count == 0)
            {
                LastRejectionSummary = "room '" + RoomId + "' has no anchors - the space has not been " +
                                       "set up on this device, or the prefab room has no geometry";
                return false;
            }

            float bestScore = float.NegativeInfinity;
            bool found = false;

            foreach (var anchor in anchors)
            {
                if (anchor == null) continue;

                // ---- geometry: planes AND volumes ------------------------------------------
                // Meta's own words: "Example of anchors with a volume are a table or a couch." A
                // TABLE is the most likely shelf host, so a finder that only reads PlaneRect is a
                // finder that misses the target case. Geometry lives in TryGeometry because P1.7's
                // re-resolution path has to describe an anchor the same way this loop does.
                Vector3 pos, up;
                Vector2 extent;
                if (!TryGeometry(anchor, head.position, out pos, out up, out extent)) continue;

                // ---- horizontal? -----------------------------------------------------------
                if (Vector3.Dot(up, Vector3.up) < 0.9f) continue;
                horizontal++;

                // ---- label -----------------------------------------------------------------
                string label = anchor.Label.ToString();
                bool floorLabel = anchor.HasAnyLabel(Meta.XR.MRUtilityKit.MRUKAnchor.SceneLabels.FLOOR);
                bool allowed = anchor.HasAnyLabel(acceptedLabels) || (acceptFloor && floorLabel);
                if (!allowed)
                {
                    wrongLabel++;
                    continue;
                }

                // ---- size and reach --------------------------------------------------------
                if (extent.x < minExtent.x || extent.y < minExtent.y) { small++; continue; }

                // ---- reach, measured HORIZONTALLY -------------------------------------------
                // This used to be the 3D distance, which quietly rejected the most natural shelf in
                // the room: a waist-high table 0.6 m in front of a standing user is 1.0 m away in 3D
                // (0.8 m of vertical drop), so it failed a 1.0 m limit meant to describe reach. The
                // question is "can I put my hand on it without leaning", and that is a horizontal
                // question - the same correction as the forward cone.
                float dx = pos.x - head.position.x, dz = pos.z - head.position.z;
                float reach = Mathf.Sqrt(dx * dx + dz * dz);
                if (reach > maxDistance) { far++; continue; }
                if (reach < minDistance) { near++; continue; }

                // ---- in front? (yaw only - see forwardConeDegrees) ---------------------------
                var headForward = head.forward; headForward.y = 0f;
                var toSurfaceFlat = pos - head.position; toSurfaceFlat.y = 0f;
                if (headForward.sqrMagnitude > 0.0001f && toSurfaceFlat.sqrMagnitude > 0.0001f)
                {
                    float yawDegrees = Vector3.Angle(headForward, toSurfaceFlat);
                    if (yawDegrees > forwardConeDegrees) { behind++; continue; }
                }

                // ---- height, relative to the floor when we know it -------------------------
                float height = HeightAboveFloor(anchor, pos);

                float heightError = Mathf.Abs(height - preferredHeight);
                if (heightError > heightTolerance) { lowOrHigh++; continue; }

                // ---- score -----------------------------------------------------------------
                var toSurface = (pos - head.position).normalized;
                float facing = Vector3.Dot(head.forward, toSurface);
                float labelBonus = anchor.HasAnyLabel(
                    Meta.XR.MRUtilityKit.MRUKAnchor.SceneLabels.TABLE |
                    Meta.XR.MRUtilityKit.MRUKAnchor.SceneLabels.STORAGE) ? 1.0f : 0.4f;

                // Free area, capped at "comfortable": the shelf is 0.45 m, and anything past
                // comfortableExtent is no more usable, only more furniture.
                float areaX = Mathf.Clamp01(extent.x / Mathf.Max(0.01f, comfortableExtent.x));
                float areaY = Mathf.Clamp01(extent.y / Mathf.Max(0.01f, comfortableExtent.y));
                float areaBonus = (areaX + areaY) * 0.5f;

                // Four terms, and the weights say what the product wants: waist height first (2.0),
                // enough room for the shelf next (1.0), close enough to touch (1.0), a prop that is
                // meant to be a surface rather than a floor (up to 1.0), and facing as a tie-break (0.5).
                float score = (1f - heightError / Mathf.Max(0.01f, heightTolerance)) * 2f
                            + areaBonus
                            + Mathf.Clamp01(1f - reach / maxDistance)
                            + Mathf.Clamp01(facing) * 0.5f
                            + labelBonus;

                string uuid = AnchorUuidOf(anchor);
                var candidate = new SurfaceCandidate
                {
                    // the uuid when the platform gives us one, because it is the only identity that
                    // is promised to survive a relaunch; the object name is the fallback
                    AnchorId = string.IsNullOrEmpty(uuid) ? anchor.name : uuid,
                    AnchorUuid = uuid,
                    RoomId = RoomId,
                    Position = pos,
                    Normal = up,
                    Extent = extent,
                    Height = height,
                    Score = score,
                    Label = label,
                    FromDevice = anchor.HasValidHandle
                };

                into.Add(candidate);
                usable++;

                if (score <= bestScore) continue;
                bestScore = score;
                best = candidate;
                found = true;
            }

            LastRejectionSummary = "anchors " + LastAnchorCount +
                " · horizontal " + horizontal +
                " · wrong label " + wrongLabel +
                " · too small " + small +
                " · too close " + near +
                " · too far " + far +
                " · behind " + behind +
                " · height out of band " + lowOrHigh +
                " · usable " + usable;

            if (!found && usable == 0 && horizontal == 0)
            {
                LastRejectionSummary += "  <- nothing horizontal at all: no scanned tabletop, bed or " +
                                        "couch in this room";
            }

            return found;
        }

        /// <summary>
        /// Current room UUID; a missing UUID is unknown, never a Unity name presented as stable.
        /// MRUKRoom.Anchor and OVRAnchor.Uuid are documented in the v207 Unity API.
        /// </summary>
        public bool TryGetCurrentRoomId(out string roomId)
        {
            roomId = "";
            var mruk = Meta.XR.MRUtilityKit.MRUK.Instance;
            var room = mruk != null ? mruk.GetCurrentRoom() : null;
            if (room == null) { RoomId = ""; return false; }
            roomId = ResolveRoomId(room);
            RoomId = roomId;
            return !string.IsNullOrEmpty(roomId);
        }

        static string ResolveRoomId(Meta.XR.MRUtilityKit.MRUKRoom room)
        {
            Guid uuid = room.Anchor.Uuid;
            return uuid == Guid.Empty ? "" : uuid.ToString("D");
        }

    }
}
