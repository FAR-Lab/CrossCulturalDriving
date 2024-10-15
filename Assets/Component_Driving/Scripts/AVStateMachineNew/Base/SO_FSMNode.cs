using System.Collections.Generic;
using UnityEngine;

public abstract class SO_FSMNode : ScriptableObject
{
    public SO_FSMNodeAction action;

    public List<SO_FSMTransition> transitions = new List<SO_FSMTransition>();

    public void OnEnter(SC_AVContext context)
    {
        action.OnEnter(context);
    }
    
    public void OnExit(SC_AVContext context)
    {
        action.OnExit(context);
    }
    
    public void OnUpdate(SC_AVContext context)
    {
        action.OnUpdate(context);
    }
    
    public SO_FSMNode CheckTransitions(SC_AVContext context)
    {
        foreach (var transition in transitions)
        {
            if (transition.condition.IsConditionMet(context))
            {
                return transition.targetNode;
            }
        }
        return null;
    }
}