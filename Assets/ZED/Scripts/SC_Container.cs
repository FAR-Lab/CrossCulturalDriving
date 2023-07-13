using System;
using UnityEngine;

public class SC_Container : MonoBehaviour
{
    public GameObject hip;         
    public GameObject VRcam;
    public GameObject head;
    public float inti_height = -1f;
    public bool fix_height = false;

    private void Start() {
        VRcam = GetComponentInChildren<Camera>().gameObject;
    }

    void LateUpdate()
    {
        // overwrite position based on head
        if(fix_height){
            if (inti_height >= 0) {
                transform.position = new Vector3(head.transform.position.x,inti_height,head.transform.position.z);
            }
        }
        else{
            transform.position = new Vector3(head.transform.position.x,head.transform.position.y,head.transform.position.z);
        }
    }


    public void Calibrate() {
        inti_height = head.transform.position.y;

        // rotation is based on hip
        float hipRotY = hip.transform.localRotation.eulerAngles.y;
        float camRotY = VRcam.transform.localRotation.eulerAngles.y;
        float rotationDifferenceY = hipRotY - camRotY;
        Vector3 containerRotation = new Vector3(0, rotationDifferenceY, 0);
        transform.localRotation = Quaternion.Euler(containerRotation);
    }

}