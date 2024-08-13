using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Mocopi.Receiver;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;


public class Mocopie_Interactable : Interactable_Object {
    private ulong m_CLID;
    private Pose StartingPose;


    private MocopiAvatar m_avatar;

    public Transform m_participantHead;
    public Transform m_mocopiHead;
    public Transform m_avatarT;
    public MocopiSimpleReceiver m_mocopi;
    [SerializeField]
    [Range(-0.5f,0.5f)]
    public float offsetUp;
    [SerializeField]
    [Range(-0.5f,0.5f)]
    public float offsetFwd;
    private bool ready = false;
    
    public SO_IPAddress m_multicastAddress;
    private NetworkVariable<FixedString64Bytes> multicastAddress = new();

    private void AttemptToFindTheAppropriateHead() {
        Debug.Log($"newValue {m_participantOrder.Value}");
        if (m_participantOrder.Value != ParticipantOrder.None  && m_participantOrder.Value != 0 && FindObjectsOfType<VR_Participant>().Length>0) {
            Debug.Log("Count oif VR participants"+ FindObjectsOfType<VR_Participant>().Length);
            m_participantHead = FindObjectsOfType<VR_Participant>()
                .First(x => x.GetParticipantOrder() == m_participantOrder.Value).GetMainCamera();
            ready = m_participantHead != null;
        }

        Debug.Log($"mocopiInteractableReferences  m_avatarT{m_avatarT}, vrhead{m_participantHead}  ready{ready}");
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        m_mocopi = transform.GetComponent<MocopiSimpleReceiver>();
        m_avatar = GetComponentInChildren<MocopiAvatar>();
        
        if (IsServer)
            multicastAddress.Value = string.IsNullOrEmpty(m_multicastAddress.ipAddress) ? m_mocopi.MulticastAddress : m_multicastAddress.ipAddress;
        
        if (m_avatar == null) {
            Debug.LogError("Not good I need an avatar!");
        }
        else{
            m_mocopiHead = m_avatar.Animator.GetBoneTransform(HumanBodyBones.Head);
            m_avatarT = m_avatar.transform;
            Debug.Log($"Got a head{m_mocopiHead} and a main T:{m_avatarT}");
        }

        m_mocopi.MulticastAddress = multicastAddress.Value.ToString();
        m_mocopi.StartReceiving();
    }

    public MocopiAvatar GetMocopiAvatar() {
        return m_avatar;
    }

    // Update is called once per frame
    void Update() {
        if (ready) {
            Vector3 tmp = m_participantHead.position + m_participantHead.up * offsetUp +
                          m_participantHead.forward * offsetFwd;
            m_avatarT.position += tmp - m_mocopiHead.position;
            float angle = Vector2.SignedAngle(new Vector2(m_participantHead.forward.x, m_participantHead.forward.z),
                new Vector2(m_mocopiHead.forward.x, m_mocopiHead.forward.z));
            m_avatarT.Rotate(Vector3.up, angle * 0.1f);
        }
        else {
            AttemptToFindTheAppropriateHead();
        }
    }

    public override void Stop_Action() {
       
    }

    public override void AssignClient(ulong CLID, ParticipantOrder participantOrder) {
        m_participantOrder.Value = participantOrder;
        m_CLID = CLID;
        m_participantHead = FindObjectsOfType<VR_Participant>().First(x => x.GetParticipantOrder()==participantOrder).GetMainCamera();
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

    public override void OnNetworkDespawn() {
        if (IsServer) {
            m_mocopi.StopReceiving();
        }
        base.OnNetworkDespawn();
        
    }
}
