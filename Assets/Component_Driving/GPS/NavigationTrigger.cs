//#define DEBUGSHADYCODE

using System;
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


    private List<Transform> ObjectsToTrack = new List<Transform>();
    private BoxCollider m_boxcollider;

    private void Start() {
        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer &&
            !NetworkManager.Singleton.IsHost) {
            Destroy(gameObject);
        }
        else {
            m_boxcollider = GetComponent<BoxCollider>();
            foreach (Client_Object participant in Client_Object.Instances) {
                if (ParticipantsToReactTo.Contains(participant.GetParticipantOrder())) {
                    ObjectsToTrack.Add(participant.GetMainCamera());
                }
            }
        }
    }

    private void Update() {
        foreach (var t in ObjectsToTrack) {
            if (!triggered && m_boxcollider.bounds.Contains(t.position)) {
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
                Debug.Log($"We triggered for Participant We want to play{temp[ParticipantOrder.A]}");
                ConnectionAndSpawning.Singleton.GetScenarioManager().UpdateAllNavigationInstructions(temp);
            }
        }
    }
}