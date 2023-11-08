using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZedAvatarInteractable : Interactable_Object {
    public GameObject ZEDMasterPrefab;
    private ParticipantOrder m_participantOrder;
    private ulong m_ClientID;
    private bool initDone = false;
    private Transform ReferenceTransformHead;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            if (ZEDMaster.Singleton == null) {
                Instantiate(ZEDMasterPrefab);
            }

            ZEDMaster.Singleton.OnChangedTrackingReferrence += TriggerCalibration;
        }
    }

    public void LateUpdate() {
        if (initDone) {
            transform.position = ReferenceTransformHead.position;
            transform.rotation = ReferenceTransformHead.rotation;
        }
    }

    private void TriggerCalibration(ZEDMaster.UpdateType ud) {
        if (ud == ZEDMaster.UpdateType.NEWSKELETON) {
            ReferenceTransformHead = ZEDMaster.Singleton.GetCameraPositionObject();
            initDone = true;
        }
        else if (ud == ZEDMaster.UpdateType.DELETESKELETON) {
            initDone = false;
        }
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
}
