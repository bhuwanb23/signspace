using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>What the hold is doing this frame. Exposed for the overlay and for the log.</summary>
    public enum HoldState
    {
        /// <summary>Nothing accumulated.</summary>
        Idle,
        /// <summary>Inside the locked band, accumulating.</summary>
        Holding,
        /// <summary>Tracking dipped, within the grace window: the timer is frozen, not reset.</summary>
        Coasting,
        /// <summary>Tracking has been gone longer than the grace: progress is bleeding, never dumped.</summary>
        Stalled,
        /// <summary>Held long enough. Latched until the hand leaves the band for good.</summary>
        Locked,
    }

    /// <summary>Everything the caller needs to react to, returned instead of raised as events.</summary>
    public struct HoldUpdate
    {
        public bool LockedNow;      // this frame earned the lock (fires once per hold)
        public bool UnlockedNow;    // this frame released the latch (left the band for long enough)
        public bool ProgressLost;   // the accumulated hold reached zero
        public HoldState State;
        public float HeldSeconds;
    }

    /// <summary>
    /// The 0.7 s hold, with the two behaviours that decide whether hand tracking feels broken (P2.4).
    ///
    /// **A shape must be held, not touched.** So the timer only advances while the distance is inside
    /// the locked band - and it accumulates *cumulatively* rather than requiring an unbroken run, because
    /// a run-based timer punishes the jitter of the hand, not the shape of the hand.
    ///
    /// **A short dropout must never reset a nearly-successful attempt.** This is the single most common
    /// defect in hand-tracking apps: the tracker loses the hand for four frames, the app throws away
    /// 0.6 s of correct work, and the user - who did nothing wrong and cannot see the tracker - concludes
    /// that the app is broken. Three separate mechanisms cover it here:
    ///
    /// 1. a **grace window** (200 ms) in which a tracking dip freezes the timer completely;
    /// 2. beyond the grace, progress **decays** instead of being dumped, so a half-second loss costs
    ///    about a third of a second of progress rather than all of it;
    /// 3. leaving the *band* - which is a fact about the hand, not about the tracker - likewise freezes
    ///    for 500 ms before any progress is lost.
    ///
    /// Decay rather than freeze-forever is deliberate: freezing indefinitely means a user can hold a
    /// shape, lose tracking, move their hand somewhere else entirely, and lock the moment it is seen
    /// again. That is a false lock, and it is worse than asking for another 0.3 s.
    ///
    /// No engine dependencies: `Tick` takes numbers and returns a struct, so the same state machine can be
    /// replayed in `scripts/hold_sim.py` and checked by `SelfTest()` without a headset or an editor.
    /// </summary>
    public class HoldTimer
    {
        // ---- tuning (mirrored in scripts/hold_sim.py; changing one means changing both) ----
        public float holdSeconds = 0.7f;
        public float dropoutGraceSeconds = 0.2f;
        public float stallDecayPerSecond = 0.5f;
        public float outOfBandForgetSeconds = 0.5f;

        // ---- state ----
        public HoldState State { get; private set; } = HoldState.Idle;
        public float HeldSeconds { get; private set; }
        public float DropoutSeconds { get; private set; }
        public float OutOfBandSeconds { get; private set; }
        public bool IsLocked => State == HoldState.Locked;
        public float Progress01 => holdSeconds <= 0.0001f ? 1f : Mathf.Clamp01(HeldSeconds / holdSeconds);

        public void Reset()
        {
            State = HoldState.Idle;
            HeldSeconds = 0f;
            DropoutSeconds = 0f;
            OutOfBandSeconds = 0f;
        }

        /// <summary>
        /// Advance one frame.
        /// </summary>
        /// <param name="distanceDeg">Current distance to the target, in degrees.</param>
        /// <param name="trustworthy">False when tracking confidence is below the matcher's floor.</param>
        /// <param name="lockedDeg">Inside this the hold accumulates.</param>
        /// <param name="releaseEdgeDeg">Outside this, a lock starts counting towards release.
        /// Pass lockedDeg + the band release offset, so the latch and the displayed band agree.</param>
        /// <param name="dt">Frame time in seconds.</param>
        public HoldUpdate Tick(float distanceDeg, bool trustworthy, float lockedDeg,
                               float releaseEdgeDeg, float dt)
        {
            var result = new HoldUpdate();

            // ---------------------------------------------------------------- tracking lost
            if (!trustworthy)
            {
                DropoutSeconds += dt;

                if (DropoutSeconds <= dropoutGraceSeconds)
                {
                    // FROZEN. Every timer is untouched - this is the defect this class exists to avoid.
                    if (State != HoldState.Locked) State = HoldState.Coasting;
                    result.State = State;
                    result.HeldSeconds = HeldSeconds;
                    return result;
                }

                // Past the grace: bleed progress, never dump it. A latched lock survives a dropout
                // entirely, because the hand did not do anything wrong by leaving the frame.
                if (State == HoldState.Locked)
                {
                    result.State = State;
                    result.HeldSeconds = HeldSeconds;
                    return result;
                }

                OutOfBandSeconds = 0f;
                HeldSeconds -= stallDecayPerSecond * dt;
                if (HeldSeconds <= 0f)
                {
                    HeldSeconds = 0f;
                    State = HoldState.Idle;
                    result.ProgressLost = true;
                }
                else
                {
                    State = HoldState.Stalled;
                }

                result.State = State;
                result.HeldSeconds = HeldSeconds;
                return result;
            }

            DropoutSeconds = 0f;

            // ---------------------------------------------------------------- latched
            if (State == HoldState.Locked)
            {
                if (distanceDeg > releaseEdgeDeg)
                {
                    OutOfBandSeconds += dt;
                    if (OutOfBandSeconds >= outOfBandForgetSeconds)
                    {
                        // The hand really has left. Release the latch so the shape can be earned again.
                        State = HoldState.Idle;
                        HeldSeconds = 0f;
                        OutOfBandSeconds = 0f;
                        result.UnlockedNow = true;
                    }
                }
                else
                {
                    OutOfBandSeconds = 0f;   // came back: a wobble out of band is not a release
                }

                result.State = State;
                result.HeldSeconds = HeldSeconds;
                return result;
            }

            // ---------------------------------------------------------------- accumulating
            if (distanceDeg <= lockedDeg)
            {
                OutOfBandSeconds = 0f;
                HeldSeconds += dt;
                State = HoldState.Holding;

                if (HeldSeconds >= holdSeconds)
                {
                    HeldSeconds = holdSeconds;
                    State = HoldState.Locked;
                    OutOfBandSeconds = 0f;
                    result.LockedNow = true;
                }
            }
            else
            {
                // Outside the band but inside the grace: hold what was earned, wait to see if it
                // comes back. Only a sustained departure costs anything.
                OutOfBandSeconds += dt;
                State = HoldState.Holding;
                if (OutOfBandSeconds >= outOfBandForgetSeconds)
                {
                    HeldSeconds = 0f;
                    State = HoldState.Idle;
                    result.ProgressLost = true;
                }
            }

            result.State = State;
            result.HeldSeconds = HeldSeconds;
            return result;
        }

        // =================================================================== self test
        // The same cases live in scripts/hold_sim.py, which is where they are actually run in CI. This
        // mirror exists so the C# can be checked on a machine that has Unity: SIGNSPACE/P2.4 Hold timer
        // self-test. Two implementations of one specification is a cost; being unable to compile is worse.
        public static string SelfTest()
        {
            var failures = new System.Text.StringBuilder();
            int run = 0;

            bool Check(string name, bool ok)
            {
                run++;
                if (!ok) failures.Append("  FAIL ").Append(name).Append('\n');
                return ok;
            }

            HoldTimer T() => new HoldTimer { holdSeconds = 0.7f, dropoutGraceSeconds = 0.2f,
                                             stallDecayPerSecond = 0.5f, outOfBandForgetSeconds = 0.5f };

            const float dt = 1f / 72f;

            // 1. a clean hold locks exactly once, at the hold time
            {
                var t = T();
                int locks = 0; float at = -1f; float elapsed = 0f;
                for (int i = 0; i < 200; i++)
                {
                    var u = t.Tick(8f, true, 12f, 14f, dt); elapsed += dt;
                    if (u.LockedNow) { locks++; at = elapsed; }
                }
                Check("clean hold locks once", locks == 1);
                Check("clean hold locks at 0.7 s", at > 0.68f && at < 0.73f);
            }

            // 2. touching is not holding
            {
                var t = T();
                bool locked = false;
                for (int i = 0; i < 45; i++) if (t.Tick(8f, true, 12f, 14f, dt).LockedNow) locked = true;  // ~0.62 s
                for (int i = 0; i < 60; i++) t.Tick(30f, true, 12f, 14f, dt);
                Check("0.62 s touch does not lock", !locked);
            }

            // 3. a dip inside the grace window freezes: the hold survives untouched
            {
                var t = T();
                for (int i = 0; i < 40; i++) t.Tick(8f, true, 12f, 14f, dt);       // 0.55 s
                float before = t.HeldSeconds;
                for (int i = 0; i < 10; i++) t.Tick(8f, false, 12f, 14f, dt);      // 0.14 s dropout
                Check("grace freezes progress", Mathf.Abs(t.HeldSeconds - before) < 0.001f);
                bool locked = false;
                for (int i = 0; i < 20; i++) if (t.Tick(8f, true, 12f, 14f, dt).LockedNow) locked = true;
                Check("hold completes after a dip inside the grace", locked);
            }

            // 4. a 0.5 s dropout never resets: progress survives, and is not dumped
            {
                var t = T();
                for (int i = 0; i < 40; i++) t.Tick(8f, true, 12f, 14f, dt);       // 0.55 s
                bool lost = false;
                for (int i = 0; i < 36; i++) if (t.Tick(8f, false, 12f, 14f, dt).ProgressLost) lost = true;  // 0.5 s
                // 0.2 s frozen + 0.3 s at 0.5/s = 0.15 s lost, so 0.55 s of held work becomes 0.40 s:
                // nothing dumped, and it is still most of the attempt.
                Check("0.5 s dropout does not reset", !lost && t.HeldSeconds > 0.35f);
                Check("0.5 s dropout decays, does not freeze forever", t.HeldSeconds < 0.45f);
            }

            // 5. a long dropout does decay to zero, so a hand seen again elsewhere cannot false-lock
            {
                var t = T();
                for (int i = 0; i < 40; i++) t.Tick(8f, true, 12f, 14f, dt);
                for (int i = 0; i < 300; i++) t.Tick(40f, false, 12f, 14f, dt);    // 4 s untracked
                Check("long dropout ends in Idle", t.State == HoldState.Idle && t.HeldSeconds == 0f);
            }

            // 6. the lock latches once, and a wobble out of the band does not release it
            {
                var t = T();
                int locks = 0, unlocks = 0;
                for (int i = 0; i < 60; i++) if (t.Tick(8f, true, 12f, 14f, dt).LockedNow) locks++;
                for (int i = 0; i < 30; i++) if (t.Tick(20f, true, 12f, 14f, dt).UnlockedNow) unlocks++;  // 0.42 s out
                for (int i = 0; i < 60; i++) if (t.Tick(8f, true, 12f, 14f, dt).LockedNow) locks++;
                Check("lock fires once", locks == 1);
                Check("a 0.42 s excursion does not release", unlocks == 0 && t.IsLocked);
            }

            // 7. leaving for longer than the release window releases, and the shape can be earned again
            {
                var t = T();
                int locks = 0, unlocks = 0;
                for (int i = 0; i < 60; i++) if (t.Tick(8f, true, 12f, 14f, dt).LockedNow) locks++;
                for (int i = 0; i < 45; i++) if (t.Tick(30f, true, 12f, 14f, dt).UnlockedNow) unlocks++;  // 0.62 s
                for (int i = 0; i < 60; i++) if (t.Tick(8f, true, 12f, 14f, dt).LockedNow) locks++;
                Check("0.62 s out releases the latch once", unlocks == 1);
                Check("the shape can be earned again", locks == 2);
            }

            // 8. a hold that is never trustworthy never locks, however perfect the distance
            {
                var t = T();
                bool locked = false;
                for (int i = 0; i < 200; i++) if (t.Tick(0f, false, 12f, 14f, dt).LockedNow) locked = true;
                Check("no lock without tracking", !locked);
            }

            // 9. an excursion out of band shorter than the window costs nothing (cumulative, not run-based)
            {
                var t = T();
                for (int i = 0; i < 40; i++) t.Tick(8f, true, 12f, 14f, dt);       // 0.55 s
                for (int i = 0; i < 20; i++) t.Tick(20f, true, 12f, 14f, dt);      // 0.28 s out
                float kept = t.HeldSeconds;
                Check("a wobble out of band keeps progress", Mathf.Abs(kept - 0.55f) < 0.02f);
                bool locked = false;
                for (int i = 0; i < 20; i++) if (t.Tick(8f, true, 12f, 14f, dt).LockedNow) locked = true;
                Check("progress resumes after the wobble", locked);
            }

            return failures.Length == 0
                ? $"HoldTimer self-test: {run}/{run} passed"
                : $"HoldTimer self-test: {run - CountLines(failures.ToString())}/{run} passed\n{failures}";
        }

        static int CountLines(string s)
        {
            int n = 0;
            foreach (char c in s) if (c == '\n') n++;
            return n;
        }
    }
}
