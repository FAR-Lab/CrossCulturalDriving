#define DEBUGAUTONMOUSDRIVER

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using UnityEditor.Rendering;
using UnityEngine;


public class AutonomousVehicleDriver : MonoBehaviour {
    public enum AVDrivingState {
        DEFAULT,
        STOPPED,
        DRIVING,
        YIELD,
        CRASH
    }

    public enum DrivingDirection {
        LEFT = -1,
        STRAIGHT = 0,
        RIGHT = 1,
    }

    private AVDrivingState _avDrivingState;
    public bool UsePythonBackend;
    public bool Running = false;
    public WayPoint StartingWayPoint;
    [SerializeField]
    private DrivingDirection _drivingDirection;
    private WayPoint NextWaypoint = null;
    private bool newWayPointFound;

    private bool newDataRead;

    private float Steering;
    private float Throttle;


    private VehicleController _vehicleController;

    private const float maxTurn = 70;


    private PID speedPID = new PID(0.2f, 0.005f, 0.005f);
    public float p1, i1, d1;
    // public float p2, i2, d2;
    private PID centerLinePID; //angle in degrees (- is left, + is right)
    // private PID anglePID;
    private float YieldTimer;

    private TriggerPlayerTracker AvoidBox;

    private TriggerPlayerTracker YieldBox;

    // Start is called before the first frame update
    private Vector3 Position;
    private Quaternion Orientation;

    public Transform IntersectionCenterPosition;
    private VehicleController otherCar;
    private NetworkVehicleController otherCarNet;
    
    private List<float[]> externData = new List<float[]>();
    void Start() {
        if (GetComponent<NetworkVehicleController>().IsServer) {
            _vehicleController = GetComponent<VehicleController>();
            _avDrivingState = AVDrivingState.DEFAULT;
            ConnectionAndSpawning.Singleton.ServerStateChange += InternalStateUpdate;

            AvoidBox = transform.Find("AvoidBox").GetComponent<TriggerPlayerTracker>();
            YieldBox = transform.Find("YieldBox").GetComponent<TriggerPlayerTracker>();
            GetComponent<Rigidbody>().isKinematic = true;
            if (UsePythonBackend) {
                StartCoroutine(PrepareToStart()); 
            }
        }
        else {
            this.enabled = false;
        }

        // anglePID = new PID(p2, i2, d2);
        centerLinePID = new PID(p1, i1, d1, -.6f, .6f);
    }

    private bool AssignOtherCar(bool usingReplay) {
        if (usingReplay) {
            var allCars = FindObjectsOfType<NetworkVehicleController>();
            foreach (var car in allCars) {
                if (car == GetComponent<NetworkVehicleController>()) continue;
                otherCar = car.GetComponent<VehicleController>();
                otherCarNet = car;
                return true;
            }
            return false;
        }
        
        if (ConnectionAndSpawning.Singleton != null
            && ConnectionAndSpawning.Singleton.ServerState is ActionState.READY or ActionState.DRIVE) {
            var t = ConnectionAndSpawning.Singleton
                .GetInteractableObjects_For_Participants(ParticipantOrder.A);

            if (t != null && t.Count>0) {
                otherCar = t.First().GetComponent<VehicleController>();
                otherCarNet = t.First().GetComponent<NetworkVehicleController>();
            }

            if (otherCar != null) {
                Debug.Log("Got the other car. Car A attempting to run NN!");
                return true;
            }
        }

        return false;
    }
    private IEnumerator PrepareToStart() 
    {
        yield return new WaitUntil(() => AssignOtherCar(true));
        
        IntersectionCenterPosition = FindObjectOfType<IntersectionCenter>().transform;
        var s = gameObject.AddComponent<UdpSocket>();
        
        yield return new WaitUntil(() => ConnectionAndSpawning.Singleton.ServerState == ActionState.DRIVE);
        s.GotNewAiData += NewPythonData;
        Running = true;
        StartCar(0);
        StartCoroutine(SendArtificalData(s, otherCar, otherCarNet));
    }
    
