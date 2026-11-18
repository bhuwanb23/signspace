using System;
using UnityEngine;

namespace Signspace.Matching
{
    /// <summary>
    /// The session state machine.
    ///
    /// ARCHITECTURE.md section 2. Every state here is informative or waiting.
    /// THERE IS NO FAILURE STATE, and that is a structural property of the
    /// product, not a UI choice: no red, no X, no "try again", no error toast.
    /// If a design idea needs a failure state, the idea is wrong.
    ///
    /// See docs/04-design/UX-STATES.md for the full list and the copy budget.
    /// </summary>
    public enum AppState
    {
        Boot,
        HandsMissing,
        TrackingLow,
        Ready,
        Inspect,
        Gazing,
        Rotating,
        Warming,
        Close,
        Locking,
        Locked,
        NearMiss,
        Paused,
        SurfaceFound,
        Plinth,
        ShelfFull,
        NoEyes,
        Credits
    }

    /// <summary>
    /// Owns which screen/mode is active. Knows nothing spatial, and knows
    /// nothing about the SDK.
    /// </summary>
    public class AppStateMachine : MonoBehaviour
    {
        public AppState Current { get; private set; } = AppState.Boot;
        public AppState Previous { get; private set; } = AppState.Boot;

        public event Action<AppState, AppState> OnStateChanged;

        [Tooltip("Seconds without a tracked hand before the app says so.")]
        public float handsMissingDelay = 3f;

        float _handsMissingTimer;

        /// <summary>Moves to a new state. Ignores a move to the state we are already in.</summary>
        public void Go(AppState next)
        {
            if (next == Current) return;
            Previous = Current;
            Current = next;
            OnStateChanged?.Invoke(Previous, Current);
        }

        /// <summary>Returns to the state we were in before the last change.</summary>
        public void Back()
        {
            if (Previous == Current) return;
            var back = Previous;
            Previous = Current;
            Current = back;
            OnStateChanged?.Invoke(Previous, Current);
        }

        /// <summary>
        /// Tracks how long the hands have been missing. Called by the session
        /// controller; the app only shows the dim outlines once the delay has
        /// passed, so a brief tracking blip never flashes a warning.
        /// </summary>
        public void UpdateHandsMissing(bool handsPresent, float dt)
        {
            if (handsPresent)
            {
                _handsMissingTimer = 0f;
                return;
            }
            _handsMissingTimer += dt;
            if (_handsMissingTimer >= handsMissingDelay && Current == AppState.Ready)
            {
                Go(AppState.HandsMissing);
            }
        }
    }
}
