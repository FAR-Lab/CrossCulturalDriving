using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;

public class SpecificSceneManager : MonoBehaviour {
    public TextAsset[] QuestionairsToAsk;
    public string conditionName;
    // Use this for initialization
    public GameObject QuestionairPrefab;
    float lerpAdaption = 1;
    bool WaitAFrame = false;
    QNSelectionManager qnmanager;

    void Start() {

        if (Camera.main != null)
            Camera.main.clearFlags = CameraClearFlags.Skybox;
       
        //SceneStateManager.Instance.SetPreDriving();//We go to predrive since this is a driving scene... THis will cause the server to load the cars

        lerpAdaption = 2;
        WaitAFrame = false;
        qnmanager = null;
        Time.timeScale = 1.0f;
    }

    
    void Update() {
        if (WaitAFrame) {
            WaitAFrame = false;
            if (qnmanager != null)
            {
                string[] tempArray=new string[QuestionairsToAsk.Length];
                int i = 0;
                foreach (TextAsset t in QuestionairsToAsk)
                {
                    tempArray[i]= t.name;
                    i++;
                }
                qnmanager.startAskingTheQuestionairs(tempArray, conditionName);
            }

        }
        if (lerpAdaption < 1) {
            lerpAdaption += Time.deltaTime * 10f;
            //Debug.Log("slowing DownTime at " + lerpAdaption);
            Time.timeScale = Mathf.Lerp(1, 0.5f, lerpAdaption);
        } else if (lerpAdaption >= 1 && lerpAdaption < 2) {
            lerpAdaption = 2;
            if (true) {
                foreach (Player_Drive_Entity vn in FindObjectsOfType<Player_Drive_Entity>()) {

                        qnmanager = Instantiate(
                            QuestionairPrefab,
                            vn.transform.position + vn.transform.up * 1.5f + vn.transform.forward * 2.5f,
                            vn.transform.rotation).GetComponent<QNSelectionManager>();

                        qnmanager.setRelativePosition(vn.transform, 1.75f, 4f);
                        
                        WaitAFrame = true;
                    
                }
            }
        }

    }
    public void runQuestionairNow() {
        Debug.Log("Running questionairNow");
        //SceneStateManager.Instance.SetQuestionair();
        lerpAdaption = 0;
    }

    private void OnCollisionEnter(Collision collision) {

        //Debug.Log("OnCollisionEnter" + other.transform.name);
        runQuestionairNow();
        /*if (other.transform.parent.GetComponent<VehicleInputControllerNetworked>() != null  //if its a car 
            && other.transform.parent.GetComponent<VehicleInputControllerNetworked>().isLocalPlayer // and Its us
            && SceneStateManager.Instance.ActionState == ActionState.DRIVE) { // and we are driving
            Debug.Log("Found the local Player that was driving slowing down time,loading uquestionair");
            other.transform.parent.GetComponent<VehicleInputControllerNetworked>().CmdStartQuestionairGloablly();
        }*/
    }

}
