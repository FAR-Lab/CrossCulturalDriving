using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using UnityEngine.XR;
using RVP;


public class seatCallibration : MonoBehaviour
{
    public HandModelBase HandModelL;
    public HandModelBase HandModelR;

    public Transform steeringWheelCenter;
    public Transform passangerCenter;


    public bool handTrackin = true;
    
    public bool forPassenger = true;


    float yRotationCorrectio = 0;
    float accumelatedYError = 0;
    bool runCorrection;
    float prevCorrectRotation;
    Quaternion prevRot;
    float rotationTimer = 0;

    public float callibrationTimer = 2;
    float refreshTimer = 0;
    int callibrationState = 0;
     bool callibrating;
    public bool startCallibrating = false;
    
    public Transform headPose;
   

    // Use this for initialization
    Vector3 handCenterPosition;
    private Vector3 OriginalPosition;
    public Vector3 offset;
    public Quaternion rotOld;
    public Quaternion driftCorrection;

    Vector3 gpsOffsest = new Vector3(0, 0, 0);
    void Start()
    {
        
        OriginalPosition = transform.position;
       
    }
    public void findHands()
    {
        XRHeightOffset x = transform.GetComponentInChildren<XRHeightOffset>();
        if (x == null) return;
        HandModelManager h = x.GetComponentInChildren<HandModelManager>();
        if (h == null) return;
        RiggedHand[] array = h.GetComponentsInChildren<RiggedHand>();
        if (array.Length<=0) return;
        reCallibrate();
        foreach (RiggedHand r in array)
        {
            if (r.Handedness == Chirality.Left)
            {
                HandModelL = r as HandModelBase;
            }
            else if (r.Handedness == Chirality.Right)
            {
                HandModelR = r as HandModelBase;
            }
        }

    }

    public void reCallibrate()
    {
        if (transform.parent.GetComponent<VehicleInputControllerNetworked>().isLocalPlayer)
        {
            if (!callibrating)
            {
                startCallibrating = true;
            }
        }
    }
    void startCallibration() { 
        if (HandModelL != null && HandModelR != null)
        {
            
            if (!callibrating)
            {
                startCallibrating = false;
                callibrationTimer = 0;
                callibrating = true;
                callibrationState = 0;
                runCorrection = false;
            }
        }

    }
    public bool isPartOfLocalPlayer() {
        return transform.parent.GetComponent<VehicleInputControllerNetworked>().isLocalPlayer;
    }
    void OnGUI()
    {
        GUIStyle gs = new GUIStyle();
        gs.fontSize = 30;
        GUI.Label(new Rect(610, 10, 600, 300), (accumelatedYError).ToString("F4") + "y Error&Diffy" + (yRotationCorrectio).ToString("F4"), gs);

    }

    // Update is called once per frame
    void Update()
    {
        if (transform.parent.GetComponent<VehicleInputControllerNetworked>().isLocalPlayer) {
            if (startCallibrating) {
                startCallibration();
                Debug.Log("Waiting to start callibrating");
            }
            if (handTrackin) {
                if (HandModelL == null || HandModelR == null) {
                    findHands();
                }
            }
            if (callibrating) {
                bool setUp = false;
                if (callibrationTimer > 0) {
                    callibrationTimer -= Time.deltaTime;
                } else {
                    callibrationState++;
                    setUp = true;
                    Debug.Log(callibrationState);
                }

                if (forPassenger) {
                    handCenterPosition = passangerCenter.position;
                } else {
                    handCenterPosition = steeringWheelCenter.position;

                }

                if (callibrationState == 1) {
                    if (setUp) {
                        callibrationTimer = 0;
                    }
                    transform.position = OriginalPosition;
                    InputTracking.Recenter();
                    XRHeightOffset x = transform.GetComponentInChildren<XRHeightOffset>(); //ToDO maybe get the normal transform and not XR height offset
                    Transform cam = x.transform.GetComponentInChildren<Camera>().transform;
                    // if (x != null) {
                    //Quaternion rotation = Quaternion.(.eulerAngles, headPose.parent.transform.forward); ;
                    Quaternion rotation = Quaternion.FromToRotation(cam.forward, transform.parent.forward);
                   // Debug.Log("XR reported rotation"+InputTracking.GetLocalRotation(XRNode.Head).eulerAngles);
                    //Debug.Log("Self reported" + cam.rotation.eulerAngles);

                    Debug.Log("Correction Rotation: " + rotation.eulerAngles.y);
                    transform.RotateAround(cam.position, transform.up, rotation.eulerAngles.y);
                        //transform.Rotate(new Vector3(0, rotation.eulerAngles.y, 0));
                        //headPose.parent.transform.rotation = transform.parent.rotation;
                        // transform.rotation = transform.parent.rotation;
                        // }

                } else if (callibrationState == 2) {
                    if (handTrackin) {
                        if (setUp) {
                            callibrationTimer = 10;

                        }
                        if (HandModelL.IsTracked && HandModelR.IsTracked) {
                            Vector3 A = HandModelL.GetLeapHand().PalmPosition.ToVector3();
                            Vector3 B = HandModelR.GetLeapHand().PalmPosition.ToVector3();
                            Vector3 AtoB = B - A;
                            Debug.DrawLine(A, B);
                            if (steeringWheelCenter != null) {
                                Vector3 transformDifference = ( A + ( AtoB * 0.5f ) ) - handCenterPosition;
                                Debug.DrawLine(transform.position, handCenterPosition);
                                offset = transformDifference;
                                transform.position -= transformDifference;

                                //  Quaternion rot = Quaternion.FromToRotation(AtoB, -steeringWheelCenter.right);
                                // rotOld = rot;
                                //Debug.Log(rot);
                                // transform.RotateAround(handCenterPosition, Vector3.up, rot.eulerAngles.y);

                            }

                        } else {
                            callibrationTimer += Time.deltaTime;
                        }
                    }


                } else {
                    callibrationTimer = 0;
                    callibrating = false;
                    callibrationState = 0;
                }
            } else {
                //  driftCorrection = initalARRotation * Quaternion.Inverse(myReceiver.marker * Quaternion.Inverse(headPose.rotation));\
                ///Adjusing the rotation over time isdifficult with markers as there is a long dela. It would be better to find adifferent solution.
                ///One approache we could explore is too look at the actual headset motion and have based on that two modes.
                ///either we are measuring the drifft that happend
                ///or
                ///we are corrrecting for the drifft all based on the speed of the motion from the headset. 

            }
        }
    }
}