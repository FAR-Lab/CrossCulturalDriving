using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.SceneManagement;

public class ConnectionAndSpawing : MonoBehaviour
{

    public List<SceneField> IncludedScenes = new List<SceneField>();
    public string WaitingRoomSceneName;
    public bool ServerisRunning;
    private GameObject myStateManager;
    private ParticipantOrder _participantOrder = ParticipantOrder.None;
    public ParticipantOrder ParticipantOrder => _participantOrder;
    //Internal StateTracking
    private bool ParticipantOrder_Set = false;

    private ScenarioManager CurrentScenarioManager;

    public ActionState ServerState;// { get; private set; }
    
    #region ParticipantMapping
    private Dictionary<ParticipantOrder,ulong> _OrderToClient;
    private Dictionary<ulong,ParticipantOrder> _ClientToOrder;

    private Dictionary<ulong, List<NetworkObject>> ClientObjects = new Dictionary<ulong, List<NetworkObject>>();


    private bool SceneSwitching = false;
    private bool SceneSwitchingFinished = false;
    private bool initalSceneLoaded = false;
        
    private bool AddParticipant(ParticipantOrder or ,ulong id)
    {
        bool outval = false;
        if (_OrderToClient == null)
        {
            initDicts();
        }
        if (!_OrderToClient.ContainsKey(or))
        {
            _OrderToClient.Add(or, id);
            _ClientToOrder.Add(id,or);
            
            
               ClientObjects.Add(id, new List<NetworkObject>());
            

            outval = true;
        }
        return outval;
    }
    private void RemoveParticipant(ulong id)
    {
        ParticipantOrder or = GetOrder(id);
        if (_OrderToClient.ContainsKey(or) &&_ClientToOrder.ContainsKey(id))
        {
            _OrderToClient.Remove(or);
            _ClientToOrder.Remove(id);
            ClientObjects.Remove(id);
        }
       
    }

    private void initDicts()
    {
        _OrderToClient=new Dictionary<ParticipantOrder, ulong>();
        _ClientToOrder=new Dictionary<ulong,ParticipantOrder>();
        
    }

    private ulong? GetClientID(ParticipantOrder or)
    {if (_OrderToClient == null)
        {
            initDicts();
        }
        if (CheckOrder(or))
        {
            return _OrderToClient[or];
        }
        else
        {
            return null;
        }
    }
    private bool CheckOrder(ParticipantOrder or)
    {if (_OrderToClient == null)
        {
            initDicts();
        }
        return _OrderToClient.ContainsKey(or);
    }
    private bool CheckClientID(ulong id)
    {if (_OrderToClient == null)
        {
            initDicts();
        }
        return _ClientToOrder.ContainsKey(id);
    }
    
    private ParticipantOrder GetOrder(ulong id)
    {if (_OrderToClient == null)
        {
            initDicts();
        }
        if (CheckClientID(id))
        {
            return _ClientToOrder[id];
        }
        else
        {
            return ParticipantOrder.None;
        }
    }

    private int GetParticipantCount()
    {
        if (_ClientToOrder.Count == _OrderToClient.Count)
        {
            return _ClientToOrder.Count;
        }
        else
        {
            Debug.LogError("Our Participant Connection has become inconsistent. This is bad. Please restart and tell david!");
            return -1;
        }
    }
    #endregion
    
    public void SetParticipantOrder(ParticipantOrder val)
    {
        Debug.Log(val+"  " + (byte) val);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = new byte[] {(byte) val};  // assigning ID 
        _participantOrder = val;
        ParticipantOrder_Set = true;
    }
    
    #region SingeltonManagment

   
    public static ConnectionAndSpawing Singleton { get; private set; }
    private void SetSingleton()
    {
        Singleton = this;

    }

    private void OnEnable()
    {
        if (Singleton != null && Singleton != this)
        {
            Destroy(gameObject);
            return;
        }
        SetSingleton();
        DontDestroyOnLoad(gameObject);
      
    }

    private void OnDestroy()
    {
      
        if (Singleton != null && Singleton == this)
        {
           
            Singleton = null;
        }
    }
    #endregion
    
    
    #region SpawingAndConnecting

    

    
    private void SetupConnectingAndSpawing()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
       NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
       NetworkManager.Singleton.OnServerStarted += ServerHasStarted;

