using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GrabCamera : NetworkBehaviour {
    Camera main;
	// Use this for initialization
	void Start () {
        main = Camera.main;
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
    private void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            main.transform.position = transform.position + transform.forward * (-2) + transform.up;
            main.transform.rotation = transform.rotation;
        }
    }
}
