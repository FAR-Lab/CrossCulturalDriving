using System;
using System.IO;
using UnityEngine;

namespace UltimateReplay.Storage
{
    /// <summary>
    /// A chunk is a container for multiple replay states that may or may not be compressed in order to reduce file size.
    /// Chunks are used to best optimize performance during playback and filesize.
    /// </summary>
    public class ReplayStreamChunk : ReplayStorageCombiner, IReplayStreamSerialize, IEquatable<ReplayStreamChunk>
    {
        // Private
        private int endSequenceIDCached = -1;
        private float endTimeCached = -1;

        // Public
        /// <summary>
        /// Due to the way chunks are stored, it is possible to attmempt to load a snapshot that lies inbetween 2 chunks.
        /// </summary>
        public const float chunkOverlapThreshold = 1f;

        public const int startChunkID = 1;

        /// <summary>
        /// Use this value to control how many identical sequentional frames can be combined while recording.
        /// Higher values will result in reduced files sizes but higher CPU usage. Lower values will result in larger file sizes but lower CPU usage.
        /// Default value = 16.
        /// </summary>
        public int maxCombineIdenticalFramesDepth = 16;

        /// <summary>
        /// The unique chunk id associated with this chunk.
        /// This identifier describes the location of the chunk in the overall playback sequence.
        /// </summary>
        [ReplayTextSerialize("Chunk ID")]
        public int chunkID = startChunkID;

        // Properties
        /// <summary>
        /// Get the time in seconds that this chunk starts.
        /// </summary>
        [ReplayTextSerialize("Chunk Start Time")]
        public float ChunkStartTime
        {
            get
            {
                if (snapshotStorage.Count == 0)
                    return 0;

                // Get timestamp of first frame
                return snapshotStorage[0].TimeStamp;
            }
        }

        [ReplayTextSerialize("Chunk Start Sequence ID")]
        public int ChunkStartSequenceID
        {
            get
            {
                if (snapshotStorage.Count == 0)
                    return -1;

                return snapshotStorage[0].SequenceID;
            }
        }

        /// <summary>
        /// Get the time in seconds that this chunk ends.
        /// </summary>
        [ReplayTextSerialize("Chunk End Time")]
        public float ChunkEndTime
        {
            get
            {
                if (endTimeCached != -1)
                    return endTimeCached;

                // No data
                if (snapshotStorage.Count == 0)
                    return 0;

                // Get the timestamp of the last frame
                return endTimeCached = snapshotStorage[snapshotStorage.Count - 1].TimeStamp;
            }
        }

        [ReplayTextSerialize("Chunk End Sequence ID")]
        public int ChunkEndSequenceID
        {
            get
            {
                if (endSequenceIDCached != -1)
                    return endSequenceIDCached;

                if (snapshotStorage.Count == 0)
                    return 0;

                return endSequenceIDCached = snapshotStorage[snapshotStorage.Count - 1].SequenceID;
            }
        }

        /// <summary>
        /// Get the time in seconds that this chunk lasts.
        /// </summary>
        [ReplayTextSerialize("Chunk Duration")]
        public float ChunkDuration
        {
            get { return ChunkEndTime - ChunkStartTime; }
        }

        // Constructor
        public ReplayStreamChunk(int chunkID, int chunkCapacity)
            : base(chunkCapacity)
        {
            this.chunkID = chunkID;
        }

        // Methods
        public bool Equals(ReplayStreamChunk other)
        {
            return chunkID == other.chunkID;
        }

        /// <summary>
        /// Create a member clone of this chunk.
        /// </summary>
        /// <returns>A cloned version of this chunk</returns>
        public ReplayStreamChunk Clone()
        {
            ReplayStreamChunk result = new ReplayStreamChunk(chunkID, SnapshotCapacity);

            
            // Store all chunks
            for (int i = 0; i < SnapshotCount; i++)
                result.AddAndCombineSnapshot(Snapshots[i], maxCombineIdenticalFramesDepth);

            //foreach (ReplaySnapshot snapshot in Snapshots)
            //    result.AddAndCombineSnapshot(snapshot);

            return result;
        }

        public override ReplaySnapshot FetchSnapshot(float timeStamp, ReplayManager.PlaybackDirection direction)
        {
            // Make sure chunk contains a snapshot with the time stamp
            if (timeStamp >= ChunkStartTime && timeStamp <= ChunkEndTime)
            {
                return base.FetchSnapshot(timeStamp, direction);
            }
            return null;
        }

        public ReplaySnapshot FetchBestMatchingSnapshot(float timeStamp)
        {
            // Dont perform any bounds checks
            return base.FetchSnapshot(timeStamp, ReplayManager.PlaybackDirection.Forward);
        }

        /// <summary>
        /// Called by the file streamer when the chunk should be serialized to the specified stream.
        /// </summary>
        /// <param name="writer"></param>
        public void OnReplayStreamSerialize(BinaryWriter writer)
        {
            writer.Write(chunkID);
            writer.Write(ChunkStartTime);
            writer.Write(ChunkEndTime);

            // Write the snapshot size
            writer.Write(snapshotStorage.Count);

            // Write all snapshot data
            foreach (ReplaySnapshot snapshot in Snapshots)
            {
                // Serialize the snapshot
                ReplayStreamSerializationUtility.StreamSerialize(snapshot, writer);
            }
        }

        public ReplaySnapshot FetchBestMatchingSequenceSnapshot(int sequenceID, ReplayManager.PlaybackDirection direction)
        {
            if (sequenceID < ChunkStartSequenceID)
            {
                return FetchSnapshot(ChunkStartTime, direction);
            }
            else if (sequenceID > ChunkEndSequenceID)
            {
                return FetchSnapshot(ChunkEndTime, direction);
            }

            return base.snapshotStorage.Find(s => s.SequenceID == sequenceID);
        }

        /// <summary>
        /// Called by the file streamer when the chunk should be deserialized from the specified stream.
        /// </summary>
        /// <param name="reader"></param>
        public void OnReplayStreamDeserialize(BinaryReader reader)
        {
            chunkID = reader.ReadInt32();
            float start = reader.ReadSingle();
            float end = reader.ReadSingle();

            // Read the snapshot size
            int size = reader.ReadInt32();

            // Read all snapshot data
            for (int i = 0; i < size; i++)
            {
                // Create a new snapshot - time stamp will be overwritten when deserialize is called
                ReplaySnapshot snapshot = new ReplaySnapshot();

                // Deserialize the snapshot
                ReplayStreamSerializationUtility.StreamDeserialize(ref snapshot, reader);

                // Register the snapshot
                snapshotStorage.Add(snapshot);
            }

            // Verify start / end times
            if (start != ChunkStartTime || end != ChunkEndTime)
                Debug.LogWarning("Possible corrupt replay file chunk - Expected time stamps do not match actual time stamps");
        }
    }
}
