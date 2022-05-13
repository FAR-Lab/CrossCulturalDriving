using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ServerTimeDisplay : MonoBehaviour
{
    private Text TotalTime;
    private Text ScenarioTime;


    private bool running = false;
    private Coroutine m_Corutine;

    private float scenarioStartTime = 0.0f;
    private float serverStartTime = 0.0f;
    public void StartDisplay(float delay = 0.5f
    ){
        running = true;
        TotalTime = transform.GetChild(0).Find("TotalTime").GetComponent<Text>();
        ScenarioTime = transform.GetChild(0).Find("ScenarioTime").GetComponent<Text>();
        serverStartTime = Time.time;
        scenarioStartTime = Time.time;
        StartCoroutine(updateDisplay(delay));
        
    }

    private ActionState lasState = default;

    private void Update(){
        if (lasState != ConnectionAndSpawing.Singleton.ServerState){
            if (lasState == ActionState.WAITINGROOM &&
                (ConnectionAndSpawing.Singleton.ServerState == ActionState.LOADINGVISUALS ||
                 ConnectionAndSpawing.Singleton.ServerState == ActionState.LOADINGSCENARIO)){
                scenarioStartTime = Time.time;
            }
            else if (lasState !=  ActionState.WAITINGROOM &&
                (ConnectionAndSpawing.Singleton.ServerState == ActionState.WAITINGROOM)){
                scenarioStartTime = Time.time;
            }

            lasState = ConnectionAndSpawing.Singleton.ServerState;
        }

        
    }

    private string FormatSeconds(float val){
        return Mathf.FloorToInt(val / 60f).ToString() + ":" + (val % 60f).ToString("F0");
    }

    private IEnumerator updateDisplay(float delay){
        while (running){
            TotalTime.text = FormatSeconds(Time.time-serverStartTime);
            ScenarioTime.text = FormatSeconds(Time.time - scenarioStartTime);

            yield return new WaitForSeconds(delay);
        }
    }

    private void OnApplicationQuit(){
        StopDisplay();
    }

    public void StopDisplay(){
        running = false;
    }
}