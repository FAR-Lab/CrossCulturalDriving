using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntersectionCenter : MonoBehaviour
{
    private BoxCollider _conflictZone {get { return GetComponent<BoxCollider>(); }}

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, _conflictZone.size);
    }
}
