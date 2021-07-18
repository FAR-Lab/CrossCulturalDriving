using UnityEngine;
using UnityEngine.UI;

public class Drive_Bridge : MonoBehaviour {
    public WheelCollider frontLeftWC, frontRightWC;
    public WheelCollider backLeftWC, backRightWC;

    public Transform frontLeftT, frontRightT;
    public Transform backLeftT, backRightT;

    public Transform steeringWheelT;

    public Material tailLight;

    public AudioSource[] soundEffects;

    public Text instructionText;

    [HideInInspector]
    public float speedParameter;
    [HideInInspector]
    public float steerParameter;
    [HideInInspector]
    public float brakeParameter;
    [HideInInspector]
    public Color _emittedColor;
    [HideInInspector]
    public Color _Color;
    [HideInInspector]
    public bool gasPressed;
    [HideInInspector]
    public float steeringWheelAngle;
    /*[HideInInspector]
    public bool carStarted;
    [HideInInspector]
    public float scenarioNum;
    can be used for GM control and in-scenario updates to instructions*/

    public bool isPlayer;

    private Rigidbody rb;
    private Vector3 pos;
    private Quaternion rot;
    /*private bool receivedInstructions;
    private bool activeCar;
    can be used for GM control and in-scenario updates to instructions*/

    private void OnDestroy() {
        tailLight.SetColor("_Color", _Color);
        tailLight.SetColor("_EmissionColor", _emittedColor);
    }
    void Start() {
        speedParameter = 0;
        steerParameter = 0;
        brakeParameter = 0;
        _Color = tailLight.GetColor("_Color");
        _emittedColor = tailLight.GetColor("_EmissionColor");
        rb = GetComponentInParent<Rigidbody>();
        rb.centerOfMass = rb.centerOfMass + new Vector3(0, -0.8f, 0);
        /*receivedInstructions = false;
        activeCar = false;
        can be used for GM control and in-scenario updates to instructions*/
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
        Accelerate();
        Steer();
        ApplyBreaks();
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

    private void ApplyBreaks() {
        backLeftWC.brakeTorque = brakeParameter;
        backRightWC.brakeTorque = brakeParameter;
        frontLeftWC.brakeTorque = brakeParameter;
        frontRightWC.brakeTorque = brakeParameter;
        if (brakeParameter != 0) {
            tailLight.SetColor("_Color", new Vector4(100, 0, 0, 500));
            tailLight.SetColor("_EmissionColor", new Vector4(100, 100, 100, 500));
        }
        else {
            tailLight.SetColor("_Color", _Color);
            tailLight.SetColor("_EmissionColor", _emittedColor);
        }
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
        steeringWheelT.localRotation = Quaternion.Euler(-steeringWheelAngle, -90, 90);
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