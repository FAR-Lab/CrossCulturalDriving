 using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ServerTimeDisplay : MonoBehaviour
{
    private Text TotalTime;
    private Text ScenarioTime;
    private Text ScenarioName;

    private bool running = false;
    private Coroutine m_Corutine;

    private int scenarioStartTime = 0;
    private int serverStartTime = 0;

    private bool init = false;
    private void Start() {
        ConnectionAndSpawning.Singleton.ServerStateChange += OnStateChange;
        gameObject.SetActive(false);
    }

    private void OnStateChange(ActionState state) {
        switch (state) {
            case ActionState.DEFAULT:
                break;
            case ActionState.WAITINGROOM:
                if (!init) run_init();
                scenarioStartTime =Mathf.RoundToInt( Time.time);
                ScenarioName.text = ConnectionAndSpawning.Singleton.GetLoadedScene();
                break;
            case ActionState.LOADINGSCENARIO:
                break;
            case ActionState.LOADINGVISUALS:
                break;
            case ActionState.READY:
                scenarioStartTime = Mathf.RoundToInt( Time.time);
                ScenarioName.text = ConnectionAndSpawning.Singleton.GetLoadedScene();
                break;
            case ActionState.DRIVE:
                break;
            case ActionState.QUESTIONS:
                break;
            case ActionState.POSTQUESTIONS:
                break;
            case ActionState.RERUN:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }


    private void run_init() {
        init = true;
        StartDisplay();
    }
    public void StartDisplay(float delay = 0.5f
    ){
        gameObject.SetActive(true);
        running = true;
        TotalTime = transform.Find("TotalTime").GetComponent<Text>();
        ScenarioTime = transform.Find("ScenarioTime").GetComponent<Text>();
        ScenarioName = transform.Find("ScenarioName").GetComponent<Text>();
        serverStartTime = Mathf.RoundToInt(Time.time);
        scenarioStartTime =Mathf.RoundToInt(Time.time);
        StartCoroutine(updateDisplay(delay));
     
    }

    private void TimerReset(){
        serverStartTime =Mathf.RoundToInt( Time.time);
       
    }

    private ActionState lasState = default;

    private void Update(){
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyUp(KeyCode.T)){
            TimerReset();
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