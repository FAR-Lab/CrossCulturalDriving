/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */


using System;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using UnityEngine;

using System.Collections;
using UnityEngine.SceneManagement;

public class ParticipantInputCapture : NetworkBehaviour
{

    private VehicleInputControllerNetworked NetworkedVehicle;
    private Transform MyRemoteCar;

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
    
    
    
    private NetworkVariableBool breakIsOn = new NetworkVariableBool(
        new NetworkVariableSettings {WritePermission = NetworkVariablePermission.OwnerOnly,ReadPermission  = NetworkVariablePermission.ServerOnly});
    
    public NetworkVariableBool LeftIndicators = new NetworkVariableBool(
        new NetworkVariableSettings {WritePermission = NetworkVariablePermission.OwnerOnly,ReadPermission  = NetworkVariablePermission.ServerOnly});
    public NetworkVariableBool RightIndicators = new NetworkVariableBool(
        new NetworkVariableSettings {WritePermission = NetworkVariablePermission.OwnerOnly,ReadPermission  = NetworkVariablePermission.ServerOnly});
   
    
    public NetworkVariableFloat SteeringInput = new NetworkVariableFloat(
        new NetworkVariableSettings {WritePermission = NetworkVariablePermission.OwnerOnly,ReadPermission  = NetworkVariablePermission.ServerOnly});
    public NetworkVariableFloat ThrottleInput = new NetworkVariableFloat(
        new NetworkVariableSettings {WritePermission = NetworkVariablePermission.OwnerOnly,ReadPermission  = NetworkVariablePermission.ServerOnly});
    void Awake()
    {
        transform.GetChild(0).gameObject.SetActive(false);
    }
    private void Start()
    {
        steeringInput = GetComponent<SteeringWheelInputController>();
        indicaterStage = 0;
        localStateManager = GetComponent<StateManager>();

    }


    public void AssignCarLocalServerCall(VehicleInputControllerNetworked VICN)
    {
        if (!IsServer) return;

        transform.parent = VICN.CameraPosition;
        NetworkedVehicle = VICN;
        
        //Assigne the value change things

        LeftIndicators.OnValueChanged += LeftIndicatorChanged;
        RightIndicators.OnValueChanged += RightIndicatorChanged;
        breakIsOn.OnValueChanged += BrakeLightChanged;
    }

    private void BrakeLightChanged(bool previousvalue, bool newvalue)
    {
        NetworkedVehicle.TurnOnBrakeLightClientRpc(newvalue);
    }


    private void RightIndicatorChanged(bool previousvalue, bool newvalue)
    {
        NetworkedVehicle.TurnOnRightClientRpc(newvalue);
        
    }
    private void LeftIndicatorChanged(bool previousvalue, bool newvalue)
    {
        NetworkedVehicle.TurnOnLeftClientRpc(newvalue);
    }

    [ClientRpc]
    public void AssignCarClientRPC(ulong ObjectID, ClientRpcParams clientRpcParams = default)
    {
        
        MyRemoteCar = GetNetworkObject(ObjectID).transform;
        transform.parent = MyRemoteCar.FindChildRecursive("CameraPosition");
        transform.localPosition = Vector3.zero;
        if (IsLocalPlayer)
        {
            transform.GetChild(0).gameObject.SetActive(true);
        }
        else
        {
            transform.GetChild(0).gameObject.SetActive(false);
        }
    }
    void startBlinking(bool left, bool right)
    {
       
        indicaterStage = 1;
        if (left == right == true)
        {
            if (LeftIsActuallyOn != true || RightIsActuallyOn != true)
            {
                LeftIsActuallyOn = true;
                RightIsActuallyOn = true;
            }
            else if (LeftIsActuallyOn == RightIsActuallyOn == true)
            {
                RightIsActuallyOn = false;
                LeftIsActuallyOn = false;
            }
        }
        if (left != right)
        {

            if (LeftIsActuallyOn == RightIsActuallyOn == true) // When we are returning from the hazard lights we make sure that not the inverse thing turns on 
            {
                LeftIsActuallyOn = false;
                RightIsActuallyOn = false;
            }
            if (left)
            {
                if (!LeftIsActuallyOn)
                {
                    LeftIsActuallyOn = true;
                    RightIsActuallyOn = false;
                }
                else
                {
                    LeftIsActuallyOn = false;
                }
            }
            if (right)
            {
                if (!RightIsActuallyOn)
                {
                    LeftIsActuallyOn = false;
                    RightIsActuallyOn = true;
                }
                else
                {

                    RightIsActuallyOn = false;
                }
            }
        }
    }

    [ServerRpc]
    public void PostQuestionServerRPC(ulong clientID)
    {
        ConnectionAndSpawing.Singleton.FinishedQuestionair(clientID);
    }
    
    
    
