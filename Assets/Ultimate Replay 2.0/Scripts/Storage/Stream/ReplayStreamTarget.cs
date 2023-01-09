// Disable threading on web platforms
#if UNITY_WEBGL
#define ULTIMATEREPLAY_NOTHREADING
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UltimateReplay.Core;
using UnityEngine;

namespace UltimateReplay.Storage
{
    /// <summary>
    /// A file task request identifier.
    /// Used to request specific file operations on the streaming thread.
    /// </summary>
    internal enum ReplayStreamRequest
    {
        /// <summary>
        /// No task, Skip cycle.
        /// </summary>
        Idle = 0,
        /// <summary>
        /// Fetch a chunk from the replay file.
        /// </summary>
        FetchChunk,
        /// <summary>
        /// Fetch a chunk from the replay file as low priority and buffer it for later use.
        /// </summary>
        FetchChunkBuffered,
        /// <summary>
        /// Write a chunk to the replay file.
        /// </summary>
        WriteChunk,
        /// <summary>
        /// Commit any buffered data and finalize the replay file.
        /// </summary>
        Commit,
        /// <summary>
        /// Discard any buffered data and destroy the replay file.
        /// </summary>
        Discard,
        /// <summary>
        /// Fetch the file header from the replay file.
        /// </summary>
        FetchHeader,
        /// <summary>
        /// Write the header to the replay file.
        /// </summary>
        WriteHeader,
        /// <summary>
        /// Fetch the file chunk table from the replay file.
        /// </summary>
        FetchTable,
        /// <summary>
        /// Fetch the initial state buffer from the replay file.
        /// </summary>
        FetchStateBuffer,
    }

    /// <summary>
    /// The priority of a file stream task request.
    /// All <see cref="ReplayFileTaskPriority.High"/> priority tasks will be pushed ahead of <see cref="ReplayFileTaskPriority.Normal"/> tasks.  
    /// </summary>
    internal enum ReplayStreamTaskPriority
    {
        /// <summary>
        /// The task is time critical and should be completed as fast as possible.
        /// </summary>
        High = 0,
        /// <summary>
        /// The task is not time critical and may be held off until other important tasks have completed.
        /// </summary>
        Normal,
    }

    internal struct ReplayStreamTaskRequest
    {
        // Public
        public ReplayStreamTaskID taskID;
        public ReplayStreamRequest task;
        public ReplayStreamTaskPriority priority;
        public object data;
    }

    /// <summary>
    /// Internal structure used for managing unique task id's for threaded task requests.
    /// The structure is thread safe.
    /// </summary>
    internal struct ReplayStreamTaskID
    {
        // Private
        private static List<int> usedTasks = new List<int>();
        private int id;

        // Public
        /// <summary>
        /// Get a task id that is initialized to the default value.
        /// </summary>
        public static ReplayStreamTaskID empty = new ReplayStreamTaskID { id = -1 };

        // Constructor
        private ReplayStreamTaskID(int id)
        {
            this.id = id;
        }

        // Methods
        /// <summary>
        /// Generate a unique task ID.
        /// </summary>
        /// <returns>A <see cref="ReplayStreamTaskID"/> that is unique for the current session</returns>
        public static ReplayStreamTaskID GenerateID()
        {
            lock (usedTasks)
            {
                int current = 0;
                int id = -1;

                while (id == -1)
                {
                    // Increase search
                    int temp = current++ * (27 + current);

                    // We have found a valid id
                    if (usedTasks.Contains(temp) == false)
                        id = temp;
                }

                // Create a task id
                return new ReplayStreamTaskID(id);
            }
        }

        /// <summary>
        /// Release a unique task ID. use this to allow previously assigned id's to be reused.
        /// </summary>
        /// <param name="taskID">The task ID to release</param>
        public static void ReleaseID(ReplayStreamTaskID taskID)
        {
            lock (usedTasks)
            {
                // Remove if present
                if (usedTasks.Contains(taskID.id) == true)
                    usedTasks.Remove(taskID.id);
            }
        }
    }

    

