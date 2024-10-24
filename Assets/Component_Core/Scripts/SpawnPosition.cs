using UnityEditor;
using UnityEngine;


public class SpawnPosition : MonoBehaviour {

    public static string SpawnPointsJSONLocation = Application.dataPath + "/SpawnPointsJSON.json";
    
    
    
    public ParticipantOrder StartingId;

 

    // Initial facing of the participant. Not exposed. Just change the transform of the spawnpoint to adjust.
    private Vector3 InitialFacingDirection;

    private void Start() {
        InitialFacingDirection = transform.position + transform.forward;
    }

    // visualization
    private void OnDrawGizmos() {
       

        // Draw red line indicating spawn orientation
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 7);
        Vector3 pos = transform.position;
        pos.y /= 2;
        Vector3 size = Vector3.one;
        size.x *= 0.15f;
        size.y =transform.position.y;
        size.z *= 0.15f;
        Gizmos.DrawCube(pos,size);
        // Draw a yellow sphere at the transform's position
        switch (StartingId)
        {
            case ParticipantOrder.A:
                Gizmos.color = Color.red;
                break;
            case ParticipantOrder.B:
                Gizmos.color = Color.blue;
                break;
            default:
                Gizmos.color = Color.black;
                break;
        }
        Gizmos.DrawSphere(transform.position, 2.5f);
    }
}
