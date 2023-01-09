using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UltimateReplay.Core;
using UltimateReplay.Storage;
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// A <see cref="ReplayState"/> allows replay objects to serialize and deserialize their data.
    /// See <see cref="IReplaySerialize"/>. 
    /// </summary>
    public sealed partial class ReplayState : IDisposable, IReplayReusable, IReplaySerialize, IReplaySnapshotStorable
    {
        // Private
        private const int maxByteAllocation = 4; // Dont allow 64 bit types

        private static readonly Dictionary<Type, MethodInfo> serializeMethods = new Dictionary<Type, MethodInfo>();
        private static readonly Dictionary<Type, MethodInfo> deserializeMethods = new Dictionary<Type, MethodInfo>();
        private static readonly byte[] sharedBuffer = new byte[maxByteAllocation];
        private static readonly byte[] sharedDataBuffer = new byte[4096];

        private List<byte> bytes = new List<byte>();
        private long dataHash = -1;
        private int dataSize = 0;
        private int readPointer = 0;
        private bool suppressDisposeCheck = false;

        // Public
        public static readonly ReplayInstancePool<ReplayState> pool = new ReplayInstancePool<ReplayState>(() => new ReplayState());

        // Properties
        /// <summary>
        /// Returns true if the state contains any more data.
        /// </summary>
        public bool CanRead
        {
            get
            {
                // Check for disposed
                CheckDisposed();

                return bytes.Count > 0;
            }
        }

        /// <summary>
        /// Returns true if the read pointer is at the end of the buffered data or false if there is still data to be read.
        /// </summary>
        public bool EndRead
        {
            get
            {
                // Check for disposed
                CheckDisposed();

                return readPointer >= Size;
            }
        }

        /// <summary>
        /// Returns the size of the object state in bytes.
        /// </summary>
        public int Size
        {
            get
            {
                // Check for disposed
                CheckDisposed();

                return bytes.Count;
            }
        }

        ReplaySnapshotStorableType IReplaySnapshotStorable.StorageType
        {
            get
            {
                // Check for disposed
                CheckDisposed();

                return ReplaySnapshotStorableType.StateStorage;
            }
        }

        public long DataHash
        {
            get
            {
                // Check for disposed
                CheckDisposed();

                if (dataHash == -1)
                    dataHash = GetDataHash();

                return dataHash;
            }
        }

        // Constructor
        static ReplayState()
        {
            foreach(MethodInfo declaredMethod in typeof(ReplayState).GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if(declaredMethod.Name == "Write")
                {
                    if (declaredMethod.GetParameters().Length == 1)
                    {
                        // Register the parameter type
                        serializeMethods.Add(declaredMethod.GetParameters()[0].ParameterType, declaredMethod);
                    }
                }
                else if(declaredMethod.Name.StartsWith("Read") == true)
                {
                    if(declaredMethod.ReturnType != typeof(void) && declaredMethod.GetParameters().Length == 0 && deserializeMethods.ContainsKey(declaredMethod.ReturnType) == false)
                    {
                        deserializeMethods.Add(declaredMethod.ReturnType, declaredMethod);
                    }
                }
            }
        }

        /// <summary>
        /// Create an empty <see cref="ReplayState"/> that can be written to. 
        /// </summary>
        private ReplayState() { }

        // Methods
        void IReplayReusable.Initialize()
        {
            // Mark as not disposed
            readPointer = 0;
            dataHash = -1;
        }

        public void InitializeFromData(byte[] stateData)
        {
            // Check for disposed
            CheckDisposed();

            bytes.AddRange(stateData);
            dataHash = -1;
        }

        public void Dispose()
        {
            bytes.Clear();
            readPointer = -1;
            dataHash = -1;

            // Add to awaiting states
            pool.PushReusable(this);
        }

        /// <summary>
        /// Prepares the state for read operatioins by seeking the read pointer back to the start.
        /// </summary>
        public void PrepareForRead()
        {
            // Check for disposed
            CheckDisposed();

            // Reset the read pointer
            readPointer = 0;
            dataSize = bytes.Count;
        }

        /// <summary>
        /// Clears all buffered data from this <see cref="ReplayState"/> and resets its state.
        /// </summary>
        public void Clear()
        {
            // Check for disposed
            CheckDisposed();

            bytes.Clear();
            readPointer = 0;
            dataSize = 0;
            dataHash = -1;
        }

        public override string ToString()
        {
            if (readPointer == -1 && dataHash == -1)
                return string.Format("ReplayState(<Disposed>)");

            return string.Format("ReplayState({0})", Size);
        }

        /// <summary>
        /// Get the <see cref="ReplayState"/> data as a byte array. 
        /// </summary>
        /// <returns>A byte array of data</returns>
        public byte[] ToArray()
        {
            // Check for disposed
            CheckDisposed();

            // Convert to byte array
            return bytes.ToArray();
        }        

        public string ToHexString()
        {
            StringBuilder builder = new StringBuilder(bytes.Count * 2);

            foreach (byte b in bytes)
                builder.AppendFormat("{0:x2}", b);

            return builder.ToString();
        }

        public bool IsDataEqual(ReplayState other)
        {
            // Check for disposed
            CheckDisposed();

            return bytes.SequenceEqual(other.bytes);
        }

        void IReplaySerialize.OnReplaySerialize(ReplayState state)
        {
            // Check for disposed
            CheckDisposed();

            if (state == this)
                throw new InvalidOperationException("Source state and target state references are the same");

            state.Write((ushort)bytes.Count);

            // Add range should be quicker
            state.bytes.AddRange(bytes);
        }

        void IReplaySerialize.OnReplayDeserialize(ReplayState state)
        {
            // Check for disposed
            CheckDisposed();

            if (state == this)
                throw new InvalidOperationException("Source state and target state references are the same");

            ushort size = state.ReadUInt16();

            byte[] data = state.ReadBytes(size);
            
            bytes.AddRange(data);

            //for (int i = 0; i < size; i++)
            //    bytes.Add(state.ReadByte());
        }

        void IReplayStreamSerialize.OnReplayStreamSerialize(BinaryWriter writer)
        {
            // Check for disposed
            CheckDisposed();

            writer.Write((ushort)bytes.Count);

            foreach (byte value in bytes)
                writer.Write(value);
        }

        void IReplayStreamSerialize.OnReplayStreamDeserialize(BinaryReader reader)
        {
            // Check for disposed
            CheckDisposed();

            ushort size = reader.ReadUInt16();

            if (size > 0)
            {
                // Read all bytes
                byte[] readBytes = reader.ReadBytes(size);

                // Add as range - faster than calling add multiple times
                bytes.AddRange(readBytes);
            }
            else
            {
                // Don't read if we dont need to
                bytes.Clear();
            }

            // Reset read pointer
            PrepareForRead();
        }

        public ReplayState ReadState()
        {
            ReplayState state = pool.GetReusable();

            ushort size = ReadUInt16();

            for (int i = 0; i < size; i++)
                state.bytes.Add(ReadByte());

            return state;
        }

        private long GetDataHash()
        {
            int p = 16777619;
            long hash = 2166136261L;
            int count = bytes.Count;

            for (int i = 0, j = count - 1; i < count; i++, j--)
            {
                hash = (hash ^ bytes[i] ^ (i * j)) * p;
            }

            hash += hash << 13;
            hash ^= hash >> 7;
            hash += hash << 3;
            hash ^= hash >> 17;
            hash += hash << 5;

            return hash;
        }

        private void CheckDisposed()
        {
            if (dataHash == -1 && readPointer == -1)
                throw new ObjectDisposedException("The replay state has been disposed");
        }

        public static bool IsTypeSerializable(Type type)
        {
            return serializeMethods.ContainsKey(type);
        }

        public static bool IsTypeSerializable<T>()
        {
            return serializeMethods.ContainsKey(typeof(T));
        }

        public static MethodInfo GetSerializeMethod(Type type)
        {
            MethodInfo method = null;
            serializeMethods.TryGetValue(type, out method);

            if (method == null && typeof(IReplaySerialize).IsAssignableFrom(type) == true)
                serializeMethods.TryGetValue(typeof(IReplaySerialize), out method);

            return method;
        }

        public static MethodInfo GetSerializeMethod<T>()
        {
            MethodInfo method = null;
            serializeMethods.TryGetValue(typeof(T), out method);

            if (method == null && typeof(IReplaySerialize).IsAssignableFrom(typeof(T)) == true)
                serializeMethods.TryGetValue(typeof(IReplaySerialize), out method);

            return method;
        }

        public static MethodInfo GetDeserializeMethod(Type type)
        {
            MethodInfo method = null;
            deserializeMethods.TryGetValue(type, out method);

            if (method == null && typeof(IReplaySerialize).IsAssignableFrom(type) == true)
                method = typeof(ReplayState).GetMethod(nameof(ReadSerializable), Type.EmptyTypes).MakeGenericMethod(type);

            return method;
        }

        public static MethodInfo GetDeserializeMethod<T>()
        {
            MethodInfo method = null;
            deserializeMethods.TryGetValue(typeof(T), out method);

            if (method == null && typeof(IReplaySerialize).IsAssignableFrom(typeof(T)) == true)
                method = typeof(ReplayState).GetMethod(nameof(ReadSerializable), Type.EmptyTypes).MakeGenericMethod(typeof(T));

            return method;
        }

        public static ReplayState FromByteArray(byte[] rawStateData)
        {
            ReplayState state = pool.GetReusable();

            state.InitializeFromData(rawStateData);

            return state;
        }
    }
}
