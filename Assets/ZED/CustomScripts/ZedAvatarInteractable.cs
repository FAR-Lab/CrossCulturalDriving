using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ZedAvatarInteractable : Interactable_Object {
    
    private ulong m_ClientID;
 
    private Transform ReferenceTransformHead;

  
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer) {
            var z = FindObjectOfType<ZedSpaceReference>();
            if (z != null) {
              transform.position= z.transform.position;
              transform.rotation= z.transform.rotation;
            }else
            {Debug.LogWarning("Could not find ZedSpaceReference, not sure where to go?!");}
        }
    }
   
 

    private void TriggerCalibration(Transform head) {
        ReferenceTransformHead = head;
          
    }



    public override void AssignClient(ulong i_CLID, ParticipantOrder i_participantOrder_) {
        m_participantOrder = i_participantOrder_;
        m_ClientID= i_CLID;
    }

    public override Transform GetCameraPositionObject() {
        return transform;
    }

    public override void SetStartingPose(Pose _pose) {
      
    }

    public override bool HasActionStopped() {
        return true;
    }
    
    public override void Stop_Action() {
       
    }

    public void ReConnectToAvatar(int skeletonID) {
       

    }

    public void WorldCalibration() {
      
    }
}
