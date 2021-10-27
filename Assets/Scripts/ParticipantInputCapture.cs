/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */


using System;
using Unity.Netcode;
using Unity.Netcode.Components;
//using  Unity.Netcode.Messaging;
using UnityEngine;
using System.Collections;
//using Unity.Transforms;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class ParticipantInputCapture : NetworkBehaviour {
    private VehicleInputControllerNetworked NetworkedVehicle;

    private StateManager localStateManager;

    public Transform SteeringWheel;
    float steeringAngle;

    private SteeringWheelInputController steeringInput;

    public bool useKeyBoard;


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

    public bool ReadyForAssignment = false;


    private bool breakIsOn;

    public float SteeringInput;
    public float ThrottleInput;
    public float SteeringInput_LastFrame;
    public float ThrottleInput_LastFrame;


    public NetworkVariable<float> selfAlignmentTorque =
        new NetworkVariable<float>(NetworkVariableReadPermission.OwnerOnly);


    void Awake() { ReadyForAssignment = false; }

    private void Start() { indicaterStage = 0; }


    public override void OnNetworkSpawn() {
        if (!IsLocalPlayer) {
            GetComponent<FFNetworkedClient>().enabled = false;
            GetComponent<SteeringWheelInputController>().enabled = false;
        }

        if (IsLocalPlayer) {
            localStateManager = GetComponent<StateManager>();
            steeringInput = GetComponent<SteeringWheelInputController>();
        }
    }

    [ServerRpc]
    private void BrakeLightChangedServerRpc(bool newvalue) { NetworkedVehicle.TurnOnBrakeLightClientRpc(newvalue); }

    [ServerRpc]
    private void RightIndicatorChangedServerRpc(bool newvalue) { NetworkedVehicle.TurnOnRightClientRpc(newvalue); }

    [ServerRpc]
    private void LeftIndicatorChangedServerRpc(bool newvalue) { NetworkedVehicle.TurnOnLeftClientRpc(newvalue); }


    [ServerRpc]
    public void PostQuestionServerRPC(ulong clientID) { ConnectionAndSpawing.Singleton.FinishedQuestionair(clientID); }


    #region IndicatorLogic

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
                    LeftIndicatorChangedServerRpc(LeftIsActuallyOn);
                    RightIndicatorChangedServerRpc(RightIsActuallyOn);
                }
                else {
                    RightIndicatorChangedServerRpc(false);
                    LeftIndicatorChangedServerRpc(false);
                }
            }

            if (indicaterStage == 2) {
                if (steeringInput != null && Mathf.Abs(steeringInput.GetSteerInput() * -450f) > 90) {
                    indicaterStage = 3;
                }
            }
            else if (indicaterStage == 3) {
                if (steeringInput != null && Mathf.Abs(steeringInput.GetSteerInput() * -450f) < 10) {
                    indicaterStage = 4;
                }
            }
        }
        else if (indicaterStage == 4) {
            indicaterStage = 0;
            ActualLightOn = false;
            LeftIsActuallyOn = false;
            RightIsActuallyOn = false;
            RightIndicatorChangedServerRpc(false);
            LeftIndicatorChangedServerRpc(false);
            // UpdateIndicatorLightsServerRpc(false, false);
        }
    }

    #endregion

    [ServerRpc]
    public void HonkMyCarServerRpc() {
        Debug.Log("HonkMyCarServerRpc");
        NetworkedVehicle.HonkMyCarClientRpc();
    }

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
        //if (IsOwner) return;
        if (MyCar.TryGet(out NetworkObject targetObject)) {
            NetworkedVehicle = targetObject.transform.GetComponent<VehicleInputControllerNetworked>();
          
        }
        else {
            Debug.LogWarning(
                "Did not manage to get my Car assigned interactions will not work. Maybe try calling this RPC later.");
        }
    }
    
    public void AssignCarTransform_OnServer(VehicleInputControllerNetworked MyCar) {
        if (IsServer) { NetworkedVehicle = MyCar; }
    }
    [ServerRpc]
    public void UpdateSteeringValServerRPC(float SteeringInput_) {
        if (IsServer && NetworkedVehicle != null) {
          
            NetworkedVehicle.SteeringInput = SteeringInput_;
        }
    }

    [ServerRpc]
    public void UpdateThrottleValServerRPC(float ThrottleInput_) {
        if (IsServer && NetworkedVehicle != null) {
          
            NetworkedVehicle.ThrottleInput = ThrottleInput_;
        }
    }

    private void LateUpdate() {
        if (SteeringInput != SteeringInput_LastFrame) {
            SteeringInput_LastFrame = SteeringInput;
            UpdateSteeringValServerRPC(SteeringInput);
        }

        if (ThrottleInput != ThrottleInput_LastFrame) {
            ThrottleInput_LastFrame = ThrottleInput;
            UpdateThrottleValServerRPC(ThrottleInput);
        }
    }

    void Update() {
        if (IsServer && NetworkedVehicle != null) { selfAlignmentTorque.Value = NetworkedVehicle.selfAlignmentTorque; }


        if (IsLocalPlayer) {
            if (ReadyForAssignment == false && transform.parent != null) { ReadyForAssignment = true; }


/*
            if (Input.GetKeyDown(KeyCode.Q))
            {
               // StartQuestionairGloabllyServerRpc();

            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                GetComponentInChildren<GpsController>().SetDirection(GpsController.Direction.Left);
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                GetComponentInChildren<GpsController>().SetDirection(GpsController.Direction.Right);
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                GetComponentInChildren<GpsController>().SetDirection(GpsController.Direction.Straight);
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                GetComponentInChildren<GpsController>().SetDirection(GpsController.Direction.Stop);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                GetComponentInChildren<GpsController>().SetDirection(GpsController.Direction.Hurry);
            }
            
*/
            if (localStateManager.GlobalState.Value == ActionState.DRIVE) {
                if (steeringInput == null || useKeyBoard) {
                    SteeringInput = Input.GetAxis("Horizontal");
                    ThrottleInput = Input.GetAxis("Vertical");
                }
                else {
                    SteeringInput = steeringInput.GetSteerInput();
                    ThrottleInput = steeringInput.GetAccelInput();
                    SteeringWheel.RotateAround(SteeringWheel.position, SteeringWheel.up,
                        steeringAngle - steeringInput.GetSteerInput() * -450f);
                    steeringAngle = steeringInput.GetSteerInput() * -450f;
                }

                bool TempLeft = Input.GetButton("indicateLeft");
                bool TempRight = Input.GetButton("indicateRight");
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


                if (Input.GetButtonDown("Horn")) { HonkMyCarServerRpc(); }

                if (ThrottleInput < 0 && !breakIsOn) {
                    BrakeLightChangedServerRpc(true);
                    breakIsOn = true;
                }
                else if (ThrottleInput >= 0 && breakIsOn) {
                    BrakeLightChangedServerRpc(false);
                    breakIsOn = false;
                }
            }
            else if (localStateManager.GlobalState.Value == ActionState.QUESTIONS) {
                SteeringInput = 0;
                ThrottleInput = -1;
            }
        }
    }
}