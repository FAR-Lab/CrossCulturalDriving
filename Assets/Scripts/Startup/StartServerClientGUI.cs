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
        ConnectionAndSpawing.Singleton.StartReRun();
        Destroy(ServerGuiSintance.gameObject);
        this.enabled = false;
    }

    public void StartAsServerInterfaceCallback(){
        string tmp = SessionName.text;
        if (tmp.Length <= 1){
            tmp = "Unnamed_" + DateTime.Now.ToString("yyyyMMddTHHmmss");
        }

        ConnectionAndSpawing.Singleton.StartAsServer(tmp);
        Destroy(ServerGuiSintance.gameObject);
        this.enabled = false;
    }

    public void StartAsClientAInterfaceCallback(){
        ConnectionAndSpawing.Singleton.StartAsClient("English", ParticipantOrder.A, "127.0.0.1", 7777,
            ResponseDelegate);
        Destroy(ServerGuiSintance.gameObject);
        this.enabled = false;
    }

    public void StartAsClientBInterfaceCallback(){
        ConnectionAndSpawing.Singleton.StartAsClient("English", ParticipantOrder.B, "127.0.0.1", 7777,
            ResponseDelegate);
        Destroy(ServerGuiSintance.gameObject);
        this.enabled = false;
    }

    
    private void ResponseDelegate(ConnectionAndSpawing.ClienConnectionResponse response){
        switch (response){
            case ConnectionAndSpawing.ClienConnectionResponse.FAILED:
                Debug.Log("Connection Failed maybe change IP address, participant order (A,b,C, etc.) or the port");
                Start();
                break;
            case ConnectionAndSpawing.ClienConnectionResponse.SUCCESS:
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