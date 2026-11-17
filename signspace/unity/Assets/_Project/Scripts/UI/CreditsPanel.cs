using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Signspace.Matching;

namespace Signspace.UI
{
    /// <summary>
    /// The credits object (P5.5).
    ///
    /// Credit is not a compliance artefact: a Deaf signer's hand is the product's content, and the
    /// product says so where the user can see it. The panel therefore lives on the shelf, opens on its
    /// own once in the first minute of the first launch, and stays re-openable by pinching its bead.
    ///
    /// THE RULES IT OBEYS
    /// ------------------
    ///   1. **The wording is data, not copy.** `creditLine` comes from `set-meta.json` and ships
    ///      verbatim (ADR-007). A state-A build names the signer; a state-B build says the review is
    ///      pending. This component never invents either.
    ///   2. **The fixed sentence is always present.** *"This is practice software, not a replacement
    ///      for a Deaf teacher."* appears in both states and is appended if the data omits it.
    ///   3. **A name is only shown when there is a name.** With `signerName` empty the panel says the
    ///      truth — no signer has reviewed this build — instead of leaving a blank line that reads as
    ///      an omission.
    /// </summary>
    public class CreditsPanel : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("The authorship file. Read, never typed: see Signspace.Matching.SetMeta.")]
        public string metaFile = "Signs/set-meta.json";

        [Header("Content")]
        [Tooltip("Everything the panel shows; hidden until Open().")]
        public GameObject content;
        public Text header;
        public Text body;

        [Header("Placement")]
        [Tooltip("Optional: the shelf this object rides on. Left empty it stays where it was planted.")]
        public Transform follow;
        [Tooltip("Offset from the shelf's origin, in metres.")]
        public Vector3 followOffset = new Vector3(0.24f, 0.06f, 0.02f);

        [Header("First launch")]
        [Tooltip("Seconds after start before the panel opens itself once. Within the first minute, and " +
                 "late enough that it does not collide with the first hand.")]
        public float showOnFirstLaunchAfterSeconds = 20f;
        [Tooltip("PlayerPrefs key recording that the first-launch showing happened.")]
        public string shownKey = "signspace.credits.shown";

        /// <summary>The fixed sentence. Never optional, in either authorship state.</summary>
        public const string FixedLine =
            "This is practice software, not a replacement for a Deaf teacher.";

        public SetMeta Meta { get; private set; }
        public bool IsOpen => content != null && content.activeSelf;

        bool _requestedAutoShow;

        void Start()
        {
            if (content != null) content.SetActive(false);

            // First launch only: open once, after the first hand has settled. If the user closes it,
            // the bead is still there and it never opens itself again.
            if (PlayerPrefs.GetInt(shownKey, 0) == 0 && !_requestedAutoShow)
            {
                _requestedAutoShow = true;
                StartCoroutine(AutoShowOnce());
            }

            StartCoroutine(Load());
        }

        void LateUpdate()
        {
            if (follow == null) return;
            transform.position = follow.TransformPoint(followOffset);
            // Face the user, like the shelf's own plate does.
            transform.rotation = follow.rotation;
        }

        IEnumerator AutoShowOnce()
        {
            yield return new WaitForSeconds(showOnFirstLaunchAfterSeconds);
            if (!IsOpen)
            {
                Open();
                PlayerPrefs.SetInt(shownKey, 1);
                PlayerPrefs.Save();
            }
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

        IEnumerator Load()
        {
            string json = null;
            using (UnityWebRequest request = UnityWebRequest.Get(SignSetLoader.ToUrl(metaFile)))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success) json = request.downloadHandler.text;
            }

            if (!string.IsNullOrEmpty(json))
            {
                try { Meta = JsonUtility.FromJson<SetMeta>(json); }
                catch (System.Exception e) { Debug.LogError("[CreditsPanel] set-meta.json: " + e.Message); }
            }
            else
            {
                Debug.LogError("[CreditsPanel] could not read " + metaFile + "; the panel will show the " +
                               "state-B sentence rather than a name it cannot verify.");
            }

            Build();
        }

        void Build()
        {
            string credit = Meta != null ? Meta.creditLine : null;
            if (string.IsNullOrEmpty(credit))
                credit = "Handshapes authored against published ASL references. Deaf signer review in " +
                         "progress. " + FixedLine;
            else if (!credit.Contains("not a replacement"))
                credit = credit + " " + FixedLine;

            if (header != null)
                header.text = "The hand in this build";

            if (body != null)
            {
                var sb = new System.Text.StringBuilder();
                if (Meta != null && !string.IsNullOrEmpty(Meta.signerName))
                {
                    sb.Append("Recorded by ").Append(Meta.signerName);
                    if (!string.IsNullOrEmpty(Meta.signerCity)) sb.Append(", ").Append(Meta.signerCity);
                    if (!string.IsNullOrEmpty(Meta.verifiedOn)) sb.Append(" — ").Append(Meta.verifiedOn);
                    sb.Append('.');
                }
                else
                {
                    sb.Append("No signer has reviewed this build yet.");
                }

                sb.AppendLine().AppendLine().Append(credit);
                body.text = sb.ToString();
            }
        }

        /// <summary>Dev check: what the panel would show, without opening it.</summary>
        public string Report()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("credits: hasMeta=").Append(Meta != null);
            sb.Append(" signer=").Append(Meta != null && !string.IsNullOrEmpty(Meta.signerName) ? Meta.signerName : "(none)");
            sb.Append(" city=").Append(Meta != null && !string.IsNullOrEmpty(Meta.signerCity) ? Meta.signerCity : "(none)");
            sb.Append(" creditLine=").Append(Meta != null && !string.IsNullOrEmpty(Meta.creditLine) ? "present" : "MISSING");
            sb.Append(" fixedLine=").Append(FixedLine.Length > 0);
            return sb.ToString();
        }
    }
}
