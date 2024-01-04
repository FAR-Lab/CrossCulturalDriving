using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowParticipant : MonoBehaviour {

    public Vector3 participantToFollow;
    public ParticipantOrder followPO;
    
    void Start()
    {
        InvokeRepeating(nameof(FindVRParticipant),0, 1);
    }

    // Update is called once per frame
    void Update() {
        transform.position = new Vector3(participantToFollow.x, transform.position.y, participantToFollow.z);
    }

    void FindVRParticipant() {
        return;
   //     var temParticipant = FindObjectOfType<ZedAvatarInteractable>();
        
      //  if(!temParticipant) return;
        
      //  if (temParticipant.m_participantOrder == followPO) {
      //      participantToFollow = temParticipant.transform.position;
      //  }
    }
    
}
