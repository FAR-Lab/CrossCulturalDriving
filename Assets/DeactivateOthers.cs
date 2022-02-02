using System;
using  Unity.Netcode;

using UnityEngine;
using System.Collections.Generic;


public class DeactivateOthers :NetworkBehaviour  {
  
    public override void OnNetworkSpawn ()
    {
        if (!IsServer)
        {
            GetComponent<VehicleController>().enabled = false;
            
            GetComponent<ForceFeedback>().enabled = false;
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
