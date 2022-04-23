using System;
using System.Collections;
using System.Collections.Generic;
using UltimateReplay;
using UnityEngine;


public class TrafficLightSupervisor : ReplayBehaviour
{
    private TrafficLightController[] m_TrafficLightControllers;

    public enum trafficLightStatus :int
    {
        IDLE,
        RED,
        GREEN
    };

  
    void Start()
    {
     

    }
    public override void OnReplayEvent(ushort eventID, ReplayState
        eventData)
    {
        Debug.Log("PlayingBack Event!!");
        switch (eventID)
        {
            case 1:
            {
                
                SetTrafficLights((trafficLightStatus) eventData.ReadByte());
                break;
            }
        }
    }

    private void SetTrafficLights(trafficLightStatus status)
    {
        if (IsRecording)
        {
            Debug.Log("Recorded event!");
            ReplayState state = ReplayState.pool.GetReusable();
            state.Write((byte) status);
            RecordEvent(1, state);
        }

        foreach (ParticipantOrder po in FElem.AllPossPart)
        {
            ConnectionAndSpawing.Singleton.GetMainClientObject(po)?.GetComponent<ParticipantInputCapture>()
                .UpdateTrafficLightsClientRPC(status);
        }


        foreach (TrafficLightController oneLight in FindObjectsOfType<TrafficLightController>())
        {
            oneLight.UpdatedTrafficlight(status);
        }
    }

    private void OnGUI()
    {
        if (!(ConnectionAndSpawing.Singleton.ServerState == ActionState.READY
              || ConnectionAndSpawing.Singleton.ServerState == ActionState.DRIVE
                // ||ConnectionAndSpawing.Singleton.ServerState == ActionState.WAITINGROOM
            ))
        {
            return;
        }

        if (GUI.Button(new Rect(20, 50, 80, 20), "TL to Green"))
        {
            SetTrafficLights(trafficLightStatus.GREEN);
        }

        if (GUI.Button(new Rect(20, 75, 80, 20), "TL to Red"))
        {
            SetTrafficLights(trafficLightStatus.RED);
        }
    }


}