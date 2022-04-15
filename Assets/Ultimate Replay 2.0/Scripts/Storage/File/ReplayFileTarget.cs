using System;
using System.IO;

namespace UltimateReplay.Storage
{
    /// <summary>
    /// A replay storage target used to stream replay data to and from file.
    /// </summary>
    public abstract class ReplayFileTarget : ReplayStorageTarget, IReplayStreamProvider, IDisposable
    {
        // Types
        /// <summary>
        /// The file format used when writing the replay to file.
        /// </summary>
        public enum ReplayFileFormat
        {
            /// <summary>
            /// A highly optimized custom binary file format.
            /// </summary>
            Binary,
            /// <summary>
            /// JSON text file.
            /// </summary>
            Json,
        }

        // Private
        private string filePath = null;
        private ReplayStreamTarget.AccessMode accessMode = ReplayStreamTarget.AccessMode.ReadWrite;

        // Public
        /// <summary>
        /// The default file extension for all replay files.
        /// </summary>
        public const string defaultExtension = ".replay";

        /// <summary>
        /// Use this value to control how many identical sequentional frames can be combined while recording.
        /// Higher values will result in reduced files sizes but higher CPU usage. Lower values will result in larger file sizes but lower CPU usage.
        /// Default value = 16.
        /// </summary>
        public int maxCombineIdenticalFramesDepth = 16;

        // Properties
        /// <summary>
        /// Get the file path of the target replay file.
        /// </summary>
        public string FilePath
        {
            get { return filePath; }
        }

        /// <summary>
        /// Get a value indicating whether the storage target can be written to.
        /// </summary>
        public override bool CanWrite
        {
            get { return accessMode == ReplayStreamTarget.AccessMode.Write || accessMode == ReplayStreamTarget.AccessMode.ReadWrite; }
        }

        /// <summary>
        /// Get a value indicating whether the storage target can be read from.
        /// </summary>
        public override bool CanRead
        {
            // A file stream can always be configured to read even if it was initially created for writing
            get { return true; }
        }

        // Constructor
        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="filePath">The file path of the target file</param>
        /// <param name="accessMode">The access mode of the file</param>
        /// <param name="compressionLevel">The <see cref="ReplayStreamCompression"/> used when creatiung a replay file</param>
        /// <param name="chunkSize">The size of a replay file chunk</param>
        public ReplayFileTarget(string filePath, ReplayStreamTarget.AccessMode accessMode = ReplayStreamTarget.AccessMode.Read, ReplayCompressionLevel compressionLevel = ReplayCompressionLevel.Optimal, int chunkSize = ReplayStreamTarget.defaultChunkSize)            
        {
            // Validate path
            if (filePath == null) throw new ArgumentNullException("filePath");
            if (string.IsNullOrEmpty(filePath) == true) throw new ArgumentException("filePath cannot be empty");

            // Make sure path has extension
            if (Path.HasExtension(this.filePath) == false)
                Path.ChangeExtension(this.filePath, defaultExtension);

            // Store file path
            this.filePath = filePath;
            this.accessMode = accessMode;
        }

        // Methods
        /// <summary>
        /// Called when the replay file should be opened for reading or writing.
        /// </summary>
        /// <param name="mode">The <see cref="ReplayStreamMode"/> required by the serializer</param>
        /// <returns>An opened stream instance</returns>
        public ReplayStreamSource OpenReplayStream(ReplayStreamMode mode)
        {
            // Get the full path
            string fullPath = filePath;

            if (File.Exists(fullPath) == true)
            {
                // Check for write only
                if (mode == ReplayStreamMode.WriteOnly)
                {
                    // Delete the file
                    File.Delete(fullPath);
                }
            }

            // Open file for reading and writing
            return new ReplayStreamSource(fullPath, mode);
        }
        
        /// <summary>
        /// Dispose of the file target.
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Discard the replay file.
        /// </summary>
        public void DiscardReplayStream()
        {
            // Delete the replay file
            if (File.Exists(filePath) == true)
                File.Delete(filePath);
        }

