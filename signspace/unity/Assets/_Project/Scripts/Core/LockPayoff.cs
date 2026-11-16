using System.Collections;
using UnityEngine;
using Signspace.Matching;
using Signspace.Spatial;
using Signspace.UI;

namespace Signspace.Core
{
    /// <summary>
    /// The first reward, and it happens inside 75 seconds (P2.5).
    ///
    /// On lock, the reference hand **becomes** an object: it brightens and shrinks, an object forms
    /// *inside where it was*, and that object arcs to a temporary anchor on the shelf. Whole sequence
    /// 0.99 s, because from the fourth repetition onwards anything longer is skipped - and a reward that
    /// gets skipped is not a reward.
    ///
    /// **Transformation, not deletion.** Three things do this work, and all three are timing decisions:
    ///
    ///   1. the hand **brightens as it shrinks** (`_EmissionColor` ramps up as scale falls), so it reads
    ///      as the same matter concentrating rather than as something being switched off;
    ///   2. the object **starts forming 0.10 s before the hand is gone** (`objectFormStart`), so there is
    ///      never a frame in which nothing is there. A gap is a cut; an overlap is a transformation;
    ///   3. the object forms with a **back-eased overshoot** and a brief settle at the anchor, which is
    ///      what an object that has been *placed* looks like.
    ///
    /// The timeline is a pure function of `_elapsed`, not a chain of nested coroutines. That is what lets
    /// `TotalSeconds` be a real number rather than a hope, lets the P2.5 timing check read it, and means a
    /// dropped frame cannot stretch the payoff - the sequence ends when the clock says so, not when the
    /// last `yield return` happens to be reached.
    /// </summary>
    public class LockPayoff : MonoBehaviour
    {
        [Header("Dependencies")]
        public Matcher matcher;
        public SaveSystem save;
        public ShelfAnchor shelf;
        public Transform referenceHand;
        public Transform flyFrom;

        [Header("View")]
        public GameObject referenceHandVisual;
        [Tooltip("Inactive prototype of the object the hand turns into. Pooled, one per lock.")]
        public GameObject objectPrototype;
        [Tooltip("Where the object comes to rest. The shelf proper arrives in P4; this is a temp anchor.")]
        public Transform restAnchor;
        [Tooltip("Optional: the controller-free 'haptic' - a pulse in the tracked hand.")]
        public HandPulse handPulse;

        [Header("Timing (seconds) — see phases/p2-core-loop/lock-payoff.md")]
        [Tooltip("Scale + fade of the reference hand.")]
        public float dissolveSeconds = 0.40f;
        [Tooltip("When the object starts forming, measured from the lock. Must be < dissolveSeconds.")]
        public float objectFormStart = 0.30f;
        [Tooltip("How long the object takes to reach full size.")]
        public float objectFormSeconds = 0.22f;
        [Tooltip("Flight time to the anchor.")]
        public float arcSeconds = 0.45f;
        [Tooltip("Overshoot settle once it arrives.")]
        public float settleSeconds = 0.12f;
        [Tooltip("The reference hand comes back for the next attempt.")]
        public float handReturnSeconds = 0.12f;
        [Tooltip("The whole payoff must finish inside this, or it becomes a cutscene.")]
        public float budgetSeconds = 1.2f;

        [Header("Repetition")]
        [Tooltip("Full fidelity for this many locks, then everything is compressed. A reward you have " +
                 "already seen twice is a reward you want to get past.")]
        public int fullFidelityRepeats = 3;
        [Tooltip("Timing multiplier after the first `fullFidelityRepeats` locks.")]
        public float hurryScale = 0.55f;

        [Header("Audio")]
        public AudioSource chime;
        public AudioClip lockChime;
        [Tooltip("Delay before the chime starts. It should land with the object, not with the button.")]
        public float chimeDelaySeconds = 0.05f;

        [Header("Temporary anchor")]
        [Tooltip("How many settled objects the anchor keeps before the oldest is removed. The shelf proper " +
                 "arrives in P4; until then this is a small pile, not a museum.")]
        public int maxObjectsOnAnchor = 3;

        [Header("Arc")]
        public float arcHeight = 0.12f;
        [Tooltip("Start the arc this far along the object's formation, so it flies while it is still growing.")]
        public float arcStartFraction = 0f;

