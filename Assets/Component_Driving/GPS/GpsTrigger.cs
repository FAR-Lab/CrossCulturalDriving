using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GpsTrigger : MonoBehaviour
{

    public List<ParticipantOrder> ParticipantsToReactTo = new List<ParticipantOrder>();
    public NavigationScreen.Direction setDirectionParticipantA;
    public NavigationScreen.Direction setDirectionParticipantB;
    public NavigationScreen.Direction setDirectionParticipantC;
    public NavigationScreen.Direction setDirectionParticipantD;
    public NavigationScreen.Direction setDirectionParticipantE;
    public NavigationScreen.Direction setDirectionParticipantF;
    private bool triggered;

    private void Start() {
        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer &&
            !NetworkManager.Singleton.IsHost)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other) {
        if (!triggered && 
            other.GetComponent<Client_Object>()!=null &&
            ParticipantsToReactTo.Contains(other.GetComponent<Client_Object>().GetParticipantOrder())) {
            Debug.Log("Got A Trigger event starting new GPS direction!");
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
            ConnectionAndSpawning.Singleton.GetScenarioManager().UpdateAllGPS(temp);
        }
    }
}