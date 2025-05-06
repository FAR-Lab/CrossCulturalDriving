using Unity.Netcode;
using UnityEngine;

public class DeactivateOthers : NetworkBehaviour {
    // Update is called once per frame
    private void Update() {
    }

    public override void OnNetworkSpawn() {
        if (!IsServer) {
            GetComponent<VehicleController>().enabled = false;

            GetComponent<ForceFeedback>().enabled = false;
            foreach (var wc in GetComponentsInChildren<WheelCollider>()) wc.enabled = false;
        }
    }
}