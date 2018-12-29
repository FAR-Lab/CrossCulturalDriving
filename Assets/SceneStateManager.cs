using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.XR;


public enum ClientState { HOST, CLIENT, DISCONECTED, NONE };
public enum ActionState { LOADING, PREDRIVE, DRIVE, QUESTIONS, WAITING };
public enum ServerState { NONE,LOADING, WAITING,RUNNING}
public enum StateMessageType { READY, QUESTIONAIR,SLOWTIME,FINISHED};

public class SceneStateManager : NetworkManager
{


    public class StateupdateMessag : MessageBase {
        public StateMessageType msgType;
        public string[] content;
        public float time;
    }
    private List<int> ClientsThatReportedReady = new List<int>();


    private static SceneStateManager _instance;
    private ClientState myState = ClientState.NONE;
    [SerializeField]
    private ActionState localActionState=ActionState.PREDRIVE;
    public  ServerState serverState = ServerState.NONE;
    private uint myID = 0;
    

    public uint MyID { get { return myID; } }
    public static SceneStateManager Instance { get { return _instance; } }
    public ClientState MyState { get { return myState; } }
    
    public ActionState ActionState { get { return localActionState; } }
    private NetworkManager manager;

    private Dictionary <uint,NetworkConnection> activeConnectedIds= new Dictionary<uint, NetworkConnection>();
    public string serverIP;

    [SerializeField]
    public static float spawnHeight = 1;
    [SerializeField]
    public static float slowDownSpeed = 1f;
    [SerializeField]
    public static float slowTargetTime=0.1f;
    NetworkClient client_;
    public NetworkClient ThisClient { get { return client_; } }
    GameObject LocalCamera;

