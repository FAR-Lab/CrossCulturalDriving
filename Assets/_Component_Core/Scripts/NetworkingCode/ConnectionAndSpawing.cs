using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rerun;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;


public class ConnectionAndSpawing : MonoBehaviour
{
    public GameObject PlayerPrefab;
    public GameObject CarPrefab;
    public GameObject VRUIStartPrefab;
    public GameObject ref_ServerTimingDisplay;

    public List<SceneField> IncludedScenes = new List<SceneField>();
    public string LastLoadedVisualScene;
    public static string WaitingRoomSceneName = "WaitingRoom";
    public bool ServerisRunning;
    private GameObject myStateManager;


    private Dictionary<SceneField, bool> VisitedScenes = new Dictionary<SceneField, bool>();
    private ParticipantOrder _participantOrder = ParticipantOrder.None;

    public ParticipantOrder ParticipantOrder => _participantOrder;

    //Internal StateTracking
    private bool ParticipantOrder_Set = false;
    private ScenarioManager CurrentScenarioManager;
    private RerunManager m_ReRunManager;


    public ActionState ServerState { get; private set; }
    public string lang { private set; get; }


    //   public bool RunAsServer;
    public static bool fakeCare = false;

    #region ParticipantMapping

    public enum ParticipantObjectSpawnType
    {
        MAIN,
        CAR,
        PEDESTRIAN,
    }


    private Dictionary<ParticipantOrder, ulong> _OrderToClient;
    private Dictionary<ulong, ParticipantOrder> _ClientToOrder;

    private Dictionary<ulong, Dictionary<ParticipantObjectSpawnType, NetworkObject>> ClientObjects =
        new Dictionary<ulong, Dictionary<ParticipantObjectSpawnType, NetworkObject>>();


    private bool initalSceneLoaded = false;

    private bool AddParticipant(ParticipantOrder or, ulong id)
    {
        bool outval = false;
        if (_OrderToClient == null)
        {
            initDicts();
        }

        if (!_OrderToClient.ContainsKey(or))
        {
            _OrderToClient.Add(or, id);
            _ClientToOrder.Add(id, or);


            ClientObjects.Add(id, new Dictionary<ParticipantObjectSpawnType, NetworkObject>());


            outval = true;
        }

        return outval;
    }

    private void RemoveParticipant(ulong id)
    {
        ParticipantOrder or = GetOrder(id);
        if (_OrderToClient.ContainsKey(or) && _ClientToOrder.ContainsKey(id))
        {
            _OrderToClient.Remove(or);
            _ClientToOrder.Remove(id);
            ClientObjects.Remove(id);
        }
    }

    private void initDicts()
    {
        _OrderToClient = new Dictionary<ParticipantOrder, ulong>();
        _ClientToOrder = new Dictionary<ulong, ParticipantOrder>();
        ClientListInitDone = true;
    }

    private ulong? GetClientID(ParticipantOrder or)
    {
        if (_OrderToClient == null)
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
    {
        if (_OrderToClient == null)
        {
            initDicts();
        }

        return _OrderToClient.ContainsKey(or);
    }

    private bool CheckClientID(ulong id)
    {
        if (_OrderToClient == null)
        {
            initDicts();
        }

        return _ClientToOrder.ContainsKey(id);
    }

    private ParticipantOrder GetOrder(ulong id)
    {
        if (_OrderToClient == null)
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
        if (_ClientToOrder == null || _OrderToClient == null)
        {
            return -1;
        }

        if (_ClientToOrder.Count == _OrderToClient.Count)
        {
            return _ClientToOrder.Count;
        }
        else
        {
            Debug.LogError(
                "Our Participant Connection has become inconsistent. This is bad. Please restart and tell david!");
            return -1;
        }
    }

    #endregion


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


    #region SpawningAndConnecting

    void SetupServerFunctionality()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        NetworkManager.Singleton.OnServerStarted += ServerHasStarted;


        NetworkManager.Singleton.StartServer();
        NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneEvent;


        SteeringWheelManager.Singleton.Init();
    }


    private void LocalLoadScene(string name)
    {
        DestroyAllClientObjects(new List<ParticipantObjectSpawnType> {ParticipantObjectSpawnType.CAR});


        NetworkManager.Singleton.SceneManager.LoadScene(name, LoadSceneMode.Single);
    }


    private void LoadSceneVisuals()
    {
        var tmp = GetScenarioManager();
        if (tmp != null && tmp.VisualSceneToUse != null && tmp.VisualSceneToUse.SceneName.Length > 0)
        {
            LastLoadedVisualScene = tmp.VisualSceneToUse.SceneName;
            NetworkManager.Singleton.SceneManager.LoadScene(tmp.VisualSceneToUse.SceneName, LoadSceneMode.Additive);
            ServerState = ActionState.LOADINGVISUALS;
        }
        else
        {
            SwitchToReady();
        }
    }

