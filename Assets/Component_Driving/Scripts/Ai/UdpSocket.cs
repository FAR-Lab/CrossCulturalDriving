/*
Created by Youssef Elashry to allow two-way communication between Python3 and Unity to send and receive strings

Feel free to use this in your individual or commercial projects BUT make sure to reference me as: Two-way communication between Python 3 and Unity (C#) - Y. T. Elashry
It would be appreciated if you send me how you have used this in your projects (e.g. Machine Learning) at youssef.elashry@gmail.com

Use at your own risk
Use under the Apache License 2.0

Modified by:
Youssef Elashry 12/2020 (replaced obsolete functions and improved further - works with Python as well)
Based on older work by Sandra Fang 2016 - Unity3D to MATLAB UDP communication - [url]http://msdn.microsoft.com/de-de/library/bb979228.aspx#ID0E3BAC[/url]
*/

using UnityEngine;
using System.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UdpSocket : MonoBehaviour
{
    bool isRunning = false;

    [SerializeField] string IP = "127.0.0.1"; // local host
    [SerializeField] int rxPort = 8000; // port to receive data from Python on
    [SerializeField] int txPort = 8001; // port to send data to Python on


    private UdpClient txClient;
    private UdpClient rxClient;

    Thread sendingThread; // Receiving Thread
    Thread receiveThread; // Receiving Thread


    static public int sendFloatArrayLength = 8;
    static public int expectedFloatReturnLength = 2;
    private ConcurrentQueue<float[]> recievedQueue = new ConcurrentQueue<float[]>();
    private ConcurrentQueue<float[]> sendQueue = new ConcurrentQueue<float[]>();


    private void SendData() // Use to send data to Python
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(IP), txPort);


        float[] dat = new float[sendFloatArrayLength];

        while (isRunning)
        {
            while (sendQueue.TryDequeue(out dat))
            {
                var byteArray = new byte[dat.Length * 4];
                Buffer.BlockCopy(dat, 0, byteArray, 0, byteArray.Length);
                txClient.Send(byteArray, byteArray.Length, remoteEndPoint);
            }

            Thread.Sleep(100);
        }

        txClient.Close();
    }

    

    private void ReceiveData()
    {
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);

        while (isRunning)
        {
            if (rxClient.Available > 0)
            {
                byte[] data = rxClient.Receive(ref anyIP);
                if (data.Length == expectedFloatReturnLength * 4)
                {
                    var floatArray2 = new float[data.Length / 4];
                    Buffer.BlockCopy(data, 0, floatArray2, 0, data.Length);
                    recievedQueue.Enqueue(floatArray2);
                }
            }
            Thread.Sleep(100);
        }

        rxClient.Close();
    }

    void Start()
    {
        txClient = new UdpClient(); //tx port used by python thread
        rxClient = new UdpClient(rxPort);
        isRunning = true;
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();

        sendingThread = new Thread(SendData);
        sendingThread.IsBackground = true;
        sendingThread.Start();
        Debug.Log(":Started sending data");
        StartCoroutine(ProcessRecievedData());
    }

    public delegate void GotNewAIData_delegate(float[] data);

    public GotNewAIData_delegate GotNewAiData;

    private IEnumerator ProcessRecievedData()
    {
        while (isRunning)
        {
            if (recievedQueue.TryDequeue(out float[] data))
            {
                GotNewAiData.Invoke(data);
            }

            yield return null;
        }
    }

    public void SendDataToPython(float[] data)
    {
        sendQueue.Enqueue(data);
    }


    private void stopAll()
    {
        if (isRunning)
        {
            isRunning = false;
            Debug.Log("Attempting to shutdown!");
            if (receiveThread != null)
            {
                receiveThread.Join();
                receiveThread.Abort();
                Debug.Log($"receiveThread is {receiveThread.IsAlive}");
            }

            if (sendingThread != null)
            {
                sendingThread.Join();
                sendingThread.Abort();
                Debug.Log($"sendingThread is {sendingThread.IsAlive}");
            }

            txClient.Close();
            rxClient.Close();
            
        }
    }
    void OnDisable()
    {
        Debug.Log("OnDisable");
        stopAll();
    }

    private void OnApplicationQuit(){
    Debug.Log("OnApplicationQuit");
        stopAll();
    }

    private void OnDestroy()
    {
        Debug.Log("OnDestroy");
        stopAll();
    }
}