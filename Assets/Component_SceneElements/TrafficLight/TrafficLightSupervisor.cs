using UltimateReplay;
using UnityEngine;

public class TrafficLightSupervisor : ReplayBehaviour {
    public enum trafficLightStatus {
        IDLE,
        RED,
        GREEN
    }

    private TrafficLightController[] m_TrafficLightControllers;


    private void Start() {
    }

    private void OnGUI() {
        if (!(ConnectionAndSpawning.Singleton.ServerState == ActionState.READY
              || ConnectionAndSpawning.Singleton.ServerState == ActionState.DRIVE
                // ||ConnectionAndSpawing.Singleton.ServerState == ActionState.WAITINGROOM
            ))
            return;

        if (GUI.Button(new Rect(20, 50, 80, 20), "TL to Green")) SetTrafficLights(trafficLightStatus.GREEN);

        if (GUI.Button(new Rect(20, 75, 80, 20), "TL to Red")) SetTrafficLights(trafficLightStatus.RED);
    }

    public override void OnReplayEvent(ushort eventID, ReplayState
        eventData) {
        Debug.Log("PlayingBack Event!!");
        switch (eventID) {
            case 1: {
                SetTrafficLights((trafficLightStatus)eventData.ReadByte());
                break;
            }
        }
    }

    private void SetTrafficLights(trafficLightStatus status) {
        if (IsRecording) {
            Debug.Log("Recorded event!");
            var state = ReplayState.pool.GetReusable();
            state.Write((byte)status);
            RecordEvent(1, state);
        }

        foreach (var po in FElem.AllPossPart)
            ConnectionAndSpawning.Singleton.GetMainClientObject(po)?.GetComponent<ParticipantInputCapture>()
                .UpdateTrafficLightsClientRPC(status);


        foreach (var oneLight in FindObjectsOfType<TrafficLightController>()) oneLight.UpdatedTrafficlight(status);
    }
}