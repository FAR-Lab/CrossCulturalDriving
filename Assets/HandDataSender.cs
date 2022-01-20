using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
//using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

public class HandDataSender : NetworkBehaviour {
    private HandDataStreamBouncer bouncer;
    private FastBufferWriter _fastBufferWriter;
    private OVRPlugin.TrackingConfidence HandConfidence;
    private ParticipantOrder MyOrder = ParticipantOrder.None;
    private OVRPlugin.HandState _handState;
    // Start is called before the first frame update
    void Start() {
        this.enabled = false; //TODO remove after debugging

    }
    
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (IsLocalPlayer) {
            Debug.Log("Registering the Hand data bouncer");
            bouncer = FindObjectOfType<HandDataStreamBouncer>();
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                HandDataStreamBouncer.HandMessageName, bouncer.ReceivingHandData);
        }
        else {
            this.enabled = false;
            Debug.Log("This is not my sender or bouncer so I dont need it.");
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (IsLocalPlayer && ! IsServer && bouncer != null) {
            GetHandState(OVRPlugin.Step.Render);
        }
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
                    HandConfidence == OVRPlugin.TrackingConfidence.High) {
                    BounceHandDataServerRPC(networkSkeletonPoseData, NetworkManager.Singleton.LocalClientId);
                }
            }
        }
    }


    [ServerRpc(Delivery = RpcDelivery.Unreliable)]
    public void BounceHandDataServerRPC(NetworkSkeletonPoseData newPose, ulong clinetID) {
        if (!IsServer) return;
        
        List<ulong> clientIds = ConnectionAndSpawing.Singleton.GetClientList();
        if (clientIds.Contains(clinetID)) { clientIds.Remove(clinetID); }
        else {
            Debug.LogError(
                "CurrentClinet not in active client list, things are getting inconsistent." +
                "Consider reqriting ConnectionAndSpawing class.");
        }

        _fastBufferWriter = new FastBufferWriter(NetworkSkeletonPoseData.GetSize(), Allocator.Temp);
        _fastBufferWriter.WriteNetworkSerializable(newPose);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
            HandDataStreamBouncer.HandMessageName,
            clientIds,
            _fastBufferWriter,
            NetworkDelivery.UnreliableSequenced); //  bouncing it to the clients
        bouncer.RecievedHandData(clinetID, newPose); // server only hand display
        Debug.Log("Bounced Hand a message");
        _fastBufferWriter.Dispose();
    }

    public void SetOrder(ParticipantOrder participantOrder) { MyOrder = participantOrder; }
}
