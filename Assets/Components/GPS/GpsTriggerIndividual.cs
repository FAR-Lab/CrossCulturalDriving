using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GpsTriggerIndividual : MonoBehaviour {

//TODO: @Ryan this GPS trigger shall only update one participants GPS screen
// selected in the script dropdown and only if the correct participant passes it (compare to QNTrigger.cs)
    bool triggered = false;
    public GpsController.Direction setDirectionParticipantA;
    public GpsController.Direction setDirectionParticipantB;
    public GpsController.Direction setDirectionParticipantC;
    public GpsController.Direction setDirectionParticipantD;
    public GpsController.Direction setDirectionParticipantE;
    public GpsController.Direction setDirectionParticipantF;

    private void Start() {
        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer &&
            !NetworkManager.Singleton.IsHost) { Destroy(gameObject); }
    }

    private void OnTriggerEnter(Collider other) {
        if (!triggered) {
            Debug.Log("Got A Trigger event starting new GPS direction!");
            triggered = true;
            Dictionary<ParticipantOrder, GpsController.Direction> temp =
                new Dictionary<ParticipantOrder, GpsController.Direction>() {
                    {ParticipantOrder.A, setDirectionParticipantA},
                    {ParticipantOrder.B, setDirectionParticipantB},
                    {ParticipantOrder.C, setDirectionParticipantC},
                    {ParticipantOrder.D, setDirectionParticipantD},
                    {ParticipantOrder.E, setDirectionParticipantE},
                    {ParticipantOrder.F, setDirectionParticipantF}
                };
            ConnectionAndSpawing.Singleton.UpdateAllGPS(temp);
        }
    }
}