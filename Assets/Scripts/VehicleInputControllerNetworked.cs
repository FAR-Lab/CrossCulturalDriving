/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */


using System;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using UnityEngine;

using System.Collections;

public class VehicleInputControllerNetworked : NetworkBehaviour //MoveTo2020
{

    public Transform CameraPosition;
    
   
    private VehicleController controller;
    

    public bool useKeyBoard;
    

    public Transform[] Left;
    public Transform[] Right;
    public Transform[] BrakeLightObjects;
    public Material baseMaterial;

    private Material materialOn;
    private Material materialOff;
    private Material materialBrake;


    public Color BrakeColor;
    public Color On;
    public Color Off;
    private AudioSource HonkSound;
    public float SteeringInput;
    public float ThrottleInput;



    public override void NetworkStart()
    {
        base.NetworkStart();
        if (IsServer)
        {
            controller = GetComponent<VehicleController>();
        }
    }

    private void Start()
    {
        //Generating a new material for on/off;
        materialOn = new Material(baseMaterial);
        materialOn.SetColor("_Color", On);
        materialOff = new Material(baseMaterial);
        materialOff.SetColor("_Color", Off);
        materialBrake = new Material(baseMaterial);
        materialBrake.SetColor("_Color", BrakeColor);


        HonkSound = GetComponent<AudioSource>();

        foreach (Transform t in Left)
        {
            t.GetComponent<MeshRenderer>().material = materialOff;
        }
        foreach (Transform t in Right)
        {
            t.GetComponent<MeshRenderer>().material = materialOff;
        }
        foreach (Transform t in BrakeLightObjects)
        {
            t.GetComponent<MeshRenderer>().material = materialOff;
        }

    }
   

   
    [ClientRpc]
    public void TurnOnLeftClientRpc(bool Leftl_)
    {
        if (Leftl_)
        {
            foreach (Transform t in Left)
            {
                t.GetComponent<MeshRenderer>().material = materialOn;
            }
        }
        else
        {
            foreach (Transform t in Left)
            {
                t.GetComponent<MeshRenderer>().material = materialOff;
            }
        }
    }
    [ClientRpc]
    public void TurnOnRightClientRpc(bool Rightl_)
    {
        if (Rightl_)
        {
            foreach (Transform t in Right)
            {
                t.GetComponent<MeshRenderer>().material = materialOn;
            }
        }
        else
        {
            foreach (Transform t in Right)
            {
                t.GetComponent<MeshRenderer>().material = materialOff;
            }
        }
    }
    [ClientRpc]
    public void TurnOnBrakeLightClientRpc(bool Active)
    {
        if (Active)
        {
            foreach (Transform t in BrakeLightObjects)
            {
                t.GetComponent<MeshRenderer>().material = materialBrake;
            }
        }
        else
        {
            foreach (Transform t in BrakeLightObjects)
            {
                t.GetComponent<MeshRenderer>().material = materialOff;
            }
        }
    }

  
    [ClientRpc]
    public void HonkMyCarClientRpc()
    {Debug.Log("HonkMyCarClientRpc");
        HonkSound.Play();
    }



   [ClientRpc]
    public void SetGPSClientRpc(GpsController.Direction[] dir)
    {
       // GetComponentInChildren<GpsController>().SetDirection(dir[SceneStateManager.Instance.getParticipantID()]);
    }

    private void LateUpdate()
    {
        if (IsServer)
        {
            controller.steerInput =SteeringInput;
            controller.accellInput =ThrottleInput;
        }
    }

    void Update()
    {
       
    }
}
