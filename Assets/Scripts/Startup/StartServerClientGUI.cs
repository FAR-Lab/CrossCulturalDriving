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
    
    
    void OnGUI () {
        if (ServerStarted || ClientStarted)
            return;
        GUI.Box(new Rect(10, 10, 120, 80), "Controlls");
        if (GUI.Button(new Rect(20, 30, 80, 20), "start server")) {
            Debug.Log("Server Started.");
            ServerStarted = true;
           
            ConnectionAndSpawing.Singleton.StartAsServer();
            this.enabled = false;
        }

        if (GUI.Button(new Rect(20, 60, 80, 20), "start client")) {
            Debug.Log("Client Started.");
            ClientStarted = true;
            
            ConnectionAndSpawing.Singleton.StartAsClient("English",ParticipantOrder.A,"192.168.1.160",7777,ResponseDelegate);
            this.enabled = false;
        }
    }

    private void ResponseDelegate(ConnectionAndSpawing.ClienConnectionResponse response) {
        switch (response) {
            case ConnectionAndSpawing.ClienConnectionResponse.FAILED: Debug.Log("Connection Failed maybe change IP address, participant order (A,b,C, etc.) or the port");
                break;
            case ConnectionAndSpawing.ClienConnectionResponse.SUCCESS: Debug.Log("We are connected you can stop showing the UI now!");
                break;
        }
    }

}