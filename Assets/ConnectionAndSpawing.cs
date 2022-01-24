using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using UnityEditor;
using UnityEngine.SceneManagement;


public class ConnectionAndSpawing : MonoBehaviour {
    public GameObject PlayerPrefab;
    public GameObject CarPrefab;
    public GameObject VRUIStartPrefab;

    public List<SceneField> IncludedScenes = new List<SceneField>();
    public string WaitingRoomSceneName;
    public bool ServerisRunning;
    private GameObject myStateManager;

    private ParticipantOrder _participantOrder = ParticipantOrder.None;

    public ParticipantOrder ParticipantOrder => _participantOrder;

    //Internal StateTracking
    private bool ParticipantOrder_Set = false;
    private ScenarioManager CurrentScenarioManager;

    public ActionState ServerState; // { get; private set; }

    public LanguageSelect lang { private set; get; }


    public bool RunAsServer;
    public static bool fakeCare = false;

    #region ParticipantMapping

    enum ParticipantObjectSpawnType {
        MAIN,
        CAR,
        PEDESTRIAN
    }


    private Dictionary<ParticipantOrder, ulong> _OrderToClient;
    private Dictionary<ulong, ParticipantOrder> _ClientToOrder;

    private Dictionary<ulong, Dictionary<ParticipantObjectSpawnType, NetworkObject>> ClientObjects =
        new Dictionary<ulong, Dictionary<ParticipantObjectSpawnType, NetworkObject>>();


    private bool initalSceneLoaded = false;

    private bool AddParticipant(ParticipantOrder or, ulong id) {
        bool outval = false;
        if (_OrderToClient == null) { initDicts(); }

        if (!_OrderToClient.ContainsKey(or)) {
            _OrderToClient.Add(or, id);
            _ClientToOrder.Add(id, or);


            ClientObjects.Add(id, new Dictionary<ParticipantObjectSpawnType, NetworkObject>());


            outval = true;
        }

        return outval;
    }

    private void RemoveParticipant(ulong id) {
        ParticipantOrder or = GetOrder(id);
        if (_OrderToClient.ContainsKey(or) && _ClientToOrder.ContainsKey(id)) {
            _OrderToClient.Remove(or);
            _ClientToOrder.Remove(id);
            ClientObjects.Remove(id);
        }
    }

    private void initDicts() {
        _OrderToClient = new Dictionary<ParticipantOrder, ulong>();
        _ClientToOrder = new Dictionary<ulong, ParticipantOrder>();
        ClientListInitDone = true;
    }

    private ulong? GetClientID(ParticipantOrder or) {
        if (_OrderToClient == null) { initDicts(); }

        if (CheckOrder(or)) { return _OrderToClient[or]; }
        else { return null; }
    }

    private bool CheckOrder(ParticipantOrder or) {
        if (_OrderToClient == null) { initDicts(); }

        return _OrderToClient.ContainsKey(or);
    }

    private bool CheckClientID(ulong id) {
        if (_OrderToClient == null) { initDicts(); }

        return _ClientToOrder.ContainsKey(id);
    }

    private ParticipantOrder GetOrder(ulong id) {
        if (_OrderToClient == null) { initDicts(); }

        if (CheckClientID(id)) { return _ClientToOrder[id]; }
        else { return ParticipantOrder.None; }
    }

    private int GetParticipantCount() {
        if (_ClientToOrder == null || _OrderToClient == null) { return -1; }

        if (_ClientToOrder.Count == _OrderToClient.Count) { return _ClientToOrder.Count; }
        else {
            Debug.LogError(
                "Our Participant Connection has become inconsistent. This is bad. Please restart and tell david!");
            return -1;
        }
    }

    #endregion


    #region SingeltonManagment

    public static ConnectionAndSpawing Singleton { get; private set; }
    private void SetSingleton() { Singleton = this; }

