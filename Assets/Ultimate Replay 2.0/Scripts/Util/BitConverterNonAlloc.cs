using System.Runtime.InteropServices;

namespace UltimateReplay.Util
{
    /// <summary>
    /// Used as a union for conversion between common 32 bit data types without the use of unsafe code.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct Common32
    {
        // Private
        private static Common32 conversion = new Common32(); // Use a cached instance

        // Public
        /// <summary>
        /// The value represented as a float.
        /// </summary>
        [FieldOffset(0)]
        public float single;

        /// <summary>
        /// The value represented as an int.
        /// </summary>
        [FieldOffset(0)]
        public int integer;

        // Methods
        /// <summary>
        /// Converts a value from an integer to float.
        /// This is the equivilent of mapping absolute bits.
        /// </summary>
        /// <param name="value">The int value to convert</param>
        /// <returns>The float value result</returns>
        public static float ToSingle(int value)
        {
            conversion.integer = value;
            return conversion.single;
        }

        /// <summary>
        /// Converts a value from a float to an integer.
        /// This is the equivilent of mapping absolute bits.
        /// </summary>
        /// <param name="value">The float value to convert</param>
        /// <returns>The int value result</returns>
        public static int ToInteger(float value)
        {
            conversion.single = value;
            return conversion.integer;
        }
    }

    /// <summary>
    /// Used as a union for conversion between common 64 bit data types without the use of unsafe code.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct Common64
    {
        // Private
        private static Common64 conversion = new Common64(); // Use a cached instance

        // Public
        /// <summary>
        /// The value represented as a double.
        /// </summary>
        [FieldOffset(0)]
        public double single;

        /// <summary>
        /// The value represented as an 64-bit int.
        /// </summary>
        [FieldOffset(0)]
        public long integer;

        // Methods
        /// <summary>
        /// Converts a value from a 64-bit integer to double.
        /// This is the equivilent of mapping absolute bits.
        /// </summary>
        /// <param name="value">The 64-bit integer value to convert</param>
        /// <returns>The double value result</returns>
        public static double ToDouble(long value)
        {
            conversion.integer = value;
            return conversion.single;
        }

        /// <summary>
        /// Converts a value from a double to a 64-bit integer.
        /// This is the equivilent of mapping absolute bits.
        /// </summary>
        /// <param name="value">The double value to convert</param>
        /// <returns>The 64-bit int value result</returns>
        public static long ToInteger(double value)
        {
            conversion.single = value;
            return conversion.integer;
        }
    }

    /// <summary>
    /// Custom implmenetation of the BitConverter class that does not make any allocations.
    /// This is important as the methods may be called thousands of times per second.
    /// </summary>
    public static class BitConverterNonAlloc
    {
        // Methods
        #region ToBytes     
        /// <summary>
        /// Store a 16 bit int into the specified byte array.
        /// <param name="buffer">The buffer to store the int which must have a size of 2 or greater</param>
        /// <param name="value">The short value to store</param>
        /// </summary>
        public static void GetBytes(byte[] buffer, short value)
        {
            unchecked
            {
                buffer[0] = (byte)((value >> 8) & 0xFF);
                buffer[1] = (byte)(value & 0xFF);
            }
        }

        /// <summary>
        /// Store a 32-bit int into the specified byte array.
        /// </summary>
        /// <param name="buffer">The buffer to store the int which must have a size of 4 or greater</param>
        /// <param name="value">The int value to store</param>
        public static void GetBytes(byte[] buffer, int value)
        {
            unchecked
            {
                buffer[0] = (byte)((value >> 24) & 0xFF);
                buffer[1] = (byte)((value >> 16) & 0xFF);
                buffer[2] = (byte)((value >> 8) & 0xFF);
                buffer[3] = (byte)(value & 0xFF);
            }
        }

        /// <summary>
        /// Store a 64-bit int into the specified byte array.
        /// </summary>
        /// <param name="buffer">The buffer to store the int which must have a size of 8 or greater</param>
        /// <param name="value">The int value to store</param>
        public static void GetBytes(byte[] buffer, long value)
        {
            unchecked
            {
                buffer[0] = (byte)((value >> 56) & 0xFF);
                buffer[1] = (byte)((value >> 48) & 0xFF);
                buffer[2] = (byte)((value >> 40) & 0xFF);
                buffer[3] = (byte)((value >> 32) & 0xFF);
                buffer[4] = (byte)((value >> 24) & 0xFF);
                buffer[5] = (byte)((value >> 16) & 0xFF);
                buffer[6] = (byte)((value >> 8) & 0xFF);
                buffer[7] = (byte)(value & 0xFF);
            }
        }

