using System;
using System.Runtime.CompilerServices;
using System.Text;
using UltimateReplay.Core;
using UltimateReplay.Util;
using UnityEngine;

namespace UltimateReplay
{
    public sealed partial class ReplayState
    {
        // Methods
        public T ReadSerializable<T>() where T : IReplaySerialize, new()
        {
            CheckDisposed();
            suppressDisposeCheck = true;

            // Create instance
            T replaySerializable = new T();

            // Deserialize the object
            replaySerializable.OnReplayDeserialize(this);

            suppressDisposeCheck = false;
            return replaySerializable;
        }

        public bool ReadSerializable(IReplaySerialize replaySerializable)
        {
            CheckDisposed();
            suppressDisposeCheck = true;

            // Deserialize the object
            replaySerializable.OnReplayDeserialize(this);

            suppressDisposeCheck = false;
            return true;
        }

        public bool ReadSerializable<T>(ref T replaySerializable) where T : IReplaySerialize
        {
            CheckDisposed();
            suppressDisposeCheck = true;

            // Deserialize the object
            replaySerializable.OnReplayDeserialize(this);

            suppressDisposeCheck = false;
            return true;
        }

        /// <summary>
        /// Read a byte from the state.
        /// </summary>
        /// <returns>Byte value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            // Check for disposed
            //CheckDisposed();

            // Does not get inlined in debug mode and can slow the editor
            if(suppressDisposeCheck == false && dataHash == -1 && readPointer == -1)
                throw new ObjectDisposedException("The replay state has been disposed");

            if (dataSize == 0)
                throw new InvalidOperationException("There is no data in the object state");

            // Check for incorrect bytes
            if (readPointer >= dataSize)
                throw new InvalidOperationException("There are not enough bytes in the data to read the specified type");

            byte value = bytes[readPointer];

            // Advance pointer
            readPointer++;

            return value;
        }

        /// <summary>
        /// Read a byte array from the state.
        /// </summary>
        /// <param name="amount">The number of bytes to read</param>
        /// <returns>Byte array value</returns>
        public byte[] ReadBytes(int amount)
        {
            // Dispose checks carried out by the calling method where possible
            if(suppressDisposeCheck == false)
                CheckDisposed();

            suppressDisposeCheck = true;

            byte[] bytes = new byte[amount];

            // Store bytes
            for (int i = 0; i < amount; i++)
                bytes[i] = ReadByte();

            suppressDisposeCheck = false;
            return bytes;
        }

        /// <summary>
        /// Fill a byte array with data from the state.
        /// </summary>
        /// <param name="buffer">The byte array to store data in</param>
        /// <param name="offset">The index offset to start filling the buffer at</param>
        /// <param name="amount">The number of bytes to read</param>
        public void ReadBytes(byte[] buffer, int offset, int amount)
        {
            // Dispose checks carried out by the calling method where possible
            if(suppressDisposeCheck == false)
                CheckDisposed();


            suppressDisposeCheck = true;

            for (int i = offset; i < amount; i++)
                buffer[i] = ReadByte();

            suppressDisposeCheck = false;
        }

        /// <summary>
        /// Read a short from the state.
        /// </summary>
        /// <returns>Short value</returns>
        public short ReadInt16()
        {
            // Read into the shared buffer
            ReadBytes(sharedBuffer, 0, sizeof(short));

            // Convert to short
            return BitConverterNonAlloc.GetInt16(sharedBuffer);
        }

        public ushort ReadUInt16()
        {
            return (ushort)ReadInt16();
        }

        /// <summary>
        /// Read an int from the state.
        /// </summary>
        /// <returns>Int value</returns>
        public int ReadInt32()
        {
            // Read into the shared buffer
            ReadBytes(sharedBuffer, 0, sizeof(int));

            // Convert to int
            return BitConverterNonAlloc.GetInt32(sharedBuffer);
        }

        public uint ReadUInt32()
        {
            return (uint)ReadInt32();
        }

        public long ReadInt64()
        {
            // Read into the shared buffer
            ReadBytes(sharedBuffer, 0, sizeof(long));

            // Convert to int
            return BitConverterNonAlloc.GetInt64(sharedBuffer);
        }

        public ulong ReadUInt64()
        {
            return (ulong)ReadInt64();
        }

        /// <summary>
        /// Read a float from the state.
        /// </summary>
        /// <returns>Float value</returns>
        public float ReadFloat()
        {
            // Read into the shared buffer
            ReadBytes(sharedBuffer, 0, sizeof(float));

            // Convert to float
            return BitConverterNonAlloc.GetFloat(sharedBuffer);
        }

        public double ReadDouble()
        {
            // Read into the shared buffer
            ReadBytes(sharedBuffer, 0, sizeof(double));

            // Convert to float
            return BitConverterNonAlloc.GetDouble(sharedBuffer);
        }