    private void OnEnable() {
        if (Singleton != null && Singleton != this) {
            Destroy(gameObject);
            return;
        }

        SetSingleton();
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy() {
        if (Singleton != null && Singleton == this) { Singleton = null; }
    }

    #endregion


    #region SpawingAndConnecting

    void SetupServerFunctionality() {
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.OnServerStarted += ServerHasStarted;


        NetworkManager.Singleton.StartServer();
        NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneEvent;


        SteeringWheelManager.Singleton.Init(); //TODO enable steering wheel
    }

    void SetupClientFunctionality() {
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected_client;
        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected_client;
    }


    private void LocalLoadScene(string name) {
        DestroyAllClientObjects(new List<ParticipantObjectSpawnType> {ParticipantObjectSpawnType.CAR});

        //PreviousScene = ActiveScene;
        //  ActiveScene = name;
        NetworkManager.Singleton.SceneManager.LoadScene(name, LoadSceneMode.Single);
    }

    private void SceneEvent(SceneEvent sceneEvent) {
        switch (sceneEvent.SceneEventType) {
            case SceneEventType.Load:
            case SceneEventType.Unload: break;
            case SceneEventType.Synchronize: break;
            case SceneEventType.ReSynchronize: break;
            case SceneEventType.LoadEventCompleted:
                if (ServerState == ActionState.LOADING) { SwitchToReady(); }

                break;
            case SceneEventType.UnloadEventCompleted:
                break;
            case SceneEventType.LoadComplete:
                // SpawnAPlayer(sceneEvent.ClientId);
                SpawnACar(sceneEvent.ClientId);
                break;
            case SceneEventType.UnloadComplete: break;
            case SceneEventType.SynchronizeComplete: break;
            default: throw new ArgumentOutOfRangeException();
        }
    }
    // DestroyAllClientObjects_OfType(ParticipantObjectSpawnType TypeToDestroy)// TODO would make implementing more features easier

    private void DestroyAllClientObjects(List<ParticipantObjectSpawnType> TypesToDestroy) {
        bool destroyMain = TypesToDestroy.Contains(ParticipantObjectSpawnType.MAIN);
        if (destroyMain) { TypesToDestroy.Remove(ParticipantObjectSpawnType.MAIN); }

        foreach (ulong id in ClientObjects.Keys) {
            foreach (ParticipantObjectSpawnType enumval in TypesToDestroy) {
                {
                    DespawnObjectOfType(id, enumval);
                }
            }

            if (destroyMain) { DespawnObjectOfType(id, ParticipantObjectSpawnType.MAIN); }
        }
    }

    private void DespawnObjectOfType(ulong clientID, ParticipantObjectSpawnType oType) {
        if (ClientObjects[clientID].ContainsKey(oType)) {
            Debug.Log("Removing for:" + clientID + " object:" + oType);
            NetworkObject no = ClientObjects[clientID][oType];
            switch (oType) {
                case ParticipantObjectSpawnType.MAIN:
                    Debug.Log("WARNING despawing MAIN!");
                    break;
                case ParticipantObjectSpawnType.CAR:
                    ClientRpcParams clientRpcParams = new ClientRpcParams {
                        Send = new ClientRpcSendParams {
                            TargetClientIds = new ulong[] {clientID}
                        }
                    };
                    if (ClientObjects[clientID][ParticipantObjectSpawnType.MAIN] != null) {
                        ClientObjects[clientID][ParticipantObjectSpawnType.MAIN].GetComponent<ParticipantInputCapture>()
                            .De_AssignCarTransform(clientRpcParams);
                    }

                    break;
                case ParticipantObjectSpawnType.PEDESTRIAN: break;
                default: throw new ArgumentOutOfRangeException(nameof(oType), oType, null);
            }

            no.Despawn(true);
            ClientObjects[clientID].Remove(oType);
        }
    }


    public enum ClienConnectionResponse {
        SUCCESS,
        FAILED
    };

    public delegate void ReponseDelegate(ClienConnectionResponse response);

    private ReponseDelegate ReponseHandler;
    private void ClientDisconnected_client(ulong ClientID) { ReponseHandler.Invoke(ClienConnectionResponse.FAILED); }
    private void ClientConnected_client(ulong ClientID) { ReponseHandler.Invoke(ClienConnectionResponse.SUCCESS); }


    private void ClientDisconnected(ulong ClientID) {
        foreach (var obj in ClientObjects[ClientID].Values) { obj.Despawn(true); }

        RemoveParticipant(ClientID);
    }

    private void ClientConnected(ulong ClientID) {
        //      if (! NetworkManager.Singleton.IsServer) return;
//      if(SceneSwitchingFinished) SpawnAPlayer(ClientID);
    }

    private bool _prepareSpawing(ulong clientID, out Pose? tempPose) {
        bool success = true;
        tempPose = GetScenarioManager().GetStartPose(GetOrder(clientID));
        if (tempPose == null) { success = false; }

        return success;
    }

    private bool SpawnACar(ulong clientID) {
        ParticipantOrder temp = GetOrder(clientID);
        if (temp == ParticipantOrder.None) return false;

        if (_prepareSpawing(clientID, out Pose? tempPose)) {
            var newCar =
                Instantiate(CarPrefab,
                    tempPose.Value.position, tempPose.Value.rotation);

            //newCar.name = "XE_Rigged_Networked_" + GetOrder(clientID);

            ClientRpcParams clientRpcParams = new ClientRpcParams {
                Send = new ClientRpcSendParams {
                    TargetClientIds = new ulong[] {clientID}
                }
            };


            newCar.GetComponent<NetworkObject>().Spawn(true);

            if (!fakeCare) {
                newCar.GetComponent<NetworkVehicleController>().AssignClient(clientID, GetOrder(clientID));


                Debug.Log("Assigning car to a new partcipant with clinetID:" + clientID.ToString() + " =>" +
                          newCar.GetComponent<NetworkObject>().NetworkObjectId);
                if (ClientObjects[clientID][ParticipantObjectSpawnType.MAIN] != null) {
                    // ClientObjects[clientID][ParticipantObjectSpawnType.MAIN].GetComponent<ParticipantInputCapture>()
                    //  .AssignCarTransformClientRPC(newCar.GetComponent<NetworkObject>(), GetOrder(clientID), lang,
                    //        clientRpcParams);

                    ClientObjects[clientID][ParticipantObjectSpawnType.MAIN].GetComponent<ParticipantInputCapture>()
                        .AssignCarTransform(newCar.GetComponent<NetworkVehicleController>(), clientRpcParams);
                }

                else { Debug.LogError("Could not find player as I am spawing the CAR. Broken please fix."); }
            }

            ClientObjects[clientID].Add(ParticipantObjectSpawnType.CAR, newCar.GetComponent<NetworkObject>());

            return true;
        }

        return false;
    }

    private bool SpawnAPlayer(ulong clientID, bool persistent) {
        ParticipantOrder temp = GetOrder(clientID);
        if (temp == ParticipantOrder.None) return false;

        if (_prepareSpawing(clientID, out Pose? tempPose)) {
            tempPose ??= Pose.identity;

            var newPlayer =
                Instantiate(PlayerPrefab,
                    tempPose.Value.position, tempPose.Value.rotation);

            newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, !persistent);
            ClientObjects[clientID].Add(ParticipantObjectSpawnType.MAIN, newPlayer.GetComponent<NetworkObject>());
            return true;
        }

        return false;
    }

