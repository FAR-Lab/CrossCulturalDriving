using System;
using  Unity.Netcode;

using UnityEngine;
using System.Collections.Generic;


public class DeactivateOthers :NetworkBehaviour  {
   // NetworkBehaviour
    public List<Behaviour> DeactivateMe = new List<Behaviour>();
    public List<Transform> AndMe = new List<Transform>();
    Camera MyCam;
    public List<MeshRenderer> DeactivateLocally= new List<MeshRenderer>();
	// Use this for initialization
    public override void OnNetworkSpawn ()
    {
        if (!IsServer)
        {
	        
            GetComponent<VehicleController>().enabled = false;
            /// Destroy (GetComponent<Rigidbody>()); //Yaaaay we have rigid body support now :-D
            foreach (WheelCollider wc in GetComponentsInChildren<WheelCollider>())

            {
	            wc.enabled = false;
            }
        }
	}

	// Update is called once per frame
	void Update () {
     
    }
}
