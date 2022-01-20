/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


public class SteeringWheelManager : MonoBehaviour {
  

  

    private class SteeringWheelData {
        public int wheelIndex;
        public float steerInput;
        public float accelInput;
        public int constant;
        public int damper;
        public int springSaturation;
        public int springCoefficient;
        public bool running;
        public bool forceFeedbackPlaying;
        public float gas;
        public float brake;
        public SteeringWheelData(int index) {
            steerInput = 0f;
            wheelIndex = index;
            accelInput = 0f;
            constant = 0;
            damper = 0;
            springSaturation = 0;
            springCoefficient = 0;
            forceFeedbackPlaying = false;
            gas = 0;
            brake = 0;
            running = false;
        }
    }

    private bool ready = false;
    public int FoundSteeringWheels;
    private struct WheelCallibration {
        public int minBrake;
        public int maxBrake;
        public int minGas;
        public int maxGas;
    }


    public float FFBGain = 1f;

    private static string WheelManufacturer = " Logitech";
    
    [SerializeField] 
    private Dictionary<string, WheelCallibration> SteeringWheelConfigs =
        new Dictionary<string, WheelCallibration> {
            [WheelManufacturer]= new WheelCallibration{
                minBrake = 1,
                maxBrake = -1,
                minGas = -1,
                maxGas = 1
            }
        };
 

    private Dictionary<ParticipantOrder, SteeringWheelData> ActiveWheels =
        new Dictionary<ParticipantOrder, SteeringWheelData>();

    
    
    public static SteeringWheelManager Singleton { get; private set; }
    private void SetSingleton() { Singleton = this; }
    private void Awake() {
        
        if (Singleton != null && Singleton != this) {
            Destroy(this);
            return;
        }

        SetSingleton();
        DontDestroyOnLoad(gameObject);
        
        this.enabled = false;
        
    }

    void Start() {
        FFBGain = 1.0f;
        Debug.Log("SteeringWheelController - Startup");
    }

    public void Init() {
        ready = true;
        DirectInputWrapper.Init();
        AssignSteeringWheels();
        var initForceFeedback = InitForceFeedback();
        StartCoroutine(initForceFeedback);
    }

    IEnumerator SpringforceFix() {
        yield return new WaitForSeconds(1f);
        StopSpringForce();
        yield return new WaitForSeconds(0.5f);
        InitSpringForce(0, 0);
    }

    void AssignSteeringWheels() {
        ParticipantOrder po = ParticipantOrder.A;
        for (int i = 0; i < DirectInputWrapper.DevicesCount(); i++) {
            Debug.Log("We got the input controller called" + DirectInputWrapper.GetProductNameManaged(i));
            ActiveWheels.Add(po, new SteeringWheelData(i));
            Debug.Log(po);
            po++;
        }
    }


    public void CleanUp() {
        
        foreach(SteeringWheelData steeringWheelData in  ActiveWheels.Values)
        {
            
            steeringWheelData.forceFeedbackPlaying= false;
            steeringWheelData.constant = 0;
            steeringWheelData.damper = 0;
        }
     
    }

    public void SetConstantForce(int force, ParticipantOrder po) {
        if (ActiveWheels.ContainsKey(po)) {
            var steeringWheelData = ActiveWheels[po];
            steeringWheelData.constant = force;
        }
     
    }

    public void SetDamperForce(int force, ParticipantOrder po) {
        if (ActiveWheels.ContainsKey(po)) {
            var steeringWheelData = ActiveWheels[po];
            steeringWheelData.damper = force;
        }
    }

    public void SetSpringForce(int sat, int coeff, ParticipantOrder po) {
            if (ActiveWheels.ContainsKey(po)) {
                var steeringWheelData = ActiveWheels[po];
                steeringWheelData.springCoefficient = coeff;
                steeringWheelData.springSaturation = sat;
            } 
            
           
        
    }

   
    public void InitSpringForce(int sat, int coeff) { StartCoroutine(_InitSpringForce(sat, coeff)); }

    public void StopSpringForce() {
        foreach (SteeringWheelData swd in ActiveWheels.Values) {
            swd.forceFeedbackPlaying = false;
            Debug.Log("stopping spring" + DirectInputWrapper.StopSpringForce(swd.wheelIndex));
        }

    }

