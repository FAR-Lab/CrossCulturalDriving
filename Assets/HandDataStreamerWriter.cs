using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

//using  Unity.Netcode.Serialization.Pooled;

//https://www.youtube.com/watch?v=lBzwUKQ3tbw

public class HandDataStreamerWriter : MonoBehaviour {
    public GameObject RightHandReader;
    public GameObject LeftHandReader;

    public GameObject RightHandReader_ReRun;
    public GameObject LeftHandReader_ReRun;

    private OVRPlugin.TrackingConfidence HandConfidence;
    private ParticipantOrder MyOrder = ParticipantOrder.None;

    private FastBufferWriter _fastBufferWriter;

    private Dictionary<ulong, Dictionary<OVRPlugin.Hand, HandDataStreamerReader>> HandClinets =
        new Dictionary<ulong, Dictionary<OVRPlugin.Hand, HandDataStreamerReader>>();

    private OVRPlugin.HandState _handState;

    public static string HandMessageName = "HandMessage";

    private ParticipantInputCapture PIC = null;

    private LocalVRPlayer m_LocalVRPlayer = null;

    // Start is called before the first frame update
    void Start() {
        IEnumerator coroutine = RegisterHandMessageHandler();
        StartCoroutine(coroutine);
    }

    IEnumerator RegisterHandMessageHandler() {
        while (true) {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null) {
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(HandMessageName,
                    RecievingHandData);
                Debug.Log("Registered hand call back!");
                break;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private void RecievingHandData(ulong senderclientid, FastBufferReader messagepayload) {
        Debug.Log("Got a HandMessage from: " + senderclientid.ToString());
        if (senderclientid == NetworkManager.Singleton.LocalClientId) return;


        messagepayload.ReadNetworkSerializable(
            out NetworkSkeletonPoseData newRemoteHandData);

        if (HandClinets.ContainsKey(senderclientid)) {
            HandClinets[senderclientid][newRemoteHandData.HandType].GetNewData(newRemoteHandData);
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
                HandClinets.Add(senderclientid, new Dictionary<OVRPlugin.Hand, HandDataStreamerReader> {
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


    private void FindReferences() {
        if (m_LocalVRPlayer == null) { m_LocalVRPlayer = FindObjectOfType<LocalVRPlayer>(); }

        if (PIC == null && m_LocalVRPlayer != null) { PIC = m_LocalVRPlayer.PIC; }
    }

    // Update is called once per frame
    void Update() {
        FindReferences();
        if (MyOrder == ParticipantOrder.None && m_LocalVRPlayer != null) {
            MyOrder = m_LocalVRPlayer.MyOrder;
        }
        else { if(PIC!=null){GetHandState(OVRPlugin.Step.Render);} }
    }

    private void GetHandState(OVRPlugin.Step step) {
        OVRPlugin.Hand[] temp = new OVRPlugin.Hand[] {
            OVRPlugin.Hand.HandLeft, OVRPlugin.Hand.HandRight
        };
        foreach (OVRPlugin.Hand HandType in temp) {
            if (OVRPlugin.GetHandState(step, HandType, ref _handState)) {
                HandConfidence = (OVRPlugin.TrackingConfidence) _handState.HandConfidence;
                NetworkSkeletonPoseData networkSkeletonPoseData = new NetworkSkeletonPoseData(
                    // MyOrder,
                    _handState.RootPose.Position.FromVector3f(),
                    _handState.RootPose.Orientation.FromQuatf(),
                    _handState.HandScale,
                    Array.ConvertAll(_handState.BoneRotations, s => s.FromQuatf()),
                    HandType
                );

                if ((_handState.Status & OVRPlugin.HandStatus.HandTracked) != 0 &&
                    HandConfidence == OVRPlugin.TrackingConfidence.High) {
                    PIC.BounceHandDataServerRPC(networkSkeletonPoseData);
                }
            }
        }
    }

    
}


public struct NetworkSkeletonPoseData : INetworkSerializable {
    // public ParticipantOrder ThisOrder;
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
        // serializer.SerializeValue(ref ThisOrder);

        serializer.SerializeValue(ref RootPos);
        serializer.SerializeValue(ref RootRot);
        serializer.SerializeValue(ref RootScale);
        int length = 0;

        if (!serializer.IsReader) { length = BoneRotations.Length; }

        serializer.SerializeValue(ref length);
        if (serializer.IsReader || true) { BoneRotations = new Quaternion[length]; }

        for (int n = 0; n < length; ++n) { serializer.SerializeValue(ref BoneRotations[n]); }

        serializer.SerializeValue(ref HandType);
    }
}