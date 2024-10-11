using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SO_TransitionCondition : ScriptableObject
{
    public SO_FSMNode targetNode;
    public abstract bool IsConditionMet(SC_AVContext context);
}
