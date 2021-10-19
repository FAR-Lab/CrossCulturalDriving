using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

namespace UltimateReplay.Storage
{
    public abstract partial class ReplayStreamTarget
    {
        // Types
        public struct ReplayFileChunkFetchRequest
        {
            // Types
            public enum FetchRequestType
            {
                FetchByChunkID,
                FetchByTimeStamp,
                FetchBySequenceID,
            }

            public FetchRequestType requestType;
            public int idValue;
            public float timeStampValue;

            // Methods
            public static ReplayFileChunkFetchRequest FetchByChunkID(int chunkID)
            {
                return new ReplayFileChunkFetchRequest
                {
                    requestType = FetchRequestType.FetchByChunkID,
                    idValue = chunkID,
                };
            }

            public static ReplayFileChunkFetchRequest FetchByTimeStamp(float timeStamp)
            {
                return new ReplayFileChunkFetchRequest
                {
                    requestType = FetchRequestType.FetchByTimeStamp,
                    timeStampValue = timeStamp,
                };
            }

            public static ReplayFileChunkFetchRequest FetchBySequenceID(int sequenceID)
            {
                return new ReplayFileChunkFetchRequest
                {
                    requestType = FetchRequestType.FetchBySequenceID,
                    idValue = sequenceID,
                };
            }
        }

        // Private
        private List<ReplayStreamTaskRequest> threadTasks = new List<ReplayStreamTaskRequest>();
        private Thread streamThread = null;
        private bool threadRunning = true;
        private bool threadStarted = false;

        // Methods
        private void InitializeThreading()
        {
#if ULTIMATEREPLAY_NOTHREADING == false
            // Create the thread
            streamThread = new Thread(new ThreadStart(StreamThreadMain));

            // Setup the thread
            streamThread.IsBackground = true;
            streamThread.Name = "UltimateReplay_StreamService";

            // Set thread flag
            threadStarted = true;

            // Launch the thread
            streamThread.Start();
#endif
        }

        private void ShutdownThreading()
        {
#if ULTIMATEREPLAY_NOTHREADING == false
            // Request the thread to exit
            threadRunning = false;
            streamThread.Join(1500);

            // Force the thread to stop
            if (streamThread.IsAlive == true)
                streamThread.Abort();
#endif
        }

        private void StreamThreadMain()
        {
            try
            {
                // Loop unitl we are asked to quit
                while (threadRunning == true)
                {
                    // Check if any tasks are waiting
                    if (StreamThreadHasTask() == true)
                    {
                        // Process the waiting tasks
                        StreamThreadProcessWaitingTask();
                    }

                    Thread.Sleep(10);
                }

                // Check for any more tasks - We need to run them all before we end if possible
                while (StreamThreadHasTask() == true)
                {
                    // Process the task
                    StreamThreadProcessWaitingTask();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format("An exception caused the 'ReplayFileTarget' to fail (file stream thread : {0})", streamThread.ManagedThreadId));
                Debug.LogException(e);

                // Unset running flag - Thread cannot continue
                threadRunning = false;
            }
        }

        private bool StreamThreadHasTask()
        {
            lock (threadTasks)
            {
                // Check for any tasks
                return threadTasks.Count > 0;
            }
        }

        private void StreamThreadProcessAllWaitingTasks()
        {
            // Process all tasks before we continue
            while (StreamThreadHasTask() == true)
                StreamThreadProcessWaitingTask();
        }

