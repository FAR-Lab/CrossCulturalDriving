/*
 * Copyright 2022 Sony Corporation
 */

using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sony.SMF;
using UnityEngine;

namespace Mocopi.Receiver.Core {
    /// <summary>
    ///     Class receiving by UDP from the sensor for handling
    /// </summary>
    public sealed class MocopiUdpReceiver {
        #region --Constructors--

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="port">Udp port number</param>
        public MocopiUdpReceiver(int port, string multicastAddress = null, int multicastPort = 0) {
            Port = port;
            // MULTICAST ADDITION
            if (multicastAddress != null) {
                _multicastAddress = multicastAddress;
                _multicastPort = multicastPort;
                UseMulticast = true;
            }
            else {
                UseMulticast = false;
            }
        }

        #endregion --Constructors--

        #region --Finalizers--

        /// <summary>
        ///     Destructor
        /// </summary>
        ~MocopiUdpReceiver() {
            UdpStop();
        }

        #endregion --Finalizers--

        #region --Fields--

        /// <summary>
        ///     Delegate type variables for bone initialization
        /// </summary>
        public ReceiveSkeletonDefinitionEvent OnReceiveSkeletonDefinition = (boneIds, parentBoneIds, rotationsX,
            rotationsY, rotationsZ, rotationsW, positionsX, positionsY, positionsZ
        ) => { };

        /// <summary>
        ///     Delegate type variables for achieving bone movement
        /// </summary>
        public ReceiveFrameDataEvent OnReceiveFrameData = (frameId, timestamp, unixTime, boneIds, rotationsX,
            rotationsY, rotationsZ, rotationsW, positionsX, positionsY, positionsZ
        ) => { };

        /// <summary>
        ///     Delegate type variables for start error event
        /// </summary>
        public ErrorOccurredEvent OnUdpStartFailed = _ => { };

        /// <summary>
        ///     Delegate type variables for receive error event
        /// </summary>
        public ErrorOccurredEvent OnUdpReceiveFailed = _ => { };

        /// <summary>
        ///     Object for exclusive access control
        /// </summary>
        private static readonly object lockObject = new();

        /// <summary>
        ///     Threading task
        /// </summary>
        private Task task;

        /// <summary>
        ///     Manage cancellation notifications to cancellation tokens
        /// </summary>
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        ///     Udp client
        /// </summary>
        private UdpClient udpClient;

        #endregion --Fields--

        #region --Delegates--

        /// <summary>
        ///     Define a delegate for bone initialization
        /// </summary>
        /// <param name="boneIds">Id of bones</param>
        /// <param name="parentBoneIds">Id of parent bones</param>
        /// <param name="rotationsX">rotations in the X direction</param>
        /// <param name="rotationsY">rotations in the Y direction</param>
        /// <param name="rotationsZ">rotations in the Z direction</param>
        /// <param name="rotationsW">rotations in the W direction</param>
        /// <param name="positionsX">X coordinate of position</param>
        /// <param name="positionsY">Y coordinate of position</param>
        /// <param name="positionsZ">Z coordinate of position</param>
        public delegate void ReceiveSkeletonDefinitionEvent(
            int[] boneIds, int[] parentBoneIds,
            float[] rotationsX, float[] rotationsY, float[] rotationsZ, float[] rotationsW,
            float[] positionsX, float[] positionsY, float[] positionsZ
        );

        /// <summary>
        ///     Define a delegate to achieve bone movement
        /// </summary>
        /// <param name="frameId">Frame Id</param>
        /// <param name="timestamp">Timestamp</param>
        /// <param name="unixTime">Unix time when sensor sent data</param>
        /// <param name="boneIds">Id of bones</param>
        /// <param name="rotationsX">rotations in the X direction</param>
        /// <param name="rotationsY">rotations in the Y direction</param>
        /// <param name="rotationsZ">rotations in the Z direction</param>
        /// <param name="rotationsW">rotations in the W direction</param>
        /// <param name="positionsX">X coordinate of position</param>
        /// <param name="positionsY">Y coordinate of position</param>
        /// <param name="positionsZ">Z coordinate of position</param>
        public delegate void ReceiveFrameDataEvent(
            int frameId, float timestamp, double unixTime,
            int[] boneIds,
            float[] rotationsX, float[] rotationsY, float[] rotationsZ, float[] rotationsW,
            float[] positionsX, float[] positionsY, float[] positionsZ
        );

        /// <summary>
        ///     Define a delegate to error event
        /// </summary>
        /// <param name="e">Content of exception</param>
        public delegate void ErrorOccurredEvent(Exception e);

        #endregion --Delegates--

        #region --Properties--

        // MULTICAST ADDITION
        public bool UseMulticast;
        private readonly string _multicastAddress;
        private readonly int _multicastPort;


