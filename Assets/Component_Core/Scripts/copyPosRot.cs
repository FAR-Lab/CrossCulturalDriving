using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class copyPosRot : MonoBehaviour {
    public Transform followObject;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        transform.position = followObject.position;
        transform.rotation = followObject.rotation;
	}
}
