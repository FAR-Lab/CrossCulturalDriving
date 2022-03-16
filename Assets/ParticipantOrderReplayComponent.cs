
using System.Collections;
using System.Collections.Generic;
using Rerun;
using UnityEngine;
using UltimateReplay;
using Unity.Netcode;


public class ParticipantOrderReplayComponent : ReplayBehaviour
{
    private RerunManager _rerunManager;
    
    [ReplayVar(false)]
    public int participantOrder;
    // Start is called before the first frame update
    void Start()
    {
        _rerunManager = ConnectionAndSpawing.Singleton.GetReRunManager();
        if (NetworkManager.Singleton.IsClient)
        {
            this.enabled = false;
        }
    }

    public void SetParticipantOrder(ParticipantOrder po)
    {
        participantOrder = (char)po;


    }

    public ParticipantOrder GetParticipantOrder()
    {
       
          
        return (ParticipantOrder) participantOrder;
    }

    // Update is called once per frame
    void Update()
    {
       
    }
    
}
