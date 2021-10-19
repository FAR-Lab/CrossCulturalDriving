using System;
using System.Collections;
using System.Collections.Generic;
using UltimateReplay.Core;

namespace UltimateReplay.Storage
{
    public class ReplayStorageCombiner
    {
        // Private
        private static Dictionary<ReplayIdentity, IReplaySnapshotStorable> overridableStates = new Dictionary<ReplayIdentity, IReplaySnapshotStorable>();

        private int capacity = 0;

        // Protected
        protected List<ReplaySnapshot> snapshotStorage = null;

        // Properties
        public ReplaySnapshot FirstSnapshot
        {
            get
            {
                if (snapshotStorage.Count > 0)
                    return snapshotStorage[0];

                return null;
            }
        }

        public ReplaySnapshot LastSnapshot
        {
            get
            {
                if (snapshotStorage.Count > 0)
                    return snapshotStorage[snapshotStorage.Count - 1];

                return null;
            }
        }

        public int Size
        {
            get
            {
                int size = 0;

                foreach (ReplaySnapshot snapshot in snapshotStorage)
                    size += snapshot.Size;

                return size;
            }
        }

        public bool IsFull
        {
            get { return snapshotStorage.Count == capacity; }
        }

        public int SnapshotCount
        {
            get { return snapshotStorage.Count; }
        }

        public int SnapshotCapacity
        {
            get { return capacity; }
        }

        public IList<ReplaySnapshot> Snapshots
        {
            get { return snapshotStorage; }
        }

        // Constructor
        public ReplayStorageCombiner(int capacity)
        {
            this.snapshotStorage = new List<ReplaySnapshot>(capacity);
            this.capacity = capacity;
        }

        // Methods
        public void AddAndCombineSnapshot(ReplaySnapshot newCombineSnapshot, int maxPointerDepth)
        {
            if (newCombineSnapshot == null)
                throw new ArgumentNullException("newCombineSnapshot");

            // CHeck for available
            if (IsFull == true)
                return;

            // Combine the snapshot
            CombineSnapshot(newCombineSnapshot, maxPointerDepth);

            // Add the new snapshot
            snapshotStorage.Add(newCombineSnapshot);
        }

        public void CombineSnapshot(ReplaySnapshot combineSnapshot, int maxPointerDepth)
        {
            // Process all identities
            foreach(ReplayIdentity identity in combineSnapshot.Identities)
            {
                // Get the object state
                IReplaySnapshotStorable objectState = combineSnapshot.GetReplayObjectState(identity);
                IReplaySnapshotStorable storable = objectState;

                if(storable.StorageType == ReplaySnapshotStorableType.StateStorage)
                {
                    // Get as replay state
                    ReplayState stateData = storable as ReplayState;

                    // Combine the data
                    storable = GetCombinedSnapshotDataForObject(identity, stateData, maxPointerDepth);
                }

                // Check for changed data
                if(storable != null && objectState.Equals(storable) == false)
                {
                    // Queue the override the data
                    overridableStates.Add(identity, storable);
                }
            }

            // Delay override the necesary states. Must be delayed otherwise the collection is modified during iteration
            foreach(KeyValuePair<ReplayIdentity, IReplaySnapshotStorable> storable in overridableStates)
            {
                combineSnapshot.OverrideStateDataForReplayObject(storable.Key, storable.Value);
            }

            // Clear the override list
            overridableStates.Clear();
        }

        public virtual ReplaySnapshot FetchSnapshot(float timeStamp, ReplayManager.PlaybackDirection direction) 
        {
            // Check for empty storage
            if (snapshotStorage.Count == 0)
                return null;

            ReplaySnapshot current = null;

            if (direction == ReplayManager.PlaybackDirection.Forward)
            {
                // Default to first frame
                current = snapshotStorage[0];

                foreach (ReplaySnapshot snapshot in snapshotStorage)
                {
                    current = snapshot;

                    // Check if the time stamp is passe the offset
                    if (snapshot.TimeStamp >= timeStamp)
                        break;
                }
            }
            else
            {
                // Default to last frame
                current = snapshotStorage[snapshotStorage.Count - 1];

                for(int i = snapshotStorage.Count - 1; i >= 0; i--)
                {
                    current = snapshotStorage[i];

                    // Check if the time stamp is passed the offset
                    if (snapshotStorage[i].TimeStamp <= timeStamp)
                        break;
                }
            }

            // Resolve snapshot references
            if (current != null)
                ResolveSnapshot(current);

            return current;
        }

        public virtual ReplaySnapshot FetchSnapshot(int sequenceID)
        {
            ReplaySnapshot snapshot = snapshotStorage.Find(s => s.SequenceID == sequenceID);

            // Resolve snapshot references
            if(snapshot != null)
                ResolveSnapshot(snapshot);

            return snapshot;
        }

        public void ResolveSnapshot(ReplaySnapshot resolveSnapshot)
        {
            // Process all identities
            foreach(ReplayIdentity identity in resolveSnapshot.Identities)
            {
                // Check for pointer
                IReplaySnapshotStorable storable = resolveSnapshot.GetReplayObjectState(identity);

                // We need to resolve the pointer
                if(storable.StorageType == ReplaySnapshotStorableType.StatePointer)
                {
                    // Get the unresolved target snapshot
                    ReplaySnapshot targetSnapshot = snapshotStorage.Find(s => s.SequenceID == ((ReplayStatePointer)storable).SnapshotSequenceID);// FetchSnapshot(((ReplayStatePointer)storable).SnapshotSequenceID);

                    // Get the state data
                    storable = targetSnapshot.GetReplayObjectState(identity);

                    // Queue the override the data
                    overridableStates.Add(identity, storable);
                }
            }

            // Delay override the necesary states. Must be delayed otherwise the collection is modified during iteration
            foreach (KeyValuePair<ReplayIdentity, IReplaySnapshotStorable> storable in overridableStates)
            {
                resolveSnapshot.OverrideStateDataForReplayObject(storable.Key, storable.Value);
            }

            // Clear the override list
            overridableStates.Clear();
        }

        private IReplaySnapshotStorable GetCombinedSnapshotDataForObject(ReplayIdentity identity, ReplayState stateData, int maxPointerDepth)
        {
            // Check if any other identical data is already stored. We can then serialize a pointer to the state instead of all the data
            int overridePointer = GetLastStateWithMatchingDataForObject(identity, stateData.DataHash, maxPointerDepth);

            // Create a state pointer
            if (overridePointer != -1)
                return new ReplayStatePointer((ushort)overridePointer);

            return null;
        }

        private int GetLastStateWithMatchingDataForObject(ReplayIdentity identity, long replayStateHash, int maxPointerDepth)
        {
            int startIndex = snapshotStorage.Count - 1;

            for (int i = startIndex; i >= 0; i--)
            {
                ReplayState state = snapshotStorage[i].GetReplayObjectState(identity) as ReplayState;

                if (state != null && state.DataHash == replayStateHash)
                {
                    // Get the state pointer
                    return snapshotStorage[i].SequenceID;
                }

                // Check for too much depth
                if(startIndex - i > maxPointerDepth)
                {
                    // We have exceeded our max storage depth - return to avoid performance issues
                    return -1;
                }
            }

            return -1;
        }
    }
}
