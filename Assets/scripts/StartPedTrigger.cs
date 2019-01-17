using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartPedTrigger : MonoBehaviour {
    bool triggered=false;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnTriggerEnter(Collider other) {
        if (!triggered) {
            foreach (VehicleInputControllerNetworked v in FindObjectsOfType<VehicleInputControllerNetworked>()) {
                if (v.isLocalPlayer) {
                    v.CmdStartWalking();
                    triggered = true;
                    break;
                }
            }
        }
    }
}
