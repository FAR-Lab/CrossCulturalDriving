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
    void OnGUI () 
    {
        GUI.Box(new Rect(10, 10, 120, 80), "Controlls");
        if (GUI.Button(new Rect(20, 30, 80, 20), "start server")) {
            Debug.Log("Server Started.");
            ServerStarted = true;
        }

        if (GUI.Button(new Rect(20, 60, 80, 20), "start client")) {
            Debug.Log("Client Started.");
            ClientStarted = true;
        }

        if (ServerStarted) {
            GUI.Box(new Rect(105, 30, 20, 20), onIcon);
        }
        
        if (ClientStarted) {
            GUI.Box(new Rect(105, 60, 20, 20), onIcon);
        }
    }

}