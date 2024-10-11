using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SO_DistanceBetween : SO_TransitionCondition
{
    public float distanceThreshold = 30f;

    public override bool IsConditionMet(SC_AVContext context)
    {
        float distance = context.GetDistanceBetween(context.GetMyNetworkVehicleController(), context.GetOtherNetworkVehicleController());
        return distance < distanceThreshold;
    }
}
