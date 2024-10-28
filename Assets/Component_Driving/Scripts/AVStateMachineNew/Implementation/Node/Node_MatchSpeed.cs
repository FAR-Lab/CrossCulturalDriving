using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node_MatchSpeed : SO_FSMNodeAction
{
    public override void OnEnter(SC_AVContext context) {
    }

    public override void OnExit(SC_AVContext context) {
    }

    public override void OnUpdate(SC_AVContext context) {
        float otherSpeed = context.OtherRb.velocity.magnitude;
        
        context.SetSpeed(otherSpeed);
    }
}