    /// <summary>
    /// Represents a file storage target were replay data can be stored persistently between game sessions.
    /// The file target has an unlimited storage capacity but you should ensure that files do not get too large by optimizing your replay objects.
    /// </summary>
    public abstract partial class ReplayStreamTarget : ReplayStorageTarget, IDisposable
    {
        // Types
        /// <summary>
        /// The stream format used to store the replay data.
        /// </summary>
        public enum ReplayStreamFormat
        {
            /// <summary>
            /// A highly optimized custom binary format.
            /// It is highly recommended that binary repolay files are used for best performance and file sizes.
            /// </summary>
            Binary,
            /// <summary>
            /// Use JSON text format to store the data.
            /// This will generate much much larger files than binary.
            /// </summary>
            Json,
        }

        /// <summary>
        /// The access mode of the stream target.
        /// </summary>
        public enum AccessMode
        {
            /// <summary>
            /// Read access only.
            /// </summary>
            Read,
            /// <summary>
            /// Write access only.
            /// </summary>
            Write,
            /// <summary>
            /// Read and write access.
            /// </summary>
            ReadWrite,
        }

        protected class ReplayStreamContext
        {
            // Public
            public ReplayStreamHeader header = new ReplayStreamHeader();
            public ReplayStreamChunkTable chunkTable = new ReplayStreamChunkTable();
            public ReplayStreamChunk chunk = null;
            public ReplayStreamBuffer buffer = new ReplayStreamBuffer();
            public ReplayInitialDataBuffer initialStateBuffer = new ReplayInitialDataBuffer();
            public ReplayStreamSource sourceStream = null;
            public ReplayStreamCompression compressionStream = null;

            // Constructor
            public ReplayStreamContext(int chunkSize)
            {
                chunk = new ReplayStreamChunk(ReplayStreamChunk.startChunkID, chunkSize);
            }

            // Methods
            public void DisposeStreams()
            {
                // Dispose of the source stream
                if (sourceStream != null && sourceStream.IsDisposed == false)
                    sourceStream.Dispose();

                // Dispose of the compression stream
                if (compressionStream != null && compressionStream.IsDisposed == false)
                    compressionStream.Dispose();

                sourceStream = null;
                compressionStream = null;
            }
        }

        // Protected
        protected const ReplayCompressionLevel headerCompression = ReplayCompressionLevel.None;
        protected const ReplayCompressionLevel initialStateBufferCompression = ReplayCompressionLevel.Optimal;
        protected const ReplayCompressionLevel chunkTableCompression = ReplayCompressionLevel.Optimal;
                
        protected Stream targetStream = null;
        protected IReplayStreamProvider targetStreamProvider = null;
        protected ReplayCompressionLevel compressionLevel = ReplayCompressionLevel.Optimal;
        protected ReplayStreamContext context = null;
        protected HashSet<int> processingChunkRequests = new HashSet<int>();
        protected int chunkSize = defaultChunkSize;
        protected int chunkIdGenerator = ReplayStreamChunk.startChunkID;
        
        // Public
        /// <summary>
        /// The ideal number of chunks that are stored as a block. Larger chunk sizes allow for better compression and seeking but cost more memory.
        /// </summary>
        public const int defaultChunkSize = 24; // 24 snapshots per chunk        

        /// <summary>
        /// Use this value to control how many identical sequentional frames can be combined while recording.
        /// Higher values will result in reduced files sizes but higher CPU usage. Lower values will result in larger file sizes but lower CPU usage.
        /// Default value = 16.
        /// </summary>
        public int maxCombineIdenticalFramesDepth = 16;

        // Properties
        /// <summary>
        /// Get the amount of time in seconds that the current recording lasts.
        /// </summary>
        public override float Duration
        {
            get
            {
                // Lock thread access
                lock (context)
                {
                    // Get duration
                    return context.header.duration;
                }
            }
        }

        /// <summary>
        /// Get the amount of bytes required to store the replay data.
        /// </summary>
        public override int MemorySize
        {
            get
            {
                // Lock thread access
                lock (context)
                {
                    // Get memory size
                    return context.header.memorySize;
                }
            }
        }

