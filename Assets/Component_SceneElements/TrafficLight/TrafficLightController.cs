using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TrafficLightController : NetworkBehaviour {
    public Material off;
    public Material red;
    public Material green;
    public Material yellow;


    public Transform LightSurfaces;


    public TrafficLightTrigger.TLState StartinTrafficlightStatus;
    public ParticipantOrder m_participantOrder;
    private bool IdleState;


    private Renderer LightRenderer;


    private void Start() {
        LightRenderer = LightSurfaces.GetComponent<Renderer>();
        InteralGraphicsUpdate(StartinTrafficlightStatus);
    }


    private void Update() {
    }


    private void InteralGraphicsUpdate(TrafficLightTrigger.TLState inval) {
        switch (inval) {
            case TrafficLightTrigger.TLState.IDLE:
                IdleState = true;
                StartCoroutine(GoIdle());
                break;
            case TrafficLightTrigger.TLState.RED:
                IdleState = false;
                StartCoroutine(GoRed());
                break;
            case TrafficLightTrigger.TLState.GREEN:
                StartCoroutine(GoGreen());
                IdleState = false;
                break;
            case TrafficLightTrigger.TLState.NONE:
            default: break; 
        }
    }

    public void UpdatedTrafficlight(Dictionary<ParticipantOrder, TrafficLightTrigger.TLState> newval) {
        if (newval.ContainsKey(m_participantOrder)) {
            InteralGraphicsUpdate(newval[m_participantOrder]);
            UpdatedTrafficlightClientRPC(newval[m_participantOrder]);
        }
        
    }

    [ClientRpc]
    public void UpdatedTrafficlightClientRPC(TrafficLightTrigger.TLState newval) {
        InteralGraphicsUpdate(newval);
    }

    private IEnumerator GoRed() {
        var mat = LightRenderer.materials;
        mat[(int)materialID.MID] = yellow;
        mat[(int)materialID.TOP] = off;
        mat[(int)materialID.BOTTOM] = off;

        LightRenderer.materials = mat;

        yield return new WaitForSeconds(1);

        mat[(int)materialID.MID] = off;
        mat[(int)materialID.TOP] = red;
        mat[(int)materialID.BOTTOM] = off;

        LightRenderer.materials = mat;
    }

    private IEnumerator GoIdle() {
        var mat = LightRenderer.materials;
        var tmp = false;
        while (IdleState) {
            mat[(int)materialID.MID] = tmp ? yellow : off;
            mat[(int)materialID.TOP] = off;
            mat[(int)materialID.BOTTOM] = off;

            LightRenderer.materials = mat;
            tmp = !tmp;
            yield return new WaitForSeconds(1);
        }

        mat[(int)materialID.MID] = off;
        mat[(int)materialID.TOP] = off;
        mat[(int)materialID.BOTTOM] = off;

        LightRenderer.materials = mat;
    }

    private IEnumerator GoGreen() {
        var mat = LightRenderer.materials;
        ///  mat[(int) materialID.MID] = yellow;
        //mat[(int) materialID.TOP] = off;
        //   mat[(int) materialID.BOTTOM] = off;

        //   LightRenderer.materials = mat;

        //  yield return new WaitForSeconds(1);

        mat[(int)materialID.MID] = off;
        mat[(int)materialID.TOP] = off;
        mat[(int)materialID.BOTTOM] = green;

        LightRenderer.materials = mat;
        yield return null;
    }

    private enum materialID {
        MID = 0,
        TOP = 1,
        BOTTOM = 2
    }
}