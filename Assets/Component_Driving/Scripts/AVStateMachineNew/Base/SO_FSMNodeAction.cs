using UnityEngine;

public abstract class SO_FSMNodeAction : ScriptableObject
{
    public abstract void OnEnter(SC_AVContext context);
    public abstract void OnExit(SC_AVContext context);
    public abstract void OnUpdate(SC_AVContext context);
}