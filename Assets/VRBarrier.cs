using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRBarrier : MonoBehaviour
{
    private void OnDrawGizmos() {
        Gizmos.color = Color.red;

        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
        Gizmos.matrix = rotationMatrix;

        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}
