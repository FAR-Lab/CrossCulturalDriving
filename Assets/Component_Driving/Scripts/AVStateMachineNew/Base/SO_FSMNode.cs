using System.Collections.Generic;
using UnityEngine;

public class SO_FSMNode : ScriptableObject
{
    public SO_FSMNodeAction Action;

    public List<SO_FSMTransition> Transitions = new List<SO_FSMTransition>();
    
    public SO_FSMNode CheckTransitions(SC_AVContext context)
    {
        foreach (var transition in Transitions)
        {
            if (transition.IsConditionMet(context))
            {
                return transition.targetNode;
            }
        }
        return null;
    }
}