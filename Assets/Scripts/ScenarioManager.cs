using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OVR.OpenVR;
using Unity.Netcode;
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

    void Start() {
        qnmanager = null;
        GetSpawnPoints();
        ready = true;
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
        qnmanager = Instantiate(
            QuestionairPrefab,
            MyLocalClient.position + MyLocalClient.forward * 2.5f + MyLocalClient.up * 1.5f,
            MyLocalClient.transform.rotation).GetComponent<QNSelectionManager>();

        qnmanager.setRelativePosition(MyLocalClient, .75f, 4f);
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