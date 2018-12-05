using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RVP;
public class AIInput : MonoBehaviour
{
    public float steerRate = 1.0f;
    public float throttleRate = 3.0f;
    public float targetSpeed = 60;
    [Range(0, 120)]// This is the speed the car is trying to get to. Its not really good to have it public just so we can see it easialy.
    public float desiredSpeed = 45;
    [Range(0, 120)]
    public float bestSpeed = 50;
    float targetSteer = 0.0f;
    float targetThrottle = 0.0f;
    float targetBrake = 0.0f;
    public PID speedController;// = new  PID (0.2f, 1.5f, 0f) ;//= new PID ();
    VehicleParent vp;
    private Vector3 MoveTarget;
    public GameObject Visual_Target;
    public bool full_stop;
    int Distress = 0;
    private bool normalTraffic = true;
    public void Move(Vector3 pos)
    {
        if (normalTraffic)
        {
            MoveTarget = pos;
            if (Visual_Target != null)
            {
                Visual_Target.transform.position = pos;
            }
        }
    }
    public void AlternateMove(Vector3 pos)
    {
        if (!normalTraffic)
        {
            MoveTarget = pos;
            Visual_Target.transform.position = pos;
        }
    }
    public void stopTheCar()
    {
        targetSpeed = 0;
        desiredSpeed = 0;
        full_stop = true;
    }
    public void startTheCar()
    {
        targetSpeed = 0;
        desiredSpeed = bestSpeed;
        targetSpeed = desiredSpeed;
        full_stop = false;
    }
    void SteerTowards(out float NewSteerValue, Vector3 NextSteerTarget)
    {
        Debug.DrawLine((transform.position + transform.forward * 2), NextSteerTarget);
        Vector3 to_target = NextSteerTarget - (transform.position + transform.forward * 2);
        to_target.y = 0;
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();
        to_target.Normalize();
        float angle = Vector3.Angle(to_target, forward);
        Vector3 cross_result = Vector3.Cross(to_target, forward);
        cross_result.Normalize();
        if (cross_result.y < 0)
        {
            NewSteerValue = scale(0, 45, 0, 1, angle);
        }
        else if (cross_result.y > 0)
        {
            NewSteerValue = -scale(0, 45, 0, 1, angle);
        }
        else
        {
            NewSteerValue = 0;
        }
    }
    bool LookForward(Vector3 origin, float Angle, out float Distance, out float Speed, out float TargetVelocityAngle, out RaycastHit hit, bool DrawLines)
    {
        if (DrawLines)
            Debug.DrawRay(origin, Quaternion.Euler(0, Angle, 0) * transform.forward);
        if (Physics.Raycast(origin, Quaternion.Euler(0, Angle, 0) * transform.forward, out hit, 50))
        {
            Distance = hit.distance;
            Debug.DrawLine(origin, hit.point);
            Rigidbody target_car = hit.transform.GetComponent<Rigidbody>();
            if (target_car != null)
            {
                TargetVelocityAngle = Vector3.Angle(transform.forward, target_car.velocity);
                Speed = target_car.velocity.magnitude * 3.6f;
            }
            else
            {
                TargetVelocityAngle = 0;
                Speed = -1;
            }
            return true;
        }
        else
        {
            Speed = -1;
            Distance = -1;
            TargetVelocityAngle = 0;
            return false;
        }
    }
    void OnEnable()
    {
        vp = GetComponent<VehicleParent>();
    }
    void Start()
    {
    }
    void Update()
    {
        //Calculating controll values for this frame...

        float speed = 3.6f * vp.velMag;
        targetSpeed = desiredSpeed;
        if (true)
        {
            SteerTowards(out targetSteer, MoveTarget);
            Vector3 rayCastOrigin = (transform.position + transform.forward * 2 + transform.up * 1);
            float DistanceMid = 0;
            float SpeedMid = 0;
            float relativeVelocityAngle = 0;
            float speedToDistanceTollerance = 0.1f;
            RaycastHit HitMid;
            float angle = 0;
            float[] viewAngelsStraight = { 10, -10, 15, 30, 45, 60 };
            bool HitSuccess = LookForward(rayCastOrigin, 0, out DistanceMid, out SpeedMid, out relativeVelocityAngle, out HitMid, true);
            int i = 0;
            while (!HitSuccess)
            {
                ///Debug.Log("We should stop maybe i: "+i +"  Length:"+viewAngelsStraight.Length); 
                if (viewAngelsStraight.Length <= i) { break; }
                if (targetSteer < 0)
                {
                    HitSuccess = LookForward(rayCastOrigin, -viewAngelsStraight[i], out DistanceMid, out SpeedMid, out relativeVelocityAngle, out HitMid, true);
                }
                else if (targetSteer > 0)
                {
                    HitSuccess = LookForward(rayCastOrigin, viewAngelsStraight[i], out DistanceMid, out SpeedMid, out relativeVelocityAngle, out HitMid, true);
                }
                else if (targetSteer == 0)
                {
                    HitSuccess = LookForward(rayCastOrigin, viewAngelsStraight[i], out DistanceMid, out SpeedMid, out relativeVelocityAngle, out HitMid, true);
                    if (!HitSuccess)
                    {
                        HitSuccess = LookForward(rayCastOrigin, -viewAngelsStraight[i], out DistanceMid, out SpeedMid, out relativeVelocityAngle, out HitMid, true);
                    }
                }
                i++;
            }
            if (HitSuccess && false) /// hello new me   how areyou ...listen, this is really npot a good way to code, i know but i did not want anything messing with targetSpeed .. cheers all the best... David
            {
                if (HitMid.transform.GetComponent<Rigidbody>() != null)
                {
                    if (relativeVelocityAngle < 15f)
                    {
                        //if thats almost the same lets say within 15 degree Absolute we adapt the speed
                        if (DistanceMid > SpeedMid + SpeedMid * speedToDistanceTollerance)
                        {
                            targetSpeed = Mathf.Clamp(scale(SpeedMid + SpeedMid * speedToDistanceTollerance, 50, SpeedMid, 120, DistanceMid), 0, 120);
                        }
                        else if (DistanceMid < SpeedMid - SpeedMid * speedToDistanceTollerance)
                        {
                            targetSpeed = Mathf.Clamp(scale(0, SpeedMid - SpeedMid * speedToDistanceTollerance, 0, SpeedMid, DistanceMid), 0, 120);
                        }
                        else
                        {
                            targetSpeed = SpeedMid;
                        }
                    }
                    else if (relativeVelocityAngle >= 15f && relativeVelocityAngle < 90f)
                    {//if not we decide to either stop or reduce speed
                        if (DistanceMid > SpeedMid + SpeedMid * speedToDistanceTollerance)
                        {
                            targetSpeed = Mathf.Clamp(scale(SpeedMid + SpeedMid * speedToDistanceTollerance, 50, SpeedMid, 120, DistanceMid), 0, 120);
                        }
                        else if (DistanceMid < SpeedMid - SpeedMid * speedToDistanceTollerance)
                        {
                            targetSpeed = Mathf.Clamp(scale(0, SpeedMid - SpeedMid * speedToDistanceTollerance, 0, SpeedMid, DistanceMid), 0, 120);
                        }
                        else
                        {
                            targetSpeed = SpeedMid;
                        }
                        targetSpeed *= 0.9f;
                    }
                    else
                    {
                        if (HitSuccess && DistanceMid < 5)
                        {
                            //full_stop = true;  /// EDIT call the car stoped function 
                            targetSpeed = 0;
                        }
                        else
                        {
                            full_stop = false;
                            targetSpeed = 15f;
                        }
                    }
                }
                //Debug.Log ("TargetCarAngle: "+target_car_angle+"TargetSpeed: " + targetSpeed + "Distance: " + hit.distance+"minDistnace: "+(target_car_speed - target_car_speed * speedToDistanceTollerance).ToString()+"MaxDistance:"+(target_car_speed + target_car_speed * speedToDistanceTollerance).ToString());
                if (HitSuccess && DistanceMid <= 4)
                {
                    // here we just start driving agiain 
                    //full_stop = true;   /// EDIT call the car stoped function 
                }
                else if (HitSuccess && DistanceMid > 4 && DistanceMid <= 20)
                {
                    targetSpeed = desiredSpeed;
                    // this should be filled with code if we get a rigid body as a raycast target
                    // that is not a a rigid body i.e. city or so... important
                }
                else if (HitSuccess && DistanceMid > 20)
                {
                    targetSpeed = desiredSpeed;
                    // this should be filled with code if we get a rigid body as a raycast target
                    // that is not a a rigid body i.e. city or so... important
                }
            }
            ///Here we Cap the speed to something that will assure we will not crash 
            /// 
            if (targetSpeed > desiredSpeed + 10)
            {
                targetSpeed = desiredSpeed + 10;
            }
        }
        else
        {
            //define new target speed and steering based on in put from the 
        }
        //// accalerator calculation 
        if (!full_stop)
        {
            // Debug.Log(targetSpeed);
            vp.SetEbrake ( 0);
            float temp = speedController.Update(targetSpeed, speed, Time.deltaTime);
            // Debug.Log("We are in the PID section its telling us a throttle of : " + temp);
            if (temp > 0)
            {
                if (targetSpeed > 0)
                {
                    vp.SetAccel( Mathf.Clamp01(temp));
                    vp.SetBrake( 0);
                }
            }
            else if (temp < 0.1f)
            {
                vp.SetAccel(0);
                
                if (targetSpeed == 0)
                {
                    vp.SetBrake(Mathf.Clamp01(-1 * temp)*3);
                }
                else
                {
                    vp.SetBrake(Mathf.Clamp01(-1 * temp)* 1.75f);
                }
            }
        }
        else
        {
            vp.SetEbrake(1);
            vp.SetAccel(0);
            vp.SetBrake( 0.23f);
        }
        vp.SetSteer( Mathf.MoveTowards(vp.steerInput, targetSteer, steerRate * Time.deltaTime));
    }
    public float scale(float OldMin, float OldMax, float NewMin, float NewMax, float OldValue)
    {
        float OldRange = (OldMax - OldMin);
        float NewRange = (NewMax - NewMin);
        float NewValue = (((OldValue - OldMin) * NewRange) / OldRange) + NewMin;
        return (NewValue);
    }
}
