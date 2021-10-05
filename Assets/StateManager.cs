using MLAPI;
using MLAPI.NetworkVariable;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateManager : NetworkBehaviour
{
    private static StateManager _instance;
    public static StateManager Instance { get { return _instance; } }

    public NetworkVariable<ActionState> GlobalState = new NetworkVariable<ActionState>(
        new NetworkVariableSettings {WritePermission = NetworkVariablePermission.ServerOnly,ReadPermission  = NetworkVariablePermission.Everyone});
    private ActionState _state = ActionState.WAITING;
    public ActionState InternalState => _state;

   void ServerStateChange(ActionState prevF, ActionState newF){
       Debug.Log("ChangeingStates");
   }
   
   
    private void Awake() {
        if (_instance != null && _instance != this) {
            Destroy(this.gameObject);
        } else {
            _instance = this;
            // DontDestroyOnLoad(this);
        }
    }
    void Start()
    {
        GlobalState.OnValueChanged += ServerStateChange;
        
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
