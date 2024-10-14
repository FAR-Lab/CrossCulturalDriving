using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SO_DistanceToCenter : SO_TransitionCondition
{
    public bool UseSO = true;

    public float distanceThreshold = 30f;
    public SO_Float distanceThresholdSO;

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

        var localDistanceThreshold = UseSO ? distanceThresholdSO.value : distanceThreshold;

        if (comparison == Comparison.LessThan) {
            return distance < localDistanceThreshold;
        }
        else {
            return distance > localDistanceThreshold;
        }
    }
}