using UnityEngine;
using System.Collections;

public class waypoint : MonoBehaviour
{
    public float targetSpeed;
    public Vector3 position;

   
    private bool GizmoDrawing = false;
    // Use this for initialization
    void Awake()
    {
        position = transform.position;
    }
    private void Start()
    {
       
    }
    public waypoint getNextWaypoint()
    {

        waypoint nextOne = null;

        if (transform.parent.GetComponent<lane>() != null)
        {
            nextOne = transform.parent.GetComponent<lane>().getNextWaypoint(this);
        }

        return nextOne;


    }
    public waypoint getPreviousWaypoint()
    {

        waypoint previousOne = null;

        if (transform.parent.GetComponent<lane>() != null)
        {// These version is 
            previousOne = transform.parent.GetComponent<lane>().getPreviousWaypoint(this);
        }

        return previousOne;


    }
    public waypoint changeLaneTo(int laneNumbe)
    {
        if (transform.parent.GetComponent<lane>() != null)
        {
            return transform.parent.GetComponent<lane>().changeLane(this, laneNumbe);
        }
        else return null;

    }
    public float getWaypointSpeed()
    {


        return targetSpeed;

    }
    void Update()
    {

    }

    void OnDrawGizmos()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.white;

        Gizmos.DrawCube(transform.position + (Vector3.up), new Vector3(1, 2, 1));

        Gizmos.color = Color.green;
        float height = scale(0, 60, 0, 7, targetSpeed);

        Gizmos.DrawCube(transform.position + (Vector3.up * (2 + height / 2)), new Vector3(1, height, 1));


    }
    void OnDrawGizmosSelected()
    {

        // Draws a blue line from this transform to the target
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position + (Vector3.up), new Vector3(1, 2, 1));
        Gizmos.color = Color.white;
        Gizmos.DrawRay(transform.position, -transform.up * 10);


    }

    public static float scale(float OldMin, float OldMax, float NewMin, float NewMax, float OldValue)
    {

        float OldRange = (OldMax - OldMin);
        float NewRange = (NewMax - NewMin);
        float NewValue = (((OldValue - OldMin) * NewRange) / OldRange) + NewMin;

        return (NewValue);
    }

}