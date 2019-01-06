/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class ForceFeedback : MonoBehaviour
{

    public WheelCollider[] wheels;

    public AnimationCurve damperCurve;
    private SteeringWheelInputController logi;
    public bool isShowingDebugDialog = false;
    public float weightIntensity = 1f;
    public float tireWidth = .1f;

    public float springSaturation;
    public float springCoeff;
    public int damperAmount = 3000;
    float selfAlignmentTorque;
    private Rigidbody rb;

    void Start()
    {
        
        if (transform.GetComponent<SteeringWheelInputController>()!=null)
        {
            logi = transform.GetComponent<SteeringWheelInputController>();

            logi.SetConstantForce(0);
            logi.SetDamperForce(0);
            logi.SetSpringForce(0, 0);
            logi.Init();
        }
        else
        {
            this.enabled = false;
        }
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
       
    }

    void OnDisable()
    {
       
    }

    void ToggleShowDebug()
    {
        isShowingDebugDialog = !isShowingDebugDialog;
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
                Debug.DrawRay(hit.point, hit.sidewaysDir, Color.red);
                Debug.DrawRay(hit.point, hit.forwardDir, Color.blue);
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
       
        float forceFeedback = selfAlignmentTorque;

        //disable during autodrive mode
        if (SceneStateManager.Instance !=null && SceneStateManager.Instance.ActionState!=ActionState.DRIVE)
        {
            if (logi != null)
            {
                
                logi.SetConstantForce(0);
                logi.SetDamperForce(0);
                logi.SetSpringForce(0, 0);
            }
        }
        else
        {
            if (logi != null)
            {
                logi.Init();
                
                logi.SetConstantForce((int)(forceFeedback * 10000f));
                    logi.SetSpringForce(Mathf.RoundToInt(springSaturation * Mathf.Abs(forceFeedback) * 10000f), Mathf.RoundToInt(springCoeff * 10000f));
                    logi.SetDamperForce(damperAmount);
                
               
            }
        }


    }

    void OnGUI()
    {
        

        if (isShowingDebugDialog)
        {
           
            
        }
    }
}
