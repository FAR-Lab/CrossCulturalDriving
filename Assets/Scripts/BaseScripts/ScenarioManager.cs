using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OVR.OpenVR;
using UnityEngine.Networking;
using UnityEngine;

public class ScenarioManager : MonoBehaviour {
    public TextAsset[] QuestionairsToAsk;
    public string conditionName;
    // Use this for initialization
    public GameObject QuestionairPrefab;
   
    bool WaitAFrame = false;
    QNSelectionManager qnmanager;
    public bool ready { get; private set; }  // property
    private Dictionary<ParticipantOrder, Pose> MySpawnPositions;

    private Transform MyLocalClient;
    private LanguageSelect lang;
    void Start() {

      //  if (Camera.main != null)
        //    Camera.main.clearFlags = CameraClearFlags.Skybox;
       
        //SceneStateManager.Instance.SetPreDriving();//We go to predrive since this is a driving scene... THis will cause the server to load the cars
        WaitAFrame = false;
        qnmanager = null;

        GetSpawnPoints();
        
        ready = true;
        lang = FindObjectOfType<LocalVRPlayer>().lang;

    }

    
    void Update() {
        
        if (WaitAFrame) {
            WaitAFrame = false;
            if (qnmanager != null)
            {
                if (QuestionairsToAsk.Length > 0)
                {
                    string[] tempArray = new string[QuestionairsToAsk.Length];
                   
                    qnmanager.startAskingTheQuestionairs(MyLocalClient,QuestionairsToAsk, conditionName,lang);
                }
            }

        }
    }

    public Pose? GetStartPose(ParticipantOrder val)
    {
        
            GetSpawnPoints();
            
            if (MySpawnPositions!=null && MySpawnPositions.Count>0 && MySpawnPositions.ContainsKey(val))
        {
            return MySpawnPositions[val];
        }
        else
        {
            Debug.Log("Did not find an assigned spawn point");
            return null;
        }

    }

    private void GetSpawnPoints()
    {
        if (MySpawnPositions == null || MySpawnPositions.Count == 0)
        {
            MySpawnPositions = new Dictionary<ParticipantOrder, Pose>();
            foreach (SpawnPosition sp in GetComponentsInChildren<SpawnPosition>())
            {
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
            WaitAFrame = true;         
        
    }
}
