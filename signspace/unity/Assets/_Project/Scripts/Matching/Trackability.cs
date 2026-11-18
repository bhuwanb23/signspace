using System;
using System.Collections.Generic;
using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>One row of the trackability table. `status` is measured or "unmeasured"; `expected` is a design decision.</summary>
    [Serializable]
    public class TrackabilityVariant
    {
        public string id;
        /// <summary>fine | may-resolve | not-resolvable | out-of-scope - from SIGNSET.md section 6.</summary>
        public string expected;
        /// <summary>tracks-and-locks | flickers | tracks-wrong-shape | tracks-never-locks | not-resolvable | unmeasured.</summary>
        public string status;
        /// <summary>Fraction of a 10 s hold inside the lock band, or -1 when unmeasured.</summary>
        public float lockRate = -1f;
        /// <summary>
        /// True when repeated holds disagreed about this shape (the S/E case in P3.6). A shape at the
        /// threshold is not rounded up or down: the count becomes a range, because the range is what
        /// was measured.
        /// </summary>
        public bool borderline;
        /// <summary>Why the design expected this shape to fail. Kept after measurement, when `note` becomes the result.</summary>
        public string designNote;
        /// <summary>What the panel prints: the measured result once there is one, the design expectation before.</summary>
        public string note;
    }

    [Serializable]
    public class TrackabilityData
    {
        public int version = 1;
        /// <summary>True only when P1.5's probe has run and every status is a real result.</summary>
        public bool measured;
        public string measuredUtc;
        public string device;
        public string conditions;
        public string note;
        /// <summary>
        /// The forward-looking sentence the panel prints under the reasons: what would change the
        /// exclusion, with no promised date. Read from the file so the panel and TRACKABILITY.md
        /// cannot drift, and so the signer's rewrite is a one-file edit.
        /// </summary>
        public string forwardLine;
        public List<TrackabilityVariant> variants = new List<TrackabilityVariant>();
    }

    /// <summary>
    /// The honest mirror's DATA PATH (P3.4).
    ///
    /// The rule this class exists to enforce: **the panel cannot print an unmeasured number.** Every
    /// figure it shows is read from `StreamingAssets/trackability.json`, and that file has a `measured`
    /// flag that is false until P1.5's probe has actually run. While it is false the panel says what it
    /// can prove from the design decision - how many shapes stay out of the build - and nothing about
    /// the sensor's accuracy.
    ///
    /// Why a data file and not fields on the component: a number typed into an inspector is a number
    /// nobody can date, trace or diff. `TRACKABILITY.md` section 3 is the same table in prose, this is
    /// the machine-readable half, and the panel reads the machine half so the two cannot drift.
    /// </summary>
    public static class Trackability
    {
        public const string Url = "trackability.json";

        public const string StatusLocks = "tracks-and-locks";
        public const string StatusUnmeasured = "unmeasured";
        public const string ExpectedNotResolvable = "not-resolvable";

        public static TrackabilityData Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var data = JsonUtility.FromJson<TrackabilityData>(json);
                if (data == null) return null;
                if (data.variants == null) data.variants = new List<TrackabilityVariant>();
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError("[Trackability] the trackability file could not be parsed: " + e.Message);
                return null;
            }
        }

        public static int Total(TrackabilityData data)
        {
            return data == null || data.variants == null ? 0 : data.variants.Count;
        }

        /// <summary>Shapes the design leaves out because the sensor is not expected to resolve them.</summary>
        public static int ExcludedByDesign(TrackabilityData data)
        {
            if (data == null || data.variants == null) return 0;
            int n = 0;
            for (int i = 0; i < data.variants.Count; i++)
                if (data.variants[i].expected == ExpectedNotResolvable) n++;
            return n;
        }

        /// <summary>
        /// Measured successes that were never in doubt. Zero while unmeasured, which is the point.
        /// Borderline shapes are deliberately NOT counted here: they belong to the upper end of the range
        /// (see <see cref="BorderlineCount"/>), because a shape that locks in some holds and not others
        /// has not earned a place in the lower number.
        /// </summary>
        public static int MeasuredRobust(TrackabilityData data)
        {
            if (data == null || data.variants == null || !data.measured) return 0;
            int n = 0;
            for (int i = 0; i < data.variants.Count; i++)
                if (data.variants[i].status == StatusLocks && !data.variants[i].borderline) n++;
            return n;
        }

        /// <summary>
        /// Shapes in doubt: measured, marked borderline, and locking at least sometimes. They widen the
        /// count into a range rather than being rounded into or out of it.
        /// </summary>
        public static int BorderlineCount(TrackabilityData data)
        {
            if (data == null || data.variants == null || !data.measured) return 0;
            int n = 0;
            for (int i = 0; i < data.variants.Count; i++)
                if (data.variants[i].borderline && data.variants[i].status != StatusUnmeasured) n++;
            return n;
        }

        /// <summary>
        /// Ids the panel renders, in file order: the shapes the design excludes AND the device agrees it
        /// cannot see. A design exclusion that turns out to lock comes off the panel - otherwise the panel
        /// would publish a limit that measurement has lifted, which is the same lie in the other direction.
        /// </summary>
        public static List<string> ExcludedIds(TrackabilityData data)
        {
            var ids = new List<string>();
            if (data == null || data.variants == null) return ids;
            for (int i = 0; i < data.variants.Count; i++)
            {
                var v = data.variants[i];
                if (v.expected != ExpectedNotResolvable) continue;
                if (data.measured && v.status == StatusLocks) continue;
                ids.Add(v.id);
            }
            return ids;
        }

        /// <summary>Numbers as words, for the count line. Four reads better than 4 in a sentence.</summary>
        static string Words(int n)
        {
            string[] words = { "No", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight",
                               "Nine", "Ten", "Eleven", "Twelve" };
            return n >= 0 && n < words.Length ? words[n] : n.ToString();
        }

        /// <summary>
        /// The count line. Two shapes, and only one of them is allowed to mention tracking:
        ///
        ///   measured   "4 of 20 handshape variants met the lock-rate threshold — measured 18 Nov 2026
        ///               on Quest 3S, normal light."
        ///   unmeasured "Four handshapes excluded by design, pending tracking measurements."
        ///
        /// A number without a date is a guess, so the dated form only appears when the file carries one.
        /// </summary>
        public static string CountLine(TrackabilityData data)
        {
            int total = Total(data);
            int excluded = ExcludedByDesign(data);

            if (data != null && data.measured)
            {
                int robust = MeasuredRobust(data);
                int inDoubt = BorderlineCount(data);
                string figure = inDoubt > 0 ? robust + " to " + (robust + inDoubt) : robust.ToString();
                string date = string.IsNullOrEmpty(data.measuredUtc) ? "an unrecorded date" : data.measuredUtc;
                string device = string.IsNullOrEmpty(data.device) ? "the headset" : data.device;
                string conditions = string.IsNullOrEmpty(data.conditions) ? "" : ", " + data.conditions;
                return figure + " of " + total + " handshape variants met the lock-rate threshold — measured "
                       + date + " on " + device + conditions + ".";
            }

            // Before device measurement, exclusions are design decisions, not sensor findings.
            return Words(excluded) + " handshape" + (excluded == 1 ? "" : "s")
                   + " excluded by design, pending tracking measurements.";
        }

        /// <summary>
        /// The line under the count, while the probe is out. Says what is not yet known rather than
        /// implying the exclusions are measurements: they are expectations with reasons.
        /// </summary>
        public static string ProvenanceLine(TrackabilityData data)
        {
            if (data != null && data.measured)
            {
                int inDoubt = BorderlineCount(data);
                return inDoubt == 0 ? "" : "Repeated holds disagreed on " + Words(inDoubt).ToLowerInvariant()
                       + "; the range is " + (inDoubt == 1 ? "that shape" : "those shapes") + ".";
            }
            return "Not measured on every hand yet.";
        }

        /// <summary>
        /// What would change the exclusion. One sentence, no date promised (P5.7): a roadmap date in a
        /// panel about honesty is the one kind of claim this product cannot afford to miss.
        /// </summary>
        public static string ForwardLine(TrackabilityData data)
        {
            if (data != null && !string.IsNullOrEmpty(data.forwardLine)) return data.forwardLine;
            return "Hand tracking that reports the depth between fingers, not only their joint angles, " +
                   "would change this.";
        }

        /// <summary>
        /// One line of why, from the file's own note, falling back to the category. Never a general
        /// statement, because "we can't see it" is not a reason a judge should have to take on trust.
        /// </summary>
        public static string ReasonFor(TrackabilityData data, string id)
        {
            if (data == null || data.variants == null) return "";
            for (int i = 0; i < data.variants.Count; i++)
            {
                if (data.variants[i].id != id) continue;
                string note = data.variants[i].note;
                return string.IsNullOrEmpty(note) ? "Not resolvable from the cameras' angle today." : note;
            }
            return "";
        }
    }
}
