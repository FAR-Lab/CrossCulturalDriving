using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GpsTrigger : MonoBehaviour { //MovoTo2020
    bool triggered=false;
    public GpsController.Direction setDirectionParticipant0;
    public GpsController.Direction setDirectionParticipant1;
    public GpsController.Direction setDirectionParticipant2;
    public GpsController.Direction setDirectionParticipant3;
    public GpsController.Direction setDirectionParticipant4;
    public GpsController.Direction setDirectionParticipant5;
    private void OnTriggerEnter(Collider other) {
        if ( !triggered)
        { //isServer &&
            triggered = true;
            GpsController.Direction[] sendArray ={
                setDirectionParticipant0,
                setDirectionParticipant1,
                setDirectionParticipant2,
                setDirectionParticipant3,
                setDirectionParticipant4,
                setDirectionParticipant5
            };
            foreach (VehicleInputControllerNetworked i in FindObjectsOfType<VehicleInputControllerNetworked>()) {
                i.RpcSetGPS(sendArray);
            }
                 
       
     }
  }
}
