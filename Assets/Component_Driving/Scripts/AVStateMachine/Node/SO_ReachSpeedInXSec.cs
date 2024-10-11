using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SO_ReachSpeedInXSec : SO_FSMNode
{
    public float targetSpeed = 10f;
    public float timeToReachSpeed = 5f;
    
    public override void OnEnter(SC_AVContext context) {
    }

    public override void OnExit(SC_AVContext context) {
    }

    public override void OnUpdate(SC_AVContext context) {
        float currentSpeed = context.GetSpeed();
        float newSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime / timeToReachSpeed);
        context.SetSpeed(newSpeed); }
}
