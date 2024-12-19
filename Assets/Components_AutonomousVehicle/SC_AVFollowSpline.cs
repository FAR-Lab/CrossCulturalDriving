using System;
using UnityEngine;
using UnityEngine.Splines;

public class SC_AVFollowSpline : MonoBehaviour
{
    public SplineContainer splineContainer;
    public NetworkVehicleController vehicleController; 
    public SO_AVFollowSplineConfig config;
    
    private float steeringIntegral = 0f;
    private float steeringPrevError = 0f;

    private float speedIntegral = 0f;
    private float speedPrevError = 0f;
    
    private Rigidbody rb;

    // for gizmos
    private float _closestT;
    private Vector3 _closestPoint;
    private float _lookT;
    private Vector3 _lookPoint;
    private Vector3 _toTarget;
    private float _headingError;

    public bool IsDriving = false;

    // variable for maintain continuity of the closest point
    private bool initializedClosestT = false;
    private float lastClosestT = 0f;

    void Start()
    {
        rb = vehicleController.GetComponent<Rigidbody>();
    }

    private void Update() {
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.D)) {
            IsDriving = !IsDriving;
        }
    }

    void FixedUpdate()
    {
        if (splineContainer == null || splineContainer.Splines.Count == 0 || !IsDriving) {
            return;
        }

        var spline = splineContainer.Spline;
        bool isClosedLoop = spline.Closed;

        Vector3 vehiclePos = transform.position;
        _closestT = FindClosestTOnSpline(spline, vehiclePos, isClosedLoop);
        _closestPoint = splineContainer.EvaluatePosition(_closestT);

        float lookDistanceNormalized = config.lookAheadDistance / spline.GetLength();
        
        // tentative wrapping solution
        _lookT = WrapT(_closestT + lookDistanceNormalized, isClosedLoop);
        _lookPoint = splineContainer.EvaluatePosition(_lookT);

        _toTarget = (_lookPoint - vehiclePos).normalized;
        Vector3 vehicleForward = transform.forward;
        _headingError = Vector3.SignedAngle(vehicleForward, _toTarget, Vector3.up) * Mathf.Deg2Rad;
        
        float currentSpeed = rb.velocity.magnitude;
        float speedError = config.desiredSpeed - currentSpeed;

        // Steering PID
        float steeringControl = PIDControl(_headingError, ref steeringIntegral, ref steeringPrevError, 
                                           config.Kp_steering, config.Ki_steering, config.Kd_steering);
        steeringControl = Mathf.Clamp(steeringControl, -1f, 1f);

        // Speed PID
        float throttleControl = PIDControl(speedError, ref speedIntegral, ref speedPrevError, 
                                           config.Kp_speed, config.Ki_speed, config.Kd_speed);
        throttleControl = Mathf.Clamp(throttleControl, -1f, 1f);

        vehicleController.SteeringInput = steeringControl;
        vehicleController.ThrottleInput = throttleControl;
    }

    private float PIDControl(float error, ref float integral, ref float prevError, float Kp, float Ki, float Kd)
    {
        float dt = Time.fixedDeltaTime;

        integral += error * dt;
        float derivative = (error - prevError) / dt;
        float output = Kp * error + Ki * integral + Kd * derivative;
        prevError = error;

        return output;
    }

    private float FindClosestTOnSpline(Spline spline, Vector3 point, bool isClosedLoop)
    {
        int fullSampleCount = 200; 
        int localSampleCount = 50;
        float searchRadius = 0.05f; // to accomodate self intersection like 8 shape course

        float DistAtT(float t)
        {
            t = WrapT(t, isClosedLoop);
            Vector3 splinePoint = splineContainer.EvaluatePosition(t);
            return Vector3.SqrMagnitude(splinePoint - point);
        }

        if (!initializedClosestT)
        {
            float closestT = 0f;
            float closestDist = Mathf.Infinity;

            for (int i = 0; i <= fullSampleCount; i++)
            {
                float t = i / (float)fullSampleCount;
                float dist = DistAtT(t);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestT = t;
                }
            }

            lastClosestT = closestT;
            initializedClosestT = true;
            return closestT;
        }
        else
        {
            float startT = lastClosestT - searchRadius;
            float endT = lastClosestT + searchRadius;

            float closestT = lastClosestT;
            float closestDist = DistAtT(lastClosestT);

            if (isClosedLoop)
            {
                for (int i = 0; i <= localSampleCount; i++)
                {
                    float lerpT = Mathf.Lerp(startT, endT, i / (float)localSampleCount);
                    float dist = DistAtT(lerpT);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestT = WrapT(lerpT, isClosedLoop);
                    }
                }
            }
            else
            {
                float clampedStart = Mathf.Clamp01(startT);
                float clampedEnd = Mathf.Clamp01(endT);

                for (int i = 0; i <= localSampleCount; i++)
                {
                    float lerpT = Mathf.Lerp(clampedStart, clampedEnd, i / (float)localSampleCount);
                    float dist = DistAtT(lerpT);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestT = lerpT;
                    }
                }
            }

            lastClosestT = closestT;
            return closestT;
        }
    }

    private float WrapT(float t, bool isClosedLoop)
    {
        if (isClosedLoop)
        {
            t = t % 1f;
            if (t < 0f) t += 1f;
        }
        else
        {
            t = Mathf.Clamp01(t);
        }

        return t;
    }

    private void OnDrawGizmos() 
    {
        if (splineContainer != null && splineContainer.Spline != null && IsDriving) 
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_closestPoint, 0.5f);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_lookPoint, 0.5f);

            Vector3 errorVector = Quaternion.AngleAxis(_headingError * Mathf.Rad2Deg, Vector3.up) * transform.forward;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + errorVector * 5f);
        }
    }
}
