using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Player_Drive_System : SystemBase {
    private InputAction accelerateAction;
    private InputAction steerAction;
    private InputAction reverseAction;
    private InputAction rightTurnSignalAction;
    private InputAction leftTurnSignalAction;
    private InputAction brakeAction;
    private InputAction resetAction;
    private bool reverse;
    private bool rightTurn;
    private bool leftTurn;
    protected override void OnCreate() {
        //KEYBOARD LEGACY INPUT---------------------------------------------------------------
        /*nothing to be done here*/

        //KEYBOARD UNITY INPUTSYSTEM----------------------------------------------------------
        /*accelerateAction = new InputAction("Accelerate");
        steerAction = new InputAction("Steer");
        accelerateAction.AddCompositeBinding("Axis")
            .With("Positive", "<Keyboard>/w")
            .With("Negative", "<Keyboard>/s");
        steerAction.AddCompositeBinding("Axis")
            .With("Positive", "<Keyboard>/d")
            .With("Negative", "<Keyboard>/a");
        accelerateAction.Enable();
        steerAction.Enable();*/

        //G29 UNITY INPUTSYSTEM---------------------------------------------------------------
        /*Debug.Log("SteeringInit:" + LogitechGSDK.LogiSteeringInitialize(false));
          accelerateAction = new InputAction("Accelerate");
          steerAction = new InputAction("Steer");
          brakeAction = new InputAction("Brake");
          reverseAction = new InputAction("Reverse");
          rightTurnSignalAction = new InputAction("Right Turn Signal");
          leftTurnSignalAction = new InputAction("Left Turn Signal");
          accelerateAction.AddBinding("<Joystick>/z");
          steerAction.AddBinding("<Joystick>/stick/x");
          brakeAction.AddBinding("<Joystick>/rz");
          reverseAction.AddBinding("<Joystick>/button24");
          rightTurnSignalAction.AddBinding("<Joystick>/button5");
          leftTurnSignalAction.AddBinding("<Joystick>/button6"); 
          accelerateAction.Enable();
          steerAction.Enable();
          brakeAction.Enable();
          reverseAction.Enable();
          rightTurnSignalAction.Enable();
          leftTurnSignalAction.Enable();
          reverse = false;
          rightTurn = false;
          leftTurn = false;*/

        //G29 LOGITECH INPUT------------------------------------------------------------------
        Debug.Log("SteeringInit:" + LogitechGSDK.LogiSteeringInitialize(false));
        accelerateAction = new InputAction("Accelerate");
        brakeAction = new InputAction("Brake");
        reverseAction = new InputAction("Reverse");
        rightTurnSignalAction = new InputAction("Right Turn Signal");
        leftTurnSignalAction = new InputAction("Left Turn Signal");
        resetAction = new InputAction("Reset Scene");
        accelerateAction.AddBinding("<Joystick>/z");
        brakeAction.AddBinding("<Joystick>/rz");
        reverseAction.AddBinding("<Joystick>/button24");
        rightTurnSignalAction.AddBinding("<Joystick>/button5");
        rightTurnSignalAction.AddBinding("<Keyboard>/d");
        leftTurnSignalAction.AddBinding("<Joystick>/button6");
        leftTurnSignalAction.AddBinding("<Keyboard>/a");
        resetAction.AddBinding("<Keyboard>/q");
        accelerateAction.Enable();
        brakeAction.Enable();
        reverseAction.Enable();
        rightTurnSignalAction.Enable();
        leftTurnSignalAction.Enable();
        resetAction.Enable();
        reverse = false;
        rightTurn = false;
        leftTurn = false;
    }
    protected override void OnUpdate() {
        /*bool engineStarted = Input.GetKeyDown(KeyCode.LeftShift); change to a key not available on the steering wheel
        float scenarioNum;
        if (!float.TryParse(Input.inputString, out scenarioNum)) {}*/
        //can be used for in-Scenario instruction updates and starting car engines


        //KEYBOARD LEGACY INPUT---------------------------------------------------------------
        /*float hAxis = Input.GetAxis("Horizontal");
          float vAxis = Input.GetAxis("Vertical");
          bool gasPedal = Input.GetKey("w") || Input.GetKey("s");
          bool spaceKey = Input.GetKey("space");*/

        //KEYBOARD UNITY INPUTSYSTEM----------------------------------------------------------
        /*float hAxis = steerAction.ReadValue<float>();
          float vAxis = accelerateAction.ReadValue<float>();
          bool gasPedal = Keyboard.current.wKey.isPressed || Keyboard.current.sKey.isPressed;
          bool spaceKey = Keyboard.current.spaceKey.isPressed;*/

        //G29 UNITY INPUTSYSTEM---------------------------------------------------------------
        /*float hAxis = steerAction.ReadValue<float>();
          float vAxis = -(accelerateAction.ReadValue<float>() - 1) / 2;
        //because default accelerator value is 1, when pressed it goes to -1
          float brakeValue = -(brakeAction.ReadValue<float>() - 1) / 2;
        //because default brake value is 1, when pressed it goes to -1
          bool gasPedal = vAxis > 0;
          bool spaceKey = brakeValue > 0;*/

        //G29 LOGITECH INPUT------------------------------------------------------------------
        LogitechGSDK.LogiUpdate();
        LogitechGSDK.DIJOYSTATE2ENGINES rec;
        rec = LogitechGSDK.LogiGetStateUnity(0);
        float hAxis = rec.lX / 32767.0f;
        //logitech sdk - eliminates dead zone
        float vAxis = -(accelerateAction.ReadValue<float>() - 1) / 2;
        //because default accelerator value is 1, when pressed it goes to -1
        float brakeValue = -(brakeAction.ReadValue<float>() - 1) / 2;
        //because default brake value is 1, when pressed it goes to -1
        bool gasPedal = vAxis > 0;
        bool spaceKey = brakeValue > 0;
        if (reverseAction.triggered) {
            reverse = !reverse;
        }
        
        if (rightTurnSignalAction.triggered) {
            rightTurn = !rightTurn;
            leftTurn = false;
        }
        
        if (leftTurnSignalAction.triggered) {
            leftTurn = !leftTurn;
            rightTurn = false;
        }
        if (resetAction.triggered)
        {
            SceneManager.LoadScene("ScenarioSelector");
        }
        bool inReverse = reverse;
        bool inLeftTurn = leftTurn;
        bool inRightTurn = rightTurn;
        int speedRatio = (int)(vAxis * 10);
        springEdit(speedRatio);


        Entities.ForEach((ref Player_Drive_Component pdc) => {            
            pdc.currentSpeed = math.sqrt(math.pow(pdc.currentVelocity.x, 2) +
                                        math.pow(pdc.currentVelocity.y, 2) +
                                        math.pow(pdc.currentVelocity.z, 2));
            pdc.steerParameter = pdc.maxSteerAngle * hAxis;
            //G29 Input-----------------------------------------------------------------------
            pdc.steeringWheelAngle = 450.0f * hAxis;
            if (spaceKey) {
                pdc.brakeParameter = pdc.maxBrakeTorque * (pdc.currentSpeed / pdc.maxSpeed) * pdc.maxAcceleration * 3.0f * brakeValue;
            } else {
                pdc.brakeParameter = 0;
            }
            pdc.gasPedal = gasPedal;
            if (inReverse) {
                pdc.inReverse = true;
                pdc.speedParameter = pdc.maxMotorTorque * pdc.maxAcceleration * vAxis * (1 - (pdc.currentSpeed / pdc.maxSpeed)) * -1f;
            }
            else {
                pdc.inReverse = false;
                pdc.speedParameter = pdc.maxMotorTorque * pdc.maxAcceleration * vAxis * (1 - (pdc.currentSpeed / pdc.maxSpeed));
            }
            pdc.inRightTurn = inRightTurn;
            pdc.inLeftTurn = inLeftTurn;

            //Keyboard Input------------------------------------------------------------------
            /*pdc.steeringWheelAngle = 90.0f * hAxis;
            if (spaceKey) {
                pdc.brakeParameter = pdc.maxBrakeTorque * (pdc.currentSpeed / pdc.maxSpeed) * pdc.maxAcceleration;
            } else {
                pdc.brakeParameter = 0;
            }
            pdc.gasPedal = gasPedal;*/

            /*pdc.scenarioNum = scenarioNum;
            pdc.carStarted = engineStarted;
            can be used for GM control and in-scenario updates to instructions*/
        }).Run();
        
    }

    protected override void OnDestroy() {
        Debug.Log("SteeringShutdown:" + LogitechGSDK.LogiSteeringShutdown());
        reverse = false;
    }

    void springEdit(int r)
    {
        LogitechGSDK.LogiPlaySpringForce(0, 0, 95, r);
    }


}