using UnityEditor;

namespace UltimateReplay
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ReplayMaterialChange))]
    public class ReplayMaterialChangeInspector : ReplayRecordableBehaviourInspector
    {
        // Private
#pragma warning disable 0414
        private ReplayMaterialChange targetMaterialChange = null;
#pragma warning restore 0414
        private ReplayMaterialChange[] targetMaterialChanges = null;

        // Methods
        public override void OnEnable()
        {
            base.OnEnable();

            // Get correct type instances
            GetTargetInstances(out targetMaterialChange, out targetMaterialChanges);
        }

        public override void OnInspectorGUI()
        {
            DisplayDefaultInspectorProperties();

            // Get values
            bool[] sharedMaterial = GetFlagValuesForTargets(ReplayMaterialChange.ReplayMaterialChangeFlags.SharedMaterial);
            bool[] allMaterials = GetFlagValuesForTargets(ReplayMaterialChange.ReplayMaterialChangeFlags.AllMaterials);

            // Display fields
            bool changed = DisplayMultiEditableToggleField("Shared Material", "Should the shared material instance be recorded or the non-shared material instance", ref sharedMaterial);
            changed |= DisplayMultiEditableToggleField("Replay All Materials", "Should all renderer materials be recorded", ref allMaterials);

            // Set flags values
            SetFlagValuesForTargets(ReplayMaterialChange.ReplayMaterialChangeFlags.SharedMaterial, sharedMaterial);
            SetFlagValuesForTargets(ReplayMaterialChange.ReplayMaterialChangeFlags.AllMaterials, allMaterials);

            // Display replay stats
            DisplayReplayStorageStatistics();

            // Check for changed
            if(changed == true)
            {
                foreach (UnityEngine.Object obj in targets)
                    EditorUtility.SetDirty(obj);
            }
        }

        private bool[] GetFlagValuesForTargets(ReplayMaterialChange.ReplayMaterialChangeFlags flag)
        {
            bool[] values = new bool[targetMaterialChanges.Length];

            for (int i = 0; i < targetMaterialChanges.Length; i++)
            {
                values[i] = (targetMaterialChanges[i].recordFlags & flag) != 0;
            }

            return values;
        }

        private void SetFlagValuesForTargets(ReplayMaterialChange.ReplayMaterialChangeFlags flag, bool[] toggleValues)
        {
            for (int i = 0; i < targetMaterialChanges.Length; i++)
            {
                if (toggleValues[i] == true) targetMaterialChanges[i].recordFlags |= flag;
                else targetMaterialChanges[i].recordFlags &= ~flag;
            }
        }
    }
}
