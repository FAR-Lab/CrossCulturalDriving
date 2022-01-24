using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;


public class SeatCalibration : MonoBehaviour {
    public enum SearCalibrationState {
        NONE,
        LOAD,
        STARTCALIBRATING,
        CALIBRATING,
        FINISHED,
        READY,
        ERROR,
        DEFAULT
    }

    public OVRCustomSkeleton HandModelL;
    public OVRCustomSkeleton HandModelR;

    public Transform steeringWheelCenter;

    float yRotationCorection = 0;
    float _accumelatedYError = 0;

    SearCalibrationState callibrationState = 0;

    private Vector3 OriginalPosition;


    void Start() { OriginalPosition = transform.position; }


    void OnGUI() {
        GUIStyle gs = new GUIStyle();
        gs.fontSize = 30;
        gs.normal.textColor = Color.white;
        string displayString = "";
        switch (callibrationState) {
            case SearCalibrationState.LOAD:
                displayString = "Loaded!";
                gs.normal.textColor = Color.green;
                break;
            case SearCalibrationState.STARTCALIBRATING:
                displayString = "Starting Callibration!";
                gs.normal.textColor = Color.green;
                break;
            case SearCalibrationState.CALIBRATING:
                displayString = "Callibrating! HOLD STILL!" + (callibrationTimer).ToString("F1");
                gs.normal.textColor = Color.red;
                break;
            case SearCalibrationState.FINISHED:
                displayString = "Finished!";
                gs.normal.textColor = Color.green;
                break;
            case SearCalibrationState.READY:
                displayString = "ready";
                gs.normal.textColor = Color.green;
                break;
            case SearCalibrationState.ERROR: break;
            case SearCalibrationState.DEFAULT:
                displayString = "Not Done Yet!";
                gs.normal.textColor = Color.red;
                break;
            default:
                break;
        }

        GUI.Label(new Rect(610, 10, 600, 300), displayString, gs);
    }


    private Transform cam;
    private ParticipantInputCapture myPic;

    public void StartCalibration(Transform SteeringWheel, Transform camera, ParticipantInputCapture pic) {
        if (callibrationState != SearCalibrationState.CALIBRATING ||
            callibrationState != SearCalibrationState.STARTCALIBRATING) {
            steeringWheelCenter = SteeringWheel;
            cam = camera;
            myPic = pic;
            if (HandModelL == null || HandModelR == null) {
                foreach (var h in transform.GetComponentsInChildren<OVRCustomSkeleton>()) {
                    if (h.GetSkeletonType() == OVRSkeleton.SkeletonType.HandLeft) { HandModelL = h; }
                    else if (h.GetSkeletonType() == OVRSkeleton.SkeletonType.HandRight) { HandModelR = h; }
                }
            }

            callibrationState = SearCalibrationState.STARTCALIBRATING;
        }
    }

    private float callibrationTimer = 0;

    void Update() {
        if (cam == null ||
            steeringWheelCenter == null ||
            HandModelL == null ||
            HandModelR == null ||
            myPic == null) {
            if (callibrationState == SearCalibrationState.STARTCALIBRATING) {
                Debug.Log("calibrationScript is not Running something is missing");
            }

            return;
        }


        switch (callibrationState) {
            case SearCalibrationState.NONE: break;
            case SearCalibrationState.LOAD: break;
            case SearCalibrationState.STARTCALIBRATING:
                OVRPlugin.RecenterTrackingOrigin(OVRPlugin.RecenterFlags.Default);
                Quaternion rotation = Quaternion.FromToRotation(cam.forward, steeringWheelCenter.parent.forward);
                Debug.Log("rotation.eulerAngles.y"+Quaternion.Euler(0,rotation.eulerAngles.y, 0));
                
                myPic.SetNewRotationOffset(Quaternion.Euler(0,rotation.eulerAngles.y, 0));
                callibrationState= SearCalibrationState.CALIBRATING;
                callibrationTimer = 5f;
                break;
            case SearCalibrationState.CALIBRATING:

                if (HandModelL.IsDataHighConfidence && HandModelR.IsDataHighConfidence) {
                    // if this does not work we might need to look further for getting the right bone
                    Vector3 A = HandModelL.Bones[9].Transform.position;//HandModelL.transform.position;
                    Vector3 B = HandModelR.Bones[9].Transform.position;//HandModelR.transform.position;
                    Vector3 AtoB = B - A;
                   
                    Vector3 transformDifference = (A + (AtoB * 0.5f)) - steeringWheelCenter.position;
                 
                    myPic.SetNewPositionOffset(-transformDifference);
                    Debug.Log("transformDifference"+(-transformDifference).ToString());
                }

                if (callibrationTimer > 0) { callibrationTimer -= Time.deltaTime; }
                else { callibrationState = SearCalibrationState.FINISHED; }

                break;
            case SearCalibrationState.FINISHED:
                myPic.FinishedCalibration();
                callibrationState = SearCalibrationState.READY;
                break;
            case SearCalibrationState.READY: break;
            case SearCalibrationState.ERROR: break;
            case SearCalibrationState.DEFAULT: break;
            default: throw new ArgumentOutOfRangeException();
        }
    }
}