using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScenarioManager))]
public class ScenarioManagerEditor : Editor {
<<<<<<< HEAD
    
    private Transform tempTransform;
    public Dictionary<string,Dictionary<ParticipantOrder, SimplePose>> SpawnPositionsDictionary;
    
    public class SimplePose
    {
        public float posX, posY, posZ; // Position components
        public float rotX, rotY, rotZ, rotW; // Quaternion components

        public SimplePose(Vector3 pos, Quaternion rot)
        {
            posX = pos.x;
            posY = pos.y;
            posZ = pos.z;
            rotX = rot.x;
            rotY = rot.y;
            rotZ = rot.z;
            rotW = rot.w;
        }
    }
    private SimplePose temPose;

=======
>>>>>>> parent of f342d865 (Nov 15th.)
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        if (GUILayout.Button("Attemp QN Deserialization")) {
            var sm = (ScenarioManager)target;
            if (sm != null) {
                var numbers = new List<int>();
                var s = sm.GetQuestionObject();
                foreach (var q in s) {
                    if (numbers.Contains(q.getInteralID()))
                        Debug.LogWarning("Duplicat ID for " + q.getInteralID());
                    else
                        numbers.Add(q.getInteralID());
                   
                    Debug.Log($"{q.QuestionText["English"]}, po:{q.ContainsOrder(ParticipantOrder.B)}");
                    var numbersA = new List<int>();
                    foreach (var a in q.Answers)
                        if (numbersA.Contains(a.index))
                            Debug.LogWarning("Duplicat question index " + a.index + " at question:" + q.getInteralID());
                        else
                            numbersA.Add(a.index);
                }
            }
        }
<<<<<<< HEAD
        // Upload the spawn points into a json file
        if (GUILayout.Button("Upload spawn points"))
        {
            var sm = (ScenarioManager)target;
            if (sm != null)
            {
                // Access the # of children need to fetch
                // Default is equal to 6, change here if number of spawn points has changed
                int childrenNum = 6;
                Dictionary<ParticipantOrder, SimplePose> transformDict = new Dictionary<ParticipantOrder, SimplePose>();
                SpawnPositionsDictionary = new Dictionary<string, Dictionary<ParticipantOrder, SimplePose>>();
                for (int i = 0; i < childrenNum; i++)
                {
                    tempTransform = sm.transform.GetChild(i);
                    SpawnPosition spawnPosition = tempTransform.GetComponent<SpawnPosition>();
                    ParticipantOrder participantOrder = spawnPosition.StartingId;
                    temPose = new SimplePose(tempTransform.position, tempTransform.rotation);
                    // store the current transform and rotation into the dictionary
                    transformDict.Add(participantOrder, temPose);
                }
                Debug.Log("Loading...");
                foreach (var entry in transformDict)
                {
                    Debug.Log($"Key: {entry.Key}, Data: {string.Join(", ", entry.Value)}");
                }
                
                /*********************** Upload the Json File***********************/
                // write it to the json file
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                // read the path from the scenario manager
                string filePath = ScenarioManager.SpawnPositonData;
                string json = "";
                if (File.Exists(filePath))
                {
                    Debug.Log("File exists, now updating");
                    string jsonContent = File.ReadAllText(filePath);
                    // convert the json file back to dictionary
                    SpawnPositionsDictionary = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<ParticipantOrder, SimplePose>>>(jsonContent);
                    // update the dictionary 
                    SpawnPositionsDictionary[sm.conditionName] = transformDict;
                    json = JsonConvert.SerializeObject(SpawnPositionsDictionary, Formatting.Indented,settings);
                    // write the file to destination
                    File.WriteAllText(filePath, json);
                    Debug.Log($"Spawn points saved to {filePath}");
                    // foreach (var scenario in SpawnPositionsDictionary)
                    // {
                    //     string scenarioName = scenario.Key;
                    //     Dictionary<ParticipantOrder, SimplePose> poseDict = scenario.Value;
                    // }

                }
                else
                {
                    Debug.Log("No existing dictionary found. Writing to a dictionary");
                    // add all spawn points ot the dictionary
                    SpawnPositionsDictionary[sm.conditionName] = transformDict;
                    json = JsonConvert.SerializeObject(SpawnPositionsDictionary, Formatting.Indented,settings);
                    // write the file to destination
                    File.WriteAllText(filePath, json);
                    Debug.Log($"Spawn points saved to {filePath}");
                }
                
            }
        }
=======
>>>>>>> parent of f342d865 (Nov 15th.)
    }
}