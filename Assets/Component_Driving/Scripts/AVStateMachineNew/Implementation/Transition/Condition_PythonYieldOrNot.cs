using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Condition_PythonYieldOrNot : SO_FSMTransitionCheck
{
    public override bool IsConditionMet(SC_AVContext context) {
        return context.ShouldYield();
    }
}