        public float LastPayoffSeconds { get; private set; }
        public int PayoffCount { get; private set; }
        public bool IsPlaying => _playing;
        public float Budget01 => TotalSeconds / Mathf.Max(0.01f, budgetSeconds);

        readonly System.Collections.Generic.List<GameObject> _spawned = new System.Collections.Generic.List<GameObject>();
        int _repetitions;
        bool _playing;
        float _scale = 1f;

        /// <summary>The planned duration of the next payoff, in seconds. Used by the timing check.</summary>
        public float TotalSeconds => Scale(settleEnd + handReturnSeconds);

        float settleEnd => Mathf.Max(objectFormStart + arcSeconds, dissolveSeconds) + settleSeconds;

        float Scale(float seconds) => _repetitions < fullFidelityRepeats ? seconds : seconds * hurryScale;

        void OnEnable()
        {
            if (matcher != null) matcher.OnLocked += OnLocked;
        }

        void OnDisable()
        {
            if (matcher != null) matcher.OnLocked -= OnLocked;
        }

        void Start()
        {
            if (TotalSeconds > budgetSeconds)
                Debug.LogError($"[LockPayoff] the payoff is {TotalSeconds:0.00} s, over its {budgetSeconds:0.00} s " +
                               "budget. It will be skipped by the third repetition - shorten the arc or the dissolve.");
            if (objectPrototype != null) objectPrototype.SetActive(false);
        }

        void OnLocked(string shapeId)
        {
            if (_playing) return;
            StartCoroutine(Sequence(shapeId));
        }

        IEnumerator Sequence(string shapeId)
        {
            _playing = true;
            PayoffCount++;
            _repetitions++;

            // Everything below is a function of one clock, scaled once so the whole sequence keeps its
            // proportions when it is compressed after the third lock.
            float k = _repetitions <= fullFidelityRepeats ? 1f : hurryScale;
            float dissolve = dissolveSeconds * k;
            float formStart = objectFormStart * k;
            float formSeconds = objectFormSeconds * k;
            float arc = arcSeconds * k;
            float settle = settleSeconds * k;
            float returnSeconds = handReturnSeconds * k;
            float total = Mathf.Max(formStart + arc, dissolve) + settle + returnSeconds;

            var handRenderers = referenceHandVisual != null
                ? referenceHandVisual.GetComponentsInChildren<Renderer>(true)
                : new Renderer[0];
            var handStart = referenceHandVisual != null ? referenceHandVisual.transform.localScale : Vector3.one;

            // Only the last few objects stay in the room: this anchor is temporary until the shelf
            // arrives in P4, and an unbounded pile at one point is not a shelf.
            while (_spawned.Count >= maxObjectsOnAnchor)
            {
                var old = _spawned[0];
                _spawned.RemoveAt(0);
                if (old != null) Destroy(old);
            }

            GameObject obj = null;
            Vector3 objFrom = Vector3.zero, objTo = Vector3.zero;
            Transform objTr = null;

            if (save != null) save.Unlock(shapeId);      // bank it immediately: the reward is not the animation

            if (chime != null && lockChime != null)
                StartCoroutine(PlayChime(chimeDelaySeconds * k));

            float elapsed = 0f;
            while (elapsed < total)
            {
                elapsed += Time.deltaTime;

                // ---- 1. the hand dissolves: smaller, brighter, turning
                if (referenceHandVisual != null)
                {
                    float d = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, dissolve));
                    float e = EaseIn(d);
                    _scale = Mathf.Lerp(1f, 0.12f, e);
                    referenceHandVisual.transform.localScale = handStart * _scale;
                    referenceHandVisual.transform.localRotation = Quaternion.Euler(0f, 12f * e, 0f);
                    SetAlpha(handRenderers, 1f - e, Mathf.Lerp(0f, 1.6f, e));
                    if (d >= 1f) referenceHandVisual.SetActive(false);
                }

                // ---- 2. the object forms where the hand was, and starts flying while it grows
                if (obj == null && elapsed >= formStart)
                {
                    obj = Spawn(flyFrom != null ? flyFrom.position : transform.position);
                    objTr = obj.transform;
                    objFrom = objTr.position;
                    objTo = restAnchor != null ? restAnchor.position : objFrom + Vector3.up * 0.2f;
                    _spawned.Add(obj);

                    if (handPulse != null) handPulse.Pulse();   // the controller-free "haptic"
                }

