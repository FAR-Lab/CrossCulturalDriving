using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using OVR.OpenVR;
using Unity.Netcode;
using UnityEditor;
using UnityEngine.Networking;
using UnityEngine;

public class ScenarioManager : MonoBehaviour {
    public TextAsset  QuestionairToAsk;

    public string conditionName;

    // Use this for initialization
    public GameObject QuestionairPrefab;

    public SceneField VisualSceneToUse;
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


    public List<CameraSetupXC> CameraSetups;
    
    
    void Start() {
        qnmanager  = Instantiate(
            QuestionairPrefab).GetComponent<QNSelectionManager>();
        qnmanager.gameObject.SetActive(false);
        GetSpawnPoints();
        ready = true;
        
        if (ConnectionAndSpawing.Singleton.ServerisRunning ||
            ConnectionAndSpawing.Singleton.ServerState == ActionState.RERUN)
        {
           
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
        if (QuestionairToAsk !=null) {
            Debug.Log("about to setup QN");
            qnmanager.startAskingTheQuestionairs(MyLocalClient, conditionName, ConnectionAndSpawing.Singleton.lang);
            Debug.Log("Started asking questions!");
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

    public TextAsset GetQuestionFile()
    {
        return QuestionairToAsk;
    }
    
   public List<QuestionnaireQuestion>   ReadString(string asset)
    {
        return JsonConvert.DeserializeObject<List<QuestionnaireQuestion>>(asset);
    }

   public List<QuestionnaireQuestion> GetQuestionObject()
   {
       return ReadString(QuestionairToAsk.text);
   }

   
}

