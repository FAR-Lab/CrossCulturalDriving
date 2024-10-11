using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SO_ConflictIsClear : SO_TransitionCondition
{
    public override bool IsConditionMet(SC_AVContext context)
    {
        return context.IsConflictZoneClear();
    }
}
