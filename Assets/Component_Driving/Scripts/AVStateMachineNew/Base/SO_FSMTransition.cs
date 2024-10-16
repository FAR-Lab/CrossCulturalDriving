using UnityEngine;

public class SO_FSMTransition : ScriptableObject
{
    public SO_FSMTransitionCheck condition;
    public SO_FSMNode targetNode;

    public bool InverseCondition = false;
    
    public bool IsConditionMet(SC_AVContext context)
    {
        bool ret = condition.IsConditionMet(context);
        
        if (InverseCondition)
        {
            return !ret;
        }
        else
        {
            return ret;
        }
    }
}