using UnityEditor;
using UnityEngine;
using Signspace.Matching;

namespace Signspace.Editor
{
    /// <summary>
    /// Runs HoldTimer's self-test inside Unity (P2.4).
    ///
    /// `scripts/hold_sim.py` is the same nine cases, and it is the copy that runs in CI - but CI has no
    /// compiler here, so the Python mirror proves the *specification* is right and this menu item is the
    /// only thing that can prove the *shipped C#* matches it. The self-test is deliberately a plain
    /// static method returning a string: no NUnit, no test runner, nothing to install, and it runs in the
    /// editor before a build rather than after it.
    /// </summary>
    public static class HoldTimerCheck
    {
        [MenuItem("SIGNSPACE/P2.4 Hold timer self-test")]
        public static void RunSelfTest()
        {
            string result = HoldTimer.SelfTest();
            if (result.Contains("FAIL"))
                Debug.LogError("[HoldTimer] " + result);
            else
                Debug.Log("[HoldTimer] " + result);
        }

        [MenuItem("SIGNSPACE/P2.4 Where is the lock log")]
        public static void WhereIsTheLog()
        {
            var logger = Object.FindFirstObjectByType<Signspace.Core.LockLogger>();
            if (logger == null)
            {
                Debug.LogWarning("[HoldTimer] no LockLogger in the scene. Run SIGNSPACE/Build Main Scene.");
                return;
            }
            Debug.Log($"[HoldTimer] lock log: {logger.LogPath}  ({logger.LockCount} locks this session)");
        }
    }
}
