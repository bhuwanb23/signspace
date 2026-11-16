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
        // ---- schema 2 (frozen in P2.1) ----
        public int version;
        public string id;
        public string handedness;       // "right" | "left"
        public string signer;           // whose NEUTRAL the rotations are relative to
        public string captured;         // ISO-8601, when that neutral was captured
        public string vantage;          // "authored-neutral" | "first-person-palm-down" | ...
        public int jointCount;
        public string[] joints;         // length == jointCount, canonical order
        public float[] rot;             // length == jointCount * 4, x,y,z,w per joint
        public string source;           // "reference-authored" | "probe-reference" | "signer-verified"
        public ConfidenceJson confidence;
        public string family;           // "spread" | "closure" -- used by the honest mirror
        public string description;      // one line, for the reference hand's label
        public string notes;
        public bool probeOnly;          // true = a P1.5 probe reference, never shipped content

        // ---- schema 1 leftovers, read only so old files survive the migration ----
        public string[] jointNames;
        public float[] q;
        public string neutralCaptured;
    }

    [Serializable]
    public class ConfidenceJson
    {
        public string authoring;        // "high" | "medium" | "low" -- the author's own certainty
        public string measured;         // null until a P1.5 session measures it. Never estimated.
        public string note;
    }

    /// <summary>In-memory handshape: the local rotation of each tracked joint.</summary>
    public class HandPose
    {
        /// <summary>The schema written by the authoring tools. POSE-FORMAT.md section 2 promises that
        /// an unknown version is refused rather than half-read; this is that promise, in code.</summary>
        public const int SupportedVersion = 2;

        /// <summary>
        /// The oldest schema still read, through an in-memory migration.
        ///
        /// P2.1 renamed three fields (jointNames -> joints, q -> rot, neutralCaptured -> captured) and
        /// added signer/vantage/confidence. Refusing v1 outright would have thrown away the six
        /// shipped poses and, worse, taught the wrong lesson: a version field exists so that old files
        /// can be READ by new builds, not so that new builds can abandon them. The migration is
        /// deliberately minimal - renames plus honest "unknown signer" labels - because a v1 file
        /// genuinely does not know who its neutral came from.
        /// </summary>
        public const int MinReadableVersion = 1;

        public string Id;
        public string Source;
        public string Family;
        public string Description;
        public string Handedness;
        public string Captured;         // the neutral capture these rotations are relative to
        public string Signer;
        public string Vantage;
        public string AuthoringConfidence;
        public bool ProbeOnly;
        /// <summary>True when the file was schema 1 and was migrated on load. Reported in the log.</summary>
        public bool MigratedFromV1;

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
            if (p.version < MinReadableVersion || p.version > SupportedVersion)
            {
                error = "unsupported schema version " + p.version + " (this build reads versions " +
                        MinReadableVersion + " to " + SupportedVersion +
                        "). Refusing to guess at a format we do not know.";
                return null;
            }

            // ---- schema 1 -> 2, in memory (P2.1's migration path) ----------------------------
            bool migrated = p.version == MinReadableVersion;
            if (migrated)
            {
                if (p.joints == null) p.joints = p.jointNames;
                if (p.rot == null) p.rot = p.q;
                if (string.IsNullOrEmpty(p.captured)) p.captured = p.neutralCaptured;
                if (string.IsNullOrEmpty(p.signer)) p.signer = "legacy-v1-file";
                if (string.IsNullOrEmpty(p.vantage)) p.vantage = "authored-neutral";
                Debug.LogWarning("[HandPose] '" + p.id + "' is schema 1; read through the P2.1 migration. " +
                                 "Its signer is unknown (legacy-v1-file) and its confidence was never " +
                                 "recorded. Run scripts/validate_poses.py --migrate to upgrade the file.");
            }

            if (string.IsNullOrEmpty(p.id)) { error = "missing 'id'"; return null; }
            if (p.jointCount <= 0) { error = "missing or non-positive 'jointCount'"; return null; }
            if (p.joints == null || p.joints.Length != p.jointCount)
                { error = $"joints length {(p.joints == null ? 0 : p.joints.Length)} != jointCount {p.jointCount}"; return null; }
            if (p.rot == null || p.rot.Length != p.jointCount * 4)
                { error = $"rot length {(p.rot == null ? 0 : p.rot.Length)} != {p.jointCount * 4}"; return null; }
            if (!migrated && string.IsNullOrEmpty(p.signer))
                { error = "missing 'signer': a rotation is relative to somebody's neutral (POSE-FORMAT.md section 4)"; return null; }

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
                var quat = new Quaternion(p.rot[o], p.rot[o + 1], p.rot[o + 2], p.rot[o + 3]);
                float sq = quat.x * quat.x + quat.y * quat.y + quat.z * quat.z + quat.w * quat.w;
                if (Mathf.Abs(sq - 1f) > 0.01f)
                {
                    error = $"joint {i} ('{p.joints[i]}') quaternion not normalised (|q|^2 = {sq:F4})";
                    return null;
                }

                string canonicalName = JointNames.Canonicalize(p.joints[i]);
                int index = canonicalName == null ? -1 : JointNames.IndexOf(canonicalName);
                if (index < 0)
                {
                    error = $"joint {i} ('{p.joints[i]}') is not a known joint name; see POSE-FORMAT.md section 3";
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
                Captured = p.captured ?? "",
                Signer = p.signer ?? "",
                Vantage = p.vantage ?? "",
                AuthoringConfidence = p.confidence != null ? p.confidence.authoring : "",
                ProbeOnly = p.probeOnly,
                MigratedFromV1 = migrated,
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
