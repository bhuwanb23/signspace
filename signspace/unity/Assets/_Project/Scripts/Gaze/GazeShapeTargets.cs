using UnityEngine;
using Signspace.Hands;
using Signspace.Matching;
using Signspace.Spatial;

namespace Signspace.Gaze
{
    /// <summary>
    /// Makes the shelf's silhouettes selectable by gaze (P6.3 step 1: "select a shape").
    ///
    /// The shelf spawns one object per unlocked shape and can add another when a lock banks a new one,
    /// so this rescans rather than wiring twelve references at build time. The scan is a 1 Hz comparison
    /// of the view count, not a per-frame search: twelve objects, one integer compare per frame, and
    /// none of it is on the lock path.
    ///
    /// WHY SELECTION LIVES HERE AND NOT ON THE SHELF
    /// ---------------------------------------------
    /// The shelf's job is to hold objects; it does not own the matcher. Putting the target selection on
    /// the gaze target means the same object can also be pinched (the shelf already handles that), and
    /// nothing about the shelf's API has to change for accessibility.
    /// </summary>
    public class GazeShapeTargets : MonoBehaviour
    {
        public Matcher matcher;
        public ReferenceHand referenceHand;

        [Tooltip("Seconds between rescans. 1 s is invisible to a user and costs nothing.")]
        public float rescanSeconds = 1f;

        public int TargetCount { get; private set; }

        float _nextScan;

        void Start()
        {
            if (matcher == null) matcher = FindFirstObjectByType<Matcher>();
            if (referenceHand == null) referenceHand = FindFirstObjectByType<ReferenceHand>();
            Scan();
        }

        void Update()
        {
            if (Time.time < _nextScan) return;
            _nextScan = Time.time + Mathf.Max(0.25f, rescanSeconds);
            Scan();
        }

        public int Scan()
        {
            var views = FindObjectsByType<ShelfObjectView>(FindObjectsSortMode.None);
            int count = 0;

            for (int i = 0; i < views.Length; i++)
            {
                var view = views[i];
                if (view == null || string.IsNullOrEmpty(view.shapeId)) continue;

                var action = view.GetComponent<DwellAction>();
                if (action == null) action = view.gameObject.AddComponent<DwellAction>();
                action.kind = DwellActionKind.SelectShape;
                action.shapeId = view.shapeId;
                action.matcher = matcher;
                action.referenceHand = referenceHand;
                count++;
            }

            TargetCount = count;
            return count;
        }
    }
}