    private void SceneEvent(SceneEvent sceneEvent)
    {
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.Load: break;
            case SceneEventType.Unload: break;
            case SceneEventType.Synchronize: break;
            case SceneEventType.ReSynchronize: break;
            case SceneEventType.LoadEventCompleted:
                Debug.Log("Load event!" + sceneEvent.ClientId + ServerState.ToString());
                if (ServerState == ActionState.LOADINGSCENARIO)
                {
                    LoadSceneVisuals();
                }
                else if (ServerState == ActionState.LOADINGVISUALS)
                {
                    SwitchToReady();
                }

                break;
            case SceneEventType.UnloadEventCompleted:
                break;
            case SceneEventType.LoadComplete:
                Debug.Log("Load completed!" + sceneEvent.ClientId + ServerState.ToString());
                if (ServerState == ActionState.READY || ServerState == ActionState.LOADINGVISUALS ||
                    ServerState == ActionState.WAITINGROOM)
                {
                    Debug.Log("Trying to Spawn A Car!");
                    StartCoroutine(SpawnACar_Await(sceneEvent.ClientId));
                }
                else
                {
                    Debug.Log("A client Finished loading But we are not gonna Spawn cause the visuals are missing!" +
                              "");
                }

                break;
            case SceneEventType.UnloadComplete: break;
            case SceneEventType.SynchronizeComplete: break;
            default: throw new ArgumentOutOfRangeException();
        }
    }
    // DestroyAllClientObjects_OfType(ParticipantObjectSpawnType TypeToDestroy)// TODO would make implementing more features easier

    private void DestroyAllClientObjects(List<ParticipantObjectSpawnType> TypesToDestroy)
    {
        bool destroyMain = TypesToDestroy.Contains(ParticipantObjectSpawnType.MAIN);
        if (destroyMain)
        {
            TypesToDestroy.Remove(ParticipantObjectSpawnType.MAIN);
        }

        foreach (ulong id in ClientObjects.Keys)
        {
            foreach (ParticipantObjectSpawnType enumval in TypesToDestroy)
            {
                {
                    DespawnObjectOfType(id, enumval);
                }
            }

            if (destroyMain)
            {
                DespawnObjectOfType(id, ParticipantObjectSpawnType.MAIN);
            }
        }
    }

    private void DespawnAllObjectsforParticipant(ParticipantOrder po)
    {
        if (po == ParticipantOrder.None) return;
        ulong clientID = _OrderToClient[po];
        DespawnAllObjectsforParticipant(clientID);
    }


    private void DespawnAllObjectsforParticipant(ulong clientID)
    {
        if (ClientObjects.ContainsKey(clientID) && ClientObjects[clientID] != null)
        {
            List<ParticipantObjectSpawnType> despawnList =
                new List<ParticipantObjectSpawnType>(ClientObjects[clientID].Keys);
            if (despawnList.Contains(ParticipantObjectSpawnType.MAIN))
            {
                despawnList.Remove(ParticipantObjectSpawnType.MAIN);
            }

            foreach (var spawntype in despawnList)
            {
                DespawnObjectOfType(clientID, spawntype);
            }
        }
    }

    private void DespawnObjectOfType(ulong clientID, ParticipantObjectSpawnType oType)
    {
        if (ClientObjects[clientID].ContainsKey(oType))
        {
            Debug.Log("Removing for:" + clientID + " object:" + oType);
            NetworkObject no = ClientObjects[clientID][oType];
            switch (oType)
            {
                case ParticipantObjectSpawnType.MAIN:
                    Debug.Log("WARNING despawing MAIN!");
                    break;
                case ParticipantObjectSpawnType.CAR:

                    if (ClientObjects[clientID][ParticipantObjectSpawnType.MAIN] != null)
                    {
                        ClientObjects[clientID][ParticipantObjectSpawnType.MAIN].GetComponent<ParticipantInputCapture>()
                            .De_AssignCarTransform(clientID);
                    }

                    break;
                case ParticipantObjectSpawnType.PEDESTRIAN: break;
                default: throw new ArgumentOutOfRangeException(nameof(oType), oType, null);
            }

            no.Despawn(true);
            ClientObjects[clientID].Remove(oType);
        }
    }


    public enum ClienConnectionResponse
    {
        SUCCESS,
        FAILED
    };


    private void ClientDisconnected(ulong ClientID)
    {
        if (ClientObjects.ContainsKey(ClientID) && ClientObjects[ClientID] != null)
        {
            DespawnAllObjectsforParticipant(ClientID);
            m_QNDataStorageServer.DeRegisterHandler(GetOrder(ClientID));
            RemoveParticipant(ClientID);
        }
    }

    private void ClientConnected(ulong ClientID)
    {
        Debug.Log("on Client Connect CallBack Was called!");
        // Whats important here is that this doesnt get called 
        SpawnAPlayer(ClientID, true);
    }

    // 2023.8.7 modification: add another out value of ParticipantObjectSpawnType
    private bool _prepareSpawing(ulong clientID, out Pose? tempPose, out ParticipantObjectSpawnType spawnType)
    {
        // get the start pose from senarioManager using clientID -> participantOrder
        ParticipantOrder clientParticipantOrder = GetOrder(clientID);
        bool success = GetScenarioManager().GetStartPose(clientParticipantOrder, out Pose outPose, out ParticipantObjectSpawnType outSpawnType);
        
        tempPose = outPose;
        if (tempPose == null)
        {
            success = false;
        }

        spawnType = outSpawnType;

        return success;
    }


    IEnumerator SpawnACar_Await(ulong clientID)
    {
        ParticipantOrder temp = GetOrder(clientID);
        if (temp != ParticipantOrder.None)
        {
            yield return new WaitUntil(() =>
                ClientObjects[clientID].ContainsKey(ParticipantObjectSpawnType.MAIN) &&
                ClientObjects[clientID][ParticipantObjectSpawnType.MAIN] != null
            );
            SpawnACar_Immediate(clientID);
        }
    }

    private bool SpawnACar_Immediate(ulong clientID)
    {
        ParticipantOrder temp = GetOrder(clientID);
        if (temp == ParticipantOrder.None) return false;

        if (_prepareSpawing(clientID, out Pose? tempPose, out ParticipantObjectSpawnType spawnType))
        {
            // get input capture script of this client
            ParticipantInputCapture clientInputCapture = ClientObjects[clientID][ParticipantObjectSpawnType.MAIN].GetComponent<ParticipantInputCapture>();

            // switch spawnType. If car, instantiate. If pedestrian, set up pedestrian.
            switch (spawnType){
                case ParticipantObjectSpawnType.CAR:
                    var newCar =
                        Instantiate(CarPrefab,
                            tempPose.Value.position, tempPose.Value.rotation);

                    newCar.GetComponent<NetworkObject>().Spawn(true);

                    if (!fakeCare)
                    {
                        newCar.GetComponent<NetworkVehicleController>().AssignClient(clientID, GetOrder(clientID));

                        #if SPAWNDEBUG
                        Debug.Log("Assigning car to a new partcipant with clinetID:" + clientID.ToString() + " =>" +
                                newCar.GetComponent<NetworkObject>().NetworkObjectId);
                        #endif
                        if (ClientObjects[clientID][ParticipantObjectSpawnType.MAIN] != null)
                        {
                            ClientObjects[clientID][ParticipantObjectSpawnType.MAIN].GetComponent<ParticipantInputCapture>()
                                .AssignCarTransform(newCar.GetComponent<NetworkVehicleController>(), clientID);
                        }

                        else
                        {
                            Debug.LogError("Could not find player as I am spawning the CAR. Broken please fix.");
                        }
                    }
                    clientInputCapture.SetMySpawnType(ParticipantObjectSpawnType.CAR);

                    break;

                case ParticipantObjectSpawnType.PEDESTRIAN:
                    clientInputCapture.SetMySpawnType(ParticipantObjectSpawnType.PEDESTRIAN);
                    // maybe use callback here to
                    // 1. turn off position tracking
                    // 2. attach VRHead to avatar head (position only, rotation is for calibration process)
                    break;
            }
            return true;
        }
        return false;
    }

    private bool SpawnAPlayer(ulong clientID, bool persistent)
    {
        ParticipantOrder temp = GetOrder(clientID);
        if (temp == ParticipantOrder.None) return false;

        if (_prepareSpawing(clientID, out Pose? tempPose, out ParticipantObjectSpawnType spawnType))
        {
            tempPose ??= Pose.identity;

            var newPlayer =
                Instantiate(PlayerPrefab,
                    tempPose.Value.position, tempPose.Value.rotation);

            newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, !persistent);
            ClientObjects[clientID].Add(ParticipantObjectSpawnType.MAIN, newPlayer.GetComponent<NetworkObject>());
            m_QNDataStorageServer.SetupForNewRemoteImage(temp);
            return true;
        }

        return false;
    }

    public ScenarioManager GetScenarioManager()
    {
        CurrentScenarioManager = FindObjectOfType<ScenarioManager>();
        if (CurrentScenarioManager == null)
        {
            Debug.LogError(
                "Tried to find a scenario manager(probably to spawn cars), but they was nothing. Did you load your scenario(subscene)?");
        }

        return CurrentScenarioManager;
    }


    // https://docs-multiplayer.unity3d.com/netcode/current/basics/connection-approval
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        //byte[] connectionData, ulong clientId,
        // NetworkManager.ConnectionApprovedDelegate callback){
        bool approve = false;
        ParticipantOrder temp = (ParticipantOrder) request.Payload[0];

        approve = AddParticipant(temp, request.ClientNetworkId);

        if (!approve)
        {
            Debug.Log("Participant Order " + request.Payload[0] +
                      " tried to join, but we already have a participant with that order. ");
        }
        else
        {
            Debug.Log("Client will connect now!");
        }


        response.Approved = approve;
        response.CreatePlayerObject = false;
        response.Pending = false;
        //SpawnAPlayer(request.ClientNetworkId, true);
    }

    #endregion


    public void StartAsServer(string pairName)
    {
        Application.targetFrameRate = 72;
        gameObject.AddComponent<farlab_logger>();
        SteeringWheelManager.Singleton.enabled = true;
        m_QNDataStorageServer = GetComponent<QNDataStorageServer>();
        m_QNDataStorageServer.enabled = true;


        GetComponent<TrafficLightSupervisor>().enabled = true;
        SetupServerFunctionality();
        m_ReRunManager.SetRecordingFolder(pairName);
        Debug.Log("Starting Server for session: " + pairName);


        if (ref_ServerTimingDisplay != null)
        {
            ServerTimeDisplay val = Instantiate(ref_ServerTimingDisplay, Vector3.zero, Quaternion.identity, transform)
                .GetComponent<ServerTimeDisplay>();
            val.StartDisplay(0.5f);
        }
    }

    private QNDataStorageServer m_QNDataStorageServer;

    public delegate void ReponseDelegate(ClienConnectionResponse response);

    private ReponseDelegate ReponseHandler;
    private bool SuccessFullyConnected = false;

    private void ClientDisconnected_client(ulong ClientID)
    {
        //        Debug.Log(SuccessFullyConnected);
        if (SuccessFullyConnected)
        {
            Debug.Log("Quitting due to disconnection.");
            Application.Quit();
        }
        else
        {
            ReponseHandler.Invoke(ClienConnectionResponse.FAILED);
            Debug.Log("Retrying connection");
        }
    }

    private void ClientConnected_client(ulong ClientID)
    {
        if (ClientID != NetworkManager.Singleton.LocalClient.ClientId) return;

        SuccessFullyConnected = true;
        ReponseHandler.Invoke(ClienConnectionResponse.SUCCESS);
        Debug.Log(SuccessFullyConnected + " CHECK HERE");
    }

    void SetupClientFunctionality()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected_client;
        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected_client;
    }


    private void SetParticipantOrder(ParticipantOrder val)
    {
        NetworkManager.Singleton.NetworkConfig.ConnectionData = new byte[] {(byte) val}; // assigning ID
        _participantOrder = val;
        ParticipantOrder_Set = true;
    }

    private void Setlanguage(string lang_)
    {
        lang = lang_;
    }

    public void StartAsClient(string lang_, ParticipantOrder val, string ip, int port, ReponseDelegate result)
    {
        SetupClientFunctionality();
        ReponseHandler += result;
        SetupTransport(ip, port);
        Setlanguage(lang_);
        SetParticipantOrder(val);
        Debug.Log("Starting as Client");

        NetworkManager.Singleton.StartClient();
    }

    private string LoadedScene = "";

    public void LoadSceneReRun(string totalPath)
    {
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
        {
            Debug.LogError("Dont try to load a scene for RERUN while the server is running. Pleas restart the program");
            Application.Quit();
        }

        var fileName = System.IO.Path.GetFileName(totalPath);
        var sceneNameList = fileName.Split('_');
        var sceneName = sceneNameList[0];
        Debug.Log("Scene Name" + sceneName);
        foreach (var v in IncludedScenes)
        {
            Debug.Log(v.SceneName);
        }

        if (IncludedScenes.ConvertAll(x => x.SceneName).Contains(sceneName))
        {
            Debug.Log("Found scene. Loading!");
            if (LoadedScene == sceneName)
            {
                Debug.Log("ReRun scene already loaded continuing!");
                return;
            }

            if (LoadedScene.Length > 0)
            {
                SceneManager.UnloadSceneAsync(LoadedScene);
            }

            LoadedScene = sceneName;
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }
        else
        {
            Debug.LogWarning("Did not find scene. Aborting!");
        }
    }

    public void StartReRun()
    {
        ServerState = ActionState.RERUN;
       // FindObjectOfType<RerunLayoutManager>().?enabled = true;
        m_ReRunManager.RegisterPreLoadHandler(LoadSceneReRun);
        NetworkManager.Singleton.enabled = false;
        GetComponent<OVRManager>().enabled = false;
        FindObjectOfType<RerunGUI>().enabled = true;
        FindObjectOfType<RerunInputManager>().enabled = true;

        SceneManager.sceneLoaded += VisualsceneLoadReRun;


        GetComponent<TrafficLightSupervisor>().enabled = true;
    }

    private void VisualsceneLoadReRun(Scene arg0, LoadSceneMode arg1)
    {
        if (arg0.name == LoadedScene)
        {
            var tmp = GetScenarioManager();
            if (tmp != null && tmp.VisualSceneToUse != null && tmp.VisualSceneToUse.SceneName.Length > 0)
            {
                if (tmp.VisualSceneToUse.SceneName != LastLoadedVisualScene)
                {
                    if (LastLoadedVisualScene.Length > 0)
                    {
                        SceneManager.UnloadSceneAsync(LastLoadedVisualScene);
                    }

                    LastLoadedVisualScene = tmp.VisualSceneToUse.SceneName;
                    SceneManager.LoadScene(tmp.VisualSceneToUse.SceneName, LoadSceneMode.Additive);
                }
            }
        }
    }

    private void SetupTransport(string ip = "127.0.0.1", int port = 7777)
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
            ip,  // The IP address is a string
            (ushort)port // The port number is an unsigned short
        );
    }

    private void ServerHasStarted()
    {
        ServerisRunning = true;
        SwitchToWaitingRoom();
    }

    #region StateChangeCalls

    private void SwitchToWaitingRoom()
    {
        if (m_ReRunManager.IsRecording())
        {
            m_ReRunManager.StopRecording();
            Debug.LogWarning(
                "I stoped Recording as I was loaded back to the Waitingroom. Recording should have stopped at the switch to the Questionnaire stage.");
        }

        ServerState = ActionState.WAITINGROOM;
        LocalLoadScene(WaitingRoomSceneName);
    }

    private void SwitchToLoading(string name)
    {
        ServerState = ActionState.LOADINGSCENARIO;
        LocalLoadScene(name);
        LastLoadedScene = name;
    }

    public string GetLoadedScene()
    {
        return LastLoadedScene;
    }

    private string LastLoadedScene = "";

    // modification 1: Add ServerStateChange deledate
    public delegate void ServerStateChange_delegate(ActionState state);
    public ServerStateChange_delegate ServerStateChange;

    private void SwitchToReady()
    {
        ServerStateChange.Invoke(ActionState.READY);
        

        ServerState = ActionState.READY;
    }


    private void SwitchToDriving()
    {
        if (!farlab_logger.Instance.ReadyToRecord())
        {
            Debug.LogWarning(
                "I was trying to start recording while something else was still storring Data. Try again in a moment!");
            return;
        }

        ServerState = ActionState.DRIVE;

        m_ReRunManager.BeginRecording(LastLoadedScene);
        m_QNDataStorageServer.StartScenario(LastLoadedScene, m_ReRunManager.GetRecordingFolder());
        farlab_logger.Instance.StartRecording(m_ReRunManager, LastLoadedScene, m_ReRunManager.GetRecordingFolder());
    }


    public void SwitchToQN()
    {
        Debug.Log("Stopping Driving and Stopping the recording.");
        m_ReRunManager.StopRecording();

        ServerState = ActionState.QUESTIONS;
        QNFinished = new Dictionary<ParticipantOrder, bool>();
        foreach (ParticipantOrder po in _OrderToClient.Keys)
        {
            QNFinished.Add(po, false);
        }


        foreach (NetworkVehicleController no in FindObjectsOfType<NetworkVehicleController>())
        {
            no.transform.GetComponent<Rigidbody>().velocity = Vector3.zero;
            no.transform.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        }

        foreach (ulong p in ClientObjects.Keys)
        {
            ClientObjects[p][ParticipantObjectSpawnType.MAIN].GetComponent<ParticipantInputCapture>()
                .StartQuestionairClientRPC();
        }

        m_QNDataStorageServer.StartQn(GetScenarioManager(), m_ReRunManager);
        StartCoroutine(farlab_logger.Instance.StopRecording());
    }

    private void ForceBackToWaitingRoom()
    {
        Debug.Log("Forced back to the waiting Room. When running studies please try to avoid this!");
        SwitchToPostQN();
    }

    private void SwitchToPostQN()
    {
        m_QNDataStorageServer.StopScenario(m_ReRunManager);
        if (farlab_logger.Instance.isRecording())
        {
            StartCoroutine(farlab_logger.Instance.StopRecording());
        }

        ServerState = ActionState.POSTQUESTIONS;
        SwitchToWaitingRoom();
    }

    #endregion

    private bool retry = true;

    private void ResponseDelegate(ConnectionAndSpawing.ClienConnectionResponse response)
    {
    }


    void Start()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            // StartAsClient("English", ParticipantOrder.A, "192.168.1.160", 7777, ResponseDelegate);

            Instantiate(VRUIStartPrefab);
            Debug.Log("Started Client");
        }
        else
        {
            GetComponent<StartServerClientGUI>().enabled = true;
        }

        if (FindObjectsOfType<RerunManager>().Length > 1)
        {
            Debug.LogError("We found more than 1 RerunManager. This is not support. Check your Hiracy");
            Application.Quit();
        }

        m_ReRunManager = FindObjectOfType<RerunManager>();
        if (m_ReRunManager == null)
        {
            Debug.LogError("Did not find a ReRunManager. Need exactly 1. Quitting!");
            Application.Quit();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.Space))
            {
                if (Input.GetKeyUp(KeyCode.A))
                {
                    ResetParticipantObject(ParticipantOrder.A, ParticipantObjectSpawnType.CAR);
                }
                else if (Input.GetKeyUp(KeyCode.B))
                {
                    ResetParticipantObject(ParticipantOrder.B, ParticipantObjectSpawnType.CAR);
                }
            }

            switch (ServerState)
            {
                case ActionState.DEFAULT: break;
                case ActionState.WAITINGROOM: break;
                case ActionState.LOADINGSCENARIO:
                case ActionState.LOADINGVISUALS:
                    if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.W))
                    {
                        Debug.LogWarning("Forcing back to Waitingroom from" + ServerState.ToString());
                        ForceBackToWaitingRoom();
                    }

                    break;
                case ActionState.READY:
                    if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.D))
                    {
                        SwitchToDriving();
                        SetStartingGPSDirections();
                    }

                    if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.W))
                    {
                        Debug.Log("Forcing back to Waitingroom from" + ServerState.ToString());
                        ForceBackToWaitingRoom();
                    }

                    break;
                case ActionState.DRIVE:
                    if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.W))
                    {
                        Debug.Log("Forcing back to Waitingroom from" + ServerState.ToString());
                        ForceBackToWaitingRoom();
                    }
                    else if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Q))
                    {
                        SwitchToQN();
                    }

                    break;
                case ActionState.QUESTIONS:
                    if (!QNFinished.ContainsValue(false))
                    {
                        // This could be come a corutine too
                        SwitchToPostQN();
                    }

                    if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.W))
                    {
                        Debug.Log("Forcing back to Waitingroom from" + ServerState.ToString());
                        ForceBackToWaitingRoom();
                    }

                    break;
                case ActionState.POSTQUESTIONS: break;
                case ActionState.RERUN:
                    if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
                    {
                        this.enabled = false;
                    }
                    else
                    {
                        Debug.LogError(
                            "We where running as either client or server while in ReRun mode. This is not supported! I am Quitting");
                        Application.Quit();
                    }

                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }


    public GUIStyle NotVisitedButton;
    public GUIStyle VisitendButton;

    void OnGUI()
    {
        if (NetworkManager.Singleton == null && !ClientListInitDone) return;
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            GUI.Label(new Rect(5, 5, 150, 50), "Server: " + ParticipantOrder + " " +
                                               NetworkManager.Singleton.ConnectedClients.Count + " " +
                                               GetParticipantCount() + "  " +
                                               ServerState + "  " + Time.timeScale);
            if (ServerState == ActionState.WAITINGROOM)
            {
                int y = 50;
                foreach (SceneField f in IncludedScenes)
                {
                    if (!VisitedScenes.ContainsKey(f))
                    {
                        VisitedScenes.Add(f, false);
                    }

                    if (GUI.Button(new Rect(5, 5 + y, 150, 25), f.SceneName,
                            VisitedScenes[f] ? VisitendButton : NotVisitedButton))
                    {
                        SwitchToLoading(f.SceneName);
                        VisitedScenes[f] = true;
                    }

                    y += 27;
                }

                y = 50;
                if (_OrderToClient == null) return;
                foreach (var p in _OrderToClient.Keys)
                {
                    if (GUI.Button(new Rect(200, 200 + y, 100, 25), "Calibrate " + p))
                    {
                        ulong clientID = _OrderToClient[p];
                        ClientRpcParams clientRpcParams = new ClientRpcParams
                        {
                            Send = new ClientRpcSendParams
                            {
                                TargetClientIds = new ulong[] {clientID}
                            }
                        };

                        ClientObjects[clientID][ParticipantObjectSpawnType.MAIN].GetComponent<ParticipantInputCapture>()
                            .CalibrateClientRPC(clientRpcParams);
                    }

                    y += 50;
                }
            }

            else if (ServerState == ActionState.QUESTIONS)
            {
                int y = 50;
                foreach (ParticipantOrder f in QNFinished.Keys)
                {
                    GUI.Label(new Rect(5, 5 + y, 150, 25), f + "  " + QNFinished[f].ToString());
                    y += 27;
                }
            }
            else if (ServerState == ActionState.READY)
            {
                if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
                {
                    GUI.Label(new Rect(5, 5, 150, 100), "Client: " +
                                                        ParticipantOrder + " " +
                                                        NetworkManager.Singleton.IsConnectedClient);
                }
            }

            if (ServerState == ActionState.DRIVE || ServerState == ActionState.READY ||
                ServerState == ActionState.QUESTIONS || ServerState == ActionState.POSTQUESTIONS)
            {
                GUI.Label(new Rect(10, Screen.height - 99, 150, 33), "Current scene: " + LastLoadedScene, style);
                if (ServerState == ActionState.QUESTIONS)
                {
                    GUI.Label(new Rect(10, Screen.height - 66, 150, 33),
                        "QN A: " + m_QNDataStorageServer.GetCurrentQuestionForParticipant(ParticipantOrder.A), style);
                    GUI.Label(new Rect(10, Screen.height - 33, 150, 33),
                        "QN B: " + m_QNDataStorageServer.GetCurrentQuestionForParticipant(ParticipantOrder.B), style);
                }
            }
        }
    }

    [SerializeField] private GUIStyle style;

    private Dictionary<ParticipantOrder, bool> QNFinished;
    private bool ClientListInitDone = false;

    public void FinishedQuestionair(ulong clientID)
    {
        ParticipantOrder po = GetOrder(clientID);
        QNFinished[po] = true;
    }


    #region GPSUpdate

    private void SetStartingGPSDirections()
    {
        UpdateAllGPS(FindObjectOfType<ScenarioManager>().GetStartingPositions());
    }

    public void UpdateAllGPS(Dictionary<ParticipantOrder, GpsController.Direction> dict)
    {
        foreach (ParticipantOrder or in dict.Keys)
        {
            ulong? cid = GetClientID(or);
            if (cid != null && ClientObjects.ContainsKey((ulong) cid))
            {
                ClientObjects[(ulong) cid][ParticipantObjectSpawnType.MAIN]
                    .GetComponent<ParticipantInputCapture>().CurrentDirection.Value = dict[or];
                ClientObjects[(ulong) cid][ParticipantObjectSpawnType.CAR]
                    .GetComponentInChildren<GpsController>().SetDirection(dict[or]);
            }
        }
    }

    #endregion


    public List<ulong> GetClientList()
    {
        if (_ClientToOrder == null) return null;
        return _ClientToOrder.Keys.ToList();
    }

    public Transform GetMainClientObject(ulong senderClientId)
    {
        if (!ClientObjects.ContainsKey(senderClientId)) return null;
        return ClientObjects[senderClientId].ContainsKey(ParticipantObjectSpawnType.MAIN)
            ? ClientObjects[senderClientId][ParticipantObjectSpawnType.MAIN].transform
            : null;
    }

    public Transform GetMainClientObject(ParticipantOrder po)
    {
        if (!_OrderToClient.Keys.Contains(po)) return null;
        ulong senderClientId = _OrderToClient[po];
        if (!ClientObjects.ContainsKey(senderClientId)) return null;
        return ClientObjects[senderClientId].ContainsKey(ParticipantObjectSpawnType.MAIN)
            ? ClientObjects[senderClientId][ParticipantObjectSpawnType.MAIN].transform
            : null;
    }

    public Transform GetClientHead(ParticipantOrder po)
    {
        if (!_OrderToClient.Keys.Contains(po)) return null;
        ulong senderClientId = _OrderToClient[po];
        if (!ClientObjects.ContainsKey(senderClientId)) return null;
        return ClientObjects[senderClientId].ContainsKey(ParticipantObjectSpawnType.MAIN)
            ? ClientObjects[senderClientId][ParticipantObjectSpawnType.MAIN].transform
                .FindChildRecursive("CenterEyeAnchor").transform
            : null;
    }

    public Transform GetClientObject(ParticipantOrder po, ParticipantObjectSpawnType type)
    {
        if (!_OrderToClient.Keys.Contains(po)) return null;
        ulong senderClientId = _OrderToClient[po];
        if (!ClientObjects.ContainsKey(senderClientId)) return null;
        return ClientObjects[senderClientId].ContainsKey(type)
            ? ClientObjects[senderClientId][type].transform
            : null;
    }

    public Transform GetMainClientCameraObject(ParticipantOrder po)
    {
        if (ServerState == ActionState.RERUN)
        {
            foreach (ParticipantInputCapture pic in FindObjectsOfType<ParticipantInputCapture>())
            {
                if (pic.GetComponent<ParticipantOrderReplayComponent>().GetParticipantOrder() == po)
                {
                    Debug.Log("Found the correct participant order trying to find eye anchor");
                    return pic.transform.FindChildRecursive("CenterEyeAnchor");
                    ;
                }
                else
                {
                    //   Debug.Log(po.ToString() +
                    //         pic.GetComponent<ParticipantOrderReplayComponent>().GetParticipantOrder());
                }
            }

            Debug.LogWarning("Never found eye anchor for participant: " + po);
            return null;
        }
        else
        {
            if (!_OrderToClient.Keys.Contains(po)) return null;
            ulong senderClientId = _OrderToClient[po];
            if (!ClientObjects.ContainsKey(senderClientId)) return null;
            Transform returnVal = ClientObjects[senderClientId].ContainsKey(ParticipantObjectSpawnType.MAIN)
                ? ClientObjects[senderClientId][ParticipantObjectSpawnType.MAIN].transform
                : null;
            return returnVal.FindChildRecursive("CenterEyeAnchor");
        }
    }

    public ParticipantOrder GetParticipantOrderClientId(ulong clientid)
    {
        if (_ClientToOrder.ContainsKey(clientid)) return _ClientToOrder[clientid];
        else return ParticipantOrder.None;
    }


    public bool GetClientIdParticipantOrder(ParticipantOrder po, out ulong val)
    {
        var tmp = GetClientID(po);
        if (tmp.HasValue)
        {
            val = tmp.Value;
            return true;
        }

        val = 0;
        return false;
    }

    public List<ParticipantOrder> GetCurrentlyConnectedClients()
    {
        return new List<ParticipantOrder>(_OrderToClient.Keys);
    }

    public void QNNewDataPoint(ParticipantOrder po, int id, int answerIndex, string lang)
    {
        if (m_QNDataStorageServer != null)
        {
            m_QNDataStorageServer.NewDatapointfromClient(po, id, answerIndex, lang);
        }
    }

    public void SendNewQuestionToParticipant(ParticipantOrder participantOrder, NetworkedQuestionnaireQuestion outval)
    {
        if (_OrderToClient.ContainsKey(participantOrder))
        {
            ClientObjects[_OrderToClient[participantOrder]][ParticipantObjectSpawnType.MAIN]
                .GetComponent<ParticipantInputCapture>().RecieveNewQuestionClientRPC(outval);
        }
    }


    public void SendTotalQNCount(ParticipantOrder participantOrder, int count)
    {
        if (_OrderToClient.ContainsKey(participantOrder))
        {
            ClientObjects[_OrderToClient[participantOrder]][ParticipantObjectSpawnType.MAIN]
                .GetComponent<ParticipantInputCapture>().SetTotalQNCountClientRpc(count);
            Debug.Log("just send total QN count to client: " + participantOrder);
        }
    }

    public RerunManager GetReRunManager()
    {
        return m_ReRunManager;
    }

    private bool ResetParticipantObject(ParticipantOrder po, ParticipantObjectSpawnType objectType)
    {
        switch (objectType)
        {
            case ParticipantObjectSpawnType.MAIN:
                break;
            case ParticipantObjectSpawnType.CAR:
                ulong? ClinetID = GetClientID(po);
                if (ClinetID.HasValue && ClientObjects.ContainsKey(ClinetID.Value) &&
                    ClientObjects[ClinetID.Value].ContainsKey(ParticipantObjectSpawnType.CAR))
                {
                    var car = ClientObjects[ClinetID.Value][ParticipantObjectSpawnType.CAR];
                    car.transform.GetComponent<Rigidbody>().velocity =
                        Vector3.zero; // Unsafe we are not sure that it has a rigid body
                    car.transform.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
                    _prepareSpawing(ClinetID.Value, out Pose? tempPose, out ParticipantObjectSpawnType spawnType);
                    if (!tempPose.HasValue)
                    {
                        Debug.LogWarning("Did not find a position to reset the participant to." + po);
                        return false;
                    }

                    car.transform.position = tempPose.Value.position;
                    car.transform.rotation = tempPose.Value.rotation;
                    return true;
                }
                else
                {
                    return false;
                }


                break;
            case ParticipantObjectSpawnType.PEDESTRIAN:
                break;
            default:
                break;
        }

        return false;
    }


    private static readonly Dictionary<ParticipantOrder, GpsController.Direction> StopDict =
        new Dictionary<ParticipantOrder, GpsController.Direction>()
        {
            {ParticipantOrder.A, GpsController.Direction.Stop},
            {ParticipantOrder.B, GpsController.Direction.Stop},
            {ParticipantOrder.C, GpsController.Direction.Stop},
            {ParticipantOrder.D, GpsController.Direction.Stop},
            {ParticipantOrder.E, GpsController.Direction.Stop},
            {ParticipantOrder.F, GpsController.Direction.Stop}
        };


    private Coroutine i_AwaitCarStopped;
    private bool FinishedRunningAwaitCorutine = true;

    public void AwaitQN()
    {
        Debug.Log("Starting Await Progress");
        if (!FinishedRunningAwaitCorutine) return;

        FinishedRunningAwaitCorutine = false;
        UpdateAllGPS(StopDict);
        i_AwaitCarStopped = StartCoroutine(AwaitCarStopped());
    }

    private float totalSpeedReturn()
    {
        float testValue = 0f;
        foreach (ulong val in GetClientList())
        {
            ParticipantOrder po = GetParticipantOrderClientId(val);
            Transform tf = GetClientObject(po, ParticipantObjectSpawnType.CAR);
            if (tf != null)
            {
                Rigidbody rb = tf.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    testValue += rb.velocity.magnitude;
                }
            }
        }

        return testValue;
    }

    IEnumerator AwaitCarStopped()
    {
        yield return new WaitUntil(() =>
            totalSpeedReturn() <= 0.1f
        );
        SwitchToQN();
        FinishedRunningAwaitCorutine = true;
    }

    public QNDataStorageServer GetQnStorageServer()
    {
        return m_QNDataStorageServer;
    }
}