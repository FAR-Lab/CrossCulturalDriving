using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlinklightTrigger : MonoBehaviour
{
    private BoxCollider collider;
    
    public enum Direction {
        Left,
        Right,
        Stop
    }

    public Direction BlinkDirection;

    private void OnValidate() {
        collider = GetComponent<BoxCollider>();
    }

    private void OnDrawGizmos() {
        if (collider != null) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
        }
    }
}
