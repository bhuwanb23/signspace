using System;
using Signspace.Matching;
using UnityEditor;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>Checks event argument order on the actual state machine; run in the Unity editor.</summary>
    public static class CoreStateChecks
    {
        [MenuItem("SIGNSPACE/Audit/Check state change events")]
        public static void Run()
        {
            var go = new GameObject("StateEventCheck");
            try
            {
                var machine = go.AddComponent<AppStateMachine>();
                AppState from = AppState.Boot, to = AppState.Boot;
                int calls = 0;
                machine.OnStateChanged += (oldState, newState) => { from = oldState; to = newState; calls++; };
                machine.Go(AppState.Ready);
                if (calls != 1 || from != AppState.Boot || to != AppState.Ready)
                    throw new InvalidOperationException("Go() event did not report Boot -> Ready");
                machine.Back();
                if (calls != 2 || from != AppState.Ready || to != AppState.Boot)
                    throw new InvalidOperationException("Back() event did not report Ready -> Boot");
                Debug.Log("[CoreStateChecks] state change event order passed.");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
