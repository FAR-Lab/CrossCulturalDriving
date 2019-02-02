using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StandAloneIndicatorAnimation : MonoBehaviour {

    public Transform[] Left;
    public Transform[] Right;
    public Transform[] BrakeLightObjects;

    public Material baseMaterial;

    public Material materialOn;
    public Material materialOff;


    public Color On;
    public Color Off;

    public bool LeftActive;
    public bool RightActive;

    private bool breakIsOn = false;


   
    private bool LeftIsActuallyOn;
    private bool RightIsActuallyOn;
    private bool ActualLightOn;


    public float indicaterTimer;
    public float interval;
    public int indicaterStage;


    public Material materialBrake;


    public Color BrakeColor;


    // Use this for initialization
    void Start () {
        materialOn = new Material(baseMaterial);
        materialOn.SetColor("_Color", On);
        materialOff = new Material(baseMaterial);
        materialOff.SetColor("_Color", Off);

        LeftIsActuallyOn = LeftActive;
        RightIsActuallyOn = RightActive;
        foreach (Transform t in Left)
        {
            t.GetComponent<MeshRenderer>().material = materialOff;
        }
        foreach (Transform t in Right)
        {
            t.GetComponent<MeshRenderer>().material = materialOff;
        }

        indicaterStage = 1;


        materialBrake = new Material(baseMaterial);
        materialBrake.SetColor("_Color", BrakeColor);
        foreach (Transform t in BrakeLightObjects)
        {
            t.GetComponent<MeshRenderer>().material = materialBrake;
        }
    }
    void UpdateIndicator()
    {
        if (indicaterStage == 1)
        {
            indicaterStage = 2;
            indicaterTimer = 0;
            ActualLightOn = false;
        }
        else if (indicaterStage == 2 || indicaterStage == 3)
        {
            indicaterTimer += Time.deltaTime;

            if (indicaterTimer > interval)
            {
                indicaterTimer = 0;
                ActualLightOn = !ActualLightOn;
                if (ActualLightOn)
                {
                    TurnOnLeft(LeftIsActuallyOn);
                    TurnOnRight(RightIsActuallyOn); 
                }
                else
                {
                    TurnOnLeft(false);
                    TurnOnRight(false);
                }
            }
           

        }
        else if (indicaterStage == 4)
        {
            indicaterStage = 0;
            ActualLightOn = false;
            LeftIsActuallyOn = false;
            RightIsActuallyOn = false;
            TurnOnLeft(false);
            TurnOnRight(false);
           

        }
    }

    public void TurnOnLeft(bool Leftl_)
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
    public void TurnOnRight(bool Rightl_)
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


    // Update is called once per frame
    void Update () {
        UpdateIndicator();

    }
}
