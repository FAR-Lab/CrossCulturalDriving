using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OVR.OpenVR;
using Unity.Netcode;
using UnityEditor;
using UnityEngine.Networking;
using UnityEngine;

public class ScenarioManager : MonoBehaviour {
    public TextAsset[] QuestionairsToAsk;

    public string conditionName;

    // Use this for initialization
    public GameObject QuestionairPrefab;

   
    QNSelectionManager qnmanager;
    public bool ready { get; private set; } // property
    private Dictionary<ParticipantOrder, Pose> MySpawnPositions;



    private Transform MyLocalClient;
   

    public GpsController.Direction StartingDirectionParticipantA;
    public GpsController.Direction StartingDirectionParticipantB;
    public GpsController.Direction StartingDirectionParticipantC;
    public GpsController.Direction StartingDirectionParticipantD;
    public GpsController.Direction StartingDirectionParticipantE;
    public GpsController.Direction StartingDirectionParticipantF;


    public List<RerunCameraIdentifier.CameraSetup> CameraSetups;
    
    
    void Start() {
        qnmanager  = Instantiate(
            QuestionairPrefab).GetComponent<QNSelectionManager>();
        qnmanager.gameObject.SetActive(false);
        GetSpawnPoints();
        ready = true;
        
        if (ConnectionAndSpawing.Singleton.ServerisRunning ||
            ConnectionAndSpawing.Singleton.ServerState == ActionState.RERUN)
        {
            foreach(RerunCameraIdentifier c in FindObjectsOfType<RerunCameraIdentifier>())
            { 
                int arraypos = (int) c.myNumber - 1;
                if (arraypos > 0 && arraypos < CameraSetups.Count)
                {
                    if (CameraSetups[arraypos].CameraMode == RerunCameraIdentifier.CameraSetup.CameraState.Fixed)
                    {
                        
                    }
                    else if (CameraSetups[arraypos].CameraMode == RerunCameraIdentifier.CameraSetup.CameraState.Followone)
                    {
                       // c.SetFollowMode(CameraSetups[arraypos].);
                        
                    }
                }
            }
        }

    }


    
    void Update() {
     
    }

    public Pose? GetStartPose(ParticipantOrder val) {
        GetSpawnPoints();

        if (MySpawnPositions != null && MySpawnPositions.Count > 0 && MySpawnPositions.ContainsKey(val)) {
            return MySpawnPositions[val];
        }
        else {
            Debug.Log("Did not find an assigned spawn point");
            return null;
        }
    }
    

    private void GetSpawnPoints() {
        if (MySpawnPositions == null || MySpawnPositions.Count == 0) {
            MySpawnPositions = new Dictionary<ParticipantOrder, Pose>();
            foreach (SpawnPosition sp in GetComponentsInChildren<SpawnPosition>()) {
                var transform1 = sp.transform;
                MySpawnPositions[sp.StartingId] = new Pose(transform1.position, transform1.rotation);
            }
        }
    }
    

    public void RunQuestionairNow(Transform MyLocalClient_) {
        MyLocalClient = MyLocalClient_;

        Transform MyCar = MyLocalClient.GetComponent<ParticipantInputCapture>().GetMyCar();
       
        qnmanager.gameObject.SetActive(true);
        qnmanager.setRelativePosition(MyCar, 1.5f, 4.5f);
        if (QuestionairsToAsk.Length > 0) {
            qnmanager.startAskingTheQuestionairs(MyLocalClient, QuestionairsToAsk, conditionName, ConnectionAndSpawing.Singleton.lang);
        }
    }

    public Dictionary<ParticipantOrder, GpsController.Direction> GetStartingPositions() {
        return new Dictionary<ParticipantOrder, GpsController.Direction>() {
            {ParticipantOrder.A, StartingDirectionParticipantA},
            {ParticipantOrder.B, StartingDirectionParticipantB},
            {ParticipantOrder.C, StartingDirectionParticipantC},
            {ParticipantOrder.D, StartingDirectionParticipantD},
            {ParticipantOrder.E, StartingDirectionParticipantE},
            {ParticipantOrder.F, StartingDirectionParticipantF}
        };
    }
}

