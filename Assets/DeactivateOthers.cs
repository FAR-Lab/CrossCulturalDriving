using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DeactivateOthers :MonoBehaviour  {
   // NetworkBehaviour
    public List<Behaviour> DeactivateMe = new List<Behaviour>();
    public List<Transform> AndMe = new List<Transform>();
    Camera MyCam;
    public List<MeshRenderer> DeactivateLocally= new List<MeshRenderer>();
	// Use this for initialization
	void Start () {

        if (true) {
            //!isLocalPlayer
            foreach (Behaviour b in DeactivateMe) {
                b.enabled = false;
                //MyCam.enabled = false;
            }
            foreach (Transform t in AndMe) {
                t.gameObject.SetActive(false);
            }
        } else {

            foreach (MeshRenderer b in DeactivateLocally) {
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