    private ScenarioManager GetScenarioManager() {
        CurrentScenarioManager = FindObjectOfType<ScenarioManager>();
        if (CurrentScenarioManager == null) {
            Debug.LogError(
                "Tried to find a scenario manager(probably to spawn cars), but they was nothing. Did you load your scenario(subscene)?");
        }

        return CurrentScenarioManager;
    }


    private void ApprovalCheck(byte[] connectionData, ulong clientId,
        NetworkManager.ConnectionApprovedDelegate callback) {
        bool approve = false;
        ParticipantOrder temp = (ParticipantOrder) connectionData[0];

        approve = AddParticipant(temp, clientId);

        if (!approve) {
            Debug.Log("Participant Order " + connectionData +
                      " tried to join, but we already have a participant with that order. " +
                      "Try to change the -po commandline argument of the participant that is" +
                      " trying to connect.");
        }

        callback(false, 0, approve, null, null);

        SpawnAPlayer(clientId, true);
    }

    #endregion


    public void StartAsServer() {
        Debug.Log("Starting as Server");
        SteeringWheelManager.Singleton.enabled = true;
        GetComponent<QNDataStorageServer>().enabled = true;
        SetupServerFunctionality();
    }

    private void SetParticipantOrder(ParticipantOrder val) {
        NetworkManager.Singleton.NetworkConfig.ConnectionData = new byte[] {(byte) val}; // assigning ID
        _participantOrder = val;
        ParticipantOrder_Set = true;
    }

