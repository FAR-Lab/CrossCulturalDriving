using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecificSceneManager : MonoBehaviour {
    public string[] questionairsToAsk;
    public string conditionName;
    // Use this for initialization
    public GameObject QuestionairPrefab;
    float lerpAdaption=1;
	void Start () {
        if(Camera.main!=null)Camera.main.clearFlags = CameraClearFlags.Skybox;
	}
	
	// Update is called once per frame
	void Update () {

        if (lerpAdaption < 1) {
            lerpAdaption += Time.deltaTime * SceneStateManager.slowDownSpeed;
            Debug.Log("slowing DownTime at " + lerpAdaption);
            Time.timeScale = Mathf.Lerp(1, SceneStateManager.slowTargetTime, lerpAdaption);
        } else if (lerpAdaption >= 1 && lerpAdaption < 2) {
            lerpAdaption = 2;
            foreach (VehicleInputControllerNetworked vn in FindObjectsOfType<VehicleInputControllerNetworked>()) {
                if (vn.isLocalPlayer) {
                    QNSelectionManager temp = Instantiate(
                        QuestionairPrefab,
                        vn.transform.position +vn.transform.up*1.5f+vn.transform.forward*2.5f,
                        vn.transform.rotation).GetComponent<QNSelectionManager>();
                    temp.startAskingTheQuestionairs(questionairsToAsk, conditionName);
                    temp.setRelativePosition(vn.transform,  1.5f , 2.5f);

                }
            }
            

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
