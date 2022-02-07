using System;
using UltimateReplay.Core;
using UltimateReplay.Statistics;

namespace UltimateReplay.Storage
{
    /// <summary>
    /// Represents a task that can be issued to a <see cref="ReplayStorageTarget"/>.
    /// </summary>
    public enum ReplayTargetTask
    {
        /// <summary>
        /// The replay target should commit all data currently in memeory to its end destination.
        /// Similar to a flush method.
        /// </summary>
        Commit,
        /// <summary>
        /// The replay target should discard any recorded data.
        /// </summary>
        Discard,
        /// <summary>
        /// The replay target should prepare for subsequent write requests.
        /// </summary>
        PrepareWrite,
        /// <summary>
        /// The replay target should prepare for subsequent read requests.
        /// </summary>
        PrepareRead,
    }

    /// <summary>
    /// Represents and abstract storage device capable of holding recorded state data for playback at a later date.
    /// Depending upon implementation, a <see cref="ReplayStorageTarget"/> may be volatile or non-volatile. 
    /// </summary>
    [Serializable]
    [ReplayIgnore]
    public abstract class ReplayStorageTarget : ReplayLockable
    {
        // Private
        private ReplayManager.PlaybackDirection playbackDirection = ReplayManager.PlaybackDirection.Forward;

        // Protected
        /// <summary>
        /// The amount of time in seconds that the current recording data lasts.
        /// If no data exists then the duration will dfault to a length of 0.
        /// </summary>
        protected float duration = 0;

        // Properties
        public abstract bool CanRead { get; }

        public abstract bool CanWrite { get; }

        /// <summary>
        /// The amount of time in seconds that this recording lasts.
        /// </summary>
        public abstract float Duration { get; }

        /// <summary>
        /// Get the total amount of bytes that this replay uses.
        /// </summary>
        public abstract int MemorySize { get; }

        internal virtual ReplayManager.PlaybackDirection PlaybackDirection
        {
            get { return playbackDirection; }
            set { playbackDirection = value; }
        }

        /// <summary>
        /// Get the initial state buffer for the replay target. The state buffer is essential for storing dynamic object information.
        /// </summary>
        public abstract ReplayInitialDataBuffer InitialStateBuffer { get; }

        // Constructor
        protected ReplayStorageTarget()
        {
            // Register storage target
            ReplayStorageTargetStatistics.AddStorageTarget(this);
        }

        // Methods
        /// <summary>
        /// Store a replay snapshot in the replay target.
        /// </summary>
        /// <param name="state">The snapshot to store</param>
        public abstract void StoreSnapshot(ReplaySnapshot state);

        /// <summary>
        /// Recall a snapshot from the replay target based on the specified replay offset.
        /// </summary>
        /// <param name="timeStamp">The time offset from the start of the recording pointing to the individual snapshot to recall</param>
        /// <returns>The replay snapshot at the specified offset</returns>
        public abstract ReplaySnapshot FetchSnapshot(float timeStamp);

        /// <summary>
        /// Recall a snapshot by its unique sequence id value.
        /// The sequence ID value indicates the snapshots 0-based index value for the recording sequence.
        /// </summary>
        /// <param name="sequenceID">The sequence ID to fetch the snapshot for</param>
        /// <returns>The replay snapshot at the specified sequence id</returns>
        public abstract ReplaySnapshot FetchSnapshot(int sequenceID);
        
        /// <summary>
        /// Called by the recording system to notify the active <see cref="ReplayStorageTarget"/> of an upcoming event. 
        /// </summary>
        /// <param name="mode">The <see cref="ReplayTargetTask"/> that the target should prepare for</param>
        public abstract void PrepareTarget(ReplayTargetTask mode);
    }
}
