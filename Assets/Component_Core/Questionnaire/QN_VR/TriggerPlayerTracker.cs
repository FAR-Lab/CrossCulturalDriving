using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerPlayerTracker : MonoBehaviour {
    private bool test = true;
    private BoxCollider boxCollider;
    
    private void Start() {
        boxCollider = GetComponent<BoxCollider>();
    }
    
    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.CompareTag("Player")) {
            test = false;
           
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.gameObject.CompareTag("Player")) {
            test = true;
           
        }
    }

    public bool IsFrontClear() {
        return test;
    }

    private void OnDrawGizmos() {
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.color = Color.red;
        if (boxCollider != null) {
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
    }
}
