/*
 * Copyright 2022 Sony Corporation
 */
using System;
using System.Runtime.InteropServices;

namespace Sony.SMF
{
    /// <summary>
    /// Class that converts received data so that it can be used (use "sony_motion_format.dll")
    /// </summary>
    public sealed class SonyMotionFormat
    {
        #region --Fields--
        /// <summary>
        /// Constant of library name
        /// </summary>
#if UNITY_IOS && !UNITY_EDITOR_OSX
        public const string SONY_MOTION_FORMAT_LIBRARY_NAME = "__Internal";
#else
        public const string SONY_MOTION_FORMAT_LIBRARY_NAME = "sony_motion_format";
#endif
        #endregion --Fields--
        #region --Methods--
        /// <summary>
        /// Converts bytes to skeleton definition
        /// </summary>
        /// <param name="bytes_size">Byte size</param>
        /// <param name="bytes">Byte data</param>
        /// <param name="sender_ip">Sender IP address</param>
        /// <param name="sender_port">Sender port number</param>
        /// <param name="size">Size</param>
        /// <param name="joint_ids">Id of joints</param>
        /// <param name="parent_joint_ids">Id of parent joints</param>
        /// <param name="rotations_x">rotations in the X direction</param>
        /// <param name="rotations_y">rotations in the Y direction</param>
        /// <param name="rotations_z">rotations in the Z direction</param>
        /// <param name="rotations_w">rotations in the W direction</param>
        /// <param name="positions_x">X coordinate of position</param>
        /// <param name="positions_y">Y coordinate of position</param>
        /// <param name="positions_z">Z coordinate of position</param>
        /// <returns>Whether the conversion was successful or not</returns>
        [DllImport(SONY_MOTION_FORMAT_LIBRARY_NAME)]
        public static extern bool ConvertBytesToSkeletonDefinition(
            int bytes_size,
            byte[] bytes,
            out ulong sender_ip,
            out int sender_port,
            out int size,
            out IntPtr joint_ids,
            out IntPtr parent_joint_ids,
            out IntPtr rotations_x,
            out IntPtr rotations_y,
            out IntPtr rotations_z,
            out IntPtr rotations_w,
            out IntPtr positions_x,
            out IntPtr positions_y,
            out IntPtr positions_z
        );

        /// <summary>
        /// Converts bytes to frame data
        /// </summary>
        /// <param name="bytes_size">Byte size</param>
        /// <param name="bytes">Byte data</param>
        /// <param name="sender_ip">Sender IP address</param>
        /// <param name="sender_port">Sender port number</param>
        /// <param name="frame_id">Frame Id</param>
        /// <param name="timestamp">Timestamp</param>
        /// <param name="unixTime">Unix time when sensor sent data</param>
        /// <param name="size">Size</param>
        /// <param name="joint_ids">Id of joints</param>
        /// <param name="rotations_x">rotations in the X direction</param>
        /// <param name="rotations_y">rotations in the Y direction</param>
        /// <param name="rotations_z">rotations in the Z direction</param>
        /// <param name="rotations_w">rotations in the W direction</param>
        /// <param name="positions_x">X coordinate of position</param>
        /// <param name="positions_y">Y coordinate of position</param>
        /// <param name="positions_z">Z coordinate of position</param>
        /// <returns>Whether the conversion was successful or not</returns>
        [DllImport(SONY_MOTION_FORMAT_LIBRARY_NAME)]
        public static extern bool ConvertBytesToFrameData(
            int bytes_size,
            byte[] bytes,
            out ulong sender_ip,
            out int sender_port,
            out int frame_id,
            out float timestamp,
            out double unixTime,
            out int size,
            out IntPtr joint_ids,
            out IntPtr rotations_x,
            out IntPtr rotations_y,
            out IntPtr rotations_z,
            out IntPtr rotations_w,
            out IntPtr positions_x,
            out IntPtr positions_y,
            out IntPtr positions_z
        );

        /// <summary>
        /// Judges if the bytes are in SonyMotionFormat's format
        /// </summary>
        /// <param name="bytes_size">Byte size</param>
        /// <param name="bytes">Byte data</param>
        /// <returns>Correct format or not</returns>
        [DllImport(SONY_MOTION_FORMAT_LIBRARY_NAME)]
        public static extern bool IsSmfBytes(
            int bytes_size,
            byte[] bytes
        );

        /// <summary>
        /// Judges if the bytes are in skeleton definition
        /// </summary>
        /// <param name="bytes_size">Byte size</param>
        /// <param name="bytes">Byte data</param>
        /// <returns>whether it is a skeleton definition</returns>
        [DllImport(SONY_MOTION_FORMAT_LIBRARY_NAME)]
        public static extern bool IsSkeletonDefinitionBytes(
            int bytes_size,
            byte[] bytes
        );

        /// <summary>
        /// Judges if the bytes are in frame data
        /// </summary>
        /// <param name="bytes_size">Byte size</param>
        /// <param name="bytes">Byte data</param>
        /// <returns>whether it is a frame data</returns>
        [DllImport(SONY_MOTION_FORMAT_LIBRARY_NAME)]
        public static extern bool IsFrameDataBytes(
            int bytes_size,
            byte[] bytes
        );
        #endregion --Methods--
    }
}
