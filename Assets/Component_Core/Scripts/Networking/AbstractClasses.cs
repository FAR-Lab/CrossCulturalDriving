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
    public static IEnumerable<Type> GetAllImplementations() { //https://stackoverflow.com/a/5411981

        return typeof(Interactable_Object)
            .Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Interactable_Object)) && !t.IsAbstract);
    }
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
    
    public abstract void SetNewNavigationInstruction(Dictionary<ParticipantOrder, NavigationScreen.Direction> Directions);

    
    
    public static Client_Object GetJoinTypeObject() // I am not sure that this is a smart thing todo...
    {
        return null;
        //ToDo: David WTF
        Type[] types = (Type[])GetAllImplementations();
        foreach (Type t in types){
            foreach (var pic in FindObjectsOfType(t)) {
                if (((Client_Object)pic).IsLocalPlayer)
                    return (Client_Object)pic;
            }
        }
        return null;
    }

    public static IEnumerable<Type> GetAllImplementations() { //https://stackoverflow.com/a/5411981

        return typeof(Client_Object)
            .Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Client_Object)) && !t.IsAbstract);
    }
}


