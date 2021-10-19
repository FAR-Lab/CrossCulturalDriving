#define ReplayIdentityBit_16

using System;
using System.Collections.Generic;
using System.IO;
using UltimateReplay.Storage;
using UnityEngine;

namespace UltimateReplay.Core
{
    /// <summary>
    /// A replay identity is an essential component in the Ultimate Replay system and is used to identify replay objects between sessions.
    /// Replay identities are assigned at edit time where possible and will never change values.
    /// Replay identities are also use to identify prefab instances that are spawned during a replay.
    /// </summary>
    [Serializable]
    public struct ReplayIdentity : IEquatable<ReplayIdentity>, IReplaySerialize, IReplayStreamSerialize, ISerializationCallbackReceiver
    {
        // Internal
        internal const int maxGenerateAttempts = 512;
        internal const int unassignedID = 0;

        // Private
        private static System.Random rand = new System.Random();
        private static List<ReplayIdentity> usedIds = new List<ReplayIdentity>();

        // Store the value in memory as 32-bit but serialize as specified byteSize
        [SerializeField]
        private uint id;

        // Public
        public static readonly ReplayIdentity invalid = new ReplayIdentity(unassignedID);

        /// <summary>
        /// Get the number of bytes that this object uses to represent its id data.
        /// </summary>
#if ReplayIdentityBit_16
        public static readonly ushort byteSize = sizeof(ushort);
#else
        public static readonly uint byteSize = sizeof(uint);
#endif

        // Properties
#if UNITY_EDITOR
        /// <summary>
        /// Enumerates all used <see cref="ReplayIdentity"/> objects in the domain. 
        /// used for debugging only.
        /// </summary>
        public static IEnumerable<ReplayIdentity> Identities
        {
            get { return usedIds; }
        }
#endif

        /// <summary>
        /// Returns true if this id is not equal to <see cref="unassignedID"/>. 
        /// </summary>
        public bool IsValid
        {
            get { return id != unassignedID; }
        }

        public string IDString
        {
            get { return id.ToString(); }
        }

        public int IDValue
        {
            get { return (int)id; }
        }

        // Constructor
        /// <summary>
        /// Clear any old data on domain reload.
        /// </summary>
        static ReplayIdentity()
        {
            // Clear the set - it will be repopulated when each identity is initialized
            usedIds.Clear();
        }

        /// <summary>
        /// Create a new instance with the specified id value.
        /// </summary>
        /// <param name="id">The id value to give this identity</param>
        public ReplayIdentity(uint id)
        {
            this.id  = id;
        }

        public ReplayIdentity(ReplayIdentity other)
        {
            this.id = other.id;
        }

        // Methods
        #region InheritedAndOperator
        /// <summary>
        /// Override implementation.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        /// <summary>
        /// Override implementation.
        /// </summary>
        /// <param name="obj">The object to compare against</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            ////Check for null
            //if (obj == null)
            //        return false;

            if (obj is ReplayIdentity)
            {
                return id == ((ReplayIdentity)obj).id;
            }
            return false;

            //try
            //{
            //    // Check for type
            //    ReplayIdentity id = (ReplayIdentity)obj;
                
            //    // Call through
            //    return Equals(id);
            //}
            //catch(InvalidCastException)
            //{
            //    return false;
            //}
        }

        /// <summary>
        /// IEquateable implementation.
        /// </summary>
        /// <param name="obj">The <see cref="ReplayIdentity"/> to compare against</param>
        /// <returns></returns>
        public bool Equals(ReplayIdentity obj)
        {
            // Compare values
            return id == obj.id;
        }

