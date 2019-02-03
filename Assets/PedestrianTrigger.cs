using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PedestrianTrigger : NetworkBehaviour
{
    bool triggered = false;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {


    }
    private void OnTriggerEnter(Collider other)
    {
        if (isServer && !triggered)
        {
            triggered = true;

            foreach (VehicleInputControllerNetworked i in FindObjectsOfType<VehicleInputControllerNetworked>())
            {
                i.RpcStartWallking();
            }


        }
    }
}
