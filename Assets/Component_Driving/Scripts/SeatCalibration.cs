using System;
using System.Collections;
using UnityEngine;

public class SeatCalibration : MonoBehaviour {
    public enum SeatCalibrationState {
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

    private SeatCalibrationState calibrationState = SeatCalibrationState.NONE;
    private float calibrationTimer;


    private Transform cam;
    private CalibrationTimerDisplay m_calibDisplay;
    private VR_Participant myPic;

    private Vector3 OriginalPosition;
    private int ReTryCount;


    private void Start() {
        OriginalPosition = transform.position;
    }

    private void Update() {
        if (cam == null ||
            steeringWheelCenter == null ||
            HandModelL == null ||
            HandModelR == null ||
            myPic == null) {
            if (calibrationState == SeatCalibrationState.STARTCALIBRATING)
                Debug.Log("calibrationScript is not Running something is missing");

            return;
        }


        switch (calibrationState) {
            case SeatCalibrationState.NONE: break;

            case SeatCalibrationState.STARTCALIBRATING:
                //TODO switch from OVR to openXR
                //OVRPlugin.RecenterTrackingOrigin(OVRPlugin.RecenterFlags.Default);

                var rotation = Quaternion.FromToRotation(cam.forward, steeringWheelCenter.parent.forward);
                Debug.DrawRay(cam.position, cam.forward * 10, Color.red, 10);
                Debug.DrawRay(steeringWheelCenter.position, steeringWheelCenter.parent.forward * 10, Color.blue, 10);

                Debug.Log($"rotation.eulerAngles.y {rotation.eulerAngles.y}");
                myPic.SetNewRotationOffset(Quaternion.Euler(0, rotation.eulerAngles.y, 0));
                calibrationState = SeatCalibrationState.CALIBRATING;
                calibrationTimer = 5f;
                break;
            case SeatCalibrationState.CALIBRATING:
                //TODO switch from OVR to openXR

                if (true) {
                    // if (HandModelL.IsDataHighConfidence && HandModelR.IsDataHighConfidence) { //TODO switch from OVR to openXR

                    // if this does not work we might need to look further for getting the right bone
                    var A = HandModelL.position;
                    var B = HandModelR.position;
                    var AtoB = B - A;
                    var midPoint = A + AtoB * 0.5f;
                    var transformDifference = midPoint - steeringWheelCenter.position;
                    Debug.DrawRay(midPoint, transformDifference);
                    if (transformDifference.magnitude > 100) {
                        Debug.Log(transformDifference.magnitude);
                        calibrationState = SeatCalibrationState.ERROR;
                    }

                    myPic.SetNewPositionOffset(-transformDifference);
//                    Debug.Log("transformDifference" + (-transformDifference).ToString());
                    m_calibDisplay.UpdateMessage(calibrationTimer.ToString("F1"));
                }

                if (calibrationTimer > 0)
                    calibrationTimer -= Time.deltaTime;
                else
                    calibrationState = SeatCalibrationState.FINISHED;

                break;
            case SeatCalibrationState.FINISHED:
                m_calibDisplay.StopDisplay();
                myPic.FinishedCalibration(steeringWheelCenter.parent);
                calibrationState = SeatCalibrationState.READY;
                break;
            case SeatCalibrationState.READY: break;
            case SeatCalibrationState.ERROR:
                if (ReTryCount > 10) {
                    if (!myPic.DeleteCallibrationFile())
                        Debug.LogWarning(
                            "Could not delete calibration file. The data in that file is probably corrupt. Please consider removing the file manually.");
                    Debug.LogError("Had 10 retries calibrating the play. Did not work. Quitting.");
                    Application.Quit();
                }
                else {
                    Debug.Log("Encountered a Calibration Error. Resetting Offsets and trying again try: " +
                              ReTryCount);

                    myPic.SetNewRotationOffset(Quaternion.identity);
                    myPic.SetNewPositionOffset(Vector3.zero);
                    ReTryCount++;
                    calibrationState = SeatCalibrationState.STARTCALIBRATING;
                }

                break;
            case SeatCalibrationState.DEFAULT: break;
            default: throw new ArgumentOutOfRangeException();
        }
    }

#if UNITY_EDITOR
    private void OnGUI() {
        /*
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
        */
    }
#endif
    public void StartCalibration(Transform SteeringWheel, Transform camera, VR_Participant pic,
        CalibrationTimerDisplay mCalibDisplay) {
        Debug.Log("Starting Calibration");
        if (calibrationState != SeatCalibrationState.CALIBRATING ||
            calibrationState != SeatCalibrationState.STARTCALIBRATING) {
            steeringWheelCenter = SteeringWheel;
            cam = camera;
            myPic = pic;
            if (HandModelL == null || HandModelR == null) {
                HandModelL = transform.Find("Camera Offset/Left Hand Tracking/L_Wrist/L_Palm");
                HandModelR = transform.Find("Camera Offset/Right Hand Tracking/R_Wrist/R_Palm");
            }

            Debug.Log(HandModelL.name);

            if (mCalibDisplay != null) m_calibDisplay = mCalibDisplay;

            StartCoroutine(DelayedCalibrationStart());
        }
    }

    private IEnumerator DelayedCalibrationStart() {
        m_calibDisplay.StartDisplay();
        m_calibDisplay.UpdateMessage("Hold still!");
        yield return new WaitForSeconds(2f);
        calibrationState = SeatCalibrationState.STARTCALIBRATING;
    }
}