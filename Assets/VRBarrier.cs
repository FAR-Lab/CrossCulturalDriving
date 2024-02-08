using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class VRBarrier : MonoBehaviour {
    private MeshFilter mesh;
    private MeshRenderer renderer;
    private void OnDrawGizmos() {
        mesh = GetComponent<MeshFilter>();
        renderer = GetComponent<MeshRenderer>();
        Gizmos.color = Color.red;

     //  Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
      //  Gizmos.matrix = rotationMatrix;
     //   Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        
        
       if (mesh.sharedMesh != null) {
           for (int i = 0; i < mesh.sharedMesh.vertices.Length; i++) {
               Vector3 startPosition = mesh.sharedMesh.vertices[i];
               Vector3 endPosition = mesh.sharedMesh.vertices[(i + 1) % mesh.sharedMesh.vertices.Length];

               Gizmos.DrawLine(transform.TransformPoint(startPosition), transform.TransformPoint(endPosition));
           }
       }

    }
    private bool BoundariesSet = false;
    private bool firstRun = true;
    public Transform m_trackingTransform;
    private float localDistance;
    public void trackingTransform(Transform trackingTransform) {
       
        if (trackingTransform != null) {
            BoundariesSet = true;
            firstRun = true;
            m_trackingTransform = trackingTransform;
                
                
        }
    }

    private  void setSolid() {
        renderer.sharedMaterial.SetFloat("_invertShape",1);
        renderer.sharedMaterial.SetColor("_Color",Color.red);
       // Debug.Log("CalledSet solid");
    }

    private void setGrid() {
        renderer.sharedMaterial.SetFloat("_invertShape",0);
        
        renderer.sharedMaterial.SetColor("_Color",Color.green);
       // Debug.Log("CalledSet Grid");
    }
    

    private void LateUpdate() {
        if (BoundariesSet) {
            if (firstRun) {
                renderer = GetComponent<MeshRenderer>();
                localDistance = transform.localPosition.magnitude;
                firstRun = false;
            }
            else {
                Vector3 localheadpose =
                    transform.InverseTransformPoint(m_trackingTransform.position + transform.forward * 0.3f);

                Vector3 fwd = transform.forward;
                //Debug.Log($"localheadpose: {localheadpose.z} transfomr.localPositon.z{transform.localPosition.z}");
                if (localheadpose.z > 0) {

                    fwd *= localheadpose.z;
                }
                else if (transform.localPosition.magnitude >= localDistance && localheadpose.z < 0) {
                    fwd *= -(transform.localPosition.magnitude - localDistance) * Time.deltaTime*2f;
                }

               // Debug.Log($"localheadpose.z:{localheadpose.z} transform.localPosition.magnitude:{transform.localPosition.magnitude} localDistance:{localDistance}");
                if (localheadpose.z>0 || transform.localPosition.magnitude > localDistance+0.25f) {
                    setSolid();
                }
                else if(localheadpose.z<0 )  {
                    setGrid();
                }

                transform.position += fwd;
            }
        }
    }
}
