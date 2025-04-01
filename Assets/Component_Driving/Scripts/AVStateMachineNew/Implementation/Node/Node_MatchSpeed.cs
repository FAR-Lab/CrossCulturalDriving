using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Node_MatchSpeed : SO_FSMNodeAction {
    // compensate PID
    public float M = 1;

    public float C = 8;
    
    public override void OnEnter(SC_AVContext context) {
    }

    public override void OnExit(SC_AVContext context) {
    }

    public override void OnUpdate(SC_AVContext context) {
        float otherSpeed = context.OtherRb.velocity.magnitude;

        float F = otherSpeed * M;
        
        if (F > C)
        {
            F = C;
        }
        
        context.SetSpeed(F);
    }
}
