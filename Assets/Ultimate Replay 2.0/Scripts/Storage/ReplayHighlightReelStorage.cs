using System;
using System.Collections.Generic;
using UltimateReplay.Core;

namespace UltimateReplay.Storage
{
    public class ReplayHighlightReelStorage : ReplayStorageTarget
    {
        // Private
        private List<ReplayStorageTarget> sourceStorage = new List<ReplayStorageTarget>();
        private ReplayInitialDataBuffer initialDataBuffer = new ReplayInitialDataBuffer();
        private int memorySize = 0;

        // Properties
        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override float Duration
        {
            get { return duration; }
        }

        public override int MemorySize
        {
            get { return memorySize; }
        }

        public override ReplayInitialDataBuffer InitialStateBuffer
        {
            get { return initialDataBuffer; }
        }

        // Constructor
        public ReplayHighlightReelStorage(params ReplayStorageTarget[] storageTargets)
            : this((IEnumerable<ReplayStorageTarget>)storageTargets)
        {
        }

        public ReplayHighlightReelStorage(IEnumerable<ReplayStorageTarget> storageTargets)
        {
            // Add storage targets
            sourceStorage.AddRange(storageTargets);

            // Build info
            float timeOffset = 0;

            foreach(ReplayStorageTarget target in sourceStorage)
            {
                duration += target.Duration;
                memorySize += target.MemorySize;


                // Intitial state buffer
                ReplayInitialDataBuffer buffer = target.InitialStateBuffer;

                // Process each stored id
                foreach(ReplayIdentity id in buffer.Identities)
                {
                    // Get the initial datas
                    IList<ReplayInitialData> initialData = buffer.GetInitialStates(id);

                    for(int i = 0; i < initialData.Count; i++)
                    {
                        // Adjust time stamps to sequential
                        ReplayInitialData data = initialData[i];

                        data.timestamp += timeOffset;

                        initialData[i] = data;
                    }

                    // Add to buffer
                    initialDataBuffer.AppendInitialStates(id, initialData);
                }

                timeOffset += target.Duration;
            }
        }

        // Methods
        public override ReplaySnapshot FetchSnapshot(float timeStamp)
        {
            float timeOffset = 0;

            foreach(ReplayStorageTarget target in sourceStorage)
            {
                float timeValue = timeStamp - timeOffset;

                if(timeValue > target.Duration)
                {
                    timeOffset += target.Duration;
                }
                else
                {
                    return target.FetchSnapshot(timeValue);
                }
            }
            return null;
        }

        public override ReplaySnapshot FetchSnapshot(int sequenceID)
        {
            foreach(ReplayStorageTarget target in sourceStorage)
            {
                // Get the last snapshot
                ReplaySnapshot lastSnapshot = target.FetchSnapshot(target.Duration);

                // Check for fopund storage
                if(sequenceID > lastSnapshot.SequenceID)
                {
                    sequenceID -= lastSnapshot.SequenceID;
                }
                else
                {
                    return target.FetchSnapshot(sequenceID);
                }
            }
            return null;
        }

        public override void PrepareTarget(ReplayTargetTask mode)
        {
            switch (mode)
            {
                case ReplayTargetTask.Commit:
                case ReplayTargetTask.PrepareWrite:
                    throw new NotSupportedException("Cannot write to a highlight reel storage device");

                case ReplayTargetTask.Discard:
                case ReplayTargetTask.PrepareRead:
                    {
                        foreach (ReplayStorageTarget target in sourceStorage)
                            target.PrepareTarget(mode);

                        break;
                    }
            }            
        }

        public override void StoreSnapshot(ReplaySnapshot state)
        {
            throw new NotSupportedException("Cannot write to a highlight reel storage device");
        }
    }
}
