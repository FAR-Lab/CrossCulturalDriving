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
    
    void OnGUI () {
        if (ServerStarted || ClientStarted)
            return;
        if (CONNECTING) {
            GUI.Label(new Rect(10, 10, 120, 80), "CONNECTING");
            return;
        }
        
        GUI.Box(new Rect(10, 10, 120, 80), "Controlls");
        if (GUI.Button(new Rect(20, 30, 80, 20), "start server")) {
            Debug.Log("Server Started.");
            ServerStarted = true;
           
            ConnectionAndSpawing.Singleton.StartAsServer();
            this.enabled = false;
        }

        if (GUI.Button(new Rect(20, 60, 80, 20), "start client")) {
            Debug.Log("Client Started.");

            CONNECTING = true;
            ConnectionAndSpawing.Singleton.StartAsClient("English",ParticipantOrder.A,"192.168.1.160",7777,ResponseDelegate);
            
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