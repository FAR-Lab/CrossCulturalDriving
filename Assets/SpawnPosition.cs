using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class SpawnPosition : MonoBehaviour
{
    public ParticipantOrder StartingId;
    public ConnectionAndSpawing.ParticipantObjectSpawnType SpawnType;

    private Vector3 InitialFacingDirection;

    void Start()
    {
        InitialFacingDirection = transform.position + transform.forward;
    }

    void Update()
    {
        
    }
    


    void OnDrawGizmos()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position,.25f);

        // Draw red line indicating spawn orientation
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 3);

    }
    
    
}
