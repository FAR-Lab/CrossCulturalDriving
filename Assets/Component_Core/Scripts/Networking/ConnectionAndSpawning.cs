using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Rerun;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectionAndSpawning : MonoBehaviour {
    public delegate void ReponseDelegate(ClienConnectionResponse response);


    public enum SpawnType {
        MAIN,
        CAR,
        PEDESTRIAN
    }

    public static string WaitingRoomSceneName = "WaitingRoom";


    //   public bool RunAsServer;
    public static bool fakeCare = false;


    private static readonly Dictionary<ParticipantOrder, GpsController.Direction> StopDict =
        new() {
            { ParticipantOrder.A, GpsController.Direction.Stop },
            { ParticipantOrder.B, GpsController.Direction.Stop },
            { ParticipantOrder.C, GpsController.Direction.Stop },
            { ParticipantOrder.D, GpsController.Direction.Stop },
            { ParticipantOrder.E, GpsController.Direction.Stop },
            { ParticipantOrder.F, GpsController.Direction.Stop }
        };

    public GameObject PlayerPrefab;
    public GameObject CarPrefab;
    public GameObject VRUIStartPrefab;
    public GameObject ref_ServerTimingDisplay;

    public List<SceneField> IncludedScenes = new();
    public string LastLoadedVisualScene;
    public bool ServerisRunning;


    public GUIStyle NotVisitedButton;
    public GUIStyle VisitendButton;

    [SerializeField] private GUIStyle style;

    private readonly ParticipantOrderMapping _participants = new();
    private readonly bool ClientListInitDone = false;

    private readonly Dictionary<ulong, Dictionary<SpawnType, NetworkObject>> ClientObjects = new();

    private ScenarioManager CurrentScenarioManager;
    private bool FinishedRunningAwaitCorutine = true;


    private Coroutine i_AwaitCarStopped;

    private bool initalSceneLoaded = false;

    private string LoadedScene = "";

    private QNDataStorageServer m_QNDataStorageServer;
    private RerunManager m_ReRunManager;
    private GameObject myStateManager;

    //Internal StateTracking
    private bool ParticipantOrder_Set;

    private Dictionary<ParticipantOrder, bool> QNFinished;

    private ReponseDelegate ReponseHandler;

    private bool retry = true;
    private bool SuccessFullyConnected;


    private readonly Dictionary<SceneField, bool> VisitedScenes = new();


  
    public ParticipantOrder ParticipantOrder { get; private set; } = ParticipantOrder.None;


    public ActionState ServerState { get; private set; }
    public string lang { private set; get; }


    private void Start() {
        
        /*
         * moved to StartUpManager.cs
         
        if (Application.platform == RuntimePlatform.Android) {
            // StartAsClient("English", ParticipantOrder.A, "192.168.1.160", 7777, ResponseDelegate);

            Instantiate(VRUIStartPrefab);
            Debug.Log("Started Client");
        }
        else {
            GetComponent<StartServerClientGUI>().enabled = true;
        }
*/
        if (FindObjectsOfType<RerunManager>().Length > 1) {
            Debug.LogError("We found more than 1 RerunManager. This is not support. Check your Hiracy");
            Application.Quit();
        }

        m_ReRunManager = FindObjectOfType<RerunManager>();
        if (m_ReRunManager == null) {
            Debug.LogError("Did not find a ReRunManager. Need exactly 1. Quitting!");
            Application.Quit();
        }
    }

    // Update is called once per frame
    private void Update() {
        if (NetworkManager.Singleton.IsServer) {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.Space)) {
                if (Input.GetKeyUp(KeyCode.A))
                    ResetParticipantObject(ParticipantOrder.A, SpawnType.CAR);
                else if (Input.GetKeyUp(KeyCode.B))
                    ResetParticipantObject(ParticipantOrder.B, SpawnType.CAR);
            }

            switch (ServerState) {
                case ActionState.DEFAULT: break;
                case ActionState.WAITINGROOM: break;
                case ActionState.LOADINGSCENARIO:
                case ActionState.LOADINGVISUALS:
                    if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.W)) {
                        Debug.LogWarning("Forcing back to Waitingroom from" + ServerState);
                        ForceBackToWaitingRoom();
                    }

                    break;
                case ActionState.READY:
                    if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.D)) {
                        SwitchToDriving();
                        SetStartingGPSDirections();
                    }

                    if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.W)) {
                        Debug.Log("Forcing back to Waitingroom from" + ServerState);
                        ForceBackToWaitingRoom();
                    }

                    break;
                case ActionState.DRIVE:
                    if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.W)) {
                        Debug.Log("Forcing back to Waitingroom from" + ServerState);
                        ForceBackToWaitingRoom();
                    }
                    else if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Q)) {
                        SwitchToQN();
                    }

                    break;
                case ActionState.QUESTIONS:
                    if (!QNFinished.ContainsValue(false))
                        // This could be come a corutine too
                        SwitchToPostQN();

                    if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.W)) {
                        Debug.Log("Forcing back to Waitingroom from" + ServerState);
                        ForceBackToWaitingRoom();
                    }

                    break;
                case ActionState.POSTQUESTIONS: break;
                case ActionState.RERUN:
                    if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient) {
                        enabled = false;
                    }
                    else {
                        Debug.LogError(
                            "We where running as either client or server while in ReRun mode. This is not supported! I am Quitting");
                        Application.Quit();
                    }

                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void OnGUI() {
        if (NetworkManager.Singleton == null && !ClientListInitDone) return;
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) {
            GUI.Label(new Rect(5, 5, 150, 50), "Server: " + ParticipantOrder + " " +
                                               NetworkManager.Singleton.ConnectedClients.Count + " " +
                                               _participants.GetParticipantCount() + "  " +
                                               ServerState + "  " + Time.timeScale);
            if (ServerState == ActionState.WAITINGROOM) {
                var y = 50;
                foreach (var f in IncludedScenes) {
                    if (!VisitedScenes.ContainsKey(f)) VisitedScenes.Add(f, false);

                    if (GUI.Button(new Rect(5, 5 + y, 150, 25), f.SceneName,
                            VisitedScenes[f] ? VisitendButton : NotVisitedButton)) {
                        SwitchToLoading(f.SceneName);
                        VisitedScenes[f] = true;
                    }

                    y += 27;
                }

                y = 50;
                if (_participants == null) return;
                foreach (var p in _participants.GetAllConnectedParticipants()) {
                    if (GUI.Button(new Rect(200, 200 + y, 100, 25), "Calibrate " + p)) {
                        var success = _participants.GetClientID(p, out var clientID);
                        if (!success) continue;

                        var clientRpcParams = new ClientRpcParams {
                            Send = new ClientRpcSendParams {
                                TargetClientIds = new[] { clientID }
                            }
                        };

                        ClientObjects[clientID][SpawnType.MAIN].GetComponent<VR_Participant>()
                            .CalibrateClientRPC(clientRpcParams);
                    }

                    y += 50;
                }
            }

            else if (ServerState == ActionState.QUESTIONS) {
                var y = 50;
                foreach (var f in QNFinished.Keys) {
                    GUI.Label(new Rect(5, 5 + y, 150, 25), f + "  " + QNFinished[f]);
                    y += 27;
                }
            }
            else if (ServerState == ActionState.READY) {
                if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
                    GUI.Label(new Rect(5, 5, 150, 100), "Client: " +
                                                        ParticipantOrder + " " +
                                                        NetworkManager.Singleton.IsConnectedClient);
            }

            if (ServerState == ActionState.DRIVE || ServerState == ActionState.READY ||
                ServerState == ActionState.QUESTIONS || ServerState == ActionState.POSTQUESTIONS) {
                GUI.Label(new Rect(10, Screen.height - 99, 150, 33), "Current scene: " + LastLoadedScene, style);
                if (ServerState == ActionState.QUESTIONS) {
                    GUI.Label(new Rect(10, Screen.height - 66, 150, 33),
                        "QN A: " + m_QNDataStorageServer.GetCurrentQuestionForParticipant(ParticipantOrder.A), style);
                    GUI.Label(new Rect(10, Screen.height - 33, 150, 33),
                        "QN B: " + m_QNDataStorageServer.GetCurrentQuestionForParticipant(ParticipantOrder.B), style);
                }
            }
        }
    }


    public void StartAsServer(string pairName) {
        Application.targetFrameRate = 72;
        gameObject.AddComponent<farlab_logger>();
        SteeringWheelManager.Singleton.enabled = true;
        m_QNDataStorageServer = GetComponent<QNDataStorageServer>();
        m_QNDataStorageServer.enabled = true;


        GetComponent<TrafficLightSupervisor>().enabled = true;
        SetupServerFunctionality();
        m_ReRunManager.SetRecordingFolder(pairName);
        Debug.Log("Starting Server for session: " + pairName);


        if (ref_ServerTimingDisplay != null) {
            var val = Instantiate(ref_ServerTimingDisplay, Vector3.zero, Quaternion.identity, transform)
                .GetComponent<ServerTimeDisplay>();
            val.StartDisplay();
        }
    }

    private void ClientDisconnected_client(ulong ClientID) {
        if (SuccessFullyConnected) {
            Debug.Log("Quitting due to disconnection.");
            Application.Quit();
        }
        else {
            ReponseHandler.Invoke(ClienConnectionResponse.FAILED);
            Debug.Log("Retrying connection");
        }
    }

    private void ClientConnected_client(ulong ClientID) {
        if (ClientID != NetworkManager.Singleton.LocalClient.ClientId) return;

        SuccessFullyConnected = true;
        ReponseHandler.Invoke(ClienConnectionResponse.SUCCESS);
        Debug.Log(SuccessFullyConnected + " CHECK HERE");
    }

    private void SetupClientFunctionality() {
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected_client;
        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected_client;
    }


    private void SetParticipantOrder(ParticipantOrder val) {
        NetworkManager.Singleton.NetworkConfig.ConnectionData = new[] { (byte)val }; // assigning ID
        ParticipantOrder = val;
        ParticipantOrder_Set = true;
    }

    private void Setlanguage(string lang_) {
        lang = lang_;
    }

    public void StartAsClient(string lang_, ParticipantOrder val, string ip, int port, ReponseDelegate result) {
        SetupClientFunctionality();
        ReponseHandler += result;
        SetupTransport(ip, port);
        Setlanguage(lang_);
        SetParticipantOrder(val);
        Debug.Log("Starting as Client.");

        NetworkManager.Singleton.StartClient();
    }

    public void LoadSceneReRun(string totalPath) {
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient) {
            Debug.LogError("Dont try to load a scene for RERUN while the server is running. Pleas restart the program");
            Application.Quit();
        }

        var fileName = Path.GetFileName(totalPath);
        var sceneNameList = fileName.Split('_');
        var sceneName = sceneNameList[0];
        Debug.Log("Scene Name" + sceneName);
        foreach (var v in IncludedScenes) Debug.Log(v.SceneName);

        if (IncludedScenes.ConvertAll(x => x.SceneName).Contains(sceneName)) {
            Debug.Log("Found scene. Loading!");
            if (LoadedScene == sceneName) {
                Debug.Log("ReRun scene already loaded continuing!");
                return;
            }

            if (LoadedScene.Length > 0) SceneManager.UnloadSceneAsync(LoadedScene);

            LoadedScene = sceneName;
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }
        else {
            Debug.LogWarning("Did not find scene. Aborting!");
        }
    }

    public void StartReRun() {
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

    private void VisualsceneLoadReRun(Scene arg0, LoadSceneMode arg1) {
        if (arg0.name == LoadedScene) {
            var tmp = GetScenarioManager();
            if (tmp != null && tmp.VisualSceneToUse != null && tmp.VisualSceneToUse.SceneName.Length > 0)
                if (tmp.VisualSceneToUse.SceneName != LastLoadedVisualScene) {
                    if (LastLoadedVisualScene.Length > 0) SceneManager.UnloadSceneAsync(LastLoadedVisualScene);

                    LastLoadedVisualScene = tmp.VisualSceneToUse.SceneName;
                    SceneManager.LoadScene(tmp.VisualSceneToUse.SceneName, LoadSceneMode.Additive);
                }
        }
    }

    private void SetupTransport(string ip = "127.0.0.1", int port = 7777) {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
            ip, // The IP address is a string
            (ushort)port // The port number is an unsigned short
        );
    }

    private void ServerHasStarted() {
        ServerisRunning = true;
        SwitchToWaitingRoom();
    }

    private void ResponseDelegate(ClienConnectionResponse response) {
    }

    public void FinishedQuestionair(ulong clientID) {
        _participants.GetOrder(clientID, out var po);
        QNFinished[po] = true;
    }


   

    public Transform GetMainClientObject(ulong senderClientId) {
        if (!ClientObjects.ContainsKey(senderClientId)) return null;
        return ClientObjects[senderClientId].ContainsKey(SpawnType.MAIN)
            ? ClientObjects[senderClientId][SpawnType.MAIN].transform
            : null;
    }

    public Transform GetMainClientObject(ParticipantOrder po) {
        bool success= _participants.GetClientID(po, out ulong senderClientId);
        if(!success) return null;
        if (!ClientObjects.ContainsKey(senderClientId)) return null;
        return ClientObjects[senderClientId].ContainsKey(SpawnType.MAIN)
            ? ClientObjects[senderClientId][SpawnType.MAIN].transform
            : null;
    }

    public Transform GetClientHead(ParticipantOrder po) {
        bool success= _participants.GetClientID(po, out ulong senderClientId);
        if(!success) return null;
        if (!ClientObjects.ContainsKey(senderClientId)) return null;
        return ClientObjects[senderClientId].ContainsKey(SpawnType.MAIN)
            ? ClientObjects[senderClientId][SpawnType.MAIN].transform
                .FindChildRecursive("CenterEyeAnchor").transform
            : null;
    }

    public Transform GetClientObject(ParticipantOrder po, SpawnType type) {
        bool success= _participants.GetClientID(po, out ulong senderClientId);
        if(!success) return null;
        if (!ClientObjects.ContainsKey(senderClientId)) return null;
        return ClientObjects[senderClientId].ContainsKey(type)
            ? ClientObjects[senderClientId][type].transform
            : null;
    }

    public Transform GetMainClientCameraObject(ParticipantOrder po) {
        if (ServerState == ActionState.RERUN) {
            foreach (var pic in FindObjectsOfType<VR_Participant>())
                if (pic.GetComponent<ParticipantOrderReplayComponent>().GetParticipantOrder() == po) {
                    Debug.Log("Found the correct participant order trying to find eye anchor");
                    return pic.transform.FindChildRecursive("CenterEyeAnchor");
                    ;
                }

            Debug.LogWarning("Never found eye anchor for participant: " + po);
            return null;
        }

        bool success= _participants.GetClientID(po, out ulong senderClientId);
        if(!success) return null;
        if (!ClientObjects.ContainsKey(senderClientId)) return null;
        var returnVal = ClientObjects[senderClientId].ContainsKey(SpawnType.MAIN)
            ? ClientObjects[senderClientId][SpawnType.MAIN].transform
            : null;
        return returnVal.FindChildRecursive("CenterEyeAnchor");
    }

    public ParticipantOrder GetParticipantOrderClientId(ulong clientid) {
        _participants.GetOrder(clientid, out ParticipantOrder outval);
        return outval;
    }


    public bool GetClientIdParticipantOrder(ParticipantOrder po, out  ulong outValue) {
         outValue = 0;
        bool var =   _participants.GetClientID(po, out outValue);
        
        return var;
    }

    public List<ParticipantOrder> GetCurrentlyConnectedClients() {
        return new List<ParticipantOrder>(_participants.GetAllConnectedParticipants());
    }

    public void QNNewDataPoint(ParticipantOrder po, int id, int answerIndex, string lang) {
        if (m_QNDataStorageServer != null) m_QNDataStorageServer.NewDatapointfromClient(po, id, answerIndex, lang);
    }

    public void SendNewQuestionToParticipant(ParticipantOrder participantOrder, NetworkedQuestionnaireQuestion outval) {
        bool success = _participants.GetClientID(participantOrder, out ulong clientID);
        if (success) {
            ClientObjects[clientID][SpawnType.MAIN]
                .GetComponent<VR_Participant>().RecieveNewQuestionClientRPC(outval);
        }
    }


    public void SendTotalQNCount(ParticipantOrder participantOrder, int count) {
        bool success = _participants.GetClientID(participantOrder, out ulong clientID);
        if (success) {
            ClientObjects[clientID][SpawnType.MAIN]
                .GetComponent<VR_Participant>().SetTotalQNCountClientRpc(count);
            Debug.Log("Just send total QN count to client: " + participantOrder);
        }
    }

    public RerunManager GetReRunManager() {
        return m_ReRunManager;
    }

    private bool ResetParticipantObject(ParticipantOrder po, SpawnType objectType) {
        switch (objectType) {
            case SpawnType.MAIN:
                break;
            case SpawnType.CAR:
                bool success = _participants.GetClientID(po,out ulong ClinetID);
                if (success && ClientObjects.ContainsKey(ClinetID) &&
                    ClientObjects[ClinetID].ContainsKey(SpawnType.CAR)) {
                    var car = ClientObjects[ClinetID][SpawnType.CAR];
                    car.transform.GetComponent<Rigidbody>().velocity =
                        Vector3.zero; // Unsafe we are not sure that it has a rigid body
                    car.transform.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
                    success = _GetCurrentSpawingData(ClinetID, out var tempPose, out var spawnType);
                    if (!success) {
                        Debug.LogWarning("Did not find a position to reset the participant to." + po);
                        return false;
                    }

                    car.transform.position = tempPose.position;
                    car.transform.rotation = tempPose.rotation;
                    return true;
                }

                return false;


                break;
            case SpawnType.PEDESTRIAN:
                break;
        }

        return false;
    }

    public void AwaitQN() {
        Debug.Log("Starting Await Progress");
        if (!FinishedRunningAwaitCorutine) return;

        FinishedRunningAwaitCorutine = false;
        UpdateAllGPS(StopDict);
        i_AwaitCarStopped = StartCoroutine(AwaitCarStopped());
    }

    private float totalSpeedReturn() {
        var testValue = 0f;
        foreach (var val in _participants.GetAllConnectedClients()) {
            var po = GetParticipantOrderClientId(val);
            var tf = GetClientObject(po, SpawnType.CAR);
            if (tf != null) {
                var rb = tf.GetComponent<Rigidbody>();
                if (rb != null) testValue += rb.velocity.magnitude;
            }
        }

        return testValue;
    }

    private IEnumerator AwaitCarStopped() {
        yield return new WaitUntil(() =>
            totalSpeedReturn() <= 0.1f
        );
        SwitchToQN();
        FinishedRunningAwaitCorutine = true;
    }

    public QNDataStorageServer GetQnStorageServer() {
        return m_QNDataStorageServer;
    }

    #region SingeltonManagment

    public static ConnectionAndSpawning Singleton { get; private set; }

    private void SetSingleton() {
        Singleton = this;
    }

    private void OnEnable() {
        if (Singleton != null && Singleton != this) {
            Destroy(gameObject);
            return;
        }

        SetSingleton();
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy() {
        if (Singleton != null && Singleton == this) Singleton = null;
    }

    #endregion


    #region SpawningAndConnecting

    private void SetupServerFunctionality() {
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        NetworkManager.Singleton.OnServerStarted += ServerHasStarted;
        
        NetworkManager.Singleton.StartServer();
        NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneEvent;

        SteeringWheelManager.Singleton.Init();
    }


    private void ServerLoadScene(string name) {
        DestroyAllClientObjects(new List<SpawnType> { SpawnType.CAR });
        NetworkManager.Singleton.SceneManager.LoadScene(name, LoadSceneMode.Single);
    }


    private void LoadSceneVisuals() {
        var tmp = GetScenarioManager();
        if (tmp != null && tmp.VisualSceneToUse != null && tmp.VisualSceneToUse.SceneName.Length > 0) {
            LastLoadedVisualScene = tmp.VisualSceneToUse.SceneName;
            NetworkManager.Singleton.SceneManager.LoadScene(tmp.VisualSceneToUse.SceneName, LoadSceneMode.Additive);
            ServerState = ActionState.LOADINGVISUALS;
        }
        else {
            SwitchToReady();
        }
    }

    private void SceneEvent(SceneEvent sceneEvent) {
        switch (sceneEvent.SceneEventType) {
            case SceneEventType.Load: break;
            case SceneEventType.Unload: break;
            case SceneEventType.Synchronize: break;
            case SceneEventType.ReSynchronize: break;
            case SceneEventType.LoadEventCompleted:
                Debug.Log("Load event!" + sceneEvent.ClientId + ServerState);
                if (ServerState == ActionState.LOADINGSCENARIO)
                    LoadSceneVisuals();
                else if (ServerState == ActionState.LOADINGVISUALS) SwitchToReady();

                break;
            case SceneEventType.UnloadEventCompleted:
                break;
            case SceneEventType.LoadComplete:
                Debug.Log("Load completed!" + sceneEvent.ClientId + ServerState);
                if (ServerState == ActionState.READY || ServerState == ActionState.LOADINGVISUALS ||
                    ServerState == ActionState.WAITINGROOM) {
                    Debug.Log("Trying to Spawn A Car!");
                    StartCoroutine(Spawn_Interactable_Await(sceneEvent.ClientId));
                }
                else {
                    Debug.Log("A client Finished loading But we are not gonna Spawn cause the visuals are missing!");
                }

                break;
            case SceneEventType.UnloadComplete: break;
            case SceneEventType.SynchronizeComplete: break;
            default: throw new ArgumentOutOfRangeException();
        }
    }
    // DestroyAllClientObjects_OfType(SpawnType TypeToDestroy)// TODO would make implementing more features easier

    private void DestroyAllClientObjects(List<SpawnType> TypesToDestroy) {
        var destroyMain = TypesToDestroy.Contains(SpawnType.MAIN);
        if (destroyMain) TypesToDestroy.Remove(SpawnType.MAIN);

        foreach (var id in ClientObjects.Keys) {
            foreach (var enumval in TypesToDestroy) DespawnObjectOfType(id, enumval);

            if (destroyMain) DespawnObjectOfType(id, SpawnType.MAIN);
        }
    }

    private void DespawnAllObjectsforParticipant(ParticipantOrder po) {
        if (po == ParticipantOrder.None) return;
        if (_participants.GetClientID(po, out ulong clientID)) {
            DespawnAllObjectsforParticipant(clientID);
        }
    }


    private void DespawnAllObjectsforParticipant(ulong clientID) {
        if (ClientObjects.ContainsKey(clientID) && ClientObjects[clientID] != null) {
            var despawnList =
                new List<SpawnType>(ClientObjects[clientID].Keys);
            if (despawnList.Contains(SpawnType.MAIN))
                despawnList.Remove(SpawnType.MAIN);

            foreach (var spawntype in despawnList) DespawnObjectOfType(clientID, spawntype);
        }
    }

    private void DespawnObjectOfType(ulong clientID, SpawnType oType) {
        if (ClientObjects[clientID].ContainsKey(oType)) {
            Debug.Log("Removing for:" + clientID + " object:" + oType);
            var no = ClientObjects[clientID][oType];
            switch (oType) {
                case SpawnType.MAIN:
                    Debug.Log("WARNING despawing MAIN!");
                    break;
                case SpawnType.CAR:

                    if (ClientObjects[clientID][SpawnType.MAIN] != null)
                        ClientObjects[clientID][SpawnType.MAIN].GetComponent<VR_Participant>()
                            .De_AssignCarTransform(clientID);

                    break;
                case SpawnType.PEDESTRIAN: break;
                default: throw new ArgumentOutOfRangeException(nameof(oType), oType, null);
            }

            no.Despawn();
            ClientObjects[clientID].Remove(oType);
        }
    }


    public enum ClienConnectionResponse {
        SUCCESS,
        FAILED
    }

    private void RemoveParticipant(ulong ClientID) {
        if (ClientObjects.ContainsKey(ClientID)) {
            ClientObjects.Remove(ClientID);
        }
    }


    private void ClientDisconnected(ulong ClientID) {
        if (ClientObjects.ContainsKey(ClientID) && ClientObjects[ClientID] != null) {
            DespawnAllObjectsforParticipant(ClientID);
            if (_participants.GetOrder(ClientID, out ParticipantOrder po)) {
                m_QNDataStorageServer.DeRegisterHandler(po);
                RemoveParticipant(ClientID);
            }
        }
    }

    private void ClientConnected(ulong ClientID) {
        Debug.Log("on Client Connect CallBack Was called!");
        // Whats important here is that this doesnt get called 
        SpawnAPlayer(ClientID, true);
    }

    // 2023.8.7 modification: add another out value of SpawnType
    private bool _GetCurrentSpawingData(ulong clientID, out Pose tempPose, out SpawnType spawnType) {
        
        if ( ! _participants.GetOrder(clientID,out  ParticipantOrder clientParticipantOrder)) {
            ErrFailToSpawn();
            tempPose = new Pose();
            spawnType = SpawnType.CAR;
            return false;
        }
        
         return GetScenarioManager().GetStartPose(clientParticipantOrder, out tempPose,
            out  spawnType);
    }

    private void ErrFailToSpawn() {
       Debug.LogError("Failed To Spawn");
    }


    private IEnumerator Spawn_Interactable_Await(ulong clientID) {
        var success = _participants.GetOrder(clientID, out var participantOrder);
        if (participantOrder != ParticipantOrder.None || success == false) {
            yield return new WaitUntil(() =>
                ClientObjects[clientID].ContainsKey(SpawnType.MAIN) &&
                ClientObjects[clientID][SpawnType.MAIN] != null
            );
            Spawn_Interactable_Immediate(clientID);
        }
    }

    private bool Spawn_Interactable_Immediate(ulong clientID) {
        var success = _participants.GetOrder(clientID, out var participantOrder);
        if (participantOrder == ParticipantOrder.None || success == false) return false;

        if (_GetCurrentSpawingData(clientID, out var tempPose, out var spawnType)) {
            // get input capture script of this client
            var clientInputCapture = ClientObjects[clientID][SpawnType.MAIN]
                .GetComponent<VR_Participant>();

            // switch spawnType. If car, instantiate. If pedestrian, set up pedestrian.
            switch (spawnType) {
                case SpawnType.CAR:
                    var newCar =
                        Instantiate(CarPrefab,
                            tempPose.position, tempPose.rotation);

                    newCar.GetComponent<NetworkObject>().Spawn(true);

                    if (!fakeCare && _participants.GetOrder(clientID,out ParticipantOrder pO)) {
                        
                        newCar.GetComponent<NetworkVehicleController>().AssignClient(clientID, pO);

#if SPAWNDEBUG
                        Debug.Log("Assigning car to a new partcipant with clinetID:" + clientID.ToString() + " =>" +
                                newCar.GetComponent<NetworkObject>().NetworkObjectId);
#endif
                        if (ClientObjects[clientID][SpawnType.MAIN] != null)
                            ClientObjects[clientID][SpawnType.MAIN]
                                .GetComponent<VR_Participant>()
                                .AssignCarTransform(newCar.GetComponent<NetworkVehicleController>(), clientID);

                        else
                            Debug.LogError("Could not find player as I am spawning the CAR. Broken please fix.");
                    }

                    clientInputCapture.SetMySpawnType(SpawnType.CAR);

                    break;

                case SpawnType.PEDESTRIAN:
                    clientInputCapture.SetMySpawnType(SpawnType.PEDESTRIAN);
                    // maybe use callback here to
                    // 1. turn off position tracking
                    // 2. attach VRHead to avatar head (position only, rotation is for calibration process)
                    break;
            }

            return true;
        }

        return false;
    }

    private bool SpawnAPlayer(ulong clientID, bool persistent) {
        var foundPlayer = _participants.GetOrder(clientID, out var _participantOrder);
        if (_participantOrder == ParticipantOrder.None || foundPlayer == false) return false;

        if (_GetCurrentSpawingData(clientID, out Pose tempPose, out SpawnType spawnType)) {
          

            var newPlayer =
                Instantiate(PlayerPrefab,
                    tempPose.position, tempPose.rotation);

            newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, !persistent);
            ClientObjects[clientID].Add(SpawnType.MAIN, newPlayer.GetComponent<NetworkObject>());
            m_QNDataStorageServer.SetupForNewRemoteImage(_participantOrder);
            return true;
        }

        return false;
    }

    public ScenarioManager GetScenarioManager() {
        CurrentScenarioManager = FindObjectOfType<ScenarioManager>();
        if (CurrentScenarioManager == null)
            Debug.LogError(
                "Tried to find a scenario manager(probably to spawn cars), but they was nothing. Did you load your scenario(subscene)?");

        return CurrentScenarioManager;
    }


    // https://docs-multiplayer.unity3d.com/netcode/current/basics/connection-approval
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response) {
        //byte[] connectionData, ulong clientId,
        // NetworkManager.ConnectionApprovedDelegate callback){
        var approve = false;
        var participantOrder = (ParticipantOrder)request.Payload[0];

        approve = _participants.AddParticipant(participantOrder, request.ClientNetworkId);

        if (!approve) {
            Debug.Log("Participant Order " + request.Payload[0] +
                      " tried to join, but we already have a participant with that order. ");
        }
        else {
            ClientObjects.Add(request.ClientNetworkId, new Dictionary<SpawnType, NetworkObject>());
            Debug.Log("Client will connect now!");
        }


        response.Approved = approve;
        response.CreatePlayerObject = false;
        response.Pending = false;
        //SpawnAPlayer(request.ClientNetworkId, true);
    }

    #endregion

    #region StateChangeCalls

    private void SwitchToWaitingRoom() {
        if (m_ReRunManager.IsRecording()) {
            m_ReRunManager.StopRecording();
            Debug.LogWarning(
                "I stoped Recording as I was loaded back to the Waitingroom. Recording should have stopped at the switch to the Questionnaire stage.");
        }

        ServerState = ActionState.WAITINGROOM;
        ServerLoadScene(WaitingRoomSceneName);
    }

    private void SwitchToLoading(string name) {
        ServerState = ActionState.LOADINGSCENARIO;
        ServerLoadScene(name);
        LastLoadedScene = name;
    }

    public string GetLoadedScene() {
        return LastLoadedScene;
    }

    private string LastLoadedScene = "";

    // modification 1: Add ServerStateChange deledate
    public delegate void ServerStateChange_delegate(ActionState state);

    public ServerStateChange_delegate ServerStateChange;

    private void SwitchToReady() {
        ServerStateChange.Invoke(ActionState.READY);


        ServerState = ActionState.READY;
    }


    private void SwitchToDriving() {
        if (!farlab_logger.Instance.ReadyToRecord()) {
            Debug.LogWarning(
                "I was trying to start recording while something else was still storring Data. Try again in a moment!");
            return;
        }

        ServerState = ActionState.DRIVE;

        m_ReRunManager.BeginRecording(LastLoadedScene);
        m_QNDataStorageServer.StartScenario(LastLoadedScene, m_ReRunManager.GetRecordingFolder());
        farlab_logger.Instance.StartRecording(m_ReRunManager, LastLoadedScene, m_ReRunManager.GetRecordingFolder());
    }


    public void SwitchToQN() {
        Debug.Log("Stopping Driving and Stopping the recording.");
        m_ReRunManager.StopRecording();

        ServerState = ActionState.QUESTIONS;
        QNFinished = new Dictionary<ParticipantOrder, bool>();
        foreach (var po in _participants.GetAllConnectedParticipants()) QNFinished.Add(po, false);


        foreach (var no in FindObjectsOfType<NetworkVehicleController>()) {
            no.transform.GetComponent<Rigidbody>().velocity = Vector3.zero;
            no.transform.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        }

        foreach (var p in ClientObjects.Keys)
            ClientObjects[p][SpawnType.MAIN].GetComponent<VR_Participant>()
                .StartQuestionairClientRPC();

        m_QNDataStorageServer.StartQn(GetScenarioManager(), m_ReRunManager);
        StartCoroutine(farlab_logger.Instance.StopRecording());
    }

    private void ForceBackToWaitingRoom() {
        Debug.Log("Forced back to the waiting Room. When running studies please try to avoid this!");
        SwitchToPostQN();
    }

    private void SwitchToPostQN() {
        m_QNDataStorageServer.StopScenario(m_ReRunManager);
        if (farlab_logger.Instance.isRecording()) StartCoroutine(farlab_logger.Instance.StopRecording());

        ServerState = ActionState.POSTQUESTIONS;
        SwitchToWaitingRoom();
    }

    #endregion


    #region GPSUpdate

    private void SetStartingGPSDirections() {
        UpdateAllGPS(FindObjectOfType<ScenarioManager>().GetStartingPositions());
    }

    public void UpdateAllGPS(Dictionary<ParticipantOrder, GpsController.Direction> dict) {
        foreach (var pO in dict.Keys) {
            bool success = _participants.GetClientID(pO, out ulong cid);
            if (success && ClientObjects.ContainsKey(cid)) {
                ClientObjects[cid][SpawnType.MAIN]
                    .GetComponent<VR_Participant>().CurrentDirection.Value = dict[pO];
                ClientObjects[cid][SpawnType.CAR]
                    .GetComponentInChildren<GpsController>().SetDirection(dict[pO]);
            }
        }
    }

    #endregion
}