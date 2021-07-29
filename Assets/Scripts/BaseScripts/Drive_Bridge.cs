using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Drive_Bridge : MonoBehaviour {
    public WheelCollider frontLeftWC, frontRightWC;
    public WheelCollider backLeftWC, backRightWC;

    public Transform frontLeftT, frontRightT;
    public Transform backLeftT, backRightT;

    public Transform steeringWheelT;

    public Material tailLight;
    public Material frontLeftLamp;
    public Material frontRightLamp;
    public Material backLeftLamp;
    public Material backRightLamp;

    public AudioSource[] soundEffects;

    public Text instructionText;

    [HideInInspector]
    public float speedParameter;
    [HideInInspector]
    public float steerParameter;
    [HideInInspector]
    public float brakeParameter;
    [HideInInspector]
    public Color emittedReverseColor;
    [HideInInspector]
    public Color reverseColor;
    [HideInInspector]
    public Color emittedTurnColor;
    [HideInInspector]
    public Color turnColor;
    [HideInInspector]
    public bool gasPressed;
    [HideInInspector]
    public float steeringWheelAngle;
    [HideInInspector]
    public bool inReverse;
    [HideInInspector]
    public bool inRightTurn;
    [HideInInspector]
    public bool inLeftTurn;
    /*[HideInInspector]
    public bool carStarted;
    [HideInInspector]
    public float scenarioNum;
    can be used for GM control and in-scenario updates to instructions*/

    public bool isPlayer;

    private Rigidbody rb;
    private Vector3 pos;
    private Quaternion rot;
    private bool turnLightOn;
    private Quaternion originalSteerRot;
    /*private bool receivedInstructions;
    private bool activeCar;
    can be used for GM control and in-scenario updates to instructions*/

    private void OnDestroy() {
        tailLight.SetColor("_Color", reverseColor);
        tailLight.SetColor("_EmissionColor", emittedReverseColor);
        TurnSignalOff(frontLeftLamp, backLeftLamp);
        TurnSignalOff(frontRightLamp, backRightLamp);
        inRightTurn = false;
        inLeftTurn = false;
        inReverse = false;
    }
    void Start() {
        speedParameter = 0;
        steerParameter = 0;
        brakeParameter = 0;
        reverseColor = tailLight.GetColor("_Color");
        emittedReverseColor = tailLight.GetColor("_EmissionColor");
        turnColor = frontLeftLamp.GetColor("_Color");
        emittedTurnColor = frontLeftLamp.GetColor("_EmissionColor");
        rb = GetComponentInParent<Rigidbody>();
        rb.centerOfMass = rb.centerOfMass + new Vector3(0, -0.8f, 0);
        turnLightOn = false;
        /*receivedInstructions = false;
        activeCar = false;
        can be used for GM control and in-scenario updates to instructions*/
        originalSteerRot = steeringWheelT.localRotation;
    }

    void Update() {
        //UpdateInstructions();
        /*if (isPlayer) {
            CheckForStart();
        }
        else {
            if (Time.realtimeSinceStartup >= 8)
            Drive();
        }*/
        Drive();
    }

    void Drive() {
        ApplyTurnSignals();
        Accelerate();
        Steer();
        ApplyBreaksAndReverse();
        UpdateWheelPositions();
        PlayAudio();
    }

    /*private void CheckForStart() {
        if (!activeCar) {
            activeCar = carStarted;
            if (isPlayer && activeCar) {
                if (!soundEffects[2].isPlaying) {
                    soundEffects[2].Play();
                }
            }
        }
        else {
            Drive();
        }
    }*/

    private void Accelerate() {
        backLeftWC.motorTorque = speedParameter;
        backRightWC.motorTorque = speedParameter;
    }

    private void Steer() {
        frontLeftWC.steerAngle = steerParameter;
        frontRightWC.steerAngle = steerParameter;
    }

    private void ApplyBreaksAndReverse() {
        backLeftWC.brakeTorque = brakeParameter;
        backRightWC.brakeTorque = brakeParameter;
        frontLeftWC.brakeTorque = brakeParameter;
        frontRightWC.brakeTorque = brakeParameter;
        if (brakeParameter != 0 || inReverse) {
            tailLight.SetColor("_Color", new Vector4(100, 0, 0, 500));
            tailLight.SetColor("_EmissionColor", new Vector4(100, 100, 100, 500));
        }
        else {
            tailLight.SetColor("_Color", reverseColor);
            tailLight.SetColor("_EmissionColor", emittedReverseColor);
        }
    }

    private void ApplyTurnSignals() {
        if (inRightTurn) {
            TurnSignalOff(frontLeftLamp, backLeftLamp);
            if (turnLightOn == false) {
                TurnSignalOff(frontRightLamp, backRightLamp);
                StartCoroutine(waitForLight(true));
            }
            if (turnLightOn == true) {
                TurnSignalOn(frontRightLamp, backRightLamp);
                StartCoroutine(waitForLight(false));
            }
        }
        if (inLeftTurn) {
            TurnSignalOff(frontRightLamp, backRightLamp);
            if (turnLightOn == false) {
                TurnSignalOff(frontLeftLamp, backLeftLamp);
                StartCoroutine(waitForLight(true));
            }
            if (turnLightOn == true) {
                TurnSignalOn(frontLeftLamp, backLeftLamp);
                StartCoroutine(waitForLight(false));
            }
        }
        if (frontRightLamp.GetColor("_Color") != turnColor && inRightTurn == false) {
            TurnSignalOff(frontRightLamp, backRightLamp);
        }
        if (frontLeftLamp.GetColor("_Color") != turnColor && inLeftTurn == false) {
            TurnSignalOff(frontLeftLamp, backLeftLamp);
        }
    }

    private void TurnSignalOn(Material front, Material back) {
        front.SetColor("_Color", new Vector4(100, 100, 0, 500));
        front.SetColor("_EmissionColor", new Vector4(100, 100, 100, 500));
        back.SetColor("_Color", new Vector4(100, 100, 0, 500));
        back.SetColor("_EmissionColor", new Vector4(100, 100, 100, 500));
    }

    private void TurnSignalOff(Material front, Material back) {
        front.SetColor("_Color", turnColor);
        front.SetColor("_EmissionColor", emittedTurnColor);
        back.SetColor("_Color", turnColor);
        back.SetColor("_EmissionColor", emittedTurnColor);
    }

    IEnumerator waitForLight(bool turn) {
        yield return new WaitForSeconds(0.8f);
        turnLightOn = turn;
    }

    private void UpdateWheelPositions() {
        UpdateWheelPosition(frontLeftT, frontLeftWC);
        UpdateWheelPosition(frontRightT, frontRightWC);
        UpdateWheelPosition(backLeftT, backLeftWC);
        UpdateWheelPosition(backRightT, backRightWC);
        if (isPlayer) {
            UpdateSteeringWheel();
        }
    }

    private void UpdateWheelPosition(Transform trans, WheelCollider wc) {
        pos = trans.position;
        rot = trans.rotation;
        wc.GetWorldPose(out pos, out rot);
        trans.position = pos;
        trans.rotation = rot;
    }

    private void UpdateSteeringWheel() {
        steeringWheelT.localRotation = originalSteerRot * Quaternion.Euler(0, steeringWheelAngle, 0);
    }

    private void PlayAudio() {
        if (isPlayer && gasPressed) {
            if (!soundEffects[1].isPlaying && !soundEffects[2].isPlaying) {
                soundEffects[1].Play();
            }
        }
        else {
            if (isPlayer) {
                if (!soundEffects[0].isPlaying && !soundEffects[2].isPlaying) {
                    soundEffects[0].Play();
                }
            }
        }
    }

    /*private void UpdateInstructions() {
        if (receivedInstructions == false && isPlayer) {
            switch (scenarioNum) {
                case 1:
                    instructionText.text = "Turn right";
                    receivedInstructions = true;
                    break;
                case 2:
                    instructionText.text = "Turn left";
                    receivedInstructions = true;
                    break;
                case 3:
                    instructionText.text = "Go straight";
                    receivedInstructions = true;
                    break;
                case 4:
                    instructionText.text = "Turn left";
                    receivedInstructions = true;
                    break;
                case 5:
                    instructionText.text = "Merge along the road";
                    receivedInstructions = true;
                    break;
                case 6:
                    instructionText.text = "Merge along the road";
                    receivedInstructions = true;
                    break;
                case 7:
                    instructionText.text = "Hurry up";
                    receivedInstructions = true;
                    break;
                case 8:
                    instructionText.text = "Stop at the intersection";
                    receivedInstructions = true;
                    break;
                case 9:
                    instructionText.text = "Follow the road";
                    receivedInstructions = true;
                    break;
                case 0:
                    instructionText.text = "Follow the road";
                    receivedInstructions = true;
                    break;
                default:
                    instructionText.text = "Start Car: [Shift]";
                    break;
            }
        } else { }
    }*/
}