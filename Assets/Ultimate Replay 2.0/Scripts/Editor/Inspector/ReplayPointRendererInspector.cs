#if UNITY_2018_2_OR_NEWER
#define UR_TRAILRENDERER
#endif

using UnityEditor;

namespace UltimateReplay
{
#if UR_TRAILRENDERER
    [CustomEditor(typeof(ReplayTrailRenderer))]
    public class ReplayTrailPointRendererInspector : ReplayPointRendererInspector
    { }
#endif

    [CustomEditor(typeof(ReplayLineRenderer))]
    public class ReplayPointRendererInspector : ReplayRecordableBehaviourInspector
    {
        // Methods
        public override void OnInspectorGUI()
        {
            DisplayDefaultInspectorProperties();

            // Get the flag value
            int flag = (targetBehaviour is ReplayLineRenderer)
                ? (int)ReplayLineRenderer.ReplayLineRendererFlags.Interpolate
#if UR_TRAILRENDERER
                : (int)ReplayTrailRenderer.ReplayTrailRendererFlags.Interpolate;
#else
                : 0;
#endif

            // Get values
            bool[] interpolate = GetFlagValuesForTargets(flag);

            // Display fields
            bool changed = DisplayMultiEditableToggleField("Interpolate", "Should the point positions be interpolated", ref interpolate);

            // Apply changes
            SetFlagValuesForTargets(flag, interpolate);


            DisplayReplayStorageStatistics();

            // Check for changed
            if (changed == true)
            {
                foreach (UnityEngine.Object obj in targets)
                    EditorUtility.SetDirty(obj);
            }
        }

        private bool[] GetFlagValuesForTargets(int flag)
        {
            bool[] values = new bool[targetBehaviours.Length];

            for (int i = 0; i < targetBehaviours.Length; i++)
            {
                if (targetBehaviours[i] is ReplayLineRenderer)
                {
                    values[i] = ((targetBehaviours[i] as ReplayLineRenderer).updateFlags & (ReplayLineRenderer.ReplayLineRendererFlags)flag) != 0;
                }

#if UR_TRAILRENDERER
                if(targetBehaviours[i] is ReplayTrailRenderer)
                {
                    values[i] = ((targetBehaviours[i] as ReplayTrailRenderer).updateFlags & (ReplayTrailRenderer.ReplayTrailRendererFlags)flag) != 0;
                }
#endif
            }

            return values;
        }

        private void SetFlagValuesForTargets(int flag, bool[] toggleValues)
        {
            for (int i = 0; i < targetBehaviours.Length; i++)
            {
                if (targetBehaviours[i] is ReplayLineRenderer)
                {
                    if (toggleValues[i] == true) (targetBehaviours[i] as ReplayLineRenderer).updateFlags |= (ReplayLineRenderer.ReplayLineRendererFlags)flag;
                    else (targetBehaviours[i] as ReplayLineRenderer).updateFlags &= ~(ReplayLineRenderer.ReplayLineRendererFlags)flag;
                }

#if UR_TRAILRENDERER
                if (targetBehaviours[i] is ReplayTrailRenderer)
                {
                    if (toggleValues[i] == true) (targetBehaviours[i] as ReplayTrailRenderer).updateFlags |= (ReplayTrailRenderer.ReplayTrailRendererFlags)flag;
                    else (targetBehaviours[i] as ReplayTrailRenderer).updateFlags &= ~(ReplayTrailRenderer.ReplayTrailRendererFlags)flag;
                }
#endif
            }
        }
    }
}
