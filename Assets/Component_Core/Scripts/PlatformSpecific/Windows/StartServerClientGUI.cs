using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class StartServerClientGUI : MonoBehaviour {
    # region Singleton
    public static StartServerClientGUI Singleton;   
    private void Awake() {
        if (Singleton == null) {
            Singleton = this;
        } else {
            Destroy(this);
        }
    }
    # endregion
    
    
    public GameObject ServerStartGUI;
    private Transform ServerGuiSintance;
    
    private Text SessionName;

    private void Start() {
#if UNITY_EDITOR || UNITY_STANDALONE


        if (ServerStartGUI == null) return;
        ServerGuiSintance = Instantiate(ServerStartGUI).transform;

        SessionName = ServerGuiSintance.Find("PairName")?.GetComponent<InputField>()?.textComponent;
        ServerGuiSintance.Find("IpAddress").GetComponent<Text>().text = LocalIPAddress();

        ServerGuiSintance.Find("StartAsServer")?.GetComponent<Button>().onClick
            .AddListener(StartAsServerInterfaceCallback);
        ServerGuiSintance.Find("StartClientA")?.GetComponent<Button>().onClick
            .AddListener(StartAsClientAInterfaceCallback);
        ServerGuiSintance.Find("StartClientB")?.GetComponent<Button>().onClick
            .AddListener(StartAsClientBInterfaceCallback);

        ServerGuiSintance.Find("StartAsReRun")?.GetComponent<Button>().onClick
            .AddListener(StartAsReRuInterfaceCallback);
        
        ServerGuiSintance.Find("StartAsHost")?.GetComponent<Button>().onClick
            .AddListener(StartAsHostCallback);
#endif
    }

    private void StartAsHostCallback() {
        var tmp = SessionName.text;
        if (tmp.Length <= 1) tmp = "Host_Unnamed_" + DateTime.Now.ToString("yyyyMMddTHHmmss");

        ConnectionAndSpawning.Singleton.StartAsHost(tmp);
        Destroy(ServerGuiSintance.gameObject);
        enabled = false;
    }



    public void StartAsReRuInterfaceCallback() {
        Debug.Log("Starting as Rerun Button Call!");
        ConnectionAndSpawning.Singleton.StartReRun();
        Destroy(ServerGuiSintance.gameObject);
        enabled = false;
    }

    public void StartAsServerInterfaceCallback() {
        var tmp = SessionName.text;
        if (tmp.Length <= 1) tmp = "Unnamed_" + DateTime.Now.ToString("yyyyMMddTHHmmss");

        ConnectionAndSpawning.Singleton.StartAsServer(tmp);
        Destroy(ServerGuiSintance.gameObject);
        enabled = false;
    }

    public void StartAsClientAInterfaceCallback() {
        ConnectionAndSpawning.Singleton.StartAsClient("English", 
            po: ParticipantOrder.A,
            ip: "127.0.0.1",
            port: 7777,
            result:ResponseDelegate,
            _spawnTypeIN: SpawnType.CAR,
            _joinTypeIN: JoinType.VR
        );
        Destroy(ServerGuiSintance.gameObject);
        enabled = false;
    }

    public void StartAsClientBInterfaceCallback() {
        ConnectionAndSpawning.Singleton.StartAsClient("English", 
            ParticipantOrder.B,
            "127.0.0.1", 
            7777,
            ResponseDelegate,SpawnType.PEDESTRIAN);
        Destroy(ServerGuiSintance.gameObject);
        enabled = false;
    }

    private void ResponseDelegate(ConnectionAndSpawning.ClienConnectionResponse response) {
        switch (response) {
            case ConnectionAndSpawning.ClienConnectionResponse.FAILED:
                Debug.Log("Connection Failed maybe change IP address, participant order (A,b,C, etc.) or the port");
                Start();
                break;
            case ConnectionAndSpawning.ClienConnectionResponse.SUCCESS:
                Debug.Log("We are connected you can stop showing the UI now!");
                enabled = false;
                break;
        }
    }

    public static string LocalIPAddress() {
        IPHostEntry host;
        var localIP = "0.0.0.0";
        try
        {
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork) {
                    localIP = ip.ToString();
                    break;
                }
        }
        catch (Exception e)
        {
          Debug.Log($"Could not get ip Address have alook at the error here{e}");
        }
       

        return localIP;
    }
}