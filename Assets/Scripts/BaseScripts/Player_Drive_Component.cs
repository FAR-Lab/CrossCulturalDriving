using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

public struct Player_Drive_Component : IComponentData {
    public float maxMotorTorque;
    public float maxBrakeTorque;
    public float maxSteerAngle;
    public float maxSpeed;
    public float maxAcceleration;

    public float3 currentVelocity;
    public float currentSpeed;

    public float speedParameter;
    public float brakeParameter;
    public float steerParameter;

    public bool gasPedal;
    public float steeringWheelAngle;
    /*public float scenarioNum;
    public bool carStarted;
    can be used for GM control and in-scenario updates to instructions*/
}
