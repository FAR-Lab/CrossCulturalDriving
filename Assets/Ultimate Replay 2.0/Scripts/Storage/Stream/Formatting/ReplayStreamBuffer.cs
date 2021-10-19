
using System;
using System.Collections.Generic;

namespace UltimateReplay.Storage
{
    public class ReplayStreamBuffer
    {
        // Types
        public enum ReplayFileChunkReleaseMode
        {
            ChunksBefore,
            ChunksAfter,
        }

        // Private
        private HashSet<ReplayStreamChunk> loadedChunks = new HashSet<ReplayStreamChunk>();
        private Queue<ReplayStreamChunk> removeQueue = new Queue<ReplayStreamChunk>();

        // Methods
        public void StoreChunk(ReplayStreamChunk chunk)
        {
            // Cache the chunk
            if (loadedChunks.Contains(chunk) == false)
                loadedChunks.Add(chunk);
        }

        public bool HasLoadedChunkWithID(int chunkID)
        {
            foreach(ReplayStreamChunk chunk in loadedChunks)
            {
                if (chunk.chunkID == chunkID)
                    return true;
            }

            return false;
        }

        public bool HasLoadedChunk(float timeStamp)
        {
            foreach(ReplayStreamChunk chunk in loadedChunks)
            {
                if (timeStamp >= chunk.ChunkStartTime && timeStamp <= chunk.ChunkEndTime)
                    return true;
            }

            return false;
        }

        public bool HasLoadedChunk(int sequenceID)
        {
            foreach (ReplayStreamChunk chunk in loadedChunks)
            {
                if (sequenceID >= chunk.ChunkStartSequenceID && sequenceID <= chunk.ChunkEndSequenceID)
                    return true;
            }

            return false;
        }

        public ReplayStreamChunk GetLoadedChunk(float timeStamp, ReplayManager.PlaybackDirection direction)
        {
            // Check all chunks
            foreach(ReplayStreamChunk chunk in loadedChunks)
            {
                // Check if restore is successful
                if(chunk.FetchSnapshot(timeStamp, direction) != null)
                {
                    // We have found the chunk
                    return chunk;
                }
            }

            // No loaded chunk found
            return null;
        }

        public ReplayStreamChunk GetLoadedChunkWithID(int chunkID)
        {
            foreach (ReplayStreamChunk chunk in loadedChunks)
            {
                if (chunkID == chunk.chunkID)
                {
                    return chunk;
                }
            }

            return null;
        }

        public ReplayStreamChunk GetLoadedChunk(int sequenceID)
        {
            // Check all chunks
            foreach (ReplayStreamChunk chunk in loadedChunks)
            {
                // Check if restore is successful
                if (sequenceID >= chunk.ChunkStartSequenceID && sequenceID <= chunk.ChunkEndSequenceID)
                {
                    // We have found the chunk
                    return chunk;
                }
            }

            // No loaded chunk found
            return null;
        }

        public void ReleaseAllChunks()
        {
            // Clear data sets
            loadedChunks.Clear();
            removeQueue.Clear();
        }

        public void ReleaseOldChunks(float currentTimeStamp, ReplayFileChunkReleaseMode mode)
        {
            switch(mode)
            {
                case ReplayFileChunkReleaseMode.ChunksBefore:
                    {
                        // Process all chunks
                        foreach(ReplayStreamChunk chunk in loadedChunks)
                        {
                            // Check if the chunk ends before the current time stamp
                            if(chunk.ChunkEndTime < currentTimeStamp)
                            {
                                // We should remove the chunk soon
                                removeQueue.Enqueue(chunk);
                            }
                        }

                        break;
                    }

                case ReplayFileChunkReleaseMode.ChunksAfter:
                    {
                        // Process all chunks
                        foreach(ReplayStreamChunk chunk in loadedChunks)
                        {
                            // Check if the chunk starts before the current time stamp
                            if(chunk.ChunkStartTime > currentTimeStamp)
                            {
                                // We should remove the chunk soon
                                removeQueue.Enqueue(chunk);
                            }
                        }

                        break;
                    }
            }

            // Remove old chunks
            while(removeQueue.Count > 0)
            {
                // Get the ext chunk
                ReplayStreamChunk current = removeQueue.Dequeue();

                // Remove the chunk from loaded chunks
                if (loadedChunks.Contains(current) == true)
                    loadedChunks.Remove(current);
            }
        }
    }
}