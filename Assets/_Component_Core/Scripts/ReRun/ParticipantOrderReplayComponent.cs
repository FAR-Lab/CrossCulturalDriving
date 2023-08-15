using System;
using System.Collections;
using System.Collections.Generic;
using Rerun;
using UnityEngine;
using UltimateReplay;
using UltimateReplay.Core;
using UltimateReplay.Serializers;
using Unity.Netcode;

//[DisallowMultipleComponent]
//[ReplaySerializer(typeof(ParticipantOrderReplayComponentSerializer))]
public class ParticipantOrderReplayComponent : ReplayRecordableBehaviour
{
    public ParticipantOrder m_participantOrder;

    private bool first = true;




    public static IList<ParticipantOrderReplayComponent> AllComponents;
    public override void Awake()
    {
        base.Awake();
        if (AllComponents == null)
        {
            AllComponents = new List<ParticipantOrderReplayComponent>();
        }
        AllComponents.Add(this);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        AllComponents.Remove(this);
    }

    // Start is called before the first frame update
    public void Start()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            this.enabled = false;
        }

    }

    public override void OnReplayCapture()
    {
        base.OnReplayCapture();
        if (first)
        {
            first = false;
            ReplayState state = ReplayState.pool.GetReusable();
            state.Write(((char) m_participantOrder).ToString());
            RecordEvent(1, state);
        }
    }

    public override void OnReplayEvent(ushort eventID, ReplayState
        eventData)
    {
        switch (eventID)
        {
            case 1:
            {
                //   Debug.Log("Event 1: " + ((ParticipantOrder)(char)eventData.ReadString()[0]).ToString());
                m_participantOrder = (ParticipantOrder) (char) eventData.ReadString()[0];
                break;
            }
        }
    }

    private void LateUpdate()
    {
        if (ConnectionAndSpawing.Singleton.ServerState != ActionState.DRIVE)
        {
            first = true;
        }
    }


    public override void OnReplaySerialize(ReplayState state)
    {
      
        state.Write((char) m_participantOrder);
    }


    public override void OnReplayDeserialize(ReplayState state)
    {
       
        m_participantOrder = (ParticipantOrder) (char) state.ReadByte();
    }


    public void SetParticipantOrder(ParticipantOrder po)
    {
        m_participantOrder = po;
    }
    public void SetParticipantOrderAsChar(char po)
    {
        m_participantOrder =(ParticipantOrder) po;
    }
    public ParticipantOrder GetParticipantOrder()
    {
        return  m_participantOrder;
    }
    public char GetParticipantOrderAsChar()
    {
        return (char) m_participantOrder;
    }
}

/*
public class ParticipantOrderReplayComponentSerializer : IReplaySerialize
{
    public ParticipantOrder po;

    public void OnReplaySerialize(ReplayState state)
    {
        state.Write((char) po);
    }


    public void OnReplayDeserialize(ReplayState state)
    {
        po = (ParticipantOrder) (char) state.ReadByte();
    }

    public static ParticipantOrderReplayComponentSerializer DeserializeReplayState(ReplayState state)
    {
        // Create a serializer
        ParticipantOrderReplayComponentSerializer serializer = new ParticipantOrderReplayComponentSerializer();

        // Try to deserialize
        serializer.OnReplayDeserialize(state);

        return serializer;
    }
}*/