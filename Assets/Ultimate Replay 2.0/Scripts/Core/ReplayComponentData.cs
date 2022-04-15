using System;
using UltimateReplay.Serializers;
using UnityEngine;

namespace UltimateReplay.Core
{
    /// <summary>
    /// Contains all serialized data relating to a specific recorder component.
    /// </summary>
    public struct ReplayComponentData : IReplaySerialize, IDisposable
    {
        // Private
        [ReplayTextSerialize("Behaviour Identity")]
        private ReplayIdentity behaviourIdentity;

        [ReplayTextSerialize("Component Serialize Type String")]
        private string componentSerializerTypeString;

        [ReplayTextSerialize("Component Serializer ID")]
        private int componentSerializerID;

        private ReplayState componentStateData;

        // Properties
        /// <summary>
        /// The <see cref="ReplayIdentity"/> of the behaviour script that the data belongs to.
        /// </summary>
        public ReplayIdentity BehaviourIdentity
        {
            get { return behaviourIdentity; }
        }

        /// <summary>
        /// An id value used to identify the corrosponding serializer or '-1' if a serializer id could not be generated.
        /// </summary>
        public int ComponentSerializerID
        {
            get { return componentSerializerID; }
        }

        /// <summary>
        /// The <see cref="ReplayState"/> containing all data that was serialized by the component.
        /// </summary>
        public ReplayState ComponentStateData
        {
            get { return componentStateData; }
        }

        // Constructor
        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="behaviourIdentity">The identity of the behaviour component</param>
        /// <param name="componentSerializerType">The type of the serializer</param>
        /// <param name="componentStateData">The data assocaited with the component</param>
        public ReplayComponentData(ReplayIdentity behaviourIdentity, Type componentSerializerType,
            ReplayState componentStateData)
        {
            this.behaviourIdentity = behaviourIdentity;
            this.componentSerializerTypeString = componentSerializerType.AssemblyQualifiedName;
            this.componentSerializerID = -1;
            this.componentStateData = componentStateData;
        }

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="behaviourIdentity">The identity of the behaviour component</param>
        /// <param name="componentSerializerID">The id of the component serializer</param>
        /// <param name="componentStateData">The data associated with the component</param>
        public ReplayComponentData(ReplayIdentity behaviourIdentity, int componentSerializerID,
            ReplayState componentStateData)
        {
            this.behaviourIdentity = behaviourIdentity;
            this.componentSerializerTypeString = null;
            this.componentSerializerID = componentSerializerID;
            this.componentStateData = componentStateData;
        }

        // Methods
        /// <summary>
        /// Release the component data.
        /// </summary>
        public void Dispose()
        {
            behaviourIdentity = ReplayIdentity.invalid;
            componentSerializerTypeString = null;
            componentSerializerID = -1;
            componentStateData.Dispose();
            componentStateData = null;
        }

        /// <summary>
        /// Serialize the component data to the specified <see cref="ReplayState"/>.
        /// </summary>
        /// <param name="state">The object state to write to</param>
        public void OnReplaySerialize(ReplayState state)
        {
            state.Write(behaviourIdentity);
            state.Write(componentSerializerTypeString == null);

            if (componentSerializerTypeString == null)
            {
                state.Write((byte) componentSerializerID);
            }
            else
            {
                state.Write(componentSerializerTypeString);
            }

            state.Write(componentStateData);
        }

        /// <summary>
        /// Deserialize the component data from the specified <see cref="ReplayState"/>.
        /// </summary>
        /// <param name="state">The object state to read from</param>
        public void OnReplayDeserialize(ReplayState state)
        {
            //behaviourIdentity = state.ReadSerializable<ReplayIdentity>();

            state.ReadSerializable(ref behaviourIdentity);

            bool useID = state.ReadBool();

            if (useID == true)
            {
                componentSerializerTypeString = null;
                componentSerializerID = state.ReadByte();
            }
            else
            {
                componentSerializerID = -1;
                componentSerializerTypeString = state.ReadString();
            }

            // Get reusable state
            //componentStateData = ReplayState.pool.GetReusable();

            //state.ReadSerializable(componentStateData);
            componentStateData = state.ReadState();
        }

        /// <summary>
        /// Try to resolve the type of the corrosponding serializer type.
        /// </summary>
        /// <returns>The type of the matching serialize or null if the type could not be resolved</returns>
        public Type ResolveSerializerType()
        {
            // Check for valid serializer id
            if (componentSerializerID != -1)
            {
                // Get the type from the serializer id
                return ReplaySerializers.GetSerializerTypeFromID(componentSerializerID);
            }

            // Check for null
            if (componentSerializerTypeString == null)
                return null;

            // Get the type from the type string
            return Type.GetType(componentSerializerTypeString);
        }

        /// <summary>
        /// Deserialize the component data onto the specified component serializer instance.
        /// The specified serialize must be the correct type or have the correct serializer id.
        /// </summary>
        /// <param name="componentSerializer">An <see cref="IReplaySerialize"/> implementation that should be a correct typed serializer</param>
        /// <returns>True if the deserialize was successful or false if not</returns>
        public bool DeserializeComponent(IReplaySerialize componentSerializer)
        {
            // Check for null
            if (componentSerializer == null) throw new ArgumentNullException("componentSerializer");

            // Get the serializer type
            Type deserializerType = ResolveSerializerType();

            // Check for found
            if (deserializerType == null)
            {
                Debug.Log("Type was null");

                return false;
            }

            // Check for matching types
            if (deserializerType != componentSerializer.GetType())
            {
                Debug.Log(deserializerType.ToString() + "Type missmatch"+ componentSerializer.GetType().ToString());
                return false;
            }

            // Prepare state and deserialize
            componentStateData.PrepareForRead();
            componentSerializer.OnReplayDeserialize(componentStateData);

            // Success
            return true;
        }
    }
}