using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// Leap.Unity; Should be something something OVR...
using UnityEngine.XR;



public class seatCallibration : MonoBehaviour
{
  //  public HandModelBase HandModelL; /// moved to what ever we used to keep track of hands with the oculus quest 2

   // public HandModelBase HandModelR;

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

        //if(SceneStateManager.Instance.CallibratedValuesAvalible){
        //    transform.localPosition = SceneStateManager.Instance.CallibratedLocalCameraPosition;
       //     transform.localRotation = SceneStateManager.Instance.CallibratedLocalCameraRotation;
       //     callibrationState = 5;
       // }



    }
    public void findHands()
    {
      //  XRHeightOffset x = transform.GetComponentInChildren<XRHeightOffset>();
       // if (x == null) return;
        //HandModelManager h = x.GetComponentInChildren<HandModelManager>();
      //  if (h == null) return;
       // RiggedHand[] array = h.GetComponentsInChildren<RiggedHand>();
       // if (array.Length<=0) return;
      //  reCallibrate();
       // foreach (RiggedHand r in array)
        //{
       //     if (r.Handedness == Chirality.Left)
        //    {
       //         HandModelL = r as HandModelBase;
       //     }
       //     else if (r.Handedness == Chirality.Right)
       //     {
       //         HandModelR = r as HandModelBase;
     //       }
       // }

    }

    public void reCallibrate()
    {
        if (true)//transform.parent.GetComponent<VehicleInputControllerNetworked>().isLocalPlayer
        {
            if (!callibrating)
            {
                startCallibrating = true;
            }
        }
    }
    void startCallibration() { 
      //  if (HandModelL != null && HandModelR != null)
      //  {
            
            if (!callibrating)
            {
                startCallibrating = false;
                callibrationTimer = 0;
                callibrating = true;
                callibrationState = 0;
                runCorrection = false;
            }
     //   }

    }
    public bool isPartOfLocalPlayer() {
        return true;// transform.parent.GetComponent<VehicleInputControllerNetworked>().isLocalPlayer;
    }
    void OnGUI()
    {
        GUIStyle gs = new GUIStyle();
        gs.fontSize = 30;
        gs.normal.textColor = Color.white;
        string displayString;
        switch (callibrationState) {
            default:
            case 0:
                displayString = "NotRunYet";
                gs.normal.textColor = Color.red;
                break;
            case 1:
                displayString = "First Step Done";
                gs.normal.textColor = Color.white;
                break;
            case 2:
                displayString = "Callibrating! HOLD STILL!"+ (callibrationTimer).ToString("F1");
                gs.normal.textColor = Color.red;
                break;
            case 3:
            case 4:
                displayString = "Callibration Done!";
                gs.normal.textColor = Color.black;
                break;
            case 5:
                displayString = "Callibration Loaded!";
                gs.normal.textColor = Color.black;
                break;

        }

        GUI.Label(new Rect(610, 10, 600, 300), displayString, gs);

    }

    // Update is called once per frame
    void Update()
    {
        if (true) {
           // transform.parent.GetComponent<VehicleInputControllerNetworked>().isLocalPlayer
            if (startCallibrating) {
                startCallibration();
                Debug.Log("Waiting to start callibrating");
            }
            if (handTrackin) {
               /// if (HandModelL == null || HandModelR == null) {
               //     findHands();
               // }
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

                if (callibrationState == 1)
                {
                    if (setUp)
                    {
                        callibrationTimer = 0;
                    }
                    transform.position = OriginalPosition;

                    // InputTracking.Recenter(); //MoveTO2020
                    //   XRHeightOffset x = transform.GetComponentInChildren<XRHeightOffset>(); //ToDO maybe get the normal transform and not XR height offset //MoveTO2020
                    //  Transform cam = x.transform.GetComponentInChildren<Camera>().transform;//MoveTO2020


                    // if (x != null) {
                    //Quaternion rotation = Quaternion.(.eulerAngles, headPose.parent.transform.forward); ;
                   // Quaternion rotation = Quaternion.FromToRotation(cam.forward, transform.parent.forward);//MoveTO2020
                    // Debug.Log("XR reported rotation"+InputTracking.GetLocalRotation(XRNode.Head).eulerAngles);
                    //Debug.Log("Self reported" + cam.rotation.eulerAngles);

                  //  Debug.Log("Correction Rotation: " + rotation.eulerAngles.y);
                  //  transform.RotateAround(cam.position, transform.up, rotation.eulerAngles.y); //MoveTO2020
                    //transform.Rotate(new Vector3(0, rotation.eulerAngles.y, 0));
                    //headPose.parent.transform.rotation = transform.parent.rotation;
                    // transform.rotation = transform.parent.rotation;
                    // }

                }
                else if (callibrationState == 2)
                {
                    if (handTrackin)
                    {
                        if (setUp)
                        {
                            callibrationTimer = 10;

                        }
                        if (true) //HandModelL.IsTracked && HandModelR.IsTracked //MoveTo2020
                        {
                            Vector3 A = Vector3.zero;//  HandModelL.GetLeapHand().PalmPosition.ToVector3();  //MoveTo2020
                            Vector3 B = Vector3.zero;  // HandModelR.GetLeapHand().PalmPosition.ToVector3(); //MoveTo2020
                            Vector3 AtoB = B - A;
                            Debug.DrawLine(A, B);
                            if (steeringWheelCenter != null)
                            {
                                Vector3 transformDifference = (A + (AtoB * 0.5f)) - handCenterPosition;
                                Debug.DrawLine(transform.position, handCenterPosition);
                                offset = transformDifference;
                                transform.position -= transformDifference;

                                //  Quaternion rot = Quaternion.FromToRotation(AtoB, -steeringWheelCenter.right);
                                // rotOld = rot;
                                //Debug.Log(rot);
                                // transform.RotateAround(handCenterPosition, Vector3.up, rot.eulerAngles.y);

                            }

                        }
                        else
                        {
                            callibrationTimer += Time.deltaTime;
                        }
                    }


                }
                else if (callibrationState == 3)
                {
                  //  SceneStateManager.Instance.CallibratedValuesAvalible = true;
                   // SceneStateManager.Instance.CallibratedLocalCameraPosition = transform.localPosition;
                  //  SceneStateManager.Instance.CallibratedLocalCameraRotation = transform.localRotation;
                    
                }
                else if (callibrationState == 4)
                {
                    callibrating = false;
                    callibrationTimer = 0;
                    callibrationState = 4;

                }
                else
                {
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