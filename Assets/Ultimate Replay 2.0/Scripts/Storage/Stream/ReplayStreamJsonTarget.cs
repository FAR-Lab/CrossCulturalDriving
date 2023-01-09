
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UltimateReplay.Core;
using UltimateReplay.Serializers;
using UnityEngine;

#if ULTIMATEREPLAY_JSON
using Newtonsoft.Json;

namespace UltimateReplay.Storage
{
    public class ReplayStreamJsonTarget : ReplayStreamTarget, IDisposable
    {
        // Private
        private JsonWriter writer = null;
        private ReplayStreamJsonSerializer jsonSerializer = null;
        private ReplayObjectSerializer objectSerializer = new ReplayObjectSerializer();

        // Properties
        public JsonWriter JsonWriter
        {
            get { return writer; }
        }

        // Constructor
        public ReplayStreamJsonTarget(IReplayStreamProvider streamProvider, ReplayCompressionLevel compressionLevel = ReplayCompressionLevel.Optimal, int chunkSize = defaultChunkSize)
            : base(streamProvider, compressionLevel, chunkSize)
        {
            // Do nothing
        }

        public ReplayStreamJsonTarget(Stream targetStream, ReplayCompressionLevel compressionLevel = ReplayCompressionLevel.Optimal, int chunkSize = defaultChunkSize)
            : base(targetStream, compressionLevel, chunkSize)
        {
            // Do nothing
        }

        // Methods
        protected override ReplayStreamSource OpenReplayStream(ReplayStreamMode mode)
        {
            ReplayStreamSource stream = base.OpenReplayStream(mode);


            if (mode == ReplayStreamMode.WriteOnly)
            {
                // Create json writer
                this.writer = new JsonTextWriter(new StreamWriter(stream.BaseStream));
                this.writer.Formatting = Formatting.Indented;

                // Create the serializer
                this.jsonSerializer = new ReplayStreamJsonSerializer(writer);

                // Root object start
                writer.WriteStartObject();
            }
            else if (mode == ReplayStreamMode.ReadOnly)
            {
                throw new NotSupportedException("Json format read support is not currently supported!");
            }


            return stream;
        }

        #region StreamThreadTasks
        protected override void ThreadCommitReplayStream()
        {
            // Write all data to file
            JsonWriter.Flush();

            base.ThreadCommitReplayStream();            
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
            //DecodeReplayDataFromStream(chunk);

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

                // Write the header entry
                writer.WritePropertyName("Replay Header");
                writer.WriteStartObject();
                {
                    // Write header as json
                    //EncodeToJson(context.header);
                    jsonSerializer.EncodeObject(context.header);
                }
                writer.WriteEndObject();                
                //writer.Flush();
            }

