using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GpsTrigger : MonoBehaviour {
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
            ConnectionAndSpawning.Singleton.UpdateAllGPS(temp);
        }
    }
}