using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RVP;

public class IntersectionApproachManager : MonoBehaviour {

    BoxMonitor[] collidersList;
    List<VehicleParent> activeVehicles = new List<VehicleParent>();
    //for initialization
	void Start () {

        collidersList= GetComponentsInChildren<BoxMonitor>();


    }
	
	// Update is called once per frame
	void Update () {
        if (collidersList[0].myVehicle != null && collidersList[1].myVehicle != null)
        {
            VehicleParent vp1 = collidersList[0].myVehicle;
            VehicleParent vp2 = collidersList[1].myVehicle;

            float distance1 = (vp1.transform.position - transform.position).magnitude;
            float distance2 = (vp2.transform.position - transform.position).magnitude;

            float speed1 = vp1.velMag;
            float speed2 = vp2.velMag;

            float TTI1 = distance1 / speed1;
            float TTI2 = distance2 / speed2;
            Debug.Log(TTI1 + "  " + TTI2);
        }
	}
}
