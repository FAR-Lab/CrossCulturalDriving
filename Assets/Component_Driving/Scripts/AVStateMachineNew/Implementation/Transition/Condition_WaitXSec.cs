using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Condition_WaitXSec : SO_FSMTransitionCheck
{
    public float timeToPass = 1f;

    public float timePassed = 0f;

    public override bool IsConditionMet(SC_AVContext context)
    {
        timePassed += Time.deltaTime;
        if (timePassed >= timeToPass)
        {
            timePassed = 0f;
            return true;
        }
        else{
            return false;
        }
    }
}