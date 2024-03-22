﻿/*
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
using System.Text;
using UnityEngine.UI;


public class SteeringWheelManager : MonoBehaviour
{
    private class SteeringWheelData
    {
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

        public bool L_IndSwitch;
        public bool R_IndSwitch;
        public bool HornButton;
        public bool HighBeamButton;
        public SteeringWheelData(int index)
        {
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
            L_IndSwitch = false;
            R_IndSwitch = false;
            HornButton = false;
            HighBeamButton = false;
        }
    }

    private bool ready = false;
    public int FoundSteeringWheels;

    private struct WheelCallibration
    {
        public int minBrake;
        public int maxBrake;
        public int minGas;
        public int maxGas;
    }


    public float FFBGain = 1f;

    private static string WheelManufacturer = " Logitech";

    [SerializeField] private Dictionary<string, WheelCallibration> SteeringWheelConfigs =
        new Dictionary<string, WheelCallibration>
        {
            [WheelManufacturer] = new WheelCallibration
            {
                minBrake = 1,
                maxBrake = -1,
                minGas = -1,
                maxGas = 1
            }
        };


    private Dictionary<ParticipantOrder, SteeringWheelData> ActiveWheels =
        new Dictionary<ParticipantOrder, SteeringWheelData>();

    private const String FileName = "ActiveWheels.conf";
    public static SteeringWheelManager Singleton { get; private set; }

    private void SetSingleton()
    {
        Singleton = this;
    }

    private void Awake()
    {
        if (Singleton != null && Singleton != this)
        {
            Destroy(this);
            return;
        }

        SetSingleton();
        DontDestroyOnLoad(gameObject);
        
    }

    void Start()
    {
        FFBGain = 1.0f;
    }
    
    public void Init()
    {
        ready = true;
        #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            LogitechGSDK.LogiSteeringInitialize(false);
        #endif 
        AssignSteeringWheels();
        var initForceFeedback = InitForceFeedback();
        StartCoroutine(initForceFeedback);
    }
  
    
    public static int IntRemap(float value, float from1 = -10000, float to1 = 10000, float from2 = -100, float to2 = 100)
    {
        float returnVal = (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        return (int) returnVal;
    }
    
    // when this gets destroyed or the application quits, we need to clean up the steering wheel
    
    #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    void OnApplicationQuit()
    {
        CleanUp();
        LogitechGSDK.LogiSteeringShutdown();
    }

    private void OnDisable()
    {
        CleanUp();
        LogitechGSDK.LogiSteeringShutdown();
    }

    private void OnDestroy()
    {
        CleanUp();
        LogitechGSDK.LogiSteeringShutdown();
    }
    #endif


    IEnumerator SpringforceFix()
    {
        yield return new WaitForSeconds(1f);
        StopSpringForce();
        yield return new WaitForSeconds(0.5f);
        InitSpringForce(0, 0);
    }

    void AssignSteeringWheels()
    {
        ParticipantOrder po = ParticipantOrder.A;
        #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        for (int i = 0; i < GetNumberOfConnectedDevices(); i++)
        {
            Debug.Log("We got the input controller called" + GetProductName(i) +
                      "Assigning it to participant: " + po.ToString());
            ActiveWheels.Add(po, new SteeringWheelData(i));
            po++;
        }
        #endif
    }
    
    public string GetProductName(int index)
    {
        int bufferSize = 256; 
        StringBuilder productName = new StringBuilder(bufferSize);

        if (LogitechGSDK.LogiGetFriendlyProductName(index, productName, bufferSize))
        {
            return productName.ToString();
        }
        else
        {
            return "Unknown";
        }
    }
    
    public static int GetNumberOfConnectedDevices()
    {
        int connectedDevicesCount = 0;
        for (int i = 0; i < LogitechGSDK.LOGI_MAX_CONTROLLERS; i++)
        {
            if (LogitechGSDK.LogiIsConnected(i))
            {
                connectedDevicesCount++;
            }
        }
        return connectedDevicesCount;
    }


    public void CleanUp()
    {
        foreach (SteeringWheelData steeringWheelData in ActiveWheels.Values)
        {
            steeringWheelData.forceFeedbackPlaying = false;
            steeringWheelData.constant = 0;
            steeringWheelData.damper = 0;
        }
    }

    public void SetConstantForce(int force, ParticipantOrder po)
    {
        if (ActiveWheels.ContainsKey(po))
        {
            var steeringWheelData = ActiveWheels[po];
            steeringWheelData.constant = force;
        }
    }

    public void SetDamperForce(int force, ParticipantOrder po)
    {
        if (ActiveWheels.ContainsKey(po))
        {
            var steeringWheelData = ActiveWheels[po];
            steeringWheelData.damper = force;
        }
    }

    public void SetSpringForce(int sat, int coeff, ParticipantOrder po)
    {
        if (ActiveWheels.ContainsKey(po))
        {
            var steeringWheelData = ActiveWheels[po];
            steeringWheelData.springCoefficient = coeff;
            steeringWheelData.springSaturation = sat;
        }
    }


    public void InitSpringForce(int sat, int coeff)
    {
        StartCoroutine(_InitSpringForce(sat, coeff));
    }

    public void StopSpringForce()
    {
        foreach (SteeringWheelData swd in ActiveWheels.Values)
        {
            swd.forceFeedbackPlaying = false;
            
            Debug.Log("stopping spring" + LogitechGSDK.LogiStopSpringForce(swd.wheelIndex));
        }
    }

    private IEnumerator _InitSpringForce(int sat, int coeff)
    {
        yield return new WaitForSeconds(1f);
        bool res = false;
        int tries = 0;
        foreach (SteeringWheelData swd in ActiveWheels.Values)
        {
            while (res == false)
            {
                res = LogitechGSDK.LogiPlaySpringForce(swd.wheelIndex, 0, IntRemap(sat * FFBGain), IntRemap(coeff * FFBGain));
                Debug.Log("starting spring for the wheel" + res);

                tries++;
                if (tries > 150)
                {
                    Debug.Log("coudn't init spring force for the steerng wheel. aborting");
                    break;
                }

                yield return null;
            }
        }
    }

    private ParticipantOrder CallibrationTarget = ParticipantOrder.None;

    public void OnGUI() // ToDo turn this into a canvas interface
    {
        if (ConnectionAndSpawning.Singleton.ServerState == ActionState.WAITINGROOM)
        {
            if (CallibrationTarget != ParticipantOrder.None)
            {
                foreach (SteeringWheelData swd in ActiveWheels.Values)
                {
                    if (swd.HornButton)
                    {
                        switchSteeringWheels(swd.wheelIndex, CallibrationTarget);
                        CallibrationTarget = ParticipantOrder.None;
                        break;
                    }
                }

                GUI.Label(new Rect(160 + 75, 10, 25 * 6, 50), "Press a button for " + CallibrationTarget.ToString());
                return;
            }

            List<ParticipantOrder> tmp =
                new List<ParticipantOrder>(Enum.GetValues(typeof(ParticipantOrder)).Cast<ParticipantOrder>().ToArray());
            tmp.Remove(ParticipantOrder.None);
            int x = 75;

            foreach (var v in tmp)
            {
                if (GUI.Button(new Rect(160 + x, 10, 25, 25), v.ToString()))
                {
                    CallibrationTarget = v;
                    return;
                }

                GUI.Label(new Rect(160 + x, 25, 25, 25), v.ToString());
                var text = "";
                if (ActiveWheels.ContainsKey(v))
                {
                    text = ActiveWheels[v].wheelIndex.ToString();
                }

                var prev = text;
                text = GUI.TextField(new Rect(150 + x, 50, 25, 25), text, 2);
                if (prev != text)
                {
                    if (int.TryParse(text, out int newIndex))
                    {
                        switchSteeringWheels(newIndex, v);
                        text = prev;
                    }
                }

                x += 25;
            }
        }
    }

    private void switchSteeringWheels(int newIndex, ParticipantOrder v)
    {
        List<ParticipantOrder> tmp =
            new List<ParticipantOrder>(Enum.GetValues(typeof(ParticipantOrder)).Cast<ParticipantOrder>().ToArray());
        tmp.Remove(ParticipantOrder.None);

        ParticipantOrder switchPartner = ParticipantOrder.None;
        foreach (var switchOrder in tmp)
        {
            if (ActiveWheels.ContainsKey(switchOrder) && ActiveWheels[switchOrder].wheelIndex == newIndex)
            {
                switchPartner = switchOrder;
                break;
            }
        }

        if (switchPartner != ParticipantOrder.None)
        {
            if (ActiveWheels.ContainsKey(v))
            {
                (ActiveWheels[v], ActiveWheels[switchPartner]) =
                    (ActiveWheels[switchPartner], ActiveWheels[v]);
                return;
            }
            else
            {
                ActiveWheels[v] = ActiveWheels[switchPartner];
                ActiveWheels.Remove(switchPartner);
                return;
            }
        }
    }

    private IEnumerator InitForceFeedback()
    {
        foreach (SteeringWheelData swd in ActiveWheels.Values)
        {
            swd.constant = 0;
            swd.damper = 0;
            swd.springCoefficient = 0;
            swd.springSaturation = 0;
        }

        yield return new WaitForSeconds(0.5f);
        foreach (SteeringWheelData swd in ActiveWheels.Values)
        {
            swd.forceFeedbackPlaying = true;
        }
    }


    void Update()
    {
        if (!ready || ActiveWheels == null) return;
        if (Application.platform == RuntimePlatform.OSXEditor) return;
        
        #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        LogitechGSDK.LogiUpdate();
        ready = GetNumberOfConnectedDevices() > 0;
        #endif

        FoundSteeringWheels = ActiveWheels.Count();
        foreach (SteeringWheelData swd in ActiveWheels.Values)
        {
            
            LogitechGSDK.DIJOYSTATE2ENGINES state;
            state = LogitechGSDK.LogiGetStateCSharp(swd.wheelIndex);
            
            swd.steerInput = state.lX / 32768f;
            swd.gas = 0.9f * swd.gas + 0.1f * ((state.lY) / (-32768f));
            swd.brake = (state.lRz) / (32768f);


            swd.L_IndSwitch = state.rgbButtons[5] > 0;
            swd.R_IndSwitch = state.rgbButtons[4] > 0;

            swd.HornButton =
                state.rgbButtons[0] > 0 ||
                state.rgbButtons[1] > 0 ||
                state.rgbButtons[2] > 0 ||
                state.rgbButtons[3] > 0 ||
                state.rgbButtons[7] > 0 ||
                state.rgbButtons[11] > 0 ||
                state.rgbButtons[23] > 0;
            
            swd.HighBeamButton = state.rgbButtons[6] > 0 ||
                                 state.rgbButtons[10] > 0;
            
            if (swd.forceFeedbackPlaying)
            {
               
                LogitechGSDK.LogiPlayConstantForce(swd.wheelIndex, IntRemap(swd.constant * FFBGain));
                LogitechGSDK.LogiPlayDamperForce(swd.wheelIndex, IntRemap(swd.damper * FFBGain));

                LogitechGSDK.LogiPlaySpringForce(swd.wheelIndex, 0,
                    IntRemap((swd.springSaturation <= 0 ? 1 : swd.springSaturation) * FFBGain),
                    IntRemap(swd.springCoefficient));

            }


            //Debug.Log(brake.ToString() + " break and gas" + gas.ToString());


            float totalGas = (SteeringWheelConfigs[WheelManufacturer].maxGas -
                              SteeringWheelConfigs[WheelManufacturer].minGas);
            float totalBrake = (SteeringWheelConfigs[WheelManufacturer].maxBrake -
                                SteeringWheelConfigs[WheelManufacturer].minBrake);

            swd.accelInput = (swd.gas - SteeringWheelConfigs[WheelManufacturer].minGas) / totalGas -
                             (swd.brake - SteeringWheelConfigs[WheelManufacturer].minBrake) / totalBrake;
        }
    }

    public void GetAccelBrakeInput(out float accel, out float brk, ParticipantOrder po)
    {
        if (ActiveWheels.ContainsKey(po))
        {
            accel = ActiveWheels[po].gas;
            brk = ActiveWheels[po].brake;
        }
        else
        {
            accel = 0;
            brk = 0;
        }
    }

    public bool GetLeftIndicatorInput(ParticipantOrder po)
    {
        if (ActiveWheels.ContainsKey(po))
        {
            return ActiveWheels[po].L_IndSwitch;
        }
        else
        {
            return false;
        }
    }

    public bool GetRightIndicatorInput(ParticipantOrder po)
    {
        if (ActiveWheels.ContainsKey(po))
        {
            return ActiveWheels[po].R_IndSwitch;
        }
        else
        {
            return false;
        }
    }

    public bool GetHornButtonInput(ParticipantOrder po)
    {
        if (ActiveWheels.ContainsKey(po))
        {
            return ActiveWheels[po].HornButton;
        }

        if (po == ParticipantOrder.A && Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.LeftShift))
        {
            return true;
        }

        if (po == ParticipantOrder.B && Input.GetKey(KeyCode.B) && Input.GetKey(KeyCode.LeftShift))
        {
            return true;
        }

        return false;
    }
    
    public bool GetHighBeamButtonInput(ParticipantOrder po)
    {
        if (ActiveWheels.ContainsKey(po))
        {
            return ActiveWheels[po].HighBeamButton;
        }

        return false;
    }

    public float GetAccelInput(ParticipantOrder po)
    {
        if (ActiveWheels.ContainsKey(po))
        {
            return ActiveWheels[po].accelInput;
        }
        else
        {
            return -2;
        }
    }

    public float GetSteerInput(ParticipantOrder po)
    {
        if (ActiveWheels.ContainsKey(po))
        {
            return ActiveWheels[po].steerInput;
        }
        else
        {
            return -2;
        }
    }

    public float GetHandBrakeInput()
    {
        return 0f;
    }
}