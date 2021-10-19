using System.Collections.Generic;
using System.IO;

namespace UltimateReplay.Storage
{
    /// <summary>
    /// A chunk table is a quick reference lookup table stored near the start of a replay file and specifys the location of varouis replay chunks using file offsets.
    /// This is used as a quick seek table for chunk jumping during playback. 
    /// Playback may pause slightly when seeking to random locations in the recording as the required chunk information is fetched (Buffering).
    /// </summary>
    public class ReplayStreamChunkTable : HashSet<ReplayStreamChunkTable.ReplayStreamChunkTableEntry>, IReplayStreamSerialize
    {
        // Types
        /// <summary>
        /// Represents a chunk entry in the <see cref="ReplayFileChunkTable"/>.
        /// </summary>
        public struct ReplayStreamChunkTableEntry
        {
            // Public
            [ReplayTextSerialize("Chunk ID")]
            /// <summary>
            /// The unique id for the current chunk.
            /// </summary>
            public int chunkID;

            /// <summary>
            /// The start time in seconds for the current chunk.
            /// </summary>
            [ReplayTextSerialize("Start TimeStamp")]
            public float startTimeStamp;
            /// <summary>
            /// The end time in seconds of the current chunk.
            /// </summary>
            [ReplayTextSerialize("End TimeStamp")]
            public float endTimeStamp;

            [ReplayTextSerialize("Start Sequence ID")]
            public short startSequenceID;

            [ReplayTextSerialize("End Sequence ID")]
            public short endSequenceID;
            /// <summary>
            /// The 32 bit file pointer representing the byte offset that the chunk data is located at.
            /// </summary>
            [ReplayTextSerialize("Chunk File Offset")]
            public int filePointer;
        }

        // Methods
        /// <summary>
        /// Add a new chunk reference to the chunk table.
        /// </summary>
        /// <param name="startTimeStamp">The start time in seconds for the chunk</param>
        /// <param name="endTimeStamp">The end time in seconds for the chunk</param>
        /// <param name="filePointer">The 32 bit file pointer for the chunk</param>
        public void CreateEntry(int chunkID, float startTimeStamp, float endTimeStamp, short startSequenceID, short endSequenceID, int filePointer)
        {
            Add(new ReplayStreamChunkTableEntry
            {
                chunkID = chunkID,
                startTimeStamp = startTimeStamp,
                endTimeStamp = endTimeStamp,
                startSequenceID = startSequenceID,
                endSequenceID = endSequenceID,
                filePointer = filePointer,
            });
        }

        /// <summary>
        /// Attempts to find the chunk pointer for the replay chunk with the matching chunk id.
        /// </summary>
        /// <param name="chunkID">The id of the chunk to get the file pointer for</param>
        /// <returns>A 32 bit file offset or -1 if the timestamp is not found in the recording</returns>
        public int GetPointerForChunk(int chunkID)
        {
            foreach (ReplayStreamChunkTableEntry entry in this)
            {
                // Check for matching ids
                if (chunkID == entry.chunkID)
                {
                    // Get the file pointer for the entry
                    return entry.filePointer;
                }
            }

            // Chunk not found
            return -1;
        }

        /// <summary>
        /// Attempts to find the chunk pointer for the replay chunk that best matches the specified time stamp.
        /// If there are no chunks that contian the specified time stamp then the return value will be -1.
        /// </summary>
        /// <param name="timeStamp">The timestamp to find the chunk offset pointer for</param>
        /// <returns>A 32 bit file offset or -1 if the timestamp is not found in the recording</returns>
        public int GetPointerForTimeStamp(float timeStamp)
        {
            // Negative time stamps are not alowed
            if (timeStamp < 0)
                return -1;

            foreach (ReplayStreamChunkTableEntry entry in this)
            {
                if (timeStamp >= entry.startTimeStamp &&
                    timeStamp <= entry.endTimeStamp)
                {
                    // Get the file pointer for the entry
                    return entry.filePointer;
                }
            }

            // We have not found an easy match yet so we need to do a bit more work
            // Due to the recording inverval, it is possible that a time stamp lies between chunks.
            // If this is the case then we need to select the chunk that the time stamp is closest to for smoothest playback.
            int index = 0;
            int size = Count;

            bool foundBestMatch = false;
            float bestMatchDifference = float.MaxValue;
            ReplayStreamChunkTableEntry bestMatch = new ReplayStreamChunkTableEntry();

            foreach (ReplayStreamChunkTableEntry entry in this)
            {
                // Make sure the time stamp is not past the end of the recording
                if (index == (size - 1))
                    break;

                // Check for closest entry
                if (timeStamp < entry.startTimeStamp)
                {
                    // Find the timestamp difference
                    float difference = entry.startTimeStamp - timeStamp;

                    // Check for smallest difference
                    if (difference < bestMatchDifference)
                    {
                        // We have found a new best match
                        foundBestMatch = true;
                        bestMatchDifference = difference;
                        bestMatch = entry;
                    }
                }
                else if (timeStamp > entry.endTimeStamp)
                {
                    // Find the timestamp difference
                    float difference = timeStamp - entry.endTimeStamp;

                    // Check for smallest difference
                    if (difference < bestMatchDifference)
                    {
                        // We have found a new best match
                        foundBestMatch = true;
                        bestMatchDifference = difference;
                        bestMatch = entry;
                    }
                }

                // Increase index
                index++;
            }

            // Check for best match
            if (foundBestMatch == true)
                return bestMatch.filePointer;

            // No entry found
            return -1;
        }

        public int GetPointerForSequenceID(int sequenceID)
        {
            // Negative time stamps are not alowed
            if (sequenceID < 0)
                return -1;

            foreach (ReplayStreamChunkTableEntry entry in this)
            {
                if (sequenceID >= entry.startSequenceID &&
                    sequenceID <= entry.endSequenceID)
                {
                    // Get the file pointer for the entry
                    return entry.filePointer;
                }
            }

            // No entry found
            return -1;
        }

        /// <summary>
        /// Called by the file streamer when the chunk table should be serialized to the specified stream.
        /// </summary>
        /// <param name="writer"></param>
        public void OnReplayStreamSerialize(BinaryWriter writer)
        {
            // Write the size
            writer.Write(Count);

            // Write the chunk table
            foreach (ReplayStreamChunkTableEntry entry in this)
            {
                // Write the table item
                writer.Write(entry.chunkID);
                writer.Write(entry.startTimeStamp);
                writer.Write(entry.endTimeStamp);
                writer.Write(entry.startSequenceID);
                writer.Write(entry.endSequenceID);
                writer.Write(entry.filePointer);
            }
        }

        /// <summary>
        /// Called by the file streamer when the chunk table should be deserialized from the specified stream.
        /// </summary>
        /// <param name="reader"></param>
        public void OnReplayStreamDeserialize(BinaryReader reader)
        {
            // Read the size
            int size = reader.ReadInt32();

            // Read each chunk item
            for (int i = 0; i < size; i++)
            {
                int id = reader.ReadInt32();
                float start = reader.ReadSingle();
                float end = reader.ReadSingle();
                short startSeq = reader.ReadInt16();
                short endSeq = reader.ReadInt16();
                int pointer = reader.ReadInt32();

                // Add the item
                Add(new ReplayStreamChunkTableEntry
                {
                    chunkID = id,
                    startTimeStamp = start,
                    endTimeStamp = end,
                    startSequenceID = startSeq,
                    endSequenceID = endSeq,
                    filePointer = pointer,
                });
            }
        }
    }
}
