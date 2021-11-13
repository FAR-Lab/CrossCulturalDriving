using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using UnityEditor;
using UnityEngine.SceneManagement;


public class ConnectionAndSpawing : MonoBehaviour {
    public GameObject PlayerPrefab;
    public GameObject CarPrefab;

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


    private bool SceneSwitching = false;
    private bool SceneSwitchingFinished = false;
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

    public void SetParticipantOrder(ParticipantOrder val) {
        NetworkManager.Singleton.NetworkConfig.ConnectionData = new byte[] {(byte) val}; // assigning ID 
        _participantOrder = val;
        ParticipantOrder_Set = true;
    }

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

    IEnumerator WaitForSetup() {
        while (NetworkManager.Singleton == null) { yield return new WaitForSeconds(0.1f); }

        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.OnServerStarted += ServerHasStarted;


        NetworkManager.Singleton.StartServer();
        while (NetworkManager.Singleton.SceneManager == null) { yield return new WaitForSeconds(0.1f); }

        NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneEvent;
        //AddParticipant(_participantOrder, NetworkManager.Singleton.LocalClientId);
        SteeringWheelManager.Singleton.Init();
    }


    private string ActiveScene;
    private string PreviousScene;

    private void LocalLoadScene(string name) {
        DestroyAllClientObjects(true);
        SceneSwitching = true;
        PreviousScene = ActiveScene;
        ActiveScene = name;
        NetworkManager.Singleton.SceneManager.LoadScene(ActiveScene, LoadSceneMode.Single);
    }

    private void SceneEvent(SceneEvent sceneEvent) {
        switch (sceneEvent.SceneEventType) {
            case SceneEventType.Load:
                SceneSwitchingFinished = false;
                break;
            case SceneEventType.Unload: break;
            case SceneEventType.Synchronize: break;
            case SceneEventType.ReSynchronize: break;
            case SceneEventType.LoadEventCompleted:
                if (ServerState == ActionState.LOADING) { SwitchToReady(); }
                break;
            case SceneEventType.UnloadEventCompleted:
                break;
            case SceneEventType.LoadComplete:
                SpawnAPlayer(sceneEvent.ClientId);
                SpawnACar(sceneEvent.ClientId);
                break;
            case SceneEventType.UnloadComplete: break;
            case SceneEventType.SynchronizeComplete: break;
            default: throw new ArgumentOutOfRangeException();
        }
    }


    private void DestroyAllClientObjects(bool DestroyMainPlayerObject = false) {
        foreach (ulong id in ClientObjects.Keys) {
            foreach (ParticipantObjectSpawnType enumval in Enum.GetValues(typeof(ParticipantObjectSpawnType))) {
                if (ClientObjects[id].ContainsKey(enumval)) {
                    NetworkObject no = ClientObjects[id][enumval];
                    if (NetworkManager.Singleton.ConnectedClients[id].PlayerObject == no && DestroyMainPlayerObject) {
                       
                        Debug.Log("Removing player object despanwn: " + no.name);
                        no.Despawn(true);
                        NetworkManager.Singleton.ConnectedClients[id].PlayerObject = null;
                        ClientObjects[id].Remove(enumval);
                    }
                    else if (NetworkManager.Singleton.ConnectedClients[id].PlayerObject != no) {
                        Debug.Log(enumval + "Trying to despanwn: " + no.name);
                        no.Despawn(true);
                        ClientObjects[id].Remove(enumval);
                    }
                }
            }
        }
    }

    private void ClientDisconnected(ulong ClientID) {
        foreach (var obj in ClientObjects[ClientID].Values) { obj.Despawn(true); }

        RemoveParticipant(ClientID);
    }

    private void ClientConnected(ulong ClientID) {
        //      if (! NetworkManager.Singleton.IsServer) return;
//      if(SceneSwitchingFinished) SpawnAPlayer(ClientID);
    }

    private bool _prepareSpawing(ulong ClientID, out Pose? tempPose) {
        bool success = true;
        tempPose = GetScenarioManager().GetStartPose(GetOrder(ClientID));
        if (tempPose == null) { success = false; }

        return success;
    }

