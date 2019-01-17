using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.XR;


public enum ClientState { HOST, CLIENT, DISCONECTED, NONE };

public enum ActionState { LOADING, PREDRIVE, READY, DRIVE, QUESTIONS, POSTQUESTIONS, WAITING };

public enum ServerState { NONE, LOADING, WAITING, RUNNING }
//public enum StateMessageType { READY, QUESTIONAIR,SLOWTIME,FINISHED};

public class SceneStateManager : NetworkManager {


    public class StateUpdateMessag : MessageBase {
        public ActionState actionState;
        public string[] content;
        public float time;
    }
    private struct RemoteClientState {
        public ActionState TheActionState;
        public uint participantID;
        public float timeScale;
        // public NetworkConnection conn;
    }
    Queue<StateUpdateMessag> messageQueue = new Queue<StateUpdateMessag>();
    private List<int> ClientsThatReportedReady = new List<int>();
    public VehicleInputControllerNetworked localPlayer;
    private static SceneStateManager _instance;
    private ClientState myState = ClientState.NONE;
    [SerializeField]
    private ActionState localActionState = ActionState.PREDRIVE;
    public ServerState serverState = ServerState.NONE;
    private uint myID = 0;


    public uint MyID { get { return myID; } }
    public static SceneStateManager Instance { get { return _instance; } }
    public ClientState MyState { get { return myState; } }

    public ActionState ActionState { get { return localActionState; } }
    private NetworkManager manager;

    private Dictionary<NetworkConnection, RemoteClientState> activeConnectedIds = new Dictionary<NetworkConnection, RemoteClientState>();
    public string serverIP;

    public static float spawnHeight = 1;
    public static float slowDownSpeed = 10f;

    public static float slowTargetTime = 0.5f;

    [SerializeField]
    public SceneField[] SceneConditions;


    NetworkClient client_;
    public NetworkClient ThisClient { get { return client_; } }
    GameObject LocalCamera;

    private bool showControlPanel;

    //private bool useVR = false;


    private void Awake() {
        if (_instance != null && _instance != this) {
            Destroy(this.gameObject);
        } else {
            _instance = this;
            // DontDestroyOnLoad(this);
        }
    }
    void Start() {
        // XRSettings.enabled = false;
        //LocalCamera = Camera.main.gameObject;
        //DontDestroyOnLoad(LocalCamera);
        manager = FindObjectOfType<NetworkManager>();
        StartCoroutine(LoadYourAsyncAddScene("Lobby"));

    }
    private void OnGUI() {
        if (myState == ClientState.CLIENT && ThisClient != null && ThisClient.connection != null) {
            GUI.Label(new Rect(25, 430, 600, 25), ( ThisClient.connection.lastMessageTime - Time.time ).ToString());

        }

        /*
        if (myState == ClientState.HOST) {
            int inMSG, inByte, outMSG, outBufMSG, outByte, outBufByteS;
            NetworkServer.GetStatsIn(out inMSG, out inByte);
            NetworkServer.GetStatsOut(out outMSG, out outBufMSG, out outByte, out outBufByteS);
            GUI.Label(new Rect(25, 400, 600, 25), "Server\t outmsg: " + outMSG + "\t outBufMSG: " + outBufMSG + "\toutByte: " + outByte + "\t outBufByteS: " + outBufByteS);
            GUI.Label(new Rect(25, 430, 600, 25), "inMSG: " + inMSG + "\t inByte: " + inByte);

        } else if (myState == ClientState.CLIENT) {
            int inMSG, inByte, outMSG, outBufMSG, outByte, outBufByteS;
            ThisClient.GetStatsIn(out inMSG, out inByte);
            ThisClient.GetStatsOut(out outMSG, out outBufMSG, out outByte, out outBufByteS);
            GUI.Label(new Rect(25, 400, 600, 25), "Client\t outmsg: " + outMSG + "\t outBufMSG: " + outBufMSG + "\toutByte: " + outByte + "\t outBufByteS: " + outBufByteS);
            GUI.Label(new Rect(25, 430, 600, 25), "inMSG: " + inMSG + "\t inByte: " + inByte);


        }
        */


        if (showControlPanel) {
            float boxWidth = 200;
            float boxHeight = 50;
            GUIStyle s = new GUIStyle();
            s.fontSize = 24;
            s.alignment = TextAnchor.MiddleCenter;
            

            if (myState == ClientState.HOST) {

                GUI.Label(new Rect(25, 0, 150, 25), "Cl. Conn: " + activeConnectedIds.Count + "Cl. Ready: " + ClientsThatReportedReady.Count);

                for (int i = 0; i < SceneConditions.Length; i++) {
                    if (GUI.Button(new Rect(25, 100 + i * boxHeight, boxWidth, boxHeight), SceneConditions[i].SceneName)) {
                        loadNextCondition(SceneConditions[i]);
                    }
                }
                float x = 250;
                foreach (RemoteClientState stat in activeConnectedIds.Values) {
                    GUI.Label(new Rect(x, 50, boxWidth, boxHeight), "Participant ID: " + stat.participantID, s);
                    GUI.Label(new Rect(x, 100, boxWidth, boxHeight), "state: " + stat.TheActionState.ToString(), s);
                    GUI.Label(new Rect(x, 150, boxWidth, boxHeight), "TimeScale: " + stat.timeScale, s);
                    x += boxWidth;
                }
            }
        } else {
            GUI.Label(new Rect(25, 0, 150, 25), "Cl. Conn: " + activeConnectedIds.Count + "Cl. Ready: " + ClientsThatReportedReady.Count);


        }
        if (GUI.Button(new Rect(0, 0, 25, 25), "CP")) {
            showControlPanel = !showControlPanel;
        }
    }
    private void loadNextCondition(string sc) {
        ClientsThatReportedReady.Clear();
        ServerChangeScene(sc);
        serverState = ServerState.LOADING;
    }
    public override void OnServerSceneChanged(string sceneName) {

        Debug.Log("OnServerSceneChanged was caled =>\t" + sceneName);

    }

