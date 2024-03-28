using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TrafficLightController : NetworkBehaviour
{
    public Material off;
    public Material red;
    public Material green;
    public Material yellow;

    public Transform LightSurfaces;

    public TrafficLightTrigger.TLState StartinTrafficlightStatus;
    [SerializeField]
    private TrafficLightTrigger.TLState _currentStatus = TrafficLightTrigger.TLState.NONE;
    public ParticipantOrder m_participantOrder;

    private Renderer LightRenderer;

    private void Start()
    {
        LightRenderer = LightSurfaces.GetComponent<Renderer>();
        InteralGraphicsUpdate(StartinTrafficlightStatus);
        _currentStatus = StartinTrafficlightStatus;
    }

    private void Update()
    {
    }

    private void InteralGraphicsUpdate(TrafficLightTrigger.TLState inval)
    {
        if(_currentStatus == inval)
        {
            return;
        }

        switch (inval)
        {
            case TrafficLightTrigger.TLState.Yellow:
                
                break;
            case TrafficLightTrigger.TLState.RED:
                StartCoroutine(GoRed());
                break;
            case TrafficLightTrigger.TLState.GREEN:
                StartCoroutine(GoGreen());
                break;
            case TrafficLightTrigger.TLState.NONE:
                StartCoroutine(GoIdle());
                break;
            default: break;
        }
    }

    public void UpdatedTrafficlight(Dictionary<ParticipantOrder, TrafficLightTrigger.TLState> newval)
    {
        if (newval.ContainsKey(m_participantOrder))
        {
            InteralGraphicsUpdate(newval[m_participantOrder]);
            UpdatedTrafficlightClientRPC(newval[m_participantOrder]);
        }

    }

    [ClientRpc]
    public void UpdatedTrafficlightClientRPC(TrafficLightTrigger.TLState newval)
    {
        InteralGraphicsUpdate(newval);
    }

    // because of how the material is set up, simply swap the material to change the color
    // ** Actually not sure if unwrapping uv and do it like this is the best practice
    private IEnumerator GoRed()
    {
        LightRenderer.material = yellow;
        yield return new WaitForSeconds(1);
        LightRenderer.material = red;
        _currentStatus = TrafficLightTrigger.TLState.RED;
    }

    private IEnumerator GoIdle()
    {
        LightRenderer.material = off;
        yield return null;
        _currentStatus = TrafficLightTrigger.TLState.NONE;
    }

    private IEnumerator GoGreen()
    {
        LightRenderer.material = yellow;
        yield return new WaitForSeconds(1);
        LightRenderer.material = green;
        yield return null;
        _currentStatus = TrafficLightTrigger.TLState.GREEN;
    }
}
