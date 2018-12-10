using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class inputtest : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        Debug.Log("Acc: " + Input.GetAxisRaw("Accel") + "\tbreak: " + Input.GetAxisRaw("Brake").ToString());

    }
}
