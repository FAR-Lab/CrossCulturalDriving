/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */


using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections;


public class VehicleInputControllerNetworked : NetworkBehaviour {
    public Transform CameraPosition;


    private VehicleController controller;


    public bool useKeyBoard;


    public Transform[] Left;
    public Transform[] Right;
    public Transform[] BrakeLightObjects;
    public Material baseMaterial;

    private Material materialOn;
    private Material materialOff;
    private Material materialBrake;


    public Color BrakeColor;
    public Color On;
    public Color Off;
    private AudioSource HonkSound;
    public float SteeringInput;
    public float ThrottleInput;

    [HideInInspector] public float selfAlignmentTorque;
    private ulong CLID;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsServer) { controller = GetComponent<VehicleController>(); }
    }

    private void Start() {
        //Generating a new material for on/off;
        materialOn = new Material(baseMaterial);
        materialOn.SetColor("_Color", On);
        materialOff = new Material(baseMaterial);
        materialOff.SetColor("_Color", Off);
        materialBrake = new Material(baseMaterial);
        materialBrake.SetColor("_Color", BrakeColor);


        HonkSound = GetComponent<AudioSource>();

        foreach (Transform t in Left) { t.GetComponent<MeshRenderer>().material = materialOff; }

        foreach (Transform t in Right) { t.GetComponent<MeshRenderer>().material = materialOff; }

        foreach (Transform t in BrakeLightObjects) { t.GetComponent<MeshRenderer>().material = materialOff; }
    }


    [ClientRpc]
    public void TurnOnLeftClientRpc(bool Leftl_) {
        if (Leftl_) {
            foreach (Transform t in Left) { t.GetComponent<MeshRenderer>().material = materialOn; }
        }
        else {
            foreach (Transform t in Left) { t.GetComponent<MeshRenderer>().material = materialOff; }
        }
    }

    [ClientRpc]
    public void TurnOnRightClientRpc(bool Rightl_) {
        if (Rightl_) {
            foreach (Transform t in Right) { t.GetComponent<MeshRenderer>().material = materialOn; }
        }
        else {
            foreach (Transform t in Right) { t.GetComponent<MeshRenderer>().material = materialOff; }
        }
    }

    [ClientRpc]
    public void TurnOnBrakeLightClientRpc(bool Active) {
        if (Active) {
            foreach (Transform t in BrakeLightObjects) { t.GetComponent<MeshRenderer>().material = materialBrake; }
        }
        else {
            foreach (Transform t in BrakeLightObjects) { t.GetComponent<MeshRenderer>().material = materialOff; }
        }
    }


    [ClientRpc]
    public void HonkMyCarClientRpc() {
        Debug.Log("HonkMyCarClientRpc");
        HonkSound.Play();
    }


    [ClientRpc]
    public void SetGPSClientRpc(GpsController.Direction[] dir) {
        // GetComponentInChildren<GpsController>().SetDirection(dir[SceneStateManager.Instance.getParticipantID()]);
    }

    private void LateUpdate() {
        if (IsServer && controller != null) {
            controller.steerInput = SteeringInput;
            controller.accellInput = ThrottleInput;
        }
    }

    private void OnGUI() {
        GUI.Box(new Rect(200, 100, 300, 30), "IsServer: " + IsServer.ToString() + "  IsHost: " + IsHost.ToString() +
                                             "  IsClient: " +
                                             IsClient.ToString());
    }

    void Update() { }

    public void AssignClient(ulong CLID_) {
        if (IsServer) {
            NetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            CLID = CLID_;
            SetPlayerParent(CLID_);
        }
        else { Debug.LogWarning("Tried to execute something that should never happen. "); }
    }


    private void SetPlayerParent(ulong clientId) {
        if (IsSpawned && IsServer) {
            // As long as the client (player) is in the connected clients list
            if (NetworkManager.ConnectedClients.ContainsKey(clientId)) {
                // Set the player as a child of this in-scene placed NetworkObject 
                NetworkManager.ConnectedClients[clientId].PlayerObject.transform.parent =
                    transform; // Should be Camera position but this doesnt work cause of NetworkObject restrictions
                NetworkManager.SceneManager.OnSceneEvent -= SceneManager_OnSceneEvent;
                
            }
        }
    }

    private void SceneManager_OnSceneEvent(SceneEvent sceneEvent) {
        Debug.Log("SceneManager_OnSceneEvent called with event:" + sceneEvent.SceneEventType.ToString());
        switch (sceneEvent.SceneEventType) {
            case SceneEventType.SynchronizeComplete: {
                Debug.Log("Scene event change by Client: " + sceneEvent.ClientId);
                if (sceneEvent.ClientId == CLID) {
                    Debug.Log("Server: " + IsServer.ToString() + "  IsClient: " + IsClient.ToString() +
                              "  IsHost: " + IsHost.ToString());
                    SetPlayerParent(sceneEvent.ClientId);
                }

                break;
            }
        }
    }
}