    //private bool useVR = false;


    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
           // DontDestroyOnLoad(this);
        }
    }
    void Start()
    {
        // XRSettings.enabled = false;
        LocalCamera = Camera.main.gameObject;
        DontDestroyOnLoad(LocalCamera);
        manager = FindObjectOfType<NetworkManager>();
        StartCoroutine(LoadYourAsyncAddScene("Lobby"));

    }
    private void OnGUI() {
        if (myState == ClientState.HOST) {
            if (GUI.Button(new Rect(10, 10, 200, 50), "Load a Thing")) {
                loadNextCondition("Environment#2");
            }
        }
    }
    private void loadNextCondition(string sc) {
        ClientsThatReportedReady.Clear();
        ServerChangeScene(sc);
        serverState = ServerState.LOADING;
    }
    public override void OnServerSceneChanged( string sceneName) {

        Debug.Log("OnServerSceneChanged was caled =>\t" + sceneName);

    }

    public override void OnClientSceneChanged(NetworkConnection conn) {
        Debug.Log("OnClientSceneChanged was caled =>\t" + conn.connectionId);
        ClientScene.Ready(conn);
        
       
    }
    public override void OnServerReady(NetworkConnection conn) {
        Debug.Log("OnServerReady was caled =>\t" + conn.connectionId);
        ClientsThatReportedReady.Add(conn.connectionId);
        Debug.Log("I should probably respawn a player here.");

    }

    public override void OnClientNotReady(NetworkConnection conn) {
        Debug.Log("OnClientNotReady was caled =>\t" + conn.connectionId);
    }


    void Update(){
        if(myState == ClientState.CLIENT){
           // Debug.Log(client_.isConnected);
                
           
        }
        if(Input.GetKeyUp(KeyCode.Space))
        {
            FindObjectOfType<seatCallibration>().reCallibrate();
            
        }
        if (serverState == ServerState.LOADING) {
            if (ClientsThatReportedReady.Count == activeConnectedIds.Count) {
                serverState = ServerState.RUNNING;
                foreach (uint id in activeConnectedIds.Keys) {
                    bool success = false;
                    Vector3 SpawnPosition = Vector3.zero;
                    Quaternion SpawnOrientation = Quaternion.identity;
                    foreach (NetworkStartPosition p in FindObjectsOfType<NetworkStartPosition>()) {
                        if (id == uint.Parse(p.transform.name[p.transform.name.Length - 1].ToString())) {  /// TODO CHANGED CONDITION;
                            SpawnPosition = p.transform.position;
                            SpawnOrientation = p.transform.rotation;
                            success = true;
                            break;
                        }
                    }if (success) {
                        GameObject player = (GameObject)Instantiate(playerPrefab, SpawnPosition, SpawnOrientation);
                        NetworkServer.AddPlayerForConnection(activeConnectedIds[id], player, 0);
                    }
                }
            }
        }
    }

    public void ConnectToServerWith(string ip, uint playerID,bool useVROrNot)
    {
        //useVR = useVROrNot;
        serverIP = ip;
        manager.networkAddress = ip;
        myID = playerID;
        client_ = manager.StartClient();
        myState = ClientState.CLIENT;
        

    }
    public void HostServer(uint playerID, bool useVROrNot)
    {
        // useVR = useVROrNot;
        serverIP = "127.0.0.1";
        myID = playerID;
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
        newSpawnMessage.netId= myID;
        conn.Send(MsgType.AddPlayer, newSpawnMessage);
        LocalCamera.SetActive(false);
       localActionState = ActionState.PREDRIVE;
    }
    

    //---//
    void reportClientID(NetworkMessage msg){ 

        var message = msg.ReadMessage<SpawnMessage>();
        uint playerid = message.netId;

        if (activeConnectedIds.ContainsKey(playerid))
        {
            msg.conn.Disconnect();
        }
        else
        {
            activeConnectedIds.Add(playerid,msg.conn);
        }
        bool success = false;
        Vector3 SpawnPosition = Vector3.zero;
        Quaternion SpawnOrientation = Quaternion.identity;
        foreach(NetworkStartPosition p in FindObjectsOfType<NetworkStartPosition>())
        {
            if(playerid==uint.Parse(p.transform.name[p.transform.name.Length-1].ToString())){
                SpawnPosition = p.transform.position;
                SpawnOrientation = p.transform.rotation; 
                success = true;

            }
        }
        if (success)
        {
            msg.conn.RegisterHandler(NetworkMessageType.uploadHand, RecieveHandData);
            //GameObject player = (GameObject)Instantiate(playerPrefab, SpawnPosition, SpawnOrientation);
            //NetworkServer.AddPlayerForConnection(msg.conn, player, 0);
        }
     }

    public override void OnServerDisconnect(NetworkConnection connection) {
        if (activeConnectedIds.ContainsValue(connection)){
            foreach (uint i in activeConnectedIds.Keys) {
                if (activeConnectedIds[i] == connection) {
                    activeConnectedIds.Remove(i);
                    Debug.Log("ClientDisconnected removed from list");
                    break;
                }

             }
        }
    }



    public void RecieveHandData(NetworkMessage msg)
    {
        int ms, ad;
        msg.conn.GetStatsIn(out ms, out ad);
        //Debug.Log("Receving hand Data" +ms+ "  "+ad);
        RemoteHandManager.HandMessage hand = msg.ReadMessage<RemoteHandManager.HandMessage>();
        hand.id = msg.conn.connectionId - hand.id;
        foreach (NetworkConnection c in NetworkServer.connections)
        {
            if (c == msg.conn) {
                //Debug.Log("I already have that information");
                continue; }
            c.Send(NetworkMessageType.DownloadHand,hand);

        }
    }
    public void SetDriving() {
        //TODO maybe contact the server;
        localActionState = ActionState.DRIVE;
    }

    public void SetQuestionair() {
        //TODO maybe contact the server;
        localActionState = ActionState.QUESTIONS;
    }

    //----//
    IEnumerator LoadYourAsyncAddScene(string newScene)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(newScene, LoadSceneMode.Additive);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
    IEnumerator UnloadYourAsyncAddScene(string oldScene)
    {
        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(oldScene);
        while (!asyncUnload.isDone)
        {
            yield return null;
        }
    }
}