using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Drive_System : SystemBase {
    private InputAction accelerateAction;
    private InputAction steerAction;
    private InputAction brakeAction;
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
          accelerateAction.AddBinding("<Joystick>/z");
          steerAction.AddBinding("<Joystick>/stick/x");
          brakeAction.AddBinding("<Joystick>/rz");
          accelerateAction.Enable();
          steerAction.Enable();
          brakeAction.Enable();*/

        //G29 LOGITECH INPUT------------------------------------------------------------------
        Debug.Log("SteeringInit:" + LogitechGSDK.LogiSteeringInitialize(false));
        accelerateAction = new InputAction("Accelerate");
        brakeAction = new InputAction("Brake");
        accelerateAction.AddBinding("<Joystick>/z");
        brakeAction.AddBinding("<Joystick>/rz");
        accelerateAction.Enable();
        brakeAction.Enable();      
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
        turnOnSpring();
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

        Entities.ForEach((ref Player_Drive_Component pdc) => {            
            pdc.currentSpeed = math.sqrt(math.pow(pdc.currentVelocity.x, 2) +
                                        math.pow(pdc.currentVelocity.y, 2) +
                                        math.pow(pdc.currentVelocity.z, 2));
            pdc.speedParameter = pdc.maxMotorTorque * pdc.maxAcceleration * vAxis * (1 - (pdc.currentSpeed / pdc.maxSpeed));
            pdc.steerParameter = pdc.maxSteerAngle * hAxis;
            //G29 Input-----------------------------------------------------------------------
            pdc.steeringWheelAngle = 450.0f * hAxis;
            if (spaceKey) {
                pdc.brakeParameter = pdc.maxBrakeTorque * (pdc.currentSpeed / pdc.maxSpeed) * pdc.maxAcceleration * 3.0f * brakeValue;
            } else {
                pdc.brakeParameter = 0;
            }
            pdc.gasPedal = gasPedal;

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
    }

    void turnOnSpring() {
        if (!LogitechGSDK.LogiIsPlaying(0, LogitechGSDK.LOGI_FORCE_SPRING)) {
                LogitechGSDK.LogiPlaySpringForce(0, 0, 95, 10);
        }
    }


}