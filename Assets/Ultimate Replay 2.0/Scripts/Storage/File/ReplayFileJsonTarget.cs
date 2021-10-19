using UltimateReplay.Core;

#if ULTIMATEREPLAY_JSON
namespace UltimateReplay.Storage
{
    public class ReplayFileJsonTarget : ReplayFileTarget
    {
        // Private
        private ReplayStreamJsonTarget jsonStream = null;

        internal override ReplayManager.PlaybackDirection PlaybackDirection
        {
            get { return base.PlaybackDirection; }
            set
            {
                base.PlaybackDirection = value;
                jsonStream.PlaybackDirection = value;
            }
        }

        public override float Duration
        {
            get { return jsonStream.Duration; }
        }

        public override int MemorySize
        {
            get { return jsonStream.MemorySize; }
        }

        public override ReplayInitialDataBuffer InitialStateBuffer
        {
            get { return jsonStream.InitialStateBuffer; }
        }

        // Constructor
        public ReplayFileJsonTarget(string filePath, ReplayStreamTarget.AccessMode accessMode = ReplayStreamTarget.AccessMode.Read, int chunkSize = ReplayStreamTarget.defaultChunkSize)
            : base(filePath, accessMode, ReplayCompressionLevel.None)
        {
            // Open the stream
            this.jsonStream = new ReplayStreamJsonTarget(this, ReplayCompressionLevel.None, chunkSize);


            // Register this as a disposable resource which must be released
            ReplayCleanupUtility.RegisterUnreleasedResource(this);

            // Unregister base stream because we will be wrapping it and handling disposal
            ReplayCleanupUtility.UnregisterUnreleasedResource(this.jsonStream);
        }

        // Methods
        public override void Dispose()
        {
            this.jsonStream.Dispose();
            this.jsonStream = null;

            // Unregister resource
            ReplayCleanupUtility.UnregisterUnreleasedResource(this);
        }

        public override ReplaySnapshot FetchSnapshot(float timeStamp)
        {
            return jsonStream.FetchSnapshot(timeStamp);
        }

        public override ReplaySnapshot FetchSnapshot(int sequenceID)
        {
            return jsonStream.FetchSnapshot(sequenceID);
        }

        public override void PrepareTarget(ReplayTargetTask mode)
        {
            jsonStream.maxCombineIdenticalFramesDepth = maxCombineIdenticalFramesDepth;

            jsonStream.PrepareTarget(mode);
        }

        public override void StoreSnapshot(ReplaySnapshot state)
        {
            jsonStream.StoreSnapshot(state);
        }
    }
}
#endif
