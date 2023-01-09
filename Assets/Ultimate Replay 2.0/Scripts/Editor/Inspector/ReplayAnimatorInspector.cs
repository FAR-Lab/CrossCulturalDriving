using UnityEngine;
using UnityEditor;

namespace UltimateReplay
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ReplayAnimator))]
    public class ReplayAnimatorInspector : ReplayRecordableBehaviourInspector
    {
        // Private
        private ReplayAnimator targetAnimator = null;
        private ReplayAnimator[] targetAnimators = null;

        // Methods
        public override void OnEnable()
        {
            base.OnEnable();

            // Get components
            GetTargetInstances(out targetAnimator, out targetAnimators);
        }

        public override void OnInspectorGUI()
        {
            DisplayDefaultInspectorProperties();

            bool[] mainState = GetFlagValuesForTargets(ReplayAnimator.ReplayAnimatorFlags.MainState);
            bool[] subStates = GetFlagValuesForTargets(ReplayAnimator.ReplayAnimatorFlags.SubStates);
            bool[] interpolate = GetFlagValuesForTargets(ReplayAnimator.ReplayAnimatorFlags.InterpolateStates);
            bool[] lowPrecision = GetFlagValuesForTargets(ReplayAnimator.ReplayAnimatorFlags.LowPrecision);
            bool[] parameters = GetFlagValuesForTargets(ReplayAnimator.ReplayAnimatorFlags.Parameters);
            bool[] intParams = GetFlagValuesForTargets(ReplayAnimator.ReplayAnimatorFlags.InterpolateIntParameters);
            bool[] floatParams = GetFlagValuesForTargets(ReplayAnimator.ReplayAnimatorFlags.InterpolateFloatParameters);

            // Display fields
            bool changed = DisplayMultiEditableToggleField("Replay Main Layer", "Record the main layer of the Animator", ref mainState);
            changed |= DisplayMultiEditableToggleField("Replay Sub Layers", "Record all sub layers of the Animator", ref subStates);
            changed |= DisplayMultiEditableToggleField("Interpolate", "Interpolate the animator time values", ref interpolate);
            changed |= DisplayMultiEditableToggleField("Low Precision", "Record supported data in low precision to reduce storage usage. Not recommended for main objects such as player", ref lowPrecision);
            changed |= DisplayMultiEditableToggleField("Replay Parameters", "Record Animator parameter values", ref parameters);

            if (AnyValuesSet(parameters, true) == true)
            {
                DisplayMultiEditableToggleField("Interpolate Int Parameters", "Should integer parameters be interpolated during playback", ref intParams);
                DisplayMultiEditableToggleField("Interpolate Float Parameters", "Should float parameters be interpolated during playback", ref floatParams);
            }

            // Set flag values
            SetFlagValuesForTargets(ReplayAnimator.ReplayAnimatorFlags.MainState, mainState);
            SetFlagValuesForTargets(ReplayAnimator.ReplayAnimatorFlags.SubStates, subStates);
            SetFlagValuesForTargets(ReplayAnimator.ReplayAnimatorFlags.InterpolateStates, interpolate);
            SetFlagValuesForTargets(ReplayAnimator.ReplayAnimatorFlags.LowPrecision, lowPrecision);
            SetFlagValuesForTargets(ReplayAnimator.ReplayAnimatorFlags.Parameters, parameters);
            SetFlagValuesForTargets(ReplayAnimator.ReplayAnimatorFlags.InterpolateIntParameters, intParams);
            SetFlagValuesForTargets(ReplayAnimator.ReplayAnimatorFlags.InterpolateFloatParameters, floatParams);
            

            // Display storage info
            ReplayStorageStats.DisplayStorageStats(targetAnimator);

            // Check for changed
            if (changed == true)
            {
                foreach (UnityEngine.Object obj in targets)
                    EditorUtility.SetDirty(obj);
            }
        }

        private bool[] GetFlagValuesForTargets(ReplayAnimator.ReplayAnimatorFlags flag)
        {
            bool[] values = new bool[targetAnimators.Length];

            for(int i = 0; i < targetAnimators.Length; i++)
            {
                values[i] = (targetAnimators[i].recordFlags & flag) != 0;
            }

            return values;
        }

        private void SetFlagValuesForTargets(ReplayAnimator.ReplayAnimatorFlags flag, bool[] toggleValues)
        {
            for(int i = 0; i < toggleValues.Length; i++)
            {
                if (toggleValues[i] == true) targetAnimators[i].recordFlags |= flag;
                else targetAnimators[i].recordFlags &= ~flag;
            }
        }

        private bool AnyValuesSet(bool[] toggleValues, bool targetValue)
        {
            foreach (bool val in toggleValues)
                if (val == targetValue)
                    return true;

            return false;
        }
    }
}