    void UpdateIndicator()
    {
        if (indicaterStage == 1)
        {
            indicaterStage = 2;
            indicaterTimer = interval;
            ActualLightOn = false;
        }
        else if (indicaterStage == 2 || indicaterStage == 3)
        {
            indicaterTimer += Time.deltaTime;

            if (indicaterTimer > interval)
            {
                indicaterTimer = 0;
                ActualLightOn = !ActualLightOn;
                if (ActualLightOn)
                {
                    LeftIndicators.Value = LeftIsActuallyOn;
                    RightIndicators.Value = RightIsActuallyOn;
                }
                else
                {
                    LeftIndicators.Value = false;
                    RightIndicators.Value = false;
                }
            }
            if (indicaterStage == 2)
            {

                if (steeringInput!=null && Mathf.Abs(steeringInput.GetSteerInput() * -450f) > 90)
                { 
                    indicaterStage = 3;
                }
            }
            else if (indicaterStage == 3)
            {
                if (steeringInput!=null &&  Mathf.Abs(steeringInput.GetSteerInput() * -450f) < 10)
                {
                    indicaterStage = 4;
                }
            }

        }
        else if (indicaterStage == 4)
        {
            indicaterStage = 0;
            ActualLightOn = false;
            LeftIsActuallyOn = false;
            RightIsActuallyOn = false;
            LeftIndicators.Value = false;
            RightIndicators.Value = false;
           // UpdateIndicatorLightsServerRpc(false, false);

        }
    }

    [ServerRpc]
    public void HonkMyCarServerRpc()
    {
        Debug.Log("HonkMyCarServerRpc");
        NetworkedVehicle.HonkMyCarClientRpc();
    }
    
    [ClientRpc]
    public void StartQuestionnaireClientRpc()
    {
        if (IsLocalPlayer)
        {
            FindObjectOfType<ScenarioManager>().RunQuestionairNow(transform);
        }
    }


   [ClientRpc]
    public void SetGPSClientRpc(GpsController.Direction[] dir)
    {
       // GetComponentInChildren<GpsController>().SetDirection(dir[SceneStateManager.Instance.getParticipantID()]);
    }

    private void OnGUI()
    {
        if(IsLocalPlayer)
        GUI.Label(new Rect(200, 5, 150, 100), "Client State" +localStateManager.GlobalState.Value);
    }


    void Update()
    {
        if (IsServer && NetworkedVehicle!=null)
        {
            NetworkedVehicle.SteeringInput =SteeringInput.Value;
            NetworkedVehicle.ThrottleInput =ThrottleInput.Value;
        }

      

        if (IsLocalPlayer)
        {
        
            if (Input.GetKeyDown(KeyCode.Return))
            {
               // StartDrivingServerRpc();
            }
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
            if (localStateManager.GlobalState.Value==ActionState.DRIVE) 
                {

                if (steeringInput == null || useKeyBoard)
                {
                   
                   SteeringInput.Value = Input.GetAxis("Horizontal");
                    ThrottleInput.Value = Input.GetAxis("Vertical");
                }
                else
                {
                  SteeringInput.Value = steeringInput.GetSteerInput();
                  ThrottleInput.Value= steeringInput.GetAccelInput();
                    SteeringWheel.RotateAround(SteeringWheel.position, SteeringWheel.up, steeringAngle - steeringInput.GetSteerInput() * -450f);
                    steeringAngle = steeringInput.GetSteerInput() * -450f;
                }
                
                bool TempLeft = Input.GetButton("indicateLeft");
                bool TempRight = Input.GetButton("indicateRight");
                if (TempLeft || TempRight)
                {
                    DualButtonDebounceIndicator = true;
                    if (TempLeft)
                    {
                        LeftIndicatorDebounce = true;
                    }
                    if (TempRight)
                    {
                       RightIndicatorDebounce = true;
                    }
                }
                else if (DualButtonDebounceIndicator && !TempLeft && !TempRight) {
                    startBlinking(LeftIndicatorDebounce, RightIndicatorDebounce);
                    DualButtonDebounceIndicator = false;
                    LeftIndicatorDebounce = false;
                    RightIndicatorDebounce = false;
                }
                
                UpdateIndicator();



                if (Input.GetButtonDown("Horn"))
                {
                    HonkMyCarServerRpc();
                }
                if (ThrottleInput.Value < 0 && !breakIsOn.Value) 
                {
                    breakIsOn.Value = true;
                }
                else if (ThrottleInput.Value >= 0 && breakIsOn.Value)
                {
                    breakIsOn.Value = false;
                }

            }
            else if (localStateManager.GlobalState.Value == ActionState.QUESTIONS)
            {
                SteeringInput.Value = 0;
                ThrottleInput.Value = -1;
            }
        }
    }
}
