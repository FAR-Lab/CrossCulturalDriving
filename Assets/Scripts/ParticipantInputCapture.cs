/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class ParticipantInputCapture : NetworkBehaviour {
    private StateManager localStateManager;
    public bool ReadyForAssignment = false;

    private NetworkVehicleController NetworkedVehicle;
    public NetworkVariable<GpsController.Direction> CurrentDirection =new NetworkVariable<GpsController.Direction>(NetworkVariableReadPermission.Everyone);
    
    private GpsController m_GpsController;
    
    private FastBufferWriter _fastBufferWriter;


    void Awake() { ReadyForAssignment = false; }

    private void Start() {
        CurrentDirection.OnValueChanged += NewGpsDirection;
        localStateManager = GetComponent<StateManager>();
    }

    private void NewGpsDirection(GpsController.Direction previousvalue, GpsController.Direction newvalue) {
        if (m_GpsController != null) { m_GpsController.SetDirection(newvalue); }
    }


    public override void OnNetworkSpawn() {
       
    }
    [ServerRpc]
    public void PostQuestionServerRPC(ulong clientID) { ConnectionAndSpawing.Singleton.FinishedQuestionair(clientID); }

    [ClientRpc]
    public void StartQuestionnaireClientRpc() {
        if (IsLocalPlayer) { FindObjectOfType<ScenarioManager>().RunQuestionairNow(transform); }
    }
    
    [ClientRpc]
    public void SetGPSClientRpc(GpsController.Direction[] dir) {
        // GetComponentInChildren<GpsController>().SetDirection(dir[SceneStateManager.Instance.getParticipantID()]);
    }
    private void OnGUI() {
        if (IsLocalPlayer)
            GUI.Label(new Rect(200, 5, 150, 100), "Client State" + localStateManager.GlobalState.Value);
    }

    [ClientRpc]
    public void AssignCarTransformClientRPC(NetworkObjectReference MyCar, ClientRpcParams clientRpcParams = default) {
       
        if (MyCar.TryGet(out NetworkObject targetObject)) {
            NetworkedVehicle = targetObject.transform.GetComponent<NetworkVehicleController>();
          
        }
        else {
            Debug.LogWarning(
                "Did not manage to get my Car assigned interactions will not work. Maybe try calling this RPC later.");
        }
    }
    
    public void AssignCarTransform_OnServer(NetworkVehicleController MyCar) {
        if (IsServer) { NetworkedVehicle = MyCar; }
    }
   

    private void LateUpdate() {
      
    }

    void Update() {
        if (IsLocalPlayer) {
            if (ReadyForAssignment == false && transform.parent != null) { ReadyForAssignment = true;}

            if (ReadyForAssignment && m_GpsController == null) {
                m_GpsController = transform.parent.GetComponentInChildren<GpsController>();
                if (m_GpsController == null) {
                    m_GpsController.SetDirection(CurrentDirection.Value);
                }
            }
        }
    }

    [ServerRpc(Delivery = RpcDelivery.Unreliable)]
    public void BounceHandDataServerRPC(NetworkSkeletonPoseData newPose ) {
        _fastBufferWriter = new FastBufferWriter(NetworkSkeletonPoseData.GetSize(), Allocator.Temp);
        _fastBufferWriter.WriteNetworkSerializable(newPose);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(HandDataStreamerWriter.HandMessageName,
            _fastBufferWriter,
            NetworkDelivery.UnreliableSequenced);
        Debug.Log("Send a message");
        _fastBufferWriter.Dispose();
    }
}