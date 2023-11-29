using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class Interactable_Object : NetworkBehaviour {

    public ParticipantOrder m_participantOrder { get; internal set; }

    public abstract void Stop_Action();
    public abstract void AssignClient(ulong CLID_, ParticipantOrder _participantOrder_);
    public abstract Transform GetCameraPositionObject();
    public abstract void SetStartingPose(Pose _pose);
    public abstract bool HasActionStopped();
    
}

public abstract class Client_Object : NetworkBehaviour {
    public abstract void SetParticipantOrder(ParticipantOrder _ParticipantOrder);
    public abstract ParticipantOrder GetParticipantOrder();
    public abstract void SetSpawnType(SpawnType _spawnType);
    public abstract void AssignFollowTransform(Interactable_Object MyInteractableObject, ulong targetClient);
    public abstract Interactable_Object GetFollowTransform();

    public abstract void De_AssignFollowTransform(ulong clientID,NetworkObject netobj);
    public abstract Transform GetMainCamera();
    public abstract void CalibrateClient();
    
    public abstract void  StartQuestionair(QNDataStorageServer m_QNDataStorageServer);
    
    public abstract void  GoForPostQuestion(); 
    
    public static Client_Object GetJoinTypeObject()
    {
        foreach (var pic in FindObjectsOfType<Client_Object>())
            if (pic.IsLocalPlayer)
                return pic;
        return null;
    }
    
}


