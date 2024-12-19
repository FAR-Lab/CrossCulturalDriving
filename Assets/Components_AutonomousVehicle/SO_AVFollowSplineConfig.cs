using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AVFollowSplineConfig", menuName = "AutonomousVehicle/FollowSplineConfig")]
public class SO_AVFollowSplineConfig : ScriptableObject
{
    public float lookAheadDistance = 5.0f;              
    public float desiredSpeed = 10f;                    

    public float Kp_steering = 1.0f;
    public float Ki_steering = 0.0f;
    public float Kd_steering = 0.1f;

    public float Kp_speed = 0.5f;
    public float Ki_speed = 0.0f;
    public float Kd_speed = 0.1f;
}
