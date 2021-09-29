using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ClientState { HOST, CLIENT, DISCONECTED, NONE };

public enum ActionState { LOADING, PREDRIVE, READY, DRIVE, QUESTIONS, POSTQUESTIONS, WAITING };

public enum ServerState { NONE, LOADING, WAITING, RUNNING }