using System;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using UltimateReplay;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TrafficLightTrigger : MonoBehaviour
{

    public List<ParticipantOrder> ParticipantsToReactTo = new List<ParticipantOrder>();
    public List<TrafficLights> InternalTfLights = new List<TrafficLights>();

    [Serializable]
    public class TrafficLights
    {
        public TrafficLightController tfLight;
        public TLState newState;
    }

    public enum TLState
    {
        NONE,
        Yellow,
        RED,
        GREEN
    }

    private void Start()
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            Destroy(gameObject);
        }
    }
    private void OnDrawGizmos()
    {
        Matrix4x4 transformMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.matrix = transformMatrix;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(Vector3.zero, transform.localScale);

        // ** Need to transform the matrix back for the lines to work correctly
        Gizmos.matrix = Matrix4x4.identity;

        foreach (TrafficLights tfLight in InternalTfLights)
        {
            if (tfLight.tfLight == null)
            {
                continue;
            }

            Gizmos.color = GetColorForTLState(tfLight.newState);
            Gizmos.DrawLine(transform.position, tfLight.tfLight.transform.position);
        }
    }

    private Color GetColorForTLState(TLState state)
    {
        switch (state)
        {
            case TLState.NONE: return Color.white;
            case TLState.Yellow: return Color.yellow;
            case TLState.RED: return Color.red;
            case TLState.GREEN: return Color.green;
            default: return Color.white;
        }
    }

    // for manually update the trigger
    [ContextMenu("Test")]
    public void Test(){
        foreach (TrafficLights tfLight in InternalTfLights)
        {
            tfLight.tfLight.UpdatedTrafficlight(new Dictionary<ParticipantOrder, TLState>() { { ParticipantOrder.A, tfLight.newState } });
        }
    }


    bool triggered = false;
    private void OnTriggerEnter(Collider other)
    {
        if (!triggered)
        {
            ParticipantOrder enteredParticipant = other.GetComponent<NetworkVehicleController>().getParticipantOrder();
            if (ParticipantsToReactTo.Contains(enteredParticipant))
            {
                foreach (TrafficLights tfLight in InternalTfLights)
                {
                    tfLight.tfLight.UpdatedTrafficlight(new Dictionary<ParticipantOrder, TLState>() { { enteredParticipant, tfLight.newState } });
                }
            }
            triggered = true;
        }
    }
}