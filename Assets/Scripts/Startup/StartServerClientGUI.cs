/* Flashing button example */
using UnityEngine;
using System.Collections;
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
            GetComponent<ConnectionAndSpawing>().Setlanguage("English");
            GetComponent<ConnectionAndSpawing>().StartAsServer();
            this.enabled = false;
        }

        if (GUI.Button(new Rect(20, 60, 80, 20), "start client")) {
            Debug.Log("Client Started.");
            ClientStarted = true;
            GetComponent<ConnectionAndSpawing>().SetParticipantOrder(ParticipantOrder.A);
            GetComponent<ConnectionAndSpawing>().Setlanguage("English");
            GetComponent<ConnectionAndSpawing>().StartAsClient();
            this.enabled = false;
        }
    }

}