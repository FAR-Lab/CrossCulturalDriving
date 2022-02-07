using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class SpawnPosition : MonoBehaviour
{
    public ParticipantOrder StartingId;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    void OnDrawGizmos()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position,.25f);
    }
    
    
}