        /// <summary>
        /// Create a replay file target from the specified file.
        /// </summary>
        /// <param name="filePath">The filepath of the target file</param>
        /// <param name="overwriteExistingFile">True if existing files should be overwritten or false if not</param>
        /// <param name="fileFormat">The <see cref="ReplayFileFormat"/> used to serialize the replay data</param>
        /// <param name="compressionLevel">The <see cref="ReplayCompressionLevel"/> used when writing the replay file</param>
        /// <param name="chunkSize">The size of a file chunk</param>
        /// <returns>A file storage target instance</returns>
        public static ReplayFileTarget CreateReplayFile(string filePath, bool overwriteExistingFile = true, ReplayFileFormat fileFormat = ReplayFileFormat.Binary, ReplayCompressionLevel compressionLevel = ReplayCompressionLevel.Optimal, int chunkSize = ReplayStreamTarget.defaultChunkSize)
        {
            // Delete the target file
            if (File.Exists(filePath) == true && overwriteExistingFile == true)
                File.Delete(filePath);

            // Open for reading
            ReplayFileTarget target = null;

            if (fileFormat == ReplayFileFormat.Binary)
            {
                target = new ReplayFileBinaryTarget(filePath, ReplayStreamTarget.AccessMode.ReadWrite, compressionLevel, chunkSize);
            }
            else if(fileFormat == ReplayFileFormat.Json)
            {
                 
#if ULTIMATEREPLAY_JSON
                target = new ReplayFileJsonTarget(filePath, ReplayStreamTarget.AccessMode.ReadWrite, chunkSize);
#endif
            }
            
            return target;
        }

        /// <summary>
        /// Create a replay file target with a unique file path.
        /// </summary>
        /// <param name="folderPath">The hint folder path where the replay file should be created</param>
        /// <param name="extension">The hint file extension that the replay file should have</param>
        /// <param name="fileFormat">The <see cref="ReplayFileFormat"/> used to serialize the file</param>
        /// <param name="compressionLevel">The <see cref="ReplayCompressionLevel"/> used when writing the replay file</param>
        /// <param name="chunkSize">The size of a replay file chunk</param>
        /// <returns>A replay file storage target instance</returns>
        public static ReplayFileTarget CreateUniqueReplayFile(string folderPath = null, string extension = null, ReplayFileFormat fileFormat = ReplayFileFormat.Binary, ReplayCompressionLevel compressionLevel = ReplayCompressionLevel.Optimal, int chunkSize = ReplayStreamTarget.defaultChunkSize)
        {
            // Check for null folder
            if (string.IsNullOrEmpty(folderPath) == true)
                folderPath = Environment.CurrentDirectory;

            // Check for null extension
            if (string.IsNullOrEmpty(extension) == true)
                extension = defaultExtension;

            // Generate a unique file name
            string fileName = Path.GetRandomFileName();

            // Check for existing file
            while (File.Exists(Path.Combine(folderPath, fileName + extension)) == true)
            {
                // Get the file name
                fileName = Path.GetRandomFileName();
            }

            // Call through
            return CreateReplayFile(Path.Combine(folderPath, fileName) + extension, false, fileFormat, compressionLevel, chunkSize);
        }

        /// <summary>
        /// Open the specified replay file for reading.
        /// </summary>
        /// <param name="filePath">The filepath of the target replay file</param>
        /// <param name="fileFormat">The file format of the target file</param>
        /// <returns>A replay file target storage instance</returns>
        public static ReplayFileTarget ReadReplayFile(string filePath, ReplayFileFormat fileFormat = ReplayFileFormat.Binary)
        {
#if ULTIMATEREPLAY_JSON
            if (fileFormat == ReplayFileFormat.Json)
                return new ReplayFileJsonTarget(filePath, ReplayStreamTarget.AccessMode.Read);
#else
            if(fileFormat == ReplayFileFormat.Json)
                return null;
#endif

            // Create binary file reader
            return new ReplayFileBinaryTarget(filePath, ReplayStreamTarget.AccessMode.Read);
        }
    }
}
