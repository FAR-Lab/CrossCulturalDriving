using System;
using System.Collections.Generic;

namespace UltimateReplay.Storage
{
    /// <summary>
    /// Represents a memory storage buffer where replay data can be stored for game sessions.
    /// The buffer can be used as a continuous rolling buffer of a fixed size where a fixed amount of playback footage is recorded and then overwritten by new data as it is received.
    /// </summary>
    [Serializable]
    public class ReplayMemoryTarget : ReplayStorageTarget
    {
        // Types
        public enum MemorySizeLimitReachedBehaviour
        {
            /// <summary>
            /// Any frames that attempt to be stored will be discarded.
            /// </summary>
            PreventFurtherRecording,
            /// <summary>
            /// An OutOfmemory exception will be thrown when the limit is exceeded.
            /// </summary>
            ThrowMemoryException,
        }

        // Private
        private const int unlimitedBufferValue = 0;
        private const int unlimitedSizeValue = -1;

        private float recordSeconds = 15;
        private int memorySizeLimit = unlimitedSizeValue;
        private MemorySizeLimitReachedBehaviour memorySizeLimitBehaviour = MemorySizeLimitReachedBehaviour.ThrowMemoryException;
        private int maxCombineFrames = 30;
        private List<ReplayStorageCombiner> storageChunks = new List<ReplayStorageCombiner>();
        private ReplayInitialDataBuffer initialStateBuffer = new ReplayInitialDataBuffer();

        // Public
        /// <summary>
        /// Use this value to control how many identical sequentional frames can be combined while recording.
        /// Higher values will result in reduced files sizes but higher CPU usage. Lower values will result in larger file sizes but lower CPU usage.
        /// Default value = 16.
        /// </summary>
        public int maxCombineIdenticalFramesDepth = 16;

        // Properties
        /// <summary>
        /// The amount of time in seconds that the recording lasts.
        /// Usually this value will be equal to <see cref="recordSeconds"/> however it will take atleast the amount of <see cref="recordSeconds"/> to initially fill the buffer before it wraps around.  
        /// </summary>
        public override float Duration
        {
            get
            {
                // Check for any information
                if (storageChunks.Count == 0)
                    return 0;

                // Get end chunk
                ReplayStorageCombiner chunk = storageChunks[storageChunks.Count - 1];
                
                // Get end frame
                ReplaySnapshot end = chunk.LastSnapshot;

                // Use the end frame as the default duration
                float duration = end.TimeStamp;

                //// Check if the buffer should be constrained
                //if (recordSeconds != 0 && end.TimeStamp > recordSeconds)
                //{
                //    // Get start frame
                //    ReplaySnapshot start = chunk.FirstSnapshot;

                //    // Take the start time into account (0 based start offset)
                //    duration -= start.TimeStamp;
                //}

                // Get the recording duration
                return duration;
            }
        }

        /// <summary>
        /// Get the amount of size in bytes that this memory target requires for all state data.
        /// This size does not include internal structures used to store the data but exclusivley contains game state sizes.
        /// </summary>
        public override int MemorySize
        {
            get
            {
                int size = 0;

                // Calculate the total size used by all frames
                foreach (ReplayStorageCombiner chunk in storageChunks)
                    size += chunk.Size;

                return size;
            }
        }

        /// <summary>
        /// Get the <see cref="ReplayInitialDataBuffer"/> for the storage targe.
        /// </summary>
        public override ReplayInitialDataBuffer InitialStateBuffer
        {
            get { return initialStateBuffer; }
        }

        /// <summary>
        /// Returns a value indicating whether the storage target can be read from.
        /// </summary>
        public override bool CanRead
        {
            get { return true; }
        }

        /// <summary>
        /// Returns a value indicating whether the storage target can be written to.
        /// </summary>
        public override bool CanWrite
        {
            get { return true; }
        }

        /// <summary>
        /// Get the amount of time in seconds of rolling recording. 
        /// This value will be '-1' if a rolling buffer is not in use.
        /// </summary>
        public float RollingBufferRecordSeconds
        {
            get { return recordSeconds; }
        }

        /// <summary>
        /// Returns a value indicating whether the storage target uses rolling recording.
        /// </summary>
        public bool IsRollingBuffer
        {
            get { return recordSeconds != unlimitedBufferValue; }
        }

        /// <summary>
        /// Get the amount of storage size in bytes that the target can store. 
        /// This value will be '-1' if an unlimited capacity is used.
        /// </summary>
        public int MemorySizeLimit
        {
            get { return memorySizeLimit; }
            set { memorySizeLimit = value; }
        }

