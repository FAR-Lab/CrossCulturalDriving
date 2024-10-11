using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SO_CompoundCondition : SO_TransitionCondition
{
    public SO_TransitionCondition[] conditions;
    public bool invertResult;
    
    public override bool IsConditionMet(SC_AVContext context) {
        bool result = true;

        foreach (var condition in conditions) {
            if (!condition.IsConditionMet(context)) {
                result = false;
                break;
            }
        }

        if (invertResult) {
            result = !result;
        }

        return result;
    }
}