        /// <summary>
        /// Get the initial state buffer for the target.
        /// </summary>
        public override ReplayInitialDataBuffer InitialStateBuffer
        {
            get
            {
                // Lock thread access
                lock (context)
                {
                    // Get the initial state buffer
                    return context.initialStateBuffer;
                }
            }
        }

        /// <summary>
        /// Returns a value indicating whether this stream target can be written to.
        /// </summary>
        public override bool CanWrite
        {
            get { return targetStream.CanWrite; }
        }

        /// <summary>
        /// Returns a value indicating whether this stream target can be read from.
        /// </summary>
        public override bool CanRead
        {
            get { return targetStream.CanRead; }
        }

        /// <summary>
        /// Returns a value indicating whether this stream target is disposed or not.
        /// </summary>
        public bool IsDisposed
        {
            get { return context == null; }
        }

        // Constructor
        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="streamProvider">The stream provider used to access the target stream</param>
        /// <param name="compressionLevel">The <see cref="ReplayCompressionLevel"/> used to store the data</param>
        /// <param name="chunkSize">The size of a stream chunk</param>
        public ReplayStreamTarget(IReplayStreamProvider streamProvider, ReplayCompressionLevel compressionLevel = ReplayCompressionLevel.Optimal, int chunkSize = defaultChunkSize)
        {
            // Check for no provider
            if (streamProvider == null) throw new ArgumentNullException("streamProvider");

            this.targetStream = null;   // Target stream must be provided by overriding 'OpenReplayStream()'
            this.targetStreamProvider = streamProvider;
            this.compressionLevel = compressionLevel;
            this.chunkSize = chunkSize;

            // Create context
            this.context = new ReplayStreamContext(chunkSize);

            // Start service thread
            InitializeThreading();

            // Register resource incase user fails to dispose
            ReplayCleanupUtility.RegisterUnreleasedResource(this);
        }

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="targetStream">The target stream object</param>
        /// <param name="compressionLevel">The <see cref="ReplayCompressionLevel"/> used to store the data</param>
        /// <param name="chunkSize">The size of a stream chunk</param>
        public ReplayStreamTarget(Stream targetStream, ReplayCompressionLevel compressionLevel = ReplayCompressionLevel.Optimal, int chunkSize = defaultChunkSize)
        {
            // Validate stream
            if (targetStream == null) throw new ArgumentNullException("targetStream");
            if (targetStream.CanSeek == false) throw new ArgumentException("targetStream must be seekable");

            this.targetStream = targetStream;
            this.compressionLevel = compressionLevel;
            this.chunkSize = chunkSize;

            // Create context
            this.context = new ReplayStreamContext(chunkSize);

            // Start service thread
            InitializeThreading();

            // Register resource incase user fails to dispose
            ReplayCleanupUtility.RegisterUnreleasedResource(this);
        }

        // Methods
        /// <summary>
        /// Called when the target will discard its recorded data.
        /// </summary>
        protected virtual void OnDiscardRecording() { }

        /// <summary>
        /// Called when the target needs to access the stream object for read or write access.
        /// </summary>
        /// <param name="mode">The <see cref="ReplayStreamMode"/> used to open the stream source</param>
        /// <returns></returns>
        protected virtual ReplayStreamSource OpenReplayStream(ReplayStreamMode mode)
        {
            if (targetStream == null)
            {
                // Use the provider to open the stream
                ReplayStreamSource source = targetStreamProvider.OpenReplayStream(mode);

                // Check for error
                if (source == null)
                    throw new IOException("The stream provider failed to create a valid replay stream source object");

                return source;
            }

            // Create the stream source
            return new ReplayStreamSource(targetStream, mode, true);
        }

        /// <summary>
        /// Closes all open file streams.
        /// Do not call this method during recording or playback.
        /// It should only be used when the <see cref="ReplayFileTarget"/> is no longer required.
        /// </summary>
        public virtual void Dispose()
        {
            if (IsDisposed == true)
                return;

            // Release thread resources
            ShutdownThreading();

            // Close the main streams
            context.DisposeStreams();

            // The file has been release by the user so no longer needs to be tracked
            ReplayCleanupUtility.UnregisterUnreleasedResource(this);
        }

