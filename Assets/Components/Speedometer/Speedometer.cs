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
    private Vector3 Rotation;
    private Vector3 OriginalRotation;

    public void OnEnable()
    {
        speedTextTMP = speedText.gameObject.GetComponent<TextMeshProUGUI>();
        Rotation = speedPointer.transform.eulerAngles;
        OriginalRotation = speedPointer.transform.eulerAngles;


        if (!isMPH) {
            speedTextUnit.gameObject.GetComponent<TextMeshProUGUI>().text = "km/h";
        }
    }
/*    public void Update() {
        UpdateSpeed(mySpeed);
    }*/


    public void UpdateSpeed(float speed)
    {
        Rotation.z = OriginalRotation.z - (speed * 1.5f);

        speedPointer.transform.eulerAngles = Rotation;

        if (isMPH)
        {
            speedTextTMP.text = Mathf.RoundToInt(speed / 1.609f).ToString();
        }
        else
        {
            speedTextTMP.text = Mathf.RoundToInt(speed).ToString();
        }
    }
}