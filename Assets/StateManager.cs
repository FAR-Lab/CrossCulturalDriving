using System;


using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;


using UnityEngine.SceneManagement;

using UnityEngine;


public class StateManager : NetworkBehaviour {
    public NetworkVariable<ActionState> GlobalState =
        new NetworkVariable<ActionState>(NetworkVariableReadPermission.Everyone);
    
    

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
            if (GlobalState.Value != ConnectionAndSpawing.Singleton.ServerState)
            {
                GlobalState.Value = ConnectionAndSpawing.Singleton.ServerState;
            }
        }
    }

    
    
    

   
}
