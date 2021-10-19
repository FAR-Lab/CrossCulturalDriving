
namespace UltimateReplay.Core
{
    // Types
    internal enum ReplayIdentifier : byte
    {
        Identity,
        Variable,
        State,
        Snapshot,
        InitialData,
        InitialDataBuffer,
        ObjectRecorder,
        EnabledStateRecorder,
        TransformRecorder,

        CustomRecorder,
    }

    /// <summary>
    /// This class should be implemented when you want to serialize custom replay data.
    /// This sould really be an interface but it needs to be a class to be assignable in the inspector.
    /// </summary>
    public interface IReplaySerialize
    {
        // Methods
        /// <summary>
        /// Called by the replay system when all replay state data should be serialized.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> to write the data to</param>
        void OnReplaySerialize(ReplayState state);

        /// <summary>
        /// Called by the replay system when all replay state data should be deserialized.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> to read the data from</param>
        void OnReplayDeserialize(ReplayState state);
    }
}