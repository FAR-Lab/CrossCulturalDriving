using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public abstract class Interactable_Object : NetworkBehaviour {

    public NetworkVariable<ParticipantOrder> m_participantOrder = new NetworkVariable<ParticipantOrder>();

    public abstract void Stop_Action();
    public abstract void AssignClient(ulong CLID_, ParticipantOrder _participantOrder_);
    public abstract Transform GetCameraPositionObject();
    public abstract void SetStartingPose(Pose _pose);
    public abstract bool HasActionStopped();
    
     
    private static List<Interactable_Object> instances = new List<Interactable_Object>(); // Can cause memory leakage if not kept clean...!!! 
    protected Interactable_Object() {
        // Add this instance to the list upon object creation
        instances.Add(this);
    }

    protected virtual void OnDestroy() {
        // Remove this instance from the list when it's destroyed
        instances.Remove(this);
    }

    public static IReadOnlyList<Interactable_Object> Instances => instances.AsReadOnly();
 
}

public abstract class Client_Object : NetworkBehaviour {
    
    private static List<Client_Object> instances = new List<Client_Object>(); // Can cause memory leakage if not kept clean...!!! 
    protected Client_Object() {
        // Add this instance to the list upon object creation
        instances.Add(this);
    }

    protected virtual void OnDestroy() {
        // Remove this instance from the list when it's destroyed
        instances.Remove(this);
    }

    public static IReadOnlyList<Client_Object> Instances => instances.AsReadOnly();

    public abstract void SetParticipantOrder(ParticipantOrder _ParticipantOrder);
    public abstract ParticipantOrder GetParticipantOrder();
    public abstract void SetSpawnType(SpawnType _spawnType);
    public abstract void AssignFollowTransform(Interactable_Object MyInteractableObject, ulong targetClient);
    public abstract Interactable_Object GetFollowTransform();

    public abstract void De_AssignFollowTransform(ulong clientID,NetworkObject netobj);
    public abstract Transform GetMainCamera();
    
    //Once finished calibration return a True to the provided function. on the server we then know calibration was successful. false for calibration failed.
    public abstract void CalibrateClient(Action<bool> calibrationFinishedCallback);
    
    public abstract void  StartQuestionair(QNDataStorageServer m_QNDataStorageServer);
    
    public abstract void  GoForPostQuestion(); 
    
    public abstract void SetNewNavigationInstruction(Dictionary<ParticipantOrder, NavigationScreen.Direction> Directions);

    
}