                if (objTr != null)
                {
                    float f = Mathf.Clamp01((elapsed - formStart) / Mathf.Max(0.01f, formSeconds));
                    objTr.localScale = Vector3.one * FormEase(f) * 0.16f;

                    float a = Mathf.Clamp01((elapsed - formStart - arcStartFraction * arc) / Mathf.Max(0.01f, arc));
                    if (a > 0f)
                    {
                        var pos = Vector3.Lerp(objFrom, objTo, Mathf.SmoothStep(0f, 1f, a));
                        pos.y += Mathf.Sin(a * Mathf.PI) * arcHeight;
                        objTr.position = pos;
                    }

                    if (a >= 1f) objTr.position = objTo;

                    // ---- 3. settle: a small overshoot that decays. The object has been *placed*.
                    float s = Mathf.Clamp01((elapsed - formStart - arc) / Mathf.Max(0.01f, settle));
                    if (s > 0f)
                    {
                        float overshoot = Mathf.Sin(s * Mathf.PI) * 0.06f;
                        objTr.localScale = Vector3.one * 0.16f * (1f + overshoot);
                    }
                }

                yield return null;
            }

            // ---- 4. leave the world in a clean state for the next attempt
            if (obj != null)
            {
                obj.transform.position = objTo;
                obj.transform.localScale = Vector3.one * 0.16f;
            }
            if (referenceHandVisual != null)
            {
                referenceHandVisual.SetActive(true);
                referenceHandVisual.transform.localScale = handStart;
                referenceHandVisual.transform.localRotation = Quaternion.identity;
                SetAlpha(handRenderers, 1f, 0f);
            }
            if (save != null) save.Save();

            LastPayoffSeconds = elapsed;
            if (elapsed > budgetSeconds)
                Debug.LogWarning($"[LockPayoff] took {elapsed:0.00} s (budget {budgetSeconds:0.00} s) - a dropped " +
                                 "frame is eating the payoff. Check the frame time before shortening the timeline.");

            _playing = false;
        }

        IEnumerator PlayChime(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            chime.PlayOneShot(lockChime);
        }

        GameObject Spawn(Vector3 at)
        {
            GameObject obj;
            if (objectPrototype != null)
            {
                obj = Instantiate(objectPrototype, transform);
                obj.SetActive(true);
                obj.transform.localScale = Vector3.zero;
            }
            else
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.transform.SetParent(transform, false);
                obj.transform.localScale = Vector3.one * 0.16f;
            }
            obj.transform.position = at;
            return obj;
        }

        static float EaseIn(float t) => t * t;

        /// <summary>
        /// Growth curve for the forming object: fast, decelerating, with a slight bulge around the
        /// middle. A back-ease was tried first and rejected - it reaches 77% of full size in the first
        /// fifth of the time, which reads as a pop rather than as something assembling.
        /// </summary>
        static float FormEase(float t) => (1f - (1f - t) * (1f - t) * (1f - t)) + Mathf.Sin(t * Mathf.PI) * 0.08f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static MaterialPropertyBlock _block;

        /// <summary>Fade + brighten without touching the shared material (no leaks, no asset churn).</summary>
        public static void SetAlpha(Renderer[] renderers, float alpha, float emission)
        {
            if (renderers == null || renderers.Length == 0) return;
            if (_block == null) _block = new MaterialPropertyBlock();

            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || r.sharedMaterial == null) continue;
                r.GetPropertyBlock(_block);

                if (r.sharedMaterial.HasProperty(BaseColorId))
                {
                    var c = r.sharedMaterial.GetColor(BaseColorId);
                    _block.SetColor(BaseColorId, new Color(c.r, c.g, c.b, alpha));
                }
                if (r.sharedMaterial.HasProperty(ColorId))
                {
                    var c = r.sharedMaterial.GetColor(ColorId);
                    _block.SetColor(ColorId, new Color(c.r, c.g, c.b, alpha));
                }
                if (r.sharedMaterial.HasProperty(EmissionId))
                {
                    // Brightening while shrinking is the whole trick: matter concentrating, not a light
                    // being switched off.
                    var e = r.sharedMaterial.HasProperty(BaseColorId)
                        ? r.sharedMaterial.GetColor(BaseColorId) : Color.white;
                    _block.SetColor(EmissionId, e * emission);
                }
                r.SetPropertyBlock(_block);
            }
        }
    }
}
