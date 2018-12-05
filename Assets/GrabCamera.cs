using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GrabCamera : NetworkBehaviour {
    Camera main;
    // Use this for initializatio
    Transform CameraPos;
    bool madeParent = false;
	void Start () {
        main = Camera.main;
        CameraPos = transform.Find("CameraPosition");

    }
	
	// Update is called once per frame
	void Update () {
		
	}
    private void FixedUpdate()
    {
        if (isLocalPlayer && !madeParent)
        {
            main.transform.position = CameraPos.position;
            main.transform.rotation = CameraPos.rotation;
            main.transform.parent = transform;
            madeParent = true;
        }
    }
}