    private void Setlanguage(LanguageSelect lang_) { lang = lang_; }

    public void StartAsClient(LanguageSelect lang_, ParticipantOrder val, string ip, int port, ReponseDelegate result) {
        SetupClientFunctionality();
        ReponseHandler += result;
        SetupTransport(ip, port);
        Setlanguage(lang_);
        SetParticipantOrder(val);
        Debug.Log("Starting as Client");
        NetworkManager.Singleton.StartClient();
    }

    private void SetupTransport(string ip = "127.0.0.1", int port = 7777) {
        NetworkManager.Singleton.GetComponent<UNetTransport>().ConnectAddress = ip;
        NetworkManager.Singleton.GetComponent<UNetTransport>().ConnectPort = port;
    }

    private void ServerHasStarted() {
        ServerisRunning = true;
        SwitchToWaitingRoom();
    }

    #region StateChangeCalls

    private void SwitchToWaitingRoom() {
        ServerState = ActionState.WAITINGROOM;
        LocalLoadScene(WaitingRoomSceneName);
    }

    private void SwitchToLoading(string name) {
        ServerState = ActionState.LOADING;
        LocalLoadScene(name);
    }


    private void SwitchToReady() { ServerState = ActionState.READY; }

    private void SwitchToDriving() {
        //  foreach (var nvc in FindObjectsOfType<NetworkVehicleController>()) { nvc.StartTheCar();}
        ServerState = ActionState.DRIVE;
    }

    public void SwitchToQN() {
        //   Debug.Log("QN triggered, canceling Velocities, and start Questionnaires");
        ServerState = ActionState.QUESTIONS;
        QNFinished = new Dictionary<ParticipantOrder, bool>();
        foreach (ParticipantOrder po in _OrderToClient.Keys) { QNFinished.Add(po, false); }

        foreach (ulong client in ClientObjects.Keys) {
            foreach (NetworkVehicleController no in FindObjectsOfType<NetworkVehicleController>()) {
                no.transform.GetComponent<Rigidbody>().velocity = Vector3.zero;
                no.transform.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            }
        }

        foreach (ulong clinet in NetworkManager.Singleton.ConnectedClients.Keys) {
            ParticipantInputCapture inCapture =
                NetworkManager.Singleton.ConnectedClients[clinet].PlayerObject
                    .GetComponent<ParticipantInputCapture>();
            if (inCapture != null) { inCapture.StartQuestionnaireClientRpc(); }
        }
    }

    private void SwitchToPostQN() {
        ServerState = ActionState.POSTQUESTIONS;
        SwitchToWaitingRoom();
    }

    #endregion

    private bool retry = true;
    private void ResponseDelegate(ConnectionAndSpawing.ClienConnectionResponse response) { }

    private bool started = false;

    void Start() {
        if (Application.platform == RuntimePlatform.Android && !started) {
            StartAsClient("English", ParticipantOrder.B, "192.168.1.160", 7777, ResponseDelegate);
            started = true;
            Debug.Log("Started Client");
        }
    }

    // Update is called once per frame
    void Update() {
        if (NetworkManager.Singleton.IsServer) {
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.D)) {
                DestroyAllClientObjects(new List<ParticipantObjectSpawnType> {ParticipantObjectSpawnType.CAR});
            }

