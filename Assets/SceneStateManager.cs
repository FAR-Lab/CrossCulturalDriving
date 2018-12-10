using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.XR;


public enum ClientState { HOST, CLIENT, DISCONECTED, NONE };

public class SceneStateManager : NetworkManager
{

    private static SceneStateManager _instance;
    private ClientState myState = ClientState.NONE;
    private uint myID = 0;

    public uint MyID { get { return myID; } }
    public static SceneStateManager Instance { get { return _instance; } }
    public ClientState MyState { get { return myState; } }
    private NetworkManager manager;

    private List <uint> activeConnectedIds=new List<uint>();

    public float spawnHeight = 1;
    NetworkClient client_;

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
        manager = FindObjectOfType<NetworkManager>();
            StartCoroutine(LoadYourAsyncAddScene("Lobby"));

    }
    void Update(){
        if(myState == ClientState.CLIENT){
            if(!client.isConnected){
                myState = ClientState.DISCONECTED;
            }
        }
        if(Input.GetKeyUp(KeyCode.Space))
        {
            InputTracking.Recenter();
        }
    }

    public void ConnectToServerWith(string ip, uint playerID){
       
        manager.networkAddress = ip;
        myID = playerID;
        client_ = manager.StartClient();
        myState = ClientState.CLIENT;

    }
    public void HostServer(uint playerID)
    {
        myID = playerID;
        client_ = manager.StartHost();
        myState = ClientState.HOST;
    }

    //----//
    public override void OnServerConnect(NetworkConnection conn)
    {
        Debug.Log(myID);
        Debug.Log("OnPlayerConnected");
        conn.RegisterHandler(MsgType.AddPlayer, reportClientID);
    }
    public override void OnClientConnect(NetworkConnection conn)
    {
        Debug.Log(conn.connectionId + " Connected!");
        // CmdSpawnMeNow(myID, conn);
        SpawnMessage newSpawnMessage = new SpawnMessage();
        newSpawnMessage.netId= myID;
        conn.Send(MsgType.AddPlayer, newSpawnMessage);
        
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
            GameObject player = (GameObject)Instantiate(playerPrefab, SpawnPosition, SpawnOrientation);
            NetworkServer.AddPlayerForConnection(msg.conn, player, 0);
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