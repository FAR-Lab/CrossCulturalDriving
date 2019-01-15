using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitSceneManager : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        switch (SceneStateManager.Instance.ActionState) {
            case ActionState.DRIVE:
            
            case ActionState.QUESTIONS:

                foreach (Transform t in transform.GetComponentsInChildren<Transform>()) {
                    if (t == transform) {
                        continue;
                    }
                    t.gameObject.SetActive(false);
                    Debug.Log("Turning off things");
                }
                break;
            case ActionState.PREDRIVE:
            case ActionState.WAITING:
            case ActionState.LOADING:
                foreach (Transform t in transform.GetComponentsInChildren<Transform>()) {
                    t.gameObject.SetActive(true);
                    if (t == transform) {
                        continue;
                    }
                    Debug.Log("Turning on things");
                }
                break;

        }



    }
}
