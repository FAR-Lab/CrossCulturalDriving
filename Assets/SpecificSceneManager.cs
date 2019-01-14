using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;

public class SpecificSceneManager : MonoBehaviour {
    public string[] questionairsToAsk;
    public string conditionName;
    // Use this for initialization
    public GameObject QuestionairPrefab;
    float lerpAdaption = 1;
    bool WaitAFrame = false;
    QNSelectionManager qnmanager;

    public waypoint StartpointLaneA;
    public waypoint StartpointLaneB;
    public int totalActiveVehiclesLaneA = 0;
    public int totalActiveVehiclesLaneB = 0;
    List<AIInput> activeCarsLaneA = new List<AIInput>();
    List<AIInput> activeCarsLaneB = new List<AIInput>();

    void Start() {
        if (Camera.main != null)
            Camera.main.clearFlags = CameraClearFlags.Skybox;
    }


    // Update is called once per frame
    void Update() {
        if (WaitAFrame) {
            WaitAFrame = false;
            if (qnmanager != null)
                qnmanager.startAskingTheQuestionairs(questionairsToAsk, conditionName);

        }
        if (lerpAdaption < 1) {
            lerpAdaption += Time.deltaTime * SceneStateManager.slowDownSpeed;
            Debug.Log("slowing DownTime at " + lerpAdaption);
            Time.timeScale = Mathf.Lerp(1, SceneStateManager.slowTargetTime, lerpAdaption);
        } else if (lerpAdaption >= 1 && lerpAdaption < 2) {
            lerpAdaption = 2;
            foreach (VehicleInputControllerNetworked vn in FindObjectsOfType<VehicleInputControllerNetworked>()) {
                if (vn.isLocalPlayer) {
                    qnmanager = Instantiate(
                        QuestionairPrefab,
                        vn.transform.position + vn.transform.up * 1.5f + vn.transform.forward * 2.5f,
                        vn.transform.rotation).GetComponent<QNSelectionManager>();

                    qnmanager.setRelativePosition(vn.transform, 1.75f, 3.5f);
                    WaitAFrame = true;
                }
            }
        }
        if (SceneStateManager.Instance != null) {
            if (SceneStateManager.Instance.MyState == ClientState.HOST && SceneStateManager.Instance.ActionState == ActionState.DRIVE) {
                if (SceneStateManager.Instance.spawnPrefabs[0].GetComponent<AIInput>() != null) {
                    if (activeCarsLaneA.Count < totalActiveVehiclesLaneA) {
                        if (activeCarsLaneA.Count == 0 || activeCarsLaneA.Count > 0 && (activeCarsLaneA[activeCarsLaneA.Count - 1].transform.position - StartpointLaneA.transform.position ).magnitude > 5) {
                            AIInput newCar = Instantiate(SceneStateManager.Instance.spawnPrefabs[0], StartpointLaneA.transform.position, StartpointLaneA.transform.rotation).GetComponent<AIInput>();
                            newCar.transform.GetComponent<waypointMovementManagerV2>().startWaypoint = StartpointLaneA;
                            NetworkServer.Spawn(newCar.gameObject);
                            activeCarsLaneA.Add(newCar);
                        }
                    }
                    if (activeCarsLaneB.Count < totalActiveVehiclesLaneB) {
                        if (activeCarsLaneB.Count == 0 || activeCarsLaneB.Count > 0 && ( activeCarsLaneB[activeCarsLaneB.Count - 1].transform.position - StartpointLaneB.transform.position ).magnitude > 5) {

                            AIInput newCar = Instantiate(SceneStateManager.Instance.spawnPrefabs[0], StartpointLaneB.transform.position, StartpointLaneB.transform.rotation).GetComponent<AIInput>();
                            newCar.transform.GetComponent<waypointMovementManagerV2>().startWaypoint = StartpointLaneB;
                            NetworkServer.Spawn(newCar.gameObject);
                            activeCarsLaneB.Add(newCar);
                        }
                    }
                }

            }
        }

    }
    public void runQuestionairNow() {
        SceneStateManager.Instance.SetQuestionair();
        lerpAdaption = 0;
    }

    private void OnTriggerEnter(Collider other) {

        Debug.Log("OnCollisionEnter" + other.transform.name);
        if (other.transform.parent.GetComponent<VehicleInputControllerNetworked>() != null  //if its a car 
            && other.transform.parent.GetComponent<VehicleInputControllerNetworked>().isLocalPlayer // and Its us
            && SceneStateManager.Instance.ActionState == ActionState.DRIVE) { // and we are driving
            Debug.Log("Found the local Player that was driving slowing down time,loading uquestionair");
            other.transform.parent.GetComponent<VehicleInputControllerNetworked>().CmdStartQuestionairGloablly();
        }
    }

}