        /// <summary>
        /// Returns a value indicating whether the storage target is limited by memory size.
        /// </summary>
        public bool IsMemorySizeLimited
        {
            get { return memorySizeLimit != unlimitedSizeValue; }
        }

        /// <summary>
        /// The behaviour of the storage target when the storage size limit is reached.
        /// </summary>
        public MemorySizeLimitReachedBehaviour MemorySizeLimitBehaviour
        {
            get { return memorySizeLimitBehaviour; }
            set { memorySizeLimitBehaviour = value; }
        }

        // Constructor
        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="memorySizeLimit">The maximum storage size in bytes that the memory target can hold. Use '-1' for an unlimited size capacity</param>
        /// <param name="maxCompressionFrames">The maximum number of frames that can be group compressed</param>
        public ReplayMemoryTarget(int memorySizeLimit = unlimitedSizeValue, int maxCompressionFrames = 30)
        {
            // Create unlimited memory buffer
            this.memorySizeLimit = memorySizeLimit;
            this.recordSeconds = unlimitedBufferValue;
            this.maxCombineFrames = maxCompressionFrames;
        }

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="rollingBufferRecordSeconds">The amount of time in seconds that the memory target should hold for rolling recording</param>
        /// <param name="memorySizeLimit">The storage size limit in bytes that the memory target can hold. Use '-1' for an unlimited size capacity</param>
        /// <param name="maxCompressionFrames">The maximum number of frames that can be group compressed</param>
        public ReplayMemoryTarget(float rollingBufferRecordSeconds, int memorySizeLimit = unlimitedSizeValue, int maxCompressionFrames = 30)
        {
            this.memorySizeLimit = memorySizeLimit;
            this.recordSeconds = rollingBufferRecordSeconds;
            this.maxCombineFrames = maxCompressionFrames;
        }

        // Methods
        /// <summary>
        /// Clear all data from the memory target.
        /// </summary>
        public void Clear()
        {
            PrepareTarget(ReplayTargetTask.Discard);
        }

        /// <summary>
        /// Store a replay snapshot in the replay target.
        /// If the new snapshot causes the internal buffer to 'overflow' then the recoding clip will be wrapped so that the recording duration is no more than <see cref="recordSeconds"/>. 
        /// </summary>
        /// <param name="state">The snapshot to store</param>
        public override void StoreSnapshot(ReplaySnapshot state)
        {
            // Check for memory limit
            if(IsMemorySizeLimited == true && MemorySize >= memorySizeLimit)
            {
                if(memorySizeLimitBehaviour == MemorySizeLimitReachedBehaviour.PreventFurtherRecording)
                {
                    // Dont store the snapshot
                    return;
                }
                else if(memorySizeLimitBehaviour == MemorySizeLimitReachedBehaviour.ThrowMemoryException)
                {
                    // Throw out of memory exception
                    throw new OutOfMemoryException("The replay target memory size limit has been reached");
                }
            }

            ReplayStorageCombiner storage = null;

            // Check if an existing chunk can be used
            if(storageChunks.Count > 0 && storageChunks[storageChunks.Count - 1].IsFull == false)
            {
                // Get the end chunk
                storage = storageChunks[storageChunks.Count - 1];
            }
            else
            {
                // Create a new chunk
                storage = new ReplayStorageCombiner(maxCombineFrames);
                storageChunks.Add(storage);
            }

            // Add and combine the snapshot
            storage.AddAndCombineSnapshot(state, maxCombineIdenticalFramesDepth);
            
            // Create a fixed size wrap around buffer if possible
            ConstrainBuffer();
        }

        /// <summary>
        /// Recall a snapshot from the replay target based on the specified replay offset.
        /// </summary>
        /// <param name="offset">The offset pointing to the individual snapshot to recall</param>
        /// <returns>The replay snapshot at the specified offset</returns>
        public override ReplaySnapshot FetchSnapshot(float offset)
        {
            // Check for no replay data
            if (storageChunks.Count == 0)
                return null;

            // Check for past clip end
            if (offset > Duration)
                return null;

            if (PlaybackDirection == ReplayManager.PlaybackDirection.Forward)
            {
                foreach (ReplayStorageCombiner chunk in storageChunks)
                {
                    // Check if the time is passed the end of the chunk
                    if (offset > chunk.LastSnapshot.TimeStamp)
                        continue;

                    // Fetch with offset
                    return chunk.FetchSnapshot(offset, PlaybackDirection);
                }
            }
            else
            {
                for(int i = storageChunks.Count - 1; i >= 0; i--)
                {
                    if (offset < storageChunks[i].FirstSnapshot.TimeStamp)
                        continue;

                    // Fetch with offset
                    return storageChunks[i].FetchSnapshot(offset, PlaybackDirection);
                }
            }

            return null;
        }