        /// <summary>
        ///     Port
        /// </summary>
        public int Port { get; }

        /// <summary>
        ///     Is it running
        /// </summary>
        public bool IsRunning => task != null && !task.IsCanceled && !task.IsCompleted;

        #endregion --Properties--

        #region --Methods--

        /// <summary>
        ///     Start receiving UDP
        /// </summary>
        public void UdpStart() {
            UdpStop();

            if (IsRunning) return;

            try {
                cancellationTokenSource = new CancellationTokenSource();
                // MULTICAST ADDITION
                if (UseMulticast) {
                    udpClient = new UdpClient(_multicastPort);
                    udpClient.JoinMulticastGroup(IPAddress.Parse(_multicastAddress));
                }
                else {
                    udpClient = new UdpClient(Port);
                }

                task = Task.Run(() => UdpTaskAsync(cancellationTokenSource.Token));
            }
            catch (Exception e) {
                Debug.LogErrorFormat($"[MocopiUdpReceiver] Udp start failed. {e.GetType()} : {e.Message}");
                OnUdpStartFailed?.Invoke(e);
            }
        }

        /// <summary>
        ///     Stop receiving UDP
        /// </summary>
        public void UdpStop() {
            if (cancellationTokenSource != null) {
                cancellationTokenSource.Cancel();
                task = null;
            }

            if (udpClient != null) {
                //MULTICAST ADDITION
                if (UseMulticast) udpClient.DropMulticastGroup(IPAddress.Parse(_multicastAddress));
                udpClient.Close();
                udpClient = null;
            }
        }

        /// <summary>
        ///     Convert pointers to arrays
        /// </summary>
        /// <typeparam name="T">Generic class</typeparam>
        /// <param name="ptr">Pointer</param>
        /// <param name="length">Length</param>
        /// <returns>Converted array</returns>
        private static T[] PointerToArray<T>(IntPtr ptr, int length) {
            var size = Marshal.SizeOf(typeof(T));
            var array = new T[length];
            for (var i = 0; i < length; i++) {
                var p = new IntPtr(ptr.ToInt64() + i * size);
                array[i] = Marshal.PtrToStructure<T>(p);
            }

            return array;
        }

        /// <summary>
        ///     Convert IP address from ULong type to string
        /// </summary>
        /// <param name="u">String type IP address</param>
        /// <returns>String type IP address</returns>
        private static string ULongToIpAddressString(ulong u) {
            short ip1, ip2, ip3, ip4;
            ip4 = (short)(u % 1000);
            u /= 1000;
            ip3 = (short)(u % 1000);
            u /= 1000;
            ip2 = (short)(u % 1000);
            u /= 1000;
            ip1 = (short)(u % 1000);

            return ip1 + "." + ip2 + "." + ip3 + "." + ip4;
        }

        /// <summary>
        ///     Convert the data received by UDP to move the avatar
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        private async void UdpTaskAsync(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested && udpClient != null) {
                var id = Thread.CurrentThread.ManagedThreadId;
                try {
                    IPEndPoint remoteEP = null;
                    var message = udpClient.Receive(ref remoteEP);

                    lock (lockObject) {
                        if (SonyMotionFormat.IsSmfBytes(message.Length, message))
                            // Processing of data acquired in "SonyMotionFormat"
                            HandleSonyMotionFormatData(message);
                    }
                }
                catch (SocketException e) {
                    Debug.Log($"[MocopiUdpReceiver] {e.Message} : {e.GetType()}");
                }
                catch (Exception e) {
                    Debug.LogErrorFormat($"[MocopiUdpReceiver] Udp receive failed. {e.Message} : {e.GetType()}");
                    UdpStop();
                    OnUdpReceiveFailed?.Invoke(e);
                    break;
                }

                await Task.Delay(1);
            }
        }

