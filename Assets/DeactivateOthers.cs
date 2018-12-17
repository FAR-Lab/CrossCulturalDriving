using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DeactivateOthers : NetworkBehaviour {
    public List<Behaviour> DeactivateMe = new List<Behaviour>();
    Camera MyCam;
	// Use this for initialization
	void Start () {

        if (!isLocalPlayer) {
            foreach (Behaviour b in DeactivateMe)
            {
                b.enabled = false;
                //MyCam.enabled = false;
            }
        }
// rh = FindObjectOfType<RemoteHandManager>();

       //     if (rh!=null)
         //// rh.FindLocalLeap();
	}

	// Update is called once per frame
	void Update () {
      //  if (SceneStateManager.Instance.MyState == ClientState.NONE || SceneStateManager.Instance.MyState == ClientState.DISCONECTED)
     

    }
}
