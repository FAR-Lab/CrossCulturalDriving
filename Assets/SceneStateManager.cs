using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.XR;


public enum ClientState { HOST, CLIENT, DISCONECTED, NONE };
public enum ActionState { PREDRIVE, DRIVE, QUESTIONS,POSTQUESTIONS };

public class SceneStateManager : NetworkManager
{

    private static SceneStateManager _instance;
    private ClientState myState = ClientState.NONE;
    private ActionState localActionState=ActionState.PREDRIVE;
    private uint myID = 0;

    public uint MyID { get { return myID; } }
    public static SceneStateManager Instance { get { return _instance; } }
    public ClientState MyState { get { return myState; } }
    public ActionState ActionState { get { return localActionState; } }
    private NetworkManager manager;
    //private NetworkServer server;
    private List <uint> activeConnectedIds=new List<uint>();
    public string serverIP;
    public float spawnHeight = 1;
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
    void Update(){
        if(myState == ClientState.CLIENT){
           // Debug.Log(client_.isConnected);
                
           
        }
        if(Input.GetKeyUp(KeyCode.Space))
        {
            FindObjectOfType<seatCallibration>().reCallibrate();
            
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
        //Debug.Log(conn.connectionId + " Connected!");
        // CmdSpawnMeNow(myID, conn);
        SpawnMessage newSpawnMessage = new SpawnMessage();
        newSpawnMessage.netId= myID;
        conn.Send(MsgType.AddPlayer, newSpawnMessage);
        LocalCamera.SetActive(false);
        localActionState = ActionState.DRIVE;

       // if (useVR)
       // {
       //      XRSettings.enabled = true;
       // }


    }
    //---//
    void reportClientID(NetworkMessage msg){ 

        var message = msg.ReadMessage<SpawnMessage>();
        uint playerid = message.netId;

        if (activeConnectedIds.Contains(playerid))
        {
            msg.conn.Disconnect();
        }
        else
        {
            activeConnectedIds.Add(playerid);
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
            msg.conn.RegisterHandler(RemoteHandManager.MessageType.uploadHand, RecieveHandData);
            GameObject player = (GameObject)Instantiate(playerPrefab, SpawnPosition, SpawnOrientation);
            NetworkServer.AddPlayerForConnection(msg.conn, player, 0);
        }
     }

    public void RecieveHandData(NetworkMessage msg)
    {
        int ms, ad;
        msg.conn.GetStatsIn(out ms, out ad);
        Debug.Log("Receving hand Data" +ms+ "  "+ad);
        RemoteHandManager.HandMessage hand = msg.ReadMessage<RemoteHandManager.HandMessage>();
        hand.id = msg.conn.connectionId - hand.id;
        foreach (NetworkConnection c in NetworkServer.connections)
        {
            if (c == msg.conn) {
                Debug.Log("I already have that information");
                continue; }
            c.Send(RemoteHandManager.MessageType.DownloadHand,hand);

        }
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