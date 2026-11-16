using UnityEditor;
using UnityEngine;
using Signspace.Matching;

namespace Signspace.Editor
{
    /// <summary>
    /// Numeric proof that the reference hand is a hand and not a bundle of fingers.
    ///
    /// WHY THIS EXISTS
    /// ---------------
    /// P2's closing report recorded a debt: HandVisual never set a joint's localPosition, so every
    /// finger chain began at the palm's centre. It was invisible to the metric (rotations only) and
    /// invisible to a compile, and there is no headset to look at it with - so the regression that
    /// matters is "do the fingertips occupy different points in space", which is a number.
    ///
    /// Run from the menu, or in batch:
    ///   Unity -batchmode -quit -executeMethod Signspace.Editor.HandRigCheck.Run
    /// </summary>
    public static class HandRigCheck
    {
        [MenuItem("SIGNSPACE/P1.1 Hand rig check")]
        public static void Run()
        {
            var go = new GameObject("HandRigCheck");
            try
            {
                var visual = go.AddComponent<HandVisual>();
                visual.scale = 1f;
                visual.Build();

                var joints = visual.joints;
                var tips = new[] { "IndexTip", "MiddleTip", "RingTip", "LittleTip" };

                float baseSpread = Distance(joints, "IndexMetacarpal", "LittleMetacarpal");
                float proximalSpread = Distance(joints, "IndexProximal", "MiddleProximal");
                float tipSpread = MinTipDistance(joints, tips);
                float span = Distance(joints, "Wrist", "MiddleTip");

                // The old rig scored 0.000 on all three spreads: every joint was at the palm's centre.
                bool basesSpread = baseSpread > 0.030f;
                bool chainsSeparate = proximalSpread > 0.010f;
                // Fingers held together are ~15-20 mm apart at the tips, so 10 mm is the guard
                // against the real failure (all joints at one point), not an ideal hand.
                bool tipsSeparate = tipSpread > 0.010f;
                bool plausibleHand = span > 0.120f && span < 0.220f;

                Debug.Log("[HandRigCheck] base spread " + baseSpread.ToString("F3") +
                          " m (need > 0.030) " + (basesSpread ? "PASS" : "FAIL"));
                Debug.Log("[HandRigCheck] proximal spread " + proximalSpread.ToString("F3") +
                          " m (need > 0.010) " + (chainsSeparate ? "PASS" : "FAIL"));
                Debug.Log("[HandRigCheck] closest pair of tips " + tipSpread.ToString("F3") +
                          " m (need > 0.010) " + (tipsSeparate ? "PASS" : "FAIL"));
                Debug.Log("[HandRigCheck] wrist to middle tip " + span.ToString("F3") +
                          " m (need 0.120-0.220) " + (plausibleHand ? "PASS" : "FAIL"));

                bool ok = basesSpread && chainsSeparate && tipsSeparate && plausibleHand;
                Debug.Log("[HandRigCheck] RESULT: " + (ok ? "PASS" : "FAIL"));

                if (Application.isBatchMode && !ok)
                    EditorApplication.Exit(1);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        static float Distance(Transform[] joints, string a, string b)
        {
            int ia = JointNames.IndexOf(a);
            int ib = JointNames.IndexOf(b);
            if (ia < 0 || ib < 0 || joints == null || ia >= joints.Length || ib >= joints.Length) return 0f;
            if (joints[ia] == null || joints[ib] == null) return 0f;
            return Vector3.Distance(joints[ia].position, joints[ib].position);
        }

        static float MinTipDistance(Transform[] joints, string[] tips)
        {
            float min = float.MaxValue;
            for (int i = 0; i < tips.Length; i++)
            {
                for (int j = i + 1; j < tips.Length; j++)
                {
                    float d = Distance(joints, tips[i], tips[j]);
                    if (d < min) min = d;
                }
            }
            return min == float.MaxValue ? 0f : min;
        }
    }
}
