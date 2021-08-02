using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIRaycast : MonoBehaviour
{
    public GameObject lastHit;
    public Vector3 collision = Vector3.zero;
    public LayerMask layer;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var ray = new Ray(this.transform.position, this.transform.forward);
        Vector3 noAngle = this.transform.forward;
        Quaternion leftSpreadAngle = Quaternion.AngleAxis(-30, new Vector3(0, 1, 0));
        Quaternion rightSpreadAngle = Quaternion.AngleAxis(30, new Vector3(0, 1, 0));
        Vector3 leftVector = leftSpreadAngle * noAngle;
        Vector3 rightVector = rightSpreadAngle * noAngle;
        var leftRay = new Ray(this.transform.position, leftVector);
        var rightRay = new Ray(this.transform.position, rightVector);
        RaycastHit leftHit;
        RaycastHit rightHit;
        RaycastHit hit;
        if (Physics.Raycast(leftRay, out leftHit, 100, layer)) {
            lastHit = leftHit.transform.gameObject;
            collision = leftHit.point;
        }
        else if (Physics.Raycast(ray, out hit, 100, layer)) {
            lastHit = hit.transform.gameObject;
            collision = hit.point;
        } 
        else if (Physics.Raycast(rightRay, out rightHit, 100, layer)) {
            lastHit = rightHit.transform.gameObject;
            collision = rightHit.point;
        }
        else {
            collision = Vector3.zero;
        }
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(collision, 2.2f);
    }
}