        #region ReplayTarget_Base
        /// <summary>
        /// Attempts to store the specified <see cref="ReplaySnapshot"/> into the file stream using a chunk based system.
        /// The operation will be pushed to a streaming thread so that game systems will not be affected performance wise.
        /// </summary>
        /// <param name="state">The snapshot to write to file</param>
        public override void StoreSnapshot(ReplaySnapshot state)
        {
            // Check for disposed
            CheckDisposed();

            // Lock the chunk from other threads
            lock (context)
            {
                // Check for write mode
                if (context.sourceStream.IsWriting == false)
                    throw new InvalidOperationException("Cannot write to the replay stream beacuse it has not been prepared for writing. You may need to call 'PrepareTarget' first");

                // Check for trial
#if ULTIMATEREPLAY_TRIAL == true
                // Limit the file recording to 5 minutes
                if(state.TimeStamp > (5f * 60f))
                {
                    Debug.LogWarning("The trial version of Ultimate Replay only allows 5 minutes recording so you can sample the file size and streaming performance");
                    return;
                }
#endif

                // Set the time stamp
                context.header.duration = state.TimeStamp;

                // Store the state
                context.chunk.AddAndCombineSnapshot(state, maxCombineIdenticalFramesDepth);

                // Check if we can commit the chunk yet
                if (context.chunk.IsFull == true)
                {
                    // Clone the chunk so we can operate on it in the streaming thread
                    ReplayStreamChunk copy = context.chunk.Clone();

                    // Create a chunk write request
                    CreateTaskAsync(ReplayStreamRequest.WriteChunk, ReplayStreamTaskPriority.Normal, copy);

                    // Reset our working chunk
                    context.chunk = new ReplayStreamChunk(++chunkIdGenerator, chunkSize);
                }
            }
        }

