using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class VRBarrier : MonoBehaviour {
    private MeshFilter m_mesh;
    private MeshRenderer m_renderer;
    private void OnDrawGizmos() {
        m_mesh = GetComponent<MeshFilter>();
        m_renderer = GetComponent<MeshRenderer>();
        Gizmos.color = Color.red;

     //  Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
      //  Gizmos.matrix = rotationMatrix;
     //   Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        
        
       if (m_mesh.sharedMesh != null) {
           for (int i = 0; i < m_mesh.sharedMesh.vertices.Length; i++) {
               Vector3 startPosition = m_mesh.sharedMesh.vertices[i];
               Vector3 endPosition = m_mesh.sharedMesh.vertices[(i + 1) % m_mesh.sharedMesh.vertices.Length];

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
        m_renderer.sharedMaterial.SetFloat("_invertShape",1);
        m_renderer.sharedMaterial.SetColor("_Color",Color.red);
       // Debug.Log("CalledSet solid");
    }

    private void setGrid() {
        m_renderer.sharedMaterial.SetFloat("_invertShape",0);
        
        m_renderer.sharedMaterial.SetColor("_Color",Color.green);
       // Debug.Log("CalledSet Grid");
    }
    

    private void LateUpdate() {
        if (BoundariesSet) {
            if (firstRun) {
                m_renderer = GetComponent<MeshRenderer>();
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
