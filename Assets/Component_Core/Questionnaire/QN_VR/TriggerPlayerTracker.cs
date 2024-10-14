using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerPlayerTracker : MonoBehaviour {
    private bool test;
 
   

 
    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.CompareTag("Player")) {
            test = true;
           
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.gameObject.CompareTag("Player")) {
            test = false;
           
        }
    }

   

    public bool GetPlayerPresent() {
        return test;
    }

}
