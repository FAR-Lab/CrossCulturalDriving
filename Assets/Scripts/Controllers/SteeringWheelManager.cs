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
using UnityEngine.UI;


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

        public bool L_IndSwitch;
        public bool R_IndSwitch;
        public bool OtherButton;

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
            L_IndSwitch = false;
            R_IndSwitch = false;
            OtherButton = false;
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

    [SerializeField] private Dictionary<string, WheelCallibration> SteeringWheelConfigs =
        new Dictionary<string, WheelCallibration> {
            [WheelManufacturer] = new WheelCallibration {
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
            Debug.Log("We got the input controller called" + DirectInputWrapper.GetProductNameManaged(i) +
                      "Asigning it to participant: " + po.ToString());
            ActiveWheels.Add(po, new SteeringWheelData(i));
            po++;
        }
    }


    public void CleanUp() {
        foreach (SteeringWheelData steeringWheelData in ActiveWheels.Values) {
            steeringWheelData.forceFeedbackPlaying = false;
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
        foreach (SteeringWheelData swd in ActiveWheels.Values) {
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
        if (ConnectionAndSpawing.Singleton.ServerState == ActionState.WAITINGROOM) {
            //   GUISkin skin = new GUISkin();
            // skin.font.fontSize = 16;
            List<ParticipantOrder> tmp =
                new List<ParticipantOrder>(Enum.GetValues(typeof(ParticipantOrder)).Cast<ParticipantOrder>().ToArray());
            tmp.Remove(ParticipantOrder.None);
            int x = 75;

            foreach (var v in tmp) {
                GUI.Label(new Rect(160 + x, 25, 25, 25), v.ToString());
                var text = "";
                if (ActiveWheels.ContainsKey(v)) { text = ActiveWheels[v].wheelIndex.ToString(); }

                var prev = text;
                text = GUI.TextField(new Rect(150 + x, 50, 25, 25), text, 2);
                if (prev != text) {
                    if (int.TryParse(text, out int newIndex)) {
                        ParticipantOrder switchPartner = ParticipantOrder.None;
                        foreach (var swd in tmp) {
                            if (ActiveWheels.ContainsKey(swd) && ActiveWheels[swd].wheelIndex == newIndex) {
                                switchPartner = swd;
                                break;
                            }
                        }

                        if (switchPartner != ParticipantOrder.None) {
                            if (ActiveWheels.ContainsKey(v)) {
                                (ActiveWheels[v], ActiveWheels[switchPartner]) =
                                    (ActiveWheels[switchPartner], ActiveWheels[v]);
                                return;
                            }
                            else {
                                ActiveWheels[v] = ActiveWheels[switchPartner];
                                ActiveWheels.Remove(switchPartner);
                                return;
                            }
                        }
                        else { text = prev; }
                    }
                    else { text = prev; }
                }


                x += 25;
            }
        }
    }

    private IEnumerator InitForceFeedback() {
        foreach (SteeringWheelData swd in ActiveWheels.Values) {
            swd.constant = 0;
            swd.damper = 0;
            swd.springCoefficient = 0;
            swd.springSaturation = 0;
        }

        yield return new WaitForSeconds(0.5f);
        foreach (SteeringWheelData swd in ActiveWheels.Values) { swd.forceFeedbackPlaying = true; }
    }


    void Update() {
        if (!ready || ActiveWheels == null) return;
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


            swd.L_IndSwitch = state.rgbButtons[5] > 0;
            swd.R_IndSwitch = state.rgbButtons[4] > 0;

            swd.OtherButton =
                state.rgbButtons[0] > 0 ||
                state.rgbButtons[1] > 0 ||
                state.rgbButtons[2] > 0 ||
                state.rgbButtons[3] > 0 ||
                state.rgbButtons[6] > 0 ||
                state.rgbButtons[7] > 0 ||
                state.rgbButtons[10] > 0 ||
                state.rgbButtons[11] > 0 ||
                state.rgbButtons[23] > 0;

            /*
            int i = 0;
            string tmp = "";
            foreach (byte b in state.rgbButtons) {
                tmp += i.ToString() + ">" + b.ToString() + "  ";
                i++;
            }
            Debug.Log(tmp);
            */
            if (swd.forceFeedbackPlaying) {
                //  Debug.Log("playing force"+swd.wheelIndex+swd.ToString());
                DirectInputWrapper.PlayConstantForce(swd.wheelIndex, Mathf.RoundToInt(swd.constant * FFBGain));
                DirectInputWrapper.PlayDamperForce(swd.wheelIndex, Mathf.RoundToInt(swd.damper * FFBGain));
                //DirectInputWrapper.PlaySpringForce(wheelIndex, 0, Mathf.RoundToInt(0 * FFBGain), springCoefficient);


                DirectInputWrapper.PlaySpringForce(swd.wheelIndex, 0,
                    Mathf.RoundToInt((swd.springSaturation <= 0 ? 1 : swd.springSaturation) * FFBGain),
                    swd.springCoefficient);
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

    public void GetAccelBrakeInput(out float accel, out float brk, ParticipantOrder po) {
        if (ActiveWheels.ContainsKey(po)) {
            accel = ActiveWheels[po].gas;
            brk = ActiveWheels[po].brake;
        }
        else {
            accel = 0;
            brk = 0;
        }
    }

    public bool GetLeftIndicatorInput(ParticipantOrder po) {
        if (ActiveWheels.ContainsKey(po)) { return ActiveWheels[po].L_IndSwitch; }
        else { return false; }
    }
    public bool GetRightIndicatorInput(ParticipantOrder po) {
        if (ActiveWheels.ContainsKey(po)) { return ActiveWheels[po].R_IndSwitch; }
        else { return false; }
    }
    public bool GetButtonInput(ParticipantOrder po) {
        if (ActiveWheels.ContainsKey(po)) {  Debug.Log(po.ToString()+"Requested Button it is "+ActiveWheels[po].OtherButton); return ActiveWheels[po].OtherButton;}
        else { return false; }
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