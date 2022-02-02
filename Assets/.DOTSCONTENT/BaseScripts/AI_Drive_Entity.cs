using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using UnityEngine.AI;

public class AI_Drive_Entity : MonoBehaviour {
    public float maxSpeed;
    public float maxAcceleration;
    public float maxMotorTorque;
    public float maxBrakeTorque;
    public float maxSteerAngle;
    public List<GameObject> destinations;
    public List<float> waitTimes;
    public NavMeshAgent agent;
    public float max_rb_agent_distance;
    public Vector3 offset;

    private float currentSpeed;
    private EntityManager entitymanager;
    private Entity entity;
    private Rigidbody rb;
    private Drive_Bridge db;
    private float speedParameter;
    private float steerParameter;
    private float brakeParameter;
    private float dotProduct;
    private bool waiting;
    private Quaternion initialRotation;

    void Start() {
        db = GetComponent<Drive_Bridge>();
        rb = GetComponentInParent<Rigidbody>();
        initialRotation = rb.transform.rotation;
        waiting = false;
        entitymanager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityArchetype ea = entitymanager.CreateArchetype(
                             typeof(AI_Drive_Component)
                             );
        entity = entitymanager.CreateEntity(ea);
        entitymanager.AddComponentData(entity, new AI_Drive_Component {
            maxAcceleration = maxAcceleration,
            maxSpeed = maxSpeed,
            maxMotorTorque = maxMotorTorque,
            maxBrakeTorque = maxBrakeTorque,
            maxSteerAngle = maxSteerAngle,
            worldDownVector = Vector3.down
        });
        agent.SetDestination(destinations[0].transform.position);
        destinations.RemoveAt(0);
    }

    void Update() {
        AI_Drive_Component adc = entitymanager.GetComponentData<AI_Drive_Component>(entity);
        adc.rbLocalToWorld.Value = rb.transform.localToWorldMatrix;
        adc.agentPosition = agent.transform.position;
        adc.currentVelocity = rb.velocity;
        entitymanager.SetComponentData(entity, adc);
        currentSpeed = adc.currentSpeed;
        speedParameter = adc.speedParameter;
        steerParameter = adc.steerParameter;
        brakeParameter = adc.brakeParameter;
        dotProduct = adc.dotProduct;
        SetDriveParameters(adc.rb_agent_distance);
        ControlAgent(adc.rb_agent_distance);
    }

    private void ResetVehicle() {
        rb.transform.position = rb.transform.position + offset;
        rb.transform.rotation = initialRotation;
    }

    private void ControlAgent(float dist) {
        if (dist > max_rb_agent_distance) {
            agent.isStopped = true;
        } else {
            agent.isStopped = false;
        }
    }
    IEnumerator wait() {
        //Debug.Log("entered");
        yield return new WaitForSeconds(waitTimes[0]);
        waiting = false;
        waitTimes.RemoveAt(0);
        agent.SetDestination(destinations[0].transform.position);
        destinations.RemoveAt(0);
    }

    private void SetDriveParameters(float dist) {
        if (db != null && !agent.pathPending && dotProduct < 0 && agent.remainingDistance + dist > 4) {
            db.steerParameter = steerParameter;
            db.brakeParameter = 0;
            if (currentSpeed < maxSpeed) {
                db.speedParameter = speedParameter;
            } else {
                db.speedParameter = 0;
            }
        } else {
            db.brakeParameter = brakeParameter;
            db.steerParameter = steerParameter;
            db.speedParameter = 0;
            if (dotProduct > 0 && currentSpeed <= 0.1) {
                ResetVehicle();
            }
            //Debug.Log("remaining dist: " + agent.remainingDistance + dist);
            //Debug.Log("waiting: " + waiting);
            if (db != null && dotProduct < 0 && agent.remainingDistance + dist < 3.5 && waiting == false && waitTimes.Count > 0) {
                waiting = true;
                StartCoroutine(wait());
            }
        }
    }
}
