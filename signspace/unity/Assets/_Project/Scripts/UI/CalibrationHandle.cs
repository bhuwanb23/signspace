using UnityEngine;
using UnityEngine.UI;
using Signspace.Hands;
using Signspace.Matching;

namespace Signspace.UI
{
    /// <summary>
    /// The calibrate block (P6.4 step 5: the flow can be re-run). Pinch starts it, pinch again skips it.
    ///
    /// It is on the same tray as the toggles because that is where a user looks when something is
    /// wrong with the input. Nothing starts a calibration without being asked: the default path is
    /// always available, and a flow that begins by itself on launch would be a modal trap for exactly
    /// the user it is meant to help.
    /// </summary>
    [RequireComponent(typeof(Grabbable))]
    public class CalibrationHandle : MonoBehaviour
    {
        public Calibration calibration;
        public Text title;
        public Text value;

        Grabbable _grabbable;

        void Awake()
        {
            _grabbable = GetComponent<Grabbable>();
            if (_grabbable != null) _grabbable.OnGrabbed += OnGrabbed;
        }

        void OnDestroy()
        {
            if (_grabbable != null) _grabbable.OnGrabbed -= OnGrabbed;
        }

        void OnGrabbed(MetaHandSource hand) => Flip();

        public void Flip()
        {
            if (calibration == null) calibration = FindFirstObjectByType<Calibration>();
            if (calibration == null)
            {
                Debug.LogWarning("[CalibrationHandle] no Calibration in the scene.");
                return;
            }

            if (calibration.IsRunning) calibration.Skip();
            else calibration.Begin();
        }

        void Update()
        {
            if (value == null) return;
            if (calibration == null)
            {
                value.text = "unavailable";
                return;
            }

            if (calibration.IsRunning) value.text = Mathf.CeilToInt(calibration.StepSecondsLeft) + " s";
            else value.text = calibration.HasResult ? "re-run" : "begin";
        }
    }
}