        /// <summary>
        /// Store a 32-bit float into the specified byte array.
        /// </summary>
        /// <param name="buffer">The buffer to store the float which must have a size of 4 or greated</param>
        /// <param name="value">The float value to store</param>
        public static void GetBytes(byte[] buffer, float value)
        {
            unchecked
            {
                // Convert the float to a common 32 bit value
                int intValue = Common32.ToInteger(value);

                // Call through
                GetBytes(buffer, intValue);
            }
        }

        /// <summary>
        /// Store a 64-bit decimal value into the specified byte array.
        /// </summary>
        /// <param name="buffer">The buffer to store thevalue which must have a size of 8 or greater</param>
        /// <param name="value">The value to store</param>
        public static void GetBytes(byte[] buffer, double value)
        {
            unchecked
            {
                // Convert the double to a common 64 bit value
                long intValue = Common64.ToInteger(value);

                // Call through
                GetBytes(buffer, intValue);
            }
        }

        /// <summary>
        /// Store an 8-bit bool into the specified byte array.
        /// </summary>
        /// <param name="buffer">The buffer to store the bool which must have a size of 1 or greater</param>
        /// <param name="value">The bool value to store</param>
        public static void GetBytes(byte[] buffer, bool value)
        {
            unchecked
            {
                buffer[0] = (byte)((value == true) ? 1 : 0);
            }
        }

        #endregion

        #region FromBytes
        /// <summary>
        /// Retreive a 16-bit int from the specified byte array.
        /// </summary>
        /// <param name="buffer">The buffer to retreive the short from which must have a size of 2 or greater</param>
        /// <returns>The unpacked short value</returns>
        public static short GetInt16(byte[] buffer)
        {
            unchecked
            {

                return (short)((buffer[0] << 8)
                    | buffer[1]);
            }
        }

        /// <summary>
        /// Retreive a 32-bit int from the specified byte array.
        /// </summary>
        /// <param name="buffer">The buffer to retreive the int from which must have a size of 4 or greater</param>
        /// <returns>The unpacked int value</returns>
        public static int GetInt32(byte[] buffer)
        {
            unchecked
            {
                return (int)((buffer[0] << 24)
                    | (buffer[1] << 16)
                    | (buffer[2] << 8)
                    | buffer[3]);
            }
        }

        /// <summary>
        /// Retreive a 64-bit int from the specified byte array.
        /// </summary>
        /// <param name="buffer">The buffer to retreive the int from which must have a size of 8 or greater</param>
        /// <returns>The unpacked long int value</returns>
        public static long GetInt64(byte[] buffer)
        {
            unchecked
            {
                return (long)((buffer[0] << 56)
                    | (buffer[1] << 48)
                    | (buffer[2] << 40)
                    | (buffer[3] << 32)
                    | (buffer[4] << 24)
                    | (buffer[5] << 16)
                    | (buffer[6] << 8)
                    | buffer[7]);
            }
        }

        /// <summary>
        /// Retreive a 32-bit float from the specified byte array.
        /// </summary>
        /// <param name="buffer">The buffer to retreive the float from which must have a size of 4 or greater</param>
        /// <returns>The unpacked float value</returns>
        public static float GetFloat(byte[] buffer)
        {
            unchecked
            {
                // Call through to read a 32 bit value
                int value = GetInt32(buffer);

                // Convert to commmon value
                return Common32.ToSingle(value);
            }
        }

        /// <summary>
        /// Get a 64-bit decimal value from the specified byte array.
        /// </summary>
        /// <param name="buffer">The buffer to retrieve the data from which must have a size of 8 or greater</param>
        /// <returns>The unpacked double value</returns>
        public static double GetDouble(byte[] buffer)
        {
            unchecked
            {
                // Call through to read 64 bits of data
                long value = GetInt64(buffer);

                // Convert to common value
                return Common64.ToDouble(value);
            }
        }

        /// <summary>
        /// Retreive a 8-bit bool from the specified byte array.
        /// </summary>
        /// <param name="buffer">The buffer to retreive the bool from which must have a size of 1 or greater</param>
        /// <returns>The unpacked bool value</returns>
        public static bool GetBool(byte[] buffer)
        {
            unchecked
            {
                return buffer[0] != 0;
            }
        }
        #endregion
    }
}
