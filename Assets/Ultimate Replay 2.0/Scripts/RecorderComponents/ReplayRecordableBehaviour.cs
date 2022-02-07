using UltimateReplay.Core;

namespace UltimateReplay
{
    /// <summary>
    /// Derive from this base class to create custom recorder components. 
    /// </summary>
    public abstract class ReplayRecordableBehaviour : ReplayBehaviour, IReplaySerialize
    {
        // Methods
        /// <summary>
        /// Called by the replay system when the recorder component should deserialize any necessary data during playback.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> containing the recorded data</param>
        public abstract void OnReplayDeserialize(ReplayState state);

        /// <summary>
        /// Called by the replay system when the recorder component should serialize and necessary data during recording.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> used to store the serialized data</param>
        public abstract void OnReplaySerialize(ReplayState state);

        /// <summary>
        /// Called by Unity.
        /// </summary>
        public override void OnDestroy()
        {
            base.OnDestroy();

            // Inform component removed
            ReplayObject.RebuildComponentList();
        }
    }
}
