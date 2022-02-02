/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class ForceFeedback : MonoBehaviour {
    public WheelCollider[] wheels;

    public AnimationCurve damperCurve;

    public float weightIntensity = 1f;
    public float tireWidth = .1f;


    public float springSaturation;
    public float springCoeff;
    public int damperAmount = 3000;
    float selfAlignmentTorque;


    private Rigidbody rb;

    bool changedtoQuestionair = false;


    float forcelerperQuestionair = -1;
    ActionState previousACtionstate;

    private ParticipantOrder _participantOrder;
    private bool ready = false;
    

    public void Init(Rigidbody rigidBodyRef, ParticipantOrder po_) {
        _participantOrder = po_;
        rb = rigidBodyRef;

      //  SteeringWheelManager SWMObj = FindObjectOfType<SteeringWheelManager>();
        
        if (SteeringWheelManager.Singleton != null) {
            SteeringWheelManager.Singleton.SetConstantForce(0, _participantOrder);
            SteeringWheelManager.Singleton.SetDamperForce(0, _participantOrder);
            SteeringWheelManager.Singleton.SetSpringForce(0, 0, _participantOrder);
            ready = true;
        }

       
        else { this.enabled = false; }
    }

  


    float RPMToAngularVel(float rpm) { return rpm * 2 * Mathf.PI / 60f; }

    void Update() {
        if (!ready) return;
        selfAlignmentTorque = 0f;
        foreach (var wheel in wheels) {
            if (wheel.isGrounded) {
                WheelHit hit;
                wheel.GetGroundHit(out hit);

                // Debug.DrawRay(hit.point, hit.sidewaysDir, Color.red);
                //Debug.DrawRay(hit.point, hit.forwardDir, Color.blue);

                Vector3 left = hit.point - (hit.sidewaysDir * tireWidth * 0.5f);
                Vector3 right = hit.point + (hit.sidewaysDir * tireWidth * 0.5f);

                Vector3 leftTangent = rb.GetPointVelocity(left);
                leftTangent -= Vector3.Project(leftTangent, hit.normal);

                Vector3 rightTangent = rb.GetPointVelocity(right);
                rightTangent -= Vector3.Project(rightTangent, hit.normal);

                float slipDifference =
                    Vector3.Dot(hit.forwardDir, rightTangent) - Vector3.Dot(hit.forwardDir, leftTangent);

                selfAlignmentTorque += (0.5f * weightIntensity * slipDifference) / 2f;
            }
        }

        float forceFeedback = selfAlignmentTorque;
    

        if (SteeringWheelManager.Singleton != null) {
            if (ConnectionAndSpawing.Singleton.ServerState == ActionState.QUESTIONS) {
                var steer = SteeringWheelManager.Singleton.GetSteerInput(_participantOrder);
                if ( steer> 0.025f) {
                    SteeringWheelManager.Singleton.SetConstantForce((int) (0.25 * 10000f), _participantOrder);
                }
                else if (steer < -0.025f) {
                    SteeringWheelManager.Singleton.SetConstantForce((int) (-0.25f * 10000f), _participantOrder);
                }
                else { SteeringWheelManager.Singleton.SetConstantForce((int) (0), _participantOrder); }


                SteeringWheelManager.Singleton.SetSpringForce(
                    Mathf.RoundToInt(springSaturation * Mathf.Abs(0) * 10000f), Mathf.RoundToInt(springCoeff * 10000f),
                    _participantOrder);
                SteeringWheelManager.Singleton.SetDamperForce((int) damperAmount / 2, _participantOrder);
            }

            else {
                
                SteeringWheelManager.Singleton.SetConstantForce((int) (forceFeedback * 10000f), _participantOrder);
                SteeringWheelManager.Singleton.SetSpringForce(
                    Mathf.RoundToInt(springSaturation * Mathf.Abs(forceFeedback) * 10000f),
                    Mathf.RoundToInt(springCoeff * 10000f), _participantOrder);
                SteeringWheelManager.Singleton.SetDamperForce(damperAmount, _participantOrder);
            }
        }
    }

}