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
        Debug.Log("Loading CalibrationPoint");
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

    public void StoreNewSetup() {
        var outVal = new Dictionary<string, Dictionary<string, List<float>>> {
            ["Cal1"] = StoreCalibrationPoint(calibrationPoint1),
            ["Cal2"] = StoreCalibrationPoint(calibrationPoint2)
        };

        for (var i = 0; i < WorkingArea.Count; i++) {
            var t = WorkingArea[i];
            outVal.Add("Border" + (i + 1), StoreBorderPoint(t));
        }

        var json = JsonConvert.SerializeObject(outVal, Formatting.Indented);
        File.WriteAllText(RoomSetUpPath, json);
    }

    private Dictionary<string, List<float>> StoreCalibrationPoint(Transform calibrationPoint) {
        var outVal = new Dictionary<string, List<float>> {
            ["pos"] = new() {
                calibrationPoint.localPosition.x,
                calibrationPoint.localPosition.y,
                calibrationPoint.localPosition.z
            },
            ["rot"] = new() {
                calibrationPoint.localRotation.eulerAngles.x,
                calibrationPoint.localRotation.eulerAngles.y,
                calibrationPoint.localRotation.eulerAngles.z
            }
        };
        return outVal;
    }

    private Dictionary<string, List<float>> StoreBorderPoint(Transform borderPoint) {
        var outVal = new Dictionary<string, List<float>> {
            ["pos"] = new() {
                borderPoint.localPosition.x,
                borderPoint.localPosition.y,
                borderPoint.localPosition.z
            }
        };
        return outVal;
    }

    private Transform CreateCalibrationPoint(Dictionary<string, List<float>> coords) {
        var t = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        t.parent = transform;

        var pos = new Vector3 {
            x = coords["pos"][0],
            y = coords["pos"][1],
            z = coords["pos"][2]
        };
        t.localPosition = pos;

        var rot = new Vector3 {
            x = coords["rot"][0],
            y = coords["rot"][1],
            z = coords["rot"][2]
        };
        var rotation = t.localRotation;
        rotation.eulerAngles = rot;
        t.localRotation = rotation;

        t.localScale = Vector3.one * 0.1f;

        return t;
    }

    private Transform CreateBorderPoint(Dictionary<string, List<float>> coords) {
        var go = new GameObject {
            transform = {
                parent = transform,
                localPosition = new Vector3 {
                    x = coords["pos"][0],
                    y = coords["pos"][1],
                    z = coords["pos"][2]
                },
                rotation = transform.rotation
            }
        };
        return go.transform;
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
            .DeserializeObject<Dictionary<string, Dictionary<string, List<float>>>>(s);
        WorkingArea.Clear();
        for (var i = transform.childCount - 1; i >= 0; i--) {
#if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(i).gameObject);

#else
            Destroy(transform.GetChild(i).gameObject);
#endif
        }


        if (calibrationPoint1 == null) {
            calibrationPoint1 = CreateCalibrationPoint(outVal["Cal1"]);
            calibrationPoint1.name = "CalibrationPoint1";
        }

        if (calibrationPoint2 == null) {
            calibrationPoint2 = CreateCalibrationPoint(outVal["Cal2"]);
            calibrationPoint2.name = "CalibrationPoint2";
        }

        for (var i = 1; i <= workAreaCount; i++) {
            var borderPoint = CreateBorderPoint(outVal["Border" + i]);
            borderPoint.name = "BorderPoint" + i;
            WorkingArea.Add(borderPoint);
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