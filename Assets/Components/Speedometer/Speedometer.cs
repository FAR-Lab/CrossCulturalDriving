using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Speedometer : MonoBehaviour
{

    public bool isMPH = true;
    public GameObject speedText;
    private TextMeshProUGUI speedTextTMP;
    private GameObject speedTextUnit;
    public float mySpeed;
    public GameObject speedPointer;
    private RectTransform speedPointerTransform;
    private float zRotation;
    private float OriginalRotation;

    public void OnEnable()
    {
        speedTextTMP = speedText.gameObject.GetComponent<TextMeshProUGUI>();
        speedPointerTransform = speedPointer.GetComponent<RectTransform>();
        zRotation = speedPointerTransform.localEulerAngles.z;
        OriginalRotation = zRotation;


        if (!isMPH) {
            speedTextUnit.gameObject.GetComponent<TextMeshProUGUI>().text = "km/h";
        }
    }
/*    public void Update() {
        UpdateSpeed(mySpeed);
    }*/


    public void UpdateSpeed(float speed)
    {
        zRotation = OriginalRotation - (speed*3.6f* 1.5f);

        speedPointerTransform.localEulerAngles = new Vector3(0,0,zRotation);

        if (isMPH)
        {
            speedTextTMP.text = Mathf.RoundToInt(speed * 2.23694f).ToString();
        }
        else
        {
            speedTextTMP.text = Mathf.RoundToInt(speed*3.6f).ToString();
        }
    }
}