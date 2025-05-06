using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PedestrianWalkingTarget : NetworkBehaviour {


    private MeshRenderer[] m_MeshRenderer;

    private SpriteRenderer[] m_SpriteRenderer;

    // Start is called before the first frame update
    private void Awake() {
        DontDestroyOnLoad(this);
    }

    void Start() {
        m_MeshRenderer = GetComponentsInChildren<MeshRenderer>();

        m_SpriteRenderer = GetComponentsInChildren<SpriteRenderer>();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        ConnectionAndSpawning.Singleton.ServerStateChange += EnableDisableFunction;
    }

    private void EnableDisableFunction(ActionState state) {
        Debug.Log($"The Pedestrianwalking sign got toled that we now{state}");
        switch (state) {
            case ActionState.DEFAULT:
                break;
            case ActionState.WAITINGROOM:
                server_SetShowing(true);
                break;
            case ActionState.LOADINGSCENARIO:
                break;
            case ActionState.LOADINGVISUALS:
                break;
            case ActionState.READY:
                server_SetShowing(false);
                break;
            case ActionState.DRIVE:
                break;
            case ActionState.QUESTIONS:
                break;
            case ActionState.POSTQUESTIONS:
                break;
            case ActionState.RERUN:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }

    }




    private void server_SetShowing(bool val) {
        i_setShowing(val);
        SetShowingClientRPC(val);
    }

    [ClientRpc]
    private void SetShowingClientRPC(bool val) {
        i_setShowing(val);
    }

    private void i_setShowing(bool val) {
        foreach (var meshRenderer in m_MeshRenderer) {
            meshRenderer.enabled = val;
        }

        foreach (var meshRenderer in m_SpriteRenderer) {
            meshRenderer.enabled = val;
        }
    }
}
