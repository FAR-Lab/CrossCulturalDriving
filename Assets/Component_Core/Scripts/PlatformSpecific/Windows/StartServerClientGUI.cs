using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class StartServerClientGUI : MonoBehaviour {
    public GameObject ServerStartGUI;
    private Transform ServerGuiInstance;

    private Text SessionName;
    private Text TargetIP;
    private Text MulticastIP;
    public SO_IPAddress MulticastIPSaver;

    private void Start() {
#if UNITY_EDITOR || UNITY_STANDALONE


        if (ServerStartGUI == null) return;
        ServerGuiInstance = Instantiate(ServerStartGUI).transform;

        SessionName = ServerGuiInstance.Find("HostPanel/PairName")?.GetComponent<InputField>().textComponent;
        ServerGuiInstance.Find("HostPanel/IpAddress").GetComponent<Text>().text = LocalIPAddress();

        TargetIP = ServerGuiInstance.Find("ClientPanel/ClientOptions/TargetIPAddress").GetComponent<InputField>()
            ?.textComponent;
        MulticastIP = ServerGuiInstance.Find("HostPanel/HostOptions/MulticastAddress").GetComponent<InputField>()
            ?.textComponent;
        
        ServerGuiInstance.Find("StartAsReRun").GetComponent<Button>().onClick
            .AddListener(() => StartAsReRuInterfaceCallback());


        foreach (var t in ServerGuiInstance.GetComponentsInChildren<ComputerStartButtonConfiguration>())
            t.GetComponent<Button>().onClick.AddListener(
                () => StartUsingParameters(t.ThisStartType,
                    t.ThisParticipantOrder,
                    TargetIP.text,
                    t.ThisSpawnType,
                    t.ThisJoinType));

#endif
    }

    public void StartUsingParameters(ComputerStartButtonConfiguration.StartType starttype, ParticipantOrder _po,
        string _ip,
        SpawnType _spawnTypeIN,
        JoinType _joinTypeIN) {
        switch (starttype) {
            case ComputerStartButtonConfiguration.StartType.Server:
                MulticastIPSaver.ipAddress = MulticastIP.text;
                ConnectionAndSpawning.Singleton.StartAsServer(SessionName.text);
                break;
            case ComputerStartButtonConfiguration.StartType.Host:
                MulticastIPSaver.ipAddress = MulticastIP.text;
                ConnectionAndSpawning.Singleton.StartAsHost(SessionName.text, _po, _joinTypeIN, _spawnTypeIN);
                break;
            case ComputerStartButtonConfiguration.StartType.Client:
                if (_ip.Length == 0) _ip = "127.0.0.1";
                ConnectionAndSpawning.Singleton.StartAsClient("English",
                    _po,
                    _ip,
                    7777,
                    ResponseDelegate,
                    _spawnTypeIN,
                    _joinTypeIN
                );
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(starttype), starttype, null);
        }


        Destroy(ServerGuiInstance.gameObject);
        enabled = false;
    }


    public void StartAsReRuInterfaceCallback() {
        Debug.Log("Starting as Rerun Button Call!");
        ConnectionAndSpawning.Singleton.StartAsRerun();
        Destroy(ServerGuiInstance.gameObject);
        enabled = false;
    }

    public void StartAsServerInterfaceCallback() {
        var tmp = SessionName.text;
        if (tmp.Length <= 1) tmp = "Unnamed_" + DateTime.Now.ToString("yyyyMMddTHHmmss");

        ConnectionAndSpawning.Singleton.StartAsServer(tmp);
        Destroy(ServerGuiInstance.gameObject);
        enabled = false;
    }


    private void ResponseDelegate(ConnectionAndSpawning.ClientConnectionResponse response) {
        switch (response) {
            case ConnectionAndSpawning.ClientConnectionResponse.FAILED:
                Debug.Log("Connection Failed maybe change IP address, participant order (A,b,C, etc.) or the port");
                Start();
                break;
            case ConnectionAndSpawning.ClientConnectionResponse.SUCCESS:
                Debug.Log("We are connected you can stop showing the UI now!");
                enabled = false;
                break;
        }
    }

    public static string LocalIPAddress() {
        IPHostEntry host;
        var localIP = "0.0.0.0";
        try {
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork) {
                    localIP = ip.ToString();
                    break;
                }
        }
        catch (Exception e) {
            Debug.Log($"Could not get ip Address have alook at the error here{e}");
        }


        return localIP;
    }

    # region Singleton

    public static StartServerClientGUI Singleton;

    private void Awake() {
        if (Singleton == null)
            Singleton = this;
        else
            Destroy(this);
    }

    # endregion
}