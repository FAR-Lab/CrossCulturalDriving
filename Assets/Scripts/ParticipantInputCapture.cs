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
    private StateManager _localStateManager;
    public bool ReadyForAssignment = false;

    private NetworkVehicleController NetworkedVehicle;

    public NetworkVariable<GpsController.Direction> CurrentDirection =
        new NetworkVariable<GpsController.Direction>(NetworkVariableReadPermission.Everyone);

    private GpsController m_GpsController;
    
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
        _localStateManager = GetComponent<StateManager>();
    }


    


    [ServerRpc]
    public void PostQuestionServerRPC(ulong clientID) { ConnectionAndSpawing.Singleton.FinishedQuestionair(clientID); }

    [ClientRpc]
    public void StartQuestionnaireClientRpc() {
        if (IsLocalPlayer) {
            FindObjectOfType<ScenarioManager>().RunQuestionairNow(transform); 
        }
    }

    [ClientRpc]
    public void SetGPSClientRpc(GpsController.Direction[] dir) {
        // GetComponentInChildren<GpsController>().SetDirection(dir[SceneStateManager.Instance.getParticipantID()]);
    }

    private void OnGUI() {
        if (IsLocalPlayer)
            GUI.Label(new Rect(200, 5, 150, 100), "Client State" + _localStateManager.GlobalState.Value);
    }

    public void AssignCarTransform(NetworkVehicleController MyCar,ClientRpcParams  clientRpcParams) {
        if (IsServer) {
            NetworkedVehicle = MyCar;
            AssignCarTransformClientRPC(MyCar.NetworkObject,clientRpcParams);
        }
    }
    
    [ClientRpc]
    private void AssignCarTransformClientRPC(NetworkObjectReference MyCar, ClientRpcParams clientRpcParams = default) {
       
        //GetComponent<HandDataSender>().SetOrder(participantOrder_);
       
        if (MyCar.TryGet(out NetworkObject targetObject)) {
            NetworkedVehicle = targetObject.transform.GetComponent<NetworkVehicleController>();

            _transform = NetworkedVehicle.transform.Find("CameraPosition");
        }
        else {
            Debug.LogWarning(
                "Did not manage to get my Car assigned interactions will not work. Maybe try calling this RPC later.");
        }
    }

   
    public void De_AssignCarTransform(ClientRpcParams  clientRpcParams) {
        if (IsServer) {
            NetworkedVehicle = null;
            De_AssignCarTransformClientRPC(clientRpcParams);
        }
    }

    [ClientRpc]
    private void De_AssignCarTransformClientRPC(ClientRpcParams clientRpcParams = default) {
        NetworkedVehicle = null;
        _transform = null;
        DontDestroyOnLoad(gameObject);
        Debug.Log("De_assign Car ClientRPC");
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

   

  
}