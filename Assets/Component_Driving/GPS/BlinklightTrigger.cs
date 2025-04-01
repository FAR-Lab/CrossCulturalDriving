using System;
using Unity.Netcode; 
using UnityEngine;

public class BlinklightTrigger : MonoBehaviour
{
    private BoxCollider collider;

    public enum Direction {
        Left,
        Right,
    }

    public Direction BlinkDirection;

    private void OnValidate() {
        collider = GetComponent<BoxCollider>();
    }

    private void OnDrawGizmos() {
        if (collider != null) {
            Gizmos.color = Color.red;
            // Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (!NetworkManager.Singleton.IsServer) return; 
        NetworkVehicleController vehicleController = other.GetComponentInParent<NetworkVehicleController>();
        if (vehicleController != null) {
            if (vehicleController.VehicleMode == NetworkVehicleController.VehicleOpperationMode.AUTONOMOUS) {
                vehicleController.SetBlinkLight(BlinkDirection);
            }
        }
    }

    private void OnTriggerExit(Collider other) {
        if (!NetworkManager.Singleton.IsServer) return; 
        NetworkVehicleController vehicleController = other.GetComponentInParent<NetworkVehicleController>();
        if (vehicleController != null) {
            if (vehicleController.VehicleMode == NetworkVehicleController.VehicleOpperationMode.AUTONOMOUS) {
                vehicleController.StopIndicating();
            }
        }
    }
}
