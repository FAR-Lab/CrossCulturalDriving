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
using System.Collections.Generic;
using System.Diagnostics;
using UltimateReplay;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;


public class NetworkVehicleController : Interactable_Object {
    public enum VehicleOpperationMode {
        KEYBOARD,
        STEERINGWHEEL,
        AUTONOMOUS,
        REMOTEKEYBOARD
    }

    [SerializeField] public VehicleOpperationMode VehicleMode;
    public Transform CameraPosition;

    private VehicleController controller;
    private AutonomousVehicleDriver _autonomousVehicleDriver;

    public Transform[] Left;
    public Transform[] Right;
    public Transform[] BrakeLightObjects;

    public Material IndicatorOn;
    public Material LightsOff;
    public Material BrakelightsOn;

    public List<Renderer> beamLightGlasses;
    private List<Material> _beamLightGlassMaterialInstances = new List<Material>();
    public Color beamLightGlassOnColor;
    public Color beamLightGlassOffColor;
    
    public List<Renderer> beamLights;
    private List<Material> _beamLightMaterialInstances = new List<Material>();
    


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

    private bool REMOTEKEYBOARD_NewData = false;
    void UpdateSounds() {
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

    public override void Stop_Action() {
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    }


    public override void OnNetworkSpawn() {
        m_Speedometer = GetComponentInChildren<Speedometer>();
        CurrentSpeed.OnValueChanged += NewSpeedRecieved;
        if (IsServer) {
            GetComponent<ParticipantOrderReplayComponent>().enabled = true;
            controller = GetComponent<VehicleController>();
        }
        else {
            GetComponent<VehicleController>().enabled = false;
            if (GetComponent<ForceFeedback>() != null) {
                GetComponent<ForceFeedback>().enabled = false;
            }

            foreach (var wc in GetComponentsInChildren<WheelCollider>()) wc.enabled = false;
            if (VehicleMode == VehicleOpperationMode.AUTONOMOUS) {
                GetComponent<AutonomousVehicleDriver>().enabled = false;
            }
        }
    }

    private void NewSpeedRecieved(float previousvalue, float newvalue) {
        if (m_Speedometer != null) m_Speedometer.UpdateSpeed(newvalue);
    }

    private void Start() {
        indicaterStage = 0;

        foreach (Renderer tmpRenderer in beamLights) {
            Debug.Log(tmpRenderer.materials[0]);
            _beamLightMaterialInstances.Add(tmpRenderer.materials[0]);
        }
        
        foreach (Renderer tmpRenderer in beamLightGlasses) {
            _beamLightGlassMaterialInstances.Add(tmpRenderer.materials[0]);
        }
        
        
        HonkSound = GetComponent<AudioSource>();

        foreach (Transform t in Left) {
            t.GetComponent<MeshRenderer>().material = LightsOff;
        }

        foreach (Transform t in Right) {
            t.GetComponent<MeshRenderer>().material = LightsOff;
        }

        foreach (Transform t in BrakeLightObjects) {
            t.GetComponent<MeshRenderer>().material = LightsOff;
        }

        if (VehicleMode == VehicleOpperationMode.AUTONOMOUS) {
            _autonomousVehicleDriver = GetComponent<AutonomousVehicleDriver>();
        }

        if (SteeringWheelManager.Singleton == null) {
            VehicleMode = VehicleOpperationMode.KEYBOARD;
        }
    }


    [ClientRpc]
    public void TurnOnLeftClientRpc(bool Leftl_) {
        TurnOnLeft(Leftl_);
    }

    private void TurnOnLeft(bool Leftl_) {
        if (Leftl_) {
            foreach (Transform t in Left) {
                t.GetComponent<MeshRenderer>().material = IndicatorOn;
            }
        }
        else {
            foreach (Transform t in Left) {
                t.GetComponent<MeshRenderer>().material = LightsOff;
            }
        }
    }

    [ClientRpc]
    public void TurnOnRightClientRpc(bool Rightl_) {
        TurnOnRight(Rightl_);
    }

    private void TurnOnRight(bool Rightl_) {
        if (Rightl_) {
            foreach (Transform t in Right) {
                t.GetComponent<MeshRenderer>().material = IndicatorOn;
            }
        }
        else {
            foreach (Transform t in Right) {
                t.GetComponent<MeshRenderer>().material = LightsOff;
            }
        }
    }

    [ClientRpc]
    public void TurnOnBrakeLightClientRpc(bool Active) {
        TurnOnBrakeLight(Active);
    }

    private void TurnOnBrakeLightLocalServer(bool Active) {
        TurnOnBrakeLightClientRpc(Active);
        TurnOnBrakeLight(Active);
    }

    private void TurnOnBrakeLight(bool Active) {
        if (Active) {
            foreach (Transform t in BrakeLightObjects) {
                t.GetComponent<MeshRenderer>().material = BrakelightsOn;
            }
        }
        else {
            foreach (Transform t in BrakeLightObjects) {
                t.GetComponent<MeshRenderer>().material = LightsOff;
            }
        }
    }

    public delegate void HonkDelegate();

    public HonkDelegate HonkHook;

    public void registerHonk(HonkDelegate val) {
        HonkHook += val;
    }

    public void DeRegisterHonk(HonkDelegate val) {
        HonkHook -= val;
    }

    [ClientRpc]
    public void HonkMyCarClientRpc() {
        // Debug.Log("HonkMyCarClientRpc");
        HonkSound.Play();
        if (HonkHook != null) {
            HonkHook.Invoke();
        }
    }

    private void LateUpdate() {
        if (IsServer && controller != null) {
            controller.steerInput = SteeringInput;
            controller.accellInput = ThrottleInput;
        }
        
        // test
        if (Input.GetKeyDown(KeyCode.Keypad1)) {
            foreach (Material mat in _beamLightMaterialInstances) {
                mat.SetFloat("_Opacity", 2);
            }
            
            foreach (Material mat in _beamLightGlassMaterialInstances) {
                mat.SetColor("_Color", beamLightGlassOnColor);
            }
        }
        if(Input.GetKeyDown(KeyCode.Keypad2)){
            foreach (Material mat in _beamLightMaterialInstances) {
                mat.SetFloat("_Opacity", 0);
            }
            
            foreach (Material mat in _beamLightGlassMaterialInstances) {
                mat.SetColor("_Color", beamLightGlassOffColor);
            }
        }
    }


    float _steeringAngle;
    public Transform SteeringWheel;


    public ParticipantOrder getParticipantOrder() {
        return m_participantOrder.Value;
    }

    void Update() {
        if (!IsServer) return;

        if (ConnectionAndSpawning.Singleton.ServerState == ActionState.DRIVE) {
            bool tempLeft = false, tempRight = false, tempHonk = false, tempHighBeam =false;

            switch (VehicleMode) {
                case VehicleOpperationMode.KEYBOARD:
                    SteeringInput = Input.GetAxis("Horizontal");
                    ThrottleInput = Input.GetAxis("Vertical");
                    break;
                case VehicleOpperationMode.STEERINGWHEEL:
                    SteeringInput = SteeringWheelManager.Singleton.GetSteerInput(m_participantOrder.Value);
                    ThrottleInput = SteeringWheelManager.Singleton.GetAccelInput(m_participantOrder.Value);
                    tempLeft =
                        SteeringWheelManager.Singleton
                            .GetLeftIndicatorInput(m_participantOrder.Value);
                    tempRight =
                        SteeringWheelManager.Singleton
                            .GetRightIndicatorInput(m_participantOrder.Value);
                    tempHonk = SteeringWheelManager.Singleton.GetHornButtonInput(m_participantOrder.Value);
                    tempHighBeam = SteeringWheelManager.Singleton.GetHighBeamButtonInput(m_participantOrder.Value);

                    break;
                case VehicleOpperationMode.AUTONOMOUS:

                    SteeringInput = _autonomousVehicleDriver.GetSteerInput();
                    ThrottleInput = _autonomousVehicleDriver.GetAccelInput();
                    tempLeft = _autonomousVehicleDriver
                        .GetLeftIndicatorInput();
                    tempRight = _autonomousVehicleDriver.GetRightIndicatorInput();
                    tempHonk = _autonomousVehicleDriver.GetHornInput();

                    if (_autonomousVehicleDriver.StopIndicating()) {
                        _StopIndicating();
                    }


                    break;
                case VehicleOpperationMode.REMOTEKEYBOARD:
                    if (REMOTEKEYBOARD_NewData) {
                        REMOTEKEYBOARD_NewData = false;

                        tempLeft = leftInput;
                        tempRight = rightInput;
                        tempHonk = honkInput;
                        
                    };
                    
                    
                    break;
                default:
                    break;
            }


            SteeringWheel.RotateAround(SteeringWheel.position, SteeringWheel.up,
                _steeringAngle - SteeringInput * -450f);
            _steeringAngle = SteeringInput * -450f;


            if (NewButtonPress && (tempLeft || tempRight)) {
                NewButtonPress = false;
                if (tempLeft && ! tempRight) {
                    toggleBlinking(true, false);
                    LeftIndicatorDebounce = true;
                }

                else if (tempRight && !tempLeft) {
                    toggleBlinking(false, true);
                    RightIndicatorDebounce = true;
                }
                else {
                    
                    toggleBlinking(true, true);
                    BothIndicatorDebounce = true;
                }
                
                
            }else if (NewButtonPress == false && !BothIndicatorDebounce && ((tempLeft&& !LeftIndicatorDebounce) || (tempRight&& !RightIndicatorDebounce))) {
                toggleBlinking(true, true);
                BothIndicatorDebounce = true;
            }
            else if (NewButtonPress == false && !tempLeft && !tempRight) {
                NewButtonPress = true;
                LeftIndicatorDebounce = false;
                RightIndicatorDebounce = false;
                BothIndicatorDebounce = false;
            }

            UpdateIndicator();


            if (tempHonk) {
                HonkMyCar();
            }
            HighBeamMyCar(tempHighBeam);

            if (ThrottleInput < 0 && !breakIsOn) {
                TurnOnBrakeLightLocalServer(true);
                breakIsOn = true;
            }
            else if (ThrottleInput >= 0 && breakIsOn) {
                TurnOnBrakeLightLocalServer(false);
                breakIsOn = false;
            }
        }
        else if (ConnectionAndSpawning.Singleton.ServerState == ActionState.QUESTIONS) {
            SteeringInput = 0;
            ThrottleInput = -1;
        }

        UpdateSounds();
    }

    
    bool  leftInput, rightInput, honkInput;
    
    
    public void NewDataToCome(float i_steering, float i_throttle, bool i_left, bool i_right, bool i_honk) {
        REMOTEKEYBOARD_NewData = true;
        SteeringInput = i_steering;
        ThrottleInput = i_throttle;
        leftInput = i_left;
        rightInput = i_right;
        honkInput = i_honk;

    }
    public override void AssignClient(ulong CLID_, ParticipantOrder _participantOrder_) {
        if (IsServer) {
            NetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            CLID = CLID_;
            m_participantOrder.Value = _participantOrder_;
            GetComponent<ParticipantOrderReplayComponent>().SetParticipantOrder(m_participantOrder.Value);
            GetComponent<ForceFeedback>()?.Init(transform.GetComponent<Rigidbody>(), m_participantOrder.Value);
        }
        else {
            Debug.LogWarning("Tried to execute something that should never happen. ");
        }
    }

    public override Transform GetCameraPositionObject() {
        return transform.Find("CameraPosition");
    }

    public override void SetStartingPose(Pose _pose) {
        if (!IsServer) return;
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


    private void SceneManager_OnSceneEvent(SceneEvent sceneEvent) {
        //   Debug.Log("SceneManager_OnSceneEvent called with event:" + sceneEvent.SceneEventType.ToString());
        switch (sceneEvent.SceneEventType) {
            case SceneEventType.SynchronizeComplete: {
                //  Debug.Log("Scene event change by Client: " + sceneEvent.ClientId);
                if (sceneEvent.ClientId == CLID) {
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


    private bool m_HighBeams;
    public void HighBeamMyCar(bool tempHighBeam) {
        if (m_HighBeams == tempHighBeam) return;
        else {
            if (tempHighBeam) {
                foreach (Material mat in _beamLightMaterialInstances) {
                    mat.SetFloat("_Opacity", 2);
                }
            
                foreach (Material mat in _beamLightGlassMaterialInstances) {
                    mat.SetColor("_Color", beamLightGlassOnColor);
                }
            }
            else{
                foreach (Material mat in _beamLightMaterialInstances) {
                    mat.SetFloat("_Opacity", 0);
                }
            
                foreach (Material mat in _beamLightGlassMaterialInstances) {
                    mat.SetColor("_Color", beamLightGlassOffColor);
                }
            }
        }

        m_HighBeams = tempHighBeam;

    }
    

    public void HonkMyCar() {
        if (HonkSound.isPlaying) {
            return;
        }

        HonkSound.Play();
        HonkMyCarClientRpc();
    }


    public void SetNewNavigationInstructions(Dictionary<ParticipantOrder, NavigationScreen.Direction> Directions) {
        if (Directions.ContainsKey(m_participantOrder.Value)) {
            GetComponentInChildren<NavigationScreen>().SetDirection(Directions[m_participantOrder.Value]);
            SetNavigationScreenClientRPC(Directions[m_participantOrder.Value]);
        }
    }

    [ClientRpc]
    private void SetNavigationScreenClientRPC(NavigationScreen.Direction Direction) {
        if (IsClient)
            GetComponentInChildren<NavigationScreen>().SetDirection(Direction);
    }


    #region IndicatorLogic

    private bool LeftActive;
    private bool RightActive;


    public bool LeftIsActuallyOn;
    public bool RightIsActuallyOn;
    public bool ActualLightOn;
    private float indicaterTimer;
    public float interval;
    public int indicaterStage;

    public bool NewButtonPress;
    public bool LeftIndicatorDebounce;
    public bool RightIndicatorDebounce;
    public bool BothIndicatorDebounce;

    void toggleBlinking(bool left, bool right) {
        if (indicaterStage == 0) {
            indicaterStage = 1;
            
        }
        
        if (left && right ) {
            if (LeftIsActuallyOn != true || RightIsActuallyOn != true) {
                LeftIsActuallyOn = true;
                RightIsActuallyOn = true;
            }
            else if (LeftIsActuallyOn == RightIsActuallyOn == true) {
                RightIsActuallyOn = false;
                LeftIsActuallyOn = false;
                indicaterStage = 4;
            }
        }

        if (left != right) {
            if (LeftIsActuallyOn && RightIsActuallyOn) 
            {
                LeftIsActuallyOn = false;
                RightIsActuallyOn = false;
                indicaterStage = 4;
            }
            if (left) {
                if (!LeftIsActuallyOn) {
                    LeftIsActuallyOn = true;
                    RightIsActuallyOn = false;
                }
                else {
                    LeftIsActuallyOn = false;
                    indicaterStage = 4;
                }
            }

            if (right) {
                if (!RightIsActuallyOn) {
                    LeftIsActuallyOn = false;
                    RightIsActuallyOn = true;
                }
                else {
                    RightIsActuallyOn = false;
                    indicaterStage = 4;
                }
            }
        }
    }

    public string GetIndicatorString() {
        return "Left" + LeftIsActuallyOn.ToString() + farlab_logger.supSep + "Right" + RightIsActuallyOn.ToString();
    }

    public void GetIndicatorState(out bool Left, out bool right) {
        Left = LeftIsActuallyOn;
        right = RightIsActuallyOn;
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
                switch (VehicleMode) {
                    
                    case VehicleOpperationMode.KEYBOARD:
                        break;
                    case VehicleOpperationMode.STEERINGWHEEL:
                        if (SteeringWheelManager.Singleton != null &&
                            Mathf.Abs(SteeringWheelManager.Singleton.GetSteerInput(m_participantOrder.Value) * -450f) > 90) {
                            indicaterStage = 3;
                        }

                        break;
                    case VehicleOpperationMode.AUTONOMOUS:
                        break;
                    case VehicleOpperationMode.REMOTEKEYBOARD:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else if (indicaterStage == 3) {
                switch (VehicleMode) {
                    case VehicleOpperationMode.KEYBOARD:
                        break;
                    case VehicleOpperationMode.STEERINGWHEEL:
                        if (SteeringWheelManager.Singleton != null &&
                            Mathf.Abs(SteeringWheelManager.Singleton.GetSteerInput(m_participantOrder.Value) * -450f) < 10) {
                            indicaterStage = 4;
                        }

                        break;
                    case VehicleOpperationMode.AUTONOMOUS:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
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

    private void _StopIndicating() {
        if (indicaterStage > 1) {
            indicaterStage = 4;
        }
    }

    private void RightIndicatorChanged(bool newvalue) {
        if (!IsServer) {
            return;
        }

        TurnOnRight(newvalue);
        TurnOnRightClientRpc(newvalue);
    }

    private void LeftIndicatorChanged(bool newvalue) {
        TurnOnLeft(newvalue);
        TurnOnLeftClientRpc(newvalue);
    }

    #endregion
}