using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node_SetSpeedAndSteeringTo0 : SO_FSMNodeAction
{
    public bool SetSpeedTo0 = true;
    public bool SetSteeringTo0 = true;
    public override void OnEnter(SC_AVContext context) {
    }

    public override void OnExit(SC_AVContext context) {
    }

    public override void OnUpdate(SC_AVContext context) {
        if (SetSpeedTo0)
        {
            context.SetSpeed(0);
        }
        if (SetSteeringTo0)
        {
            context.SetSteering(0);
        }
    }
}
