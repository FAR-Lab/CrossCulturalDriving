using System;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using UltimateReplay;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TrafficLightTrigger : MonoBehaviour {

    public List<ParticipantOrder> ParticipantsToReactTo = new List<ParticipantOrder>();
    public List<TrafficLights> InternalTfLights = new List<TrafficLights>();

    public struct TrafficLights {
        public TrafficLightController tfLight;
        public TLState newState;
    }
    public enum TLState {
        NONE,
        IDLE,
        RED,
        GREEN
    }

    private void Start() {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer) {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmos() {
        
        //ToDo: draw a line from the collider center to the traffic lights that are affected.. the color of the line should be hte "new state"
    }


    bool triggered=false;
    private void OnTriggerEnter(Collider other) {
        if (!triggered 
            
            ){
            
            triggered = true;
          
            
        }
    }
}