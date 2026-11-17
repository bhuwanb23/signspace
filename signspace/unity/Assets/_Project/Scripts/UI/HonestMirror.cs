using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Signspace.Matching;

namespace Signspace.UI
{
    /// <summary>
    /// The panel that publishes what the sensor cannot see (P3.4).
    ///
    /// Nobody in a field of 650 submissions advertises their platform's limits. This does, with the
    /// reasons. It is a never-cut item: the cheapest trust-building object in the product and the one
    /// thing a Deaf judge is most likely to respect.
    ///
    /// THE THREE RULES IT OBEYS
    /// ------------------------
    ///   1. **Every excluded shape is drawn CORRECTLY.** The hands below are built by the same HandVisual
    ///      and posed with the same authored capture the rest of the app uses - a shape shown wrong
    ///      because "it is excluded anyway" would discredit the whole panel. Their pose files pass the
    ///      same validator as the shipped set.
    ///   2. **No number appears without a measurement.** The count line comes from
    ///      StreamingAssets/trackability.json and only mentions tracking when that file says it was
    ///      measured, on a named device, on a named date. Until then it states what is true by design -
    ///      how many shapes stay out - and says plainly that tracking is not yet measured on every hand.
    ///   3. **It is a demonstration of rigour, not an apology.** No "sorry", no red, no hedge: what is
    ///      out, why, and what would change it.
    ///
    /// It is reached from a physical object in the space (PanelOpener on the plinth), never from a
    /// settings menu. P4 re-homes that object onto the shelf.
    /// </summary>
    public class HonestMirror : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("Folder the excluded shapes' pose files are read from, relative to StreamingAssets.")]
        public string poseFolder = "Probe";
        [Tooltip("The trackability file. Read, never typed: see Signspace.Matching.Trackability.")]
        public string trackabilityFile = Trackability.Url;

        [Header("Content")]
        // Hidden until Open(), so the panel is something you find. The component itself must stay on an
        // active object or the load coroutine never runs.
        [Tooltip("Everything the panel shows; hidden until Open().")]
        public GameObject content;

        [Header("View")]
        public Text header;
        public Text body;
        public Text credit;

        [Header("The greyed hands")]
        // World-space, one per excluded shape, in the order the trackability file lists them. NOT canvas
        // children: a hand is built in metres and the canvas scale (0.0007) would shrink it to nothing.
        [Tooltip("One world-space slot per excluded shape.")]
        public Transform[] handSlots;
        [Tooltip("Material for the excluded hands. Grey and unlit - a specimen, not a target.")]
        public Material excludedMaterial;
        [Tooltip("Scale of each drawn hand. Under 1: they are specimens, not shapes to copy.")]
        public float handScale = 0.85f;

        [Header("Authorship - mirrors set-meta.json (ADR-007)")]
        public SetMeta meta;

        public TrackabilityData Data { get; private set; }

        /// <summary>Shapes currently drawn, in slot order. Empty until the load finishes.</summary>
        public readonly List<string> Drawn = new List<string>();

        readonly List<HandVisual> _hands = new List<HandVisual>();
        bool _loaded;

        void Start()
        {
            if (content != null) content.SetActive(false);
            StartCoroutine(Load());
        }

        public void Open()
        {
            Build();
            if (content != null) content.SetActive(true);
        }

        public void Close()
        {
            if (content != null) content.SetActive(false);
        }

        public void Toggle()
        {
            if (content == null) return;
            if (content.activeSelf) Close(); else Open();
        }

        // ------------------------------------------------------------------ loading

        IEnumerator Load()
        {
            string json = null;
            yield return ReadText(trackabilityFile, r => json = r);

            Data = Trackability.Parse(json);

            // No count is better than an invented one: with no file the panel shows no number at all.
            if (Data == null)
                Debug.LogError("[HonestMirror] no readable " + trackabilityFile + "; the panel will show no count.");

            List<string> ids = Trackability.ExcludedIds(Data);
            for (int i = 0; i < ids.Count && i < handSlots.Length; i++)
            {
                string poseJson = null;
                yield return ReadText(poseFolder + "/" + ids[i] + ".json", r => poseJson = r);

                // A blank slot is better than a made-up hand - this panel exists to not do that.
                if (string.IsNullOrEmpty(poseJson))
                {
                    Debug.LogError("[HonestMirror] no pose file for excluded shape " + ids[i] + ". Not drawn.");
                    continue;
                }

                string error;
                HandPose pose = HandPose.FromJson(poseJson, out error);
                if (pose == null)
                {
                    Debug.LogError("[HonestMirror] " + ids[i] + " pose unreadable (" + error + "). Not drawn.");
                    continue;
                }

                DrawHand(ids[i], pose, handSlots[i]);
            }

            _loaded = true;
            Build();
        }

        void DrawHand(string id, HandPose pose, Transform slot)
        {
            if (slot == null || excludedMaterial == null) return;

            var go = new GameObject("Excluded_" + id);
            go.transform.SetParent(slot, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;   // HandVisual.scale carries the size

            var visual = go.AddComponent<HandVisual>();
            visual.material = excludedMaterial;
            visual.highlightMaterial = null;
            visual.scale = handScale;
            visual.Build();
            visual.ApplyPose(pose.LocalRotations);

            _hands.Add(visual);
            Drawn.Add(id);
        }

        IEnumerator ReadText(string relativePath, System.Action<string> done)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(SignSetLoader.ToUrl(relativePath)))
            {
                yield return request.SendWebRequest();
                done(request.result == UnityWebRequest.Result.Success ? request.downloadHandler.text : null);
            }
        }

        // ------------------------------------------------------------------ the text

        void Build()
        {
            if (header != null) header.text = Trackability.CountLine(Data);

            if (body != null)
            {
                var sb = new System.Text.StringBuilder();

                string provenance = Trackability.ProvenanceLine(Data);
                if (!string.IsNullOrEmpty(provenance)) sb.AppendLine(provenance).AppendLine();

                List<string> ids = Trackability.ExcludedIds(Data);
                if (ids.Count == 0)
                {
                    sb.AppendLine("Every shape in this build has a reason to be here.");
                }
                else
                {
                    // One line per shape, from the file's own note. A shape with a reason is a decision, and
                    // a decision is worth reading; a general "the cameras are limited" is worth nothing.
                    for (int i = 0; i < ids.Count; i++)
                        sb.AppendLine(ids[i] + "   " + Trackability.ReasonFor(Data, ids[i]));

                    // What would change it, with no date attached (P5.7). A panel about what the hardware
                    // cannot do is allowed to say what would fix it; it is not allowed to promise when.
                    string forward = Trackability.ForwardLine(Data);
                    if (!string.IsNullOrEmpty(forward))
                        sb.AppendLine().Append(forward);
                }

                if (!_loaded) sb.AppendLine().Append("Reading the shapes...");
                body.text = sb.ToString();
            }

            if (credit != null)
            {
                // Authorship is data, never copy (ADR-007): whatever set-meta.json says ships verbatim.
                credit.text = meta != null && !string.IsNullOrEmpty(meta.creditLine)
                    ? meta.creditLine
                    : "Handshapes authored against published ASL references. Signer review in progress.";
            }
        }

        /// <summary>Dev check: what the panel is actually showing, for the overlay and the log.</summary>
        public string Report()
        {
            return "honest mirror: " + Drawn.Count + " hands drawn (" + string.Join(",", Drawn) + "), "
                   + Trackability.Total(Data) + " variants in the file, measured=" +
                   (Data != null && Data.measured);
        }
    }
}
