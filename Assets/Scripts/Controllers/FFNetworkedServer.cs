/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class FFNetworkedServer : MonoBehaviour
{

    public WheelCollider[] wheels;

    public float weightIntensity = 1f;
    public float tireWidth = .1f;

    float selfAlignmentTorque;
   
    
    private Rigidbody rb;


    private VehicleInputControllerNetworked VICN;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        VICN = GetComponent<VehicleInputControllerNetworked>();
    }

  
    float RPMToAngularVel(float rpm)
    {
        return rpm * 2 * Mathf.PI / 60f;
    }

    void Update()
    {
        
        selfAlignmentTorque = 0f;
        foreach (var wheel in wheels) {
            if (wheel.isGrounded) {
                WheelHit hit;
                wheel.GetGroundHit(out hit);
               
               //Debug.DrawRay(hit.point, hit.sidewaysDir, Color.red);
                //Debug.DrawRay(hit.point, hit.forwardDir, Color.blue);
               
                Vector3 left = hit.point - ( hit.sidewaysDir * tireWidth * 0.5f );
                Vector3 right = hit.point + ( hit.sidewaysDir * tireWidth * 0.5f );

                Vector3 leftTangent = rb.GetPointVelocity(left);
                leftTangent -= Vector3.Project(leftTangent, hit.normal);

                Vector3 rightTangent = rb.GetPointVelocity(right);
                rightTangent -= Vector3.Project(rightTangent, hit.normal);

                float slipDifference = Vector3.Dot(hit.forwardDir, rightTangent) - Vector3.Dot(hit.forwardDir, leftTangent);

                selfAlignmentTorque += ( 0.5f * weightIntensity * slipDifference ) / 2f;

            }
        }
       
        VICN.selfAlignmentTorque = selfAlignmentTorque;
    }

}