        /// <summary>
        /// Override implementation.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("ReplayIdentity({0})", id);
        }

        /// <summary>
        /// Override equals operator.
        /// </summary>
        /// <param name="a">First <see cref="ReplayIdentity"/></param>
        /// <param name="b">Second <see cref="ReplayIdentity"/></param>
        /// <returns></returns>
        public static bool operator ==(ReplayIdentity a, ReplayIdentity b)
        {
            return a.Equals( b) == true;
        }

        /// <summary>
        /// Override not-equals operator.
        /// </summary>
        /// <param name="a">First <see cref="ReplayIdentity"/></param>
        /// <param name="b">Second <see cref="ReplayIdentity"/></param>
        /// <returns></returns>
        public static bool operator !=(ReplayIdentity a, ReplayIdentity b)
        {
            // Check for not equal
            return a.Equals(b) == false;
        }
        #endregion

        void IReplaySerialize.OnReplaySerialize(ReplayState state)
        {
            state.Write((byte)byteSize);

            if (byteSize == sizeof(ushort))
            {
                state.Write((ushort)id);
            }
            else if(byteSize == sizeof(uint))
            {
                state.Write(id);
            }
        }

        void IReplaySerialize.OnReplayDeserialize(ReplayState state)
        {
            byte byteSize = state.ReadByte();

            if (byteSize == sizeof(ushort))
            {
                id = state.ReadUInt16();
            }
            else if (byteSize == sizeof(uint))
            {
                id = state.ReadUInt32();
            }
        }

        void IReplayStreamSerialize.OnReplayStreamSerialize(BinaryWriter writer)
        {
            writer.Write((byte)byteSize);

            if (byteSize == sizeof(ushort))
            {
                writer.Write((ushort)id);
            }
            else
            {
                writer.Write((uint)id);
            }
        }

        void IReplayStreamSerialize.OnReplayStreamDeserialize(BinaryReader reader)
        {
            byte byteSize = reader.ReadByte();

            if(byteSize == sizeof(ushort))
            {
                id = reader.ReadUInt16();
            }
            else if(byteSize == sizeof(uint))
            {
                id = (ushort)reader.ReadUInt32();
            }
        }

        public void OnBeforeSerialize()
        {
            // Generate unique id
            if (IsValid == false)
                Generate(ref this);
        }

        public void OnAfterDeserialize()
        {
            //if (IsUnique(this, true) == false)
            //    Generate(ref this);

            // Register id
            RegisterIdentity(this);
        }

        public static void RegisterIdentity(ReplayIdentity identity)
        {
            // Register the id
            if(usedIds.Contains(identity) == false)
                usedIds.Add(identity);
        }

        public static void UnregisterIdentity(ReplayIdentity identity)
        {
            // Remove the id
            if (usedIds.Contains(identity) == true)
                usedIds.Remove(identity);
        }


        internal static void Generate(ref ReplayIdentity identity)
        {
            // Unregister current id
            UnregisterIdentity(identity);

#if ReplayIdentityBit_16
            ushort next = unassignedID;
            ushort count = 0;

            // Use 2 byte array to create 16 bit int
            byte[] buffer = new byte[2];

#else
            uint next = unassignedID;
            uint count = 0;
#endif
                        
            do
            {
                // Check for long loop
                if (count > maxGenerateAttempts)
                    throw new OperationCanceledException("Attempting to find a unique replay id took too long. The operation was canceled to prevent a long or infinite loop");
                
#if ReplayIdentityBit_16
                // Randomize the buffer
                rand.NextBytes(buffer);

                // Use random instead of linear
                next = (ushort)(buffer[0] << 8 | buffer[1]);
#else
                // Get random int
                next = (uint)random.Next();
#endif

                // Keep track of how many times we have tried
                count++;
            }
            // Make sure our set does not contain the id
            while (next == unassignedID || IsValueUnique(next, false) == false);

            // Update identity with unique value
            identity.id = next;     
        }

        internal static bool IsUnique(ReplayIdentity identity, bool ignoreOneMatch)
        {
            return IsValueUnique(identity.id, ignoreOneMatch);
        }

        private static bool IsValueUnique(uint value, bool ignoreOneMatch)
        {
            int matchCount = 0;

            foreach (ReplayIdentity used in usedIds)
            {
                if (used.id == value)
                {
                    if (ignoreOneMatch == true)
                    {
                        matchCount++;

                        if (matchCount > 1)
                            return false;
                    }
                    else
                        return false;
                }
            }

            return true;
        }

        //private static bool IsUnique(ReplayIdentity identity)
        //{
        //    if (identity.IsValid == false)
        //        return false;

        //    foreach(ReplayIdentity used in usedIds)
        //    {
        //        if(ReferenceEquals(used, identity) == false)
        //            if (used.id == identity.id)
        //                return false;
        //    }

        //    return true;
        //}
    }
}
