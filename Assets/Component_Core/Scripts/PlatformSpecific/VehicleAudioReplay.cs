using System.Collections;
using System.Collections.Generic;
using UltimateReplay;
using UnityEngine;

public class VehicleAudioReplay : ReplayBehaviour
{
    private VehicleAudio _vehicleAudio;

    private VehicleAudioController _vehicleAudioController;
    private NetworkVehicleController _networkVehicleController;
    [ReplayVar(false)] public bool IsShifting;
    [ReplayVar(true)] public float accellInput;
    [ReplayVar(true)] public float RPM;
    [ReplayVar(true)] public float traction;
    [ReplayVar(true)] public float MotorWheelsSlip;
    [ReplayVar(true)] public float CurrentSpeed;
    [ReplayVar(false)] public int CurrentSurface;

    private float load = 0f;

    private RoadSurface lastSurface = RoadSurface.Airborne;


    private AudioSource HonkSound;

    // Start is called before the first frame update
    void Start(){
        _vehicleAudio = GetComponent<VehicleAudio>();
        _networkVehicleController = GetComponent<NetworkVehicleController>();
        _networkVehicleController.registerHonk(HonkRecord);

        HonkSound = GetComponent<AudioSource>();
    }

    private void HonkRecord(){
        if (IsRecording){
            RecordEvent(1);
        }
    }

    public override void OnReplayEvent(ushort eventID, ReplayState
        eventData){
        switch (eventID){
            case 1:{
                if (HonkSound.isPlaying){
                    return;
                }

                HonkSound.Play();
                break;
            }
        }
    }

    // Update is called once per frame
    void Update(){
        if (!IsReplaying){
            IsShifting = _networkVehicleController.IsShifting.Value;

            accellInput = _networkVehicleController.accellInput.Value;
            RPM = _networkVehicleController.RPM.Value;
            traction = _networkVehicleController.traction.Value;

            MotorWheelsSlip = _networkVehicleController.MotorWheelsSlip.Value;
            CurrentSpeed = _networkVehicleController.CurrentSpeed.Value;
            CurrentSurface = (int) _networkVehicleController.CurrentSurface.Value;
        }
        else{
            if (_vehicleAudio == null){
                Start();}
            if (!_vehicleAudio.enabled) _vehicleAudio.enabled = true;
            if (!_vehicleAudioController.roadAudio.enabled) _vehicleAudioController.roadAudio.enabled = true;
            if (!_vehicleAudioController.windAudio.enabled) _vehicleAudioController.windAudio.enabled = true;

            load = Mathf.Lerp(load, IsShifting ? 0f : accellInput, Time.deltaTime * 2f);
            _vehicleAudio.rpm = RPM;
            _vehicleAudio.load = load;


            var accellTraction = 1f - Mathf.Clamp01(MotorWheelsSlip);
            var brakeTraction = 1f - Mathf.Clamp01(-traction);

            _vehicleAudioController.roadAudio.accellTraction = accellTraction;
            _vehicleAudioController.roadAudio.brakeTraction = brakeTraction;
            _vehicleAudioController.roadAudio.speed = CurrentSpeed;
            _vehicleAudioController.roadAudio.surface = (RoadSurface) CurrentSurface;

            if ((RoadSurface) CurrentSurface != RoadSurface.Airborne && lastSurface != (RoadSurface) CurrentSurface){
                lastSurface = (RoadSurface) CurrentSurface;
                _vehicleAudioController.PlaySurfaceBump();
            }

            _vehicleAudioController.windAudio.speed = CurrentSpeed;
        }
    }
}