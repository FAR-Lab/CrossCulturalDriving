/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;



public class SteeringWheelInputController : MonoBehaviour {

    private static SteeringWheelInputController inited = null;


    private float steerInput = 0f;
    private float accelInput = 0f;
    private int constant = 0;
    private int damper = 0;
    private int springSaturation = 0;
    private int springCoefficient = 0;
    private bool running;


    private bool forceFeedbackPlaying = false;
    private bool debugInfo = false;

  
      

    private int minBrake;
    private int maxBrake;
    private int minGas;
    private int maxGas;

    float gas = 0;
    float brake = 0;

    private GUIStyle debugStyle;

    private int wheelIndex = 0;
    private int pedalIndex = 1;

    private int masterIndex = 2;

    public float FFBGain = 1f;
    public bool Running { get { return running; } }


    void Start() {

        debugStyle = new GUIStyle();
        debugStyle.fontSize = 45;
        debugStyle.normal.textColor = Color.white;

        if (inited == null) {
            inited = this;
        } else {
            return;
        }

        DirectInputWrapper.Init();

        wheelIndex = 0;

        minBrake = 1;
        maxBrake = -1;
        minGas = -1;
        maxGas = 1;


        FFBGain = 1.0f;


    }

    IEnumerator SpringforceFix() {
        yield return new WaitForSeconds(1f);
        StopSpringForce();
        yield return new WaitForSeconds(0.5f);
        InitSpringForce(0, 0);
    }

    public void Init() {
        forceFeedbackPlaying = true;
    }

    public void CleanUp() {
        forceFeedbackPlaying = false;
        constant = 0;
        damper = 0;
    }

    public void SetConstantForce(int force) {
        constant = force;
    }

    public void SetDamperForce(int force) {
        damper = force;
    }

    public void SetSpringForce(int sat, int coeff) {
        springCoefficient = coeff;
        springSaturation = sat;
    }
    /// DAVID: Additional controlls for the slave steering wheel to follow the main steeringwheel
    /// 

    public void InitSpringForce(int sat, int coeff) {
        StartCoroutine(_InitSpringForce(sat, coeff));
    }

    public void StopSpringForce() {
        Debug.Log("stopping spring" + DirectInputWrapper.StopSpringForce(wheelIndex));

    }

    private IEnumerator _InitSpringForce(int sat, int coeff) {

        yield return new WaitForSeconds(1f);


        Debug.Log("stopping spring" + DirectInputWrapper.StopSpringForce(wheelIndex));

        yield return new WaitForSeconds(1f);
        long res = -1;
        int tries = 0;
        while (res < 0) {
            res = DirectInputWrapper.PlaySpringForce(wheelIndex, 0, Mathf.RoundToInt(sat * FFBGain), Mathf.RoundToInt(coeff * FFBGain));
            Debug.Log("starting spring for the wheel" + res);

            tries++;
            if (tries > 150) {
                Debug.Log("coudn't init spring forcefor the steerng wheel. aborting");
                break;
            }

            yield return null;
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
        constant = 0;
        damper = 0;
        springCoefficient = 0;
        springSaturation = 0;


        yield return new WaitForSeconds(0.5f);
        ///David: what was written here
        yield return new WaitForSeconds(0.5f);
        forceFeedbackPlaying = true;
    }



    void Update() {
        if (inited != this)
            return;





        if (Application.platform != RuntimePlatform.OSXEditor) {
            DirectInputWrapper.Update();


            if (DirectInputWrapper.DevicesCount() > 0) {
                running = true;
            } else { running = false; }

                DeviceState state;


                state = DirectInputWrapper.GetStateManaged(wheelIndex);

           // Debug.Log("Device One: \tlRx: " + state.lRx + "\tlRy: " + state.lRy + "\tlRz: " + state.lRz + "\tlX: " + state.lX + "\tlY: " + state.lY + "\tlZ: " + state.lZ);

                steerInput = state.lX / 32768f;
               // accelInput = (state.lY- 32768f) / -32768f;
           
                gas = 0.9f* gas+0.1f*(( state.lY  ) / (-32768f));
               
            brake = ( state.lRz ) / (32768f);

            //Debug.Log(brake + "break and gas"+gas);
            /* x = state.lX;
         y = state.lY;
         z = state.lZ;
         s0 = state.rglSlider[0];
         s1 = state.rglSlider[1];*/
            if (forceFeedbackPlaying) {
               
                    DirectInputWrapper.PlayConstantForce(wheelIndex, Mathf.RoundToInt(constant * FFBGain));
                    DirectInputWrapper.PlayDamperForce(wheelIndex, Mathf.RoundToInt(damper * FFBGain));
                //DirectInputWrapper.PlaySpringForce(wheelIndex, 0, Mathf.RoundToInt(0 * FFBGain), springCoefficient);

                
                DirectInputWrapper.PlaySpringForce(wheelIndex, 0, Mathf.RoundToInt((springSaturation<=0 ? 1: springSaturation )* FFBGain), springCoefficient);
            }
               

                    //Debug.Log(brake.ToString() + " break and gas" + gas.ToString());
                   float totalGas = ( maxGas - minGas );
                   float totalBrake = ( maxBrake - minBrake );

                    accelInput = ( gas - minGas ) / totalGas - ( brake - minBrake ) / totalBrake;
                }
 
        
    }

    public void GetAccelBrakeInput(out float accel,out float brk) {
        accel = gas;
        brk = brake;
    }
    public float GetAccelInput() {
        return accelInput;
    }

    public float GetSteerInput() {
        return steerInput;
    }

    public float GetHandBrakeInput() {
        return 0f;
    }

}
