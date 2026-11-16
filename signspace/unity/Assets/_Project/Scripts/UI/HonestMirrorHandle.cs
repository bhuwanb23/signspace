using UnityEngine;
using Signspace.Matching;

namespace Signspace.UI
{
    /// <summary>
    /// The physical object that opens the honest mirror (P3.4).
    ///
    /// The phase is explicit that the mirror must be reached from inside the space, not from a
    /// settings menu: a panel you have to go looking for is an apology, and a panel you pick up is a
    /// part of the room. This is deliberately the smallest possible binding - a pinch on the object
    /// toggles the panel - so it can be replaced by the P4 shelf without touching HonestMirror.
    /// </summary>
    [RequireComponent(typeof(Grabbable))]
    public class HonestMirrorHandle : MonoBehaviour
    {
        public HonestMirror mirror;

        Grabbable _grabbable;

        void Awake()
        {
            _grabbable = GetComponent<Grabbable>();
            _grabbable.OnGrabbed += OnGrabbed;
        }

        void OnDestroy()
        {
            if (_grabbable != null) _grabbable.OnGrabbed -= OnGrabbed;
        }

        void OnGrabbed(MetaHandSource hand)
        {
            if (mirror != null) mirror.Toggle();
        }
    }
}
