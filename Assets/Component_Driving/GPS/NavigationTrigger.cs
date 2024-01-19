//#define DEBUGSHADYCODE
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NavigationTrigger : MonoBehaviour {

    public List<ParticipantOrder> ParticipantsToReactTo = new List<ParticipantOrder>();
    public NavigationScreen.Direction setDirectionParticipantA;
    public NavigationScreen.Direction setDirectionParticipantB;
    public NavigationScreen.Direction setDirectionParticipantC;
    public NavigationScreen.Direction setDirectionParticipantD;
    public NavigationScreen.Direction setDirectionParticipantE;
    public NavigationScreen.Direction setDirectionParticipantF;
    public bool triggered;

    private void Start() {
        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer &&
            !NetworkManager.Singleton.IsHost)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other) {


        if (!triggered) {

            ParticipantOrder incoming = ParticipantOrder.None;
            bool success = false;
            foreach (var t in Client_Object.GetAllImplementations()) {
          #if DEBUGSHADYCODE      
                Debug.Log($"Found type{t.FullName}");
#endif
                var elem = other.transform.GetComponent(t);
#if DEBUGSHADYCODE  
                Debug.Log($"Found elem{elem}");
#endif
                if (elem != null) {
                    incoming= ((Client_Object)elem).GetParticipantOrder();
#if DEBUGSHADYCODE
                    Debug.Log($"Cast worked and po is {incoming}");
#endif
                    success = true;
                    break;
                }
            }

            if (!success) {
                foreach (var t in Interactable_Object.GetAllImplementations()) {
#if DEBUGSHADYCODE
                    Debug.Log($"Found type{t.FullName}");
#endif
                    var elem = other.transform.GetComponent(t);
                    if (other.transform.parent != null) {
                        elem = other.transform.GetComponentInParent(t);
                    }
#if DEBUGSHADYCODE
                    Debug.Log($"Found elem{elem}");
#endif
                    if (elem != null) {
                        var io = (Interactable_Object)elem;
                        if (io != null && io.m_participantOrder != null) {
                            incoming = ((Interactable_Object)elem).m_participantOrder.Value;
#if DEBUGSHADYCODE
                            Debug.Log($"Cast worked and po is {incoming}");
                    #endif        
                            success = true;
                            break;
                        }
                    }
                }
            }

            if (!success) {
                return;
            }

            if (ParticipantsToReactTo.Contains(incoming)) {
                triggered = true;
               
                var temp =
                    new Dictionary<ParticipantOrder, NavigationScreen.Direction> {
                        { ParticipantOrder.A, setDirectionParticipantA },
                        { ParticipantOrder.B, setDirectionParticipantB },
                        { ParticipantOrder.C, setDirectionParticipantC },
                        { ParticipantOrder.D, setDirectionParticipantD },
                        { ParticipantOrder.E, setDirectionParticipantE },
                        { ParticipantOrder.F, setDirectionParticipantF }
                    };
                Debug.Log($"We triggered for Participant A We want to play{temp[ParticipantOrder.A]}");
                ConnectionAndSpawning.Singleton.GetScenarioManager().UpdateAllNavigationInstructions(temp);
            }
        }
    }

}