            // Replay data start
            writer.WritePropertyName("Replay Data");
            writer.WriteStartArray();
        }

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
                    writer.WriteStartObject();
                    {
                        // Write chunk info
                        //EncodeToJson(chunk);
                        jsonSerializer.EncodeObject(chunk);

                        writer.WritePropertyName("Replay Snapshots");
                        writer.WriteStartArray();
                        {
                            foreach(ReplaySnapshot snapshot in chunk.Snapshots)
                            {
                                writer.WriteStartObject();
                                {
                                    //EncodeToJson(snapshot);
                                    jsonSerializer.EncodeObject(snapshot);

                                    writer.WritePropertyName("Object States");
                                    writer.WriteStartArray();
                                    {
                                        foreach(ReplayIdentity id in snapshot.Identities)
                                        {
                                            writer.WriteStartObject();
                                            {
                                                writer.WritePropertyName("Replay Identity");
                                                writer.WriteValue(id.IDString);

                                                // Get the state data
                                                ReplayState state = snapshot.RestoreSnapshot(id);

                                                if (state != null)
                                                {
                                                    // Deserialize the state
                                                    objectSerializer.OnReplayDeserialize(state);

                                                    // Write the object serializer
                                                    //EncodeToJson(objectSerializer);
                                                    jsonSerializer.EncodeObject(objectSerializer);


                                                    // Write the components states
                                                    writer.WritePropertyName("Component States");
                                                    writer.WriteStartArray();
                                                    {
                                                        foreach (ReplayComponentData componentData in objectSerializer.ComponentStates)
                                                        {
                                                            writer.WriteStartObject();
                                                            {
                                                                //EncodeToJson(componentData);
                                                                jsonSerializer.EncodeObject(componentData);

                                                                // Get the serializer
                                                                Type serializerType = componentData.ResolveSerializerType();
                                                                bool didSerialize = false;

                                                                // Check for null type
                                                                if (serializerType != null)
                                                                {
                                                                    try
                                                                    {
                                                                        IReplaySerialize instance = (IReplaySerialize)Activator.CreateInstance(serializerType);

                                                                        componentData.ComponentStateData.PrepareForRead();
                                                                        instance.OnReplayDeserialize(componentData.ComponentStateData);

                                                                        //EncodeToJson(instance);
                                                                        jsonSerializer.EncodeObject(instance);
                                                                        didSerialize = true;
                                                                    }
                                                                    catch(Exception e)
                                                                    { 
                                                                    }
                                                                }

                                                                if (didSerialize == false)
                                                                {
                                                                    writer.WritePropertyName("State Data");
                                                                    writer.WriteValue(componentData.ComponentStateData.ToHexString());
                                                                }
                                                            }
                                                            writer.WriteEndObject();
                                                        }

                                                    }
                                                    writer.WriteEndArray();


                                                    objectSerializer.Reset();
                                                }
                                            }
                                            writer.WriteEndObject();
                                        }
                                    }
                                    writer.WriteEndArray();
                                }
                                writer.WriteEndObject();
                            }
                        }
                        writer.WriteEndArray();
                    }
                    writer.WriteEndObject();
                }
            }
        }

        protected override void ThreadWriteReplayChunkTable()
        {
            writer.WriteEndArray();

            // Lock thread access
            lock (context)
            {
                // Check for closed stream
                if (context.sourceStream == null)
                    return;

                // Select the compression level
                ReplayCompressionLevel compression = (compressionLevel == ReplayCompressionLevel.Optimal) ? chunkTableCompression : ReplayCompressionLevel.None;

                writer.WritePropertyName("Chunk Table");
                writer.WriteStartArray();
                {
                    foreach(ReplayStreamChunkTable.ReplayStreamChunkTableEntry tableEntry in context.chunkTable)
                    {
                        writer.WriteStartObject();
                        {
                            //EncodeToJson(tableEntry);
                            jsonSerializer.EncodeObject(tableEntry);
                        }
                        writer.WriteEndObject();
                    }
                }
                writer.WriteEndArray();
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

                //EncodeReplayDataToStream(context.initialStateBuffer, compression);

                writer.WritePropertyName("Initial State Buffer");
                writer.WriteStartArray();
                {
                    foreach(ReplayIdentity id in context.initialStateBuffer.Identities)
                    {
                        writer.WriteStartObject();
                        {
                            writer.WritePropertyName("Replay Identity");
                            jsonSerializer.EncodeValue(id);

                            // Write all states
                            writer.WritePropertyName("Initial States");
                            writer.WriteStartArray();
                            {
                                IList<ReplayInitialData> initialStates = context.initialStateBuffer.GetInitialStates(id);

                                if (initialStates != null)
                                {
                                    foreach (ReplayInitialData initialData in initialStates)
                                    {
                                        writer.WriteStartObject();
                                        {
                                            jsonSerializer.EncodeObject(initialData);
                                        }
                                        writer.WriteEndObject();
                                    }
                                }
                            }
                            writer.WriteEndArray();
                        }
                        writer.WriteEndObject();
                    }
                }
                writer.WriteEndArray();

                // Root object end
                writer.WriteEndObject();
                writer.Flush();
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
                //header = DecodeReplayDataFromStream<ReplayStreamHeader>();

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
                //DecodeReplayDataFromStream(table);

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
                //DecodeReplayDataFromStream(buffer);

                // Assign the loaded state buffer
                context.initialStateBuffer = buffer;
            }
        }
        #endregion

        private void EncodeToJson(object obj)
        {
            Type type = obj.GetType();

            // Get all members
            foreach (MemberInfo member in type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.GetProperty))
            {
                // Check for attribute
                if (member.IsDefined(typeof(ReplayTextSerializeAttribute), false) == true)
                {
                    // Get the attribute
                    ReplayTextSerializeAttribute attrib = member.GetCustomAttributes(typeof(ReplayTextSerializeAttribute), false)[0] as ReplayTextSerializeAttribute;

                    string displayName = member.Name;

                    if (attrib.OverrideName != null)
                        displayName = attrib.OverrideName;

                    // Write to json
                    writer.WritePropertyName(displayName);

                    object writeValue = null;

                    if (member is FieldInfo)
                    {
                        writeValue = ((FieldInfo)member).GetValue(obj);
                    }
                    else if (member is PropertyInfo)
                    {
                        writeValue = ((PropertyInfo)member).GetValue(obj, null);
                    }


                    // Check for replay id
                    if (writeValue is ReplayIdentity)
                        writeValue = ((ReplayIdentity)writeValue).IDString;

                    if (writeValue is Vector3)
                        writeValue = ((Vector3)writeValue).ToString();

                    if (writeValue is Quaternion)
                        writeValue = ((Quaternion)writeValue).ToString();

                    writer.WriteValue(writeValue);
                }
            }
        }
    }
}

#endif