        /// <summary>
        ///     Convert "SonyMotionFormat" data to move the avatar
        /// </summary>
        /// <param name="message">Udp data</param>
        private void HandleSonyMotionFormatData(byte[] message) {
            var bytesSize = message.Length;
            if (SonyMotionFormat.IsSkeletonDefinitionBytes(bytesSize, message)) {
                if (SonyMotionFormat.ConvertBytesToSkeletonDefinition(
                        bytesSize,
                        message,
                        out var ulongSenderIp,
                        out var senderPort,
                        out var size,
                        out var ptrBoneIds,
                        out var ptrParentBoneIds,
                        out var ptrRotationsX,
                        out var ptrRotationsY,
                        out var ptrRotationsZ,
                        out var ptrRotationsW,
                        out var ptrPositionsX,
                        out var ptrPositionsY,
                        out var ptrPositionsZ
                    ))
                    SetSkeletonDefinition(
                        ULongToIpAddressString(ulongSenderIp),
                        senderPort,
                        PointerToArray<int>(ptrBoneIds, size),
                        PointerToArray<int>(ptrParentBoneIds, size),
                        PointerToArray<float>(ptrRotationsX, size),
                        PointerToArray<float>(ptrRotationsY, size),
                        PointerToArray<float>(ptrRotationsZ, size),
                        PointerToArray<float>(ptrRotationsW, size),
                        PointerToArray<float>(ptrPositionsX, size),
                        PointerToArray<float>(ptrPositionsY, size),
                        PointerToArray<float>(ptrPositionsZ, size)
                    );
            }
            else if (SonyMotionFormat.IsFrameDataBytes(bytesSize, message)) {
                if (SonyMotionFormat.ConvertBytesToFrameData(
                        bytesSize,
                        message,
                        out var ulongSenderIp,
                        out var senderPort,
                        out var frameid,
                        out var timestamp,
                        out var unixTime,
                        out var size,
                        out var ptrBoneIds,
                        out var ptrRotationsX,
                        out var ptrRotationsY,
                        out var ptrRotationsZ,
                        out var ptrRotationsW,
                        out var ptrPositionsX,
                        out var ptrPositionsY,
                        out var ptrPositionsZ
                    ))
                    SetSkeletonData(
                        ULongToIpAddressString(ulongSenderIp),
                        senderPort,
                        frameid,
                        timestamp,
                        unixTime,
                        PointerToArray<int>(ptrBoneIds, size),
                        PointerToArray<float>(ptrRotationsX, size),
                        PointerToArray<float>(ptrRotationsY, size),
                        PointerToArray<float>(ptrRotationsZ, size),
                        PointerToArray<float>(ptrRotationsW, size),
                        PointerToArray<float>(ptrPositionsX, size),
                        PointerToArray<float>(ptrPositionsY, size),
                        PointerToArray<float>(ptrPositionsZ, size)
                    );
            }
        }

        /// <summary>
        ///     Set skeleton data for bone initialization
        /// </summary>
        /// <param name="senderIp">Sender IP address</param>
        /// <param name="senderPort">Sender port number</param>
        /// <param name="boneIds">Id of bones</param>
        /// <param name="parentBoneIds">Id of parent bones</param>
        /// <param name="rotationsX">rotations in the X direction</param>
        /// <param name="rotationsY">rotations in the Y direction</param>
        /// <param name="rotationsZ">rotations in the Z direction</param>
        /// <param name="rotationsW">rotations in the W direction</param>
        /// <param name="positionsX">X coordinate of position</param>
        /// <param name="positionsY">Y coordinate of position</param>
        /// <param name="positionsZ">Z coordinate of position</param>
        private void SetSkeletonDefinition(
            string senderIp, int senderPort,
            int[] boneIds, int[] parentBoneIds,
            float[] rotationsX, float[] rotationsY, float[] rotationsZ, float[] rotationsW,
            float[] positionsX, float[] positionsY, float[] positionsZ
        ) {
            OnReceiveSkeletonDefinition?.Invoke(
                boneIds, parentBoneIds,
                rotationsX, rotationsY, rotationsZ, rotationsW,
                positionsX, positionsY, positionsZ
            );
        }

        /// <summary>
        ///     Set skeleton data for achieving bone movement
        /// </summary>
        /// <param name="senderIp">Sender IP address</param>
        /// <param name="senderPort">Sender port number</param>
        /// <param name="frameid">Frame Id</param>
        /// <param name="timestamp">Timestamp</param>
        /// <param name="unixTime">Unix time when sensor sent data</param>
        /// <param name="boneIds">Id of bones</param>
        /// <param name="rotationsX">rotations in the X direction</param>
        /// <param name="rotationsY">rotations in the Y direction</param>
        /// <param name="rotationsZ">rotations in the Z direction</param>
        /// <param name="rotationsW">rotations in the W direction</param>
        /// <param name="positionsX">X coordinate of position</param>
        /// <param name="positionsY">Y coordinate of position</param>
        /// <param name="positionsZ">Z coordinate of position</param>
        private void SetSkeletonData(
            string senderIp, int senderPort,
            int frameid, float timestamp, double unixTime,
            int[] boneIds,
            float[] rotationsX, float[] rotationsY, float[] rotationsZ, float[] rotationsW,
            float[] positionsX, float[] positionsY, float[] positionsZ
        ) {
            OnReceiveFrameData?.Invoke(
                frameid, timestamp, unixTime,
                boneIds,
                rotationsX, rotationsY, rotationsZ, rotationsW,
                positionsX, positionsY, positionsZ
            );
        }

        #endregion --Methods--
    }
}