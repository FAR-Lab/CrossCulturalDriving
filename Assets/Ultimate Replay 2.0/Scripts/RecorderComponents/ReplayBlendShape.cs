using System;
using System.Collections.Generic;
using UltimateReplay.Serializers;
using UnityEngine;

// Credit to Richdog567 for initially developing this component.

namespace UltimateReplay
{
    [ReplaySerializer(typeof(ReplayBlendShapeSerializer))]
    public class ReplayBlendShape : ReplayRecordableBehaviour
    {
        // Types
        [Flags]
        public enum ReplayBlendShapeFlags
        {
            None = 0,
            Interpolate = 1 << 1,
        }

        // Private
        private ReplayBlendShapeSerializer serializer = new ReplayBlendShapeSerializer();

        private List<float> lastWeights = new List<float>();
        private List<float> targetWeights = new List<float>();

        // Public
        public SkinnedMeshRenderer observedSkinnedMeshRenderer;
        [HideInInspector]
        public ReplayBlendShapeFlags updateFlags = ReplayBlendShapeFlags.Interpolate;

        // Methods
        public void Start()
        {
            if (observedSkinnedMeshRenderer == null)
                Debug.LogWarningFormat("Replay blend shape '{0}' will not record or replay because the observed skinned mesh renderer has not been assigned", this);
        }

        public override void Reset()
        {
            // Call base method
            base.Reset();

            // Try to auto-find skinned mesh renderer
            if (observedSkinnedMeshRenderer == null)
                observedSkinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        }

        public override void OnReplayReset()
        {
            lastWeights.Clear();
            lastWeights.AddRange(targetWeights);
        }

        public override void OnReplayUpdate(ReplayTime replayTime)
        {
            // Check for no component
            if (observedSkinnedMeshRenderer == null || observedSkinnedMeshRenderer.enabled == false)
                return;

            // Get the array size
            int weightsCount = observedSkinnedMeshRenderer.sharedMesh.blendShapeCount;

            // Set all points
            if (targetWeights.Count >= weightsCount)
            {
                for (int i = 0; i < weightsCount; i++)
                {
                    // Get the target weight
                    float updateWeight = targetWeights[i];

                    // Check for interpolate
                    if((updateFlags & ReplayBlendShapeFlags.Interpolate) != 0 && lastWeights.Count == targetWeights.Count)
                    {
                        // Interpolate the value
                        updateWeight = Mathf.Lerp(lastWeights[i], updateWeight, replayTime.Delta);
                    }

                    // Set weight
                    observedSkinnedMeshRenderer.SetBlendShapeWeight(i, updateWeight);
                }
            }
        }

        public override void OnReplaySerialize(ReplayState state)
        {
            // Check for no component
            if (observedSkinnedMeshRenderer == null || observedSkinnedMeshRenderer.enabled == false)
                return;

            // Reset serializer
            serializer.Reset();

            int blendShapeCount = observedSkinnedMeshRenderer.sharedMesh.blendShapeCount;

            for (int i = 0; i < blendShapeCount; i++)
            {
                float weight = observedSkinnedMeshRenderer.GetBlendShapeWeight(i);
                serializer.BlendWeights.Add(weight);
            }

            // Run the serializer
            serializer.OnReplaySerialize(state);
        }

        public override void OnReplayDeserialize(ReplayState state)
        {
            // Check for no component
            if (observedSkinnedMeshRenderer == null)
                return;

            // Run the serializer
            serializer.OnReplayDeserialize(state);

            // Get weights
            targetWeights.Clear();
            targetWeights.AddRange(serializer.BlendWeights);
        }

        
    }
}