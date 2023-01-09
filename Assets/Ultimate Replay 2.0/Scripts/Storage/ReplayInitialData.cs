using System;
using UltimateReplay.Core;
using UnityEngine;

namespace UltimateReplay.Storage
{
    /// <summary>
    /// Represents the intial settings of a newly spawned replay object.
    /// When a game object is instantiated it must be given an initial position and rotation.
    /// </summary>
    public struct ReplayInitialData : IReplaySerialize
    {
        // Types
        /// <summary>
        /// Represents initial data that may be stored by an object.
        /// </summary>
        [Flags]
        public enum ReplayInitialDataFlags : byte
        {
            /// <summary>
            /// No initial data is stored.
            /// </summary>
            None = 0,
            /// <summary>
            /// Initial position is recorded.
            /// </summary>
            Position = 1,
            /// <summary>
            /// Initial rotation is recorded.
            /// </summary>
            Rotation = 2,
            /// <summary>
            /// Initial scale is recorded.
            /// </summary>
            Scale = 4,
            /// <summary>
            /// Initial parent is recorded.
            /// </summary>
            Parent = 8,
        }

        // Public
        [ReplayTextSerialize("Serialize Flags")]
        public ReplayInitialDataFlags flags;

        /// <summary>
        /// Initial replay object identity.
        /// </summary>
        [ReplayTextSerialize("Object Identity")]
        public ReplayIdentity objectIdentity;
        /// <summary>
        /// The timestamp when this object was instantiated.
        /// </summary>
        [ReplayTextSerialize("Time Stamp")]
        public float timestamp;
        /// <summary>
        /// Initial position data.
        /// </summary>
        [ReplayTextSerialize("Position")]
        public Vector3 position;
        /// <summary>
        /// Initial rotation data.
        /// </summary>
        [ReplayTextSerialize("Rotation")]
        public Quaternion rotation;
        /// <summary>
        /// Initial scale data.
        /// </summary>
        [ReplayTextSerialize("Scale")]
        public Vector3 scale;
        /// <summary>
        /// Initial parent data.
        /// </summary>
        [ReplayTextSerialize("Parent Identity")]
        public ReplayIdentity parentIdentity;
        /// <summary>
        /// The replay ids for all observed components ordered by array index.
        /// </summary>
        [ReplayTextSerialize("Observed Component Identities")]
        public ReplayIdentity[] observedComponentIdentities;

        // Properties
        public ReplayInitialDataFlags InitialFlags
        {
            get { return flags; }
        }

        // Methods
        public void GenerateDataFlags()
        {
            // Reset flags
            flags = ReplayInitialDataFlags.None;

            // Create the initial object flag data
            if (position != Vector3.zero) flags |= ReplayInitialDataFlags.Position;
            if (rotation != Quaternion.identity) flags |= ReplayInitialDataFlags.Rotation;
            if (scale != Vector3.one) flags |= ReplayInitialDataFlags.Scale;
            if (parentIdentity != ReplayIdentity.invalid) flags |= ReplayInitialDataFlags.Parent;
        }

        public void OnReplaySerialize(ReplayState state)
        {
            // Write the object identity
            state.Write(objectIdentity);
            state.Write(timestamp);

            // Calcualte the flags
            flags = ReplayInitialDataFlags.None;

            // Mkae sure initial state flags are updated
            GenerateDataFlags();

            // Write the data flags
            state.Write((short)flags);

            // Write Position
            if ((flags & ReplayInitialDataFlags.Position) != 0)
                state.Write(position);

            // Write rotation
            if ((flags & ReplayInitialDataFlags.Rotation) != 0)
                state.Write(rotation);

            // Write scale
            if ((flags & ReplayInitialDataFlags.Scale) != 0)
                state.Write(scale);

            // Write parent
            if ((flags & ReplayInitialDataFlags.Parent) != 0)
                state.Write(parentIdentity);

            // Write the component identities
            int size = (observedComponentIdentities == null) ? 0 : observedComponentIdentities.Length;

            // Write the number of ids
            state.Write((short)size);

            // Write all ids
            for (int i = 0; i < size; i++)
            {
                // Write the identity
                state.Write(observedComponentIdentities[i]);
            }
        }

        public void OnReplayDeserialize(ReplayState state)
        {
            // Read the object identity
            state.ReadSerializable(ref objectIdentity);
            timestamp = state.ReadFloat();

            // Read the flags
            flags = (ReplayInitialDataFlags)state.ReadInt16();

            // Read position
            if ((flags & ReplayInitialDataFlags.Position) != 0)
                position = state.ReadVec3();

            // Read rotation
            if ((flags & ReplayInitialDataFlags.Rotation) != 0)
                rotation = state.ReadQuat();

            // Read scale
            if ((flags & ReplayInitialDataFlags.Scale) != 0)
                scale = state.ReadVec3();

            // Read parent identity
            if ((flags & ReplayInitialDataFlags.Parent) != 0)
                state.ReadSerializable(ref parentIdentity);

            // Read the number of observed components
            int size = state.ReadInt16();

            // Allocate the array
            observedComponentIdentities = new ReplayIdentity[size];

            // Read all ids
            for (int i = 0; i < size; i++)
            {
                // Read the identity
                state.ReadSerializable(ref observedComponentIdentities[i]);
            }
        }
    }
}
