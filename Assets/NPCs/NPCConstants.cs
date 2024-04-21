using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCConstants : MonoBehaviour
{
    public enum NPCState {
        Move,
        CrossStreet,
        Pause,
        Idle,
        Finished
    }
    
    public enum NPCMoveMode {
        Walk,
        Run,
        Wander
    }
    
}
