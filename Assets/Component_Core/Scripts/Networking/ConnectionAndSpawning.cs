 using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
 using Rerun;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
 using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
 using Pose = UnityEngine.Pose;
 using Rect = UnityEngine.Rect;


 public class ConnectionAndSpawning : MonoBehaviour {
    public struct JoinParameters {
        public JoinType _jointype;
        public SpawnType _spawnType;
        public ParticipantOrder _participantOrder;
        public string _Language;
    }

    public GameObject LocalServerCameraRig;
    public static string WaitingRoomSceneName = "WaitingRoom";
    public SO_JoinTypeToClientObject JoinTypeConfig;
    public Dictionary<JoinType, Client_Object> JoinType_To_Client_Object;
    public SO_SpawnTypeToInteractableObject SpawnTypeConfig;
    public Dictionary<SpawnType, Interactable_Object> SpawnType_To_InteractableObjects;

    private static readonly Dictionary<ParticipantOrder, NavigationScreen.Direction> StopDict =
        new() {
            { ParticipantOrder.A, NavigationScreen.Direction.Stop },
            { ParticipantOrder.B, NavigationScreen.Direction.Stop },
            { ParticipantOrder.C, NavigationScreen.Direction.Stop },
            { ParticipantOrder.D, NavigationScreen.Direction.Stop },
            { ParticipantOrder.E, NavigationScreen.Direction.Stop },
            { ParticipantOrder.F, NavigationScreen.Direction.Stop }
        };

    
    public SerializedDictionary<string, SerializedDictionary<ParticipantOrder, Pose>> WaitingRoomSpawnPositonData =
        new SerializedDictionary<string, SerializedDictionary<ParticipantOrder, Pose>>();

    public GameObject ref_ServerTimingDisplay;
    public GameObject ZedManagerPrefab;

    public List<SceneField> IncludedScenes = new();
    private string LastLoadedVisualScene;
    public bool ServerisRunning { private set; get; }

    public delegate void ReponseDelegate(ClienConnectionResponse response);

    [SerializeField] private GUIStyle style;

    public ParticipantOrderMapping participants;
    private readonly bool ClientListInitDone = false;

    public Dictionary<ParticipantOrder, Client_Object> Main_ParticipantObjects { private set; get; }

    private Dictionary<ParticipantOrder, List<Interactable_Object>> Interactable_ParticipantObjects;

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


    public readonly Dictionary<SceneField, bool> VisitedScenes = new();


    public ParticipantOrder ParticipantOrder { get; private set; } = ParticipantOrder.None;


    public ActionState ServerState { get; private set; }
    public JoinParameters ThisClient { private set; get; } = new JoinParameters();

    private void Awake() {
        participants = new ParticipantOrderMapping();
        Main_ParticipantObjects = new Dictionary<ParticipantOrder, Client_Object>();
        Interactable_ParticipantObjects = new Dictionary<ParticipantOrder, List<Interactable_Object>>();

        // populate the dictionaries
        JoinType_To_Client_Object = JoinTypeConfig.EnumToValueDictionary;
        SpawnType_To_InteractableObjects = SpawnTypeConfig.EnumToValueDictionary;
    }

    private void Start() {
        DontDestroyOnLoad(FindObjectOfType<InputSystemUIInputModule>());
        LastLoadedVisualScene = "";
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
                    ResetInteractableObject(ParticipantOrder.A);
                else if (Input.GetKeyUp(KeyCode.B))
                    ResetInteractableObject(ParticipantOrder.B);
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
                        GetScenarioManager().SetStartingGPSDirections();
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
            if (ServerState == ActionState.QUESTIONS) {
                var y = 50;
                foreach (var f in QNFinished.Keys) {
                    GUI.Label(new Rect(5, 5 + y, 150, 25), f + "  " + QNFinished[f]);
                    y += 27;
                }
            }

            if (ServerState == ActionState.DRIVE || ServerState == ActionState.READY ||
                ServerState == ActionState.QUESTIONS || ServerState == ActionState.POSTQUESTIONS) {
                if (ServerState == ActionState.QUESTIONS) {
                    GUI.Label(new Rect(10, Screen.height - 66, 150, 33),
                        "QN A: " + m_QNDataStorageServer.GetCurrentQuestionForParticipant(ParticipantOrder.A), style);
                    GUI.Label(new Rect(10, Screen.height - 33, 150, 33),
                        "QN B: " + m_QNDataStorageServer.GetCurrentQuestionForParticipant(ParticipantOrder.B), style);
                }
            }
        }
    }

    private void SetUpToServe(string pairName) {
        Application.targetFrameRate = 72;

      
        
        gameObject.AddComponent<SteeringWheelManager>();
        gameObject.AddComponent<farlab_logger>();
        m_QNDataStorageServer = gameObject.AddComponent<QNDataStorageServer>();

        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        NetworkManager.Singleton.OnServerStarted += ServerHasStarted;

        SteeringWheelManager.Singleton.Init();

        Debug.Log("Starting Server for session: " + pairName);

        if (ref_ServerTimingDisplay != null) {
            var val = Instantiate(ref_ServerTimingDisplay, Vector3.zero, Quaternion.identity, transform)
                .GetComponent<ServerTimeDisplay>();
            val.StartDisplay();
        }
    }


    public void StartAsServer(string pairName) {
        SetUpToServe(pairName);

        if (LocalServerCameraRig == null) {
            Debug.LogError(
                "We do not have a LocalServerCameraRig I need this to start the server. Please make a reference in the inspector in ConnectionAndSpawning");
            Application.Quit();
        }

        var ServerCamera =
            Instantiate(LocalServerCameraRig, Vector3.zero, Quaternion.identity);

        Debug.Log(ServerCamera);
        DontDestroyOnLoad(ServerCamera);
        m_ReRunManager.RerunInitialization(true, ServerCamera.GetComponent<RerunPlaybackCameraManager>(),
            RerunManager.StartUpMode.RECORDING);
        m_ReRunManager.SetRecordingFolder(pairName);



        NetworkManager.Singleton.StartServer();
        participants.AddParticipant(ParticipantOrder.None, NetworkManager.Singleton.LocalClientId, SpawnType.NONE,
            JoinType.SERVER);
        NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneEvent_Server;


     //  var t=  Instantiate(ZedManagerPrefab);
      // DontDestroyOnLoad(t);
       
     //   t=  Instantiate(ZedManagerPrefab);
      //  var z = t.GetComponent<ZEDManager>();
        //z.Close();
      //  z.cameraID = ZED_CAMERA_ID.CAMERA_ID_02;
         //     z.StartBodyTracking();
      // DontDestroyOnLoad(t);

    }

    public void StartAsHost(string pairName,ParticipantOrder po, JoinType _jt,SpawnType _st) {
        SetUpToServe(pairName);
        m_ReRunManager.RerunInitialization(true, null, RerunManager.StartUpMode.RECORDING);
        m_ReRunManager.SetRecordingFolder(pairName);
        JoinParameters connectionDataRequest = new JoinParameters() {
            _participantOrder = po,
            _spawnType = _st,
            _jointype = _jt,
            _Language = "English"
        };
        ThisClient = connectionDataRequest;
        
        string jsonstring = JsonConvert.SerializeObject(connectionDataRequest);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(jsonstring); // assigning ID
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneEvent_Server;
        
      //  var t=  Instantiate(ZedManagerPrefab);
    //    DontDestroyOnLoad(t);
        
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
        Debug.Log("Debug: Client connected");
        if (ClientID != NetworkManager.Singleton.LocalClient.ClientId) return; Debug.Log("Debug: Success!");
        SuccessFullyConnected = true;
        ReponseHandler.Invoke(ClienConnectionResponse.SUCCESS);

        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnLoadCompleteCLient;
    }

    private void OnLoadCompleteCLient(ulong clientid, string scenename, LoadSceneMode loadscenemode) {
        if (loadscenemode == LoadSceneMode.Additive && NetworkManager.Singleton.LocalClientId == clientid) {
            ActivateVisualScene();
        }
    }

    private void SetupClientFunctionality() {
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected_client;
        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected_client;
    }


    private void SetParticipantOrder(ParticipantOrder val) {
        ParticipantOrder = val;
        ParticipantOrder_Set = true;
    }

    

    public void StartAsClient(string _langIN, ParticipantOrder po, string ip, int port, ReponseDelegate result,
        SpawnType _spawnTypeIN = SpawnType.CAR, JoinType _joinTypeIN = JoinType.VR) {
        Debug.Log(
            $"Log: Starting as Client. IP: {ip} Port: {port} Language: {_langIN} ParticipantOrder: {po} SpawnType: {_spawnTypeIN} JoinType: {_joinTypeIN}");

       
        SetupClientFunctionality();
        ReponseHandler += result;
        SetupTransport(ip, port);
        SetParticipantOrder(po);
        JoinParameters connectionDataRequest = new JoinParameters() {
            _participantOrder = po,
            _spawnType = _spawnTypeIN,
            _jointype = _joinTypeIN,
            _Language = _langIN
        };

        ThisClient = connectionDataRequest;
        string jsonstring = JsonConvert.SerializeObject(connectionDataRequest);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(jsonstring); // assigning ID

        Debug.Log("Attempting to connect as a client.");

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

    public void StartAsRerun() {
        ServerState = ActionState.RERUN;


        if (_VerifyPrefabAvalible(JoinType.SERVER)) {
            //ToDo: here we would really want to remove the explicit rerun reference, enable callbacks to spawn cameras etc...
            var ServerCamera =
                Instantiate(JoinType_To_Client_Object[JoinType.SERVER],
                    Vector3.zero, Quaternion.identity);

            Debug.Log(ServerCamera);
            DontDestroyOnLoad(ServerCamera);
            m_ReRunManager.RerunInitialization(true, ServerCamera.GetComponent<RerunPlaybackCameraManager>(),
                RerunManager.StartUpMode.REPLAY);

            m_ReRunManager.RegisterPreLoadHandler(LoadSceneReRun);
        }

        NetworkManager.Singleton.enabled = false;
        SceneManager.sceneLoaded += VisualsceneLoadReRun;

        ServerStateChange.Invoke(ActionState.RERUN);
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
        else if (arg0.name == LastLoadedVisualScene) {
            if (SceneManager.GetActiveScene().name != LastLoadedVisualScene) {
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(LastLoadedVisualScene));
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

    private void ResponseDelegate(ClienConnectionResponse response) { }

    public void FinishedQuestionair(ulong clientID) {
        participants.GetOrder(clientID, out var po);
        QNFinished[po] = true;
    }


    public Transform GetMainClientObject(ulong senderClientId) {
        bool success = participants.GetOrder(senderClientId, out ParticipantOrder po);
        if (success) {
            return GetMainClientObject(po);
        }

        return null;
    }

    public Transform GetMainClientObject(ParticipantOrder po) {
        if (!Main_ParticipantObjects.ContainsKey(po)) return null;
        return Main_ParticipantObjects[po].transform;
    }


    public List<Interactable_Object> GetInteractableObjects_For_Participants(ulong senderClientId) {
        bool success = participants.GetOrder(senderClientId, out ParticipantOrder po);
        if (success) {
            return GetInteractableObjects_For_Participants(po);
        }

        return null;
    }

    public List<Interactable_Object> GetInteractableObjects_For_Participants(ParticipantOrder po) {
        if (!Interactable_ParticipantObjects.ContainsKey(po)) return null;
        return Interactable_ParticipantObjects[po];
    }


    public Transform GetClientMainCameraObject(ulong senderClientId) {
        bool success = participants.GetOrder(senderClientId, out ParticipantOrder po);
        if (success) {
            return GetClientMainCameraObject(po);
        }

        return null;
    }

    public Transform GetClientMainCameraObject(ParticipantOrder po) {
        if (ServerState == ActionState.RERUN) {
            foreach (var pic in FindObjectsOfType<Client_Object>())
                if (pic.GetComponent<ParticipantOrderReplayComponent>().GetParticipantOrder() == po) {
                    Debug.Log("Found the correct participant order trying to find eye anchor");
                    return pic.GetMainCamera();
                }

            Debug.LogWarning("Never found eye anchor for participant: " + po);
            return null;
        }

        if (!Main_ParticipantObjects.ContainsKey(po)) return null;
        return Main_ParticipantObjects[po].GetMainCamera();
    }


    public ParticipantOrder GetParticipantOrderClientId(ulong clientid) {
        participants.GetOrder(clientid, out ParticipantOrder outval);
        return outval;
    }

    public bool GetClientIdParticipantOrder(ParticipantOrder po, out ulong outValue) {
        outValue = 0;
        bool var = participants.GetClientID(po, out outValue);
        return var;
    }

    public List<ParticipantOrder> GetCurrentlyConnectedClients() {
        return new List<ParticipantOrder>(participants.GetAllConnectedParticipants());
    }

//    public void QNNewDataPoint(ParticipantOrder po, int id, int answerIndex, string lang) {
 //       if (m_QNDataStorageServer != null) m_QNDataStorageServer.NewDatapointfromClient(po, id, answerIndex, lang);
  //  }


    public RerunManager GetReRunManager() {
        return m_ReRunManager;
    }

    private void ResetInteractableObject(ParticipantOrder po) {
        if (Interactable_ParticipantObjects.ContainsKey(po)) {
            foreach (Interactable_Object io in Interactable_ParticipantObjects[po]) {
                bool success = _GetCurrentSpawingData(po, out var tempPose);
                if (!success) {
                    Debug.LogWarning("Did not find a position to reset the participant to." + po);
                    return;
                }

                io.SetStartingPose(tempPose);
            }
        }
    }

    public void AwaitQN() {
        Debug.Log("Starting Await Progress");
        if (!FinishedRunningAwaitCorutine) return;

        FinishedRunningAwaitCorutine = false;
        GetScenarioManager().UpdateAllNavigationInstructions(StopDict);
        i_AwaitCarStopped = StartCoroutine(AwaitCarStopped());
    }

    private bool AllActionStopped() {
        bool testValue = true;
        foreach (var po in participants.GetAllConnectedParticipants()) {
            if (po == ParticipantOrder.None) continue;
            foreach (var IO in Interactable_ParticipantObjects[po])
                testValue &= IO.GetComponent<Interactable_Object>().HasActionStopped();
        }

        return testValue;
    }

    private IEnumerator AwaitCarStopped() {
        yield return new WaitUntil(() =>
            AllActionStopped()
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

    private void ServerLoadScene(string name) {
        DestroyALLInteractable_ALLClients();
        NetworkManager.Singleton.SceneManager.LoadScene(name, LoadSceneMode.Single);
    }


    private void LoadSceneVisuals() {
        if (GetScenarioManager() && GetScenarioManager().HasVisualScene()) {
            LastLoadedVisualScene = GetScenarioManager().VisualSceneToUse.SceneName;
            NetworkManager.Singleton.SceneManager.LoadScene(GetScenarioManager().VisualSceneToUse.SceneName,
                LoadSceneMode.Additive);
            ServerState = ActionState.LOADINGVISUALS;
            ServerStateChange?.Invoke(ActionState.LOADINGVISUALS);
        }
        else {
            SwitchToReady();
        }
    }

    private void ActivateVisualScene() {
        if (GetScenarioManager().HasVisualScene()) {
            SceneManager.SetActiveScene(
                SceneManager.GetSceneByName(GetScenarioManager()
                    .VisualSceneToUse)); // This is icky needs to be verifyeid that its also the one we loaded!
        }
    }

    private void SceneEvent_Server(SceneEvent sceneEvent) {
        switch (sceneEvent.SceneEventType) {
            case SceneEventType.Load: break;
            case SceneEventType.Unload: break;
            case SceneEventType.Synchronize: break;
            case SceneEventType.ReSynchronize: break;
            case SceneEventType.LoadEventCompleted:
                Debug.Log("Load event!" + sceneEvent.ClientId + ServerState);
                if (ServerState == ActionState.LOADINGSCENARIO)
                    LoadSceneVisuals();
                else if (ServerState == ActionState.LOADINGVISUALS) {
                    ActivateVisualScene();

                    SwitchToReady();
                }

                break;
            case SceneEventType.UnloadEventCompleted:
                break;
            case SceneEventType.LoadComplete:
                Debug.Log($"Load completed! sceneEvent:{sceneEvent.LoadSceneMode} ServerState:{ServerState}");

                if (ServerState is ActionState.READY or ActionState.LOADINGVISUALS or ActionState.WAITINGROOM) {
                    Debug.Log("Lets see if we should Spawn an interactable!");
                    if (sceneEvent.LoadSceneMode == LoadSceneMode.Additive && GetScenarioManager().HasVisualScene() ||
                        sceneEvent.LoadSceneMode == LoadSceneMode.Single &&
                        !GetScenarioManager()
                            .HasVisualScene()) //TODO: feels like a bad hack  but makes sure that late joining a scene we dont spawn a car twice (scenario and visual scene callback)
                    {
                        bool success = participants.GetOrder(sceneEvent.ClientId, out ParticipantOrder po);
                        Debug.Log($"About to spawn an interactable for participant :{po}");
                        StartCoroutine(Spawn_Interactable_Await(po));
                    }
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

    private void DestroyALLInteractable_ALLClients() {
        foreach (var po in participants.GetAllConnectedParticipants()) {
            if (po == ParticipantOrder.None) continue;
            Debug.Log($"Destroying Interactable for po:{po}");
            DespawnAllInteractableObject(po);
        }
    }


    private void DespawnAllObjectsforParticipant(ParticipantOrder po) {
        if (po == ParticipantOrder.None) return;
        Debug.Log($"DespawnAllObjectsforParticipant for po:{po}");
        DespawnAllInteractableObject(po);

        //DespawnMainClientObject(po);
    }

    private void DespawnMainClientObject(ParticipantOrder po) {
        if (po == ParticipantOrder.None) return;
        Debug.Log($"DespawnMainClientObject for po:{po}");
        if (Main_ParticipantObjects.ContainsKey(po) && Main_ParticipantObjects[po] != null) {
            Main_ParticipantObjects[po].GetComponent<NetworkObject>().Despawn();
        }

        Main_ParticipantObjects.Remove(po);
    }

    private void DespawnAllInteractableObject(ParticipantOrder po) {
        Debug.Log($"Despawning All interactables for po:{po}");
        if (Interactable_ParticipantObjects.TryGetValue(po, out var interactableObjects)) {
            HashSet<Interactable_Object>
                toRemove =
                    new HashSet<Interactable_Object>(); // based on 4. in https://www.techiedelight.com/remove-elements-from-list-while-iterating-csharp/#:~:text=An%20elegant%20solution%20is%20to,()%20method%20for%20removing%20elements.
            Debug.Log($"Found{interactableObjects.Count} interactable for po{po}.");
            foreach (var io in interactableObjects) {
                if (Main_ParticipantObjects[po] != null) {
                    bool success = participants.GetClientID(po, out ulong clientID);
                    if (success) {
                        Main_ParticipantObjects[po]
                            .De_AssignFollowTransform(clientID, io.GetComponent<NetworkObject>());
                    }
                }

                Debug.Log($"Despawin interact able {io.name}");
                io.GetComponent<NetworkObject>().Despawn();
                toRemove.Add(io);
            }

            Interactable_ParticipantObjects[po].RemoveAll(toRemove.Contains);
        }
    }


    public enum ClienConnectionResponse {
        SUCCESS,
        FAILED
    }


    private void ClientDisconnected(ulong clientID) {
        bool success = participants.GetOrder(clientID, out ParticipantOrder po);
        Debug.Log($"Got a client Disconnect for client:{po}");
        if (success && Main_ParticipantObjects.ContainsKey(po) && Main_ParticipantObjects[po] != null) {
            DespawnAllObjectsforParticipant(po);
            participants.RemoveParticipant(po);
            Main_ParticipantObjects.Remove(po);
            Interactable_ParticipantObjects.Remove(po);
        }
    }

    private void ClientConnected(ulong ClientID) {
        Debug.Log("On Client Connect CallBack Was called!");

        Spawn_Client(ClientID);
    }


    private bool _GetCurrentSpawingData(ulong clientID, out Pose tempPose) {
        if (!participants.GetOrder(clientID, out ParticipantOrder clientParticipantOrder)) {
            ErrFailToSpawn();
            tempPose = new Pose();
            return false;
        }

        return _GetCurrentSpawingData(clientParticipantOrder, out tempPose);
    }

    private bool _GetCurrentSpawingData(ParticipantOrder po, out Pose tempPose) {
        return GetScenarioManager().GetSpawnPose(po, out tempPose);
    }

    private void ErrFailToSpawn() {
        Debug.LogError("Failed To Spawn");
    }


    private IEnumerator Spawn_Interactable_Await(ParticipantOrder po) {
        if (po != ParticipantOrder.None) {
            Debug.Log("ok getting ready to spawn an object");
            yield return new WaitUntil(() =>
                Main_ParticipantObjects.ContainsKey(po) &&
                Main_ParticipantObjects[po] != null
            );
            Spawn_Interactable_Immediate(po);
        }
    }

    private bool _VerifyPrefabAvalible(SpawnType st) {
        bool tmp = (SpawnType_To_InteractableObjects.ContainsKey(st) && SpawnType_To_InteractableObjects[st] != null);

        return tmp;
    }

    private bool _VerifyPrefabAvalible(JoinType jt) {
        return (JoinType_To_Client_Object.ContainsKey(jt) && JoinType_To_Client_Object[jt] != null);
    }

    private void Spawn_Interactable_Immediate(ParticipantOrder po) {
        if (_GetCurrentSpawingData(po, out var tempPose)) {
            Debug.Log("GOt a spawn Point lets get the object.");
            Client_Object mainParticipantObject = Main_ParticipantObjects[po];
            Debug.Log("Got the main Client Object lets go.");
            bool success = participants.GetSpawnType(po, out SpawnType spawnType);
            if (_VerifyPrefabAvalible(spawnType) && success) {
                var newInteractableObject =
                    Instantiate(SpawnType_To_InteractableObjects[spawnType],
                        tempPose.position, tempPose.rotation);

                participants.GetClientID(po, out ulong clientID);
                newInteractableObject.GetComponent<NetworkObject>().Spawn(true);

                newInteractableObject.GetComponent<Interactable_Object>().AssignClient(clientID, po);
                newInteractableObject.GetComponent<Interactable_Object>().m_participantOrder.Value = po; //ToDo unecessairy
                Debug.Log(
                    $"Is the interactable Spawned here already: {newInteractableObject.GetComponent<Interactable_Object>().IsSpawned}");
                Interactable_ParticipantObjects[po].Add(newInteractableObject.GetComponent<Interactable_Object>());
                
                if (Main_ParticipantObjects[po] != null) {
                    Main_ParticipantObjects[po]
                        .AssignFollowTransform(newInteractableObject.GetComponent<Interactable_Object>(), clientID);
                }
                else {
                    Debug.LogError("Could not find Client as I am spawning the Interactable. Broken please fix.");
                }

                
            }
        }
    }

    private void Spawn_Client(ulong clientID) {
        StartCoroutine(Spawn_Client_Await(clientID));
    }

    private IEnumerator Spawn_Client_Await(ulong clientID) {
        Debug.Log("Awaiting the scenarioManger and for the waiting room scene to be loaded.");
        yield return new WaitUntil(() =>
            FindObjectOfType<ScenarioManager>() != null
        );
        Debug.Log("Found the scenarioManger and waiting room, spawning Player");
        _Spawn_Client_Internal(clientID);
    }

    private void _Spawn_Client_Internal(ulong clientID) {
        var success = participants.GetOrder(clientID, out ParticipantOrder po);

        if (po == ParticipantOrder.None || success == false) return;

        if (_GetCurrentSpawingData(clientID, out Pose tempPose) &&
            participants.GetJoinType(po, out JoinType joinType) &&
            participants.GetSpawnType(po, out SpawnType spawnType)) {
            if (_VerifyPrefabAvalible(joinType)) {
                var mainParticipantObject =
                    Instantiate(JoinType_To_Client_Object[joinType],
                        tempPose.position, tempPose.rotation);
                //   DontDestroyOnLoad(mainParticipantObject);
                Debug.Log($"obj:{mainParticipantObject.name} n o:{mainParticipantObject.GetComponent<NetworkObject>()} spawn:{mainParticipantObject.IsSpawned}");
               // mainParticipantObject.GetComponent<NetworkObject>().Spawn();
                mainParticipantObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, false);
                Debug.Log($"I just spawned:{mainParticipantObject.IsSpawned}");

                Main_ParticipantObjects.Add(po, mainParticipantObject.GetComponent<Client_Object>());
                Main_ParticipantObjects[po].SetParticipantOrder(po);
                m_QNDataStorageServer.SetupForNewRemoteImage(po);
                mainParticipantObject.SetSpawnType(spawnType);
            }
            else {
                Debug.LogError($"Could Not spawn a ClientObject for PO:{po} joinType:{joinType} spawnType:{spawnType}");
            }
        }
    }

    public ScenarioManager GetScenarioManager() {
        CurrentScenarioManager = FindObjectOfType<ScenarioManager>();
        if (CurrentScenarioManager == null)
            Debug.LogError(
                "Tried to find a scenario manager(probably to spawn cars), but they was nothing. Did you load your scenario(subscene)?");

        return CurrentScenarioManager;
    }

  
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response) {
        var approve = false;


        JoinParameters cdr =
            JsonConvert.DeserializeObject<JoinParameters>(Encoding.ASCII.GetString(request.Payload));


        approve = participants.AddParticipant(cdr._participantOrder, request.ClientNetworkId, cdr._spawnType, cdr._jointype);

        if (!approve) {
            Debug.Log("Participant Order " + request.Payload[0] +
                      " tried to join, but we already have a participant with that order. ");
        }
        else {
            // ParticipantObjects.Add(request.ClientNetworkId, new Dictionary<SpawnType, NetworkObject>());
            // populate Interactable_ParticipantObjects with the current participant
            Interactable_ParticipantObjects.Add(cdr._participantOrder, new List<Interactable_Object>());
            Debug.Log("Client will connect now!");
        }


        response.Approved = approve;
        response.CreatePlayerObject = false;
        response.Pending = false;
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
        ServerStateChange.Invoke(ActionState.WAITINGROOM);

        ServerLoadScene(WaitingRoomSceneName);
    }

    public void SwitchToLoading(string name) {
        ServerState = ActionState.LOADINGSCENARIO;
        ServerStateChange.Invoke(ActionState.LOADINGSCENARIO);

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
        ServerState = ActionState.READY;
        ServerStateChange.Invoke(ActionState.READY);
    }


    private void SwitchToDriving() {
        if (!farlab_logger.Instance.ReadyToRecord()) {
            Debug.LogWarning(
                "I was trying to start recording while something else was still storring Data. Try again in a moment!");
            return;
        }

        ServerState = ActionState.DRIVE;
        ServerStateChange.Invoke(ActionState.DRIVE);

        m_ReRunManager.BeginRecording(LastLoadedScene);
        m_QNDataStorageServer.StartScenario(LastLoadedScene, m_ReRunManager.GetRecordingFolder());
        farlab_logger.Instance.StartRecording(m_ReRunManager, LastLoadedScene, m_ReRunManager.GetRecordingFolder());
    }


    public void SwitchToQN() {
        Debug.Log("Stopping Driving and Stopping the recording.");
        m_ReRunManager.StopRecording();

        ServerState = ActionState.QUESTIONS;
        ServerStateChange.Invoke(ActionState.QUESTIONS);

        QNFinished = new Dictionary<ParticipantOrder, bool>();
        foreach (var po in participants.GetAllConnectedParticipants()) QNFinished.Add(po, false);


        foreach (var no in FindObjectsOfType<Interactable_Object>()) {
            no.Stop_Action();
        }


        foreach (var po in Main_ParticipantObjects.Keys) {
            
            
            Main_ParticipantObjects[po].GetComponent<Client_Object>()
                .StartQuestionair(m_QNDataStorageServer);

        }

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
        ServerStateChange.Invoke(ActionState.POSTQUESTIONS);

        SwitchToWaitingRoom();
    }

    #endregion
}