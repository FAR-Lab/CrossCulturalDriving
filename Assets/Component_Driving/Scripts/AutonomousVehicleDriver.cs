#define DEBUGAUTONMOUSDRIVER

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutonomousVehicleDriver : MonoBehaviour
{
    public WayPoint StartingWayPoint;
    private WayPoint NextWaypoint;
    
    private float Steering;
    private float Throttle;
    private bool IndicateLeft;
    private bool IndicateRight;
    private bool Honk;

    private VehicleController _vehicleController;

    private const float maxTurn = 70;


    private PID speedPID = new PID(0.1f, 0.01f, 0.005f);
    // Start is called before the first frame update
    void Start()
    {
        _vehicleController = GetComponent<VehicleController>();
        NextWaypoint = StartingWayPoint;
    }

    // Update is called once per frame
    void Update()
    {
        
        #if DEBUGAUTONMOUSDRIVER && UNITY_EDITOR
        
        Debug.DrawLine(transform.position,NextWaypoint.transform.position);
        
        
        
        #endif
        
        

        if (!NextWaypoint.isLastWaypoint)
        {

            if((NextWaypoint.transform.position - transform.position).magnitude<=NextWaypoint.ArrivalRange)
            {
                NextWaypoint = NextWaypoint.nextWayPoint;
            }
           
                
            
            
            Throttle = speedPID.Update(StartingWayPoint.targetSpeed, _vehicleController.CurrentSpeed,
                Time.fixedDeltaTime);
        }
        else
        {
            Throttle = speedPID.Update(0, _vehicleController.CurrentSpeed,
                Time.fixedDeltaTime);
        }
        Steering = -Vector3.SignedAngle(transform.forward,
            (StartingWayPoint.transform.position - transform.position), Vector3.up)/maxTurn;
        Debug.Log($"Throttle:{Throttle} \t Steering:{Steering}");
        

    }

    public float GetSteerInput()
    {
        return  Mathf.Clamp(Steering,-1,1);;
    }

    public float GetAccelInput()
    {
        return Mathf.Clamp(Throttle,-1,1);
    }

    public bool GetLeftIndicatorInput()
    {
        return StartingWayPoint == null && StartingWayPoint.IndicateLeft;
    }

    public bool GetRightIndicatorInput()
    {
        return StartingWayPoint == null && StartingWayPoint.IndicateRight;
    }

    public bool GetHornInput()
    {
        return StartingWayPoint == null && StartingWayPoint.Horn;
    }
}