    private bool SpawnACar(ulong ClientID) {
        ParticipantOrder temp = GetOrder(ClientID);
        if (temp == ParticipantOrder.None) return false;
        if (_prepareSpawing(ClientID, out Pose? tempPose)) {
            var newCar =
                Instantiate(CarPrefab,
                    tempPose.Value.position, tempPose.Value.rotation);

            newCar.name = "XE_Rigged_Networked_" + GetOrder(ClientID);

            ClientRpcParams clientRpcParams = new ClientRpcParams {
                Send = new ClientRpcSendParams {
                    TargetClientIds = new ulong[] {ClientID}
                }
            };

            newCar.GetComponent<NetworkObject>().Spawn(true);

            newCar.GetComponent<NetworkVehicleController>().AssignClient(ClientID, GetOrder(ClientID));
            // newPlayer.GetComponent<ParticipantInputCapture>().AssignCarLocalServerCall(newCar.GetComponent<VehicleInputControllerNetworked>());
            Debug.Log("Assigning car to a new partcipant with clinetID:" + ClientID.ToString() + " =>" +
                      newCar.GetComponent<NetworkObject>().NetworkObjectId);
            if (ClientObjects[ClientID][ParticipantObjectSpawnType.MAIN] != null) {
                ClientObjects[ClientID][ParticipantObjectSpawnType.MAIN].GetComponent<ParticipantInputCapture>()
                    .AssignCarTransformClientRPC(newCar.GetComponent<NetworkObject>(), GetOrder(ClientID), lang,
                        clientRpcParams);

                ClientObjects[ClientID][ParticipantObjectSpawnType.MAIN].GetComponent<ParticipantInputCapture>()
                    .AssignCarTransform_OnServer(newCar.GetComponent<NetworkVehicleController>());
            }
            else { Debug.LogError("Could not find player as I am spawing the CAR. Broken please fix."); }

            ClientObjects[ClientID].Add(ParticipantObjectSpawnType.CAR, newCar.GetComponent<NetworkObject>());

            return true;
        }

        return false;
    }

    private bool SpawnAPlayer(ulong ClientID) {
        ParticipantOrder temp = GetOrder(ClientID);
        if (temp == ParticipantOrder.None) return false;

        if (_prepareSpawing(ClientID, out Pose? tempPose)) {
            tempPose ??= Pose.identity;

            var newPlayer =
                Instantiate(PlayerPrefab,
                    tempPose.Value.position, tempPose.Value.rotation);

            newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(ClientID, true);
            ClientObjects[ClientID].Add(ParticipantObjectSpawnType.MAIN, newPlayer.GetComponent<NetworkObject>());
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

        //SpawnAPlayer(clientId);
    }

    #endregion


    private StartUpType m_StartUpType;

    enum StartUpType {
        Client,
        Host,
        Server,
        Completed
    }

    private void _internalStart(StartUpType startUpType) {
        Debug.Log(startUpType);
        switch (startUpType) {
            default:
            case StartUpType.Client:
                NetworkManager.Singleton.StartClient();
                Destroy(this);
                break;
            case StartUpType.Server:
            case StartUpType.Host:

                IEnumerator RegisterTask = WaitForSetup();
                StartCoroutine(RegisterTask);

                break;
        }

        m_StartUpType = StartUpType.Completed;
    }


    public void StartAsHost() { m_StartUpType = StartUpType.Host; }

    public void StartAsServer() { m_StartUpType = StartUpType.Server; }

    public void StartAsClient() {
        m_StartUpType = StartUpType.Client;
        SteeringWheelManager.Singleton.enabled = false;
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

    private void SwitchToDriving() { ServerState = ActionState.DRIVE; }

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
                NetworkManager.Singleton.ConnectedClients[clinet].PlayerObject.GetComponent<ParticipantInputCapture>();
            if (inCapture != null) { inCapture.StartQuestionnaireClientRpc(); }
        }
    }

    private void SwitchToPostQN() {
        ServerState = ActionState.POSTQUESTIONS;
        DestroyAllClientObjects(false);
        SwitchToWaitingRoom();
    }

    #endregion

    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update() {
        if (NetworkManager.Singleton != null && m_StartUpType != StartUpType.Completed) {
            _internalStart(m_StartUpType);
        }

        if (NetworkManager.Singleton.IsServer) {
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
                case ActionState.DRIVE: break;
                case ActionState.QUESTIONS:
                    if (!QNFinished.ContainsValue(false)) { SwitchToPostQN(); }

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

    public void Setlanguage(LanguageSelect lang_) { lang = lang_; }

    public List<ulong> GetClientList() {
        if (_ClientToOrder == null) return null;
        return _ClientToOrder.Keys.ToList();
    }
}