using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;

public class Calibration : MonoBehaviour
{
    public Transform steeringWheelT;
    public Transform OVRRig;
    public Transform leftHandAnchor;
    public Transform rightHandAnchor;

    private Vector3 steeringWheelLeft;
    private Vector3 steeringWheelRight;
    private Vector3 leftDifference;
    private Vector3 rightDifference;
    private InputAction calibrateAction;
    private bool calculated = false;
    private bool calibrated = false;

    // Start is called before the first frame update
    void Start()
    {
        Vector3 leftOffset = new Vector3(0.01071f, 0.012f, 0.0088f);
        Vector3 rightOffset = new Vector3(0.01071f, 0.012f, -0.0088f);
        steeringWheelLeft = steeringWheelT.position + leftOffset;
        steeringWheelRight = steeringWheelT.position + rightOffset;
        calibrateAction = new InputAction("Calibrate");
        calibrateAction.AddBinding("<Joystick>/button5");
        calibrateAction.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        if (calibrateAction.triggered) {
            calculateDifferenceVectors();           
        }

        if (!calibrated && calculated) {
            Vector3 average = ((leftDifference + rightDifference) / 2.0f);
            Vector3 headsetOffset = new Vector3(average.x, -0.032f, average.z + 0.025f);
            OVRRig.position += headsetOffset;
            calibrated = true;
        }
    }

    private void calculateDifferenceVectors() {
        leftDifference = steeringWheelLeft - leftHandAnchor.position;
        rightDifference = steeringWheelRight - rightHandAnchor.position;
        calculated = true;
    }
}
