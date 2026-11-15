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
        public string Id;
        public string Source;
        public string Family;
        public string Description;
        public Quaternion[] LocalRotations;

        public static HandPose FromJson(string json, out string error)
        {
            error = null;
            PoseJson p;
            try { p = JsonUtility.FromJson<PoseJson>(json); }
            catch (Exception e) { error = "unparseable JSON: " + e.Message; return null; }

            if (p == null) { error = "unparseable JSON"; return null; }
            if (string.IsNullOrEmpty(p.id)) { error = "missing 'id'"; return null; }
            if (p.jointCount <= 0) { error = "missing or non-positive 'jointCount'"; return null; }
            if (p.jointNames == null || p.jointNames.Length != p.jointCount)
                { error = $"jointNames length {(p.jointNames == null ? 0 : p.jointNames.Length)} != jointCount {p.jointCount}"; return null; }
            if (p.q == null || p.q.Length != p.jointCount * 4)
                { error = $"q length {(p.q == null ? 0 : p.q.Length)} != {p.jointCount * 4}"; return null; }

            var rots = new Quaternion[p.jointCount];
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
                rots[i] = quat.normalized;
            }

            return new HandPose
            {
                Id = p.id,
                Source = p.source,
                Family = p.family,
                Description = p.description,
                LocalRotations = rots
            };
        }

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
