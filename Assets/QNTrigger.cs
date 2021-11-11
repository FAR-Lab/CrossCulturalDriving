using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class QNTrigger : MonoBehaviour
{
    private ScenarioManager scenMen;
    // Start is called before the first frame update
    void Start()
    {
        if (NetworkManager.Singleton.IsServer)
        {


            scenMen = transform.parent.GetComponentInParent<ScenarioManager>();
            if (scenMen == null)
            {
                Debug.LogError("Could not find a scenario manager to call when I get triggered");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("On trigger enter with from the server detected."+other.transform.parent.name);
            
            if (other.transform.GetComponentInParent<NetworkVehicleController>() != null)
            {
                Debug.Log("Found a car so I am telling the server to switch to QNs");
                ConnectionAndSpawing.Singleton.SwitchToQN();
            }
        }
    }
}