        /// <summary>
        /// Attempts to fetch a replay chunk from the replay file containing the required replay data and then returns the desired <see cref="ReplaySnapshot"/> for the specified replay offset.
        /// Seeking may cause higher fetch times as random seek offsets cannot be predicted (Buffering).
        /// </summary>
        /// <param name="offset">The replay time offset to fetch the snapshot for</param>
        /// <returns>A napshot of the recording for the specified time offset</returns>
        public override ReplaySnapshot FetchSnapshot(float offset)
        {
            // Check for disposed
            CheckDisposed();

            bool fetchChunkRequired = false;

            // Get the last chunk
            ReplayStreamChunk previousChunk = context.chunk;

            lock (context)
            {
                // Check if we need to load the chunk
                if (context.chunk.FetchSnapshot(offset, PlaybackDirection) == null)
                {
                    // Try to get best matching chunk
                    int chunkID = 1;

                    foreach (ReplayStreamChunkTable.ReplayStreamChunkTableEntry entry in context.chunkTable)
                    {
                        if (PlaybackDirection == ReplayManager.PlaybackDirection.Forward)
                        {
                            if (offset < entry.endTimeStamp)
                            {
                                chunkID = entry.chunkID;
                                break;
                            }
                        }
                    }

                    if (context.buffer.HasLoadedChunkWithID(chunkID) == true)
                    {
                        context.chunk = context.buffer.GetLoadedChunkWithID(chunkID);
                    }
                    else
                    {
                        fetchChunkRequired = true;
                    }
                }

                if(fetchChunkRequired == true)
                {
                    // Check for write mode
                    if (context.sourceStream.IsReading == false)
                        throw new InvalidOperationException("Cannot read from the replay stream beacuse it has not been prepared for reading. You may need to call 'PrepareTarget' first");
                }
            }

            // Check if we need to fetch the chunk from the file
            if (fetchChunkRequired == true)
            {
                // Request a chunk fetch quickly
                ReplayStreamTaskID task = CreateTaskAsync(ReplayStreamRequest.FetchChunk, ReplayStreamTaskPriority.High, ReplayFileChunkFetchRequest.FetchByTimeStamp(offset));

                // Wait for completion - This may take a few 100 milliseconds
                WaitForSingleTask(task);

                // Check for timestamp inbetween chunks and select the best match
                if(PlaybackDirection == ReplayManager.PlaybackDirection.Forward && offset < context.chunk.ChunkStartTime)
                {
                    float deltaA = Mathf.Abs(offset - previousChunk.ChunkEndTime);
                    float deltaB = Mathf.Abs(context.chunk.ChunkStartTime - offset);

                    if (deltaA < deltaB)
                        context.chunk = previousChunk;
                }
            }

            // Chunk Prediciton - This section will issue a number of fetch requests for chunks that will likley be required soon
            lock (context)
            {
                // Fetch the next chunk
                int currentChunkID = context.chunk.chunkID;

                // Get a direction indicator value
                int incrementValue = (PlaybackDirection == ReplayManager.PlaybackDirection.Forward) ? 1 : -1;

                // Queue up some fetch requests
                int current = currentChunkID + incrementValue;

                
                // Check if the chunk is loaded - if so then skip it since we are only interested in non-loaded chunks
                while (/*context.buffer.HasLoadedChunkWithID(current) == true
                    && */Mathf.Abs(current) < chunkSize && Mathf.Abs(current) < context.header.chunkCount)
                {
                    // Increment the chunk id until we find a chunk that we have not loaded
                    current += incrementValue;
                }

                int start = currentChunkID;
                int end = currentChunkID + current;

                for (int i = start; i <= end; i += incrementValue)
                {
                    int fetchChunkID = i;

                    // Check if current is a valid chunk id
                    if (fetchChunkID >= 0 && fetchChunkID < context.header.chunkCount)
                    {
                        if (context.buffer.HasLoadedChunkWithID(fetchChunkID) == false && processingChunkRequests.Contains(fetchChunkID) == false)
                        {
                            // Begin streaming the next chunk from file
                            CreateTaskAsync(ReplayStreamRequest.FetchChunkBuffered, ReplayStreamTaskPriority.Normal, ReplayFileChunkFetchRequest.FetchByChunkID(fetchChunkID));

                            // Register the chunk as being requested
                            if (processingChunkRequests.Contains(fetchChunkID) == false)
                                processingChunkRequests.Add(fetchChunkID);
                        }


#if ULTIMATEREPLAY_NOTHREADING == false
                        // Chunk Dropout - This section will cleanup any hanging chunks which will likley not be required in the near future 
                        if (PlaybackDirection == ReplayManager.PlaybackDirection.Forward)
                        {
                            // Release chunks before the current chunk start time because we are replaying in forward mode
                            context.buffer.ReleaseOldChunks(context.chunk.ChunkStartTime, ReplayStreamBuffer.ReplayFileChunkReleaseMode.ChunksBefore);
                        }
                        else
                        {
                            // Release chunks after the current chunk end time because we are replaying in reverse
                            context.buffer.ReleaseOldChunks(context.chunk.ChunkEndTime, ReplayStreamBuffer.ReplayFileChunkReleaseMode.ChunksAfter);
                        }
#endif
                    }
                }

                // Get the best matching snapshot
                //return context.chunk.FetchSnapshot(offset, PlaybackDirection);
                // Get the best matching snapshot
                return context.chunk.FetchBestMatchingSnapshot(offset);
            }
        }

