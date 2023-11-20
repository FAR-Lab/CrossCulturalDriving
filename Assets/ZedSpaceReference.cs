using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        ZedSpaceReference script = (ZedSpaceReference)target;

        // Button to deserialize JSON data
        if (GUILayout.Button("Deserialize Object Data"))
        {
            script.DeserializeObjectData();
        }
        /*
        if (GUILayout.Button("SotreSelectedAgentLocation")) {
            var t = FindObjectOfType<ZEDMaster>();
            if (t != null) {
                script.WorkingArea[i].position = t.GetHipPosition();
                i++;
                if(script.WorkingArea.Count>i)
                { i = 0;}
            }
        }*/
    }
}
#endif
public class ZedSpaceReference : MonoBehaviour {

    public TextAsset ZedCallibrationFile;

    
    public Dictionary<string, DeviceConfig> m_devices;

    [SerializeField]
    public List<DeviceConfigMono> m_ZedCameras;

    public Transform ZedRootObject;
    public const string ZedRootName = "ZedRootObject";

    public List<Transform> WorkingArea = new List<Transform>();
    // Start is called before the first frame update
    void Start() {
      
    }
    private void OnDrawGizmos() {
        
        if (m_ZedCameras != null && m_ZedCameras.Count() > 0) {

            float hightoffset = m_ZedCameras[0].transform.position.y;
            Vector3 of = new Vector3(0, hightoffset, 0);
            Vector3 prev = Vector3.zero;
            bool first = true;
            foreach (var cam  in m_ZedCameras) {
                if (cam != null) {
                    if (first) {
                        first = false;
                        prev = cam.transform.position;
                    }
                    else {
                     //   Gizmos.DrawLine(cam.transform.position+of,prev+of);
                        prev = cam.transform.position;
                    }
                }
            } 
            
        }

        if (WorkingArea != null && WorkingArea.Count > 1) {
            for (int i = 0; i < WorkingArea.Count-1; i++) {
                Gizmos.DrawLine(WorkingArea[i].position, WorkingArea[i + 1].position);
            }
            Gizmos.DrawLine(WorkingArea[0].position, WorkingArea[WorkingArea.Count-1].position);
        }
        
    }

    public void DeserializeObjectData() {
        ZedRootObject = transform.Find(ZedRootName);
        if (ZedRootObject == null) {
            ZedRootObject = new GameObject().transform;
            ZedRootObject.name = ZedRootName;   
            ZedRootObject.parent=transform;
            ZedRootObject.localPosition=Vector3.zero;
        }

        if (m_ZedCameras != null ) {
            for (int i = 0; i < m_ZedCameras.Count(); i++) {
                if(m_ZedCameras[i] != null){

                    DestroyImmediate(m_ZedCameras[i].gameObject);
                }
            }

            m_ZedCameras.Clear();
        }
        else {
            m_ZedCameras = new List<DeviceConfigMono>();
        }

        for (int i = 0; i < ZedRootObject.childCount; i++) {
            DestroyImmediate(ZedRootObject.GetChild(i).gameObject);
        }
        m_devices = new Dictionary<string, DeviceConfig>();
        m_devices =  JsonConvert.DeserializeObject<Dictionary<string, DeviceConfig>>(ZedCallibrationFile.text);
        Debug.Log(JsonConvert.DeserializeObject<Dictionary<string, DeviceConfig>>(ZedCallibrationFile.text).Values.Count() );
        
        foreach (var d in m_devices.Values) {
            
            GameObject g = new GameObject();
            m_ZedCameras.Add(g.AddComponent<DeviceConfigMono>());
            g.transform.name = d.Input.Zed.Configuration;
            g.transform.parent = ZedRootObject;
            g.GetComponent<DeviceConfigMono>().setConfig(d);

        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    
    
    
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

// Main class to hold the dictionary
  
}
