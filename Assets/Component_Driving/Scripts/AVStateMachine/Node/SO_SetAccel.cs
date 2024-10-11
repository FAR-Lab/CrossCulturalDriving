using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SO_SetAccel : SO_FSMNode
{
    float accel = 0.1f;


    public override void OnEnter(SC_AVContext context) {
    }

    public override void OnExit(SC_AVContext context) {
    }

    public override void OnUpdate(SC_AVContext context) {
        context.SetSpeed(context.GetSpeed() + accel);
    }
}
