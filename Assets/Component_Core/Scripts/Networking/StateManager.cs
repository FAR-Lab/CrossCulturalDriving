using System;


using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;


public class StateManager : NetworkBehaviour {
    public NetworkVariable<ActionState> GlobalState =
        new NetworkVariable<ActionState>();
    
    

   void ServerStateChange(ActionState prevState, ActionState newState){
       
   }
   void Start()
    {
        GlobalState.OnValueChanged += ServerStateChange;
    }

    // Update is called once per frames
    void Update()
    {
        if (IsServer)
        {
            if (GlobalState.Value != ConnectionAndSpawning.Singleton.ServerState)
            {
                GlobalState.Value = ConnectionAndSpawning.Singleton.ServerState;
            }
        }
    }

    
    
    

   
}
