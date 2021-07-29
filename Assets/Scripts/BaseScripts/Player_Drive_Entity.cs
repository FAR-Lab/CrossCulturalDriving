using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class Player_Drive_Entity : MonoBehaviour {
    public float maxSpeed;
    public float maxAcceleration;
    public float maxMotorTorque;
    public float maxBrakeTorque;
    public float maxSteerAngle;
    private float currentSpeed;
    private EntityManager entitymanager;
    private Entity entity;
    private Rigidbody rb;
    private Drive_Bridge db;
    private float speedParameter;
    private float steerParameter;
    private float brakeParameter;
    private float steeringWheelAngle;
    private bool gasPressed;
    private bool inReverse;
    private bool inRightTurn;
    private bool inLeftTurn;
    /*private bool carStarted;
    private float scenarioNum;
    can be used for GM control and in-scenario updates to instructions*/

    void Start() {
        db = GetComponent<Drive_Bridge>();
        rb = GetComponentInParent<Rigidbody>();

        entitymanager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityArchetype ea = entitymanager.CreateArchetype(
                             typeof(Player_Drive_Component)
                             );
        entity = entitymanager.CreateEntity(ea);
        entitymanager.AddComponentData(entity, new Player_Drive_Component {
            maxAcceleration = maxAcceleration,
            maxSpeed = maxSpeed,
            maxMotorTorque = maxMotorTorque,
            maxBrakeTorque = maxBrakeTorque,
            maxSteerAngle = maxSteerAngle,
            gasPedal = false,
            inReverse = false,
            inRightTurn = false,
            inLeftTurn = false,
            /*scenarioNum = 0,
            carStarted = false
            can be used for GM control and in-scenario updates to instructions*/
        });;

    }

    void Update() {
        Player_Drive_Component pdc = entitymanager.GetComponentData<Player_Drive_Component>(entity);
        pdc.currentVelocity = rb.velocity;
        entitymanager.SetComponentData(entity, pdc);
        currentSpeed = pdc.currentSpeed;
        speedParameter = pdc.speedParameter;
        steerParameter = pdc.steerParameter;
        brakeParameter = pdc.brakeParameter;
        steeringWheelAngle = pdc.steeringWheelAngle;
        gasPressed = pdc.gasPedal;
        inReverse = pdc.inReverse;
        inRightTurn = pdc.inRightTurn;
        inLeftTurn = pdc.inLeftTurn;
        /*scenarioNum = pdc.scenarioNum;
        carStarted = pdc.carStarted;
        can be used for GM control and in-scenario updates to instructions */
        SetDriveParameters();
    }

    private void SetDriveParameters() {
        if (db != null) {
            db.steerParameter = steerParameter;
            db.brakeParameter = brakeParameter;
            db.gasPressed = gasPressed;
            db.steeringWheelAngle = steeringWheelAngle;
            /*db.scenarioNum = scenarioNum;
            //db.carStarted = carStarted;
            can be used for GM control and in-scenario updates to instructions */
            if (currentSpeed < maxSpeed) {
                db.speedParameter = speedParameter;
            } else {
                db.speedParameter = 0;
            }
            db.inReverse = inReverse;
            db.inRightTurn = inRightTurn;
            db.inLeftTurn = inLeftTurn;
        }
    }
}
