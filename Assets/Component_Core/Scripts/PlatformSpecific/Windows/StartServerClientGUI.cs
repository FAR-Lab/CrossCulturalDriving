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
    private Text TargetIP;
    private void Start() {
#if UNITY_EDITOR || UNITY_STANDALONE


        if (ServerStartGUI == null) return;
        ServerGuiSintance = Instantiate(ServerStartGUI).transform;

        SessionName = ServerGuiSintance.Find("HostPanel/PairName")?.GetComponent<InputField>().textComponent;
        ServerGuiSintance.Find("HostPanel/IpAddress").GetComponent<Text>().text = LocalIPAddress();
        
        TargetIP = ServerGuiSintance.Find("ClientPanel/ClientOptions/TargetIPAddress").GetComponent<InputField>()?.textComponent;
        ServerGuiSintance.Find("StartAsReRun").GetComponent<Button>().onClick.AddListener(()=>StartAsReRuInterfaceCallback());
        
        
        foreach (var t in ServerGuiSintance.GetComponentsInChildren<ComputerStartButtonConfiguration>()) {
            t.GetComponent<Button>().onClick.AddListener(
                ()=>StartUsingParameters(t.ThisStartType,
                    t.ThisParticipantOrder,
                    TargetIP.text,
                    t.ThisSpawnType,
                    t.ThisJoinType));
                    
        }
        
#endif
    }

    public void StartUsingParameters(ComputerStartButtonConfiguration.StartType starttype, ParticipantOrder _po,
        string _ip,
        SpawnType _spawnTypeIN, 
        JoinType _joinTypeIN) {
        switch (starttype) {
            case ComputerStartButtonConfiguration.StartType.Server:
                ConnectionAndSpawning.Singleton.StartAsServer(SessionName.text);
                break;
            case ComputerStartButtonConfiguration.StartType.Host:
                ConnectionAndSpawning.Singleton.StartAsHost(SessionName.text, _joinTypeIN);
                break;
            case ComputerStartButtonConfiguration.StartType.Client:
                if (_ip.Length == 0) {
                    _ip = "127.0.0.1";
                }
                ConnectionAndSpawning.Singleton.StartAsClient("English", 
                    po:_po,
                    ip: _ip,
                    port: 7777,
                    result:ResponseDelegate,
                    _spawnTypeIN: _spawnTypeIN,
                    _joinTypeIN: _joinTypeIN
                );
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(starttype), starttype, null);
        }

        
        Destroy(ServerGuiSintance.gameObject);
        enabled = false;
    }
    
   

    public void StartAsReRuInterfaceCallback() {
        Debug.Log("Starting as Rerun Button Call!");
        ConnectionAndSpawning.Singleton.StartAsRerun();
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