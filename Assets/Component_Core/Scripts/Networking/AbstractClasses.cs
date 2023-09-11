using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class Interactable_Object : NetworkBehaviour {
    
    public abstract void Stop_Action();
    public abstract void AssignClient(ulong CLID_, ParticipantOrder _participantOrder_);
    public abstract Transform GetCameraPositionObject();
    public abstract void SetStartingPose(Pose _pose);
    public abstract bool HasActionStopped();
    
}


public abstract class Client_Object : NetworkBehaviour {
    public abstract void SetSpawnType(SpawnType _spawnType);
    public abstract void AssignFollowTransform(Interactable_Object MyInteractableObject, ulong targetClient);
    public abstract void De_AssignFollowTransform(ulong clientID,NetworkObject netobj);
    public abstract Transform GetMainCamera();
    public abstract void CalibrateClient(ClientRpcParams clientRpcParams);
    
    public abstract void  StartQuestionair(QNDataStorageServer m_QNDataStorageServer);
}


