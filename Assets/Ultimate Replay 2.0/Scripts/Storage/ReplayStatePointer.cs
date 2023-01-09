
using System.IO;

namespace UltimateReplay.Storage
{
    internal struct ReplayStatePointer : IReplaySnapshotStorable
    {
        // Internal
        internal ushort snapshotSequenceID;

        // Properties
        public ushort SnapshotSequenceID
        {
            get { return snapshotSequenceID; }
        }

        public ReplaySnapshotStorableType StorageType
        {
            get { return ReplaySnapshotStorableType.StatePointer; }
        }

        // Constructor
        public ReplayStatePointer(ushort snapshotSequenceID)
        {
            this.snapshotSequenceID = snapshotSequenceID;
        }

        // Methods
        public override string ToString()
        {
            return string.Format("ReplayStatePointer({0})", snapshotSequenceID);
        }

        void IReplayStreamSerialize.OnReplayStreamSerialize(BinaryWriter writer)
        {
            writer.Write(snapshotSequenceID);
        }

        void IReplayStreamSerialize.OnReplayStreamDeserialize(BinaryReader reader)
        {
            snapshotSequenceID = reader.ReadUInt16();
        }
    }
}
