using UnityEngine;

namespace Signspace.Gaze
{
    /// <summary>
    /// Marks something gazeable. Attach to every finger of the reference hand.
    ///
    /// Group exists because at arm's length two adjacent fingers are ~15 mm apart, which may be
    /// smaller than a dwell can resolve (GAZE-INPUT.md §7). If P6.1 shows single fingers are
    /// unreliable, set Group for each pair and the highlighter will highlight the group instead
    /// of failing to pick a finger.
    /// </summary>
    public class FingerTarget : MonoBehaviour
    {
        [Tooltip("0 = thumb, 1 = index, 2 = middle, 3 = ring, 4 = little")]
        public int fingerIndex;
        [Tooltip("Shown when this finger is focused, e.g. 'index extended, thumb tucked'.")]
        public string label;
        [Tooltip("Optional coarser target: fingers sharing a group highlight together.")]
        public int group = -1;

        public bool UsesGroup => group >= 0;
    }
}
