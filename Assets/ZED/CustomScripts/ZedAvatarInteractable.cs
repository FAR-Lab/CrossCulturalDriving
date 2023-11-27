using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ZedAvatarInteractable : Interactable_Object {
    public GameObject ZEDMasterPrefab;
    private ulong m_ClientID;
    private bool initDone = false;
    private Transform ReferenceTransformHead;

    public NetworkVariable<Vector3> fwd = new NetworkVariable<Vector3>();

  
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            //if (ZEDMaster.Singleton == null)
            {
                Instantiate(ZEDMasterPrefab);
            }
        }
    }
   
    public void Update() {
      
        if (initDone && IsServer) {
            if (ReferenceTransformHead == null) {
                initDone = false;
                return;
            }
            transform.position = ReferenceTransformHead.position;
            fwd.Value = ReferenceTransformHead.forward;
           
        }
    }

    private void TriggerCalibration(Transform head) {
        ReferenceTransformHead = head;
            initDone = true;
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
        //ZEDMaster.Singleton.e_ReconnectionStart(skeletonID,TriggerCalibration);

    }

    public void InitialCalibration(Action<int> setSkeletonID) {
        //ZEDMaster.Singleton.e_StartCalibrationSequence(setSkeletonID,TriggerCalibration);
    }
}
