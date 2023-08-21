using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


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
    ROBOT
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
    public static readonly ParticipantOrder[] AllPossPart =
    {
        ParticipantOrder.A, ParticipantOrder.B, ParticipantOrder.C, ParticipantOrder.D, ParticipantOrder.E,
        ParticipantOrder.F
    };
}