       NetworkSceneManager.OnSceneSwitchStarted += SceneLoading;
       NetworkSceneManager.OnSceneSwitched += SceneIsLoaded;
       Debug.Log("Set up server Callbacks");
    }

    private void SceneLoading(AsyncOperation operation)
    {
        SceneSwitchingFinished = false;
       
    }

    private void SceneIsLoaded()
    {
       
        foreach (ulong ClientID  in _ClientToOrder.Keys)
        {
            var tmp=SpawnAPlayer(ClientID);
            if (tmp == false)
            {
                Debug.LogError("Could not spawn a player!");
            }
        }

        if (ServerState == ActionState.LOADING)
        {
            SwitchToReady();
        }

        SceneSwitchingFinished = true;
    }

    private void DestroyAllClientObjects()
    {
        
        foreach (ulong id in ClientObjects.Keys)
        {
            foreach (NetworkObject no in ClientObjects[id])
            {
                
                if (NetworkManager.Singleton.ConnectedClients[id].PlayerObject == no)
                {
                    NetworkManager.Singleton.ConnectedClients[id].PlayerObject = null;
                    Debug.Log("Removing player object despanwn: "+no.name);
                }
                else
                {
                    Debug.Log("Trying to despanwn: "+no.name);
                }
                no.Despawn(true);
                
            }
            ClientObjects[id].Clear();
            
        }
    }

    private void ClientDisconnected(ulong ClientID)
    {
        foreach (NetworkObject obj in ClientObjects[ClientID])
        {
            obj.Despawn(true);
        }
        RemoveParticipant(ClientID);
    }

    private void ClientConnected(ulong ClientID)
    {
        if (! NetworkManager.Singleton.IsServer) return;
      if(SceneSwitchingFinished) SpawnAPlayer(ClientID);
         
    }

    private bool SpawnAPlayer(ulong ClientID)
    {
        Debug.Log("trying to spawn a player for"+ClientID);
        ParticipantOrder temp = GetOrder(ClientID);
        if (temp == ParticipantOrder.None)
        {
            return false;
            
        }
        else
        {
            Pose? tempPose = GetScenarioManager().GetStartPose(temp);
            if (tempPose == null)
            {
                return false;
            }
            
            var newPlayer =
                Instantiate(NetworkManager.Singleton.NetworkConfig.NetworkPrefabs[0].Prefab,
                    tempPose.Value.position, tempPose.Value.rotation);
            
            var newCar =
                Instantiate(NetworkManager.Singleton.NetworkConfig.NetworkPrefabs[1].Prefab,
                    tempPose.Value.position, tempPose.Value.rotation);
            newCar.name = "XE_Rigged_Networked" +  GetOrder(ClientID) ;
            
            newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(ClientID, null, false);
            newCar.GetComponent<NetworkObject>().Spawn();
            
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[]{ClientID}
                }
            };

            
            newPlayer.GetComponent<ParticipantInputCapture>().AssignCarLocalServerCall(newCar.GetComponent<VehicleInputControllerNetworked>());
            
            Debug.Log("Assigning car to a new partcipant with clinetID:"+ClientID.ToString());
            newPlayer.GetComponent<ParticipantInputCapture>()
                .AssignCarClientRPC(newCar.GetComponent<NetworkObject>().NetworkObjectId,clientRpcParams);
            
            ClientObjects[ClientID].Add(newPlayer.GetComponent<NetworkObject>());
            ClientObjects[ClientID].Add(newCar.GetComponent<NetworkObject>());
            
            return true;
        }
    }

    private ScenarioManager GetScenarioManager()
    {
        CurrentScenarioManager = FindObjectOfType<ScenarioManager>();
        if (CurrentScenarioManager == null)
        {
            Debug.LogError("Tried to find a scenario manager(probably to spawn cars), but they was nothing. Did you load your scenario(subscene?");
        }
        return CurrentScenarioManager;

    }

    private void ApprovalCheck(byte[] connectionData, ulong clientId,
        MLAPI.NetworkManager.ConnectionApprovedDelegate callback)
    {
        Debug.Log("Adding a player " + connectionData);
        //Your logic here
        bool approve = false;
        ParticipantOrder temp = (ParticipantOrder) connectionData[0];
       
        approve = AddParticipant(temp, clientId);

        if (!approve)
        {
            Debug.Log("Participant Order " + connectionData +
                  " tried to join, but we already have a participant with that order. " +
                  "Try to change the -po commandline argument of the participant that is" +
                  " trying to connect.");
               
        }
        callback(false, 0, approve,null, null);

        
    }
    #endregion
    
    
    public void StartAsHost()
    {
        
        SetupConnectingAndSpawing();
        NetworkManager.Singleton.StartHost();
        AddParticipant(_participantOrder, NetworkManager.Singleton.LocalClientId);
        //SpawnAPlayer(NetworkManager.Singleton.LocalClientId);
        
    }

    public void StartAsClient()
    {
        NetworkManager.Singleton.StartClient();
        Destroy(this);
    }

    private void ServerHasStarted()
    {
        
        ServerisRunning = true;
        SwitchToWaitingRoom();
    }

    #region StateChangeCalls

    private void SwitchToWaitingRoom()
    {
        ServerState = ActionState.WAITINGROOM;
        SceneSwitching = true;
        NetworkSceneManager.SwitchScene(WaitingRoomSceneName);
    }
    
    private void SwitchToLoading(string name)
    {
        DestroyAllClientObjects();
        ServerState = ActionState.LOADING;
        SceneSwitching = true;
        NetworkSceneManager.SwitchScene(name);
    }
    private void SwitchToReady()
    {
        ServerState = ActionState.READY;
    }
    private void SwitchToDriving()
    {
        ServerState = ActionState.DRIVE;
    }
    public void SwitchToQN()
    {
        Debug.Log("QN triggered, canceling Velocities, and start Questionnaires");
        ServerState = ActionState.QUESTIONS;
        QNFinished = new Dictionary<ParticipantOrder, bool>();
        foreach(ParticipantOrder po in _OrderToClient.Keys)
        {
            QNFinished.Add(po,false);
        }
        
        foreach (ulong client in ClientObjects.Keys)
        {
            foreach(VehicleInputControllerNetworked  no in FindObjectsOfType<VehicleInputControllerNetworked>())
            {
                
                    no.GetComponent<Rigidbody>().velocity=Vector3.zero;
                    no.GetComponent<Rigidbody>().angularVelocity=Vector3.zero;
                   
            }
        }

        foreach (ulong clinet in NetworkManager.Singleton.ConnectedClients.Keys)
        {
            ParticipantInputCapture inCapture =
                NetworkManager.Singleton.ConnectedClients[clinet].PlayerObject.GetComponent<ParticipantInputCapture>();
            if (inCapture != null)
            {
                inCapture.StartQuestionnaireClientRpc();
            }
        }
    }

   private void SwitchToPostQN()
   {
       ServerState = ActionState.POSTQUESTIONS;
       DestroyAllClientObjects();
       SwitchToWaitingRoom();

   }

    #endregion
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (NetworkManager.Singleton.IsServer)
        {
           
            if(Input.GetKeyUp(KeyCode.Return) && ServerState == ActionState.READY)
            {
                SwitchToDriving();
            }

            if (ServerState == ActionState.QUESTIONS)
            {
                if (!QNFinished.ContainsValue(false))
                {
                    
                    SwitchToPostQN();
                }
            }
        }
        
    }



    void OnGUI()
    {

        if (NetworkManager.Singleton.IsHost)
        {
            GUI.Label(new Rect(5, 5, 150, 50), "Server: " + ParticipantOrder + " " +
                                                NetworkManager.Singleton.ConnectedClients.Count + " " +
                                                GetParticipantCount() + "  " +
                                                ServerState+"  "+Time.timeScale);
            if (ServerState == ActionState.WAITINGROOM)
            {
                int y=50;
                foreach(SceneField f in IncludedScenes){
                    if (GUI.Button(new Rect(5, 5 + y, 150, 25), f.SceneName))
                    {
                        SwitchToLoading(f.SceneName);
                    }
                    y += 27;
                }
            }
            
            else if (ServerState == ActionState.QUESTIONS)
            {
                int y=50;
                foreach(ParticipantOrder f in QNFinished.Keys)
                {
                    GUI.Label(new Rect(5, 5 + y, 150, 25), f + "  "+QNFinished[f].ToString());
                    y += 27;
                }
            }

        }
        else if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
        {
            GUI.Label(new Rect(5, 5, 150, 100), "Client: " +
                                                ParticipantOrder + " " +
                                                NetworkManager.Singleton.IsConnectedClient);
        }
        else
        {
            if (GUI.Button(new Rect(5, 105, 150, 50), "StartHost"))
            {
                StartAsHost();
            }

        }
    }

    private Dictionary<ParticipantOrder, bool> QNFinished;
    public void FinishedQuestionair(ulong clientID)
    {
        ParticipantOrder po = GetOrder(clientID);
        QNFinished[po] = true;
    }
}


/*
switch (GlobalState.Value)
{
    case ActionState.DEFAULT:
        if (ConnectionAndSpawing.Singleton.ServerisRunning)
        {
            NetworkSceneManager.SwitchScene(WaitingRoomSceneName);
            GlobalState.Value = ActionState.WAITINGROOM;
        }
        break;
    case ActionState.WAITINGROOM:
        DontDestroyOnLoad(gameObject);
        break;
    case ActionState.LOADING:
        break;
    case ActionState.READY:
        break;
    case ActionState.DRIVE:
        break;
    case ActionState.QUESTIONS:
        break;
    case ActionState.POSTQUESTIONS:
        break;
    default:
        throw new ArgumentOutOfRangeException();
}*/