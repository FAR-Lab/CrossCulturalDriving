using System.Collections;
using System.Collections.Generic;
using Mocopi.Receiver;
using UnityEngine;

public class Mocopie_Interactable : Interactable_Object {
    private ParticipantOrder m_participantOrder;

    private ulong m_CLID;
    private Pose StartingPose;


    private MocopiAvatar m_avatar;

    private Transform m_participantHead;
    private Transform m_mocopiHead;
    private Transform m_avatarT;

    private bool ready = false;
    // Start is called before the first frame update
    void Start() {
        m_avatar = FindObjectOfType<MocopiAvatar>();
        if (m_avatar == null) {
            Debug.LogError("Not good I need an avatar!");
            m_mocopiHead = m_avatar.Animator.GetBoneTransform(HumanBodyBones.Head);
            m_avatarT = m_avatar.transform;
        }
    }

    // Update is called once per frame
    void Update() {
        if(!ready) {
            return;
        }
    m_avatarT.position += m_participantHead.position - m_mocopiHead.position;
    }

    public override void Stop_Action() {
       
    }

    public override void AssignClient(ulong CLID, ParticipantOrder participantOrder) {
        m_participantOrder = participantOrder;
        m_CLID = CLID;
        m_participantHead = ConnectionAndSpawning.Singleton.GetClientMainCameraObject(participantOrder);
        ready = true;
    }

    public override Transform GetCameraPositionObject() {
        return transform;
    }

    public override void SetStartingPose(Pose _pose) {
        StartingPose = _pose;
        
    }

    public override bool HasActionStopped() {
       return true;
    }
}
