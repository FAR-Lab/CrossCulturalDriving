using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class lane : MonoBehaviour
{

    public List<waypoint> Direction = new List<waypoint>();
    private waypointStreetManagerV2 parent;
    public int laneNumber; // lane numbers are the number assosiated with each lane counted inwards out from the barrier.
    public int myDirection;
    public void Start()
    {
        parent = transform.GetComponentInParent<waypointStreetManagerV2>();
    }

    public Vector3 getDirectionVector()
    {
        return (Direction[Direction.Count - 1].position - Direction[0].position);
    }

    public waypoint getLastRelevant()
    {
        return Direction[Direction.Count - 1];

    }

    public waypoint getFirstRelevant()
    {
        return Direction[0];

    }

    public waypoint changeLane(waypoint currentWaypoint, int targetLaneNumber)
    {
        if (targetLaneNumber != laneNumber)
        {
            return parent.getclosestPointOnLane(targetLaneNumber, myDirection, currentWaypoint.position);

        }
        else
        {
            return currentWaypoint;
        }

    }


    public waypoint getPreviousWaypoint(waypoint currentWaypoint)
    {
        if (Direction.Contains(currentWaypoint))
        {
            if (Direction.IndexOf(currentWaypoint) >= 1)
            {
                return Direction[Direction.IndexOf(currentWaypoint) - 1];
            }
            else
            {
                waypoint returnCandidat = parent.getNextLane(currentWaypoint);
                if ((returnCandidat.position - currentWaypoint.position).magnitude < 1.5)
                {
                    returnCandidat = returnCandidat.getNextWaypoint();
                }

                return returnCandidat;
            }

        }
        else
        {
            Debug.Log("This should not be happening! The waypoint does not belong to this street");
            return null;
        }

    }

    public waypoint getNextWaypoint(waypoint currentWaypoint)
    {

        if (Direction.Contains(currentWaypoint))
        {
            if (Direction.IndexOf(currentWaypoint) < Direction.Count - 1)
            {
                return Direction[Direction.IndexOf(currentWaypoint) + 1];
            }
            else
            {
                waypoint returnCandidat = parent.getNextLane(currentWaypoint);
                //Debug.Log (returnCandidat.position);
                if (returnCandidat == null)
                {
                    return null;
                }
                else
                {
                    if ((returnCandidat.position - currentWaypoint.position).magnitude < 1.5)
                    {
                        returnCandidat = returnCandidat.getNextWaypoint();
                    }

                    return returnCandidat;
                }

            }

        }
        else
        {
            Debug.Log("This should not be happening! The waypoint does not belong to this street");
            return null;
        }

    }


    public waypoint getClosestWaypoint(Vector3 pos)
    {

        //Debug.Log("Target Layer is A input is: " +layer);
        float distance = (Direction[0].position - pos).magnitude;
        int counter = 0;
        int i = 0;
        foreach (waypoint wp in Direction)
        {
            float NewDistance = (wp.position - pos).magnitude;
            if (distance > NewDistance)
            {
                distance = NewDistance;
                counter = i;
            }
            i++;
        }
        return Direction[counter];





    }
    ///----///
    ///GIZMO draw code
    void OnDrawGizmos()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.white;
        for (int i = 0; i < Direction.Count; i++)
        {

            if (i < (Direction.Count - 1))
            {
                Gizmos.DrawLine(Direction[i].transform.position, Direction[i + 1].transform.position);
            }
        }


    }


}