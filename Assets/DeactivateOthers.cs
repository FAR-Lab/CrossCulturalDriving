using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using UnityEngine;
using System.Collections.Generic;


public class DeactivateOthers :NetworkBehaviour  {
   // NetworkBehaviour
    public List<Behaviour> DeactivateMe = new List<Behaviour>();
    public List<Transform> AndMe = new List<Transform>();
    Camera MyCam;
    public List<MeshRenderer> DeactivateLocally= new List<MeshRenderer>();
	// Use this for initialization
	void Awake () {

        if (IsLocalPlayer) {
            foreach (Behaviour b in DeactivateMe) {
                b.enabled = false;
                
            }
            foreach (Transform t in AndMe) {
                t.gameObject.SetActive(false);
            }
        } else {

            foreach (MeshRenderer b in DeactivateLocally) {
                b.enabled = false;
                
            }
        }
// rh = FindObjectOfType<RemoteHandManager>();

       //     if (rh!=null)
         //// rh.FindLocalLeap();
	}

	// Update is called once per frame
	void Update () {
     
    }
}
