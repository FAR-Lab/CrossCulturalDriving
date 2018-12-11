using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GrabCamera : NetworkBehaviour {
    Transform main;
    // Use this for initializatio
    Transform CameraPos;
    bool madeParent = false;
	void Start () {
        main = Camera.main.transform.parent;
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
            main.transform.parent = CameraPos;
            madeParent = true;
            seatCallibration s =main.transform.GetComponent<seatCallibration>();
            if (s != null)
            {
                s.findHands();
                s.reCallibrate();
            }
        }
    }
}
