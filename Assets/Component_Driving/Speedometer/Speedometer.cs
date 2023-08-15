using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UltimateReplay;
using UnityEngine.UI;

public class Speedometer : ReplayBehaviour
{
    
    [ReplayVar(false)]
    public bool isMPH = true;
    public GameObject speedText;


    private TextMeshProUGUI speedTextTMP;


    private GameObject speedTextUnit;
    [ReplayVar(false)] public float mySpeed;
    public GameObject speedPointer;
    private RectTransform speedPointerTransform;


    private float zRotation;
    private float OriginalRotation;

    public void Start()
    {
        speedTextTMP = speedText.gameObject.GetComponent<TextMeshProUGUI>();
        speedPointerTransform = speedPointer.GetComponent<RectTransform>();
        zRotation = speedPointerTransform.localEulerAngles.z;
        OriginalRotation = zRotation;


        if (!isMPH)
        {
            speedTextUnit.gameObject.GetComponent<TextMeshProUGUI>().text = "km/h";
        }
        
        if (ConnectionAndSpawing.Singleton.ServerState == ActionState.RERUN)
        {
            UpdateSpeed(mySpeed, true);
            GetComponentInChildren<Canvas>().enabled = true;
            foreach (var b in GetComponentsInChildren<Image>())
            {
                b.enabled= true;
            }
            foreach (var b in GetComponentsInChildren<TextMeshProUGUI>())
            {
                b.enabled= true;
            }
        }
    }

    private void Update()
    {
        if (ConnectionAndSpawing.Singleton.ServerState == ActionState.RERUN)
        {
            UpdateSpeed(mySpeed, true);
        }
    }


    public void UpdateSpeed(float speed, bool IsReplaying = false)
    {
        if(speedPointerTransform==null)
        {
            Debug.Log("Could not find speedometer. Trying again");
            Start();
        }
        if (!IsReplaying)
        {
            mySpeed = speed;
        }

        zRotation = OriginalRotation - (speed * 3.6f * 1.5f);
        speedPointerTransform.localEulerAngles = new Vector3(0, 0, zRotation);

        if (isMPH)
        {
            speedTextTMP.text = Mathf.RoundToInt(speed * 2.23694f).ToString();
        }
        else
        {
            speedTextTMP.text = Mathf.RoundToInt(speed * 2.23694f).ToString();
        }
    }
}