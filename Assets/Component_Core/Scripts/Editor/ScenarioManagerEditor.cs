using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;



[CustomEditor(typeof(ScenarioManager))]
public class ScenarioManagerEditor : Editor {
   
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        var sm = (ScenarioManager)target;
        if (GUILayout.Button("Attempt QN-Deserialization")) {
            
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
        GUILayout.Label($"ConditionName: {sm.ConditionName}");
        if (GUILayout.Button("StoreSpawnPositons")) {
            if (sm != null) {
                const ParticipantOrder pedPO = ParticipantOrder.B;
                var l = sm.transform.GetComponentsInChildren<SpawnPosition>()
                    .First(x => x.StartingId == pedPO);

                var zed = FindObjectOfType<ZedSpaceReference>();

                if (zed != null && l != null) {
                    
                    var position = zed.transform.InverseTransformPoint(l.transform.position);
                    float fwd = Vector3.SignedAngle(zed.transform.forward, l.transform.forward,Vector3.up);
                    string path = Application.dataPath +"/Resources/"+ScenarioManager.PedestrianSpawnPointLocationPathJson;
                    Debug.Log(path);
                    Dictionary<string, Dictionary<ParticipantOrder, float[]>> dict;
                    var sceneName = sm.gameObject.scene.name;
                    float[] o_pose = new float[4];
                    o_pose[0] = position.x;
                    o_pose[1] = position.y;
                    o_pose[2] = position.z;
                    o_pose[3] = fwd;
                    
                    if (!File.Exists(path)) {
                        dict = new Dictionary<string, Dictionary<ParticipantOrder, float[]>>();
                    }
                    else {
                        string s = File.ReadAllText(path);
                        dict = JsonConvert
                            .DeserializeObject<Dictionary<string, Dictionary<ParticipantOrder, float[]>>>(s);
                    }

                    if (!dict.ContainsKey(sceneName)) {
                        dict.Add(sceneName, new Dictionary<ParticipantOrder, float[]>());
                    }

                    if (dict[sceneName].ContainsKey(pedPO)) {
                        dict[sceneName][pedPO] = o_pose;
                    }
                    else {
                        dict[sceneName].Add(pedPO, o_pose);
                    }

                    string json = JsonConvert.SerializeObject(dict, Formatting.Indented);
                    File.WriteAllText(path, json);
                }
                else {Debug.LogError("Could not store participantB pose!"); }
            }
        }

        if (GUILayout.Button("Name Spawn Spots")) {
            foreach (var sp in sm.GetComponentsInChildren<SpawnPosition>()) {
                sp.name = $"SpawnPoint for {sp.StartingId}";
            }
            
        }
    }
}