        /// <summary>
        /// Read a bool from the state.
        /// </summary>
        /// <returns>Bool value</returns>
        public bool ReadBool()
        {
            // Read into the shared buffer
            ReadBytes(sharedBuffer, 0, sizeof(bool));

            // Convert to bool
            return BitConverterNonAlloc.GetBool(sharedBuffer);
        }

        /// <summary>
        /// Read a string from the state
        /// </summary>
        /// <returns>string value</returns>
        public string ReadString()
        {
            // Read the string size
            short size = ReadInt16();

            // Read the required number of bytes
            byte[] bytes = ReadBytes(size);

            // Decode the string
#if UNITY_WINRT && !UNITY_EDITOR
            return Encoding.UTF8.GetString(bytes);
#else
            return Encoding.Default.GetString(bytes);
#endif
        }
        
        /// <summary>
        /// Read a vector2 from the state.
        /// </summary>
        /// <returns>Vector2 value</returns>
        public Vector2 ReadVec2()
        {
            float x = ReadFloat();
            float y = ReadFloat();

            // Create vector
            return new Vector2(x, y);
        }

        /// <summary>
        /// Read a vector3 from the state.
        /// </summary>
        /// <returns>Vector3 value</returns>
        public Vector3 ReadVec3()
        {
            float x = ReadFloat();
            float y = ReadFloat();
            float z = ReadFloat();

            // Create vector
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Read a vector4 from the state.
        /// </summary>
        /// <returns>Vector4 value</returns>
        public Vector4 ReadVec4()
        {
            float x = ReadFloat();
            float y = ReadFloat();
            float z = ReadFloat();
            float w = ReadFloat();

            // Create vector
            return new Vector4(x, y, z, w);
        }

        /// <summary>
        /// Read a quaternion from the state.
        /// </summary>
        /// <returns>Quaternion value</returns>
        public Quaternion ReadQuat()
        {
            float x = ReadFloat();
            float y = ReadFloat();
            float z = ReadFloat();
            float w = ReadFloat();

            // Create quaternion
            return new Quaternion(x, y, z, w);
        }

        /// <summary>
        /// Read a colour from the state.
        /// </summary>
        /// <returns>Colour value</returns>
        public Color ReadColor()
        {
            float r = ReadFloat();
            float g = ReadFloat();
            float b = ReadFloat();
            float a = ReadFloat();

            // Create colour
            return new Color(r, g, b, a);
        }

        /// <summary>
        /// Read a colour32 from the state.
        /// </summary>
        /// <returns>Colour32 value</returns>
        public Color32 ReadColor32()
        {
            byte r = ReadByte();
            byte g = ReadByte();
            byte b = ReadByte();
            byte a = ReadByte();

            // Create colour
            return new Color32(r, g, b, a);
        }

        /// <summary>
        /// Attempts to read a low precision float.
        /// You should only use this method when the value is relativley small (less than 65000) and accuracy is not essential.
        /// </summary>
        /// <returns>float value</returns>
        public float ReadFloatLowPrecision()
        {
            // Read 16 bits
            short value = ReadInt16();

            //// Find the factor
            //int count = value >> 12;

            //// Decode main
            //float decoded = value & 0xfff;

            //while(count > 0)
            //{
            //    decoded /= 10f;
            //    count--;
            //}

            float decoded = value / 256f;

            return decoded;
        }

        /// <summary>
        /// Attempts to read a low precision vector2.
        /// </summary>
        /// <returns>vector2 value</returns>
        public Vector2 ReadVec2LowPrecision()
        {
            float x = ReadFloatLowPrecision();
            float y = ReadFloatLowPrecision();

            // Create vector
            return new Vector2(x, y);
        }

        /// <summary>
        /// Attempts to read a low precision vector3.
        /// </summary>
        /// <returns>vector3 value</returns>
        public Vector3 ReadVec3LowPrecision()
        {
            float x = ReadFloatLowPrecision();
            float y = ReadFloatLowPrecision();
            float z = ReadFloatLowPrecision();

            // Create vector
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Attempts to read a low precision vector4.
        /// </summary>
        /// <returns>vector4 value</returns>
        public Vector4 ReadVec4LowPrecision()
        {
            float x = ReadFloatLowPrecision();
            float y = ReadFloatLowPrecision();
            float z = ReadFloatLowPrecision();
            float w = ReadFloatLowPrecision();

            // Create vector
            return new Vector4(x, y, z, w);
        }

        /// <summary>
        /// Attempts to read a low precision quaternion.
        /// </summary>
        /// <returns>quaternion value</returns>
        public Quaternion ReadQuatLowPrecision()
        {
            float x = ReadFloatLowPrecision();
            float y = ReadFloatLowPrecision();
            float z = ReadFloatLowPrecision();
            float w = ReadFloatLowPrecision();

            // Create vector
            return new Quaternion(x, y, z, w);
        }
    }
}
