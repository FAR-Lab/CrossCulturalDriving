using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using sl;
using UnityEngine;

public class ZEDStreamingClient : MonoBehaviour {
    public delegate void onNewDetectionTriggerDelegate(Bodies bodies);

    public bool useMulticast = true;

    public int port = 20000;
    public string multicastIpAddress = "230.0.0.1";

    public bool showZEDFusionMetrics;
    private AsyncCallback AC;

    private readonly int bufferSize = 10;
    private UdpClient clientData;
    private DetectionData data;

    private IPEndPoint ipEndPointData;

    private bool newDataAvailable;

    private readonly object obj = null;
    private byte[] receivedBytes;
    private LinkedList<byte[]> receivedDataBuffer;

    private void Start() {
        InitializeUDPListener();
    }


    private void Update() {
        if (IsNewDataAvailable()) {
            OnNewDetection(GetLastBodiesData());
            newDataAvailable = false;

            if (ShowFusionMetrics()) {
                var metrics = GetLastFusionMetrics();
                var tmpdbg = "";
                foreach (var camera in metrics.camera_individual_stats)
                    tmpdbg += "SN : " + camera.sn + " Synced Latency: " + camera.synced_latency + "FPS : " +
                              camera.received_fps + "\n";
                Debug.Log(tmpdbg);
            }
        }
    }

    private void OnDestroy() {
        if (clientData != null) {
            Debug.Log("Stop receiving ..");
            clientData.Close();
        }
    }

    public event onNewDetectionTriggerDelegate OnNewDetection;

    public void InitializeUDPListener() {
        receivedDataBuffer = new LinkedList<byte[]>();

        ipEndPointData = new IPEndPoint(IPAddress.Any, port);
        clientData = new UdpClient();
        clientData.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        clientData.ExclusiveAddressUse = false;
        clientData.EnableBroadcast = false;

        if (useMulticast) clientData.JoinMulticastGroup(IPAddress.Parse(multicastIpAddress));
        clientData.Client.Bind(ipEndPointData);

        clientData.DontFragment = true;
        AC = ReceivedUDPPacket;
        clientData.BeginReceive(AC, obj);
        Debug.Log("UDP - Start Receiving..");
    }

    private void ReceivedUDPPacket(IAsyncResult result) {
        //stopwatch.Start();
        receivedBytes = clientData.EndReceive(result, ref ipEndPointData);
        ParsePacket();
        clientData.BeginReceive(AC, obj);
    } // ReceiveCallBack

    private void ParsePacket() {
        if (receivedDataBuffer.Count == bufferSize) receivedDataBuffer.RemoveFirst();
        receivedDataBuffer.AddLast(receivedBytes);
        newDataAvailable = true;
    }

    public bool IsNewDataAvailable() {
        return newDataAvailable;
    }

    public Bodies GetLastBodiesData() {
        data = DetectionData.CreateFromJSON(receivedDataBuffer.Last.Value);

        return data.bodies;
    }

    public FusionMetrics GetLastFusionMetrics() {
        return data.fusionMetrics;
    }

    public bool ShowFusionMetrics() {
        return showZEDFusionMetrics && data.fusionMetrics != null;
    }
}