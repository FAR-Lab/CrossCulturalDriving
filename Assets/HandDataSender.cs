using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Oculus.Platform;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
//using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

public class HandDataSender : NetworkBehaviour {
    private FastBufferWriter _fastBufferWriter;
    private OVRPlugin.TrackingConfidence HandConfidence;

    private OVRPlugin.HandState _handState;

    public GameObject RightHandReader_Prefab;
    public GameObject LeftHandReader_Prefab;


    HandDataStreamerReader leftHand = null;
    HandDataStreamerReader rightHand = null;
    public static string BroadcastHandMessageName = "BroadcastHandMessage";

    public static string ServerHandMessageName = "ServerHandMessage";
    // Start is called before the first frame update

    void Start() { }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (!this.enabled) return;
        if (IsLocalPlayer) { Debug.Log("Nothing to register I am sending!"); }
        else if (!IsServer) {
            Debug.Log("Registering hand callback on a client!");
            leftHand = Instantiate(LeftHandReader_Prefab, transform)
                .GetComponent<HandDataStreamerReader>();
            rightHand = Instantiate(RightHandReader_Prefab, transform)
                .GetComponent<HandDataStreamerReader>();


            Debug.Log("Client Registering CustomMessage Recieve for:" + GETMessageNameBroadcast());
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                GETMessageNameBroadcast(), ClientReceivingHandData);
        }
        else if (IsServer) {
            Debug.Log("Server Registering CustomMessage Recieve for:" + GETMessageNameServer());
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                GETMessageNameServer(), ServerReceivingHandData);
        }
    }

    public string GETMessageNameBroadcast() { return BroadcastHandMessageName + OwnerClientId; }
    public string GETMessageNameServer() { return ServerHandMessageName + OwnerClientId; }

    public void ClientReceivingHandData(ulong senderClientId, FastBufferReader messagePayload) {
        //Debug.Log("Client got HandMessage from: " + senderClientId.ToString());
        messagePayload.ReadNetworkSerializable<NetworkSkeletonPoseData>(
            out NetworkSkeletonPoseData newRemoteHandData);
        if (senderClientId == NetworkManager.Singleton.LocalClientId) return;
        if (leftHand == null || rightHand == null) return;
        if (newRemoteHandData.HandType == OVRPlugin.Hand.HandLeft) { leftHand.GetNewData(newRemoteHandData); }
        else if (newRemoteHandData.HandType == OVRPlugin.Hand.HandRight) { rightHand.GetNewData(newRemoteHandData); }
    }

    public void ServerReceivingHandData(ulong senderClientId, FastBufferReader messagePayload) {
        if (!IsServer) return;
       // Debug.Log("Bouncing Hand Data for:" + senderClientId.ToString());
        messagePayload.ReadNetworkSerializable<NetworkSkeletonPoseData>(
            out NetworkSkeletonPoseData newRemoteHandData);
       // HandDataStreamRecorder.Singleton.StoreHandData(senderClientId, newRemoteHandData);
        
            
      
        
        _fastBufferWriter = new FastBufferWriter(NetworkSkeletonPoseData.GetSize(), Allocator.Temp);
        _fastBufferWriter.WriteNetworkSerializable(newRemoteHandData);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(GETMessageNameBroadcast(),  // optimization option dont send to all
            _fastBufferWriter, NetworkDelivery.UnreliableSequenced);
        _fastBufferWriter.Dispose();
    }

    

    // Update is called once per frame
    void Update() {
        if (IsLocalPlayer) { GetHandState(OVRPlugin.Step.Render); }
    }

    public void GetHandState(OVRPlugin.Step step) {
        OVRPlugin.Hand[] temp = new OVRPlugin.Hand[] {
            OVRPlugin.Hand.HandLeft, OVRPlugin.Hand.HandRight
        };
        foreach (OVRPlugin.Hand HandType in temp) {
            if (OVRPlugin.GetHandState(step, HandType, ref _handState)) {
                HandConfidence = (OVRPlugin.TrackingConfidence) _handState.HandConfidence;
                NetworkSkeletonPoseData networkSkeletonPoseData = new NetworkSkeletonPoseData(
                    _handState.RootPose.Position.FromVector3f(),
                    _handState.RootPose.Orientation.FromQuatf(),
                    _handState.HandScale,
                    Array.ConvertAll(_handState.BoneRotations, s => s.FromQuatf()),
                    HandType
                );

                if ((_handState.Status & OVRPlugin.HandStatus.HandTracked) != 0 &&
                    HandConfidence == OVRPlugin.TrackingConfidence.High) { BoradCastHandData(networkSkeletonPoseData); }
            }
        }
    }


    public void BoradCastHandData(NetworkSkeletonPoseData newPose) {
        _fastBufferWriter = new FastBufferWriter(NetworkSkeletonPoseData.GetSize(), Allocator.Temp);
        _fastBufferWriter.WriteNetworkSerializable(newPose);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
            GETMessageNameServer(),
            NetworkManager.Singleton.ServerClientId,
            _fastBufferWriter,
            NetworkDelivery.UnreliableSequenced);
      //  Debug.Log("Send message to:" + GETMessageNameServer());
        //  HandDataStreamRecorder.Singleton.ForwardedHandData(clientID,newPose);// server only hand display
        _fastBufferWriter.Dispose();
    }
}