using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Drive_System : SystemBase {
    private InputAction accelerateAction;
    private InputAction steerAction;
    protected override void OnCreate() {
        accelerateAction = new InputAction("Accelerate");
        accelerateAction.AddCompositeBinding("Axis")
            .With("Positive", "<Keyboard>/w")
            .With("Negative", "<Keyboard>/s");
        accelerateAction.Enable();
        steerAction = new InputAction("Steer");
        steerAction.AddCompositeBinding("Axis")
            .With("Positive", "<Keyboard>/d")
            .With("Negative", "<Keyboard>/a");
        steerAction.Enable();
    }
    protected override void OnUpdate() {
        float hAxis = steerAction.ReadValue<float>();
        float vAxis = accelerateAction.ReadValue<float>();
        bool gasPedal = Keyboard.current.wKey.isPressed || Keyboard.current.sKey.isPressed;
        bool spaceKey = Keyboard.current.spaceKey.isPressed;
        /*float hAxis = Input.GetAxis("Horizontal");
        float vAxis = Input.GetAxis("Vertical");
        bool gasPedal = Input.GetKey("w") || Input.GetKey("s");
        bool spaceKey = Input.GetKey("space");
        bool engineStarted = Input.GetKeyDown(KeyCode.LeftShift); change to a key not available on the steering wheel
        float scenarioNum;
        if (!float.TryParse(Input.inputString, out scenarioNum)) {}
        Legacy input system*/



        Entities.ForEach((ref Player_Drive_Component pdc) => {            
            pdc.currentSpeed = math.sqrt(math.pow(pdc.currentVelocity.x, 2) +
                                        math.pow(pdc.currentVelocity.y, 2) +
                                        math.pow(pdc.currentVelocity.z, 2));
            pdc.speedParameter = pdc.maxMotorTorque * pdc.maxAcceleration * vAxis * (1 - (pdc.currentSpeed / pdc.maxSpeed));
            pdc.steerParameter = pdc.maxSteerAngle * hAxis;
            if (spaceKey) {
                pdc.brakeParameter = pdc.maxBrakeTorque * (pdc.currentSpeed / pdc.maxSpeed) * pdc.maxAcceleration;
            } else {
                pdc.brakeParameter = 0;
            }
            pdc.gasPedal = gasPedal;
            /*pdc.scenarioNum = scenarioNum;
            pdc.carStarted = engineStarted;
            can be used for GM control and in-scenario updates to instructions*/
        }).Run();

    }
}