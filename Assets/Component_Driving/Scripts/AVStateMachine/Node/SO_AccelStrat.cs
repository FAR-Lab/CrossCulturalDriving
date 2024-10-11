using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SO_AccelStrat : SO_FSMNode
{
    public float max_accel = 0.7418f; 
    public float max_speed = 6.3637f;
    public float delta = 0.8403f;    

    public override void OnEnter(SC_AVContext context)
    {
        Debug.Log("FSM: Entering Acceleration Strategy Node");
    }

    public override void OnExit(SC_AVContext context)
    {
        Debug.Log("FSM: Exiting Acceleration Strategy Node");
    }

    public override void OnUpdate(SC_AVContext context)
    {
        float currentSpeed = context.GetSpeed();

        float acceleration = max_accel * (1 - Mathf.Pow(currentSpeed / max_speed, delta));

        float newSpeed = currentSpeed + acceleration * Time.deltaTime;

        context.SetSpeed(newSpeed);

        Debug.Log($"Current Speed: {currentSpeed}, New Speed: {newSpeed}, Acceleration: {acceleration}");
    }
}