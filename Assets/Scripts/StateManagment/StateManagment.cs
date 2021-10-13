using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ClientState { HOST, CLIENT, DISCONECTED, NONE };

public enum ActionState {DEFAULT, WAITINGROOM, LOADING, READY, DRIVE, QUESTIONS, POSTQUESTIONS };

public enum ServerState { NONE, LOADING, WAITING, RUNNING }




public enum ParticipantOrder : byte
{
    A=(byte) 'a',
    B=(byte) 'b',
    C=(byte) 'c',
    D=(byte) 'd',
    E=(byte) 'e',
    F=(byte) 'f',
    None=(byte) '-'
};