            switch (ServerState) {
                case ActionState.DEFAULT: break;
                case ActionState.WAITINGROOM: break;
                case ActionState.LOADING: break;
                case ActionState.READY:
                    if (Input.GetKeyUp(KeyCode.Return)) {
                        SwitchToDriving();
                        SetStartingGPSDirections();
                    }

                    break;
                case ActionState.DRIVE:
                    if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Q)) {
                        Debug.Log("Forcing back to Waitingroom from" + ServerState.ToString());
                        SwitchToPostQN();
                    }
                    else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Q)) {
                        SwitchToQN();
                    }

                    break;
                case ActionState.QUESTIONS:
                    if (!QNFinished.ContainsValue(false)) { SwitchToPostQN(); }

                    if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Q)) {
                        Debug.Log("Forcing back to Waitingroom from" + ServerState.ToString());
                        SwitchToPostQN();
                    }

                    break;
                case ActionState.POSTQUESTIONS: break;
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }


    void OnGUI() {
        if (NetworkManager.Singleton == null && !ClientListInitDone) return;
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) {
            GUI.Label(new Rect(5, 5, 150, 50), "Server: " + ParticipantOrder + " " +
                                               NetworkManager.Singleton.ConnectedClients.Count + " " +
                                               GetParticipantCount() + "  " +
                                               ServerState + "  " + Time.timeScale);
            if (ServerState == ActionState.WAITINGROOM) {
                int y = 50;
                foreach (SceneField f in IncludedScenes) {
                    if (GUI.Button(new Rect(5, 5 + y, 150, 25), f.SceneName)) { SwitchToLoading(f.SceneName); }

                    y += 27;
                }

                y = 50;
                if (_OrderToClient == null) return;
                foreach (var p in _OrderToClient.Keys) {
                    if (GUI.Button(new Rect(200, 5 + y, 100, 25), "Calibrate " + p)) {
                        ulong clientID = _OrderToClient[p];
                        ClientRpcParams clientRpcParams = new ClientRpcParams {
                            Send = new ClientRpcSendParams {
                                TargetClientIds = new ulong[] {clientID}
                            }
                        };

                        ClientObjects[clientID][ParticipantObjectSpawnType.MAIN].GetComponent<ParticipantInputCapture>()
                            .CalibrateClientRPC(clientRpcParams);
                    }
                }
            }

            else if (ServerState == ActionState.QUESTIONS) {
                int y = 50;
                foreach (ParticipantOrder f in QNFinished.Keys) {
                    GUI.Label(new Rect(5, 5 + y, 150, 25), f + "  " + QNFinished[f].ToString());
                    y += 27;
                }
            }
        }
        else if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost) {
            GUI.Label(new Rect(5, 5, 150, 100), "Client: " +
                                                ParticipantOrder + " " +
                                                NetworkManager.Singleton.IsConnectedClient);
        }
    }

    private Dictionary<ParticipantOrder, bool> QNFinished;
    private bool ClientListInitDone = false;

    public void FinishedQuestionair(ulong clientID) {
        ParticipantOrder po = GetOrder(clientID);
        QNFinished[po] = true;
    }

    #region GPSUpdate

    private void SetStartingGPSDirections() {
        UpdateAllGPS(FindObjectOfType<ScenarioManager>().GetStartingPositions());
    }

    public void UpdateAllGPS(Dictionary<ParticipantOrder, GpsController.Direction> dict) {
        foreach (ParticipantOrder or in dict.Keys) {
            ulong? cid = GetClientID(or);
            if (cid != null) {
                NetworkManager.Singleton.ConnectedClients[(ulong) cid].PlayerObject
                    .GetComponent<ParticipantInputCapture>().CurrentDirection.Value = dict[or];
            }
        }
    }

    #endregion


    public List<ulong> GetClientList() {
        if (_ClientToOrder == null) return null;
        return _ClientToOrder.Keys.ToList();
    }

    public Transform GetMainClientObject(ulong senderClientId) {
        if (!ClientObjects.ContainsKey(senderClientId)) return null;
        return ClientObjects[senderClientId].ContainsKey(ParticipantObjectSpawnType.MAIN)
            ? ClientObjects[senderClientId][ParticipantObjectSpawnType.MAIN].transform
            : null;
    }

    public ParticipantOrder GetParticipantOrderClientId(ulong clientid) {
        if (_ClientToOrder.ContainsKey(clientid)) return _ClientToOrder[clientid];
        else return ParticipantOrder.None;
    }
}
