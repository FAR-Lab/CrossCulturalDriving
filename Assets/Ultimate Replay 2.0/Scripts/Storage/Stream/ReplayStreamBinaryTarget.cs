using System;
using System.IO;

namespace UltimateReplay.Storage
{
    public partial class ReplayStreamBinaryTarget : ReplayStreamTarget, IDisposable
    {  
        // Constructor
        public ReplayStreamBinaryTarget(IReplayStreamProvider streamProvider, ReplayCompressionLevel compressionLevel = ReplayCompressionLevel.Optimal, int chunkSize = defaultChunkSize)
            : base(streamProvider, compressionLevel, chunkSize)
        {
            // Do nothing
        }

        public ReplayStreamBinaryTarget(Stream targetStream, ReplayCompressionLevel compressionLevel = ReplayCompressionLevel.Optimal, int chunkSize = defaultChunkSize)
            : base(targetStream, compressionLevel, chunkSize)
        {
            // Do nothing
        }

        // Methods
        #region StreamThreadTasks
        protected override void ThreadWriteReplayChunk(ReplayStreamChunk chunk)
        {
            // Lock thread access
            lock (context)
            {
                // Check for valid stream
                if (context.sourceStream != null)
                {
                    // Get the file pointer for the chunk - offsets are 0 based so we need to account for the header size and the data header size of the main header
                    int pointer = context.sourceStream.Position - (context.header.dataOffset + ReplayStreamDataHeader.headerSize);

                    // Add an entry in the chunk table
                    context.chunkTable.CreateEntry(chunk.chunkID, chunk.ChunkStartTime, chunk.ChunkEndTime, (short)chunk.ChunkStartSequenceID, (short)chunk.ChunkEndSequenceID, pointer);

                    // Encode the chunk 
                    EncodeReplayDataToStream(chunk, compressionLevel);
                }
            }
        }

        protected override ReplayStreamChunk ThreadReadReplayChunk(ReplayFileChunkFetchRequest fetchData)// float timeStamp)
        {
            // Check for error
            if (context.sourceStream == null)
                return null;

            int pointer = -1;

            if (fetchData.requestType == ReplayFileChunkFetchRequest.FetchRequestType.FetchByChunkID)
            {
                // Try to get the file pointer of the chunk from the id
                pointer = context.chunkTable.GetPointerForChunk(fetchData.idValue);

                // Check for invalid pointer
                if (pointer == -1)
                {
                    return null;
                }
            }
            else if (fetchData.requestType == ReplayFileChunkFetchRequest.FetchRequestType.FetchBySequenceID)
            {
                // Try to get the file pointer of the chunk from the id
                pointer = context.chunkTable.GetPointerForSequenceID(fetchData.idValue);

                // Check for invalid pointer
                if (pointer == -1)
                {
                    return null;
                }
            }
            else if (fetchData.requestType == ReplayFileChunkFetchRequest.FetchRequestType.FetchByTimeStamp)
            {
                // Get the chunk for the time stamp
                pointer = context.chunkTable.GetPointerForTimeStamp(fetchData.timeStampValue);

                // Check for invalid pointer
                if (pointer == -1)
                {
                    return null;
                }
            }


            // Seek to offset - jump over the header and chunk table data
            int fileOffset = context.header.dataOffset + ReplayStreamDataHeader.headerSize + pointer;

            // Move to chunk location
            context.sourceStream.Seek(fileOffset, SeekOrigin.Begin);

            // Create a chunk to hold the data
            ReplayStreamChunk chunk = new ReplayStreamChunk(0, chunkSize);

            // Decode the replay chunk
            DecodeReplayDataFromStream(chunk);

            // Get the chunk data
            return chunk;
        }

        protected override void ThreadWriteReplayHeader()
        {
            // Lock thread access
            lock (context)
            {
                context.header.dataOffset = ReplayStreamHeader.headerSize;
                context.header.chunkCount = context.chunkTable.Count;

                // Encode the header
                EncodeReplayDataToStream(context.header, headerCompression);
            }
        }

        protected override void ThreadWriteReplayChunkTable()
        {
            // Lock thread access
            lock (context)
            {
                // Check for closed stream
                if (context.sourceStream == null)
                    return;

                // Select the compression level
                ReplayCompressionLevel compression = (compressionLevel == ReplayCompressionLevel.Optimal) ? chunkTableCompression : ReplayCompressionLevel.None;

                // Encode the chunk table
                EncodeReplayDataToStream(context.chunkTable, compression);
            }
        }

