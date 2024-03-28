using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawErrorEditor : MonoBehaviour {
 private Transform child;
 public float distance;
 private void OnDrawGizmos() {
  if (child == null) {
   child = transform.GetChild(0);
  }
  else {
   Gizmos.color=Color.green;
   Gizmos.DrawLine(transform.position, child.position);
   Gizmos.color=Color.red;
   Gizmos.DrawRay(transform.position,Vector3.up*5);
   Gizmos.DrawRay(child.position,Vector3.up*5);
   
   distance = (transform.position - child.position).magnitude;

  }
 }
}
