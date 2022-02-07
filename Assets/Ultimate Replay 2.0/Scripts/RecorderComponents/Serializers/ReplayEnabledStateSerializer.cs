using UltimateReplay.Core;

namespace UltimateReplay.Serializers
{
    /// <summary>
    /// A dedicated serializer used to serialize and deserialize data for the <see cref="ReplayEnabledState"/> component.
    /// </summary>
    public sealed class ReplayEnabledStateSerializer : IReplaySerialize
    {
        // Private
        [ReplayTextSerialize("Enabled")]
        private bool enabled = true;

        // Properties
        /// <summary>
        /// The enabled state of the object.
        /// </summary>
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        // Methods
        /// <summary>
        /// Invoke this method to serialize the enabled state data to the specified <see cref="ReplayState"/>.
        /// </summary>
        /// <param name="state">The state object to write to</param>
        public void OnReplaySerialize(ReplayState state)
        {
            state.Write(enabled);
        }

        /// <summary>
        /// Invoke this method to deserialize the enabled state from the specified <see cref="ReplayState"/>.
        /// </summary>
        /// <param name="state">The state object to read from</param>
        public void OnReplayDeserialize(ReplayState state)
        {
            enabled = state.ReadBool();
        }
    }
}