    private void StartCar(float startingCenterlineOffset) {
        Debug.Log("Starting AI Car");
        var intersectionCenter = FindObjectOfType<IntersectionCenter>().transform;

        //find starting pos and rot
        var line = _vehicleController.SplineCLCreator.points;

        Vector3 startRot = Vector3.zero, startPos = Vector3.zero;
        
        //loop through points until it is 30 meters or less from the intersection center
        for (int i = 0; i < line.Count; i++) {
            var thisPoint = line[i];
            if (Vector3.Distance(thisPoint, intersectionCenter.position) <= 30) {
                var nextPoint = line[i + 1];
                startRot = Vector3.Normalize(nextPoint - thisPoint);
                var rightDirection = new Vector3(startRot.z, 0, -startRot.x);
                startPos = thisPoint + rightDirection * startingCenterlineOffset;
                break;
            }
        }
        Debug.Log("Starting Pos: " + intersectionCenter.TransformPoint(startPos));
        
        var rb = GetComponent<Rigidbody>();
        rb.MovePosition(intersectionCenter.TransformPoint(new Vector3(startPos.x, 0.1f, startPos.z)));
        rb.MoveRotation(Quaternion.Euler(0, Vector3.SignedAngle(startRot, Vector3.forward, Vector3.up), 0) * intersectionCenter.rotation);
        rb.velocity = intersectionCenter.TransformDirection(5 * startRot);
    }

    private float[] m_outdata = new float[7];
    private void OnGUI() {
        string s = string.Concat(m_outdata.Select(x => x.ToString("F2")+" ,")) + $" throttle{externData.Last()[1]}";
        GUI.Label(new Rect(0,0,400,20),s );
        
    }
    private IEnumerator SendArtificalData(UdpSocket udpSocket, VehicleController l_otherCar, NetworkVehicleController l_otherCarNet) {
        Rigidbody m_rigidBody = _vehicleController.GetComponent<Rigidbody>();
        Rigidbody o_rigidBody = l_otherCar.GetComponent<Rigidbody>();


        float[] outdata = new float[10];
        Vector3 Distance, RelVelocity;
        float dot, Rel_Pos_Magnitude, ApproachRate, RelativeRotation;
        
        while (Running)
        {
            float b_indicator = 0f;

            foreach (Transform t in l_otherCarNet.Right)
            {
                // In Unity, we can't compare a material instance (when the game is playing) to a base material
                // Therefore, we check to see if their properties are equal instead
                if (t.GetComponent<MeshRenderer>().material.color.Equals(l_otherCarNet.IndicatorOn.color))
                {
                    Debug.Log("Right turn signal on");
                    b_indicator = 1f; // Right indicator is on
                }
            }
            foreach (Transform t in l_otherCarNet.Left)
            {
                if (t.GetComponent<MeshRenderer>().material.color.Equals(l_otherCarNet.IndicatorOn.color))
                {
                    b_indicator = -1f;  // Left indicator is on
                }
            }
            Distance = m_rigidBody.position - o_rigidBody.position;
            RelVelocity = m_rigidBody.velocity - o_rigidBody.velocity;
            dot = Vector3.Dot(Distance, RelVelocity);
            Rel_Pos_Magnitude =
                Distance.magnitude; // innerArea['Rel_Pos_Magnitude'] = np.sqrt(innerArea['Rel_Distance_X']**2 + innerArea['Rel_Distance_X']**2 + innerArea['Rel_Distance_X']**2)
            ApproachRate = dot / Rel_Pos_Magnitude; //  still should run a 1/dconvolve 
            //innerArea['ApproachRate'] = np.convolve((innerArea['Dot_Product'] / innerArea['Rel_Pos_Magnitude']), kernel, 'same')
            RelativeRotation =
                m_rigidBody.rotation.eulerAngles.y -
                o_rigidBody.rotation.eulerAngles
                    .y; //innerArea['RelativeRotation'] = innerArea['HeadrotYA']- innerArea['HeadrotYB']
//HeaderWithoutAccel = ["ApproachRate", "Rel_Pos_Magnitude", "SteerB", "A_Head_Center_Distance", "B_Head_Center_Distance", "Filtered_B_Head_Velocity_Total","RelativeRotation"]
            outdata[0] = ApproachRate; // "ApproachRate", 
            outdata[1] = Rel_Pos_Magnitude; //"Rel_Pos_Magnitude", 
            outdata[2] = l_otherCar.steerInput; //"SteerB", 
            outdata[3] = (m_rigidBody.position-IntersectionCenterPosition.position).magnitude; //"A_Head_Center_Distance",
            outdata[4] = (o_rigidBody.position-IntersectionCenterPosition.position).magnitude; // "B_Head_Center_Distance",
            outdata[5] = o_rigidBody.velocity.magnitude; // "Filtered_B_Head_Velocity_Total",
            outdata[6] = (int) _drivingDirection; // A Turn
            outdata[7] = b_indicator; //    B Indicator
            outdata[8] = l_otherCar.SplineCLCreator.GetClosestDistanceToSpline(o_rigidBody.position); // Centerline Offset_B
            outdata[9] = RelativeRotation; // "RelativeRotation"
            udpSocket.SendDataToPython(outdata);
            m_outdata = outdata;
            yield return new WaitForSeconds(1f / 18f);
        }
    }
    private void NewPythonData(float[] data) {
        if (data.Length > 1) {
            externData.Add(new []{Time.time, data[0], Mathf.Clamp(data[1], -2, 2)});
        }
    }

