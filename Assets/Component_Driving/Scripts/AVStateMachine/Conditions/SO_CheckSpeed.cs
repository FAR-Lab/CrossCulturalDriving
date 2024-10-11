using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SO_CheckSpeed : SO_TransitionCondition
{
    public float speedThreshold = 1.98f;

    public enum Comparison
    {
        LessThan,
        GreaterThan
    }
    
    public Comparison comparison;
    
    public override bool IsConditionMet(SC_AVContext context) {
        float speed;
        
        speed = context.GetSpeed();
        Debug.Log("Speed: " + speed);
   

        if (comparison == Comparison.LessThan) {
            return speed < speedThreshold;
        }
        else {
            return speed > speedThreshold;
        }
    }
}
