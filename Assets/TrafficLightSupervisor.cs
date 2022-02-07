using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;



public class TrafficLightSupervisor : MonoBehaviour {
    private TrafficLightController[] m_TrafficLightControllers;

    public enum trafficLightStatus {
        IDLE,
        RED,
        GREEN
    };

    // Start is called before the first frame update
    void Start() { }

   
    
    private void SetTrafficLightsGreen(trafficLightStatus status) {
        m_TrafficLightControllers = FindObjectsOfType<TrafficLightController>();

        foreach (TrafficLightController oneLight in m_TrafficLightControllers) {
            switch (status) {
                case trafficLightStatus.IDLE: oneLight.ToggleOOSCoroutine();
                    break;
                case trafficLightStatus.RED:
                    oneLight.StartRedCoroutine();
                    break;
                case trafficLightStatus.GREEN: 
                    oneLight.StartGreenCoroutine();
                    break;
                default:
                    break;
            }
        }
    }

    private void OnGUI() {
        if (!(ConnectionAndSpawing.Singleton.ServerState == ActionState.READY 
              || ConnectionAndSpawing.Singleton.ServerState == ActionState.DRIVE 
             // ||ConnectionAndSpawing.Singleton.ServerState == ActionState.WAITINGROOM
              )) { return; }

        if (GUI.Button(new Rect(20, 50, 80, 20), "TL to Green")) { SetTrafficLightsGreen(trafficLightStatus.GREEN); }

        if (GUI.Button(new Rect(20, 75, 80, 20), "TL to Red")) { SetTrafficLightsGreen(trafficLightStatus.RED); }
    }

   
    void Update() { }
}