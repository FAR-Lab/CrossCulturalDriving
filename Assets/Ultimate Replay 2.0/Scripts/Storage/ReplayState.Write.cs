using System;
using System.Text;
using UltimateReplay.Core;
using UltimateReplay.Util;
using UnityEngine;

namespace UltimateReplay
{
    public partial class ReplayState
    {
        public void Write(IReplaySerialize replaySerializable)
        {
            CheckDisposed();
            suppressDisposeCheck = true;

            // Serialize the object
            replaySerializable.OnReplaySerialize(this);

            suppressDisposeCheck = false;
        }

        /// <summary>
        /// Write a byte to the state.
        /// </summary>
        /// <param name="value">Byte value</param>
        public void Write(byte value)
        {
            // Check for disposed
            //CheckDisposed();

            // Does not get inlined in debug mode and can slow the editor
            if (suppressDisposeCheck == false && dataHash == -1 && readPointer == -1)
                throw new ObjectDisposedException("The replay state has been disposed");

            bytes.Add(value);

            // Reset cached hash because the data has changed
            dataHash = -1;
        }

        public void Write(sbyte value)
        {
            Write((byte)value);
        }

        /// <summary>
        /// Write a byte array to the state.
        /// </summary>
        /// <param name="bytes">Byte array value</param>
        public void Write(byte[] bytes)
        {
            // Dispose checks carried out by the calling method where possible
            if (suppressDisposeCheck == false)
                CheckDisposed();


            suppressDisposeCheck = true;

            for (int i = 0; i < bytes.Length; i++)
                Write(bytes[i]);

            suppressDisposeCheck = false;
        }

        /// <summary>
        /// Write a byte array to the state using an offset position and length.
        /// </summary>
        /// <param name="bytes">Byte array value</param>
        /// <param name="offset">The start index to read data from the array</param>
        /// <param name="length">The amount of data to read</param>
        public void Write(byte[] bytes, int offset, int length)
        {
            // Dispose checks carried out by the calling method where possible
            if (suppressDisposeCheck == false)
                CheckDisposed();

            suppressDisposeCheck = true;

            for (int i = offset; i < length; i++)
                Write(bytes[i]);

            suppressDisposeCheck = false;
        }

        /// <summary>
        /// Write a short to the state.
        /// </summary>
        /// <param name="value">Short value</param>
        public void Write(short value)
        {
            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, value);

            // Write all bytes
            Write(sharedBuffer, 0, sizeof(short));
        }

        public void Write(ushort value)
        {
            Write((short)value);
        }

        /// <summary>
        /// Write an int to the state.
        /// </summary>
        /// <param name="value">Int value</param>
        public void Write(int value)
        {
            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, value);

