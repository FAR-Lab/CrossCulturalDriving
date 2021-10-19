using UnityEngine;
using System.Collections;
public class Car : MonoBehaviour
{
    // Private
    private Rigidbody body = null;
    private float hInput = 0;
    private float vInput = 0;

    // Public
    public WheelCollider WheelFL;
    public WheelCollider WheelFR;
    public WheelCollider WheelRL;
    public WheelCollider WheelRR;
    public Transform centreOfMass;
    public float maxSteerAngle = 35;
    public float maxTorque = 1000;
    public float maxBrakeTorque = 500;
    
    // Methods
    public void Start()
    {
        body = GetComponent<Rigidbody>();
        body.centerOfMass = centreOfMass.transform.localPosition;

        WheelRR.ConfigureVehicleSubsteps(5, 5, 5);
    }

    public void Update()
    {
        hInput = Input.GetAxis("Horizontal");
        vInput = Input.GetAxis("Vertical");
    }

    public void FixedUpdate()
    {
        // Accelerating
        if(vInput > 0.05f)
        {
            // Disable braking
            SetWheelBrakeTorque(0);

            // Apply the torque
            SetWheelDriveTorque(maxTorque * vInput);
        }
        // Braking
        else if(vInput < 0.05f)
        {
            if(WheelRL.rpm > 10 && WheelRR.rpm > 10)
            {
                // Disable acceleration
                SetWheelDriveTorque(0);

                // Add brake torque
                SetWheelBrakeTorque(maxBrakeTorque * -vInput);
            }
            else //if(body.velocity.magnitude < 0)
            {
                SetWheelDriveTorque(maxTorque * vInput);
                SetWheelBrakeTorque(0);
            }
        }
        // Coasting
        else
        {
            SetWheelDriveTorque(0);
            SetWheelBrakeTorque(0);
        }
        
        // Apply steering
        WheelFL.steerAngle = maxSteerAngle * hInput;
        WheelFR.steerAngle = maxSteerAngle * hInput;
    }

    private void SetWheelDriveTorque(float value)
    {
        WheelRL.motorTorque = value;
        WheelRR.motorTorque = value;
    }

    private void SetWheelBrakeTorque(float value)
    {
        WheelFL.brakeTorque = value;
        WheelFR.brakeTorque = value;
        WheelRL.brakeTorque = value;
        WheelRR.brakeTorque = value;
    }
}