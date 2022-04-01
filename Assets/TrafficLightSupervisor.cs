using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TrafficLightSupervisor : MonoBehaviour
{
    private TrafficLightController[] m_TrafficLightControllers;

    public enum trafficLightStatus :int
    {
        IDLE,
        RED,
        GREEN
    };

    // Start is called before the first frame update
    void Start()
    {
    }


    private void SetTrafficLights(trafficLightStatus status)
    {
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