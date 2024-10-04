using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AVProcessFlow : MonoBehaviour
{
    public CURRENTDIRECTION currentDirection = CURRENTDIRECTION.LeftTurn;
    public CONDITION currentCondition = CONDITION.None;

    public StateFlowVariables states;

    public float actuatingDistance = 30f;

    public Vector3 decelerationPoint;

    public void Start()
    {
        SetFlowVariables();
    }

    void Update()
    {
        if(currentCondition == CONDITION.None)
            return;

        // Left turn
        if(currentDirection == CURRENTDIRECTION.LeftTurn)
        {
            ProcessLeftTurn();
        }
        else if(currentDirection == CURRENTDIRECTION.Straight)
        {
            ProcessGoingForward();
        }
        else if(currentDirection == CURRENTDIRECTION.RightTurn)
        {
            ProcessRightTurn();
        }
    }


    public void SetFlowVariables()
    {
        //East Vehicle Turning Right in D2
        if(currentCondition == CONDITION.CP6)
        {
            states.isEastVehicleTurningRightInD2 = true;
        }
        else
        {
            states.isEastVehicleTurningRightInD2 = false;
        }

        //East Vehicle Turning Left in D3
        if(currentCondition == CONDITION.CP2)
        {
            states.isEastVehicleTurningLeftInD3 = true;
        }
        else
        {
            states.isEastVehicleTurningLeftInD3 = false;
        }

        //East Vehicle Going Straight in D4
        if(currentCondition == CONDITION.CP7)
        {
            states.isEastVehicleGoingStraightInD4 = true;
        }
        else
        {
            states.isEastVehicleGoingStraightInD4 = false;
        }

        //North Vehicle Turning in D5
        if(currentCondition == CONDITION.CP5)
        {
            states.isNorthVehicleTurningInD5 = true;
        }
        else
        {
            states.isNorthVehicleTurningInD5 = false;
        }

        //East Vehicle Turning Left in D6
        if(currentCondition == CONDITION.CP6)
        {
            states.isEastVehicleTurningLeftInD6 = true;
        }
        else
        {
            states.isEastVehicleTurningLeftInD6 = false;
        }   
    }

    public void UpdateStateFlowVariables()
    {
        //Intersection Distance 
        //states.distancetoCenter = ;

        //Distance between cars
        //states.distanceBetweenCars = ;

        //Deceleration Point Distance
        //states.decelerationPointDistance = ;

        //Intersection Clear
        //states.isIntersectionClear = ;
    }

    public void ProcessLeftTurn()
    {
        if(states.distancetoCenter < actuatingDistance)
        {
            //Enter the intersection
            if(states.distancetoCenter > states.decelerationPointDistance)
            {
                //continue going straight
            }
            else
            {
                //decelerate to desired velocity to prepare for turning
                if(states.isIntersectionClear && states.isNorthVehicleTurningInD5 && states.isEastVehicleTurningLeftInD6)
                {
                    // Go straight (this is what KK's note says, it probably means proceed with the turn and go make a left turn)
                }
                else
                {
                    // Decelerate to a stop
                }
            }
        }
    }
    public void ProcessGoingForward()
    {
        if(states.distancetoCenter < actuatingDistance)
        {
            //Enter the intersection
            if(states.distancetoCenter > states.decelerationPointDistance)
            {
                //continue going straight
            }
            else
            {
                //decelerate to desired velocity to prepare for turning
                if(states.isIntersectionClear && states.isEastVehicleTurningRightInD2 && states.isEastVehicleTurningLeftInD3 && states.isEastVehicleGoingStraightInD4)
                {
                    // Go straight
                }
                else
                {
                    // Decelerate to a stop
                }
            }
        }
    }
    public void ProcessRightTurn()
    {
        if(states.distancetoCenter < actuatingDistance)
            {
                //Enter the intersection
                if(states.distancetoCenter > states.decelerationPointDistance)
                {
                    //continue going straight
                }
                else
                {
                    //decelerate to desired velocity to prepare for turning
                    if(states.isIntersectionClear)
                    {
                        // Make a right turn
                    }
                    else
                    {
                        // Decelerate to a stop
                    }
                }
            }
    }
}

public struct StateFlowVariables
{
    public float distancetoCenter;
    public float distanceBetweenCars;
    public float decelerationPointDistance;
    public bool isIntersectionClear; //C1
    public bool isEastVehicleTurningRightInD2; //C2
    public bool isEastVehicleTurningLeftInD3; //C3
    public bool isEastVehicleGoingStraightInD4; //C4
    public bool isNorthVehicleTurningInD5; //C5
    public bool isEastVehicleTurningLeftInD6; //C6
}

public enum IntersectionDirection
{
    North,
    East,
    South,
    West,
    None
}
public enum CONDITION
{
    CP1,
    CP2,
    CP3,
    CP5,
    CP6,
    CP7,
    CP8,
    None
}
public enum CURRENTDIRECTION{
    LeftTurn,
    Straight,
    RightTurn
}