        private void StreamThreadProcessWaitingTask()
        {
            // Get a thread task
            ReplayStreamTaskRequest request;

            lock (threadTasks)
            {
                // Require a task to carry on
                if (threadTasks.Count == 0)
                    return;

                // Get the front item
                request = threadTasks[0];
            }

            // Switch for task event
            switch (request.task)
            {
                // Idle - do nothing
                case ReplayStreamRequest.Idle:
                    break;

                // Write the specified chunk to the file
                case ReplayStreamRequest.WriteChunk:
                    {
                        // Get the chunk passed through the request
                        ReplayStreamChunk chunk = request.data as ReplayStreamChunk;

                        // Try to write the active chunk to the file
                        ThreadWriteReplayChunk(chunk);
                        break;
                    }

                // Fetch a chunk with the specified chunk id
                case ReplayStreamRequest.FetchChunk:
                    {
                        // Get the requested time stamp
                        //float timeStamp = (float)request.data;

                        ReplayFileChunkFetchRequest fetchData = (ReplayFileChunkFetchRequest)request.data;

                        // Load the chunk from file
                        ReplayStreamChunk chunk = ThreadReadReplayChunk(fetchData);// timeStamp);

                        // Make sure that we do not set the active chunk to null 
                        if (chunk != null)
                        {
                            lock (context)
                            {
                                // Store the loaded chunk
                                context.chunk = chunk;
                                context.buffer.StoreChunk(chunk);
                            }

                            // The chunk has been loaded so we can remove it from the requested set
                            if (processingChunkRequests.Contains(chunk.chunkID) == true)
                                processingChunkRequests.Remove(chunk.chunkID);
                        }
                        break;
                    }

                case ReplayStreamRequest.FetchChunkBuffered:
                    {
                        ReplayFileChunkFetchRequest fetchData = (ReplayFileChunkFetchRequest)request.data;

                        // Make sure it is not already buffered
                        lock (context)
                        {
                            if (fetchData.requestType == ReplayFileChunkFetchRequest.FetchRequestType.FetchByChunkID)
                            {
                                // The chunk is already loaded so we dont need to do anything
                                if (context.buffer.HasLoadedChunkWithID(fetchData.idValue) == true)
                                    break;
                            }
                        }

                        // Load the chunk from file
                        ReplayStreamChunk chunk = ThreadReadReplayChunk(fetchData);

                        // make sure the chunk was loaded
                        if (chunk != null)
                        {
                            lock (context)
                            {
                                // Store the chunk in the buffer for later use
                                context.buffer.StoreChunk(chunk);
                            }

                            // The chunk has been loaded so we can remove it from the requested set
                            if (processingChunkRequests.Contains(chunk.chunkID) == true)
                                processingChunkRequests.Remove(chunk.chunkID);
                        }
                        break;
                    }

                // Commit any data still in memeory to stream
                case ReplayStreamRequest.Commit:
                    {
                        // Commit the data to file
                        ThreadCommitReplayStream();
                        break;
                    }

                // Throw the replay data away
                case ReplayStreamRequest.Discard:
                    {
                        // Discard any replay data and stream data
                        ThreadDiscardReplayStream();
                        break;
                    }

                case ReplayStreamRequest.WriteHeader:
                    {
                        // Write the header to file
                        ThreadWriteReplayHeader();
                        break;
                    }

                case ReplayStreamRequest.FetchHeader:
                    {
                        // Fetch the replay header
                        ThreadFetchReplayHeader();
                        break;
                    }

                case ReplayStreamRequest.FetchTable:
                    {
                        // Fetch the replay chunk table
                        ThreadFetchReplayChunkTable();
                        break;
                    }

                case ReplayStreamRequest.FetchStateBuffer:
                    {
                        // Fetch the state buffer
                        ThreadFetchInitialStateBuffer();
                        break;
                    }
            }

            // Remove the task from the queue
            lock (threadTasks)
            {
                // Remove the request
                if (threadTasks.Contains(request) == true)
                    threadTasks.Remove(request);
            }
        }

        #region StreamThreadTasks
        protected abstract void ThreadWriteReplayChunk(ReplayStreamChunk chunk);

        protected abstract ReplayStreamChunk ThreadReadReplayChunk(ReplayFileChunkFetchRequest fetchData);

        protected abstract void ThreadWriteReplayHeader();

        protected abstract void ThreadWriteReplayChunkTable();

        protected abstract void ThreadWriteInitialStateBuffer();

        protected abstract void ThreadFetchReplayHeader();

        protected abstract void ThreadFetchReplayChunkTable();

        protected abstract void ThreadFetchInitialStateBuffer();

        protected virtual void ThreadCommitReplayStream()
        {
            lock (context)
            {
                // Check for open stream
                if (context.sourceStream == null)
                    return;

                // Get the end position of the file
                int chunkTableStart = context.sourceStream.Position;

                // Write the chunk table at the end of the file
                ThreadWriteReplayChunkTable();

                // Get the end position of the file
                int stateBufferStart = context.sourceStream.Position;

                // Write the initial state information at the end of the file
                ThreadWriteInitialStateBuffer();

                // Go back to the start of the file
                context.sourceStream.Seek(0, SeekOrigin.Begin);

                // Update the header info
                {
                    context.header.chunkTableOffset = chunkTableStart; // chunkTableSize;
                    context.header.stateBufferOffset = stateBufferStart;
                }

                // We need to overwrite the header with the correct info - The first time we wrote it, it was just as a size placeholder
                ThreadWriteReplayHeader();

                // Reset the memory buffers so that identical information is not re-commited
                context.chunkTable = new ReplayStreamChunkTable();
                context.chunk = new ReplayStreamChunk(ReplayStreamChunk.startChunkID, chunkSize);

                // Release final stream
                context.sourceStream.Dispose();
                context.sourceStream = null;
            }
        }

        protected virtual void ThreadDiscardReplayStream()
        {            
            // Release buffered chunks
            context.buffer.ReleaseAllChunks();

            // Reset all information
            context.header = new ReplayStreamHeader();
            context.chunkTable = new ReplayStreamChunkTable();
            context.chunk = new ReplayStreamChunk(ReplayStreamChunk.startChunkID, chunkSize);

            // Empty the temp stream
            if (context.sourceStream != null)
            {
                context.sourceStream.Clear();
                context.sourceStream.Dispose();
                context.sourceStream = null;
            }

            // Delete the recording file
            OnDiscardRecording();

            // Call discard on provider
            if (targetStreamProvider != null)
                targetStreamProvider.DiscardReplayStream();
        }
        #endregion
    }
}
