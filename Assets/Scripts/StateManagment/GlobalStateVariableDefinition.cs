using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ActionState {
    DEFAULT,
    WAITINGROOM,
    LOADING,
    READY,
    DRIVE,
    QUESTIONS,
    POSTQUESTIONS
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