    public override void OnClientSceneChanged(NetworkConnection conn) {
        Debug.Log("OnClientSceneChanged was caled =>\t" + conn.connectionId);
        if (!conn.isReady) {
            ClientScene.Ready(conn);
        }

            if (SceneManager.GetActiveScene().name != "Lobby") {
                SetPreDriving();
            }


    }
    public override void OnServerReady(NetworkConnection conn) {
        Debug.Log("OnServerReady was caled =>\t" + conn.connectionId);
        ClientsThatReportedReady.Add(conn.connectionId);
        Debug.Log("I should probably respawn a player here.");

    }

    public override void OnClientNotReady(NetworkConnection conn) {
        Debug.Log("OnClientNotReady was caled =>\t" + conn.connectionId);
    }




    void Update() {

        if (localPlayer == null) {
            foreach (VehicleInputControllerNetworked v in FindObjectsOfType<VehicleInputControllerNetworked>()) {
                if (v.isLocalPlayer) {
                    localPlayer = v;
                    break;
                }
            }
        }
       
        if (ThisClient != null && ThisClient.connection != null) {
            if (ThisClient.connection.isReady) {
                
                if (messageQueue.Count > 0) {
                    ThisClient.Send(NetworkMessageType.StateUpdate, messageQueue.Dequeue());
                }
            }
            // Debug.Log(ThisClient.connection.lastError);
        }
        //foreach (short s in NetworkServer.GetConnectionStats().Keys) {
        //   Debug.Log("Short: " + s);
        //    Debug.Log("PacketStat" + NetworkServer.GetConnectionStats()[s]);
        //  }
        if (serverState == ServerState.NONE) {
            return;
        }
        Debug.Log(serverState.ToString() + "as well as" + ActionState.ToString());
        Debug.Log(clientStateReturn(ActionState.PREDRIVE) + "Count in Predrive");

        foreach (RemoteClientState c in activeConnectedIds.Values) {
            Debug.Log(c.TheActionState);
        }
        if (serverState == ServerState.LOADING) {
            if  (clientStateReturn(ActionState.WAITING) == activeConnectedIds.Count) {
                showControlPanel = true;
            }
            
            else if (clientStateReturn(ActionState.PREDRIVE) == activeConnectedIds.Count) {
                //LocalCamera.SetActive(false); //TODO CAMERA LOBBY LOADING?
                Debug.Log("About to instasiace all kinds of cars here for the players");
                    ClientsThatReportedReady.Clear();
                    serverState = ServerState.RUNNING;
                    foreach (NetworkConnection id in activeConnectedIds.Keys) {
                        bool success = false;
                        Vector3 SpawnPosition = Vector3.zero;
                        Quaternion SpawnOrientation = Quaternion.identity;
                        foreach (NetworkStartPosition p in FindObjectsOfType<NetworkStartPosition>()) {
                            if (activeConnectedIds[id].participantID == uint.Parse(p.transform.name[p.transform.name.Length - 1].ToString())) {  /// TODO CHANGED CONDITION;
                                SpawnPosition = p.transform.position;
                                SpawnOrientation = p.transform.rotation;
                                success = true;
                                break;
                            }
                        }
                        if (success) {
                            GameObject player = (GameObject)Instantiate(playerPrefab, SpawnPosition, SpawnOrientation);
                            NetworkServer.AddPlayerForConnection(id, player, 0);
                        }
                    }
                    showControlPanel = false;
                }
        } else if (serverState == ServerState.RUNNING) {
            if (clientStateReturn(ActionState.READY) == activeConnectedIds.Count) {

            }
            else if (clientStateReturn(ActionState.QUESTIONS) == activeConnectedIds.Count) {

            } else if (clientStateReturn(ActionState.POSTQUESTIONS) == activeConnectedIds.Count) {
                loadNextCondition("Lobby");
            }
        } else if (serverState == ServerState.WAITING) {
            if (ClientsThatReportedReady.Count == activeConnectedIds.Count) {
                //LocalCamera.SetActive(true);//TODO CAMERA LOBBY LOADING?
                showControlPanel = true;

            }
        }
    }
    private int clientStateReturn(ActionState state) {
        int output = 0;

        foreach (RemoteClientState r in activeConnectedIds.Values) {
            if (r.TheActionState == state) {
                output++;
            }
        }
        return output;

    }
    public void ConnectToServerWith(string ip, uint playerID, bool useVROrNot) {
        //useVR = useVROrNot;
        serverIP = ip;
        // manager.networkAddress = ip;
        // myID = playerID;
        //HARDCODED OVERWRITE
        manager.networkAddress = "192.168.0.100";
        myID = 1;
        client_ = manager.StartClient();
        myState = ClientState.CLIENT;
        serverState = ServerState.NONE;
        //LocalCamera.SetActive(false);


    }
    public void HostServer(uint playerID, bool useVROrNot) {
        // useVR = useVROrNot;
        serverIP = "127.0.0.1";
        myID = playerID;///HARDCODED OVERWRITE
        myID = 0;
        client_ = manager.StartHost();
        myState = ClientState.HOST;
        serverState = ServerState.WAITING;


    }
    void activatehandSending(NetworkClient cl) {
        RemoteHandManager[] rhm = FindObjectsOfType<RemoteHandManager>();
        Debug.Log(rhm.Length);

    }

