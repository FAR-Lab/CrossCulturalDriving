using System;
using System.IO;
using System.Runtime.InteropServices;
using UltimateReplay.Core;

#if ULTIMATEREPLAY_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace UltimateReplay.Storage
{
    /// <summary>
    /// The main file header for replay files.
    /// Contains information about the replay data stored within the file.
    /// </summary>
    public struct ReplayStreamHeader : IReplayStreamSerialize
    {
        // Public
        /// <summary>
        /// Replay file identifier used to ensure that the specified file is actually a replay file.
        /// </summary>
        public const int replayIdentifier = ((byte)'U' |
                                            ((byte)'R' << 8) |
                                            ((byte)'2' << 16) |
                                            ((byte)'0' << 24));

        public static readonly int headerSize = 30; // 30 bytes required for main stream header

        /// <summary>
        /// The number of bytes used to store a ReplayIdentity structure in the stream.
        /// </summary>
        [ReplayTextSerialize("Identity Byte Size")]
        public ushort identityByteSize;
        /// <summary>
        /// The amount of size in bytes that the recording requires.
        /// </summary>
        [ReplayTextSerialize("Memory Size")]
        public int memorySize;
        /// <summary>
        /// The file offset for the main replay data.
        /// </summary>
        [ReplayTextSerialize("Data File offset")]
        public int dataOffset;
        /// <summary>
        /// The number of chunks that are stored in the stream.
        /// </summary>
        [ReplayTextSerialize("Chunk Count")]
        public int chunkCount;
        /// <summary>
        /// The negative file offset of the chunk table.
        /// The chunk table is stored at the very end of the file for performance and as a result the offset is from the end of the file.
        /// </summary>
        [ReplayTextSerialize("Chunk Table File Offset")]
        public int chunkTableOffset;
        /// <summary>
        /// The state buffer is stored after the chunk table and contains initial state information for all dynamic replay objects.
        /// </summary>
        [ReplayTextSerialize("State Buffer File Offset")]
        public int stateBufferOffset;
        /// <summary>
        /// The amount of time in seconds that the recording lasts.
        /// </summary>
        [ReplayTextSerialize("Duration")]
        public float duration;

        // Methods
        /// <summary>
        /// Called by the file streamer when the file header should be written to the specified binary stream.
        /// </summary>
        /// <param name="writer">A binary writer used to store the data</param>
        public void OnReplayStreamSerialize(BinaryWriter writer)
        {
            // Write identifier
            writer.Write(replayIdentifier);

            // Write header data
            writer.Write(ReplayIdentity.byteSize);
            writer.Write(memorySize);
            writer.Write(dataOffset);
            writer.Write(chunkCount);
            writer.Write(chunkTableOffset);
            writer.Write(stateBufferOffset);
            writer.Write(duration);
        }

        /// <summary>
        /// Called by the file streamer when the file header should be read from the specified binary stream.
        /// </summary>
        /// <param name="reader">A binary reader to read the data from</param>
        public void OnReplayStreamDeserialize(BinaryReader reader)
        {
            int identifier = reader.ReadInt32();

            // Check for the file identifier
            if (replayIdentifier != identifier)
                throw new FormatException("The specified stream does not contain valid UltimateReplay data");

            // Read header data
            identityByteSize = reader.ReadUInt16();
            memorySize = reader.ReadInt32();
            dataOffset = reader.ReadInt32();
            chunkCount = reader.ReadInt32();
            chunkTableOffset = reader.ReadInt32();
            stateBufferOffset = reader.ReadInt32();
            duration = reader.ReadSingle();
        }

#if ULTIMATEREPLAY_JSON
        public void OnReplayStreamSerialize(JsonWriter writer)
        {
            writer.WriteStartObject();
            {
                writer.WritePropertyName("IdentityByteSize"); writer.WriteValue(identityByteSize);
                writer.WritePropertyName("MemorySize"); writer.WriteValue(memorySize);
                writer.WritePropertyName("DataFileOffset"); writer.WriteValue(dataOffset);
                writer.WritePropertyName("ChunkCount"); writer.WriteValue(chunkCount);
                writer.WritePropertyName("ChunkTableFileOffset"); writer.WriteValue(chunkTableOffset);
                writer.WritePropertyName("StateBufferFileOffset"); writer.WriteValue(stateBufferOffset);
                writer.WritePropertyName("Duration"); writer.WriteValue(duration);
            }
            writer.WriteEndObject();
        }

        public void OnReplayStreamDeserialize(JsonReader reader)
        {
            JObject header = JObject.Load(reader);

            identityByteSize = header.GetValue("IdentityByteSize").Value<ushort>();
            memorySize = header.GetValue("MemorySize").Value<int>();
            dataOffset = header.GetValue("DataFileOffset").Value<int>();
            chunkCount = header.GetValue("ChunkCount").Value<int>();
            chunkTableOffset = header.GetValue("ChunkTableFileOffset").Value<int>();
            stateBufferOffset = header.GetValue("StateBufferFileOffset").Value<int>();
            duration = header.GetValue("Duration").Value<float>();
        }
#endif
    }

    public struct ReplayStreamDataHeader : IReplayStreamSerialize
    {
        // Public
        public static readonly int headerSize = 6; // 6 bytes required for serialized struct

        public ReplayCompressionLevel compressionLevel;
        public uint dataSize;

        // Methods
        public void OnReplayStreamSerialize(BinaryWriter writer)
        {
            writer.Write((ushort)compressionLevel);
            writer.Write(dataSize);
        }

        public void OnReplayStreamDeserialize(BinaryReader reader)
        {
            compressionLevel = (ReplayCompressionLevel)reader.ReadUInt16();
            dataSize = reader.ReadUInt32();
        }
    }
}
