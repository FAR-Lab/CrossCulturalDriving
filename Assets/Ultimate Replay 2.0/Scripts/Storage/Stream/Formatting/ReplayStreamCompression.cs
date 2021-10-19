using System;
using System.IO;
using System.IO.Compression;

namespace UltimateReplay.Storage
{
    /// <summary>
    /// The amount of compression to apply to a data stream.
    /// </summary>
    public enum ReplayCompressionLevel
    {
        /// <summary>
        /// No compression is applied and all data is left unchanged.
        /// </summary>
        None = 0,
        /// <summary>
        /// All data is compressed to the optimal level.
        /// </summary>
        Optimal,
    }

    public sealed class ReplayStreamCompression : IDisposable
    {
        // Private
        private static readonly int decompressBufferSize = (4 * 1024); // 4kb
        private static byte[] decompressBuffer = null;

        private MemoryStream compressionStream = new MemoryStream();
        private BinaryWriter writer = null;
        private BinaryReader reader = null;

        // Properties
        public bool IsDisposed
        {
            get
            {
                try
                {
                    DisposeCheck();
                    return false;
                }
                catch (ObjectDisposedException) { }
                {
                    // Is disposed
                    return true;
                }
            }
        }

        /// <summary>
        /// Get the <see cref="BinaryWriter"/> for the writable stream.
        /// If the stream is not currently writable then this value will be null.
        /// </summary>
        private BinaryWriter Writer
        {
            get
            {
                // Check if writing is enabled
                if (writer == null)
                    throw new InvalidOperationException("Failed to write to file stream. The stream is not writable in its current state");

                return writer;
            }
        }

        /// <summary>
        /// Get the <see cref="BinaryReader"/> for the readable stream. 
        /// If the stream is not currently readable then this value will be null.
        /// </summary>
        private BinaryReader Reader
        {
            get
            {
                // Check if reading is enabled
                if (reader == null)
                    throw new InvalidOperationException("Failed to read from file stream. The stream is not readable in its current state");

                return reader;
            }
        }

        // Constructor
        public ReplayStreamCompression(ReplayStreamMode streamMode)
        {
            // Check for writing
            if(streamMode == ReplayStreamMode.WriteOnly)
                writer = new BinaryWriter(compressionStream);

            // Check for reading
            if(streamMode == ReplayStreamMode.ReadOnly)
                reader = new BinaryReader(compressionStream);
        }

        // Methods
        public byte[] Encode(IReplayStreamSerialize serializable, ReplayCompressionLevel compressionLevel)
        {
            DisposeCheck();

            // Write the serializable object
            ReplayStreamSerializationUtility.StreamSerialize(serializable, writer);

            // Get the stream buffer - WARNING - this may have extra unrelated data at the end of the array
            byte[] data = compressionStream.ToArray();

            if (compressionLevel == ReplayCompressionLevel.Optimal)
            {
                // Compress the data
                data = CompressData(data, 0, data.Length, compressionLevel);
            }

            // Clear the memory buffer for re-use
            compressionStream.SetLength(0);

            return data;
        }

        public void Decode(IReplayStreamSerialize serializable, byte[] data, ReplayCompressionLevel compressionLevel)
        {
            DisposeCheck();

            // Check for compression
            if (compressionLevel == ReplayCompressionLevel.Optimal)
            {
                // Decompress the data
                data = DecompressData(data, compressionLevel);
            }

            // Write to the compression stream
            compressionStream.Write(data, 0, data.Length);

            // Reset to begining
            compressionStream.Seek(0, SeekOrigin.Begin);


            // Deserialize the object
            ReplayStreamSerializationUtility.StreamDeserialize(ref serializable, Reader);

            // Clear the memory buffer for re-use
            compressionStream.SetLength(0);
        }

        public T Decode<T>(byte[] data, ReplayCompressionLevel compressionLevel) where T : IReplayStreamSerialize, new()
        {
            DisposeCheck();

            // Check for compression
            if (compressionLevel == ReplayCompressionLevel.Optimal)
            {
                // Decompress the data
                data = DecompressData(data, compressionLevel);
            }

            // Write to the compression stream
            compressionStream.Write(data, 0, data.Length);

            // Reset to begining
            compressionStream.Seek(0, SeekOrigin.Begin);

            // Create the instance
            T serializable = new T();

            // Deserialize the object
            ReplayStreamSerializationUtility.StreamDeserialize(ref serializable, Reader);

            // Clear the memory buffer for re-use
            compressionStream.SetLength(0);

            return serializable;
        }

        public void Dispose()
        {
            if(IsDisposed == false)
            {
                // Close writer
                if (writer != null)
                    writer.Close();

                // Close reader
                if (reader != null)
                    reader.Close();

                // Dispose of stream and reset
                compressionStream.Dispose();
                writer = null;
                reader = null;
                compressionStream = null;
            }
        }

        /// <summary>
        /// Compress a data stream using the GZip compression algorithm.
        /// </summary>
        /// <param name="data">The input data to compress</param>
        /// <param name="level">The target compression level to use</param>
        /// <returns>The compressed data</returns>
        public static byte[] CompressData(byte[] data, int offset, int length, ReplayCompressionLevel level = ReplayCompressionLevel.Optimal)
        {
            // Check for no compression
            if (level == ReplayCompressionLevel.None)
                return data;

            // Create a memory stream to manage the array
            using (MemoryStream stream = new MemoryStream())
            {
                using (MemoryStream dataStream = new MemoryStream(data))
                {
                    // Create a gzip stream
                    using (GZipStream compressStream = new GZipStream(stream, CompressionMode.Compress))
                    {
                        //// Write the data for compression
                        //compressStream.Write(data, offset, length);

                        dataStream.CopyTo(compressStream);
                    }
                }

                // Get the compressed bytes
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Decompress a data stream using the GZip compression algorithm.
        /// </summary>
        /// <param name="data">The input data to decompress</param>
        /// <param name="level">The target compression level to use</param>
        /// <returns>The decompressed data</returns>
        public static byte[] DecompressData(byte[] data, ReplayCompressionLevel level = ReplayCompressionLevel.Optimal)
        {
            // Check for no compression
            if (level == ReplayCompressionLevel.None)
                return data;

            // Create our decompress buffer if it has not been allocated
            if (decompressBuffer == null)
                decompressBuffer = new byte[decompressBufferSize];

            // Create a memory stream to manage the array
            using (MemoryStream stream = new MemoryStream(data))
            {
                using (MemoryStream outputStream = new MemoryStream())
                {
                    // Create a gzip stream
                    using (GZipStream decompressStream = new GZipStream(stream, CompressionMode.Decompress))
                    {
                        //int readSize = 0;

                        //Read into our buffer while there is data left
                        //while ((readSize = decompressStream.Read(decompressBuffer, 0, decompressBufferSize)) > 0)
                        //    {
                        //        Write the data to our output stream
                        //    outputStream.Write(decompressBuffer, 0, readSize);
                        //    }

                        decompressStream.CopyTo(outputStream);
                    }

                    // Get the stream bytes
                    return outputStream.ToArray();
                }
            }
        }

        private void DisposeCheck()
        {
            // Check for already disposed
            if (compressionStream == null)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}
