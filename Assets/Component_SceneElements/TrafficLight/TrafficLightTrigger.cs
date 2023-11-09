using System;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using UltimateReplay;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TrafficLightTrigger : MonoBehaviour {

    public List<ParticipantOrder> ParticipantsToReactTo = new List<ParticipantOrder>();
    public TLState T_F_ForA;
    public TLState T_F_ForB;
    public TLState T_F_ForC;
    public TLState T_F_ForD;
    public TLState T_F_ForE;
    public TLState T_F_ForF;
    
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

   

        bool triggered=false;
    private void OnTriggerEnter(Collider other) {
        if (!triggered 
            //&& other.GetComponent<Client_Object>()!=null &&(
           // ParticipantsToReactTo.Contains(other.GetComponent<Client_Object>().GetParticipantOrder()) || 
          //  ParticipantsToReactTo.Contains(other.GetComponentInChildren<Client_Object>().GetParticipantOrder()) || 
          //  ParticipantsToReactTo.Contains(other.GetComponentInParent<Client_Object>().GetParticipantOrder())
           // )
            ){
            
            triggered = true;
            var temp =
                new Dictionary<ParticipantOrder, TLState> {
                    { ParticipantOrder.A, T_F_ForA },
                    { ParticipantOrder.B, T_F_ForB },
                    { ParticipantOrder.C, T_F_ForC },
                    { ParticipantOrder.D, T_F_ForD },
                    { ParticipantOrder.E, T_F_ForE },
                    { ParticipantOrder.F, T_F_ForF }
                };
            foreach (var tmp in FindObjectsOfType<TrafficLightController>()) tmp.UpdatedTrafficlight(temp);

        }
    }
}