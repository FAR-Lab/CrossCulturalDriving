using System;
using System.Collections.Generic;
using UltimateReplay.Core;

namespace UltimateReplay.Serializers
{
    /// <summary>
    /// Used to handle the serialization of a <see cref="ReplayObject"/> component. 
    /// </summary>
    public sealed class ReplayObjectSerializer : IReplaySerialize
    {
        // Types
        /// <summary>
        /// Serialize flags used to specify which data elements will be stored.
        /// </summary>
        [Flags]
        public enum ReplayObjectSerializeFlags : byte
        {
            /// <summary>
            /// Component data will be serialized.
            /// </summary>
            Components = 1 << 3,
            /// <summary>
            /// Recorded variables will be serialzied.
            /// </summary>
            Variables = 1 << 4,
            /// <summary>
            /// Recorded events will be serialized.
            /// </summary>
            Events = 1 << 5,
            /// <summary>
            /// Recorded methods will be serialized.
            /// </summary>
            Methods = 1 << 6,
        }

        // Private
        [ReplayTextSerialize("Serialize Flags")]
        private ReplayObjectSerializeFlags serializeFlags = 0;
        [ReplayTextSerialize("Prefab Identity")]
        private ReplayIdentity prefabIdentity = new ReplayIdentity();

        private List<ReplayComponentData> componentStates = new List<ReplayComponentData>();
        private List<ReplayVariableData> variableStates = new List<ReplayVariableData>();
        private List<ReplayEventData> eventStates = new List<ReplayEventData>();
        private List<ReplayMethodData> methodStates = new List<ReplayMethodData>();

        // Properties
        /// <summary>
        /// The <see cref="ReplayObjectSerializeFlags"/> used to specify which data elements will be stored.
        /// This value should be set prior to serializing or will be automatically filled when calling deserializing.
        /// </summary>
        public ReplayObjectSerializeFlags SerializeFlags
        {
            get { return serializeFlags; }
            set { serializeFlags = value; }
        }

        /// <summary>
        /// The <see cref="ReplayIdentity"/> of the parent prefab if applicable.
        /// </summary>
        public ReplayIdentity PrefabIdentity
        {
            get { return prefabIdentity; }
            set { prefabIdentity = value; }
        }

        /// <summary>
        /// A collection of <see cref="ReplayComponentData"/> containing all the necessary persistent data for all observed components.
        /// </summary>
        public IList<ReplayComponentData> ComponentStates
        {
            get { return componentStates; }
        }

        /// <summary>
        /// A collection of <see cref="ReplayVariableData"/> containing all the necessary persistent data for all recorded variables.
        /// </summary>
        public IList<ReplayVariableData> VariableStates
        {
            get { return variableStates; }
        }

        /// <summary>
        /// A collection of <see cref="ReplayEventData"/> containing all the necessary persistent data for all recorded events.
        /// </summary>
        public IList<ReplayEventData> EventStates
        {
            get { return eventStates; }
        }

        /// <summary>
        /// A collection of <see cref="ReplayMethodData"/> containing all the necessary persistent data for all recorded methods.
        /// </summary>
        public IList<ReplayMethodData> MethodStates
        {
            get { return methodStates; }
        }

        // Methods
        /// <summary>
        /// Causes the serializer to be reset to its initial state.
        /// </summary>
        public void Reset()
        {
            // Dispose of component states
            for(int i = 0; i < componentStates.Count; i++)
            {
                componentStates[i].Dispose();
            }

            // Dispose of variable states
            for(int i = 0; i < variableStates.Count; i++)
            {
                variableStates[i].VariableStateData.Dispose();
            }

            // Dispose of event states
            for(int i = 0; i < eventStates.Count; i++)
            {
                ReplayEventData evtData = eventStates[i];

                if (evtData.EventState != null)
                    evtData.EventState.Dispose();
            }

            // Clear all
            componentStates.Clear();
            variableStates.Clear();
            eventStates.Clear();
            methodStates.Clear();
        }