        /// <summary>
        /// Attempt to fetch the snapshot data for the specified sequence id.
        /// </summary>
        /// <param name="sequenceID">The sequence id of the snapshot</param>
        /// <returns>A <see cref="ReplaySnapshot"/> instance of the corrosponding snapshot or null if the snapshot does not exist</returns>
        public override ReplaySnapshot FetchSnapshot(int sequenceID)
        {
            // Check for no replay data
            if (storageChunks.Count == 0)
                return null;
            
            foreach (ReplayStorageCombiner chunk in storageChunks)
            {
                // Check if the time is passed the end of the chunk
                if (sequenceID > chunk.LastSnapshot.SequenceID)
                    continue;

                // Get snapshot for id
                return chunk.FetchSnapshot(sequenceID);
            }

            return null;
        }

        /// <summary>
        /// Clears all state information for the current recording essentially restoring the memory target to its initial state.
        /// </summary>
        public override void PrepareTarget(ReplayTargetTask mode)
        {
            // Only listen for discard events - We can switch between read and write instantly
            if (mode == ReplayTargetTask.Discard)
            {
                // Clear all recorded data
                storageChunks.Clear();
                duration = 0;

                // Reset state buffer
                initialStateBuffer = new ReplayInitialDataBuffer();
            }
            else if (mode == ReplayTargetTask.Commit)
            {
                // Modify the snapshot time stamps so that the recording is 0 offset based
                if(storageChunks.Count > 0)
                {
                    // Get the first snapshot
                    ReplaySnapshot first = storageChunks[0].FirstSnapshot;

                    if (first != null)
                    {
                        // Get the timestamp of the first frame
                        float offsetTime = first.TimeStamp;
                        int offsetSequence = first.SequenceID - ReplaySnapshot.startSequenceID;

                        // Deduct the time stamp from all other frames to make them offset based on 0 time
                        foreach (ReplayStorageCombiner storage in storageChunks)
                        {
                            foreach (ReplaySnapshot snapshot in storage.Snapshots)
                            {
                                snapshot.CorrectTimestamp(-offsetTime);
                                snapshot.CorrectSequenceID(-offsetSequence);
                            }
                        }
                    }
                }
            }
            else if(mode == ReplayTargetTask.PrepareWrite)
            {
                // Check for existing data
                if (storageChunks.Count > 0)
                    throw new InvalidOperationException("The memory storage target already has data stored. You must clear the data to begin new writing operations");
            }
        }

        private void ConstrainBuffer()
        {
            // Check for unlimited buffer
            if (recordSeconds == unlimitedBufferValue)
                return;

            // Make sure we have more than one state
            if (storageChunks.Count == 0)
                return;

            // Keep an additional .2 of a second to ensure we have atleast the requested amount of recording
            float timeCompensation = 0.2f;

            // Calculate the start time
            float targetStartTime = storageChunks[storageChunks.Count - 1].LastSnapshot.TimeStamp - (recordSeconds + timeCompensation);

            // Process all buffered chunks - Note that we can only remove full chunks and not individual snapshots because snapshots may reference other snapshots in that chunk to save storage space
            for(int i = 0; i < storageChunks.Count; i++)
            {
                if(storageChunks[i].LastSnapshot.TimeStamp <= targetStartTime)
                {
                    storageChunks.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Create an unlimited memory buffer.
        /// </summary>
        /// <returns>A new <see cref="ReplayMemoryTarget"/> with unlimited storage capacity</returns>
        public static ReplayMemoryTarget CreateUnlimited()
        {
            return new ReplayMemoryTarget();
        }

        /// <summary>
        /// Create a time limited rolling memory buffer.
        /// This will create a memory target with continuous rolling recording of the last x amount of seconds.
        /// </summary>
        /// <param name="recordPreviousSeconds">The amount of recording in seconds that the memory target should hold</param>
        /// <returns>A new <see cref="ReplayMemoryTarget"/> with a time limited rolling buffer</returns>
        public static ReplayMemoryTarget CreateTimeLimitedRolling(float recordPreviousSeconds)
        {
            return new ReplayMemoryTarget(recordPreviousSeconds);
        }

        public static ReplayMemoryTarget CreateTimeAndMemorySizeLimitedRolling(float recordPreviousSeconds, int maxMemoryUsage)
        {
            return new ReplayMemoryTarget(recordPreviousSeconds, maxMemoryUsage);
        }

        public static ReplayMemoryTarget CreateMemorySizeLimited(int maxMemoryUsage)
        {
            return new ReplayMemoryTarget(maxMemoryUsage);
        }
    }
}
