using UltimateReplay.Serializers;
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// A replay component used to record the enabled state of a behaviour component.
    /// </summary>
    [ReplaySerializer(typeof(ReplayEnabledStateSerializer))]
    public class ReplayComponentEnabledState : ReplayRecordableBehaviour
    {
        // Private
        private static ReplayEnabledStateSerializer sharedSerializer = new ReplayEnabledStateSerializer();

        // Public
        /// <summary>
        /// The behaviour component that will have its enabled state recorded and replayed.
        /// </summary>
        public Behaviour observedComponent;

        // Methods
        /// <summary>
        /// Called by Unity.
        /// </summary>
        public void Start()
        {
            if (observedComponent == null)
                Debug.LogWarningFormat("Replay component enabled state '{0}' will not record or replay because the observed component has not been assigned", this);
        }

        /// <summary>
        /// Called by the replay system when the component should serialize its recorded data.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> to write to</param>
        public override void OnReplaySerialize(ReplayState state)
        {
            // Check for no component
            if (observedComponent == null)
                return;

            // Set enabled state
            sharedSerializer.Enabled = observedComponent.enabled;

            // Run serializer
            sharedSerializer.OnReplaySerialize(state);
        }

        /// <summary>
        /// Called by the replay system when the component should deserialize previously recorded data.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> to read from</param>
        public override void OnReplayDeserialize(ReplayState state)
        {
            // Check for no component
            if (observedComponent == null)
                return;

            // Run serializer
            sharedSerializer.OnReplayDeserialize(state);

            // Check for changed and apply state
            if(observedComponent.enabled != sharedSerializer.Enabled)
                observedComponent.enabled = sharedSerializer.Enabled;
        }       
    }
}
