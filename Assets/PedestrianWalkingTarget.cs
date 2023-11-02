using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PedestrianWalkingTarget : NetworkBehaviour {

    public List<ParticipantOrder> ParticipantsToShowTo = new List<ParticipantOrder>();
    public List<ParticipantOrder> ParticipantsToTriggerOn = new List<ParticipantOrder>();

    private MeshRenderer m_MeshRenderer;
     
    // Start is called before the first frame update
    void Start() {
        m_MeshRenderer = GetComponent<MeshRenderer>();
        
        m_MeshRenderer.enabled = false;
        
    }

  

    public void trigger(Interactable_Object io) {
        i_trigger(io.m_participantOrder);
    }
    
    public void trigger(Client_Object io) {
        i_trigger(io.GetParticipantOrder());

    }
    public void trigger(ParticipantOrder po) {
        i_trigger(po);

    }
    private void i_trigger(ParticipantOrder po) {
        if (!IsServer) return;
        if(ParticipantsToTriggerOn.Contains(po)) {
            server_StartShowing(ParticipantsToShowTo.ToArray());
        }

    }

    private void server_StartShowing(ParticipantOrder[] pos) {//ToDo: This will not work in Host mode
        m_MeshRenderer.enabled = true;
        StartShowingClientRPC(pos);
    }

    [ClientRpc]
    private void StartShowingClientRPC(ParticipantOrder[] pos) {
        var t = VR_Participant.GetJoinTypeObject();
        
        if (pos.Contains(t.m_participantOrder) &&  m_MeshRenderer.enabled==false) {
            m_MeshRenderer.enabled = true;
        }
    }
    
}
