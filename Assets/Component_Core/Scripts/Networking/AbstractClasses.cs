using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class Interactable_Object : NetworkBehaviour {
    private static readonly List<Interactable_Object>
        instances = new(); // Can cause memory leakage if not kept clean...!!! 

    public NetworkVariable<ParticipantOrder> m_participantOrder = new();

    protected Interactable_Object() {
        // Add this instance to the list upon object creation
        instances.Add(this);
    }

    public static IReadOnlyList<Interactable_Object> Instances => instances.AsReadOnly();

    protected virtual void OnDestroy() {
        // Remove this instance from the list when it's destroyed
        instances.Remove(this);
    }

    public abstract void Stop_Action();
    public abstract void AssignClient(ulong CLID_, ParticipantOrder _participantOrder_);
    public abstract Transform GetCameraPositionObject();
    public abstract void SetStartingPose(Pose _pose);
    public abstract bool HasActionStopped();
}

public abstract class Client_Object : NetworkBehaviour {
    private static readonly List<Client_Object> instances = new(); // Can cause memory leakage if not kept clean...!!! 

    protected Client_Object() {
        // Add this instance to the list upon object creation
        instances.Add(this);
    }

    public static IReadOnlyList<Client_Object> Instances => instances.AsReadOnly();

    protected virtual void OnDestroy() {
        // Remove this instance from the list when it's destroyed
        instances.Remove(this);
    }

    public abstract void SetParticipantOrder(ParticipantOrder _ParticipantOrder);
    public abstract ParticipantOrder GetParticipantOrder();
    public abstract void SetSpawnType(SpawnType _spawnType);
    public abstract void AssignFollowTransform(Interactable_Object MyInteractableObject, ulong targetClient);
    public abstract Interactable_Object GetFollowTransform();

    public abstract void De_AssignFollowTransform(ulong clientID, NetworkObject netobj);
    public abstract Transform GetMainCamera();

    public abstract void StartQuestionair(QNDataStorageServer m_QNDataStorageServer);

    public abstract void GoForPostQuestion();

    public abstract void SetNewNavigationInstruction(
        Dictionary<ParticipantOrder, NavigationScreen.Direction> Directions);
}