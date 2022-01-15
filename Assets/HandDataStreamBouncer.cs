using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

//using  Unity.Netcode.Serialization.Pooled;

//https://www.youtube.com/watch?v=lBzwUKQ3tbw

public class HandDataStreamBouncer : MonoBehaviour {
    public GameObject RightHandReader;
    public GameObject LeftHandReader;

    public GameObject RightHandReader_ReRun;
    public GameObject LeftHandReader_ReRun;






    private Dictionary<ulong, Dictionary<OVRPlugin.Hand, HandDataStreamerReader>> HandClinets =
        new Dictionary<ulong, Dictionary<OVRPlugin.Hand, HandDataStreamerReader>>();

    public static string HandMessageName = "HandMessage";

    //private bool Ready = false;


    // Start is called before the first frame update
    void Start() { }





    public void ReceivingHandData(ulong senderClientId, FastBufferReader messagePayload) {
        Debug.Log("Got a HandMessage from: " + senderClientId.ToString());
        if (senderClientId == NetworkManager.Singleton.LocalClientId) return;


        messagePayload.ReadNetworkSerializable(
            out NetworkSkeletonPoseData newRemoteHandData);
        RecievedHandData(senderClientId, newRemoteHandData);
    }

public void RecievedHandData(ulong senderClientId, NetworkSkeletonPoseData newRemoteHandData){

if (HandClinets.ContainsKey(senderClientId)) {
            HandClinets[senderClientId][newRemoteHandData.HandType].GetNewData(newRemoteHandData);
        }
        else {
            HandDataStreamerReader leftHand = null;
            HandDataStreamerReader rightHand = null;

            if (NetworkManager.Singleton.IsServer) {
                leftHand = Instantiate(LeftHandReader_ReRun, transform)
                    .GetComponent<HandDataStreamerReader>();
                rightHand = Instantiate(RightHandReader_ReRun, transform)
                    .GetComponent<HandDataStreamerReader>();
            }
            else {
                leftHand = Instantiate(LeftHandReader, transform).GetComponent<HandDataStreamerReader>();
                rightHand = Instantiate(RightHandReader, transform).GetComponent<HandDataStreamerReader>();
            }

            if (leftHand != null && rightHand != null) {
                HandClinets.Add(senderClientId, new Dictionary<OVRPlugin.Hand, HandDataStreamerReader> {
                        {OVRPlugin.Hand.HandLeft, leftHand},
                        {OVRPlugin.Hand.HandRight, rightHand}
                    }
                );
            }
            else {
                Debug.LogWarning(
                    "Something with instantiateing the hands failed. This is not a safe condition. You should stop.");
            }
        }
    }


    // Update is called once per frame
    void Update() {
       
      
    }

   
}

public struct NetworkSkeletonPoseData : INetworkSerializable {
    public OVRPlugin.Hand HandType;
    public Vector3 RootPos;
    public Quaternion RootRot;
    public float RootScale;
    public Quaternion[] BoneRotations;

    public static int GetSize() {
        //ToDO  Only way for now to write it as safe code. Could be improved to actually reference Vector3 and quaternions etc.
        int size = //sizeof(ParticipantOrder) + //1
            2 * sizeof(float) + // OVRPlugin.Hand  / should 
            3 * sizeof(float) +
            4 * sizeof(float) +
            sizeof(float) +
            24 * 4 * sizeof(float);
        return size; /// This is strange
    }

    public NetworkSkeletonPoseData(Vector3 RootPos_, Quaternion RootRot_,
        float RootScale_, //ParticipantOrder ThisOrder_,
        Quaternion[] BoneRotations_, OVRPlugin.Hand HandType_) {
        // ThisOrder = ThisOrder_;
        RootPos = RootPos_;
        RootRot = RootRot_;
        RootScale = RootScale_;
        BoneRotations = new Quaternion[BoneRotations_.Length];
        Array.Copy(BoneRotations_, BoneRotations, BoneRotations_.Length);
        HandType = HandType_;
    }


    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
       

        serializer.SerializeValue(ref RootPos);
        serializer.SerializeValue(ref RootRot);
        serializer.SerializeValue(ref RootScale);
        int length=0;

        if (!serializer.IsReader) { length = BoneRotations.Length; }

        serializer.SerializeValue(ref length);
        if (serializer.IsReader) { BoneRotations = new Quaternion[length]; }
        for (int n = 0; n < length; ++n) { serializer.SerializeValue(ref BoneRotations[n]); }
        serializer.SerializeValue(ref HandType);
    }
}