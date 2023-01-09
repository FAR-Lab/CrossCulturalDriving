
// Only available in 2018.2 or newer due to 'SetPositions' not existing in older versions
#if UNITY_2018_2_OR_NEWER
using System;
using System.Collections.Generic;
using UltimateReplay.Serializers;
using UnityEngine;


namespace UltimateReplay
{
    [ReplaySerializer(typeof(ReplayPointRendererSerializer))]
    public class ReplayTrailRenderer : ReplayRecordableBehaviour
    {
        // Types
        [Flags]
        public enum ReplayTrailRendererFlags
        {
            None = 0,
            Interpolate = 1 << 1,
        }

        // Private
        private static ReplayPointRendererSerializer sharedSerializer = new ReplayPointRendererSerializer();

        private List<Vector3> lastPoints = new List<Vector3>();
        private List<Vector3> targetPoints = new List<Vector3>();

        // Public
        public TrailRenderer observedTrailRenderer = null;
        [HideInInspector]
        public ReplayTrailRendererFlags updateFlags = ReplayTrailRendererFlags.Interpolate;
        public bool clearOnReplayEnd = true;

        // Methods
        public void Start()
        {
            if (observedTrailRenderer == null)
                Debug.LogWarningFormat("Replay trail renderer '{0}' will not record or replay because the observed trail renderer has not been assigned", this);
        }

        public override void OnReplayReset()
        {
            lastPoints.Clear();
            lastPoints.AddRange(targetPoints);
        }

        public override void OnReplayEnd()
        {
            if (clearOnReplayEnd == true)
                observedTrailRenderer.Clear();
        }

        public override void OnReplayUpdate(ReplayTime replayTime)
        {
            // Check for no component
            if (observedTrailRenderer == null || observedTrailRenderer.enabled == false)
                return;

            // Check for point changes
            if (targetPoints.Count != observedTrailRenderer.positionCount)
            {
                int requiredCount = targetPoints.Count;

                // Remove all items
                //if (targetPoints.Count > observedTrailRenderer.positionCount)
                    observedTrailRenderer.Clear();

                for (int i = 0; i < requiredCount; i++)
                    observedTrailRenderer.AddPosition(Vector3.zero);

                // Add items
                //while (targetPoints.Count < observedTrailRenderer.positionCount)
                //    targetPoints.Add(Vector3.zero);

            }

            // Set all points
            for (int i = 0; i < targetPoints.Count; i++)
            {
                // Get the target point
                Vector3 updatePoint = targetPoints[i];

                // Check for interpolate
                if ((updateFlags & ReplayTrailRendererFlags.Interpolate) != 0 && lastPoints.Count == targetPoints.Count)
                {
                    // Interpolate the value
                    updatePoint = Vector3.Lerp(lastPoints[i], updatePoint, replayTime.Delta);
                }

                // Set point
                observedTrailRenderer.SetPosition(i, updatePoint);
            }
        }

        public override void OnReplaySerialize(ReplayState state)
        {
            // Check for no component
            if (observedTrailRenderer == null || observedTrailRenderer.enabled == false)
                return;

            // Reset serializer
            sharedSerializer.Reset();

            // Fill points
            for(int i = 0; i < observedTrailRenderer.positionCount; i++)
            {
                // Get the point
                sharedSerializer.Points.Add(observedTrailRenderer.GetPosition(i));
            }

            // Run the serializer
            sharedSerializer.OnReplaySerialize(state);
        }

        public override void OnReplayDeserialize(ReplayState state)
        {
            // Check for no component
            if (observedTrailRenderer == null || observedTrailRenderer.enabled == false)
                return;

            // Run serializer
            sharedSerializer.OnReplayDeserialize(state);

            // Get points
            targetPoints.Clear();
            targetPoints.AddRange(sharedSerializer.Points);
        }
    }
}
#endif
