using System.Collections;
using UnityEngine;
using Signspace.Matching;
using Signspace.Spatial;

namespace Signspace.Core
{
    /// <summary>
    /// The first reward, and it happens inside 75 seconds.
    ///
    /// On a lock the reference hand dissolves and re-forms as an object that arcs to the shelf
    /// (P2.5, FIRST-FIVE-MINUTES.md). Whole sequence under 1.2 s, because from the third repetition
    /// onwards anything longer is skipped — and a reward that gets skipped is not a reward.
    ///
    /// Deliberately no particle effects and no audio clip here: the sound is handled by the sound map,
    /// and the payoff must read as a *transformation* rather than an effect.
    /// </summary>
    public class LockPayoff : MonoBehaviour
    {
        [Header("Dependencies")]
        public Matcher matcher;
        public SaveSystem save;
        public ShelfAnchor shelf;
        public Transform referenceHand;
        public Transform flyFrom;

        [Header("Timing")]
        public float dissolveSeconds = 0.18f;
        public float arcSeconds = 0.45f;
        public float arcHeight = 0.12f;

        [Header("View")]
        public GameObject referenceHandVisual;

        bool _playing;

        void OnEnable()
        {
            if (matcher != null) matcher.OnLocked += OnLocked;
        }

        void OnDisable()
        {
            if (matcher != null) matcher.OnLocked -= OnLocked;
        }

        void OnLocked(string shapeId)
        {
            if (_playing) return;
            StartCoroutine(Sequence(shapeId));
        }

        IEnumerator Sequence(string shapeId)
        {
            _playing = true;

            // 1. the reference hand dissolves
            if (referenceHandVisual != null)
            {
                var t = referenceHandVisual.transform;
                var start = t.localScale;
                float k = 0f;
                while (k < 1f)
                {
                    k += Time.deltaTime / Mathf.Max(0.01f, dissolveSeconds);
                    t.localScale = Vector3.Lerp(start, start * 0.05f, k);
                    yield return null;
                }
                referenceHandVisual.SetActive(false);
                t.localScale = start;
            }

            // 2. an object is born where the hand was, and arcs to the shelf
            int slot = save != null ? save.Data.unlocked.Count : 0;
            var obj = shelf != null ? shelf.AddObject(shapeId, slot) : null;

            if (obj != null && flyFrom != null)
            {
                var tr = obj.transform;
                var slotLocal = tr.localPosition;          // where the shelf put it
                var from = flyFrom.position;
                var to = shelf.transform.TransformPoint(slotLocal);
                float k = 0f;
                while (k < 1f)
                {
                    k += Time.deltaTime / Mathf.Max(0.01f, arcSeconds);
                    var pos = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, k));
                    pos.y += Mathf.Sin(k * Mathf.PI) * arcHeight;
                    tr.position = pos;
                    yield return null;
                }
                // Leave it exactly on its slot, or it ends up floating at the shelf origin.
                tr.localPosition = slotLocal;
            }

            // 3. persist — the shelf is only an artefact if it survives the session
            if (save != null)
            {
                save.Unlock(shapeId);
                save.Save();
            }

            if (referenceHandVisual != null) referenceHandVisual.SetActive(true);
            _playing = false;
        }
    }
}
