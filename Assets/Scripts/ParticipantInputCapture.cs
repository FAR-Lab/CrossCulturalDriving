/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
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

    public NetworkVariable<GpsController.Direction> CurrentDirection =
        new NetworkVariable<GpsController.Direction>(NetworkVariableReadPermission.Everyone);

    private GpsController m_GpsController;

    private FastBufferWriter _fastBufferWriter;

    public ParticipantOrder participantOrder { private set; get; }
    public Transform _transform;
    public LanguageSelect lang { private set; get; }
    void Awake() { ReadyForAssignment = false; }

    public static ParticipantInputCapture GetMyPIC() {
        foreach (ParticipantInputCapture pic in FindObjectsOfType<ParticipantInputCapture>()) {
            if (pic.IsLocalPlayer) { return pic; }
        }

        return null;
    }

    private void Start() { }

    private void NewGpsDirection(GpsController.Direction previousvalue, GpsController.Direction newvalue) {
        if (m_GpsController != null) { m_GpsController.SetDirection(newvalue); }
    }


    public override void OnNetworkSpawn() {
       
        CurrentDirection.OnValueChanged += NewGpsDirection;
        localStateManager = GetComponent<StateManager>();
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
    public void AssignCarTransformClientRPC(NetworkObjectReference MyCar, ParticipantOrder participantOrder_,
        LanguageSelect lang_, ClientRpcParams clientRpcParams = default) {
        participantOrder = participantOrder_;
        lang = lang_;
        if (MyCar.TryGet(out NetworkObject targetObject)) {
            NetworkedVehicle = targetObject.transform.GetComponent<NetworkVehicleController>();

            _transform = NetworkedVehicle.transform.Find("CameraPosition");
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
        if (_transform != null) {
            var transform1 = transform;
            var transform2 = _transform;
            transform1.position = transform2.position;
            transform1.rotation = transform2.rotation;
        }
    }

    void Update() {
        if (IsLocalPlayer) {
            if (m_GpsController == null && _transform!=null) {
                m_GpsController = _transform.parent.GetComponentInChildren<GpsController>();
                if (m_GpsController != null) { m_GpsController.SetDirection(CurrentDirection.Value); }
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
            HandDataStreamerWriter.HandMessageName,
            clientIds,
            _fastBufferWriter,
            NetworkDelivery.UnreliableSequenced);
        Debug.Log("bounced Hand a message");
        _fastBufferWriter.Dispose();
    }

  
}