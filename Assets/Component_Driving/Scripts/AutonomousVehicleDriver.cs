#define DEBUGAUTONMOUSDRIVER

using System;
using System.Collections;
using System.Collections.Generic;
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
    void Start() {
        _vehicleController = GetComponent<VehicleController>();
        _avDrivingState = AVDrivingState.DEFAULT;
        ConnectionAndSpawning.Singleton.ServerStateChange += InterntalStateUpdate;

        AvoidBox = transform.Find("AvoidBox").GetComponent<TriggerPlayerTracker>();
        YieldBox = transform.Find("YieldBox").GetComponent<TriggerPlayerTracker>();
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
            NextWaypoint=StartingWayPoint;
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
                    Debug.Log("No Player in Avoid box");
                }
                else {
                    Throttle = speedPID.Update(-3, _vehicleController.CurrentSpeed,
                        Time.fixedDeltaTime);
                    Debug.Log("Player  in Avoid box STOPPING");
                }

                break;
            default:
                break;
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
        return NextWaypoint != null ? NextWaypoint.IndicateLeft & newWayPointFound : false;
    }

    public bool GetRightIndicatorInput() {
        newDataRead = true;
        return NextWaypoint != null ? NextWaypoint.IndicateRight & newWayPointFound : false;
    }

    public bool StopIndicating() {
        newDataRead = true;
        return NextWaypoint != null ? NextWaypoint.StopIndicating & newWayPointFound : false;
    }


    public bool GetHornInput() {
        newDataRead = true;
        return NextWaypoint != null ? NextWaypoint.Horn & newWayPointFound : false;
    }
}