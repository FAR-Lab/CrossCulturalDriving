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


public class NetworkVehicleController : NetworkBehaviour {
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


    private ulong CLID;



    /// <summary>
    /// SoundRelevant Variables
    /// </summary>

    public NetworkVariable<bool> IsShifting;

    public NetworkVariable<float> accellInput;
    public NetworkVariable<float> RPM;
    public NetworkVariable<float> traction;
    public NetworkVariable<float> MotorWheelsSlip;

    public NetworkVariable<float> CurrentSpeed;
    public NetworkVariable<RoadSurface> CurrentSurface;
    
    void UpdateSounds() {
        if (controller == null) return;
        IsShifting.Value = controller.IsShifting;
        accellInput.Value = controller.accellInput;
        RPM.Value = controller.RPM;
        traction.Value=(controller.traction + controller.tractionR + controller.rtraction + controller.rtractionR)/4.0f;
        MotorWheelsSlip.Value = controller.MotorWheelsSlip;
        CurrentSpeed.Value = controller.CurrentSpeed;
        CurrentSurface.Value = controller.CurrentSurface;

    }


    
    public void StartTheCar() {
        GetComponent<VehicleAudioController>().PlayIgnition();
        StartTheCarClientRpc();

    }

    [ClientRpc]
    public void StartTheCarClientRpc() {
       GetComponent<VehicleAudioController>().PlayIgnition();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (IsServer) { controller = GetComponent<VehicleController>(); }
    }