    private void OnDestroy() {
        if (GetComponent<NetworkVehicleController>().IsServer &&
            ConnectionAndSpawning.Singleton != null) {
            ConnectionAndSpawning.Singleton.ServerStateChange -= InternalStateUpdate;
        }
    }

    private void InternalStateUpdate(ActionState state) {
        switch (state) {
            case ActionState.DEFAULT:
                _avDrivingState = AVDrivingState.DEFAULT;
                break;
            case ActionState.WAITINGROOM:
                _avDrivingState = AVDrivingState.DEFAULT;
                break;
            case ActionState.LOADINGSCENARIO:
                _avDrivingState = AVDrivingState.DEFAULT;
                break;
            case ActionState.LOADINGVISUALS:
                _avDrivingState = AVDrivingState.DEFAULT;
                break;
            case ActionState.READY:
                GetComponent<Rigidbody>().isKinematic = false;
                _avDrivingState = AVDrivingState.STOPPED;
                break;
            case ActionState.DRIVE:
                _avDrivingState = AVDrivingState.DRIVING;
                UpdateWaypoint();
                break;
            case ActionState.QUESTIONS:
                _avDrivingState = AVDrivingState.STOPPED;
                break;
            case ActionState.POSTQUESTIONS:
                _avDrivingState = AVDrivingState.DEFAULT;
                break;
            case ActionState.RERUN:
                _avDrivingState = AVDrivingState.DEFAULT;
                break;
            default:
                _avDrivingState = AVDrivingState.DEFAULT;
                break;
        }
    }

