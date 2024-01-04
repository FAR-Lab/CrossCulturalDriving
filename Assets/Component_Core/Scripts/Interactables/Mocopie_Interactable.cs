using System.Collections;
using System.Collections.Generic;
using Mocopi.Receiver;
using UnityEngine;
using UnityEngine.Serialization;


public class Mocopie_Interactable : Interactable_Object {
    private ParticipantOrder m_participantOrder;

    private ulong m_CLID;
    private Pose StartingPose;


    private MocopiAvatar m_avatar;

    public Transform m_participantHead;
    public Transform m_mocopiHead;
    public Transform m_avatarT;
    [FormerlySerializedAs("offset")] [Range(-0.5f,0.5f)]
    public float offsetUp;
    [Range(-0.5f,0.5f)]
    public float offsetFwd;
    private bool ready = false;
    // Start is called before the first frame update
    void Start() {
        m_avatar = FindObjectOfType<MocopiAvatar>();
        if (m_avatar == null) {
            Debug.LogError("Not good I need an avatar!");
        }
        else{
               m_mocopiHead = m_avatar.Animator.GetBoneTransform(HumanBodyBones.Head);
               m_avatarT = m_avatar.transform;
               Debug.Log($"Got a head{m_mocopiHead} and a main T:{m_avatarT}");
           }
        
    }

    // Update is called once per frame
    void Update() {
        if(!ready) {
            return;
        }

        Vector3 tmp = m_participantHead.position - (-m_participantHead.up * offsetUp) + (m_participantHead.forward* offsetFwd);
        m_avatarT.position += tmp- m_mocopiHead.position;
        float angle = Vector2.SignedAngle(new Vector2(m_participantHead.forward.x, m_participantHead.forward.z),
            new Vector2(m_mocopiHead.forward.x, m_mocopiHead.forward.z));



        m_avatarT.Rotate(Vector3.up, angle*0.1f);

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
