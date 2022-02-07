using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using Rerun;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;


public class TrafficLightController : NetworkBehaviour {
    enum materialID : int {
        MID = 0,
        TOP = 1,
        BOTTOM = 2
    }


    public Material off;
    public Material red;
    public Material green;
    public Material yellow;


    public Transform LightSurfaces;


    private Renderer LightRenderer;
    private bool TrafficLightOutOfServic = false;


    public TrafficLightSupervisor.trafficLightStatus StartinTrafficlightStatus;

    void Start() {
        LightRenderer = LightSurfaces.GetComponent<Renderer>();
        switch (StartinTrafficlightStatus) {
            case TrafficLightSupervisor.trafficLightStatus.IDLE:
                TrafficLightOutOfServic = true;
                StartCoroutine(GoIdle());
                break;
            case TrafficLightSupervisor.trafficLightStatus.RED:
                StartCoroutine(GoRed());
                break;
            case TrafficLightSupervisor.trafficLightStatus.GREEN:
                StartCoroutine(GoGreen());
                break;
            default: throw new ArgumentOutOfRangeException();
        }
    }

    void Update() { }


    public void StartGreenCoroutine() {
        if (!IsServer) return;
        StartCoroutine(GoGreen());
        StartGreenCoroutineClientRpc();
    }

    [ClientRpc]
    private void StartGreenCoroutineClientRpc() { StartCoroutine(GoGreen()); }

    public void StartRedCoroutine() {
        if (!IsServer) return;
        StartCoroutine(GoRed());
        StartRedCoroutineClientRpc();
    }


    [ClientRpc]
    private void StartRedCoroutineClientRpc() { StartCoroutine(GoRed()); }


    public void ToggleOOSCoroutine() {
        if (!IsServer) return;
        if (TrafficLightOutOfServic) { TrafficLightOutOfServic = false; }
        else {
            TrafficLightOutOfServic = true;
            StartCoroutine(GoIdle());
        }

        ToggleOOSCoroutineClientRpc();
    }


    [ClientRpc]
    private void ToggleOOSCoroutineClientRpc() {
        if (TrafficLightOutOfServic) { TrafficLightOutOfServic = false; }
        else {
            TrafficLightOutOfServic = true;
            StartCoroutine(GoIdle());
        }
    }


    private IEnumerator GoRed() {
        Material[] mat = LightRenderer.materials;
        mat[(int) materialID.MID] = yellow;
        mat[(int) materialID.TOP] = off;
        mat[(int) materialID.BOTTOM] = off;

        LightRenderer.materials = mat;

        yield return new WaitForSeconds(1);

        mat[(int) materialID.MID] = off;
        mat[(int) materialID.TOP] = red;
        mat[(int) materialID.BOTTOM] = off;

        LightRenderer.materials = mat;
    }

    private IEnumerator GoIdle() {
        Material[] mat = LightRenderer.materials;
        bool tmp = false;
        while (TrafficLightOutOfServic) {
            mat[(int) materialID.MID] = tmp ? yellow : off;
            mat[(int) materialID.TOP] = off;
            mat[(int) materialID.BOTTOM] = off;

            LightRenderer.materials = mat;
            tmp = !tmp;
            yield return new WaitForSeconds(1);
        }

        mat[(int) materialID.MID] = off;
        mat[(int) materialID.TOP] = off;
        mat[(int) materialID.BOTTOM] = off;

        LightRenderer.materials = mat;
    }

    private IEnumerator GoGreen() {
        Material[] mat = LightRenderer.materials;
        mat[(int) materialID.MID] = yellow;
        mat[(int) materialID.TOP] = off;
        mat[(int) materialID.BOTTOM] = off;

        LightRenderer.materials = mat;

        yield return new WaitForSeconds(1);

        mat[(int) materialID.MID] = off;
        mat[(int) materialID.TOP] = off;
        mat[(int) materialID.BOTTOM] = green;

        LightRenderer.materials = mat;
    }
}