using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SO_DistanceToCenter : SO_TransitionCondition
{
    public float distanceThreshold = 30f;

    public enum Party
    {
        Self,
        Other
    }
    
    public Party party;
    
    public enum Comparison
    {
        LessThan,
        GreaterThan
    }
    
    public Comparison comparison;
    
    public override bool IsConditionMet(SC_AVContext context) {
        float distance;
        
        if (party == Party.Self) {
            distance = context.GetDistanceToCenter(context.GetMyNetworkVehicleController());
        }
        else {
            distance = context.GetDistanceToCenter(context.GetOtherNetworkVehicleController());
        }

        if (comparison == Comparison.LessThan) {
            return distance < distanceThreshold;
        }
        else {
            return distance > distanceThreshold;
        }
    }
}