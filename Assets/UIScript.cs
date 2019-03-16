using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIScript : MonoBehaviour {

    public Transform notRunningUI;
    public Transform hostingUI;
    public Transform connectedUI;

    public bool useHebrewLanguage = true;
    //public Transform serverIPField;
    //public Transform participatID;
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if(SceneStateManager.Instance.MyState==ClientState.NONE || SceneStateManager.Instance.MyState == ClientState.DISCONECTED)
        {
            notRunningUI.gameObject.SetActive(true);
            hostingUI.gameObject.SetActive(false);
            connectedUI.gameObject.SetActive(false);
        }else if(SceneStateManager.Instance.MyState == ClientState.HOST){
            notRunningUI.gameObject.SetActive(false);
            hostingUI.gameObject.SetActive(false);// dirty fix should be true
            connectedUI.gameObject.SetActive(false);
        } else if (SceneStateManager.Instance.MyState == ClientState.CLIENT){
            notRunningUI.gameObject.SetActive(false);
            hostingUI.gameObject.SetActive(false);
            connectedUI.gameObject.SetActive(false);//  dirty fix should be true
        }
    }
    public void toggleVR(Toggle change)
    {
        useHebrewLanguage = change.isOn;
    }

    public void HostTheServer(){
       string id= GameObject.Find("ParticipantIDField").GetComponent<InputField>().text;
        uint partiID = 0;
        if (id.Length>0){
            partiID = uint.Parse(id);
        }
        Debug.Log(partiID);
        SceneStateManager.Instance.HostServer(partiID, useHebrewLanguage);
        //

    }
    public void ConnectToServer()
    {//TODO: Convert this into a int.tryparse and reject the connection if its not an integer.
        string id = GameObject.Find("ParticipantIDField").GetComponent<InputField>().text;
        uint partiID = 0;
        if (id.Length > 0)
        {
            partiID = uint.Parse(id);
        }

        string ip = GameObject.Find("ServerIPField").GetComponent<InputField>().text;
        if (ip.Length == 0)
        {
            ip = "127.0.0.1";
        }


        SceneStateManager.Instance.ConnectToServerWith(ip, partiID, useHebrewLanguage);

    }
    public void StartGame(){

    }
}
