
namespace UltimateReplay.Storage
{
    public enum ReplaySnapshotStorableType
    {
        /// <summary>
        /// The storage element contains replay data.
        /// </summary>
        StateStorage,
        /// <summary>
        /// The storage element points to a replay data segment.
        /// </summary>
        StatePointer,
    }

    /// <summary>
    /// Represents a replay data stream that could be recorded data or a pointer to recorded data. 
    /// Used for lossless compression to reduce storage size by combining snapshots frames with identical data.
    /// </summary>
    public interface IReplaySnapshotStorable : IReplayStreamSerialize
    {
        // Properties
        /// <summary>
        /// Get the <see cref="ReplaySnapshotStorableType"/> of this replay data stream.
        /// </summary>
        ReplaySnapshotStorableType StorageType { get; }
    }
}
