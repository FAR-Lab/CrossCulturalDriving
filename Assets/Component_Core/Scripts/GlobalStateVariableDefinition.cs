using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ActionState {
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


public enum ParticipantOrder : byte {
    A = (byte) 'a',
    B = (byte) 'b',
    C = (byte) 'c',
    D = (byte) 'd',
    E = (byte) 'e',
    F = (byte) 'f',
    None = (byte) '-'
};



public struct FElem
{
    public static readonly ParticipantOrder[] AllPossPart =
    {
        ParticipantOrder.A, ParticipantOrder.B, ParticipantOrder.C, ParticipantOrder.D, ParticipantOrder.E,
        ParticipantOrder.F
    };
}


/*
public static class TextExtension
{
    public static SafeBu(this TextAsset val)
    {
        
    }
}

*/