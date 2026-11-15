using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Signspace.Matching;

namespace Signspace.UI
{
    /// <summary>
    /// The panel that publishes what the sensor cannot see (NOVELTY-AND-PRIOR-ART.md N2).
    ///
    /// Nobody in a field of 650 submissions advertises their platform's limits. This does, with the
    /// reasons, and it is a never-cut item: it is the cheapest trust-building object in the product
    /// and the one thing a Deaf judge is most likely to respect.
    ///
    /// Rules it must obey:
    ///   - numbers come from measurement (TRACKABILITY.md), never from rounding up to look better;
    ///   - every excluded shape is rendered CORRECTLY — an excluded shape shown wrong is worse
    ///     than not showing it;
    ///   - it is reached from inside the space (a shelf object), not a settings menu.
    /// </summary>
    public class HonestMirror : MonoBehaviour
    {
        [Header("Content")]
        [Tooltip("Shapes the harness cannot resolve. Rendered greyed, with a reason each.")]
        public string[] excludedShapes = { "M", "N", "T", "R" };
        public string excludedReason =
            "The cameras cannot see a finger that is behind another finger.";

        [Header("Measured numbers — from TRACKABILITY.md, never invented")]
        public int robustVariants;
        public int totalVariants;
        public string measuredOn = "";
        public string device = "Quest 3S";

        [Header("Authorship — mirrors set-meta.json (ADR-007)")]
        public SetMeta meta;

        [Header("View")]
        public GameObject root;
        public Text header;
        public Text body;
        public Text credit;

        void Start()
        {
            if (root != null) root.SetActive(false);
            Build();
        }

        public void Open()
        {
            Build();
            if (root != null) root.SetActive(true);
        }

        public void Close()
        {
            if (root != null) root.SetActive(false);
        }

        public void Toggle()
        {
            if (root == null) return;
            if (root.activeSelf) Close(); else Open();
        }

        void Build()
        {
            if (header != null)
                header.text = $"{robustVariants} of {totalVariants} handshape variants are robustly trackable today";

            if (body != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine(excludedReason);
                sb.AppendLine();
                sb.Append("Not taught here: ");
                sb.AppendLine(string.Join(", ", excludedShapes));
                if (!string.IsNullOrEmpty(measuredOn))
                    sb.Append($"Measured on {device}, {measuredOn}.");
                body.text = sb.ToString();
            }

            if (credit != null)
            {
                // The authorship line is data, never copy (ADR-007). Whatever ships, ships truthfully.
                credit.text = meta != null && !string.IsNullOrEmpty(meta.creditLine)
                    ? meta.creditLine
                    : "Handshapes authored against published ASL references. Deaf signer review in progress.";
            }
        }
    }
}
