using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node_KeepSpeedSameAsOther : SO_FSMNodeAction
{
    public override void OnEnter(SC_AVContext context) {
        throw new System.NotImplementedException();
    }

    public override void OnExit(SC_AVContext context) {
        throw new System.NotImplementedException();
    }

    public override void OnUpdate(SC_AVContext context) {
        var otherRb = context.OtherRb;
        
        var otherSpeed = otherRb.velocity.magnitude;
        
        context.SetSpeed(otherSpeed);

    }
}
