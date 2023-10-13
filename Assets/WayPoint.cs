using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class WayPoint : MonoBehaviour
{
    [SerializeField]
    public WayPoint nextWayPoint;
   
    [SerializeField]
    [Range(0.3f,3f)]
    public float ArrivalRange;
    
    [SerializeField]
    ///in MPH
    public float targetSpeed;
    [SerializeField]
    public bool IndicateLeft;
    [SerializeField]
    public bool IndicateRight;
    [SerializeField]
    public bool Horn;
    // Start is called before the first frame update
    public bool isLastWaypoint { private set; get; }

    private void Start()
    {
        if (nextWayPoint == null)
        {
            isLastWaypoint = true;
        }
}

    public void OnDrawGizmos()
    {
        Gizmos.DrawSphere(transform.position,0.25f);
        if (nextWayPoint != null)
        {
            Gizmos.DrawLine(transform.position,nextWayPoint.transform.position);
        }
        Gizmos.DrawWireSphere(transform.position,ArrivalRange);
    }

    
}
