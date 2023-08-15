using System;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;


public class StartServerClientGUI : MonoBehaviour
{
    public GameObject ServerStartGUI;
    private Transform ServerGuiSintance;


    private Text SessionName;

    private void Start(){
#if UNITY_EDITOR || UNITY_STANDALONE

      
        if (ServerStartGUI == null) return;
        ServerGuiSintance = GameObject.Instantiate(ServerStartGUI).transform;

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
#endif
    }


    public void StartAsReRuInterfaceCallback(){
        Debug.Log("Starting as Rerun Button Call!");
        ConnectionAndSpawning.Singleton.StartReRun();
        Destroy(ServerGuiSintance.gameObject);
        this.enabled = false;
    }

    public void StartAsServerInterfaceCallback(){
        string tmp = SessionName.text;
        if (tmp.Length <= 1){
            tmp = "Unnamed_" + DateTime.Now.ToString("yyyyMMddTHHmmss");
        }

        ConnectionAndSpawning.Singleton.StartAsServer(tmp);
        Destroy(ServerGuiSintance.gameObject);
        this.enabled = false;
    }

    public void StartAsClientAInterfaceCallback(){
        ConnectionAndSpawning.Singleton.StartAsClient("English", ParticipantOrder.A, "127.0.0.1", 7777,
            ResponseDelegate);
        Destroy(ServerGuiSintance.gameObject);
        this.enabled = false;
    }

    public void StartAsClientBInterfaceCallback(){
        ConnectionAndSpawning.Singleton.StartAsClient("English", ParticipantOrder.B, "127.0.0.1", 7777,
            ResponseDelegate);
        Destroy(ServerGuiSintance.gameObject);
        this.enabled = false;
    }

    void OnGUI(){
        /*
        if (ServerStarted || ClientStarted)
            return;
        if (CONNECTING){
            GUI.Label(new Rect(10, 10, 200, 80), "CONNECTING", Style);
            return;
        }

        GUI.Box(new Rect(10, 10, 400, 240), "");

        GUI.Label(new Rect(20, 20, 80, 20), "pairname:", Style);
        pairName = GUI.TextField(new Rect(240, 20, 80, 20), pairName, Style);

        if (GUI.Button(new Rect(20, 50, 80, 20), "start server", Style)){
            ServerStarted = true;
            ConnectionAndSpawing.Singleton.StartAsServer(pairName);
            this.enabled = false;
        }

        if (GUI.Button(new Rect(240, 50, 80, 20), "RERUN", Style)){
            Debug.Log("Server Started.");

            ConnectionAndSpawing.Singleton.StartReRun();
            this.enabled = false;
        }

        if (GUI.Button(new Rect(20, 100, 80, 20), "Client A", Style)){
            Debug.Log("Client Started.");
            CONNECTING = true;
            ConnectionAndSpawing.Singleton.StartAsClient("English", ParticipantOrder.A, "127.0.0.1", 7777,
                ResponseDelegate);
        }

        if (GUI.Button(new Rect(240, 100, 80, 20), "client B", Style)){
            Debug.Log("Client Started.");
            CONNECTING = true;
            ConnectionAndSpawing.Singleton.StartAsClient("English", ParticipantOrder.B, "127.0.0.1", 7777,
                ResponseDelegate);
        }

        if (IpString.Length <= 1){
            IpString += LocalIPAddress();
        }

        GUI.Label(new Rect(20, 150, 200, 80), "IP:" + IpString, Style);
        */
    }

    private void ResponseDelegate(ConnectionAndSpawning.ClienConnectionResponse response){
        switch (response){
            case ConnectionAndSpawning.ClienConnectionResponse.FAILED:
                Debug.Log("Connection Failed maybe change IP address, participant order (A,b,C, etc.) or the port");
                Start();
                break;
            case ConnectionAndSpawning.ClienConnectionResponse.SUCCESS:
                Debug.Log("We are connected you can stop showing the UI now!");
                this.enabled = false;
                break;
        }
    }

    public static string LocalIPAddress(){
        IPHostEntry host;
        string localIP = "0.0.0.0";
        host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList){
            if (ip.AddressFamily == AddressFamily.InterNetwork){
                localIP = ip.ToString();
                break;
            }
        }

        return localIP;
    }
}