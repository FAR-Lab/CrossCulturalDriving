using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RVP;
public class BoxMonitor : MonoBehaviour {

    public VehicleParent myVehicle;
	// Use this for initialization
	void Start () {


    }
	
	// Update is called once per frame
	void Update () {
		
	}
    private void OnGUI()
    {
         
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.transform.GetComponent<VehicleParent>())
        {
            if (myVehicle == null)
            {
                myVehicle = collision.transform.GetComponent<VehicleParent>();
            }
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        if (collision.transform.GetComponent<VehicleParent>())
        {
            if (myVehicle == collision.transform.GetComponent<VehicleParent>())
            {
                myVehicle = null;
            }
        }
    }
}
