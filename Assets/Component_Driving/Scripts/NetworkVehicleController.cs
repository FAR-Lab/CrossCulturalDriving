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
using UltimateReplay;
using UnityEngine.Serialization;


public class NetworkVehicleController : Interactable_Object
{
    public Transform CameraPosition;

    private VehicleController controller;
    public bool useKeyBoard;


    public Transform[] Left;
    public Transform[] Right;
    public Transform[] BrakeLightObjects;

    public Material IndicatorOn;
    public Material LightsOff;
    public Material BrakelightsOn;


    public AudioSource HonkSound;
    public float SteeringInput;
    public float ThrottleInput;


    private ulong CLID;

    Speedometer m_Speedometer;

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


    void UpdateSounds()
    {
        if (controller == null) return;
        IsShifting.Value = controller.IsShifting;
        accellInput.Value = controller.accellInput;
        RPM.Value = controller.RPM;
        traction.Value = (controller.traction + controller.tractionR + controller.rtraction + controller.rtractionR) /
                         4.0f;
        MotorWheelsSlip.Value = controller.MotorWheelsSlip;
        CurrentSpeed.Value = controller.CurrentSpeed;


        CurrentSurface.Value = controller.CurrentSurface;
    }

    public override void Stop_Action() {GetComponent<Rigidbody>().velocity = Vector3.zero;
       GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    }

    
    public override void OnNetworkSpawn()
    {
        m_Speedometer = GetComponentInChildren<Speedometer>();
        CurrentSpeed.OnValueChanged += NewSpeedRecieved;
        if (IsServer)
        {
            GetComponent<ParticipantOrderReplayComponent>().enabled = true;
            controller = GetComponent<VehicleController>();
        }
    }

    private void NewSpeedRecieved(float previousvalue, float newvalue)
    {
        if (m_Speedometer != null) m_Speedometer.UpdateSpeed(newvalue);
    }

    private void Start()
    {
        indicaterStage = 0;

        HonkSound = GetComponent<AudioSource>();

        foreach (Transform t in Left)
        {
            t.GetComponent<MeshRenderer>().material = LightsOff;
        }

        foreach (Transform t in Right)
        {
            t.GetComponent<MeshRenderer>().material = LightsOff;
        }

        foreach (Transform t in BrakeLightObjects)
        {
            t.GetComponent<MeshRenderer>().material = LightsOff;
        }
    }


    [ClientRpc]
    public void TurnOnLeftClientRpc(bool Leftl_)
    {
        TurnOnLeft(Leftl_);
    }

    private void TurnOnLeft(bool Leftl_)
    {
        if (Leftl_)
        {
            foreach (Transform t in Left)
            {
                t.GetComponent<MeshRenderer>().material = IndicatorOn;
            }
        }
        else
        {
            foreach (Transform t in Left)
            {
                t.GetComponent<MeshRenderer>().material = LightsOff;
            }
        }
    }

    [ClientRpc]
    public void TurnOnRightClientRpc(bool Rightl_)
    {
        TurnOnRight(Rightl_);
    }
    private void TurnOnRight(bool Rightl_)
    {
        if (Rightl_)
        {
            foreach (Transform t in Right)
            {
                t.GetComponent<MeshRenderer>().material = IndicatorOn;
            }
        }
        else
        {
            foreach (Transform t in Right)
            {
                t.GetComponent<MeshRenderer>().material = LightsOff;
            }
        }
    }

    [ClientRpc]
    public void TurnOnBrakeLightClientRpc(bool Active)
    {
        TurnOnBrakeLight(Active);
    }
    
    private void TurnOnBrakeLightLocalServer(bool Active){
        TurnOnBrakeLightClientRpc(Active);
        TurnOnBrakeLight(Active);
    }

    private void TurnOnBrakeLight(bool Active)
    {
       
        if (Active)
        {
            foreach (Transform t in BrakeLightObjects)
            {
                t.GetComponent<MeshRenderer>().material = BrakelightsOn;
            }
        }
        else
        {
            foreach (Transform t in BrakeLightObjects)
            {
                t.GetComponent<MeshRenderer>().material = LightsOff;
            }
        }
    }

    public delegate void HonkDelegate();

    public HonkDelegate HonkHook;

    public void registerHonk(HonkDelegate val){
        HonkHook += val;}
    public void DeRegisterHonk(HonkDelegate val){
        HonkHook -= val;}

    [ClientRpc]
    public void HonkMyCarClientRpc()
    {
       // Debug.Log("HonkMyCarClientRpc");
        HonkSound.Play();
        if (HonkHook != null){
            HonkHook.Invoke();
        }
    }

