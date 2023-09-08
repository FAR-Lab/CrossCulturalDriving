using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;


public class SeatCalibration : MonoBehaviour {
    public enum SearCalibrationState {
        NONE,
        STARTCALIBRATING,
        CALIBRATING,
        FINISHED,
        READY,
        ERROR,
        DEFAULT,
        FAILED
    }

    public Transform HandModelL;
    public Transform HandModelR;

    public Transform steeringWheelCenter;

    SearCalibrationState callibrationState = SearCalibrationState.NONE;

    private Vector3 OriginalPosition;


    void Start() { OriginalPosition = transform.position; }

#if UNITY_EDITOR
    void OnGUI() {
        GUIStyle gs = new GUIStyle();
        gs.fontSize = 30;
        gs.normal.textColor = Color.white;
        string displayString = "";
        switch (callibrationState) {
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
#endif


    private Transform cam;
    private VR_Participant myPic;

    public void StartCalibration(Transform SteeringWheel, Transform camera, VR_Participant pic) {
        Debug.Log("Starting Calibration");
        if (callibrationState != SearCalibrationState.CALIBRATING ||
            callibrationState != SearCalibrationState.STARTCALIBRATING) {
            steeringWheelCenter = SteeringWheel;
            cam = camera;
            myPic = pic;
            if (HandModelL == null || HandModelR == null){
                HandModelL= transform.Find("Camera Offset/Left Hand Tracking/L_Wrist/L_Palm");
                HandModelR = transform.Find("Camera Offset/Right Hand Tracking/R_Wrist/R_Palm");
            }

            Debug.Log(HandModelL.name);
            callibrationState = SearCalibrationState.STARTCALIBRATING;
        }
    }

    private float callibrationTimer = 0;
    int ReTryCount = 0;

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
          
            case SearCalibrationState.STARTCALIBRATING:
                //TODO switch from OVR to openXR
                //OVRPlugin.RecenterTrackingOrigin(OVRPlugin.RecenterFlags.Default);
                
                Quaternion rotation = Quaternion.FromToRotation(cam.forward, steeringWheelCenter.parent.forward);
                Debug.DrawRay(cam.position,cam.forward*10,Color.red,10);
                Debug.DrawRay(steeringWheelCenter.position,steeringWheelCenter.parent.forward*10,Color.blue,10);
                
                Debug.Log($"rotation.eulerAngles.y { rotation.eulerAngles.y}");
                myPic.SetFollowMode(true, true);
                myPic.SetNewRotationOffset(Quaternion.Euler(0, rotation.eulerAngles.y, 0));
                callibrationState = SearCalibrationState.CALIBRATING;
                callibrationTimer = 5f;
                break;
            case SearCalibrationState.CALIBRATING:
                //TODO switch from OVR to openXR
                
               if(true){// if (HandModelL.IsDataHighConfidence && HandModelR.IsDataHighConfidence) { //TODO switch from OVR to openXR
                    
                    // if this does not work we might need to look further for getting the right bone
                    Vector3 A = HandModelL.position; 
                    Vector3 B = HandModelR.position;
                    Vector3 AtoB = B - A;
                    Vector3 midPoint= (A + (AtoB * 0.5f));
                    Vector3 transformDifference = midPoint - steeringWheelCenter.position;
                    Debug.DrawRay(midPoint,transformDifference);
                    if (transformDifference.magnitude > 100) {
                        Debug.Log(transformDifference.magnitude);
                        callibrationState = SearCalibrationState.ERROR;
                    }

                    myPic.SetNewPositionOffset(-transformDifference);
                    Debug.Log("transformDifference" + (-transformDifference).ToString());
                }

                if (callibrationTimer > 0) { callibrationTimer -= Time.deltaTime; }
                else { callibrationState = SearCalibrationState.FINISHED; }

                break;
            case SearCalibrationState.FINISHED:
                myPic.FinishedCalibration();
                callibrationState = SearCalibrationState.READY;
                break;
            case SearCalibrationState.READY: break;
            case SearCalibrationState.ERROR:
                if (ReTryCount > 10) {
                    if (!myPic.DeleteCallibrationFile()) {
                        Debug.LogWarning("Could not delete calibration file. The data in that file is probably corrupt. Please consider removing the file manually.");
                    }
                    Debug.LogError("Had 10 retries calibrating the play. Did not work. Quitting.");
                    Application.Quit();
                   
                }
                else {
                    Debug.Log("Encountered a Calibration Error. Resetting Offsets and trying again try: " +
                              ReTryCount.ToString());

                    myPic.SetNewRotationOffset(Quaternion.identity);
                    myPic.SetNewPositionOffset(Vector3.zero);
                    ReTryCount++;
                    callibrationState = SearCalibrationState.STARTCALIBRATING;
                }

                break;
            case SearCalibrationState.DEFAULT: break;
            default: throw new ArgumentOutOfRangeException();
        }
    }
}