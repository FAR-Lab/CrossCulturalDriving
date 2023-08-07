using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPosition : MonoBehaviour
{
    public ParticipantOrder StartingId;

    // Spawn type will determine what kind of participant will spawn at this spawn point
    public ConnectionAndSpawing.ParticipantObjectSpawnType SpawnType = ConnectionAndSpawing.ParticipantObjectSpawnType.MAIN;

    // Initial facing of the participant. Not exposed. Just change the transform of the spawnpoint to adjust.
    private Vector3 InitialFacingDirection;

    void Start()
    {
        InitialFacingDirection = transform.position + transform.forward;
    }

    // visualization
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
