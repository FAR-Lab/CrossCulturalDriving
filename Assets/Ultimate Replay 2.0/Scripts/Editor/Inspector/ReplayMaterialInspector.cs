using UnityEditor;
namespace UltimateReplay
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ReplayMaterial))]
    public class ReplayMaterialInspector : ReplayRecordableBehaviourInspector
    {
        // Private
#pragma warning disable 0414
        private ReplayMaterial targetMaterial = null;
#pragma warning restore 0414
        private ReplayMaterial[] targetMaterials = null;

        // Methods
        public override void OnEnable()
        {
            base.OnEnable();

            // Get correct type instances
            GetTargetInstances(out targetMaterial, out targetMaterials);
        }

        public override void OnInspectorGUI()
        {
            // Display inspector
            DisplayDefaultInspectorProperties();

            // Get values
            bool[] sharedMaterial = GetFlagValuesForTargets(ReplayMaterial.ReplayMaterialFlags.SharedMaterial);
            bool[] color = GetFlagValuesForTargets(ReplayMaterial.ReplayMaterialFlags.Color);
            bool[] mainTextureOffset = GetFlagValuesForTargets(ReplayMaterial.ReplayMaterialFlags.MainTextureOffset);
            bool[] mainTextureScale = GetFlagValuesForTargets(ReplayMaterial.ReplayMaterialFlags.MainTextureScale);
            bool[] doubleSidedGI = GetFlagValuesForTargets(ReplayMaterial.ReplayMaterialFlags.DoubleSidedGlobalIllumination);
            bool[] globalIlluminationFlags = GetFlagValuesForTargets(ReplayMaterial.ReplayMaterialFlags.GlobalIlluminationFlags);
            bool[] interpolate = GetFlagValuesForTargets(ReplayMaterial.ReplayMaterialFlags.Interpolate);

            // Display fields
            bool changed = DisplayMultiEditableToggleField("Shared Material", "Should the shared material instance be recorded or the non-shared material instance", ref sharedMaterial);
            changed |= DisplayMultiEditableToggleField("Replay Color", "Record the material color", ref color);
            changed |= DisplayMultiEditableToggleField("Replay Main Texture Offset", "Record the uv offset of the main texture", ref mainTextureOffset);
            changed |= DisplayMultiEditableToggleField("Replay Main Texture Scale", "Record the scale of the main texture", ref mainTextureScale);
            changed |= DisplayMultiEditableToggleField("Replay Double Sided GI", "Record the double sided GI parameter of the material", ref doubleSidedGI);
            changed |= DisplayMultiEditableToggleField("Replay Global Illumination Flags", "Record the global illumination flags of the material", ref globalIlluminationFlags);
            changed |= DisplayMultiEditableToggleField("Interpolate", "Interpolate supported material properties such as material", ref interpolate);

            // Set flags values
            SetFlagValuesForTargets(ReplayMaterial.ReplayMaterialFlags.SharedMaterial, sharedMaterial);
            SetFlagValuesForTargets(ReplayMaterial.ReplayMaterialFlags.Color, color);
            SetFlagValuesForTargets(ReplayMaterial.ReplayMaterialFlags.MainTextureOffset, mainTextureOffset);
            SetFlagValuesForTargets(ReplayMaterial.ReplayMaterialFlags.MainTextureScale, mainTextureScale);
            SetFlagValuesForTargets(ReplayMaterial.ReplayMaterialFlags.DoubleSidedGlobalIllumination, doubleSidedGI);
            SetFlagValuesForTargets(ReplayMaterial.ReplayMaterialFlags.GlobalIlluminationFlags, globalIlluminationFlags);
            SetFlagValuesForTargets(ReplayMaterial.ReplayMaterialFlags.Interpolate, interpolate);

            // Display replay stats
            DisplayReplayStorageStatistics();

            // Check for changed
            if (changed == true)
            {
                foreach (UnityEngine.Object obj in targets)
                    EditorUtility.SetDirty(obj);
            }
        }

        private bool[] GetFlagValuesForTargets(ReplayMaterial.ReplayMaterialFlags flag)
        {
            bool[] values = new bool[targetMaterials.Length];

            for (int i = 0; i < targetMaterials.Length; i++)
            {
                values[i] = (targetMaterials[i].recordFlags & flag) != 0;
            }

            return values;
        }

        private void SetFlagValuesForTargets(ReplayMaterial.ReplayMaterialFlags flag, bool[] toggleValues)
        {
            for (int i = 0; i < targetMaterials.Length; i++)
            {
                if (toggleValues[i] == true) 
                    targetMaterials[i].recordFlags |= flag;
                else targetMaterials[i].recordFlags &= ~flag;
            }
        }
    }
}

