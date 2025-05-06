/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using UnityEngine.Audio;

public class VehicleAudioController : MonoBehaviour {
    public VehicleAudio vehicleAudio;
    public RoadAudio roadAudio;
    public CollisionAudio collisionAudio;
    public IgnitionAudio ignitionAudio;
    public WindAudio windAudio;

    public AudioMixerSnapshot driveSnapshot;
    public AudioMixerSnapshot selectSnapshot;

    private RoadSurface lastSurface = RoadSurface.Airborne;

    private float load;

    private NetworkVehicleController vehicleController;

    private void Awake() {
        vehicleController = GetComponent<NetworkVehicleController>();
    }

    private void Update() {
        load = Mathf.Lerp(load, vehicleController.IsShifting.Value ? 0f : vehicleController.accellInput.Value,
            Time.deltaTime * 2f);
        vehicleAudio.rpm = vehicleController.RPM.Value;
        vehicleAudio.load = load;


        var traction = vehicleController.traction.Value;

        var accellTraction = 1f - Mathf.Clamp01(vehicleController.MotorWheelsSlip.Value);
        var brakeTraction = 1f - Mathf.Clamp01(-traction);

        roadAudio.accellTraction = accellTraction;
        roadAudio.brakeTraction = brakeTraction;
        roadAudio.speed = vehicleController.CurrentSpeed.Value;
        roadAudio.surface = vehicleController.CurrentSurface.Value;

        if (vehicleController.CurrentSurface.Value != RoadSurface.Airborne &&
            lastSurface != vehicleController.CurrentSurface.Value) {
            lastSurface = vehicleController.CurrentSurface.Value;
            PlaySurfaceBump();
        }

        windAudio.speed = vehicleController.CurrentSpeed.Value;
    }

    private void OnCollisionEnter(Collision collision) {
        collisionAudio.PlayCollision(collision.contacts[0].point, collision.relativeVelocity.magnitude);
    }

    public void PlayIgnition() {
        selectSnapshot.TransitionTo(0f);
        ignitionAudio.Play(PlayEngine);
    }

    public void PlayEngine() {
        enabled = true;
        driveSnapshot.TransitionTo(3f);
    }

    public void StopEngine() {
        enabled = false;
        selectSnapshot.TransitionTo(0f);
    }

    public void PlaySurfaceBump() {
        roadAudio.PlaySurfaceBump();
    }
}