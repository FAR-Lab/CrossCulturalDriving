using UnityEngine;

public abstract class SO_FSMTransitionCheck : ScriptableObject
{
    public abstract bool IsConditionMet(SC_AVContext context);
}