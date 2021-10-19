using System;
using System.IO;
using System.Collections.Generic;
using UltimateReplay.Core;
using UnityEngine;

namespace UltimateReplay.Storage
{
    public sealed class ReplayInitialDataBuffer : IReplaySerialize, IReplayStreamSerialize
    {
        // Private
        private Dictionary<ReplayIdentity, List<ReplayInitialData>> initialStates = new Dictionary<ReplayIdentity, List<ReplayInitialData>>(); 
        
        // Properties
        public IEnumerable<ReplayIdentity> Identities
        {
            get { return initialStates.Keys; }
        }

        // Methods
        public void OnReplaySerialize(ReplayState state)
        {
            state.Write(initialStates.Count);

            foreach(KeyValuePair<ReplayIdentity, List<ReplayInitialData>> initialState in initialStates)
            {
                // Write the identity key and state data size
                state.Write(initialState.Key);
                state.Write(initialState.Value.Count);

                // Write all states
                for(int i = 0; i < initialState.Value.Count; i++)
                {
                    initialState.Value[i].OnReplaySerialize(state);
                }
            }
        }

        public void OnReplayDeserialize(ReplayState state)
        {
            int size = state.ReadInt32();

            for (int i = 0; i < size; i++)
            {
                // Read the target identity                
                ReplayIdentity identity = new ReplayIdentity();// state.ReadSerializable<ReplayIdentity>();

                state.ReadSerializable(ref identity);

                int localSize = state.ReadInt32();

                // Read all states
                for (int j = 0; j < localSize; j++)
                {
                    // Create empty data container
                    ReplayInitialData data = new ReplayInitialData();

                    // Deserialize data
                    data.OnReplayDeserialize(state);

                    // Register state
                    if (initialStates.ContainsKey(identity) == false)
                        initialStates.Add(identity, new List<ReplayInitialData>());

                    initialStates[identity].Add(data);
                }
            }
        }

        void IReplayStreamSerialize.OnReplayStreamSerialize(BinaryWriter writer)
        {
            // Get a replay state
            ReplayState state = ReplayState.pool.GetReusable();

            // Use default serialize method
            OnReplaySerialize(state);

            // Write to stream
            ReplayStreamSerializationUtility.StreamSerialize(state, writer);
        }

        void IReplayStreamSerialize.OnReplayStreamDeserialize(BinaryReader reader)
        {
            ReplayState state = ReplayState.pool.GetReusable();

            // Read from stream
            ReplayStreamSerializationUtility.StreamDeserialize(ref state, reader);
            
            // Deserialize state
            OnReplayDeserialize(state);
        }

        public IList<ReplayInitialData> GetInitialStates(ReplayIdentity id)
        {
            List<ReplayInitialData> data;

            initialStates.TryGetValue(id, out data);

            return data;
        }

        public void AppendInitialStates(ReplayIdentity id, IEnumerable<ReplayInitialData> states)
        {
            if (initialStates.ContainsKey(id) == false)
                initialStates.Add(id, new List<ReplayInitialData>());

            initialStates[id].AddRange(states);
        }

        public bool HasInitialReplayObjectData(ReplayIdentity identity)
        {
            // Check for any initial states
            return initialStates.ContainsKey(identity);
        }

        public void RecordInitialReplayObjectData(ReplayObject obj, float timestamp, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            // Create the initial data
            ReplayInitialData data = new ReplayInitialData();

            // Store the object identity
            data.objectIdentity = obj.ReplayIdentity;
            data.timestamp = timestamp;

            // Store the transform information
            data.position = position;
            data.rotation = rotation;
            data.scale = scale;

            // Store the parent identities
            if(obj.transform.parent != null)
            {
                // Get the parent replay object
                ReplayObject replayParent = obj.transform.parent.GetComponent<ReplayObject>();

                // Store parent identity
                if (replayParent != null)
                    data.parentIdentity = replayParent.ReplayIdentity;
            }


            // Store observed component identity array
            int size = obj.ObservedComponents.Count;
            int index = 0;

            // Allocate array
            data.observedComponentIdentities = new ReplayIdentity[size];

            foreach(ReplayBehaviour behaviour in obj.ObservedComponents)
            {
                // Store component identity in array
                data.observedComponentIdentities[index] = behaviour.ReplayIdentity;
                index++;
            }


            // Update stored data flags
            data.GenerateDataFlags();


            // Register the initial data with the corrosponding identity
            if (initialStates.ContainsKey(obj.ReplayIdentity) == false)
                initialStates.Add(obj.ReplayIdentity, new List<ReplayInitialData>());

            // Store the state data
            initialStates[obj.ReplayIdentity].Add(data);
        }

        public ReplayInitialData RestoreInitialReplayObjectData(ReplayIdentity identity, float timestamp)
        {
            // Create an error return value
            ReplayInitialData data = new ReplayInitialData
            {
                objectIdentity = identity,
                timestamp = timestamp,
                position = Vector3.zero,
                rotation = Quaternion.identity,
                scale = Vector3.one,
                parentIdentity = ReplayIdentity.invalid,
                observedComponentIdentities = null,
            };

            // Check for existing object identity
            if(initialStates.ContainsKey(identity) == true)
            {
                // Get the state data
                List<ReplayInitialData> states = initialStates[identity];

                // Check for any states
                if (states.Count > 0)
                {
                    // Check for trivial case
                    if (states.Count == 1)
                        return states[0];

                    int index = -1;
                    float smallestDifference = float.MaxValue;

                    // Find the best matching time stamp
                    for(int i = 0; i < states.Count; i++)
                    {
                        // Get timestamp difference
                        float timeDifference = Mathf.Abs(timestamp - states[i].timestamp);

                        // Check for smaller difference
                        if(timeDifference < smallestDifference)
                        {
                            // We have a new smallest time difference so make that the new target to beat
                            index = i;
                            smallestDifference = timeDifference;
                        }
                    }

                    // Check for valid index
                    if(index != -1)
                    {
                        // Use the state data at the best index match
                        data = states[index];
                    }
                }
            }
            return data;
        }
    }
}
