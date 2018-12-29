using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecificSceneManager : MonoBehaviour {
    public string[] questionairsToAsk;
    // Use this for initialization
    
    float lerpAdaption=1;
	void Start () {
        if(Camera.main!=null)Camera.main.clearFlags = CameraClearFlags.Skybox;
	}
	
	// Update is called once per frame
	void Update () {

        if (lerpAdaption<1) {
            lerpAdaption += Time.deltaTime * SceneStateManager.slowDownSpeed;
            Debug.Log("slowing DownTime at "+lerpAdaption);
            Time.timeScale = Mathf.Lerp(1, SceneStateManager.slowTargetTime, lerpAdaption);
        }


    }
    private void OnTriggerEnter(Collider other) {
        
        Debug.Log("OnCollisionEnter"+ other.transform.name);
        if (other.transform.parent.GetComponent<VehicleInputControllerNetworked>()!=null  //if its a car 
            && other.transform.parent.GetComponent<VehicleInputControllerNetworked>().isLocalPlayer // and Its us
            && SceneStateManager.Instance.ActionState==ActionState.DRIVE) { // and we are driving
            Debug.Log("Found the local Player that was driving slowing down time,loading uquestionair");
            SceneStateManager.Instance.SetQuestionair();
            lerpAdaption = 0;
        }
    }
   
}
