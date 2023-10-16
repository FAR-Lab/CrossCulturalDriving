using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerPlayerTracker : MonoBehaviour {
    private bool test;
 
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

 
    private void OnTriggerStay(Collider other) {
        if (other.gameObject.CompareTag("Player")) {
            test = true;
           
        }
    }

   

    public bool GetPlayerPresent() {
        return test;
    }
    private void LateUpdate() {
        test = false;
    }
}
