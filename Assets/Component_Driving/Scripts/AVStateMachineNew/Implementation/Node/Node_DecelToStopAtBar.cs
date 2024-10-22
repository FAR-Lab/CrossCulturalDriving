using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node_DecelToStopAtBar : SO_FSMNodeAction 
{
    public float barDistanceFromCenter = 10f; 

    private float decel;

    public override void OnEnter(SC_AVContext context) {
    }

    public override void OnExit(SC_AVContext context) {
    }

    public override void OnUpdate(SC_AVContext context) {
        float currentSpeed = context.GetSpeed();

        float distanceToBar = context.GetDistanceToCenter(context.MyCtrl) - barDistanceFromCenter;

        if (distanceToBar <= 0) {
            Debug.Log("Reached or passed the bar, setting speed to 0.");
            context.SetSpeed(0);  
            return;
        }

        decel = (currentSpeed * currentSpeed) / (2 * distanceToBar);

        decel = Mathf.Abs(decel);

        float newSpeed = currentSpeed - decel * Time.deltaTime;

        newSpeed = Mathf.Max(newSpeed, 0);
        
        context.SetSpeed(newSpeed);
    }
}