
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UltimateReplay.Core;
using UltimateReplay.Serializers;
using UnityEngine;

#if ULTIMATEREPLAY_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace UltimateReplay.Storage
{
    internal struct ReplayCreatedObject
    {
        // Public
        public ReplayObject replayObject;
        public ReplayInitialData replayInitialData;
    }

    /// <summary>
    /// A frame state is a snapshot of a replay frame that is indexed based on its time stamp.
    /// By sequencing multiple frame states you can create the replay effect.
    /// </summary>
    [Serializable]
    public sealed class ReplaySnapshot : IReplayStreamSerialize
    {
        // Private
        private static ReplayObjectSerializer sharedSerializer = new ReplayObjectSerializer();
        private static Queue<ReplayObject> sharedDestroyQueue = new Queue<ReplayObject>();

        private float timeStamp = 0;
        private int sequenceID = -1;
        private int storageSize = 0;
        private Dictionary<ReplayIdentity, ReplayInitialData> newReplayObjectsThisFrame = new Dictionary<ReplayIdentity, ReplayInitialData>();
        private Dictionary<ReplayIdentity, IReplaySnapshotStorable> states = new Dictionary<ReplayIdentity, IReplaySnapshotStorable>();

        public static readonly int startSequenceID = 1;

        /// <summary>
        /// The time stamp for this snapshot.
        /// The time stamp is used to identify the snapshot location in the sequence.
        /// </summary>
        [ReplayTextSerialize("Time Stamp")]
        public float TimeStamp
        {
            get { return timeStamp; }
        }

        [ReplayTextSerialize("Sequence ID")]
        public int SequenceID
        {
            get { return sequenceID; }
        }

        /// <summary>
        /// Get the size in bytes of the snapshot data.
        /// </summary>
        [ReplayTextSerialize("Size")]
        public int Size
        {
            get
            {
                if(storageSize == -1)
                {
                    storageSize = 0;

                    // Calcualte the size of each object
                    foreach (IReplaySnapshotStorable storable in states.Values)
                    {
                        // Snapshot storable type
                        storageSize += sizeof(byte);

                        if (storable.StorageType == ReplaySnapshotStorableType.StatePointer)
                        {
                            // Snapshot pointer value
                            storageSize += sizeof(ushort);
                        }
                        else
                        {
                            ReplayState state = storable as ReplayState;
                            storageSize += state.Size;
                        }
                    }
                }

                return storageSize;
            }
        }

        public IEnumerable<ReplayIdentity> Identities
        {
            get { return states.Keys; }
        }

        // Constructor
        internal ReplaySnapshot() { }

        /// <summary>
        /// Create a new snapshot with the specified time stamp.
        /// </summary>
        /// <param name="timeStamp">The time stamp to give to this snapshot</param>
        public ReplaySnapshot(float timeStamp, int sequenceID)
        {
            this.timeStamp = timeStamp;
            this.sequenceID = sequenceID;
        }

        // Methods
        public override string ToString()
        {
            return string.Format("ReplaySnapshot(timestamp={0}, id={1}, size={2})", timeStamp, sequenceID, Size);
        }

        /// <summary>
        /// Called by the replay system when this <see cref="ReplaySnapshot"/> should be serialized to binary. 
        /// </summary>
        /// <param name="writer">The binary stream to write te data to</param>
        public void OnReplayStreamSerialize(BinaryWriter writer)
        {
            writer.Write(timeStamp);
            writer.Write(sequenceID);
            writer.Write(storageSize);

            writer.Write((ushort)states.Count);

            foreach(KeyValuePair<ReplayIdentity, IReplaySnapshotStorable> objectState in states)
            {
                // Write the identity
                ReplayStreamSerializationUtility.StreamSerialize(objectState.Key, writer);

                // Write the storable type
                writer.Write((byte)objectState.Value.StorageType);

                // Write the storable
                ReplayStreamSerializationUtility.StreamSerialize(objectState.Value, writer);
            }
        }

        /// <summary>
        /// Called by the replay system when this <see cref="ReplaySnapshot"/> should be deserialized from binary. 
        /// </summary>
        /// <param name="reader">The binary stream to read the data from</param>
        public void OnReplayStreamDeserialize(BinaryReader reader)
        {
            timeStamp = reader.ReadSingle();
            sequenceID = reader.ReadInt32();
            storageSize = reader.ReadInt32();

            ushort count = reader.ReadUInt16();

            for(int i = 0; i < count; i++)
            {
                //////////// IMPORANT - The replay identity is not being deserialized because it is being boxed to the interface type and passed by value
                ReplayIdentity identity = new ReplayIdentity();

                // Read the identity
                ReplayStreamSerializationUtility.StreamDeserialize(ref identity, reader);

                // Read the storable type
                ReplaySnapshotStorableType storableType = (ReplaySnapshotStorableType)reader.ReadByte();
                IReplaySnapshotStorable storable = null;

                if(storableType == ReplaySnapshotStorableType.StatePointer)
                {
                    // Create the pointer
                    storable = new ReplayStatePointer();
                }
                else
                {
                    // Create the state
                    storable = ReplayState.pool.GetReusable();
                }

                // Deserialize the data
                ReplayStreamSerializationUtility.StreamDeserialize(ref storable, reader);

                // Register with snapshot
                states.Add(identity, storable);
            }
        }

#if ULTIMATEREPLAY_JSON
        //public void OnReplayStreamSerialize(JsonWriter writer)
        //{
        //    writer.WriteStartObject();
        //    {
        //        writer.WritePropertyName("TimeStamp"); writer.WriteValue(timeStamp);
        //        writer.WritePropertyName("SequenceID"); writer.WriteValue(sequenceID);
        //        writer.WritePropertyName("StorageSize"); writer.WriteValue(storageSize);


        //        writer.WritePropertyName("States");
        //        writer.WriteStartArray();
        //        {
        //            // Write all states
        //            foreach (KeyValuePair<ReplayIdentity, IReplaySnapshotStorable> objectState in states)
        //            {
        //                writer.WriteStartObject();
        //                {
        //                    // Write the identity
        //                    ReplayStreamSerializationUtility.StreamSerialize(objectState.Key, writer);

        //                    writer.WritePropertyName("DataType"); writer.WriteValue(objectState.Value.StorageType);

        //                    // Write the storable
        //                    ReplayStreamSerializationUtility.StreamSerialize(objectState.Value, writer);
        //                }
        //                writer.WriteEndObject();
        //            }
        //        }
        //        writer.WriteEndArray();
        //    }
        //    writer.WriteEndObject();
        //}

        //public void OnReplayStreamDeserialize(JsonReader reader)
        //{
        //    JObject obj = JObject.Load(reader);

        //    timeStamp = obj.GetValue("TimeStamp").Value<float>();
        //    sequenceID = obj.GetValue("SequenceID").Value<int>();
        //    storageSize = obj.GetValue("StorageSize").Value<int>();

        //    // Get the array
        //    JArray array = obj.GetValue("States") as JArray;

        //    for(int i = 0; i < array.Count; i++)
        //    {
        //        // Get the nexted object
        //        JObject stateObj = array[i] as JObject;

        //        // Get identity
        //        ReplayIdentity identity = new ReplayIdentity(stateObj.GetValue("ReplayIdentity").Value<uint>());

        //        ReplaySnapshotStorableType storageType = stateObj.GetValue("DataType").Value<ReplaySnapshotStorableType>();

        //        IReplaySnapshotStorable storable = null;

        //        if(storageType == ReplaySnapshotStorableType.StatePointer)
        //        {
        //            //storable = state
        //        }
        //        else
        //        {

        //        }
        //    }
        //}
#endif

        public bool VerifySnapshot(bool throwOnError = false)
        {
            bool valid = true;

            // Process all storables
            foreach(IReplaySnapshotStorable storable in states.Values)
            {
                // Check for pointer
                if(storable.StorageType == ReplaySnapshotStorableType.StatePointer)
                {
                    // Get pointer object
                    ReplayStatePointer pointer = (ReplayStatePointer)storable;

                    // Validate pointer
                    if(pointer.SnapshotSequenceID < 0 && pointer.SnapshotSequenceID >= sequenceID)
                    {
                        valid = false;
                        break;
                    }
                }
            }

            if (valid == false && throwOnError == true)
                throw new Exception("The snapshot state is invalid and could cause corruption during playback");

            return valid;
        }

        /// <summary>
        /// Registers the specified replay state with this snapshot.
        /// The specified identity is used during playback to ensure that the replay objects receives the correct state to deserialize.
        /// </summary>
        /// <param name="identity">The identity of the object that was serialized</param>
        /// <param name="state">The state data for the object</param>
        public void RecordSnapshot(ReplayIdentity identity, ReplayState state)
        {
            // Register the state
            if (states.ContainsKey(identity) == false)
            {
                states.Add(identity, state);

                // Reset cached size
                storageSize = -1;
            }
        }

        /// <summary>
        /// Attempts to recall the state information for the specified replay object identity.
        /// If the identity does not exist in the scene then the return value will be null.
        /// </summary>
        /// <param name="identity">The identity of the object to deserialize</param>
        /// <returns>The state information for the specified identity or null if the identity does not exist</returns>
        public ReplayState RestoreSnapshot(ReplayIdentity identity)
        {
            // Try to get the state
            if (states.ContainsKey(identity) == true)
            {
                // Get the state
                ReplayState state = states[identity] as ReplayState;

                // Check for error
                if (state == null)
                    return null;

                // Reset the object state for reading
                state.PrepareForRead();

                return state;
            }

            // No state found
            return null;
        }

        /// <summary>
        /// Attempts to restore any replay objects that were spawned or despawned during this snapshot.
        /// </summary>
        public void RestoreReplayObjects(ReplayScene scene, ReplayInitialDataBuffer initialStateBuffer)
        {
            // Get all active scene objects
            IReadOnlyCollection<ReplayObject> activeReplayObjects = scene.ActiveReplayObjects;

            // Find all active replay objects
            //foreach (ReplayObject obj in activeReplayObjects)
            //for(int i = 0; i < activeReplayObjects.Count; i++)
            //{
            //    ReplayObject obj = activeReplayObjects[i];

            //    // Check if the object is no longer in the scene
            //    if (states.ContainsKey(obj.ReplayIdentity) == false)
            //    {
            //        // Check for a prefab
            //        if (UltimateReplay.Settings.prefabs.HasReplayPrefab(obj.PrefabIdentity) == false)
            //        {
            //            ReplayPlaybackAccuracyReporter.RecordPlaybackAccuracyError(obj.ReplayIdentity, ReplayPlaybackAccuracyReporter.PlaybackAccuracyError.DestroyNotARegisteredPrefab);
            //            continue;
            //        }

            //        // We need to destroy the replay object
            //        sharedDestroyQueue.Enqueue(obj);
            //    }
            //}
            foreach(ReplayObject obj in activeReplayObjects)
            {
                // Check if the object is no longer in the scene
                if (states.ContainsKey(obj.ReplayIdentity) == false)
                {
                    // Check for a prefab
                    if (UltimateReplay.Settings.prefabs.HasReplayPrefab(obj.PrefabIdentity) == false)
                    {
                        ReplayPlaybackAccuracyReporter.RecordPlaybackAccuracyError(obj.ReplayIdentity, ReplayPlaybackAccuracyReporter.PlaybackAccuracyError.DestroyNotARegisteredPrefab);
                        continue;
                    }

                    // We need to destroy the replay object
                    sharedDestroyQueue.Enqueue(obj);
                }
            }

            // Destroy all waiting objects
            while (sharedDestroyQueue.Count > 0)
            {
                // Get the target object
                ReplayObject destroyObject = sharedDestroyQueue.Dequeue();

                // Remove from the scene
                scene.RemoveReplayObject(destroyObject);

                // Destroy the game object
                UltimateReplay.ReplayDestroy(destroyObject.gameObject);                
            }


            /// CHANGE THIS - Replay object lookup should be performed based on available scene objects for best performance

            
            // Process all snapshot state data to check if we need to add any scene objects
            foreach(KeyValuePair<ReplayIdentity, IReplaySnapshotStorable> replayObject in states)
            {
                bool found = false;

                //found = activeReplayObjects.Exists(o => o.ReplayIdentity == replayObject.Key);

                found = scene.HasReplayObject(replayObject.Key);

                // Check if the desiered object is active in the scene
                //foreach(ReplayObject obj in activeReplayObjects)
                //{
                //    // Check for matching identity
                //    if(obj.ReplayIdentity == replayObject.Key)
                //    {
                //        // The object is in the scene - do nothing
                //        found = true;
                //        break;
                //    }
                //}

                // We need to spawn the object
                if(found == false)
                {
                    // Get the replay state for the object because it contains the prefab information we need
                    ReplayState state = replayObject.Value as ReplayState;

                    if (state == null)
                        continue;

                    // Reset the state for reading
                    state.PrepareForRead();

                    // Deserialize the object
                    sharedSerializer.OnReplayDeserialize(state);

                    // Get the prefab identity
                    ReplayIdentity prefabIdentity = sharedSerializer.PrefabIdentity;

                    // Reset the serializer
                    sharedSerializer.Reset();

                    // Read the name of the prefab that we need to spawn
                    //string name = state.ReadString();
                    string name = "";

                    // Try to find the matching prefab in our replay manager
                    ReplayObject prefab = UltimateReplay.Settings.prefabs.GetReplayPrefab(prefabIdentity);// ReplayManager.FindReplayPrefab(name);

                    // Check if the prefab was found
                    if(prefab == null)
                    {
                        // Check for no prefab identity
                        if (prefabIdentity == ReplayIdentity.invalid)
                        {
                            // Display scene object warning
                            ReplayPlaybackAccuracyReporter.RecordPlaybackAccuracyError(replayObject.Key, ReplayPlaybackAccuracyReporter.PlaybackAccuracyError.InstantiateMissingObjectAndNotPrefab);
                        }
                        else
                        {
                            // Display prefab object warning
                            ReplayPlaybackAccuracyReporter.RecordPlaybackAccuracyError(prefabIdentity, ReplayPlaybackAccuracyReporter.PlaybackAccuracyError.InstantiatePrefabNotFound);
                        }
                        continue;
                    }

                    // Restore initial data
                    ReplayInitialData initialData = new ReplayInitialData();

                    // Check for valid state buffer
                    if (initialStateBuffer != null && initialStateBuffer.HasInitialReplayObjectData(replayObject.Key) == true)
                    {
                        // Restore the objects state data
                        initialData = initialStateBuffer.RestoreInitialReplayObjectData(replayObject.Key, timeStamp);//  RestoreInitialReplayObjectData(replayObject.Key);
                    }

                    Vector3 position = Vector3.zero;
                    Quaternion rotation = Quaternion.identity;
                    Vector3 scale = Vector3.one;

                    // Update transform values
                    if ((initialData.InitialFlags & ReplayInitialData.ReplayInitialDataFlags.Position) != 0) position = initialData.position;
                    if ((initialData.InitialFlags & ReplayInitialData.ReplayInitialDataFlags.Rotation) != 0) rotation = initialData.rotation;
                    if ((initialData.InitialFlags & ReplayInitialData.ReplayInitialDataFlags.Scale) != 0) scale = initialData.scale;

                    // Call the instantiate method
                    GameObject result = UltimateReplay.ReplayInstantiate(prefab.gameObject, position, rotation);

                    if(result == null)
                    {
                        Debug.LogWarning(string.Format("Replay instanitate failed for prefab '{0}'. Some replay objects may be missing", name));
                        continue;
                    }

                    // Be sure to apply initial scale also
                    result.transform.localScale = scale;
                    
                    // try to find the replay object script
                    ReplayObject obj = result.GetComponent<ReplayObject>();

                    // Check if we have the component
                    if (obj != null)
                    {
                        // Give the replay object its serialized identity so that we can send replay data to it
                        obj.ReplayIdentity = replayObject.Key;

                        // Map observed component identities
                        if(initialData.observedComponentIdentities != null)
                        {
                            int index = 0;

                            foreach(ReplayBehaviour behaviour in obj.ObservedComponents)
                            {
                                if(initialData.observedComponentIdentities.Length > index)
                                {
                                    behaviour.ReplayIdentity = initialData.observedComponentIdentities[index];
                                }
                                index++;
                            }
                        }

                        // Register the created object
                        newReplayObjectsThisFrame.Add(obj.ReplayIdentity, initialData);

                        // Add to replay scene
                        scene.AddReplayObject(obj);

                        // Trigger spawned event
                        ReplayBehaviour.InvokeReplaySpawnedEvent(scene.ActiveReplayBehaviours, position, rotation);
                        //ReplayBehaviour.Events.CallReplaySpawnedEvents(obj, position, rotation);
                    }
                }
            }


            // Re-parent replay objects
            foreach(KeyValuePair<ReplayIdentity, ReplayInitialData> created in newReplayObjectsThisFrame)
            {
                // Try to get the target object
                ReplayObject createdObject = scene.GetReplayObject(created.Key);

                // Check if the object could not be found for some reason
                if (createdObject == null)
                    continue;

                // Check for a parent identity
                if(created.Value.parentIdentity != ReplayIdentity.invalid)
                {
                    bool foundTargetParent = false;

                    // We need to find the references parent
                    foreach(ReplayObject obj in scene.ActiveReplayObjects)
                    {
                        if(obj.ReplayIdentity == created.Value.parentIdentity)
                        {
                            // Parent the objects
                            createdObject.transform.SetParent(obj.transform, false);

                            // Set the flag
                            foundTargetParent = true;
                            break;
                        }
                    }

                    // Check if we failed to find the parent object
                    if (foundTargetParent == false)
                    {
                        // The target parent object is missing
                        Debug.LogWarning(string.Format("Newly created replay object '{0}' references identity '{1}' as a transform parent but the object could not be found in the current scene. Has the target parent been deleted this frame?", createdObject.name, created.Value.parentIdentity));
                    }
                }
            }

            // Clear ll tracked replay objects this frame
            newReplayObjectsThisFrame.Clear();


            // Report all playback errors
            ReplayPlaybackAccuracyReporter.ReportAllPlaybackAccuracyErrors();
        }

        internal void OverrideStateDataForReplayObject(ReplayIdentity identity, IReplaySnapshotStorable storable)
        {
            if (states.ContainsKey(identity) == true)
            {
                states[identity] = storable;

                // Reset cached size
                storageSize = -1;
            }
        }

        /// <summary>
        /// Clears all state information from the snapshot but keeps the time stamp.
        /// </summary>
        public void Reset()
        {
            states.Clear();

            // Reset cached size
            storageSize = -1;
        }

        public ReplayInitialData GetReplayObjectInitialState(ReplayIdentity identity)
        {
            ReplayInitialData result = new ReplayInitialData();

            // Try to find matching data
            newReplayObjectsThisFrame.TryGetValue(identity, out result);

            return result;
        }

        internal IReplaySnapshotStorable GetReplayObjectState(ReplayIdentity identity)
        {
            IReplaySnapshotStorable result = null;

            // Try to find matching data
            states.TryGetValue(identity, out result);

            return result;
        }        

        /// <summary>
        /// Attempts to modify the current snapshot time stamp by offsetting by the specified value.
        /// Negative values will reduce the timestamp.
        /// </summary>
        /// <param name="offset">The value to modify the timestamp with</param>
        internal void CorrectTimestamp(float offset)
        {
            // Modify the timestamp
            timeStamp += offset;
        }

        internal void CorrectSequenceID(int offset)
        {
            // Modify the sequence id
            sequenceID += offset;

            for(int i = 0; i < states.Count; i++)
            {
                if(states[states.ElementAt(i).Key] is ReplayStatePointer)
                {
                    ReplayStatePointer pointer = (ReplayStatePointer)states[states.ElementAt(i).Key];

                    pointer.snapshotSequenceID += (ushort)offset;

                    states[states.ElementAt(i).Key] = pointer;
                }
            }
        }
    }
}