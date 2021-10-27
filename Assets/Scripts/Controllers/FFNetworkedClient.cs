/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class FFNetworkedClient : MonoBehaviour
{

  
    
    private SteeringWheelInputController logi;
  
    public float springSaturation;
    public float springCoeff;
    public int damperAmount = 3000;
    float selfAlignmentTorque;
   
    
    bool changedtoQuestionair = false;
    
    float forcelerperQuestionair = -1;
    ActionState previousACtionstate;


    private StateManager _stateManager;
    private ParticipantInputCapture _participantInputCapture;
    void Start()
    {

        _stateManager = GetComponent<StateManager>();
        _participantInputCapture = GetComponent<ParticipantInputCapture>();
        if (transform.GetComponent<SteeringWheelInputController>()!=null)
        {
            logi = transform.GetComponent<SteeringWheelInputController>();

            logi.SetConstantForce(0);
            logi.SetDamperForce(0);
            logi.SetSpringForce(0, 0);
            logi.Init();
        }
        else
        {
            this.enabled = false;
        }

        _stateManager.GlobalState.OnValueChanged += StartTransition;
       // Debug.Log("State,Network,Steering:"+_stateManager?.ToString()+
                //  _participantInputCapture?.ToString()+logi?.ToString());

    }

    private void StartTransition(ActionState previousvalue, ActionState newvalue)
    {
        if (newvalue == ActionState.QUESTIONS)
        {

            forcelerperQuestionair = 0;
            
        }
        else if(newvalue == ActionState.DRIVE || newvalue == ActionState.READY)
        {
            forcelerperQuestionair = -1;
        }
    }


    void LateUpdate()
    {

        if (logi == null) {logi = transform.GetComponent<SteeringWheelInputController>();
            return;}
        if (_stateManager == null) {_stateManager = GetComponent<StateManager>();
            return;}
        
       
        
         
               
           
       
                if (_stateManager.GlobalState.Value==ActionState.QUESTIONS) //SceneStateManager.Instance.ActionState == ActionState.QUESTIONS && changedtoQuestionairs
                {
                    if (forcelerperQuestionair >= 0 && forcelerperQuestionair<=1)
                    {
                        forcelerperQuestionair += Time.deltaTime;
                    }
                    if (forcelerperQuestionair > 1 )
                    {
                        forcelerperQuestionair = 1;
                    }
                    if (logi.GetSteerInput() > 0.025f)
                    {
                        logi.SetConstantForce((int)(0.25 * (10000f*forcelerperQuestionair)));

                    }
                    else if (logi.GetSteerInput() < -0.025f)
                    {
                        logi.SetConstantForce((int)(-0.25f * (10000f*forcelerperQuestionair)));
                    }
                    else
                    {
                        logi.SetConstantForce((int)(0));
                    }
                    logi.SetSpringForce(Mathf.RoundToInt(springSaturation * Mathf.Abs(0) * 10000f), Mathf.RoundToInt(springCoeff * 10000f));
                    logi.SetDamperForce((int)damperAmount/2);
                }
                
                else if (_stateManager.GlobalState.Value==ActionState.DRIVE)
                {
                    float forceFeedback = _participantInputCapture.selfAlignmentTorque.Value;

                    //logi.Init();
                    logi.SetConstantForce((int)(forceFeedback * 10000f));
                    logi.SetSpringForce(Mathf.RoundToInt(springSaturation * Mathf.Abs(forceFeedback) * 10000f), Mathf.RoundToInt(springCoeff * 10000f));
                    logi.SetDamperForce(damperAmount);
                }
                else{ 
                    logi.SetConstantForce(0);
                    logi.SetDamperForce(0);
                    logi.SetSpringForce(0, 0);
                    
                }
            
        
    }
}
