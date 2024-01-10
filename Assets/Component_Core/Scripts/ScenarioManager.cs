using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;

public class ScenarioManager : MonoBehaviour {
    public TextAsset QuestionairToAsk;
    
    public const string PedestrianSpawnPointLocationPathJson = "PedestrianSpawn.json";


   
    public  string ConditionName {
        get { return gameObject.scene.name;}
    }


    public SceneField VisualSceneToUse;

    public NavigationScreen.Direction StartingDirectionParticipantA;
    public NavigationScreen.Direction StartingDirectionParticipantB;
    public NavigationScreen.Direction StartingDirectionParticipantC;
    public NavigationScreen.Direction StartingDirectionParticipantD;
    public NavigationScreen.Direction StartingDirectionParticipantE;
    public NavigationScreen.Direction StartingDirectionParticipantF;


    public List<CameraSetupXC> CameraSetups;


    private Transform MyLocalClient;
    private Dictionary<ParticipantOrder, Pose> MySpawnPositions;
   
    public bool ready { get; private set; } // property

    public enum FloorType {
        CROSSWALK,
        EMPTY,
        LIGHTS
    }

    [SerializeField] public FloorType m_FloorType;
  

    public bool HasVisualScene()
    {
        if (VisualSceneToUse != null && VisualSceneToUse.SceneName.Length > 0)
        {
            return true;
        }

        return false;
        
    }

   

    private void Start() {
       
        GetSpawnPoints();
        ready = true;
       
    }

    public FloorType GetFloorTypeForThisScenarion() {
        return m_FloorType;
    }


    private void Update() {
    }

    public bool GetSpawnPose(ParticipantOrder participantOrder, out Pose pose) {
        GetSpawnPoints();

        if (MySpawnPositions != null) {
            if (MySpawnPositions.Count > 0 && MySpawnPositions.ContainsKey(participantOrder)) {
                pose = MySpawnPositions[participantOrder];
                return true;
            }

            Debug.Log("Did not find an assigned spawn point");
            pose = new Pose();
            return false;
        }

        Debug.Log("SpawnPoint is null");
        pose = new Pose();
        return false;
    }


    private void GetSpawnPoints() {
        if (MySpawnPositions == null || MySpawnPositions.Count == 0) {
            MySpawnPositions = new Dictionary<ParticipantOrder, Pose>();
           
            foreach (var sp in GetComponentsInChildren<SpawnPosition>()) {
                var transform1 = sp.transform;
                MySpawnPositions[sp.StartingId] = new Pose(transform1.position, transform1.rotation);
            }
        }
    }

    
    public Dictionary<ParticipantOrder, NavigationScreen.Direction> GetStartingPositions() {
        return new Dictionary<ParticipantOrder, NavigationScreen.Direction> {
            { ParticipantOrder.A, StartingDirectionParticipantA },
            { ParticipantOrder.B, StartingDirectionParticipantB },
            { ParticipantOrder.C, StartingDirectionParticipantC },
            { ParticipantOrder.D, StartingDirectionParticipantD },
            { ParticipantOrder.E, StartingDirectionParticipantE },
            { ParticipantOrder.F, StartingDirectionParticipantF }
        };
    }

   

    public List<QuestionnaireQuestion> ReadString(string asset) {
        return JsonConvert.DeserializeObject<List<QuestionnaireQuestion>>(asset);
    }

    private string OverwriteQNDataFolderName = "QN_DATA";
    public List<QuestionnaireQuestion> GetQuestionObject() {
        var p = Path.Combine(Application.persistentDataPath, OverwriteQNDataFolderName);
        var file = Path.Combine(Application.persistentDataPath, OverwriteQNDataFolderName,
            QuestionairToAsk.name + ".json");
        Debug.Log(file);
        if (!Directory.Exists(p)) Directory.CreateDirectory(p);

        if (!File.Exists(file)) File.WriteAllText(file, QuestionairToAsk.text);


        var outval = ReadString(File.ReadAllText(file));

        var startCounter = 0;
        foreach (var q in outval) {
            q.setInternalID(startCounter);
            startCounter++;
        }
        return outval;
    }
    
    
    #region GPSUpdate

    public void SetStartingGPSDirections() {
        UpdateAllNavigationInstructions(FindObjectOfType<ScenarioManager>().GetStartingPositions());
    }

    public void UpdateAllNavigationInstructions(Dictionary<ParticipantOrder, NavigationScreen.Direction> dict) {
        foreach (var t in ConnectionAndSpawning.Singleton.Main_ParticipantObjects.Values) {
            t.SetNewNavigationInstruction(dict);
        }
        
    }

    #endregion
    
    
}