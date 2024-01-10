using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PedestrianWalkingTarget : NetworkBehaviour {

  
    private MeshRenderer m_MeshRenderer;
     
    // Start is called before the first frame update
    private void Awake() {
        DontDestroyOnLoad(this);
    }

    void Start() {
        m_MeshRenderer = GetComponentInChildren<MeshRenderer>();
        m_MeshRenderer.enabled = true;
        
    }


    private void server_StartShowing() {
        m_MeshRenderer.enabled = true;
        StartShowingClientRPC();
    }

    [ClientRpc]
    private void StartShowingClientRPC() {
        var t = VR_Participant.GetJoinTypeObject();
        m_MeshRenderer.enabled = true;
    }
    
}
