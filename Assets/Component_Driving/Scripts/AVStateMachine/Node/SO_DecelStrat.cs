using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SO_DecelStrat : SO_FSMNode
{
    public float decel_limit = -0.5821f;  // Maximum deceleration (negative value)
    public float decel_delta = 0.6014f;   // Deceleration decay exponent
    public float targetDistance = 15.6831f; // Distance towards center to aim for

    public override void OnEnter(SC_AVContext context)
    {
        Debug.Log("FSM: Entering Deceleration Strategy Node");
    }

    public override void OnExit(SC_AVContext context)
    {
        Debug.Log("FSM: Exiting Deceleration Strategy Node");
    }

    public override void OnUpdate(SC_AVContext context)
    {
        float currentSpeed = context.GetSpeed();

        // Calculate the distance to the target (center position)
        float distanceToTarget = context.GetDistanceToCenter(context.GetMyNetworkVehicleController());

        // Invert the ratio to prioritize deceleration when far from the target
        float ratio = Mathf.Clamp01(targetDistance / distanceToTarget);

        // Calculate the deceleration using the adjusted formula
        float deceleration = decel_limit * (1 - Mathf.Pow(ratio, decel_delta));

        Debug.Log($"Deceleration: {deceleration}, Distance to Target: {distanceToTarget}");

        // Update the speed based on the calculated deceleration
        float newSpeed = currentSpeed + deceleration * Time.deltaTime;

        // Clamp the speed to ensure it does not go below zero
        newSpeed = Mathf.Max(newSpeed, 0);

        // Set the updated speed in the context
        context.SetSpeed(newSpeed);
    }
}