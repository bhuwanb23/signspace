using System;
using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>
    /// Serialised form of a handshape. One file per shape in StreamingAssets/Signs.
    /// Quaternions are stored FLAT (x,y,z,w per joint) because Unity's JsonUtility
    /// cannot deserialise nested arrays, and a flat array diffs cleanly in git.
    /// </summary>
    [Serializable]
    public class PoseJson
    {
        public int version;
        public string id;
        public string handedness;       // "right" | "left"
        public int jointCount;
        public string neutralCaptured;  // ISO-8601, when the neutral pose was captured
        public string source;           // "reference-authored" | "signer-verified"
        public string family;           // "spread" | "closure" -- used by the honest mirror
        public string description;      // one line, for the reference hand's label
        public string[] jointNames;     // length == jointCount, in canonical order
        public float[] q;               // length == jointCount * 4
    }

    /// <summary>In-memory handshape: the local rotation of each tracked joint.</summary>
    public class HandPose
    {
        /// <summary>The only schema this loader accepts. POSE-FORMAT.md section 2 promises that an
        /// unknown version is refused rather than half-read; this is that promise, in code.</summary>
        public const int SupportedVersion = 1;

        public string Id;
        public string Source;
        public string Family;
        public string Description;
        public string Handedness;
        public string NeutralCaptured;

        /// <summary>
        /// Canonical-ordered rotations: index i is JointNames.Canonical[i], ALWAYS, regardless of the
        /// order the file listed its joints in. Length is the canonical joint count.
        /// </summary>
        public Quaternion[] LocalRotations;

        /// <summary>How many joints the FILE declared, before mapping. Kept for reporting.</summary>
        public int FileJointCount;

        public static HandPose FromJson(string json, out string error)
        {
            error = null;
            PoseJson p;
            try { p = JsonUtility.FromJson<PoseJson>(json); }
            catch (Exception e) { error = "unparseable JSON: " + e.Message; return null; }

            if (p == null) { error = "unparseable JSON"; return null; }

            // POSE-FORMAT.md section 2: "the loader refuses unknown versions". Before P1.4 this was a
            // promise the code did not keep - a version-2 file would have been read as if it were 1,
            // which is the one failure mode a version field exists to prevent.
            if (p.version != SupportedVersion)
            {
                error = "unsupported schema version " + p.version + " (this build reads version " +
                        SupportedVersion + "). Refusing to guess at a format we do not know.";
                return null;
            }

            if (string.IsNullOrEmpty(p.id)) { error = "missing 'id'"; return null; }
            if (p.jointCount <= 0) { error = "missing or non-positive 'jointCount'"; return null; }
            if (p.jointNames == null || p.jointNames.Length != p.jointCount)
                { error = $"jointNames length {(p.jointNames == null ? 0 : p.jointNames.Length)} != jointCount {p.jointCount}"; return null; }
            if (p.q == null || p.q.Length != p.jointCount * 4)
                { error = $"q length {(p.q == null ? 0 : p.q.Length)} != {p.jointCount * 4}"; return null; }

            // ---- map by NAME into canonical order ------------------------------------------
            // This is the P1.4 trap made concrete: "baking absolute world quaternions instead of
            // relative... breaks the moment a user has a different hand size". The relative part is
            // the format's job; the ORDER part is this loader's job, and it used to be nobody's.
            // The rotations were read positionally, so a file whose jointNames were in a different
            // order would have put every rotation on the wrong joint - silently, and with a 26/26
            // "loaded successfully" in the log.
            var canonical = new Quaternion[JointNames.Canonical.Length];
            var filled = new bool[JointNames.Canonical.Length];

            for (int i = 0; i < p.jointCount; i++)
            {
                int o = i * 4;
                var quat = new Quaternion(p.q[o], p.q[o + 1], p.q[o + 2], p.q[o + 3]);
                float sq = quat.x * quat.x + quat.y * quat.y + quat.z * quat.z + quat.w * quat.w;
                if (Mathf.Abs(sq - 1f) > 0.01f)
                {
                    error = $"joint {i} ('{p.jointNames[i]}') quaternion not normalised (|q|^2 = {sq:F4})";
                    return null;
                }

                string canonicalName = JointNames.Canonicalize(p.jointNames[i]);
                int index = canonicalName == null ? -1 : JointNames.IndexOf(canonicalName);
                if (index < 0)
                {
                    error = $"joint {i} ('{p.jointNames[i]}') is not a known joint name; see POSE-FORMAT.md section 3";
                    return null;
                }
                if (filled[index])
                {
                    error = $"joint '{canonicalName}' appears twice";
                    return null;
                }

                canonical[index] = quat.normalized;
                filled[index] = true;
            }

            // A joint the file did not mention keeps identity - the neutral rotation - rather than
            // borrowing a neighbour's. It means a reduced-joint file still loads, and the honest
            // mirror can say which joints were absent instead of inventing them.
            int missing = 0;
            for (int i = 0; i < filled.Length; i++) if (!filled[i]) missing++;
            if (missing > 0)
            {
                Debug.LogWarning("[HandPose] '" + p.id + "' omits " + missing + " canonical joints; " +
                                 "those stay at the neutral rotation.");
            }

            return new HandPose
            {
                Id = p.id,
                Source = p.source,
                Family = p.family,
                Description = p.description,
                Handedness = p.handedness,
                NeutralCaptured = p.neutralCaptured,
                LocalRotations = canonical,
                FileJointCount = p.jointCount
            };
        }

        /// <summary>Canonical joint count of the applied pose (always the full canonical set).</summary>
        public int JointCount => LocalRotations?.Length ?? 0;
    }

    /// <summary>Authorship/credit metadata. Read from set-meta.json — see ADR-007.</summary>
    [Serializable]
    public class SetMeta
    {
        public int version;
        public string authorship;         // "reference-authored" | "signer-verified"
        public string verificationMode;   // "pending-review" | "per-shape-approved"
        public string signerName;
        public string signerCity;
        public string verifiedOn;
        public string creditLine;         // rendered verbatim in the credits object
    }
}
