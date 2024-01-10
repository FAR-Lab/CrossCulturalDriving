
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(ZedSpaceReference))]
public class DeviceConfigManagerEditor : Editor {
    private int i = 0;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draws the default inspector
        var reference = (ZedSpaceReference)target;
        if (GUILayout.Button("Store New Zed Setup")) {
            reference.storeNewSetup();
        }
        if (GUILayout.Button("Re-Load Setup")) {
            reference.LoadSetup();
        }
    }

    private void OnSceneGUI() {
      
    }

    private void OnEnable() {
        
    }
}
#endif

public class ZedSpaceReference : MonoBehaviour {

   private List<Transform> WorkingArea = new List<Transform>();
   public static string RoomSetUpPath = Application.dataPath + "/Resources/ZedRoomSetUp.json";
   
    private void OnDrawGizmos() {
        Gizmos.color=Color.red;
        if (WorkingArea != null && WorkingArea.Count > 1) {
            for (int i = 0; i < WorkingArea.Count-1; i++) {
                Gizmos.DrawLine(WorkingArea[i].position, WorkingArea[i + 1].position);
            }
            Gizmos.DrawLine(WorkingArea[0].position, WorkingArea[WorkingArea.Count-1].position);
        }
        Gizmos.DrawCube(transform.position,Vector3.one*0.1f);
    }

    public void storeNewSetup() {
        float[][] outVal = new float[transform.childCount][]; 
        for ( int i=0; i< transform.childCount; i++) {
            var t = transform.GetChild(i);
            float[] tmp = new float[3];
            tmp[0] = t.localPosition.x;
            tmp[1] = t.localPosition.y;
            tmp[2] = t.localPosition.z;
            outVal[i] =tmp;
            
        }

        
        string json = JsonConvert.SerializeObject(outVal, Formatting.Indented);
        File.WriteAllText(RoomSetUpPath, json);
        
    }
    
    public void LoadSetup() {
        if (!File.Exists(RoomSetUpPath)) {
            return;
        }
        string s = File.ReadAllText(RoomSetUpPath);
        float[][] outVal = JsonConvert
            .DeserializeObject<float[][]>(s);

        if (outVal.Length > transform.childCount) {
            int count = outVal.Length - transform.childCount;
            for (int i = 0; i < count; i++) {
                var w = new GameObject();
                w.transform.parent = transform;
            }
        }

        WorkingArea.Clear();
        for ( int i=0; i< transform.childCount; i++) {
            var t = transform.GetChild(i);
            Vector3 tmp = new Vector3();
            
            tmp.x = outVal[i][0];
            tmp.y = outVal[i][1];
            tmp.z = outVal[i][2];
           
            t.localPosition = tmp;
            WorkingArea.Add(t);
        }
        string json = JsonConvert.SerializeObject(outVal, Formatting.Indented);
        File.WriteAllText(RoomSetUpPath, json);
        
    }

    
    #region OriginalZedConfigData
    public class DeviceConfig
    {
        [JsonProperty("input")]
        public InputData Input { get; set; }

        [JsonProperty("world")]
        public WorldData World { get; set; }
    }

    public class InputData
    {
        [JsonProperty("fusion")]
        public FusionData Fusion { get; set; }

        [JsonProperty("zed")]
        public ZedData Zed { get; set; }
    }

    public class FusionData
    {
        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class ZedData
    {
        [JsonProperty("configuration")]
        public string Configuration { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class WorldData
    {
        [JsonProperty("rotation")]
        public List<double> Rotation { get; set; }

        [JsonProperty("translation")]
        public List<double> Translation { get; set; }
    }
#endregion

}
