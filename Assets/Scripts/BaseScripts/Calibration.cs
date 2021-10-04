using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;

public class Calibration : MonoBehaviour
{
    public Transform steeringWheelCenter;
    public Transform OVRRig;
    public Transform leftHandPalm;
    public Transform rightHandPalm;
    public Transform steeringWheelLeft;
    public Transform steeringWheelRight;
    public Text instructionText;

    private Vector3 leftDifference;
    private Vector3 rightDifference;
    private InputAction calibrateAction;
    private InputAction leftCalibrateAction;
    private bool calibrated = false;
    private Vector3 leftToRight;
    private Vector3 right;
    private ConfigFileTester confTester;
    private InputAction exitCalibration;

    void Start()
    {
        calibrateAction = new InputAction("Calibrate");
        calibrateAction.AddBinding("<Joystick>/button5");
        calibrateAction.Enable();
        leftCalibrateAction = new InputAction("Calibrate");
        leftCalibrateAction.AddBinding("<Joystick>/button5");
        leftCalibrateAction.Enable();
        exitCalibration = new InputAction("Exit");
        exitCalibration.AddBinding("<Joystick>/trigger");
        exitCalibration.AddBinding("<Joystick>/button2");
        exitCalibration.AddBinding("<Joystick>/button3");
        exitCalibration.AddBinding("<Joystick>/button4");
        exitCalibration.AddBinding("<Joystick>/button7");
        exitCalibration.AddBinding("<Joystick>/button8");
        exitCalibration.AddBinding("<Joystick>/button9");
        exitCalibration.AddBinding("<Joystick>/button10");
        exitCalibration.AddBinding("<Joystick>/button11");
        exitCalibration.AddBinding("<Joystick>/button12");
        exitCalibration.AddBinding("<Joystick>/button20");
        exitCalibration.AddBinding("<Joystick>/button21");
        exitCalibration.AddBinding("<Joystick>/button22");
        exitCalibration.AddBinding("<Joystick>/button23");
        exitCalibration.AddBinding("<Joystick>/button24");
        exitCalibration.AddBinding("<Joystick>/hat/down");
        exitCalibration.AddBinding("<Joystick>/hat/up");
        exitCalibration.AddBinding("<Joystick>/hat/left");
        exitCalibration.AddBinding("<Joystick>/hat/right");
        exitCalibration.Enable();
        confTester = GetComponent<ConfigFileTester>();
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.DrawLine(leftHandPalm.position, steeringWheelLeft.position, Color.green);
        //Debug.DrawLine(rightHandPalm.position, steeringWheelRight.position, Color.green);
        leftToRight = rightHandPalm.position - leftHandPalm.position;
        right = -steeringWheelCenter.forward;

        if (calibrateAction.ReadValue<float>() != 0 && leftCalibrateAction.ReadValue<float>() != 0 && !calibrated)
        {
            OVRRig.RotateAround(steeringWheelCenter.position, transform.up, calculateAngle());
            calculateDifferenceVectors();
            Vector3 average = ((leftDifference + rightDifference) / 2.0f);
            OVRRig.position += average;
            calibrated = true;
            instructionText.text = "Press any button on the steering wheel besides the left or right turn signals";
        }

        if (exitCalibration.triggered && !confTester.StoringComplete)
        {
            confTester.store = true;
            instructionText.text = "Press the same button again and wait for the scenario to begin";

        }
        else if(exitCalibration.triggered && confTester.StoringComplete)
        {

            SceneManager.LoadScene("ScenarioSelector");
        }
    }

    private void calculateDifferenceVectors()
    {
        leftDifference = steeringWheelLeft.position - leftHandPalm.position;
        rightDifference = steeringWheelRight.position - rightHandPalm.position;
    }
    

    private float calculateAngle()
    {
        Vector3 projection = new Vector3(leftToRight.x, 0f, leftToRight.z);
        return Vector3.SignedAngle(projection, right, Vector3.up);
    }

    private void OnDestroy()
    {
        instructionText.text = "Place your hands at 9 and 3 on the steering wheel, then pull on the left and right turn signalers located just behind the steering wheel.";
    }
}
