using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SO_TransitionCondition : ScriptableObject
{
    public SO_FSMNode targetNode;
    public virtual bool IsConditionMet(SC_AVContext context){
        return false;
    }
}
