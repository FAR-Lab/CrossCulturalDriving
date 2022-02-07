using UltimateReplay.Serializers;
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// A replay component used to record the enabled state of a game object.
    /// </summary>
    [ReplaySerializer(typeof(ReplayEnabledStateSerializer))]
    [DisallowMultipleComponent]
    public sealed class ReplayEnabledState : ReplayRecordableBehaviour
    {
        // Private
        private ReplayEnabledStateSerializer sharedSerializer = new ReplayEnabledStateSerializer();

        // Methods
        /// <summary>
        /// Called by the replay system when recorded data should be captured.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> used to store the recorded data</param>
        public override void OnReplaySerialize(ReplayState state)
        {
            // Set enabled
            sharedSerializer.Enabled = gameObject.activeSelf;

            // Run serializer
            sharedSerializer.OnReplaySerialize(state);
        }

        /// <summary>
        ///  Called by the replay stystem when replay data should be restored.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> containig the previously recorded data</param>
        public override void OnReplayDeserialize(ReplayState state)
        {
            // Run serializer
            sharedSerializer.OnReplayDeserialize(state);

            // Check for change and apply
            if (gameObject.activeSelf != sharedSerializer.Enabled)
                gameObject.SetActive(sharedSerializer.Enabled);
        }        
    }
}
