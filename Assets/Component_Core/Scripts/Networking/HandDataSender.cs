using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
#if USING_OVR

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
        if (IsLocalPlayer) { Debug.Log("Nothing to register I am sending! under the following address"+GETMessageNameServer() ); }
        else if (!IsServer) {
            Debug.Log("Registering hand callback on a client!");
            leftHand = Instantiate(LeftHandReader_Prefab, transform)
                .GetComponent<HandDataStreamerReader>();
            rightHand = Instantiate(RightHandReader_Prefab, transform)
                .GetComponent<HandDataStreamerReader>();


            Debug.Log("Client Registering CustomMessage Receive for: " + GETMessageNameBroadcast());
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                GETMessageNameBroadcast(), ClientReceivingHandData);
        }
        else if (IsServer) {
            Debug.Log("Server Registering CustomMessage Receive for: " + GETMessageNameServer());
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                GETMessageNameServer(), ServerReceivingHandData);
        }
    }

    public string GETMessageNameBroadcast() { return BroadcastHandMessageName + OwnerClientId; }
    public string GETMessageNameServer() { return ServerHandMessageName + OwnerClientId; }

    public void ClientReceivingHandData(ulong senderClientId, FastBufferReader messagePayload) {
        messagePayload.ReadNetworkSerializable<NetworkSkeletonPoseData>(
            out NetworkSkeletonPoseData newRemoteHandData);

        if (leftHand == null || rightHand == null) return;

        if (newRemoteHandData.HandType == OVRPlugin.Hand.HandRight) { rightHand.GetNewData(newRemoteHandData); }

        if (newRemoteHandData.HandType == OVRPlugin.Hand.HandLeft) { leftHand.GetNewData(newRemoteHandData); }
    }

    public void ServerReceivingHandData(ulong senderClientId, FastBufferReader messagePayload) {
        if (!IsServer) return;
        //Debug.Log("Bouncing Hand Data for:" + senderClientId.ToString());
        messagePayload.ReadNetworkSerializable<NetworkSkeletonPoseData>(
            out NetworkSkeletonPoseData newRemoteHandData);
        HandDataStreamRecorder.Singleton.StoreHandData(senderClientId, newRemoteHandData);

        // Debug.Log("GotMessage from Client About to bounce it out on: "+GETMessageNameBroadcast());


        _fastBufferWriter = new FastBufferWriter(NetworkSkeletonPoseData.GetSize(), Allocator.Temp);
        _fastBufferWriter.WriteNetworkSerializable(newRemoteHandData);

        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(
            GETMessageNameBroadcast(), // optimization option dont send to all
            _fastBufferWriter, NetworkDelivery.UnreliableSequenced);
        _fastBufferWriter.Dispose();
    }


    // Update is called once per frame
    void Update() {
        if (IsLocalPlayer) { GetHandState(OVRPlugin.Step.Render); }
    }

    public void GetHandState(OVRPlugin.Step step) {
       // OVRPlugin.Hand[] temp = new OVRPlugin.Hand[] {
      //      OVRPlugin.Hand.HandLeft, OVRPlugin.Hand.HandRight
      //  };


        OVRPlugin.Hand[] temp =  {
             OVRPlugin.Hand.HandRight,OVRPlugin.Hand.HandLeft
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
      //  Debug.Log("Sending HandData" + OwnerClientId);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
            GETMessageNameServer(),
            NetworkManager.ServerClientId,
            _fastBufferWriter
            );

        _fastBufferWriter.Dispose();
    }
}
#endif