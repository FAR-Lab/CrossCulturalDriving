/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class VehicleInputControllerNetworked : NetworkBehaviour {

    public Transform SteeringWheel;
    float steeringAngle;
    private VehicleController controller;
    private SteeringWheelInputController steeringInput;

    public Material Left;
    public Material Right;
    public Color On;
    public Color Off;

    public bool LeftActive;
    public bool RightActive;
    
    public bool lightOn;

    public float indicaterTimer;
    public float interval;
    public int indicaterStage;
    void Awake()
    {
        controller = GetComponent<VehicleController>();
        
    }
    private void Start() {
        steeringInput = GetComponent<SteeringWheelInputController>();
        indicaterStage = 0;
    }
    void startBlinking(bool left) {
        if (indicaterStage == 0) {
            indicaterStage = 1;
            if (left) {
                LeftActive = true;
                RightActive = false;
            } else {
                LeftActive = false;
                RightActive = true;
            }
        }

    }

    void UpdateIndicator() {
        if (indicaterStage == 1) {
            indicaterStage = 2;
            indicaterTimer = 0;

        } else if (indicaterStage == 2 || indicaterStage == 3) {
            indicaterTimer += Time.deltaTime;

            if (indicaterTimer > interval) {
                indicaterTimer = 0;
                Color c;
                if (lightOn) {
                    c = On;
                } else {
                    c = Off;
                }
                lightOn = !lightOn;
                if (LeftActive) {
                    Left.color = c;
                }
                if (RightActive) {
                    Right.color = c;
                }
            }
            if (indicaterStage == 2) {
                
                if (Mathf.Abs(steeringInput.GetSteerInput() * -450f )> 90) {
                    indicaterStage = 3;
                }
            } else if (indicaterStage == 3) {
                if (Mathf.Abs(steeringInput.GetSteerInput() * -450f) < 10) {
                    indicaterStage = 4;
                }
            }

        } else if(indicaterStage==4) {
            indicaterStage = 0;
            Left.color =Off;
            Right.color = Off;
        }




    }
    void Update () {
        if (isLocalPlayer) {
            if (steeringInput == null) {
                controller.steerInput = Input.GetAxis("Horizontal");
                controller.accellInput = Input.GetAxis("Vertical");
            } else {
                controller.steerInput = steeringInput.GetSteerInput();
                controller.accellInput = steeringInput.GetAccelInput();

                SteeringWheel.RotateAround(SteeringWheel.position, SteeringWheel.up, steeringAngle - steeringInput.GetSteerInput() * -450f);
                steeringAngle = steeringInput.GetSteerInput() * -450f;
            }
            if (Input.GetButtonDown("indicateLeft")) {
                Debug.Log("PushedIndicator Left");
                startBlinking(true);
            } else if (Input.GetButtonDown("indicateRight")) {
                Debug.Log("PushedIndicator Right");
                startBlinking(false);
            }
            UpdateIndicator();

        }
    }
}