        public override ReplaySnapshot FetchSnapshot(int sequenceID)
        {
            // Check for disposed
            CheckDisposed();

            bool fetchChunkRequired = false;

            lock (context)
            {
                // Check if we need to load the chunk
                if (context.chunk.FetchSnapshot(sequenceID) == null)
                {
                    // Check for a match
                    if (context.buffer.HasLoadedChunk(sequenceID) == true)
                    {
                        // Make the chunk active
                        context.chunk = context.buffer.GetLoadedChunk(sequenceID);
                    }
                    else
                    {
                        // We need to fetch the chunk from file
                        fetchChunkRequired = true;
                    }
                }

                if(fetchChunkRequired == true)
                {
                    // Check for read mode
                    if (context.sourceStream.IsReading == false)
                        throw new InvalidOperationException("Cannot read from the replay stream beacuse it has not been prepared for reading. You may need to call 'PrepareTarget' first");
                }
            }

            // Check if we need to fetch the chunk from the file
            if (fetchChunkRequired == true)
            {
                // Request a chunk fetch quickly
                ReplayStreamTaskID task = CreateTaskAsync(ReplayStreamRequest.FetchChunk, ReplayStreamTaskPriority.High, ReplayFileChunkFetchRequest.FetchBySequenceID(sequenceID));

                // Wait for completion - This may take a few 100 milliseconds
                WaitForSingleTask(task);
            }

            // Chunk Prediciton - This section will issue a number of fetch requests for chunks that will likley be required soon
            lock (context)
            {
                // Fetch the next chunk
                int currentChunkID = context.chunk.chunkID;

                // Get a direction indicator value
                int incrementValue = (PlaybackDirection == ReplayManager.PlaybackDirection.Forward) ? 1 : -1;

                // Queue up some fetch requests
                int current = currentChunkID + incrementValue;

                // Check if the chunk is loaded - if so then skip it since we are only interested in non-loaded chunks
                while (context.buffer.HasLoadedChunkWithID(current) == true
                    && Mathf.Abs(current) < chunkSize)
                {
                    // Increment the chunk id until we find a chunk that we have not loaded
                    current += incrementValue;
                }

                // Check if current is a valid chunk id
                if (current >= 0 && current < context.header.chunkCount)
                {
                    if (context.buffer.HasLoadedChunkWithID(current) == false && processingChunkRequests.Contains(current) == false)
                    {
                        // Begin streaming the next chunk from file
                        CreateTaskAsync(ReplayStreamRequest.FetchChunkBuffered, ReplayStreamTaskPriority.Normal, ReplayFileChunkFetchRequest.FetchByChunkID(current));

                        // Register the chunk as being requested
                        if (processingChunkRequests.Contains(current) == false)
                            processingChunkRequests.Add(current);
                    }


#if ULTIMATEREPLAY_NOTHREADING == false
                    // Chunk Dropout - This section will cleanup any hanging chunks which will likley not be required in the near future 
                    if (PlaybackDirection == ReplayManager.PlaybackDirection.Forward)
                    {
                        // Release chunks before the current chunk start time because we are replaying in forward mode
                        context.buffer.ReleaseOldChunks(context.chunk.ChunkStartTime, ReplayStreamBuffer.ReplayFileChunkReleaseMode.ChunksBefore);
                    }
                    else
                    {
                        // Release chunks after the current chunk end time because we are replaying in reverse
                        context.buffer.ReleaseOldChunks(context.chunk.ChunkEndTime, ReplayStreamBuffer.ReplayFileChunkReleaseMode.ChunksAfter);
                    }
#endif
                }

                // Get the best matching snapshot
                return context.chunk.FetchBestMatchingSequenceSnapshot(sequenceID, PlaybackDirection); //context.chunk.FetchBestMatchingSnapshot(sequenceID);
            }
        }

