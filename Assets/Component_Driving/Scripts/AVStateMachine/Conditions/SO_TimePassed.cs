using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SO_TimePassed : SO_TransitionCondition
{
    public bool UseSO = false;

    public SO_Float timeToPassSO;
    public float timeToPass = 1f;

    public float timePassed = 0f;

    public override bool IsConditionMet(SC_AVContext context)
    {
        float localTimeToPass = UseSO ? timeToPassSO.value : timeToPass;

        timePassed += Time.deltaTime;
        if (timePassed >= localTimeToPass)
        {
            timePassed = 0f;
            return true;
        }
        else{
            return false;
        }
    }
}
