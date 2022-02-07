/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using UnityEngine.Audio;

public class VehicleAudioController : MonoBehaviour {

    public VehicleAudio vehicleAudio;
    public RoadAudio roadAudio;
    public CollisionAudio collisionAudio;
    public IgnitionAudio ignitionAudio;
    public WindAudio windAudio;

    private NetworkVehicleController vehicleController;

    public AudioMixerSnapshot driveSnapshot;
    public AudioMixerSnapshot selectSnapshot;

    private float load = 0f;

    private RoadSurface lastSurface = RoadSurface.Airborne;

    void Awake()
    {
        vehicleController = GetComponent<NetworkVehicleController>();
    }

    public void PlayIgnition()
    {
        selectSnapshot.TransitionTo(0f);
        ignitionAudio.Play(PlayEngine);
    }

    public void PlayEngine()
    {
        this.enabled = true;
        driveSnapshot.TransitionTo(3f);
        
    }

    public void StopEngine()
    {
        this.enabled = false;
        selectSnapshot.TransitionTo(0f);
    }

    private void PlaySurfaceBump()
    {
        roadAudio.PlaySurfaceBump();
    }
	
	void Update () {
        load = Mathf.Lerp(load, vehicleController.IsShifting.Value ? 0f : vehicleController.accellInput.Value, Time.deltaTime * 2f);
        vehicleAudio.rpm = vehicleController.RPM.Value;
        vehicleAudio.load = load;

       
       var traction =vehicleController.traction.Value;

        var accellTraction = 1f - Mathf.Clamp01(vehicleController.MotorWheelsSlip.Value);
        var brakeTraction = 1f - Mathf.Clamp01(-traction);

        roadAudio.accellTraction = accellTraction;
        roadAudio.brakeTraction = brakeTraction;
        roadAudio.speed = vehicleController.CurrentSpeed.Value;
        roadAudio.surface = vehicleController.CurrentSurface.Value;

        if(vehicleController.CurrentSurface.Value != RoadSurface.Airborne && lastSurface != vehicleController.CurrentSurface.Value)
        {
            lastSurface = vehicleController.CurrentSurface.Value;
            PlaySurfaceBump();
        }

        windAudio.speed = vehicleController.CurrentSpeed.Value;

    }

    void OnCollisionEnter(Collision collision)
    {
        collisionAudio.PlayCollision(collision.contacts[0].point, collision.relativeVelocity.magnitude);       
    }

}
