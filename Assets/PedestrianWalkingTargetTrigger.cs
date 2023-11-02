using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianWalkingTargetTrigger : MonoBehaviour
{
    // Start is called before the first frame update
    private PedestrianWalkingTarget m_parent;
    public bool TestingOverrwrite;
    void Start() {
        m_parent = GetComponentInParent<PedestrianWalkingTarget>();
        if (!m_parent.IsServer) {
            enabled = false;
        }
}

    private void Update() {
        if (TestingOverrwrite) {

            TestingOverrwrite = false;
            m_parent.trigger(ParticipantOrder.A);
        }
    }

    private void OnTriggerEnter(Collider other) {

        Debug.Log($"I got an on Trigger Enter Event{other.transform.name}");
        var io =  other.transform.GetComponentInParent<NetworkVehicleController>();
       if (io != null) {
           m_parent.trigger(io);
       }
       else {
           
           Debug.Log("Did not find Interactable_Object");

       }
       var co =  other.transform.GetComponent<VR_Participant>();
       if (co != null) {
           m_parent.trigger(co);
       } else {
           
           Debug.Log("Did not find Client_Object");

       }
       
    }
}
