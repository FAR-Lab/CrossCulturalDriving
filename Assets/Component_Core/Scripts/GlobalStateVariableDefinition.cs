using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public static class ComponentExtensions
{
    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }
        return component;
    }
}

public enum ActionState
{
    DEFAULT,
    WAITINGROOM,
    LOADINGSCENARIO,
    LOADINGVISUALS,
    READY,
    DRIVE,
    QUESTIONS,
    POSTQUESTIONS,
    RERUN
};

[Serializable]
public struct ParticipantConfig
{
    public string ServerIPString;
    public string ParticipantIDString;
    public string LanguageString;
    public string SpawnTypeString;
}

public enum ParticipantOrder : byte {
    A = (byte) 'a',
    B = (byte) 'b',
    C = (byte) 'c',
    D = (byte) 'd',
    E = (byte) 'e',
    F = (byte) 'f',
    None = (byte) '-'
};

public enum Language {
    English, 
    Hebrew, 
    Chinese, 
    German
}

public enum JoinType {
    SERVER,
    SCREEN,
    VR,
    ROBOT,
    INTERACTIONTESTING
}


public enum SpawnType {
    NONE,
    CAR,
    PEDESTRIAN,
    PASSENGER,
    ROBOT
}

public struct FElem
{
    public static readonly ParticipantOrder[] AllPossibleParticipant =
    {
        ParticipantOrder.A, ParticipantOrder.B, ParticipantOrder.C, ParticipantOrder.D, ParticipantOrder.E,
        ParticipantOrder.F
    };
}