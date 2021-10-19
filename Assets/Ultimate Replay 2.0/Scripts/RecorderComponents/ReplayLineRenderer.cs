using System;
using System.Collections.Generic;
using UltimateReplay.Serializers;
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// Recorder component used to record and replay the Unity line renderer component.
    /// </summary>
    [ReplaySerializer(typeof(ReplayPointRendererSerializer))]
    public class ReplayLineRenderer : ReplayRecordableBehaviour
    {
        // Types
        /// <summary>
        /// Flags used to specify which features are enabled on the recorder.
        /// </summary>
        [Flags]
        public enum ReplayLineRendererFlags
        {
            /// <summary>
            /// No additional features.
            /// </summary>
            None = 0,
            /// <summary>
            /// interpolation will be used during playback to create smoother results.
            /// </summary>
            Interpolate = 1 << 1,
        }

        // Private
        private ReplayPointRendererSerializer sharedSerializer = new ReplayPointRendererSerializer();

        private List<Vector3> lastPoints = new List<Vector3>();
        private List<Vector3> targetPoints = new List<Vector3>();

        // Public
        public LineRenderer observedLineRenderer = null;
        [HideInInspector]
        public ReplayLineRendererFlags updateFlags = ReplayLineRendererFlags.Interpolate;

        // Methods
        public void Start()
        {
            if (observedLineRenderer == null)
                Debug.LogWarningFormat("Replay line renderer '{0}' will not record or replay because the observed line renderer has not been assigned", this);
        }

        public override void Reset()
        {
            // Call base method
            base.Reset();

            // Try to auto-find line renderer
            if (observedLineRenderer == null)
                observedLineRenderer = GetComponent<LineRenderer>();
        }

        public override void OnReplayReset()
        {
            lastPoints.Clear();
            lastPoints.AddRange(targetPoints);
        }

        public override void OnReplayUpdate(ReplayTime replayTime)
        {
            // Check for no component
            if (observedLineRenderer == null || observedLineRenderer.enabled == false)
                return;

            // Check for point changes
            if (targetPoints.Count != observedLineRenderer.positionCount)
                observedLineRenderer.positionCount = targetPoints.Count;

            // Set all points
            for (int i = 0; i < targetPoints.Count; i++)
            {
                // Get the target point
                Vector3 updatePoint = targetPoints[i];

                // Check for interpolate
                if((updateFlags & ReplayLineRendererFlags.Interpolate) != 0 && lastPoints.Count == targetPoints.Count)
                {
                    // Interpolate the value
                    updatePoint = Vector3.Lerp(lastPoints[i], updatePoint, replayTime.Delta);
                }

                // Set point
                observedLineRenderer.SetPosition(i, updatePoint);
            }
        }

        public override void OnReplaySerialize(ReplayState state)
        {
            // Check for no component
            if (observedLineRenderer == null || observedLineRenderer.enabled == false)
                return;

            // Reset serialize
            sharedSerializer.Reset();

            // Fill points
            for(int i = 0; i < observedLineRenderer.positionCount; i++)
            {
                // Get the point
                sharedSerializer.Points.Add(observedLineRenderer.GetPosition(i));
            }

            // Run the serializer
            sharedSerializer.OnReplaySerialize(state);
        }

        public override void OnReplayDeserialize(ReplayState state)
        {
            // Check for no component
            if (observedLineRenderer == null)
                return;

            // Run serializer
            sharedSerializer.OnReplayDeserialize(state);

            // Get points
            targetPoints.Clear();
            targetPoints.AddRange(sharedSerializer.Points);
        }
    }
}