        /// <summary>
        /// Called by the replay system when file target preparation is required.
        /// </summary>
        /// <param name="mode">The <see cref="ReplayTargetTask"/> that the target should prepare for</param>
        public override void PrepareTarget(ReplayTargetTask mode)
        {
            // Check for target disposed
            CheckDisposed();

            switch (mode)
            {
                case ReplayTargetTask.Commit:
                    {
                        ReplayStreamTaskID task = ReplayStreamTaskID.empty;

                        // Check if the working chunk has any data that should be flushed
                        lock (context)
                        {
                            if (context.chunk.SnapshotCount > 0)
                            {
                                // Copy the chunk so we can work on it in the streaming thread
                                ReplayStreamChunk copy = context.chunk.Clone();

                                // Write the working chunk
                                task = CreateTaskAsync(ReplayStreamRequest.WriteChunk, ReplayStreamTaskPriority.High, copy);

                                // Set the flushing flag - force wait for completion
                                context.chunk = new ReplayStreamChunk(ReplayStreamChunk.startChunkID, chunkSize);
                            }
                        }

                        // Wait for the task to complete
                        if (task.Equals(ReplayStreamTaskID.empty) == false)
                            WaitForSingleTask(task);

#if ULTIMATEREPLAY_NOTHREADING
                        // Process all waiting tasks on the main thread - all chunks will be waiting to be written and registered with the chunk table
                        StreamThreadProcessAllWaitingTasks();
#endif

                        // We can finally commit the data
                        task = CreateTaskAsync(ReplayStreamRequest.Commit, ReplayStreamTaskPriority.High);

                        // Wait for the commit to complete before returning
                        WaitForSingleTask(task);

#if ULTIMATEREPLAY_NOTHREADING
                        // Process all waiting tasks on the main thread
                        StreamThreadProcessAllWaitingTasks();
#endif
                        break;
                    }

                case ReplayTargetTask.Discard:
                    {
                        // Run the task on the background thread
                        ReplayStreamTaskID task = CreateTaskAsync(ReplayStreamRequest.Discard);

                        // Wait for the discard to complete before returning
                        WaitForSingleTask(task);

#if ULTIMATEREPLAY_NOTHREADING
                        // Process all waiting tasks on the main thread
                        StreamThreadProcessAllWaitingTasks();
#endif
                        break;
                    }

                case ReplayTargetTask.PrepareWrite:
                    {
                        // Dispose of any open streams
                        context.DisposeStreams();

                        // Create new header
                        context.header = new ReplayStreamHeader();

                        // Open temp stream
                        context.sourceStream = OpenReplayStream(ReplayStreamMode.WriteOnly);
                        context.compressionStream = new ReplayStreamCompression(ReplayStreamMode.WriteOnly);


                        // Reset chunk generator
                        chunkIdGenerator = ReplayStreamChunk.startChunkID;

                        // Write the header - We will overwirte thislater during the commit stage so most data can be meaningless
                        ReplayStreamTaskID headerTask = CreateTaskAsync(ReplayStreamRequest.WriteHeader, ReplayStreamTaskPriority.High);

                        // Wait for the header to be written
                        WaitForSingleTask(headerTask);

#if ULTIMATEREPLAY_NOTHREADING
                        // Process all waiting tasks on the main thread
                        StreamThreadProcessAllWaitingTasks();
#endif
                        break;
                    }

                case ReplayTargetTask.PrepareRead:
                    {
                        // Dispose of any open streams
                        context.DisposeStreams();

                        // Open the file for reading
                        context.sourceStream = OpenReplayStream(ReplayStreamMode.ReadOnly);
                        context.compressionStream = new ReplayStreamCompression(ReplayStreamMode.ReadOnly);

                        // Request the file header and wait for completion
                        ReplayStreamTaskID headerTask = CreateTaskAsync(ReplayStreamRequest.FetchHeader, ReplayStreamTaskPriority.High);

                        // Wait for the header to be loaded - We need it before we can accept fetch calls
                        WaitForSingleTask(headerTask);

                        // Request the chunk table and wait for completion
                        ReplayStreamTaskID tableTask = CreateTaskAsync(ReplayStreamRequest.FetchTable, ReplayStreamTaskPriority.High);

                        WaitForSingleTask(tableTask);

                        // Request the initial state buffer and wait for completion
                        ReplayStreamTaskID bufferTask = CreateTaskAsync(ReplayStreamRequest.FetchStateBuffer, ReplayStreamTaskPriority.High);

                        WaitForSingleTask(bufferTask);

#if ULTIMATEREPLAY_NOTHREADING
                        // Fetch the header and chunk table before we can identify the chunks that the file contains
                        StreamThreadProcessAllWaitingTasks();

                        // Fetch all chunks in the file immediatley - Be very carefull - large files may cause out of memory issues of have long wait times on the main thread
                        foreach (ReplayStreamChunkTable.ReplayStreamChunkTableEntry chunkEntry in context.chunkTable)
                        {
                            CreateTaskAsync(ReplayStreamRequest.FetchChunkBuffered, ReplayStreamTaskPriority.High, ReplayFileChunkFetchRequest.FetchByChunkID(chunkEntry.chunkID));
                        }

                        // Process all waiting tasks on the main thread
                        StreamThreadProcessAllWaitingTasks();
#endif
                        break;
                    }
            }
        }
        #endregion

