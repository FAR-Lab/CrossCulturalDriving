using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkSpawnGizmo : MonoBehaviour {

   
    void Start()
    {
        RaycastHit hit;
        if(Physics.Raycast(transform.position,-transform.up,out hit)){
            transform.position = hit.point + Vector3.up * SceneStateManager.Instance.spawnHeight;
        }
    }
    void OnDrawGizmos()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.white;
        Gizmos.DrawCube(transform.position, new Vector3(.9f,.9f,.9f));
        Gizmos.DrawRay(transform.position, Vector3.down * 5);
    }
    void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.green;
        Gizmos.DrawCube(transform.position, new Vector3(1.1f, 1.1f, 1.1f));
        Gizmos.DrawRay(transform.position, Vector3.down * 5);
    }

}
