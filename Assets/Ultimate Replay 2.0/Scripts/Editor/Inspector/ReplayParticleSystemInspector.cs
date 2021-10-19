using UnityEngine;
using UnityEditor;

namespace UltimateReplay
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ReplayParticleSystem))]
    public class ReplayParticleSystemInspector : ReplayRecordableBehaviourInspector
    {
        // Private
#pragma warning disable 0414
        private ReplayParticleSystem targetParticleSystem = null;
#pragma warning restore 0414
        private ReplayParticleSystem[] targetParticleSystems = null;

        // Methods
        public override void OnEnable()
        {
            base.OnEnable();

            GetTargetInstances(out targetParticleSystem, out targetParticleSystems);
        }

        public override void OnInspectorGUI()
        {
            DisplayDefaultInspectorProperties();


            // Get flag values
            bool[] interpolate = GetFlagValuesForTarget(ReplayParticleSystem.ReplayParticleSystemFlags.Interpolate);

            // Display field
            DisplayMultiEditableToggleField("Interpolate", "Should simulation time value be interpolated", ref interpolate);

            // Apply changedd values
            SetFlagValuesForTargets(ReplayParticleSystem.ReplayParticleSystemFlags.Interpolate, interpolate);


            // Display replay stats
            DisplayReplayStorageStatistics();
        }

        private bool[] GetFlagValuesForTarget(ReplayParticleSystem.ReplayParticleSystemFlags flag)
        {
            bool[] values = new bool[targetParticleSystems.Length];

            for (int i = 0; i < targetParticleSystems.Length; i++)
            {
                values[i] = (targetParticleSystems[i].updateFlags & flag) != 0;
            }

            return values;
        }

        private void SetFlagValuesForTargets(ReplayParticleSystem.ReplayParticleSystemFlags flag, bool[] toggleValues)
        {
            for (int i = 0; i < targetParticleSystems.Length; i++)
            {
                if (toggleValues[i] == true) targetParticleSystems[i].updateFlags |= flag;
                else targetParticleSystems[i].updateFlags &= ~flag;
            }
        }
    }
}
