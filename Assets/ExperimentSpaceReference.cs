using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
[CustomEditor(typeof(ExperimentSpaceReference))]
public class SpaceReferenceEditor : Editor {
    private int i = 0;

    public override void OnInspectorGUI() {
        DrawDefaultInspector(); // Draws the default inspector
        var reference = (ExperimentSpaceReference) target;
        if (GUILayout.Button("Store New Zed Setup")) {
            //reference.storeNewSetup();
        }

        if (GUILayout.Button("Re-Load Setup")) reference.LoadSetup();
        if (GUILayout.Button("Clear Working Area")) reference.ClearWorkingArea();

        if (GUILayout.Button("Demo Guardian System")) {
            reference.LoadSetup();
            var tmp = new GameObject();
            tmp.transform.parent = reference.transform;
            tmp.transform.localPosition = Vector3.zero;
            tmp.name = "tmpHead";
            tmp.AddComponent<Camera>();
            reference.Create3DRectangularMeshes(tmp.transform);
        }
    }
}
#endif

public class ExperimentSpaceReference : MonoBehaviour {
    private const int workAreaCount = 4;

    // first two vector3s are the pos and rot of calibration point
    // rotation of the calib point should face same way as participant when calibrating
    // next four vector3s are the corners of the room
    public static string RoomSetUp = "SpaceReference2";
    public static string RoomSetUpPath = Application.dataPath + "/Resources/" + RoomSetUp + ".json";
    public List<Transform> WorkingArea = new();

    public Transform calibrationPoint1;
    public Transform calibrationPoint2;

    public float MeshWidth = 0.2f;
    public float MeshHeight = 2f;
    public int NumMeshes = 3;
    public float MeshSpacing = 1.0f;
    public Material MeshMaterial;

    private void Start() {
        Debug.Log("Loading CallibrationPoint");
        LoadSetup();
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        WorkingArea.RemoveAll(item => item == null);

        if (WorkingArea != null && WorkingArea.Count > 1) {
            for (var i = 0; i < WorkingArea.Count - 1; i++)
                Gizmos.DrawLine(WorkingArea[i].position, WorkingArea[i + 1].position);
            Gizmos.DrawLine(WorkingArea[0].position, WorkingArea[WorkingArea.Count - 1].position);
        }

        if (calibrationPoint1 != null) Gizmos.DrawCube(calibrationPoint1.position, Vector3.one * 0.1f);
    }

    public void ClearWorkingArea() {
        WorkingArea.Clear();

        for (var i = transform.childCount - 1; i >= 0; i--) {
#if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(i).gameObject);
#else
            Destroy(transform.GetChild(i).gameObject);
#endif
        }
    }

    // TODO: Make this store 2 calibration points
    public void StoreNewSetup() {
        var outVal = new float[transform.childCount + 1][];
        var tmp = new float[3];

        tmp[0] = calibrationPoint1.localPosition.x;
        tmp[1] = calibrationPoint1.localPosition.y;
        tmp[2] = calibrationPoint1.localPosition.z;
        outVal[0] = tmp;
        tmp = new float[3];
        tmp[0] = calibrationPoint1.localRotation.eulerAngles.x;
        tmp[1] = calibrationPoint1.localRotation.eulerAngles.y;
        tmp[2] = calibrationPoint1.localRotation.eulerAngles.z;
        outVal[1] = tmp;

        for (var i = 2; i < WorkingArea.Count + 2; i++) {
            var t = WorkingArea[i - 2];
            tmp = new float[3];
            tmp[0] = t.localPosition.x;
            tmp[1] = t.localPosition.y;
            tmp[2] = t.localPosition.z;
            outVal[i] = tmp;
            Debug.Log(t.localPosition.x);
        }

        var json = JsonConvert.SerializeObject(outVal, Formatting.Indented);
        File.WriteAllText(RoomSetUpPath, json);
    }

    private GameObject CreateCalibrationPoint(Dictionary<string, List<int>> coords) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.parent = transform;

        var pos = new Vector3 {
            x = coords["pos"][0],
            y = coords["pos"][1],
            z = coords["pos"][2]
        };
        calibrationPoint1.localPosition = pos;

        var rot = new Vector3 {
            x = coords["rot"][0],
            y = coords["rot"][1],
            z = coords["rot"][2]
        };
        var rotation = calibrationPoint1.localRotation;
        rotation.eulerAngles = rot;
        calibrationPoint1.localRotation = rotation;

        calibrationPoint1.localScale = Vector3.one * 0.1f;

