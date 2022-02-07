/* Flashing button example */
using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEngine.SceneManagement;

public class StartServerClientGUI : MonoBehaviour
{
    public Texture2D onIcon;
    public Texture2D offIcon;
    private bool ServerStarted = false;
    private bool ClientStarted = false;
    private bool CONNECTING = false;
    
    private string pairName="test";
    void OnGUI () {
        if (ServerStarted || ClientStarted)
            return;
        if (CONNECTING) {
            GUI.Label(new Rect(10, 10, 200, 80), "CONNECTING");
            return;
        }
        
        GUI.Box(new Rect(10, 10, 200, 120), "");

        GUI.Label(new Rect(20, 20, 80, 20),"pairname:");
        pairName = GUI.TextField(new Rect(120, 20, 80, 20), pairName);
        
        if (GUI.Button(new Rect(20, 50, 80, 20), "start server")) {
        
            ServerStarted = true;
            ConnectionAndSpawing.Singleton.StartAsServer(pairName);
            this.enabled = false;
        }
        
        if (GUI.Button(new Rect(120, 50, 80, 20), "RERUN")) {
            Debug.Log("Server Started.");

            ConnectionAndSpawing.Singleton.StartReRun();
            this.enabled = false;
        }

        if (GUI.Button(new Rect(20, 100, 80, 20), "Client A")) {
            Debug.Log("Client Started.");
            CONNECTING = true;
            ConnectionAndSpawing.Singleton.StartAsClient("English",ParticipantOrder.A,"192.168.1.163",7777,ResponseDelegate);
          
        }
        if (GUI.Button(new Rect(120, 100, 80, 20), "client B")) {
            Debug.Log("Client Started.");
            CONNECTING = true;
            ConnectionAndSpawing.Singleton.StartAsClient("English",ParticipantOrder.B,"192.168.1.163",7777,ResponseDelegate);
          
        }
    }

    private void ResponseDelegate(ConnectionAndSpawing.ClienConnectionResponse response) {
        switch (response) {
            case ConnectionAndSpawing.ClienConnectionResponse.FAILED: 
                Debug.Log("Connection Failed maybe change IP address, participant order (A,b,C, etc.) or the port");
                CONNECTING = false;
                break;
            case ConnectionAndSpawing.ClienConnectionResponse.SUCCESS: 
                Debug.Log("We are connected you can stop showing the UI now!");
                ClientStarted = true;
                CONNECTING = false;
                this.enabled = false;
                break;
        }
    }

}