    private void UpdateWaypoint() {
        if (NextWaypoint == null) {
            NextWaypoint = StartingWayPoint;
        }
        else {
            NextWaypoint = NextWaypoint.nextWayPoint;
        }

        newWayPointFound = true;
        newDataRead = false;


        if (NextWaypoint.yield) {
            _avDrivingState = AVDrivingState.YIELD;
            YieldTimer = NextWaypoint.minimumYieldTime;
        }
    }

// Update is called once per frame
    void FixedUpdate() {
#if DEBUGAUTONMOUSDRIVER && UNITY_EDITOR
        if (NextWaypoint != null) {
            Debug.DrawLine(transform.position, NextWaypoint.transform.position);
        }
#endif


        if (newWayPointFound &&
            newDataRead) {
            newWayPointFound = false;
        }

        switch (_avDrivingState) { //StateUpdate 
            case AVDrivingState.DRIVING:
                if (!NextWaypoint.isLastWaypoint) {
                    if ((NextWaypoint.transform.position - transform.position).magnitude <= NextWaypoint.ArrivalRange) {
                        UpdateWaypoint();
                    }
                }
                else {
                    _avDrivingState = AVDrivingState.STOPPED;
                }

                break;
            case AVDrivingState.YIELD:
                if (YieldTimer <= 0) {
                    if (!YieldBox.GetPlayerPresent()) {
                        Debug.Log("Going in Yield box");
                        _avDrivingState = AVDrivingState.DRIVING;
                    }
                }
                else {
                    YieldTimer -= Time.deltaTime;
                }

                break;
        }

        if (UsePythonBackend && IntersectionCenterPosition != null && (transform.position-IntersectionCenterPosition.position).magnitude<30) {
            DriveUsingAI();
        }
        else {
            DriveUsingWaypoints();
        }
    }

    private void DriveUsingAI() {
        Throttle = externData.Last()[1] * 0.9f + Throttle * 0.1f;
        var currentCenterlineOffset = _vehicleController.SplineCLCreator.GetClosestDistanceToSpline(transform.position);
        var steer = centerLinePID.Update(AverageLastSteer(1),
            currentCenterlineOffset, Time.deltaTime);
        //Steering = anglePID.Update(targetAngle, 0, Time.deltaTime);
        Steering = steer * 0.9f + Steering * 0.1f;
        Debug.Log(currentCenterlineOffset);
    }

    //average last steer inputs in externData for the past timeInPast seconds
    private float AverageLastSteer(float timeInPast) {
        return externData.Where(x => x[0] > Time.time - timeInPast).Select(x => x[2]).Average();
    }

    private void DriveUsingWaypoints() {
        switch (_avDrivingState)
        {
            case AVDrivingState.DEFAULT:
                Steering = 0;
                break;
            case AVDrivingState.STOPPED:
            case AVDrivingState.YIELD:
            case AVDrivingState.CRASH:
                Throttle = speedPID.Update(-3, _vehicleController.CurrentSpeed,
                    Time.fixedDeltaTime);
                break;
            case AVDrivingState.DRIVING:
                if (!AvoidBox.GetPlayerPresent()) {
                    Throttle = speedPID.Update(NextWaypoint.targetSpeed, _vehicleController.CurrentSpeed,
                        Time.fixedDeltaTime);
                }
                else {
                    Throttle = speedPID.Update(-3, _vehicleController.CurrentSpeed,
                        Time.fixedDeltaTime);
                }
                
                Steering = Mathf.Lerp(Steering, Vector3.SignedAngle(transform.forward,
                    (NextWaypoint.transform.position - transform.position), Vector3.up) / maxTurn, 0.25f);

                break;
        }
    }

    public float GetSteerInput() {
        return Mathf.Clamp(Steering, -1, 1);
        ;
    }

    public float GetAccelInput() {
        return Mathf.Clamp(Throttle, -1, 1);
    }

    public bool GetLeftIndicatorInput() {
        newDataRead = true;
        return NextWaypoint != null && NextWaypoint.IndicateLeft & newWayPointFound;
    }

    public bool GetRightIndicatorInput() {
        newDataRead = true;
        return NextWaypoint != null && NextWaypoint.IndicateRight & newWayPointFound;
    }

    public bool StopIndicating() {
        newDataRead = true;
        return NextWaypoint != null && NextWaypoint.StopIndicating & newWayPointFound;
    }


    public bool GetHornInput() {
        newDataRead = true;
        return NextWaypoint != null && NextWaypoint.Horn & newWayPointFound;
    }
}