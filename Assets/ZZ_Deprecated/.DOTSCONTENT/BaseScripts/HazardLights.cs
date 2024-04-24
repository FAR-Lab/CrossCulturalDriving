using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HazardLights : MonoBehaviour
{
    public Material tailLight;
    public Material frontLeftLamp;
    public Material frontRightLamp;
    [HideInInspector]
    public Color emittedReverseColor;
    [HideInInspector]
    public Color reverseColor;
    [HideInInspector]
    public Color emittedFrontColor;
    [HideInInspector]
    public Color frontColor;
    private bool turnLightOn;
    // Start is called before the first frame update

    private void OnDestroy()
    {
        tailLight.SetColor("_Color", reverseColor);
        tailLight.SetColor("_EmissionColor", emittedReverseColor);
        frontLeftLamp.SetColor("_Color", frontColor);
        frontLeftLamp.SetColor("_EmissionColor", emittedFrontColor);
        frontRightLamp.SetColor("_Color", frontColor);
        frontRightLamp.SetColor("_EmissionColor", emittedFrontColor);
        FlashLightOff(frontLeftLamp, frontRightLamp, tailLight);
    }

    void Start()
    {
        reverseColor = tailLight.GetColor("_Color");
        emittedReverseColor = tailLight.GetColor("_EmissionColor");
        frontColor = frontLeftLamp.GetColor("_Color");
        emittedFrontColor = frontLeftLamp.GetColor("_EmissionColor");
        turnLightOn = false;
    }

    // Update is called once per frame
    void Update()
    {
        ApplyLights();
    }

    private void ApplyLights()
    {
        if (turnLightOn == false)
        {
            FlashLightOff(frontLeftLamp, frontRightLamp, tailLight);
            StartCoroutine(waitForLight(true));
        }
        if (turnLightOn == true)
        {
            FlashLightOn(frontLeftLamp, frontRightLamp, tailLight);
            StartCoroutine(waitForLight(false));
        }
    }

    private void FlashLightOn(Material frontLeft, Material frontRight, Material back)
    {
        frontLeft.SetColor("_Color", new Vector4(100, 100, 0, 500));
        frontLeft.SetColor("_EmissionColor", new Vector4(100, 100, 100, 500));
        frontRight.SetColor("_Color", new Vector4(100, 100, 0, 500));
        frontRight.SetColor("_EmissionColor", new Vector4(100, 100, 100, 500));
        back.SetColor("_Color", new Vector4(100, 0, 0, 500));
        back.SetColor("_EmissionColor", new Vector4(100, 100, 100, 500));
    }

    private void FlashLightOff(Material frontLeft, Material frontRight, Material back)
    {
        frontLeft.SetColor("_Color", frontColor);
        frontLeft.SetColor("_EmissionColor", emittedFrontColor);
        frontRight.SetColor("_Color", frontColor);
        frontRight.SetColor("_EmissionColor", emittedFrontColor);
        back.SetColor("_Color", reverseColor);
        back.SetColor("_EmissionColor", emittedReverseColor);
    }

    IEnumerator waitForLight(bool turn)
    {
        yield return new WaitForSeconds(0.8f);
        turnLightOn = turn;
    }

}
