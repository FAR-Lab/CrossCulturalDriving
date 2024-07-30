#define DEBUGAUTONMOUSDRIVER

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;


public class AutonomousVehicleDriver : MonoBehaviour {
    public enum AVDrivingState {
        DEFAULT,
        STOPPED,
        DRIVING,
        YIELD,
        CRASH
    }

    private AVDrivingState _avDrivingState;
    public bool UsePythonBackend;
    public bool Running = false;
    public WayPoint StartingWayPoint;
    private WayPoint NextWaypoint = null;
    private bool newWayPointFound;

    private bool newDataRead;

    private float Steering;
    private float Throttle;


    private VehicleController _vehicleController;

    private const float maxTurn = 70;


    private PID speedPID = new PID(0.2f, 0.005f, 0.005f);
    private float YieldTimer;

    private TriggerPlayerTracker AvoidBox;

    private TriggerPlayerTracker YieldBox;

    // Start is called before the first frame update
    private Vector3 Position;
    private Quaternion Orientation;

    public Transform IntersectionCenterPosition;
    private VehicleController otherCar;
    private NetworkVehicleController otherCarNet;
    void Start() {
        if (GetComponent<NetworkVehicleController>().IsServer) {
            _vehicleController = GetComponent<VehicleController>();
            _avDrivingState = AVDrivingState.DEFAULT;
            ConnectionAndSpawning.Singleton.ServerStateChange += InterntalStateUpdate;

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
    }

    private bool assignOtherCar() {
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
    private IEnumerator PrepareToStart() {

        yield return new WaitUntil(()=> assignOtherCar());
        
        IntersectionCenterPosition = FindObjectOfType<IntersectionCenter>().transform;
        var s = gameObject.AddComponent<UdpSocket>();
        
        s.GotNewAiData += NewPythonData;
        Running = true;
        
        StartCoroutine(SendArtificalData(s, otherCar, otherCarNet)); 
    }

    private float[] m_outdata = new float[7];
    private void OnGUI() {
        string s = string.Concat(m_outdata.Select(x => x.ToString("F2")+" ,")) + $" throtle{ExternThrottle}";
        GUI.Label(new Rect(0,0,400,20),s );
        
    }
    private IEnumerator SendArtificalData(UdpSocket udpSocket, VehicleController l_otherCar, NetworkVehicleController l_otherCarNet) {
        Rigidbody m_rigidBody = _vehicleController.GetComponent<Rigidbody>();
        Rigidbody o_rigidBody = l_otherCar.GetComponent<Rigidbody>();


        float[] outdata = new float[7];
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
                    Debug.Log("Left turn signal on");
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
            outdata[0] = ApproachRate; // ["ApproachRate", 
            outdata[1] = Rel_Pos_Magnitude; //"Rel_Pos_Magnitude", 
            outdata[2] = l_otherCar.steerInput; //"SteerB", 
            outdata[3] = (m_rigidBody.position-IntersectionCenterPosition.position).magnitude; //"A_Head_Center_Distance",
            outdata[4] = (o_rigidBody.position-IntersectionCenterPosition.position).magnitude; // "B_Head_Center_Distance",
            outdata[5] = o_rigidBody.velocity.magnitude; // "Filtered_B_Head_Velocity_Total",
            outdata[6] = _vehicleController.WheelFL.steerAngle; //    A Turn
            outdata[7] = b_indicator; //    B Indicator
            outdata[8] = l_otherCar.SplineCLCreator.GetClosestDistanceToSpline(o_rigidBody.position); //    Centerline Offset_B
            outdata[9] = RelativeRotation; // "RelativeRotation"]
            udpSocket.SendDataToPython(outdata);
            m_outdata = outdata;
            yield return new WaitForSeconds(1f / 18f);
        }
    }

    float ExternThrottle;
    float ExternCenterlineOffset;
    
    private void NewPythonData(float[] data) {
        if (data.Length > 1)
            ExternThrottle = data[0];
            ExternCenterlineOffset = data[1];
    }

    private void OnDestroy() {
        if (GetComponent<NetworkVehicleController>().IsServer &&
            ConnectionAndSpawning.Singleton != null) {
            ConnectionAndSpawning.Singleton.ServerStateChange -= InterntalStateUpdate;
        }
    }

    private void InterntalStateUpdate(ActionState state) {
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
    void Update() {
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
            default:
                break;
        }

        if (UsePythonBackend && IntersectionCenterPosition!=null && (transform.position-IntersectionCenterPosition.position).magnitude<30) {
            Throttle = ExternThrottle * 0.9f + Throttle * 0.1f;
            
        }
        else {
            switch (_avDrivingState) //Throttle Update
            {
                case AVDrivingState.DEFAULT:
                case AVDrivingState.STOPPED:
                case AVDrivingState.YIELD:
                    Throttle = speedPID.Update(-3, _vehicleController.CurrentSpeed,
                        Time.fixedDeltaTime);
                    break;
                case AVDrivingState.CRASH:
                    Throttle = speedPID.Update(-3, _vehicleController.CurrentSpeed,
                        Time.fixedDeltaTime);
                    break;
                case AVDrivingState.DRIVING:
                    if (!AvoidBox.GetPlayerPresent()) {
                        Throttle = speedPID.Update(NextWaypoint.targetSpeed, _vehicleController.CurrentSpeed,
                            Time.fixedDeltaTime);
                        //Debug.Log("No Player in Avoid box");
                    }
                    else {
                        Throttle = speedPID.Update(-3, _vehicleController.CurrentSpeed,
                            Time.fixedDeltaTime);
                        //Debug.Log("Player  in Avoid box STOPPING");
                    }

                    break;
                default:
                    break;
            }
        }

        switch (_avDrivingState) //SteeringUpdate Update
        {
            case AVDrivingState.CRASH:
            case AVDrivingState.DEFAULT:
                Steering = 0;
                break;
            case AVDrivingState.STOPPED:
                break;
            case AVDrivingState.DRIVING:
                Steering = Mathf.Lerp(Steering, Vector3.SignedAngle(transform.forward,
                    (NextWaypoint.transform.position - transform.position), Vector3.up) / maxTurn, 0.25f);
                break;
            case AVDrivingState.YIELD:
                break;
            default:
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