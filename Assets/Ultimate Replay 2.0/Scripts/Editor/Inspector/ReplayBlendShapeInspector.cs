using UnityEditor;

namespace UltimateReplay
{
    [CustomEditor(typeof(ReplayBlendShape))]
    public class ReplayBlendShapeInspector : ReplayRecordableBehaviourInspector
    {
        // Methods
        public override void OnInspectorGUI()
        {
            DisplayDefaultInspectorProperties();

            // Get values
            bool[] interpolate = GetFlagValuesForTargets((int)ReplayBlendShape.ReplayBlendShapeFlags.Interpolate);

            // Display fields
            bool changed = DisplayMultiEditableToggleField("Interpolate", "Should the blend weights be interpolated", ref interpolate);

            // Apply changes
            SetFlagValuesForTargets((int)ReplayBlendShape.ReplayBlendShapeFlags.Interpolate, interpolate);


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
                if (targetBehaviours[i] is ReplayBlendShape)
                {
                    values[i] = ((targetBehaviours[i] as ReplayBlendShape).updateFlags & (ReplayBlendShape.ReplayBlendShapeFlags)flag) != 0;
                }
            }

            return values;
        }

        private void SetFlagValuesForTargets(int flag, bool[] toggleValues)
        {
            for (int i = 0; i < targetBehaviours.Length; i++)
            {
                if (targetBehaviours[i] is ReplayBlendShape)
                {
                    if (toggleValues[i] == true) (targetBehaviours[i] as ReplayBlendShape).updateFlags |= (ReplayBlendShape.ReplayBlendShapeFlags)flag;
                    else (targetBehaviours[i] as ReplayBlendShape).updateFlags &= ~(ReplayBlendShape.ReplayBlendShapeFlags)flag;
                }
            }
        }
    }
}