        protected override void ThreadWriteInitialStateBuffer()
        {
            // Lock thread access
            lock (context)
            {
                // Check for closed stream
                if (context.sourceStream == null)
                    return;

                // Select the compression level
                ReplayCompressionLevel compression = (compressionLevel == ReplayCompressionLevel.Optimal) ? initialStateBufferCompression : ReplayCompressionLevel.None;

                EncodeReplayDataToStream(context.initialStateBuffer, compression);
            }
        }

        protected override void ThreadFetchReplayHeader()
        {
            // Create the header
            ReplayStreamHeader header = new ReplayStreamHeader();

            // Lock thread access
            lock (context)
            {
                // Check for closed stream
                if (context.sourceStream == null)
                {
                    context.header = header;
                    return;
                }

                // Move stream to start
                context.sourceStream.Seek(0, SeekOrigin.Begin);

                // Read the stream header
                header = DecodeReplayDataFromStream<ReplayStreamHeader>();

                // Assign the new header
                context.header = header;
            }
        }

        protected override void ThreadFetchReplayChunkTable()
        {
            // Create a chunk table
            ReplayStreamChunkTable table = new ReplayStreamChunkTable();

            // Lock thread access
            lock (context)
            {
                // Check for closed stream
                if (context.sourceStream == null)
                {
                    context.chunkTable = new ReplayStreamChunkTable();
                    return;
                }

                // Get the file offset
                int offset = context.header.chunkTableOffset;

                // Seek stream
                context.sourceStream.Seek(offset, SeekOrigin.Begin);

                // Read the chunk table
                DecodeReplayDataFromStream(table);

                // Assign the loaded table
                context.chunkTable = table;
            }
        }

        protected override void ThreadFetchInitialStateBuffer()
        {
            // Create a state buffer
            ReplayInitialDataBuffer buffer = new ReplayInitialDataBuffer();

            // Lock thread access
            lock (context)
            {
                // Check for closed stream
                if (context.sourceStream == null)
                {
                    context.initialStateBuffer = new ReplayInitialDataBuffer();
                    return;
                }

                // Get the file offset
                int offset = context.header.stateBufferOffset;

                // Seek stream
                context.sourceStream.Seek(offset, SeekOrigin.Begin);

                // Read the initial state buffer
                DecodeReplayDataFromStream(buffer);

                // Assign the loaded state buffer
                context.initialStateBuffer = buffer;
            }
        }
        #endregion

        private void EncodeReplayDataToStream(IReplayStreamSerialize serializable, ReplayCompressionLevel compressionLevel)
        {
            ReplayStreamDataHeader header = new ReplayStreamDataHeader
            {
                compressionLevel = compressionLevel,
                dataSize = 0,
            };

            // Encode the replay data
            byte[] replayData = context.compressionStream.Encode(serializable, compressionLevel);

            // Update the header data size
            header.dataSize = (uint)replayData.Length;

            // Encode the header data
            byte[] headerData = context.compressionStream.Encode(header, headerCompression);


            // Write the header then data
            context.sourceStream.Write(headerData);
            context.sourceStream.Write(replayData);
        }

        private void DecodeReplayDataFromStream(IReplayStreamSerialize serializable)
        {
            // Read the header data
            byte[] headerData = context.sourceStream.Read(ReplayStreamDataHeader.headerSize);

            // Decode the header
            ReplayStreamDataHeader header = context.compressionStream.Decode<ReplayStreamDataHeader>(headerData, headerCompression);


            // Read the actual data
            byte[] replayData = context.sourceStream.Read((int)header.dataSize);

            // Decode the data
            context.compressionStream.Decode(serializable, replayData, header.compressionLevel);
        }

        private T DecodeReplayDataFromStream<T>() where T : IReplayStreamSerialize, new()
        {
            // Read the header data
            byte[] headerData = context.sourceStream.Read(ReplayStreamDataHeader.headerSize);

            // Decode the header
            ReplayStreamDataHeader header = context.compressionStream.Decode<ReplayStreamDataHeader>(headerData, headerCompression);

            // Read the actual data
            byte[] replayData = context.sourceStream.Read((int)header.dataSize);

            // Decode the data
            return context.compressionStream.Decode<T>(replayData, header.compressionLevel);
        }
    }
}