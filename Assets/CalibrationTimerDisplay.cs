using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CalibrationTimerDisplay : MonoBehaviour {
    private TextMeshProUGUI Text;

    public const float TurnOfDelay = 1f;
    // Start is called before the first frame update
    void Start() {
        Debug.Log(transform.Find("timer"));
        Text = transform.Find("timer").GetComponent<TextMeshProUGUI>();
        Text.text = "";
        SetActive(false);
    }
    
 

    public void StartDispaly() {
        SetActive(true);
        Text.color=Color.red;
    }
    private void SetActive(bool val) {
        
        gameObject.SetActive(val);
    }
    public void updateMessage(string toString) {
        Text.text = toString;
    }

    public void StopDisplay() {
        if (gameObject.activeSelf) {
            Text.text = "Done!";
            Text.color = Color.green;
            StartCoroutine(StopDisplayCoroutine());
        }
        
    }

    private IEnumerator StopDisplayCoroutine() {
            yield return new WaitForSeconds(TurnOfDelay);
            Text.text = "";
            SetActive(false);
    }
}