        return go;
    }

    private GameObject CreateBorderPoint(Dictionary<string, List<int>> coords) {
        var go = new GameObject {
            transform = {
                parent = transform,
                position = new Vector3 {
                    x = coords["pos"][0],
                    y = coords["pos"][1],
                    z = coords["pos"][2]
                },
                rotation = transform.rotation
            }
        };
        return go;
    }

    public void LoadSetup() {
#if UNITY_EDITOR
        if (!File.Exists(RoomSetUpPath)) return;
        var s = File.ReadAllText(RoomSetUpPath);
#else
        var t = Resources.Load<TextAsset>(RoomSetUp);
       string s = t.ToString();

#endif

        var outVal = JsonConvert
            .DeserializeObject<Dictionary<string, Dictionary<string, List<int>>>>(s);
        WorkingArea.Clear();
        for (var i = transform.childCount - 1; i >= 0; i--) {
#if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(i).gameObject);

#else
            Destroy(transform.GetChild(i).gameObject);
#endif
        }


        if (calibrationPoint1 == null) {
            calibrationPoint1 = CreateCalibrationPoint(outVal["Cal1"]).transform;
            calibrationPoint1.name = "CalibrationPoint1";
        }

        if (calibrationPoint2 == null) {
            calibrationPoint2 = CreateCalibrationPoint(outVal["Cal2"]).transform;
            calibrationPoint2.name = "CalibrationPoint2";
        }

        for (var i = 2; i < workAreaCount + 2; i++) {
            var go = new GameObject();

            go.transform.parent = transform;
            go.transform.name = $"Border {i - 1}";

            tmp = new Vector3();
            tmp.x = outVal[i][0];
            tmp.y = outVal[i][1];
            tmp.z = outVal[i][2];

            go.transform.localPosition = tmp;
            go.transform.rotation = transform.rotation;

            WorkingArea.Add(go.transform);
        }

        // load mesh
        //Create3DRectangularMeshes();
    }

    public Transform GetCalibrationPoint() {
        if (calibrationPoint1 == null) LoadSetup();
        return calibrationPoint1;
    }

    public void Create3DRectangularMeshes(Transform transform1) {
        for (var i = 0; i < WorkingArea.Count; i++) {
            var startPosition = WorkingArea[i].position;
            var endPosition = WorkingArea[(i + 1) % WorkingArea.Count].position;
            var edgeDirection = (endPosition - startPosition).normalized;
            var midPoint = (startPosition + endPosition) / 2;
            var rotation = Quaternion.LookRotation(edgeDirection);

            // mornal used for direction to move3 outwards from the edge  | -->
            var normalToEdge = Vector3.Cross(edgeDirection, Vector3.up).normalized;
            var scale = new Vector3(MeshWidth, MeshHeight, (endPosition - startPosition).magnitude);


            var tmp = new GameObject();
            tmp.name = $"VR-Guardian{i}";
            var meshFilter = tmp.AddComponent<MeshFilter>();
            var renderer = tmp.AddComponent<MeshRenderer>();
            var barrier = tmp.AddComponent<VRBarrier>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;
            renderer.allowOcclusionWhenDynamic = false;
            renderer.material = Instantiate(MeshMaterial);

            barrier.trackingTransform(transform1);

            tmp.transform.parent = transform;
            tmp.transform.position = midPoint;
            tmp.transform.forward = normalToEdge;


            var vertices = new Vector3[4];


            vertices[0] =
                tmp.transform.InverseTransformPoint(new Vector3(startPosition.x, transform.position.y,
                    startPosition.z));
            vertices[1] = tmp.transform.InverseTransformPoint(new Vector3(startPosition.x,
                transform.position.y + MeshHeight, startPosition.z));
            vertices[3] =
                tmp.transform.InverseTransformPoint(new Vector3(endPosition.x, transform.position.y, endPosition.z));
            vertices[2] =
                tmp.transform.InverseTransformPoint(new Vector3(endPosition.x, transform.position.y + MeshHeight,
                    endPosition.z));


            meshFilter.sharedMesh = new Mesh();
            meshFilter.sharedMesh.vertices = vertices;
            meshFilter.sharedMesh.triangles = new[] {0, 1, 2, 2, 3, 0};
            meshFilter.sharedMesh.uv = new[]
                {new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0)};
        }
    }


    public void SetBoundaries(Transform transform1) {
        if (transform1 != null) Create3DRectangularMeshes(transform1);
    }
}