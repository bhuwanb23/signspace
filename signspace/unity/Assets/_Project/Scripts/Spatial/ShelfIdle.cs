using System.Collections.Generic;
using UnityEngine;

namespace Signspace.Spatial
{
    /// <summary>
    /// One owner for the shelf's breathing (P4.2 step 3).
    ///
    /// The trap in the task is twelve objects each animating themselves. One Update over twelve transform
    /// writes is microseconds; twelve Updates doing the same thing is the same microseconds plus twelve
    /// calls that will eventually grow a `Debug.Log` or a material fetch. So there is exactly one Update, it
    /// writes one Vector3 per object, and it allocates nothing.
    ///
    /// **It can be turned off in one place.** If P7's frame budget is tight, `breathing = false` and the
    /// objects stop - which is a decision someone makes, not a bug someone debugs. The task's rule is
    /// "<1% CPU or drop the idle"; the switch is how the second option stays available.
    ///
    /// What it costs, exactly: one `Mathf.Sin`, one multiply and one transform assignment per object per
    /// frame, plus one float comparison when it is off. At twelve objects that is ~36 float operations a
    /// frame. It is not measured on device yet; `phases/p4-shelf/budget-check.md` records that honestly.
    /// </summary>
    public class ShelfIdle : MonoBehaviour
    {
        /// <summary>Set by Awake. Objects register themselves; nothing searches the scene per frame.</summary>
        public static ShelfIdle Instance { get; private set; }

        [Tooltip("Off = the objects are still. The one switch for the whole shelf.")]
        public bool breathing = true;

        [Tooltip("Breaths per second. 0.18 is one breath every 5.5 s: alive, not fidgeting.")]
        public float breathHz = 0.18f;

        [Tooltip("Breath amplitude as a fraction of the object's size. 0.006 on a 16 cm object is ~1 mm.")]
        public float amount = 0.006f;

        [Tooltip("Phase offset per slot, in turns, so the shelf does not pulse in unison.")]
        public float phasePerObject = 0.17f;

        readonly List<Transform> _targets = new List<Transform>(16);
        readonly List<Vector3> _rest = new List<Vector3>(16);
        float _clock;

        /// <summary>How many objects breathe. Dev readout; also the number the budget is argued from.</summary>
        public int Count => _targets.Count;
        public bool IsOn => breathing;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[ShelfIdle] a second ShelfIdle in the scene; keeping the first and " +
                                 "disabling this one so nothing breathes twice.");
                enabled = false;
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Register(Transform t)
        {
            if (t == null || _targets.Contains(t)) return;
            _targets.Add(t);
            _rest.Add(t.localScale);
        }

        public void Unregister(Transform t)
        {
            int i = t == null ? -1 : _targets.IndexOf(t);
            if (i < 0) return;
            _targets.RemoveAt(i);
            _rest.RemoveAt(i);
        }

        void Update()
        {
            if (!breathing || _targets.Count == 0) return;

            _clock += Time.deltaTime;
            float basePhase = _clock * breathHz * Mathf.PI * 2f;

            for (int i = _targets.Count - 1; i >= 0; i--)
            {
                Transform t = _targets[i];
                if (t == null)          // destroyed without telling us: drop it rather than null-check forever
                {
                    _targets.RemoveAt(i);
                    _rest.RemoveAt(i);
                    continue;
                }

                float k = 1f + Mathf.Sin(basePhase + i * phasePerObject * Mathf.PI * 2f) * amount;
                t.localScale = _rest[i] * k;
            }
        }

        /// <summary>Re-baseline the resting scales after something else resized an object (the settle).</summary>
        public void Rebaseline(Transform t)
        {
            int i = t == null ? -1 : _targets.IndexOf(t);
            if (i >= 0) _rest[i] = t.localScale;
        }
    }
}
