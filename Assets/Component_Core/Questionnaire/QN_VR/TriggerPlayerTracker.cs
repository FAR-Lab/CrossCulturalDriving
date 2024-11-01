using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TriggerPlayerTracker : NetworkBehaviour {
    private bool test = true;
    private Collider boxCollider;

    public float AfterIntersectionXScale = 80;

    public float YRotationLimit = 45f;
    
    private Quaternion _originalRotation;
    private Vector3 _originalScale;

    private void Start() {
        if (!IsServer) {
            Destroy(this);
            return;
        }

        boxCollider = GetComponent<Collider>();

        _originalScale = transform.localScale;
        _originalRotation = transform.localRotation;
    }

    // input angle is from -1 to 1
    public void RotateCollider(float angle){
        angle = Mathf.Clamp(angle, -1, 1);
        angle *= YRotationLimit;
        Vector3 targetRot = new Vector3(_originalRotation.eulerAngles.x, _originalRotation.eulerAngles.y + angle, _originalRotation.eulerAngles.z);

        // lerping rotation
        transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(targetRot), Time.deltaTime * 5f);

    }



    public void ShrinkCollider(){
        transform.localScale = new Vector3(AfterIntersectionXScale, transform.localScale.y, transform.localScale.z);
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
        // Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        // Gizmos.color = Color.red;
        // if (boxCollider != null) {
        //     Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        // }
    }
}
