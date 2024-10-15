using UnityEngine;

public class SO_FSMTransition : ScriptableObject
{
    public SO_FSMTransitionCheck condition;
    public SO_FSMNode targetNode;

    public bool IsConditionMet(SC_AVContext context)
    {
        if (condition != null)
        {
            return condition.IsConditionMet(context);
        }
        return false;
    }
}