        /// <summary>
        /// Invoke this method to serialize all the <see cref="ReplayObject"/> data to the specified <see cref="ReplayState"/>.
        /// </summary>
        /// <param name="state">The state object to write to</param>
        public void OnReplaySerialize(ReplayState state)
        {
            state.Write((byte)serializeFlags);

            // Write prefab identity
            state.Write(prefabIdentity);

            // Check for no flags
            if (serializeFlags == 0)
                return;            

            // Write components
            if((serializeFlags & ReplayObjectSerializeFlags.Components) != 0)
            {
                // Component count
                state.Write((ushort)componentStates.Count);

                // Write all components
                foreach(ReplayComponentData componentItem in componentStates)
                {
                    // Write the component data
                    state.Write(componentItem);
                }
            }

            // Write variables
            if((serializeFlags & ReplayObjectSerializeFlags.Variables) != 0)
            {
                // Variable count
                state.Write((ushort)variableStates.Count);

                // Write all variables
                foreach(ReplayVariableData variableItem in variableStates)
                {
                    // Write variable data
                    variableItem.OnReplaySerialize(state);
                }
            }

            // Write events
            if((serializeFlags & ReplayObjectSerializeFlags.Events) != 0)
            {
                // Event count
                state.Write((ushort)eventStates.Count);

                // Write all events
                foreach(ReplayEventData eventItem in eventStates)
                {
                    // Write event data
                    eventItem.OnReplaySerialize(state);
                }
            }

            // Write methods
            if((serializeFlags & ReplayObjectSerializeFlags.Methods) != 0)
            {
                // Method count
                state.Write((ushort)methodStates.Count);

                // Write all methods
                foreach(ReplayMethodData methodState in methodStates)
                {
                    // Write method data
                    state.Write(methodState);
                }
            }
        }

        /// <summary>
        /// Invoke this method to deserialize the <see cref="ReplayObject"/> data from the specified <see cref="ReplayState"/>.
        /// </summary>
        /// <param name="state">The state object to read from</param>
        public void OnReplayDeserialize(ReplayState state)
        {
            // Clear old states as we will be rebuilding them
            Reset();


            // Read storage flags
            serializeFlags = (ReplayObjectSerializeFlags)state.ReadByte();

            // Read prefab identity
            //prefabIdentity = state.ReadSerializable<ReplayIdentity>();

            state.ReadSerializable(ref prefabIdentity);

            // Check for 0 flags
            if (serializeFlags == 0)
                return;

            // Read components
            if ((serializeFlags & ReplayObjectSerializeFlags.Components) != 0)
            {
                // Component count
                ushort count = state.ReadUInt16();

                for(int i = 0; i < count; i++)
                {
                    // Read the component data
                    ReplayComponentData componentData = new ReplayComponentData();// state.ReadSerializable<ReplayComponentData>();

                    state.ReadSerializable(ref componentData);

                    // Register state
                    componentStates.Add(componentData);
                }
            }

            // Read variables
            if ((serializeFlags & ReplayObjectSerializeFlags.Variables) != 0)
            {
                // Variable count
                ushort count = state.ReadUInt16();

                for(int i = 0; i < count; i++)
                {
                    // Read the variable data
                    ReplayVariableData variableData = new ReplayVariableData();// state.ReadSerializable<ReplayVariableData>();

                    state.ReadSerializable(ref variableData);

                    // Register state
                    variableStates.Add(variableData);
                }
            }

            // Read events
            if ((serializeFlags & ReplayObjectSerializeFlags.Events) != 0)
            {
                // Event count
                ushort count = state.ReadUInt16();

                for(int i = 0; i < count; i++)
                {
                    // Read the event data
                    ReplayEventData eventData = new ReplayEventData();// state.ReadSerializable<ReplayEventData>();

                    state.ReadSerializable(ref eventData);

                    // Register event state
                    eventStates.Add(eventData);
                }
            }

            // Read methods
            if ((serializeFlags & ReplayObjectSerializeFlags.Methods) != 0)
            {
                // Method count
                ushort count = state.ReadUInt16();

                for(int i = 0; i < count; i++)
                {
                    // Read the method data
                    ReplayMethodData methodData = new ReplayMethodData();// state.ReadSerializable<ReplayMethodData>();

                    state.ReadSerializable(ref methodData);

                    // Register method state
                    methodStates.Add(methodData);
                }
            }
        }        
    }
}
