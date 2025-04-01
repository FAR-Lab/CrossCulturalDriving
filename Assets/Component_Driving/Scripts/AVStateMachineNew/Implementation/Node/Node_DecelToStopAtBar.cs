using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node_DecelToStopAtBar : SO_FSMNodeAction 
{
    public float barDistanceFromCenter = 10f;
    public float TargetSpeed = 0f; 

    private float decel;
    
    

    public override void OnEnter(SC_AVContext context) {
    }

    public override void OnExit(SC_AVContext context) {
    }

    public override void OnUpdate(SC_AVContext context) {
        float currentSpeed = context.GetSpeed();
        float distanceToBar = context.GetDistanceToCenter(context.MyCtrl) - barDistanceFromCenter;

        if (distanceToBar <= 0) {
            Debug.Log("Reached or passed the bar, setting speed to " + TargetSpeed);
            context.SetSpeed(TargetSpeed);
            return;
        }

        if (TargetSpeed == 0f) {
            decel = (currentSpeed * currentSpeed) / (2 * distanceToBar);
        } else {
            decel = (currentSpeed * currentSpeed - TargetSpeed * TargetSpeed) / (2 * distanceToBar);
        }

        float newSpeed = currentSpeed - decel * Time.deltaTime;

        if (currentSpeed > TargetSpeed) {
            newSpeed = Mathf.Max(newSpeed, TargetSpeed);
        } else if (currentSpeed < TargetSpeed) {
            newSpeed = Mathf.Min(newSpeed, TargetSpeed);
        }
        
        newSpeed = Mathf.Max(newSpeed, 0f);
        
        context.SetSpeed(newSpeed);
    }
}