using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SO_ConstantSpeed : SO_FSMNode
{
    public float speed = 10f;

    public override void OnEnter(SC_AVContext context)
    {
        context.SetSpeed(speed);
    }

    public override void OnExit(SC_AVContext context)
    {
    }

    public override void OnUpdate(SC_AVContext context)
    {
    }
}