    //----//
    public override void OnServerConnect(NetworkConnection conn) //Runs ONLY on the server
    {
        //Debug.Log(myID);
        // Debug.Log("OnPlayerConnected");
        conn.RegisterHandler(MsgType.AddPlayer, reportClientID);




    }
    public override void OnClientConnect(NetworkConnection conn)// Runs ONLY on the client
    {
        SpawnMessage newSpawnMessage = new SpawnMessage();
        newSpawnMessage.netId = myID;
        conn.Send(MsgType.AddPlayer, newSpawnMessage);

        ThisClient.connection.RegisterHandler(NetworkMessageType.DownloadVRHead, FindObjectOfType<RemoteHandManager>().RecieveOtherVRHead);
        localActionState = ActionState.PREDRIVE;
    }


    //---//
    void reportClientID(NetworkMessage msg) {


        var message = msg.ReadMessage<SpawnMessage>();
        uint playerid = message.netId;



        foreach (RemoteClientState a in activeConnectedIds.Values) {
            if (a.participantID == playerid) {
                msg.conn.Disconnect();
                return;
            }
        }
        RemoteClientState rcs = new RemoteClientState {
            participantID = playerid,
            //TheActionState = ActionState.WAITING
        };


        activeConnectedIds.Add(msg.conn, rcs);

        bool success = false;
        Vector3 SpawnPosition = Vector3.zero;
        Quaternion SpawnOrientation = Quaternion.identity;

        foreach (NetworkStartPosition p in FindObjectsOfType<NetworkStartPosition>()) {
            if (playerid == uint.Parse(p.transform.name[p.transform.name.Length - 1].ToString())) {
                SpawnPosition = p.transform.position;
                SpawnOrientation = p.transform.rotation;
                success = true;

            }
        }
        if (success) {


            msg.conn.RegisterHandler(NetworkMessageType.uploadHand, RecieveHandData);
            msg.conn.RegisterHandler(NetworkMessageType.uploadVRHead, RecieveVRHeadData);
            msg.conn.RegisterHandler(NetworkMessageType.StateUpdate, ReceiveUpdatedState);
            Debug.Log("Update all handlers");


            //msg.conn.UnregisterHandler(NetworkMessageType.uploadHand);
            //msg.conn.UnregisterHandler(NetworkMessageType.StateUpdate);
            //GameObject player = (GameObject)Instantiate(playerPrefab, SpawnPosition, SpawnOrientation);
            //NetworkServer.AddPlayerForConnection(msg.conn, player, 0);
        }
    }

