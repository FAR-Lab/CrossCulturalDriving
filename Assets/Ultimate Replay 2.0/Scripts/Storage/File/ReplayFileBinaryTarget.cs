
using System;
using UltimateReplay.Core;

namespace UltimateReplay.Storage
{
    public class ReplayFileBinaryTarget : ReplayFileTarget
    {
        // Private
        private ReplayStreamBinaryTarget binaryStream = null;

        // Properties
        internal override ReplayManager.PlaybackDirection PlaybackDirection
        {
            get
            {
                CheckDisposed();
                return base.PlaybackDirection;
            }
            set
            {
                CheckDisposed();
                base.PlaybackDirection = value;
                binaryStream.PlaybackDirection = value;
            }
        }

        public override float Duration
        {
            get
            {
                CheckDisposed();
                return binaryStream.Duration;
            }
        }

        public override int MemorySize
        {
            get
            {
                CheckDisposed();
                return binaryStream.MemorySize;
            }
        }

        public override ReplayInitialDataBuffer InitialStateBuffer
        {
            get
            {
                CheckDisposed();
                return binaryStream.InitialStateBuffer;
            }
        }

        // Constructor
        public ReplayFileBinaryTarget(string filePath, ReplayStreamTarget.AccessMode accessMode = ReplayStreamTarget.AccessMode.Read, ReplayCompressionLevel compressionLevel = ReplayCompressionLevel.Optimal, int chunkSize = ReplayStreamTarget.defaultChunkSize)
            : base(filePath, accessMode, compressionLevel, chunkSize)
        {
            // Open the stream
            this.binaryStream = new ReplayStreamBinaryTarget(this, compressionLevel, chunkSize);


            // Register this as a disposable resource which must be released
            ReplayCleanupUtility.RegisterUnreleasedResource(this);

            // Unregister base stream because we will be wrapping it and handling disposal
            ReplayCleanupUtility.UnregisterUnreleasedResource(this.binaryStream);
        }

        // Methods
        public override void Dispose()
        {
            if (this.binaryStream != null)
            {
                this.binaryStream.Dispose();
                this.binaryStream = null;

                // Unregister resource
                ReplayCleanupUtility.UnregisterUnreleasedResource(this);
            }
        }

        public override ReplaySnapshot FetchSnapshot(float timeStamp)
        {
            return binaryStream.FetchSnapshot(timeStamp);
        }

        public override ReplaySnapshot FetchSnapshot(int sequenceID)
        {
            CheckDisposed();
            return binaryStream.FetchSnapshot(sequenceID);
        }

        public override void PrepareTarget(ReplayTargetTask mode)
        {
            binaryStream.maxCombineIdenticalFramesDepth = maxCombineIdenticalFramesDepth;

            CheckDisposed();
            binaryStream.PrepareTarget(mode);
        }

        public override void StoreSnapshot(ReplaySnapshot state)
        {
            CheckDisposed();
            binaryStream.StoreSnapshot(state);
        }

        private void CheckDisposed()
        {
            if (binaryStream == null)
                throw new ObjectDisposedException("File target has been disposed");
        }
    }
}
