using System;
using MLAPI;
using MLAPI.NetworkVariable;

using MLAPI.Spawning;
using System.Collections;
using System.Collections.Generic;
using MLAPI.Messaging;
using MLAPI.SceneManagement;
using Unity.Properties;
using UnityEngine.SceneManagement;

using UnityEngine;


public class StateManager : NetworkBehaviour
{
    public NetworkVariable<ActionState> GlobalState = new NetworkVariable<ActionState>(
        new NetworkVariableSettings {WritePermission = NetworkVariablePermission.ServerOnly,ReadPermission  = NetworkVariablePermission.Everyone});
    
    

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
