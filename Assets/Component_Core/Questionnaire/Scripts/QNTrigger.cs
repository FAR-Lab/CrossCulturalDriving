using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class QNTrigger : MonoBehaviour
{
    private ScenarioManager scenarioManager;

    public ParticipantOrder StartingId;

    public bool UseParticipantStopSign = true;

    // Start is called before the first frame update
    void Start(){
        if (NetworkManager.Singleton.IsServer){
            scenarioManager = transform.parent.GetComponentInParent<ScenarioManager>();
            if (scenarioManager == null){
                Debug.LogError("Could not find a scenario manager to call when I get triggered");
            }
        }
        else{
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update(){ }

 

    private void OnTriggerEnter(Collider other){
        if (NetworkManager.Singleton.IsServer) {
            return; //TODO: THIS IS BAD DAVID!!
            if (StartingId == ParticipantOrder.None){
                if (other.transform.GetComponentInParent<NetworkVehicleController>() != null){
                    Debug.Log("Found a car so I am telling the server to switch to QNs");

                    if (UseParticipantStopSign){
                       
                        ConnectionAndSpawning.Singleton.AwaitQN();
                    }
                    else{
                        ConnectionAndSpawning.Singleton.SwitchToQN();
                    }
                }
            }
            else if (other.transform.GetComponentInParent<NetworkVehicleController>().getParticipantOrder() ==
                     StartingId){
                Debug.Log("Found the matching car so I am telling the server to switch to QNs");


                if (UseParticipantStopSign){
                    ConnectionAndSpawning.Singleton.AwaitQN();
                   
                }
                else{
                    ConnectionAndSpawning.Singleton.SwitchToQN();
                }
            }
        }
    }
}