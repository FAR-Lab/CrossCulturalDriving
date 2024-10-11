using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SO_FSMNode : ScriptableObject
{
    public abstract void OnEnter(SC_AVContext context);
    public abstract void OnExit(SC_AVContext context);
    public abstract void OnUpdate(SC_AVContext context);

    public SO_TransitionCondition[] transitionConditions;

    public SO_FSMNode CheckTransitions(SC_AVContext context)
    {
        foreach (var condition in transitionConditions)
        {
            if (condition.IsConditionMet(context))
            {
                SO_FSMNode targetNode = condition.targetNode;
                return targetNode;
            }
        }
        return null; 
    }
}