            // Write all bytes
            Write(sharedBuffer, 0, sizeof(int));
        }

        public void Write(uint value)
        {
            Write((int)value);
        }

        public void Write(long value)
        {
            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, value);

            // Write all bytes
            Write(sharedBuffer, 0, sizeof(long));
        }

        public void Write(ulong value)
        {
            Write((long)value);
        }

        /// <summary>
        /// Write a float to the state.
        /// </summary>
        /// <param name="value">Float value</param>
        public void Write(float value)
        {
            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, value);

            // Write all bytes
            Write(sharedBuffer, 0, sizeof(float));
        }

        public void Write(double value)
        {
            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, value);

            // Write all bytes
            Write(sharedBuffer, 0, sizeof(double));
        }

        /// <summary>
        /// Write a bool to the state.
        /// </summary>
        /// <param name="value">bool value</param>
        public void Write(bool value)
        {
            // Use the shared buffer instead of allocating a new array
            BitConverterNonAlloc.GetBytes(sharedBuffer, value);

            // Write all bytes
            Write(sharedBuffer, 0, sizeof(bool));
        }

        /// <summary>
        /// Write a string to the state.
        /// </summary>
        /// <param name="value">string value</param>
        public void Write(string value)
        {
            // Get string bytes
#if UNITY_WINRT && !UNITY_EDITOR
            byte[] bytes = Encoding.UTF8.GetBytes(value);
#else
            byte[] bytes = Encoding.Default.GetBytes(value);
#endif

            // Write all bytes
            Write((short)bytes.Length);
            Write(bytes);
        }

        /// <summary>
        /// Write a vector2 to the state.
        /// </summary>
        /// <param name="value">Vector2 value</param>
        public void Write(Vector2 value)
        {
            CheckDisposed();
            suppressDisposeCheck = true;

            Write(value.x);
            Write(value.y);

            suppressDisposeCheck = false;
        }

        /// <summary>
        /// Write a vector3 to the state.
        /// </summary>
        /// <param name="value">Vector3 value</param>
        public void Write(Vector3 value)
        {
            CheckDisposed();
            suppressDisposeCheck = true;

            Write(value.x);
            Write(value.y);
            Write(value.z);

            suppressDisposeCheck = false;
        }

        /// <summary>
        /// Write a vector4 to the state.
        /// </summary>
        /// <param name="value">Vector4 value</param>
        public void Write(Vector4 value)
        {
            CheckDisposed();
            suppressDisposeCheck = true;

            Write(value.x);
            Write(value.y);
            Write(value.z);
            Write(value.w);

            suppressDisposeCheck = false;
        }

        /// <summary>
        /// Write a quaternion to the state.
        /// </summary>
        /// <param name="value">Quaternion value</param>
        public void Write(Quaternion value)
        {
            CheckDisposed();
            suppressDisposeCheck = true;

            Write(value.x);
            Write(value.y);
            Write(value.z);
            Write(value.w);

            suppressDisposeCheck = false;
        }

        /// <summary>
        /// Write a colour to the state.
        /// </summary>
        /// <param name="value">Colour value</param>
        public void Write(Color value)
        {
            CheckDisposed();
            suppressDisposeCheck = true;

            Write(value.r);
            Write(value.g);
            Write(value.b);
            Write(value.a);

            suppressDisposeCheck = false;
        }

        /// <summary>
        /// Write a colour32 value to the state.
        /// </summary>
        /// <param name="value">Colour32 value</param>
        public void Write(Color32 value)
        {
            CheckDisposed();
            suppressDisposeCheck = true;

            Write(value.r);
            Write(value.g);
            Write(value.b);
            Write(value.a);

            suppressDisposeCheck = false;
        }

        /// <summary>
        /// Attempts to write a 32 bit float value as a low precision 16 bit representation.
        /// You should only use this method when the value is relativley small (less than 65000).
        /// Accuracy may be lost by storing low precision values.
        /// </summary>
        /// <param name="value">float value</param>
        public void WriteLowPrecision(float value)
        {
            //int count = 0;

            //while(value != Math.Floor(value))
            //{
            //    // Shift left
            //    value *= 10f;
            //    count++;
            //}

            //// Encode the value into 2 bytes
            //short encoded = (short)((count << 12) + (int)value);

            short encoded = (short)(value * 256);

            // Write the short value
            Write(encoded);
        }

        /// <summary>
        /// Write a vector2 to the state using half precision packing.
        /// Accuracy may be lost by storing low precision values.
        /// </summary>
        /// <param name="value">vector2 value</param>
        public void WriteLowPrecision(Vector2 value)
        {
            CheckDisposed();
            suppressDisposeCheck = true;

            WriteLowPrecision(value.x);
            WriteLowPrecision(value.y);

            suppressDisposeCheck = false;
        }

        /// <summary>
        /// Write a vector3 to the state using half precision packing.
        /// Accuracy may be lost by storing low precision values.
        /// </summary>
        /// <param name="value">vector3 value</param>
        public void WriteLowPrecision(Vector3 value)
        {
            CheckDisposed();
            suppressDisposeCheck = true;

            WriteLowPrecision(value.x);
            WriteLowPrecision(value.y);
            WriteLowPrecision(value.z);

            suppressDisposeCheck = false;
        }

        /// <summary>
        /// Write a vector4 to the state using half precision packing.
        /// Accuracy may be lost by storing low precision values.
        /// </summary>
        /// <param name="value">vector4 value</param>
        public void WriteLowPrecision(Vector4 value)
        {
            CheckDisposed();
            suppressDisposeCheck = true;

            WriteLowPrecision(value.x);
            WriteLowPrecision(value.y);
            WriteLowPrecision(value.z);
            WriteLowPrecision(value.w);

            suppressDisposeCheck = false;
        }

        /// <summary>
        /// Write a quaternion to the state using half precision packing.
        /// Accuracy may be lost by storing low precision values.
        /// </summary>
        /// <param name="value">quaternion value</param>
        public void WriteLowPrecision(Quaternion value)
        {
            CheckDisposed();
            suppressDisposeCheck = true;

            WriteLowPrecision(value.x);
            WriteLowPrecision(value.y);
            WriteLowPrecision(value.z);
            WriteLowPrecision(value.w);

            suppressDisposeCheck = false;
        }
    }
}