    public override void OnServerDisconnect(NetworkConnection connection) {
        if (activeConnectedIds.ContainsKey(connection)) {
            activeConnectedIds.Remove(connection);
            Debug.Log("ClientDisconnected removed from list");
        }
    }




    public void RecieveHandData(NetworkMessage msg) //Spell check
    {
        //int ms, ad;
        //msg.conn.GetStatsIn(out ms, out ad);
        //Debug.Log("Receving hand Data" +ms+ "  "+ad);
        RemoteHandManager.HandMessage hand = msg.ReadMessage<RemoteHandManager.HandMessage>();
        hand.id = ( 2 * msg.conn.connectionId ) - hand.id;
        foreach (NetworkConnection c in NetworkServer.connections) {
            if (c == msg.conn) {
                //Debug.Log("I already have that information");
                continue;
            }
            c.SendUnreliable(NetworkMessageType.DownloadHand, hand);

        }
    }

    public void RecieveVRHeadData(NetworkMessage msg) //Spell check
    {
        //int ms, ad;
        //msg.conn.GetStatsIn(out ms, out ad);
        //Debug.Log("Receving hand Data" +ms+ "  "+ad);
        RemoteHandManager.VRHeadMessage head = msg.ReadMessage<RemoteHandManager.VRHeadMessage>();
        //hand.id = msg.conn.connectionId - hand.id;
        head.ID = msg.conn.connectionId;
        foreach (NetworkConnection c in NetworkServer.connections) {
            if (c == msg.conn) {
                //Debug.Log("I already have that information");
                continue;
            }


            c.SendUnreliable(NetworkMessageType.DownloadVRHead, head);

        }
    }





    public void ReceiveUpdatedState(NetworkMessage msg) {
        StateUpdateMessag theMessage = msg.ReadMessage<StateUpdateMessag>();

        RemoteClientState rcs = new RemoteClientState {
            TheActionState = theMessage.actionState,
            timeScale = theMessage.time,
            participantID = activeConnectedIds[msg.conn].participantID
        };

        activeConnectedIds[msg.conn] = rcs;


    }

    void ReportCurrentState() {
        StateUpdateMessag msg = new StateUpdateMessag();
        msg.content = new string[1];
        msg.actionState = localActionState;
        msg.time = Time.timeScale;
        messageQueue.Enqueue(msg);
        

    }
    public void SetPreDriving() {
        //TODO maybe contact the server;
        localActionState = ActionState.PREDRIVE;
        ReportCurrentState();
    }

    public void SetReady() {
        //TODO maybe contact the server;
        localActionState = ActionState.READY;
        ReportCurrentState();
    }
    public void SetDriving() {
        //TODO maybe contact the server;
        localActionState = ActionState.DRIVE;
        ReportCurrentState();
    }

    public void SetQuestionair() {
        //TODO maybe contact the server;
        localActionState = ActionState.QUESTIONS;
        ReportCurrentState();
    }
    public void SetPostQuestionair() {
        //TODO maybe contact the server;
        localActionState = ActionState.POSTQUESTIONS;
        ReportCurrentState();
    }

    public void SetWaiting() {
        localActionState = ActionState.WAITING;
        ReportCurrentState();
    }

    //----//
    IEnumerator ReturnToLobbyFromPrevSceneAsync(string lobby, string oldScene) {


        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(lobby, LoadSceneMode.Additive);
        while (!asyncLoad.isDone) {
            yield return null;
        }

        Debug.Log("Finished loading lobby");

        yield return new WaitForSecondsRealtime(1.0f);


        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(oldScene);

        while (!asyncUnload.isDone) {
            yield return null;
        }

    }

    IEnumerator LoadYourAsyncAddScene(string newScene) {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(newScene, LoadSceneMode.Additive);
        while (!asyncLoad.isDone) {
            yield return null;
        }
    }
    IEnumerator UnloadYourAsyncAddScene(string oldScene) {
        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(oldScene);
        while (!asyncUnload.isDone) {
            yield return null;
        }
    }
}