    private void Start() {
        indicaterStage = 0;
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

    float steeringAngle;
    public Transform SteeringWheel;

    void Update() {
        if (!IsServer) return;
       
        if (ConnectionAndSpawing.Singleton.ServerState == ActionState.DRIVE) {
            if (SteeringWheelManager.Singleton == null || useKeyBoard) {
                SteeringInput = Input.GetAxis("Horizontal");
                ThrottleInput = Input.GetAxis("Vertical");
            }
            else {
                SteeringInput = SteeringWheelManager.Singleton.GetSteerInput(_participantOrder);
                ThrottleInput = SteeringWheelManager.Singleton.GetAccelInput(_participantOrder);
               
                SteeringWheel.RotateAround(SteeringWheel.position, SteeringWheel.up,
                    steeringAngle - SteeringInput * -450f);
                steeringAngle = SteeringInput * -450f;
            }

            bool TempLeft = SteeringWheelManager.Singleton.GetLeftIndicatorInput(_participantOrder); //Input.GetButton("indicateLeft");
            bool TempRight =SteeringWheelManager.Singleton.GetRightIndicatorInput(_participantOrder);// Input.GetButton("indicateRight");
            if (TempLeft || TempRight) {
                DualButtonDebounceIndicator = true;
                if (TempLeft) { LeftIndicatorDebounce = true; }

                if (TempRight) { RightIndicatorDebounce = true; }
            }
            else if (DualButtonDebounceIndicator && !TempLeft && !TempRight) {
                startBlinking(LeftIndicatorDebounce, RightIndicatorDebounce);
                DualButtonDebounceIndicator = false;
                LeftIndicatorDebounce = false;
                RightIndicatorDebounce = false;
            }

            UpdateIndicator();


            if (SteeringWheelManager.Singleton.GetButtonInput(_participantOrder)) { HonkMyCar(); }

            if (ThrottleInput < 0 && !breakIsOn) {
                BrakeLightChangedServerRpc(true);
                breakIsOn = true;
            }
            else if (ThrottleInput >= 0 && breakIsOn) {
                BrakeLightChangedServerRpc(false);
                breakIsOn = false;
            }
        }
        else if (ConnectionAndSpawing.Singleton.ServerState == ActionState.QUESTIONS) {
            SteeringInput = 0;
            ThrottleInput = -1;
        }

        UpdateSounds();
    }

    public void AssignClient(ulong CLID_, ParticipantOrder _participantOrder_) {
        if (IsServer) {
            NetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            CLID = CLID_;
            _participantOrder = _participantOrder_;
            GetComponent<ForceFeedback>()?.Init(transform.GetComponent<Rigidbody>(), _participantOrder);
        }
        else { Debug.LogWarning("Tried to execute something that should never happen. "); }
    }


    private void SceneManager_OnSceneEvent(SceneEvent sceneEvent) {
        Debug.Log("SceneManager_OnSceneEvent called with event:" + sceneEvent.SceneEventType.ToString());
        switch (sceneEvent.SceneEventType) {
            case SceneEventType.SynchronizeComplete: {
                Debug.Log("Scene event change by Client: " + sceneEvent.ClientId);
                if (sceneEvent.ClientId == CLID) {
                    Debug.Log("Server: " + IsServer.ToString() + "  IsClient: " + IsClient.ToString() +
                              "  IsHost: " + IsHost.ToString());
                    //SetPlayerParent(sceneEvent.ClientId);
                }

                break;
            }
            default:
                break;
        }
    }


    private bool breakIsOn;

    [ServerRpc]
    private void BrakeLightChangedServerRpc(bool newvalue) { TurnOnBrakeLightClientRpc(newvalue); }

    
    public void HonkMyCar() {
        
       if(HonkSound.isPlaying){return;}
        HonkSound.Play();
      //  Debug.Log("HonkMyCarServerRpc");
        HonkMyCarClientRpc();
    }

    #region IndicatorLogic

    private bool LeftActive;
    private bool RightActive;


    private bool LeftIsActuallyOn;
    private bool RightIsActuallyOn;
    private bool ActualLightOn;
    private float indicaterTimer;
    public float interval;
    private int indicaterStage;

    private bool DualButtonDebounceIndicator;
    private bool LeftIndicatorDebounce;
    private bool RightIndicatorDebounce;
    private ParticipantOrder _participantOrder;


    void startBlinking(bool left, bool right) {
        indicaterStage = 1;
        if (left == right == true) {
            if (LeftIsActuallyOn != true || RightIsActuallyOn != true) {
                LeftIsActuallyOn = true;
                RightIsActuallyOn = true;
            }
            else if (LeftIsActuallyOn == RightIsActuallyOn == true) {
                RightIsActuallyOn = false;
                LeftIsActuallyOn = false;
            }
        }

        if (left != right) {
            if (LeftIsActuallyOn == RightIsActuallyOn ==
                true) // When we are returning from the hazard lights we make sure that not the inverse thing turns on 
            {
                LeftIsActuallyOn = false;
                RightIsActuallyOn = false;
            }

            if (left) {
                if (!LeftIsActuallyOn) {
                    LeftIsActuallyOn = true;
                    RightIsActuallyOn = false;
                }
                else { LeftIsActuallyOn = false; }
            }

            if (right) {
                if (!RightIsActuallyOn) {
                    LeftIsActuallyOn = false;
                    RightIsActuallyOn = true;
                }
                else { RightIsActuallyOn = false; }
            }
        }
    }


    void UpdateIndicator() {
        if (indicaterStage == 1) {
            indicaterStage = 2;
            indicaterTimer = interval;
            ActualLightOn = false;
        }
        else if (indicaterStage == 2 || indicaterStage == 3) {
            indicaterTimer += Time.deltaTime;

            if (indicaterTimer > interval) {
                indicaterTimer = 0;
                ActualLightOn = !ActualLightOn;
                if (ActualLightOn) {
                    LeftIndicatorChanged(LeftIsActuallyOn);
                    RightIndicatorChanged(RightIsActuallyOn);
                }
                else {
                    RightIndicatorChanged(false);
                    LeftIndicatorChanged(false);
                }
            }

            if (indicaterStage == 2) {
                if (SteeringWheelManager.Singleton != null &&
                    Mathf.Abs(SteeringWheelManager.Singleton.GetSteerInput(_participantOrder) * -450f) > 90) {
                    indicaterStage = 3;
                }
            }
            else if (indicaterStage == 3) {
                if (SteeringWheelManager.Singleton != null &&
                    Mathf.Abs(SteeringWheelManager.Singleton.GetSteerInput(_participantOrder) * -450f) < 10) {
                    indicaterStage = 4;
                }
            }
        }
        else if (indicaterStage == 4) {
            indicaterStage = 0;
            ActualLightOn = false;
            LeftIsActuallyOn = false;
            RightIsActuallyOn = false;
            RightIndicatorChanged(false);
            LeftIndicatorChanged(false);
            // UpdateIndicatorLightsServerRpc(false, false);
        }
    }

    private void RightIndicatorChanged(bool newvalue) { TurnOnRightClientRpc(newvalue); }

    private void LeftIndicatorChanged(bool newvalue) { TurnOnLeftClientRpc(newvalue); }

    #endregion
}