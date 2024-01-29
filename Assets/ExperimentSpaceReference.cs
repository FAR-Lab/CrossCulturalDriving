
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(ExperimentSpaceReference))]
public class SpaceReferenceEditor : Editor
{
    private int i = 0;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draws the default inspector
        var reference = (ExperimentSpaceReference)target;
        if (GUILayout.Button("Store New Zed Setup"))
        {
            //reference.storeNewSetup();
        }
        if (GUILayout.Button("Re-Load Setup"))
        {
           reference.LoadSetup();
        }
        if (GUILayout.Button("Clear Working Area"))
        {
            reference.ClearWorkingArea();
        }
    }

    private void OnSceneGUI()
    {

    }

    private void OnEnable()
    {

    }
}
#endif

public class ExperimentSpaceReference : MonoBehaviour {
    private const int workAreaCount = 4;
    public List<Transform> WorkingArea = new List<Transform>();
    public static string RoomSetUp = "SpaceReference";
    public static string RoomSetUpPath = Application.dataPath + "/Resources/"+RoomSetUp+".json";

[SerializeField]
    public Transform callibrationPoint;

    public float MeshWidth = 0.2f;
    public float MeshHeight = 2f;
    public int NumMeshes = 3;
    public float MeshSpacing = 1.0f;
    public Material MeshMaterial;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (WorkingArea != null && WorkingArea.Count > 1)
        {
            for (int i = 0; i < WorkingArea.Count - 1; i++)
            {
                Gizmos.DrawLine(WorkingArea[i].position, WorkingArea[i + 1].position);
            }
            Gizmos.DrawLine(WorkingArea[0].position, WorkingArea[WorkingArea.Count - 1].position);
        }

        if (callibrationPoint != null) {
            Gizmos.DrawCube(callibrationPoint.position, Vector3.one * 0.1f);
        }
    }

    public void ClearWorkingArea()
    {
        WorkingArea.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            #if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(i).gameObject);
            #else
            Destroy(transform.GetChild(i).gameObject);
            #endif
        }       
        
    }

    private void Start() {
        Debug.Log("Loading CallibrationPoint");
        LoadSetup();
    }

    public void storeNewSetup()
    {
        float[][] outVal = new float[transform.childCount+1][];
        float[] tmp = new float[3];
        
        tmp[0] = callibrationPoint.localPosition.x;
        tmp[1] = callibrationPoint.localPosition.y;
        tmp[2] = callibrationPoint.localPosition.z;
        outVal[0] = tmp;
        tmp = new float[3];
        tmp[0] = callibrationPoint.localRotation.eulerAngles.x;
        tmp[1] = callibrationPoint.localRotation.eulerAngles.y;
        tmp[2] = callibrationPoint.localRotation.eulerAngles.z;
        outVal[1] = tmp;
        
        for (int i = 2; i < WorkingArea.Count+2; i++) {
            var t = WorkingArea[i-2];
            tmp = new float[3];
            tmp[0] = t.localPosition.x;
            tmp[1] = t.localPosition.y;
            tmp[2] = t.localPosition.z;
            outVal[i] = tmp;
            Debug.Log(t.localPosition.x);
        }

        string json = JsonConvert.SerializeObject(outVal, Formatting.Indented);
        File.WriteAllText(RoomSetUpPath, json);

    }

    public void LoadSetup()
    {
        #if UNITY_EDITOR
        if (!File.Exists(RoomSetUpPath))
        {
            return;
        }
        string s = File.ReadAllText(RoomSetUpPath);
        #else 
        var t =  Resources.Load<TextAsset>(RoomSetUp);
       string s = t.ToString();
        
        #endif
       
        float[][] outVal = JsonConvert
            .DeserializeObject<float[][]>(s);
        WorkingArea.Clear();
        for (int i = transform.childCount-1; i >=0 ; i--) {
          
            #if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(i).gameObject);

            #else
            Destroy(transform.GetChild(i).gameObject);
#endif
        }
        
        
        if (callibrationPoint == null) {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            callibrationPoint = go.transform;
            callibrationPoint.parent = transform;
            callibrationPoint.name = "CallibrationPoint";
        }
        Vector3 tmp = new Vector3();
        
        tmp.x = outVal[0][0];
        tmp.y = outVal[0][1];
        tmp.z = outVal[0][2];
        callibrationPoint.localPosition = tmp;
        tmp = new Vector3();
        tmp.x = outVal[1][0];
        tmp.y = outVal[1][1];
        tmp.z = outVal[1][2];
        var rotation = callibrationPoint.localRotation;
        rotation.eulerAngles = tmp;
        callibrationPoint.localRotation = rotation;
        callibrationPoint.localScale=Vector3.one*0.1f;
        
        
        for (int i = 2; i < workAreaCount+2; i++)
        {
            
            var go = new GameObject();
            
            go.transform.parent = transform;
            go.transform.name = $"Border {i-1}";
            
            tmp = new Vector3();
            tmp.x = outVal[i][0];
            tmp.y = outVal[i][1];
            tmp.z = outVal[i][2];

            go.transform.localPosition = tmp;
            go.transform.rotation = transform.rotation;

            WorkingArea.Add(go.transform);
        }
        
        // load mesh
        Create3DRectangularMeshes();

    }

    public Transform GetCallibrationPoint() {
        return callibrationPoint;
    }
    public void Create3DRectangularMeshes()
    {
        for (int i = 0; i < WorkingArea.Count; i++)
        {
            Vector3 startPosition = WorkingArea[i].position;
            Vector3 endPosition = WorkingArea[(i + 1) % WorkingArea.Count].position;
            Vector3 edgeDirection = (endPosition - startPosition).normalized;
            Vector3 midPoint = (startPosition + endPosition) / 2;
            Quaternion rotation = Quaternion.LookRotation(edgeDirection);

            // mornal used for direction to move outwards from the edge  | -->
            Vector3 normalToEdge = Vector3.Cross(edgeDirection, Vector3.up).normalized;
            Vector3 scale = new Vector3(MeshWidth, MeshHeight, (endPosition - startPosition).magnitude);

            for (int j = 0; j < NumMeshes; j++)
            {
                // move outwards from the last mesh
                Vector3 meshPosition = midPoint + normalToEdge * MeshSpacing * j;

                GameObject meshObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                meshObj.transform.position = meshPosition;
                meshObj.transform.rotation = rotation;
                meshObj.transform.localScale = scale;
                meshObj.transform.parent = transform;

                if (MeshMaterial != null)
                {
                    Renderer meshRenderer = meshObj.GetComponent<Renderer>();
                    meshRenderer.material = MeshMaterial;
                    meshObj.AddComponent<VRBarrier>();
                }
            }
        }
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