    private void LateUpdate()
    {
        if (IsServer && controller != null)
        {
            controller.steerInput = SteeringInput;
            controller.accellInput = ThrottleInput;
        }
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(200, 100, 300, 30), "IsServer: " + IsServer.ToString() + "  IsHost: " + IsHost.ToString() +
                                             "  IsClient: " +
                                             IsClient.ToString());
    }

    float steeringAngle;
    public Transform SteeringWheel;


    public ParticipantOrder getParticipantOrder()
    {
        return _participantOrder;
    }
    void Update() {
        if (!IsServer) return;

        if (ConnectionAndSpawning.Singleton.ServerState == ActionState.DRIVE)
        {
            if (SteeringWheelManager.Singleton == null || useKeyBoard)
            {
                SteeringInput = Input.GetAxis("Horizontal");
                ThrottleInput = Input.GetAxis("Vertical");
            }
            else
            {
                SteeringInput = SteeringWheelManager.Singleton.GetSteerInput(_participantOrder);
                ThrottleInput = SteeringWheelManager.Singleton.GetAccelInput(_participantOrder);

                SteeringWheel.RotateAround(SteeringWheel.position, SteeringWheel.up,
                    steeringAngle - SteeringInput * -450f);
                steeringAngle = SteeringInput * -450f;
            }

            bool TempLeft =
                SteeringWheelManager.Singleton
                    .GetLeftIndicatorInput(_participantOrder); //Input.GetButton("indicateLeft");
            bool TempRight =
                SteeringWheelManager.Singleton
                    .GetRightIndicatorInput(_participantOrder); // Input.GetButton("indicateRight");
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
            else if (DualButtonDebounceIndicator && !TempLeft && !TempRight)
            {
                startBlinking(LeftIndicatorDebounce, RightIndicatorDebounce);
                DualButtonDebounceIndicator = false;
                LeftIndicatorDebounce = false;
                RightIndicatorDebounce = false;
            }

            UpdateIndicator();


            if (SteeringWheelManager.Singleton.GetButtonInput(_participantOrder))
            {
                HonkMyCar();
            }

            if (ThrottleInput < 0 && !breakIsOn)
            {
                TurnOnBrakeLightLocalServer(true);
                breakIsOn = true;
               
            }
            else if (ThrottleInput >= 0 && breakIsOn)
            {
                
                TurnOnBrakeLightLocalServer(false);
                breakIsOn = false;
            }
        }
        else if (ConnectionAndSpawning.Singleton.ServerState == ActionState.QUESTIONS)
        {
            SteeringInput = 0;
            ThrottleInput = -1;
        }

        UpdateSounds();
    }

    public override void AssignClient(ulong CLID_, ParticipantOrder _participantOrder_)
    {
        if (IsServer)
        {
            NetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            CLID = CLID_;
            _participantOrder = _participantOrder_;
            GetComponent<ParticipantOrderReplayComponent>().SetParticipantOrder(_participantOrder);
            GetComponent<ForceFeedback>()?.Init(transform.GetComponent<Rigidbody>(), _participantOrder);
        }
        else
        {
            Debug.LogWarning("Tried to execute something that should never happen. ");
        }
    }

    public override Transform GetCameraPositionObject()
    {
        return transform.Find("CameraPosition");
    }

    public override void SetStartingPose(Pose _pose) {
        if (! IsServer) return;
        transform.GetComponent<Rigidbody>().velocity =
            Vector3.zero; // Unsafe we are not sure that it has a rigid body
        transform.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        transform.position = _pose.position;
        transform.rotation = _pose.rotation;
    }

    public override bool HasActionStopped() {
        if (transform.GetComponent<Rigidbody>().velocity.magnitude < 0.01f) {
            return true;
        }

        return false;
    }


    private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
    {
        //   Debug.Log("SceneManager_OnSceneEvent called with event:" + sceneEvent.SceneEventType.ToString());
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.SynchronizeComplete:
            {
                //  Debug.Log("Scene event change by Client: " + sceneEvent.ClientId);
                if (sceneEvent.ClientId == CLID)
                {
                    // Debug.Log("Server: " + IsServer.ToString() + "  IsClient: " + IsClient.ToString() +
                    //  "  IsHost: " + IsHost.ToString());
                    //SetPlayerParent(sceneEvent.ClientId);
                }

                break;
            }
            default:
                break;
        }
    }


    private bool breakIsOn;



    public void HonkMyCar()
    {
        if (HonkSound.isPlaying)
        {
            return;
        }

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
            if (LeftIsActuallyOn == RightIsActuallyOn ==
                true) // When we are returning from the hazard lights we make sure that not the inverse thing turns on
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

    public string GetIndicatorString()
    {
        return "Left" + LeftIsActuallyOn.ToString() + farlab_logger.supSep + "Right" + RightIsActuallyOn.ToString();
    }
    public void GetIndicatorState(out bool Left, out bool right)
    {
        Left = LeftIsActuallyOn;
        right = RightIsActuallyOn;
        
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
                    LeftIndicatorChanged(LeftIsActuallyOn);
                    RightIndicatorChanged(RightIsActuallyOn);
                }
                else
                {
                    RightIndicatorChanged(false);
                    LeftIndicatorChanged(false);
                }
            }

            if (indicaterStage == 2)
            {
                if (SteeringWheelManager.Singleton != null &&
                    Mathf.Abs(SteeringWheelManager.Singleton.GetSteerInput(_participantOrder) * -450f) > 90)
                {
                    indicaterStage = 3;
                }
            }
            else if (indicaterStage == 3)
            {
                if (SteeringWheelManager.Singleton != null &&
                    Mathf.Abs(SteeringWheelManager.Singleton.GetSteerInput(_participantOrder) * -450f) < 10)
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
            RightIndicatorChanged(false);
            LeftIndicatorChanged(false);
            // UpdateIndicatorLightsServerRpc(false, false);
        }
    }

    private void RightIndicatorChanged(bool newvalue)
    {
        if(!IsServer)
        {
            return;
        }
        TurnOnRight(newvalue);
        TurnOnRightClientRpc(newvalue);
    }

    private void LeftIndicatorChanged(bool newvalue)
    {
        TurnOnLeft(newvalue);
        TurnOnLeftClientRpc(newvalue);
    }

    #endregion
}