    private IEnumerator _InitSpringForce(int sat, int coeff) {
      

        yield return new WaitForSeconds(1f);
        long res = -1;
        int tries = 0;
        foreach (SteeringWheelData  swd in  ActiveWheels.Values ) {
            while (res < 0) {

                res = DirectInputWrapper.PlaySpringForce(swd.wheelIndex, 0, Mathf.RoundToInt(sat * FFBGain),
                    Mathf.RoundToInt(coeff * FFBGain));
                Debug.Log("starting spring for the wheel" + res);

                tries++;
                if (tries > 150) {
                    Debug.Log("coudn't init spring force for the steerng wheel. aborting");
                    break;
                }

                yield return null;
            }
        }
    }


    public void OnGUI() {
        /* if (GUI.Button(new Rect(new Vector2(50, 50), new Vector2(50, 50)), "ResetForceFeedBack")) {
             Init();
 
         }
         if (debugInfo) {
             GUI.Label(new Rect(20, Screen.height - 180, 500, 100), "Raw Input: " + accelInput, debugStyle);
             GUI.Label(new Rect(20, Screen.height - 100, 500, 100), "Adjusted Input: " + gas, debugStyle);
         }
 
         GUI.Label(new Rect(0, 0, 100, 100), "alignment Torque: " + constant
                 + "\n spring coeff" + springCoefficient
                  + "\n spring sat" + springSaturation
                   + "\n damp" + damper
 
                 );*/
    }

    private IEnumerator InitForceFeedback() {
        foreach (SteeringWheelData swd in ActiveWheels.Values) {
            swd.constant = 0;
            swd.damper = 0;
            swd.springCoefficient = 0;
            swd.springSaturation = 0;
        }
        yield return new WaitForSeconds(0.5f);
        foreach (SteeringWheelData swd in ActiveWheels.Values) {
            swd.forceFeedbackPlaying = true;
        }
    }


    void Update() {
        if (!ready || ActiveWheels==null) return;
        if (Application.platform == RuntimePlatform.OSXEditor) return;
        DirectInputWrapper.Update();
        ready = DirectInputWrapper.DevicesCount() > 0;

        FoundSteeringWheels = ActiveWheels.Count();
        foreach (SteeringWheelData swd in ActiveWheels.Values) {
            DeviceState state;
            state = DirectInputWrapper.GetStateManaged(swd.wheelIndex);
            swd.steerInput = state.lX / 32768f;
            // accelInput = (state.lY- 32768f) / -32768f;

            swd.gas = 0.9f * swd.gas + 0.1f * ((state.lY) / (-32768f));

            swd.brake = (state.lRz) / (32768f);


            if (swd.forceFeedbackPlaying) {
                Debug.Log("playing force"+swd.wheelIndex+swd.ToString());
                DirectInputWrapper.PlayConstantForce(swd.wheelIndex, Mathf.RoundToInt(swd.constant * FFBGain));
                DirectInputWrapper.PlayDamperForce(swd.wheelIndex, Mathf.RoundToInt(swd.damper * FFBGain));
                //DirectInputWrapper.PlaySpringForce(wheelIndex, 0, Mathf.RoundToInt(0 * FFBGain), springCoefficient);


                DirectInputWrapper.PlaySpringForce(swd.wheelIndex, 0,
                    Mathf.RoundToInt((swd.springSaturation <= 0 ? 1 : swd.springSaturation) * FFBGain), swd.springCoefficient);
            }


            //Debug.Log(brake.ToString() + " break and gas" + gas.ToString());
                
                
            float totalGas = (SteeringWheelConfigs[WheelManufacturer].maxGas - SteeringWheelConfigs[WheelManufacturer].minGas);
            float totalBrake = (SteeringWheelConfigs[WheelManufacturer].maxBrake - SteeringWheelConfigs[WheelManufacturer].minBrake);

            swd.accelInput = (swd.gas - SteeringWheelConfigs[WheelManufacturer].minGas) / totalGas - (swd.brake - SteeringWheelConfigs[WheelManufacturer].minBrake) / totalBrake;
        }
    }

    public void GetAccelBrakeInput(out float accel, out float brk,ParticipantOrder po) {
        if (ActiveWheels.ContainsKey(po)) {
            accel = ActiveWheels[po].gas;
            brk = ActiveWheels[po].brake;
        }
        else {
            accel = 0;
            brk = 0;
        }
    }

    public float GetAccelInput(ParticipantOrder po) {
        if (ActiveWheels.ContainsKey(po)) { return ActiveWheels[po].accelInput; }
        else { return -1; }
    }

    public float GetSteerInput(ParticipantOrder po) {
        if (ActiveWheels.ContainsKey(po)) { return ActiveWheels[po].steerInput; }
        else { return -1; }
    }

    public float GetHandBrakeInput() { return 0f; }
}