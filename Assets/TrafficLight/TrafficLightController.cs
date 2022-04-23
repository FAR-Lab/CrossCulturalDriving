using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using Rerun;
using UltimateReplay;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SocialPlatforms;


public class TrafficLightController : MonoBehaviour
{
    enum materialID : int
    {
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
    private bool IdleState = false;


    public TrafficLightSupervisor.trafficLightStatus StartinTrafficlightStatus;
    public ParticipantOrder participantAssociation;


    void Start()
    {
        LightRenderer = LightSurfaces.GetComponent<Renderer>();
        InteralGraphicsUpdate(StartinTrafficlightStatus);
       


    }
   
  
    private void Update()
    {
    }
    
    


    private void InteralGraphicsUpdate(TrafficLightSupervisor.trafficLightStatus inval)
    {
        
       

       

        switch (inval)
        {
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

    public void UpdatedTrafficlight(TrafficLightSupervisor.trafficLightStatus newval)

    {
        InteralGraphicsUpdate(newval);
    }


    private IEnumerator GoRed()
    {
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

    private IEnumerator GoIdle()
    {
        Material[] mat = LightRenderer.materials;
        bool tmp = false;
        while (IdleState)
        {
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

    private IEnumerator GoGreen()
    {
       Material[] mat = LightRenderer.materials;
      ///  mat[(int) materialID.MID] = yellow;
        //mat[(int) materialID.TOP] = off;
     //   mat[(int) materialID.BOTTOM] = off;

     //   LightRenderer.materials = mat;

      //  yield return new WaitForSeconds(1);

        mat[(int) materialID.MID] = off;
        mat[(int) materialID.TOP] = off;
        mat[(int) materialID.BOTTOM] = green;

        LightRenderer.materials = mat;
        yield return null;
    }
}