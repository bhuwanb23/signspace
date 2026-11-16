namespace Signspace.Matching
{
    /// <summary>Why the near-miss did or did not fire this frame. Dev-facing: the reason is the honesty.</summary>
    public enum NearMissReason
    {
        None = 0,
        Fires = 1,
        TargetLocked = 2,
        NoOtherInside = 3,
        Ambiguous = 4,
        NotClearlyCloser = 5
    }

    /// <summary>One frame's verdict, with the number that supports it.</summary>
    public struct NearMissDecision
    {
        public bool Fires;
        /// <summary>The shape the hand is actually making. Null unless <see cref="Fires"/>.</summary>
        public string ShapeId;
        /// <summary>
        /// How close the hand came to that shape, in degrees. This is the only number in the claim that
        /// can be checked against the hand, which is why it - and not the gap to the target - is what
        /// gets shown.
        /// </summary>
        public float Degrees;
        public NearMissReason Reason;
        /// <summary>How many shapes sat inside the lock band. 2+ is why silence is sometimes right.</summary>
        public int Qualifiers;

        public string Explain()
        {
            switch (Reason)
            {
                case NearMissReason.Fires:
                    return "names " + ShapeId + " at " + Degrees.ToString("0.0") + " deg";
                case NearMissReason.TargetLocked:
                    return "silent: the target is already inside the lock band";
                case NearMissReason.NoOtherInside:
                    return "silent: no other shape is inside the lock band";
                case NearMissReason.Ambiguous:
                    return "silent: " + Qualifiers + " shapes are inside the lock band - ambiguity is not a diagnosis";
                case NearMissReason.NotClearlyCloser:
                    return "silent: closer to another shape, but not by the margin";
                default:
                    return "silent";
            }
        }
    }

    /// <summary>
    /// "That's not C. That's O." - the rule, in one place, with no Unity in it (P3.1).
    ///
    /// Every other app returns correct/incorrect. Naming the *confusable* sign is the thing no prior
    /// project in the field does, and it is worth nothing if it is ever wrong - so the rule is
    /// deliberately narrow:
    ///
    ///   1. the target must NOT be inside the lock band (otherwise there is nothing to explain);
    ///   2. exactly one OTHER shape must be inside it (the hand genuinely makes that shape);
    ///   3. that shape must be closer than the target by a margin (a tie is not a diagnosis).
    ///
    /// Condition 2 is the one that keeps it honest, and it is stronger than "which shape is nearest":
    /// it says the user's hand is *lock-quality on a different sign*. Anything looser - "the nearest
    /// other shape is within 26 deg" - is a claim about proximity, and proximity is not authorship.
    ///
    /// MEASURED (scripts/nearmiss_sim.py, phases/p3-feedback/near-miss.md):
    ///   - asked for A, made B, 200 noisy samples per direction, all four testable pairs and both
    ///     directions: fires 100% of the time and names B every time;
    ///   - 2000 sloppy hands that resemble nothing (8-25 deg of noise on any shape): 0 false positives,
    ///     for both this rule and the looser one it replaced;
    ///   - the ambiguity branch is unreachable on the shipped six - the closest pair is 26.0 deg apart,
    ///     so two shapes cannot both be inside a 12 deg band - and reachable the moment the 12-shape
    ///     set ships (closest pair R/U at 14.2 deg: a hand between them reads 7.0 and 7.2, and both
    ///     rules must stay silent). It is tested on the probe set, not left to chance.
    ///
    /// The distances themselves are computed by the caller for every loaded shape. That is the task's
    /// own trap note and it is right: 12 shapes x 26 joints is nothing, and computing only the target
    /// and guessing the nearest is how you get a confident wrong answer.
    /// </summary>
    public static class NearMiss
    {
        /// <summary>
        /// Decide from a full distance row. <paramref name="ids"/> and <paramref name="degrees"/> are
        /// parallel and include the target; allocations: none.
        /// </summary>
        public static NearMissDecision Evaluate(float targetDegrees, string targetId,
                                                string[] ids, float[] degrees, int count,
                                                float lockedDegrees, float marginDegrees)
        {
            var decision = new NearMissDecision { Reason = NearMissReason.None };

            if (targetDegrees < lockedDegrees)
            {
                decision.Reason = NearMissReason.TargetLocked;
                return decision;
            }

            string bestId = null;
            float bestDegrees = float.PositiveInfinity;
            int qualifiers = 0;

            for (int i = 0; i < count; i++)
            {
                if (ids[i] == null || ids[i] == targetId) continue;
                float d = degrees[i];
                if (d >= lockedDegrees) continue;

                qualifiers++;
                if (d < bestDegrees)
                {
                    bestDegrees = d;
                    bestId = ids[i];
                }
            }

            decision.Qualifiers = qualifiers;

            if (qualifiers == 0)
            {
                decision.Reason = NearMissReason.NoOtherInside;
                return decision;
            }
            if (qualifiers > 1)
            {
                // Two shapes inside the band means the hand is between them. Naming either one would be
                // a coin flip wearing a diagnosis, and the product's whole claim is that it does not lie.
                decision.Reason = NearMissReason.Ambiguous;
                decision.ShapeId = null;
                decision.Degrees = 0f;
                return decision;
            }
            if (!(bestDegrees < targetDegrees - marginDegrees))
            {
                decision.Reason = NearMissReason.NotClearlyCloser;
                decision.ShapeId = bestId;
                decision.Degrees = bestDegrees;
                return decision;
            }

            decision.Fires = true;
            decision.Reason = NearMissReason.Fires;
            decision.ShapeId = bestId;
            decision.Degrees = bestDegrees;
            return decision;
        }
    }
}