        #region StreamThread
        private ReplayStreamTaskID CreateTaskAsync(ReplayStreamRequest task, ReplayStreamTaskPriority priority = ReplayStreamTaskPriority.Normal, object data = null)
        {
            // Create a task id
            ReplayStreamTaskID taskID = ReplayStreamTaskID.GenerateID();

            // Create the request
            ReplayStreamTaskRequest request = new ReplayStreamTaskRequest
            {
                taskID = taskID,
                task = task,
                priority = priority,
                data = data,
            };

            // Lock the collection from other threads
            lock (threadTasks)
            {
                // Push the request to the queue
                threadTasks.Add(request);

                // Sort based on priority
                threadTasks.Sort((x, y) =>
                {
                    // Check for higher priority
                    return x.priority.CompareTo(y.priority);
                });
            }

            return taskID;
        }

        /// <summary>
        /// Causes the calling thread to wait until the specified task has completed on the streaming thread.
        /// If the thread is not running or has been stopped during waiting then an exception will occur to break the infinite loop.
        /// </summary>
        /// <param name="taskID">The task to wait for. If the task is not in the thread queue then this method will do nothing</param>
        private void WaitForSingleTask(ReplayStreamTaskID taskID)
        {
#if ULTIMATEREPLAY_NOTHREADING
            // Thread operations are performed on the main thread after commit
            if(threadStarted == false)
                return;
#endif

            // Check for thread flag - We need the thread to be running or we will wait infinitley
            if (threadStarted == false || threadRunning == false)
                throw new InvalidOperationException("File operations cannot be awaited due to the current state of the file streamer: Stream thread is not running");

            // We need to wait for the task
            while (true)
            {
                // Make sure our thread is still running
                if (streamThread.IsAlive == false)
                    throw new ThreadStateException("The stream thread was aborted unexpectedly. Waiting was canceled to avoid infinite waiting but this may cause the state of the file streamer to be corrupted");

                bool foundTask = false;

                // Lock access to the thread tasks
                lock (threadTasks)
                {
                    // Check if the task is still in the queue
                    foreach (ReplayStreamTaskRequest request in threadTasks)
                    {
                        if (request.taskID.Equals(taskID) == true)
                        {
                            // We can release the task id now
                            ReplayStreamTaskID.ReleaseID(taskID);

                            foundTask = true;
                            break;
                        }
                    }
                }

                // The task is no longer in the queue
                if (foundTask == false)
                    break;

                // Wait for a bit while the thread works
                Thread.Sleep(5);
            }
        }
        #endregion
        
        private void CheckDisposed()
        {
            if (IsDisposed == true)
                throw new ObjectDisposedException("Replay File Target");
        }

        /// <summary>
        /// Create a <see cref="ReplayStreamTarget"/> from the specified stream object.
        /// </summary>
        /// <param name="stream">The target stream to read from or write to</param>
        /// <param name="fileFormat">The <see cref="ReplayStreamFormat"/> used to write the replay data</param>
        /// <param name="compressionLevel">The <see cref="ReplayCompressionLevel"/> used when writing the replay data</param>
        /// <param name="chunkSize">The size of a stream chunk</param>
        /// <returns>A <see cref="ReplayStreamTarget"/> instance</returns>
        public static ReplayStreamTarget CreateReplayStream(Stream stream, ReplayStreamFormat fileFormat = ReplayStreamFormat.Binary, ReplayCompressionLevel compressionLevel = ReplayCompressionLevel.Optimal, int chunkSize = ReplayStreamTarget.defaultChunkSize)
        {
            ReplayStreamTarget target = null;

            if(fileFormat == ReplayStreamFormat.Binary)
            {
                target = new ReplayStreamBinaryTarget(stream, compressionLevel, chunkSize);
            }
            else if(fileFormat == ReplayStreamFormat.Json)
            {
#if ULTIMATEREPLAY_JSON
                target = new ReplayStreamJsonTarget(stream, compressionLevel, chunkSize);
#endif
            }

            return target;
        }
    }
}