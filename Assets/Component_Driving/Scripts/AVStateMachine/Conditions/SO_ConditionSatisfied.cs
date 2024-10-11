using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SO_ConditionSatisfied : SO_TransitionCondition
{
    public bool conditionSatisfied = true;
    
    public override bool IsConditionMet(SC_AVContext context)
    {
        return conditionSatisfied;
    }
}
