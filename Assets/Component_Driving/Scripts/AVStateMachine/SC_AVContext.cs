using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SC_AVContext : MonoBehaviour {
    public Vector3 centerPos;
    public BoxCollider conflictZone;
        
    [SerializeField]private VehicleController otherVehicleController;
    [SerializeField]private NetworkVehicleController otherNetworkVehicleController;

    [SerializeField]private VehicleController myVehicleController;
    [SerializeField]private NetworkVehicleController myNetworkVehicleController;

    private float _speed;

    #region Debug

    [SerializeField] private float distanceToCenter;

    #endregion
    
    private void Update() {
        if (myNetworkVehicleController == null) {
            return;
        }
        
        distanceToCenter = GetDistanceToCenter(myNetworkVehicleController);
    }
    
    public void AssignVehicles() {
        myVehicleController = GetComponent<VehicleController>();
        myNetworkVehicleController = GetComponent<NetworkVehicleController>();
        
        Interactable_Object obj = ConnectionAndSpawning.Singleton.GetInteractableObject_For_Participant(ParticipantOrder.A);

        if (obj != null) {
            otherVehicleController = obj.GetComponent<VehicleController>();
            otherNetworkVehicleController = obj.GetComponent<NetworkVehicleController>();
        }
    }

    public NetworkVehicleController GetMyNetworkVehicleController() {
        return myNetworkVehicleController;
    }
    
    public NetworkVehicleController GetOtherNetworkVehicleController() {
        return otherNetworkVehicleController;
    }
    
    public float GetDistanceToCenter(NetworkVehicleController vehicleController) {
        return Vector3.Distance(centerPos, vehicleController.transform.position);
    } 
    
    public float GetDistanceBetween(NetworkVehicleController vehicleController1, NetworkVehicleController vehicleController2) {
        return Vector3.Distance(vehicleController1.transform.position, vehicleController2.transform.position);
    }
    
    public bool IsConflictZoneClear() {
        if (conflictZone.bounds.Contains(otherVehicleController.transform.position)) {
            return false;
        }
        else if (conflictZone.bounds.Contains(myVehicleController.transform.position)) {
            return false;
        }
        return true;
    }
    
    public void SetSpeed(float speed) {
        _speed = speed;
    }
    
    public float GetSpeed() {
        return _speed;
    }
    
    
    
}
