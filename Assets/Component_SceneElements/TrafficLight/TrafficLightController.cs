using System;
using System.Collections;
using UnityEngine;

public class TrafficLightController : MonoBehaviour {
    public Material off;
    public Material red;
    public Material green;
    public Material yellow;


    public Transform LightSurfaces;


    public TrafficLightSupervisor.trafficLightStatus StartinTrafficlightStatus;
    public ParticipantOrder participantAssociation;
    private bool IdleState;


    private Renderer LightRenderer;


    private void Start() {
        LightRenderer = LightSurfaces.GetComponent<Renderer>();
        InteralGraphicsUpdate(StartinTrafficlightStatus);
    }


    private void Update() {
    }


    private void InteralGraphicsUpdate(TrafficLightSupervisor.trafficLightStatus inval) {
        switch (inval) {
            case TrafficLightSupervisor.trafficLightStatus.IDLE:
                IdleState = true;
                StartCoroutine(GoIdle());
                break;
            case TrafficLightSupervisor.trafficLightStatus.RED:
                IdleState = false;
                StartCoroutine(GoRed());
                break;
            case TrafficLightSupervisor.trafficLightStatus.GREEN:
                StartCoroutine(GoGreen());
                IdleState = false;
                break;
            default: throw new ArgumentOutOfRangeException();
        }
    }

    public void UpdatedTrafficlight(TrafficLightSupervisor.trafficLightStatus newval) {
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